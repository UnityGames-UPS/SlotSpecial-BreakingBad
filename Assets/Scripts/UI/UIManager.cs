using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
  [SerializeField] private SlotManager slotManager;
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private BonusManager bonusManager;

  [Header("Spin & Autoplay Rework UI")]
  [SerializeField] private GameObject autoplaySelectionPanel;
  [SerializeField] private TMP_Dropdown autoplayOptionsDropdown;
  [SerializeField] private Button autoplayStartButton;
  [SerializeField] private GameObject autoplayCounterObject;
  [SerializeField] private TMP_Text autoplayCounterText;

  [Header("Jackpot UI")]
  [SerializeField] private List<TMP_Text> JackpotText;

  [Header("RaycastBlocker")]
  [SerializeField] internal GameObject RaycastBlocker;

  [Header("Feature Panels")]
  [SerializeField] private GameObject featureWinPanel;
  [SerializeField] private GameObject spinCounterPanel;
  [SerializeField] private TMP_Text featureWinText;
  [SerializeField] private TMP_Text spinCounterText;

  [Header("Feature Popup UI")]
  [SerializeField] private GameObject featurePopup;
  [SerializeField] private Button featureStartButton;
  [SerializeField] private GameObject featureTitleObject;
  [SerializeField] private GameObject featureWinObject;
  [SerializeField] private TMP_Text featureWinAmountText;

  [Header("Walter Stash Grand Prize Popup UI")]
  [SerializeField] private GameObject walterStashPopup;
  [SerializeField] private TMP_Text walterStashAmountText;

  [Header("Feature Controls")]
  [SerializeField] private Button featureSpinButton;

  private int totalBonusSpins = 3;

  internal Coroutine BonusCoroutine;
  internal bool animationFinish = false;
  internal int multiplierCount = 0;
  private Jackpot currentJackpotData;

  // Registered Slot Buttons & Texts (from SlotManager)
  [Header("HUD Objects")]
  [SerializeField] private Button slotStartButton;
  [SerializeField] private Button autoSpinButton;
  [SerializeField] private Button autoSpinStopButton;
  [SerializeField] private Button totalBetPlusButton;
  [SerializeField] private Button totalBetMinusButton;

  [SerializeField] private Button turboButton;
  [SerializeField] private GameObject turboOnObject;
  [SerializeField] private Button stopSpinButton;

  [SerializeField] private TMP_Text balanceText;
  [SerializeField] private TMP_Text totalBetText;
  [SerializeField] private TMP_Text totalWinText;
  [SerializeField] private TMP_Text fsNumText;
  


  private Tween BalanceTween;

  private void Start()
  {
    if (totalWinText != null) totalWinText.text = "0.000";
    if (featureWinPanel != null) featureWinPanel.SetActive(false);
    if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
    if (featurePopup != null) featurePopup.SetActive(false);
    if (walterStashPopup != null) walterStashPopup.SetActive(false);
    if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

    // Bind to Model Events
    slotManager.OnBalanceChanged += UpdateBalanceText;
    slotManager.OnTotalBetChanged += UpdateTotalBetText;
    slotManager.OnFreeSpinsChanged += UpdateFreeSpinsText;


    slotManager.OnSpinStateChanged += HandleSpinStateChanged;
    slotManager.OnAutoSpinStateChanged += HandleAutoplayStateChanged;
    slotManager.OnAutoplayCountChanged += UpdateAutoplayCounter;
    slotManager.OnAutoplayStopped += HandleAutoplayStopped;

    if (autoplayStartButton) {
      autoplayStartButton.onClick.RemoveAllListeners();
      autoplayStartButton.onClick.AddListener(OnAutoplayStartPressed);
    }
    if (autoplayOptionsDropdown) {
      autoplayOptionsDropdown.ClearOptions();
      List<string> options = new List<string> {
          "Until Feature",
          "100 Spins",
          "50 Spins",
          "30 Spins",
          "15 Spins",
          "10 Spins",
          "5 Spins",
          "3 Spins"
      };
      autoplayOptionsDropdown.AddOptions(options);
    }
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);
    if (autoplayCounterObject) autoplayCounterObject.SetActive(false);

    InitializeHUD();

    if (featureSpinButton != null)
    {
        featureSpinButton.onClick.RemoveAllListeners();
        featureSpinButton.onClick.AddListener(() => {
            if (featureSpinButton.IsInteractable() && slotManager.IsBonus && !bonusManager.IsSpinning)
            {
                bonusManager.StartBonusSlot();
            }
        });
    }
  }


  private void InitializeHUD()
  {
      // Register HUD Click Handlers
      if (slotStartButton) {
          slotStartButton.onClick.RemoveAllListeners();
          
          HoldButtonHandler holdHandler = slotStartButton.gameObject.GetComponent<HoldButtonHandler>();
          if (holdHandler == null) {
              holdHandler = slotStartButton.gameObject.AddComponent<HoldButtonHandler>();
          }
          holdHandler.onClick.RemoveAllListeners();
          holdHandler.onClick.AddListener(() => {
              if (slotStartButton.IsInteractable() && slotManager && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin && !slotManager.IsBonus) {
                  // Allow starting a new spin even if animations are playing
                  // ForceCleanupPreviousSpin inside StartSlots handles cleanup
                  slotManager.StartSlots();
                  CanCloseMenu();
              }
          });
          holdHandler.onLongPress.RemoveAllListeners();
          holdHandler.onLongPress.AddListener(() => {
              if (slotStartButton.IsInteractable() && slotManager && !slotManager.IsSpinning && !slotManager.IsAutoSpin && !slotManager.IsBonus) {
                  OpenAutoplayPanel();
                  CanCloseMenu();
              }
          });
      }
      if (autoSpinButton) {
          autoSpinButton.onClick.RemoveAllListeners();
          autoSpinButton.onClick.AddListener(() => {
              if (autoSpinButton.IsInteractable()) {
                  OpenAutoplayPanel();
                  CanCloseMenu();
              }
          });
      }
      if (autoSpinStopButton) {
          autoSpinStopButton.onClick.RemoveAllListeners();
          autoSpinStopButton.onClick.AddListener(() => { if (slotManager) slotManager.StopAutoSpin(); CanCloseMenu(); });
      }
      if (totalBetPlusButton) {
          totalBetPlusButton.onClick.RemoveAllListeners();
          totalBetPlusButton.onClick.AddListener(() => { if (slotManager) slotManager.ChangeBet(true); CanCloseMenu(); });
      }
      if (totalBetMinusButton) {
          totalBetMinusButton.onClick.RemoveAllListeners();
          totalBetMinusButton.onClick.AddListener(() => { if (slotManager) slotManager.ChangeBet(false); CanCloseMenu(); });
      }

      if (turboButton) {
          turboButton.onClick.RemoveAllListeners();
          turboButton.onClick.AddListener(() => { TurboToggle(); CanCloseMenu(); });
          SetTurboActiveState(slotManager != null ? slotManager.IsTurboOn : false);
      }
      if (stopSpinButton) {
          stopSpinButton.onClick.RemoveAllListeners();
          stopSpinButton.onClick.AddListener(() => { 
              if (slotManager)
              {
                  if (slotManager.IsBonus)
                  {
                      if (bonusManager != null)
                      {
                          bonusManager.StopSpinToggle = true;
                          // Immediately change stop button to disabled feature spin button during feature
                          UpdateFeatureButtonsState(false, slotManager.LinkRespinsRemaining);
                          if (featureSpinButton != null) featureSpinButton.interactable = false;
                      }
                  }
                  else
                  {
                      slotManager.StopSpinToggle = true;
                      // Immediately change stop button to disabled spin button for normal game
                      ShowSpinButtonCooldown(true);
                  }
              }
          });
      }

      UpdateButtonsState();
  }



  public void SetNormalSpinButtonActive(bool active)
  {
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
  }

  public void SetBonusSpinCounter(int count)
  {
      if (spinCounterText != null)
      {
          spinCounterText.text = count.ToString() + "/" + totalBonusSpins.ToString();
      }
      if (autoplayCounterText != null)
      {
          autoplayCounterText.text = count.ToString();
      }
  }

  public void OpenBonusUI(int totalSpins, double initialWin)
  {
      totalBonusSpins = totalSpins;
      if (featureWinPanel != null) featureWinPanel.SetActive(false);
      if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
      
      // Hide standard UI elements during bonus game
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(false);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
      if (turboButton != null) turboButton.gameObject.SetActive(false);
      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(true);

      if (featureWinText != null) featureWinText.text = initialWin.ToString("f3");
      SetBonusSpinCounter(totalSpins);
      UpdateFeatureButtonsState(false, totalSpins);
  }

  public void CloseBonusUI()
  {
      if (featureWinPanel != null) featureWinPanel.SetActive(false);
      if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);
      if (stopSpinButton != null && slotManager.IsBonus) stopSpinButton.gameObject.SetActive(false);

      // Restore standard UI elements when exiting bonus game
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(true);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(true);
      if (turboButton != null) turboButton.gameObject.SetActive(true);
      
      // Also update buttons state to match current normal/autoplay state
      UpdateButtonsState();
  }

  public void SetFeatureWinText(double value)
  {
      if (featureWinText != null)
      {
          featureWinText.text = value.ToString("f3");
      }
  }

  public void OpenFeaturePopup(Action onStartClicked)
  {
      if (featurePopup == null || featureStartButton == null)
      {
          onStartClicked?.Invoke();
          return;
      }

      // Hide standard buttons and count UI when feature popup gets active
      if (slotStartButton != null) slotStartButton.gameObject.SetActive(false);
      if (turboButton != null) turboButton.gameObject.SetActive(false);
      if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton != null) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      if (featureTitleObject != null)
      {
          featureTitleObject.SetActive(true);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(false);
      }
      if (walterStashPopup != null)
      {
          walterStashPopup.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(true);
      }
      featureStartButton.onClick.RemoveAllListeners();
      featureStartButton.onClick.AddListener(() => {
          featurePopup.SetActive(false);
          onStartClicked?.Invoke();
      });
  }

  public void OpenFeatureWinPopup(double winAmount, Action onCloseClicked)
  {
      if (featurePopup == null || featureStartButton == null)
      {
          onCloseClicked?.Invoke();
          return;
      }

      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      if (featureTitleObject != null)
      {
          featureTitleObject.SetActive(false);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(true);
      }
      if (walterStashPopup != null)
      {
          walterStashPopup.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(true);
      }
      if (featureWinAmountText != null)
      {
          featureWinAmountText.text = winAmount.ToString("f3");
      }
      featureStartButton.onClick.RemoveAllListeners();
      featureStartButton.onClick.AddListener(() => {
          featurePopup.SetActive(false);
          onCloseClicked?.Invoke();
      });
  }

  public void OpenWalterStashPopup(double amount, Action onComplete)
  {
      if (featurePopup == null || walterStashPopup == null)
      {
          onComplete?.Invoke();
          return;
      }

      if (autoplayCounterObject != null) autoplayCounterObject.SetActive(false);
      if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

      featurePopup.SetActive(true);
      if (featureTitleObject != null)
      {
          featureTitleObject.SetActive(false);
      }
      if (featureWinObject != null)
      {
          featureWinObject.SetActive(false);
      }
      if (featureStartButton != null)
      {
          featureStartButton.gameObject.SetActive(false);
      }

      walterStashPopup.SetActive(true);
      if (walterStashAmountText != null)
      {
          walterStashAmountText.text = amount.ToString("f3");
      }

      StartCoroutine(CloseWalterStashAfterDelay(2f, onComplete));
  }

  private IEnumerator CloseWalterStashAfterDelay(float delay, Action onComplete)
  {
      yield return new WaitForSeconds(delay);
      if (walterStashPopup != null)
      {
          walterStashPopup.SetActive(false);
      }
      onComplete?.Invoke();
  }

  public void UpdateFeatureButtonsState(bool isSpinning, int remaining)
  {
      if (featureSpinButton != null)
      {
          featureSpinButton.gameObject.SetActive(!isSpinning);
          featureSpinButton.interactable = !isSpinning && (remaining > 0);
      }
      if (stopSpinButton != null)
      {
          stopSpinButton.gameObject.SetActive(isSpinning);
          stopSpinButton.interactable = isSpinning;
      }
  }

  public void AddFreeSpinsText(int count)
  {
      if (fsNumText != null && int.TryParse(fsNumText.text, out int currentVal))
      {
          fsNumText.text = (currentVal + count).ToString();
      }
  }

  public void SetTotalWinText(string text)
  {
      if (totalWinText) totalWinText.text = text;
  }

  public void ShowStopButton(bool show)
  {
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(show);
  }

  // Shows spin button in non-interactable state during cooldown after stop press
  public void ShowSpinButtonCooldown(bool cooldown)
  {
      if (cooldown)
      {
          if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
          if (slotStartButton)
          {
              slotStartButton.gameObject.SetActive(true);
              slotStartButton.interactable = false;
          }
      }
      else
      {
          UpdateButtonsState();
      }
  }

  private void UpdateBalanceText(double val)
  {
      if (balanceText) balanceText.text = val.ToString("f3");
  }


  private void UpdateTotalBetText(double val)
  {
      if (totalBetText) totalBetText.text = val.ToString();
      UpdateJackpotDisplay();
  }

  private void UpdateFreeSpinsText(int val)
  {
      if (fsNumText) fsNumText.text = val.ToString();
  }

  public void SetFreeSpinsActive(bool active)
  {
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
      if (slotStartButton) slotStartButton.interactable = !active;
      if (autoSpinButton) autoSpinButton.gameObject.SetActive(!active);
      if (autoSpinButton) autoSpinButton.interactable = true;
      if (totalBetPlusButton) totalBetPlusButton.interactable = false;
      if (totalBetMinusButton) totalBetMinusButton.interactable = false;
  }

  public void SetButtonsInteractable(bool toggle)
  {
      if (toggle && slotManager != null && (slotManager.IsBonus || slotManager.IsFeatureTransitioning || slotManager.IsFreeSpin))
      {
          toggle = false;
      }
      if (slotStartButton && !slotManager.IsAutoSpin) slotStartButton.interactable = toggle;
      if (autoSpinButton && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin) autoSpinButton.gameObject.SetActive(toggle);
      if (autoSpinButton && !slotManager.IsAutoSpin) autoSpinButton.interactable = toggle;
      if (totalBetPlusButton && !slotManager.IsAutoSpin) totalBetPlusButton.interactable = toggle;
      if (totalBetMinusButton && !slotManager.IsAutoSpin) totalBetMinusButton.interactable = toggle;
  }

  public void SetAutoSpinActive(bool active)
  {
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(active);
      if (autoSpinButton) autoSpinButton.gameObject.SetActive(!active);
  }

  private void SetTurboActiveState(bool active)
  {
      if (turboOnObject != null)
      {
          turboOnObject.SetActive(active);
      }
      else if (turboButton != null)
      {
          Transform foundChild = null;
          for (int i = 0; i < turboButton.transform.childCount; i++)
          {
              Transform child = turboButton.transform.GetChild(i);
              string lowerName = child.name.ToLower();
              if (lowerName.Contains("on") || lowerName.Contains("active") || lowerName.Contains("turbo"))
              {
                  foundChild = child;
                  break;
              }
          }
          if (foundChild == null && turboButton.transform.childCount > 0)
          {
              foundChild = turboButton.transform.GetChild(0);
          }
          if (foundChild != null)
          {
              foundChild.gameObject.SetActive(active);
          }
      }
  }

  private void TurboToggle()
  {
    if (slotManager.IsTurboOn)
    {
      slotManager.IsTurboOn = false;
      SetTurboActiveState(false);
    }
    else
    {
      slotManager.IsTurboOn = true;
      SetTurboActiveState(true);
    }
  }



  internal void CanCloseMenu()
  {
  }

  internal void LowBalPopup()
  {
    // No-op to remove low balance popup
  }

  internal void PopulateWin(int value)
  {
    // No-op to remove win popups/animations from UI
  }



  internal void ADfunction()
  {
    // No-op to remove another device popup
  }

  internal void InitialiseUIData(PaylineData symbolsText)
  {
    // No-op
  }

  internal IEnumerator TrailRendererAnimation(GameObject TrailRendererGO, int textIndex, int coinvalue, bool IsBonus = false)
  {
    yield break;
  }

  internal IEnumerator MidGameImageAnimation(ImageAnimation imageAnimation, double num = 0)
  {
    yield break;
  }





  private void CallOnExitFunction()
  {
    slotManager.CallCloseSocket();
  }



  internal void SetJackpotText(Jackpot jackpot)
  {
    currentJackpotData = jackpot;
    UpdateJackpotDisplay();
  }

  internal void UpdateJackpotDisplay()
  {
    if (currentJackpotData == null || currentJackpotData.payout == null || JackpotText == null) return;

    double totalBet = slotManager != null ? slotManager.TotalBet : 0;

    for (int i = 0; i < currentJackpotData.payout.Count; i++)
    {
      if (i >= JackpotText.Count) break;
      if (JackpotText[i] != null)
      {
        double multiplier = currentJackpotData.payout[i];
        double jackpotValue = multiplier * totalBet;
        JackpotText[i].text = jackpotValue.ToString("F2");
      }
    }
  }

  internal void DisconnectionPopup()
  {
    // No-op to remove disconnection popup
  }

  internal void CheckAndClosePopups()
  {
    // No-op
  }

  internal void ReconnectionPopup()
  {
    // No-op to remove reconnection popup
  }

  internal void OpenFreeSpinsUI()
  {
    if (fsNumText) fsNumText.text = slotManager.FreeSpinsCount.ToString();
  }

  internal void CloseFreeSpinsUI()
  {
    slotManager.IsFreeSpin = false;
    if (fsNumText) fsNumText.text = "0";
  }

  internal void WinningsTextAnimation()
  {
    double winAmt = slotManager.WinAmount;
    if (!double.TryParse(balanceText.text, out double currentBal))
    {
      Debug.Log("Error balance conversion: " + balanceText.text);
    }
    if (!double.TryParse(socketManager.playerdata.balance.ToString("f3"), out double Balance))
    {
      Debug.Log("Error: " + socketManager.playerdata.balance);
    }
    if (!double.TryParse(totalWinText.text, out double currentWin))
    {
      Debug.Log("Error total win: " + totalWinText.text);
    }
    DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
    {
      if (totalWinText) totalWinText.text = currentWin.ToString("f3");
    });
    BalanceTween?.Kill();
    DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
    {
      if (balanceText) balanceText.text = currentBal.ToString("f3");
    });
  }

  internal void DeductBalanceUI()
  {
    double bet = slotManager.TotalBet;
    double balance = slotManager.Balance;
    double initAmount = balance;
    balance -= bet;

    BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
    {
      if (balanceText) balanceText.text = initAmount.ToString("f3");
    });
  }
  
  public void SwitchTopUI(bool trigger)
  {
    // No-op for now
  }

  // --- Autoplay & Spin Button Rework Methods ---

  private void OpenAutoplayPanel()
  {
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(true);
  }

  private void OnAutoplayStartPressed()
  {
    if (!autoplayOptionsDropdown) return;

    int selectedIndex = autoplayOptionsDropdown.value;
    string selectedText = autoplayOptionsDropdown.options[selectedIndex].text;

    int spinCount = 0;
    bool untilFeature = false;

    if (selectedText.Equals("Until Feature", StringComparison.OrdinalIgnoreCase))
    {
      untilFeature = true;
      spinCount = -1;
    }
    else
    {
      string[] parts = selectedText.Split(' ');
      if (parts.Length > 0 && int.TryParse(parts[0], out int count))
      {
        spinCount = count;
      }
      else
      {
        spinCount = 100; // default fallback
      }
    }

    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);

    // Call SlotManager to start autoplay
    slotManager.StartAutoplay(spinCount, untilFeature);
  }

  private void HandleSpinStateChanged(bool isSpinning)
  {
    UpdateButtonsState();
  }

  private void HandleAutoplayStateChanged(bool isAutoSpin)
  {
    UpdateButtonsState();
  }

  private void UpdateAutoplayCounter(int count, bool untilFeature)
  {
    if (untilFeature)
    {
      if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    }
    else
    {
      if (autoplayCounterObject) autoplayCounterObject.SetActive(true);
      if (autoplayCounterText) autoplayCounterText.text = count.ToString();
    }
  }

  private void HandleAutoplayStopped()
  {
    if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    UpdateButtonsState();
  }

  public void UpdateButtonsState()
  {
    if (slotManager == null) return;

    // Early exit if the feature popup or walter stash popup is active to ensure count UI and standard buttons remain inactive
    if ((featurePopup != null && featurePopup.activeSelf) || (walterStashPopup != null && walterStashPopup.activeSelf))
    {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (turboButton) turboButton.gameObject.SetActive(false);
        if (autoSpinButton) autoSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
        if (featureSpinButton) featureSpinButton.gameObject.SetActive(false);
        return;
    }

    if (slotManager.IsAutoSpin)
    {
      if (slotStartButton) slotStartButton.gameObject.SetActive(false);
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(true);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(true);
    }
    else if (slotManager.IsFreeSpin)
    {
      if (slotStartButton) {
        slotStartButton.gameObject.SetActive(true);
        slotStartButton.interactable = false;
      }
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);
    }
    else if (slotManager.IsSpinning)
    {
      if (slotStartButton) slotStartButton.gameObject.SetActive(false);
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(true);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);
    }
    else
    {
      if (slotStartButton) {
        slotStartButton.gameObject.SetActive(true);
        // Disable the normal spin button if the feature is active or transition/trigger is happening
        slotStartButton.interactable = !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
      }
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);

      // Also ensure all other buttons are non-interactable during feature transitions/triggers
      if (slotManager.IsBonus || slotManager.IsFeatureTransitioning)
      {
          SetButtonsInteractable(false);
      }
    }
  }
}
