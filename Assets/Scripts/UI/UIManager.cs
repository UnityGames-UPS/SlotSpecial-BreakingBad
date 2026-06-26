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
  [SerializeField] private PopupManager popupManager;

  [System.Serializable]
  public struct SymbolTextMap
  {
      public int symbolId;
      public TMP_Text textComponent;
  }

  [Header("Info Panel UI")]
  [SerializeField] private GameObject infoPanel;
  [SerializeField] private Button infoButton;
  [SerializeField] private Button infoBackButton;
  [SerializeField] private List<SymbolTextMap> symbolTextMaps;

  [Header("Spin & Autoplay Rework UI")]
  [SerializeField] private GameObject autoplaySelectionPanel;
  [SerializeField] private TMP_Dropdown autoplayOptionsDropdown;
  [SerializeField] private Button autoplayStartButton;
  [SerializeField] private Button autoplayPanelClose;
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

  [Header("Free Spin Transition UI")]
  [SerializeField] private CanvasGroup normalBgCanvasGroup;
  [SerializeField] private CanvasGroup freeSpinBgCanvasGroup;
  [SerializeField] private TMP_Text losPolosTextPrefab;
  [SerializeField] private Transform spinCountSnapParent;
  [SerializeField] private RectTransform flyingTextParent;
  [SerializeField] private GameObject freeSpinPopupTitleObject;
  [SerializeField] private TMP_Text freeSpinPopupSpinsText;
  [SerializeField] private GameObject midSumAnimObj;
  [SerializeField] private GameObject dissolveAnimObj;

  [Header("Cash Collect Feature UI")]
  [SerializeField] private CanvasGroup jackpotPanelCanvasGroup;
  [SerializeField] private CanvasGroup coinWinDisplayPanelCanvasGroup;
  [SerializeField] private TMP_Text coinWinDisplayText;
  [SerializeField] private GameObject trailRendererPrefab;
  [Range(0.2f, 3.0f)]
  [SerializeField] private float trailMoveDuration = 1.0f;
  [SerializeField] private float delayBetweenTrails = 0.5f;

  internal int totalFreeSpins = 0;
  private double accumulatedFreeSpinWin = 0f;
  internal double AccumulatedFreeSpinWin => accumulatedFreeSpinWin;

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
  [SerializeField] private Button gameExitButton;

  [SerializeField] private TMP_Text balanceText;
  [SerializeField] private TMP_Text totalBetText;
  [SerializeField] private TMP_Text totalWinText;
  [SerializeField] private TMP_Text fsNumText;
  


  private Tween BalanceTween;
  private Coroutine autoClickCoroutine;

  private void Start()
  {

    if (totalWinText != null) totalWinText.text = "00.00";
    if (featureWinPanel != null) featureWinPanel.SetActive(false);
    if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
    if (featurePopup != null) featurePopup.SetActive(false);
    if (walterStashPopup != null) walterStashPopup.SetActive(false);
    if (featureSpinButton != null) featureSpinButton.gameObject.SetActive(false);

    if (midSumAnimObj != null) midSumAnimObj.SetActive(false);
    if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);

    if (normalBgCanvasGroup != null) normalBgCanvasGroup.alpha = 1f;
    if (freeSpinBgCanvasGroup != null) freeSpinBgCanvasGroup.alpha = 0f;

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
    if (autoplayPanelClose) {
      autoplayPanelClose.onClick.RemoveAllListeners();
      autoplayPanelClose.onClick.AddListener(CloseAutoplayPanel);
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
          "3 Spins",
          "" // Extra item at the end for scroll padding
      };
      autoplayOptionsDropdown.AddOptions(options);

      DropdownItemDisabler disabler = autoplayOptionsDropdown.gameObject.GetComponent<DropdownItemDisabler>();
      if (disabler == null) {
          disabler = autoplayOptionsDropdown.gameObject.AddComponent<DropdownItemDisabler>();
      }
      disabler.indexesToDisable = new List<int> { options.Count - 1 };
    }
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);
    if (autoplayCounterObject) autoplayCounterObject.SetActive(false);

    if (infoButton != null)
    {
        infoButton.onClick.RemoveAllListeners();
        infoButton.onClick.AddListener(OpenInfoPanel);
    }
    if (infoBackButton != null)
    {
        infoBackButton.onClick.RemoveAllListeners();
        infoBackButton.onClick.AddListener(CloseInfoPanel);
    }
    if (infoPanel != null)
    {
        infoPanel.SetActive(false);
    }

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
          stopSpinButton.onClick.AddListener(() => { PerformStop(); });
      }

      if (gameExitButton) {
          gameExitButton.onClick.RemoveAllListeners();
          gameExitButton.onClick.AddListener(() => {
              if (popupManager != null) {
                  popupManager.ShowExitGamePopup();
              } else {
                  CallOnExitFunction();
              }
              CanCloseMenu();
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

      if (featureWinText != null) featureWinText.text = FormatSpriteText(FormatStaticValue(initialWin));
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
          featureWinText.text = FormatSpriteText(FormatStaticValue(value));
  }

  private IEnumerator AutoClickFeatureStart(float delay)
  {
      yield return new WaitForSeconds(delay);
      if (featureStartButton != null && featureStartButton.gameObject.activeSelf && featureStartButton.interactable)
      {
          featureStartButton.onClick.Invoke();
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
      bool isFSTriggered = (slotManager.OriginalFeatureTriggerResult != null &&
                            slotManager.OriginalFeatureTriggerResult.payload != null &&
                            slotManager.OriginalFeatureTriggerResult.payload.isFreeSpinTriggered) ||
                           (slotManager.ResultData != null &&
                            slotManager.ResultData.payload != null &&
                            slotManager.ResultData.payload.isFreeSpinTriggered);
      if (slotManager != null && !slotManager.IsBonus && (slotManager.IsFreeSpin || isFSTriggered))
      {
          if (featureTitleObject != null) featureTitleObject.SetActive(false);
          if (freeSpinPopupTitleObject != null) freeSpinPopupTitleObject.SetActive(true);
          if (freeSpinPopupSpinsText != null) freeSpinPopupSpinsText.text = totalFreeSpins.ToString();
      }
      else
      {
          if (featureTitleObject != null) featureTitleObject.SetActive(true);
          if (freeSpinPopupTitleObject != null) freeSpinPopupTitleObject.SetActive(false);
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
          if (autoClickCoroutine != null)
          {
              StopCoroutine(autoClickCoroutine);
              autoClickCoroutine = null;
          }
          featurePopup.SetActive(false);
          onStartClicked?.Invoke();
      });

      if (slotManager != null && (slotManager.IsAutoSpin || slotManager.WasAutoSpinOn))
      {
          if (autoClickCoroutine != null) StopCoroutine(autoClickCoroutine);
          autoClickCoroutine = StartCoroutine(AutoClickFeatureStart(2f));
      }
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
      if (freeSpinPopupTitleObject != null)
      {
          freeSpinPopupTitleObject.SetActive(false);
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
          featureWinAmountText.text = FormatStaticValue(winAmount);
      }
      featureStartButton.onClick.RemoveAllListeners();
      featureStartButton.onClick.AddListener(() => {
          if (autoClickCoroutine != null)
          {
              StopCoroutine(autoClickCoroutine);
              autoClickCoroutine = null;
          }
          featurePopup.SetActive(false);
          onCloseClicked?.Invoke();
      });

      if (slotManager != null && (slotManager.IsAutoSpin || slotManager.WasAutoSpinOn))
      {
          if (autoClickCoroutine != null) StopCoroutine(autoClickCoroutine);
          autoClickCoroutine = StartCoroutine(AutoClickFeatureStart(2f));
      }
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
      if (freeSpinPopupTitleObject != null)
      {
          freeSpinPopupTitleObject.SetActive(false);
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
          walterStashAmountText.text = FormatStaticValue(amount);
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
      if (balanceText) balanceText.text = FormatStaticValue(val);
  }


  private void UpdateTotalBetText(double val)
  {
      if (totalBetText) totalBetText.text = val.ToString();
      UpdateJackpotDisplay();
      UpdateInfoMultiplierTexts();
  }

  private void UpdateFreeSpinsText(int val)
  {
      if (fsNumText) fsNumText.text = val.ToString();

      if (spinCounterText != null)
      {
          // If a free spin retrigger is triggered, do NOT update the text right now
          if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.isFreeSpinTriggered)
          {
              return;
          }

          // Otherwise, update totalFreeSpins from the response if available
          if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.totalFreeSpins > 0)
          {
              totalFreeSpins = slotManager.ResultData.payload.totalFreeSpins;
          }

          int spinsUsed = totalFreeSpins - val;
          spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
      }
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
      if (totalBetPlusButton) totalBetPlusButton.interactable = (slotManager != null && slotManager.IsAutoSpin) ? false : toggle;
      if (totalBetMinusButton) totalBetMinusButton.interactable = (slotManager != null && slotManager.IsAutoSpin) ? false : toggle;
      
      bool isInfoExitInteractable = slotManager == null || (!slotManager.IsBonus && !slotManager.IsFeatureTransitioning);
      if (infoButton) infoButton.interactable = isInfoExitInteractable;
      if (gameExitButton) gameExitButton.interactable = isInfoExitInteractable;
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
      UpdateInfoMultiplierTexts();
  }

  internal IEnumerator TrailRendererAnimation(GameObject TrailRendererGO, int textIndex, int coinvalue, bool IsBonus = false)
  {
    yield break;
  }

  internal IEnumerator PlayCashCollectSequence(CashCollectResult result)
  {
      if (result == null || !result.triggered) yield break;

      // Check if there are any actual cash coins to display in the Coin Win Display Panel
      bool hasCashCoins = false;
      if (result.collectedCoins != null)
      {
          foreach (var coin in result.collectedCoins)
          {
              if (coin.symbolId == 13 || coin.symbolId == 15 || coin.symbolId == 16)
              {
                  hasCashCoins = true;
                  break;
              }
          }
      }

      // 1. Hide Jackpot Panel & Show Coin Win Display Panel (only if there are cash coins)
      if (hasCashCoins)
      {
          if (jackpotPanelCanvasGroup != null)
          {
              jackpotPanelCanvasGroup.DOKill();
              jackpotPanelCanvasGroup.DOFade(0f, 0.5f);
          }
          
          if (coinWinDisplayPanelCanvasGroup != null)
          {
              coinWinDisplayPanelCanvasGroup.DOKill();
              coinWinDisplayPanelCanvasGroup.gameObject.SetActive(true);
              coinWinDisplayPanelCanvasGroup.alpha = 0f;
              coinWinDisplayPanelCanvasGroup.DOFade(1f, 0.5f);
          }

          if (coinWinDisplayText != null)
          {
              coinWinDisplayText.text = "00.00";
          }

          yield return new WaitForSeconds(0.5f);
      }

      // Get CashCollect symbol positions
      List<List<int>> ccPositions = new List<List<int>>();
      if (result.positions != null)
      {
          foreach (var pos in result.positions)
          {
              if (pos.Type == Newtonsoft.Json.Linq.JTokenType.Array)
              {
                  int r = (int)pos[0];
                  int c = (int)pos[1];
                  ccPositions.Add(new List<int> { r, c });
              }
          }
      }

      double accumulatedVal = 0f;

      // 2. Play flying animations from cash coins to Cash Collect symbols sequentially (one by one pair)
      if (result.collectedCoins != null && result.collectedCoins.Count > 0 && ccPositions.Count > 0)
      {
          List<CollectedCoin> sortedCoins = new List<CollectedCoin>(result.collectedCoins);
          sortedCoins.Sort((a, b) =>
          {
              if (a.symbolId == 16 && b.symbolId != 16) return -1;
              if (b.symbolId == 16 && a.symbolId != 16) return 1;
              return 0;
          });

          foreach (var ccPos in ccPositions)
          {
              SlotSymbolView ccView = slotManager.GetSymbolView(ccPos[0], ccPos[1]);

              foreach (var coin in sortedCoins)
              {
                  // Only for Cash Coin [15], Multiplier Coin [13], and Prize Coin [16]
                  if (coin.symbolId == 15 || coin.symbolId == 16 || coin.symbolId == 13)
                  {
                      int r = coin.position[0];
                      int c = coin.position[1];
                      SlotSymbolView cashCoinView = slotManager.GetSymbolView(r, c);
                      if (cashCoinView == null)
                      {
                          continue;
                      }

                      // Pop animation on cash/prize coin (punch scale)
                      float animDuration = slotManager.animationManager != null ? slotManager.animationManager.winSymbolLoopDuration / 2f : 0.75f;
                      cashCoinView.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), animDuration, 1, 0.5f);

                      // Play one-loop animation on the active Cash Collect symbol
                      if (slotManager.animationManager != null)
                      {
                          slotManager.animationManager.PlaySpecialAnimationForCell(ccPos[0], ccPos[1]);
                      }

                      // Spawn trail renderer prefab in the middle of the cash coin
                      if (trailRendererPrefab != null)
                      {
                          GameObject trInstance = Instantiate(trailRendererPrefab, flyingTextParent != null ? flyingTextParent : transform);
                          
                          // Ensure local scale is (1,1,1) to avoid canvas scale distortions
                          trInstance.transform.localScale = Vector3.one;
                          
                          // Position at coin's world position
                          trInstance.transform.position = cashCoinView.transform.position;
                          
                          // Reset local Z position to 0 to ensure rendering on the UI camera view plane
                          Vector3 localPos = trInstance.transform.localPosition;
                          localPos.z = 0f;
                          trInstance.transform.localPosition = localPos;

                          // Set text of the spawned object to show the coin's collected value
                          double coinAmt = coin.coinValue * slotManager.TotalBet;
                          TMP_Text trText = trInstance.GetComponentInChildren<TMP_Text>();
                          if (trText != null)
                          {
                              trText.text = FormatStaticValue(coinAmt);
                          }

                          // Fly animation using DOTween to the coinWinDisplayPanel position
                          Vector3 targetPos = coinWinDisplayPanelCanvasGroup != null ? coinWinDisplayPanelCanvasGroup.transform.position : Vector3.zero;
                          
                          double finalTarget = accumulatedVal + coinAmt;
                          accumulatedVal = finalTarget;

                          bool trailCompleted = false;
                          trInstance.transform.DOMove(targetPos, trailMoveDuration).SetEase(Ease.OutQuad).OnComplete(() =>
                          {
                              Destroy(trInstance);
                              
                              double currentVal = 0;
                              if (coinWinDisplayText != null)
                              {
                                  double.TryParse(coinWinDisplayText.text, out currentVal);
                              }
                              
                              string coinFormat = GetAnimationFormat(finalTarget);
                              DOTween.To(() => currentVal, (val) =>
                              {
                                  if (coinWinDisplayText != null)
                                      coinWinDisplayText.text = finalTarget <= 0 ? "00.00" : val.ToString(coinFormat);
                              }, finalTarget, 0.3f).OnComplete(() =>
                              {
                                  if (coinWinDisplayText != null)
                                      coinWinDisplayText.text = FormatStaticValue(finalTarget);
                                  trailCompleted = true;
                              });
                          });

                          // Wait for this specific trail to complete (reach target + count-up finished)
                          yield return new WaitUntil(() => trailCompleted);
                      }
                      else
                      {
                          // Fallback if no prefab is assigned
                          double startVal = accumulatedVal;
                          accumulatedVal += coin.coinValue * slotManager.TotalBet;
                          double endVal = accumulatedVal;
                          if (coinWinDisplayText != null)
                              coinWinDisplayText.text = FormatStaticValue(endVal);
                      }

                      // Wait between trails
                      yield return new WaitForSeconds(delayBetweenTrails);
                  }
              }
          }
      }

      yield return new WaitForSeconds(1.0f);

      // Stop loop animations on the CashCollect symbols (safety cleanup)
      foreach (var ccPos in ccPositions)
      {
          if (slotManager.animationManager != null)
          {
              slotManager.animationManager.StopSymbolAnimationLoop(ccPos[0], ccPos[1]);
          }
      }

      // 3. Hide Coin Win Display Panel & Show Jackpot Panel (only if they were modified)
      if (hasCashCoins)
      {
          if (coinWinDisplayPanelCanvasGroup != null)
          {
              coinWinDisplayPanelCanvasGroup.DOKill();
              coinWinDisplayPanelCanvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
              {
                  coinWinDisplayPanelCanvasGroup.gameObject.SetActive(false);
              });
          }

          if (jackpotPanelCanvasGroup != null)
          {
              jackpotPanelCanvasGroup.DOKill();
              jackpotPanelCanvasGroup.DOFade(1f, 0.5f);
          }

          yield return new WaitForSeconds(0.5f);
      }
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
        JackpotText[i].text = FormatStaticValue(jackpotValue);
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
    totalFreeSpins = 0;

    if (normalBgCanvasGroup != null) normalBgCanvasGroup.DOFade(1f, 1f);
    if (freeSpinBgCanvasGroup != null) freeSpinBgCanvasGroup.DOFade(0f, 1f);

    if (spinCounterPanel != null) spinCounterPanel.SetActive(false);
    if (featureWinPanel != null) featureWinPanel.SetActive(false);
    if (spinCounterText != null) spinCounterText.gameObject.SetActive(true);
    if (featureWinText != null) featureWinText.gameObject.SetActive(true);

    // Restore standard UI elements when exiting free spins
    if (slotStartButton != null) slotStartButton.gameObject.SetActive(true);
    if (autoSpinButton != null) autoSpinButton.gameObject.SetActive(true);
    if (turboButton != null) turboButton.gameObject.SetActive(true);

    UpdateButtonsState();
  }

  internal void WinningsTextAnimation(Action onComplete = null)
  {
    double winAmt = slotManager.WinAmount;
    if (!double.TryParse(balanceText.text, out double currentBal))
    {
      Debug.Log("Error balance conversion: " + balanceText.text);
    }
    if (!double.TryParse(FormatStaticValue(socketManager.playerdata.balance), out double Balance))
    {
      Debug.Log("Error: " + socketManager.playerdata.balance);
    }
    if (!double.TryParse(totalWinText.text, out double currentWin))
    {
      Debug.Log("Error total win: " + totalWinText.text);
    }

    int completedTweens = 0;
    int targetTweensCount = slotManager.IsFreeSpin ? 3 : 2;

    Action checkComplete = () =>
    {
        completedTweens++;
        if (completedTweens >= targetTweensCount)
        {
            onComplete?.Invoke();
        }
    };

    string winFormat = GetAnimationFormat(winAmt);
    DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
    {
      if (totalWinText) totalWinText.text = winAmt <= 0 ? "00.00" : currentWin.ToString(winFormat);
    }).OnComplete(() => {
      if (totalWinText) totalWinText.text = FormatStaticValue(winAmt);
      checkComplete();
    });

    if (slotManager.IsFreeSpin)
    {
      double targetFeatureWin = 0;
      if (socketManager != null && socketManager.resultData != null && socketManager.resultData.features != null)
      {
          targetFeatureWin = socketManager.resultData.features.featureWin;
      }
      else
      {
          accumulatedFreeSpinWin += winAmt;
          targetFeatureWin = accumulatedFreeSpinWin;
      }
      accumulatedFreeSpinWin = targetFeatureWin;
      double tempWin = targetFeatureWin - winAmt;
      if (tempWin < 0) tempWin = 0;
      string featFormat = GetAnimationFormat(targetFeatureWin);
      DOTween.To(() => tempWin, (val) => tempWin = val, targetFeatureWin, 0.8f).OnUpdate(() =>
      {
         if (featureWinText) featureWinText.text = FormatSpriteText(targetFeatureWin <= 0 ? "00.00" : tempWin.ToString(featFormat));
      }).OnComplete(() => {
         if (featureWinText) featureWinText.text = FormatSpriteText(FormatStaticValue(targetFeatureWin));
         checkComplete();
      });
    }

    BalanceTween?.Kill();
    string balFormat = GetAnimationFormat(Balance);
    BalanceTween = DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
    {
      if (balanceText) balanceText.text = Balance <= 0 ? "00.00" : currentBal.ToString(balFormat);
    }).OnComplete(() => {
      if (balanceText) balanceText.text = FormatStaticValue(Balance);
      checkComplete();
    });
  }

  internal void DeductBalanceUI()
  {
    BalanceTween?.Kill();
    double bet = slotManager.TotalBet;
    double balance = slotManager.Balance;
    balance -= bet;
    if (balanceText) balanceText.text = FormatStaticValue(balance);
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

  private void CloseAutoplayPanel()
  {
    if (autoplaySelectionPanel) autoplaySelectionPanel.SetActive(false);
  }

  private void OpenInfoPanel()
  {
      if (infoPanel != null)
      {
          infoPanel.SetActive(true);
          UpdateInfoMultiplierTexts();
      }
  }

  private void CloseInfoPanel()
  {
      if (infoPanel != null)
      {
          infoPanel.SetActive(false);
      }
  }

  private void UpdateInfoMultiplierTexts()
  {
      if (slotManager == null || symbolTextMaps == null) return;

      double lineBet = slotManager.LineBet;

      foreach (var map in symbolTextMaps)
      {
          if (map.textComponent == null) continue;

          var symbolInfo = slotManager.GetSymbolInfo(map.symbolId);
          if (symbolInfo != null && symbolInfo.multiplier != null && symbolInfo.multiplier.Count >= 3)
          {
              double mult5 = symbolInfo.multiplier[0];
              double mult4 = symbolInfo.multiplier[1];
              double mult3 = symbolInfo.multiplier[2];

              double val5 = lineBet * mult5;
              double val4 = lineBet * mult4;
              double val3 = lineBet * mult3;

              map.textComponent.text = $"5x {FormatStaticValue(val5)}\n4x {FormatStaticValue(val4)}\n3x {FormatStaticValue(val3)}";
          }
      }
  }

  private void OnAutoplayStartPressed()
  {
    if (!autoplayOptionsDropdown) return;

    int selectedIndex = autoplayOptionsDropdown.value;
    DropdownItemDisabler disabler = autoplayOptionsDropdown.GetComponent<DropdownItemDisabler>();
    if (disabler != null && disabler.SelectedIndexOverride != -1)
    {
      selectedIndex = disabler.SelectedIndexOverride;
    }
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
        if (infoButton) infoButton.interactable = false;
        if (gameExitButton) gameExitButton.interactable = false;
        return;
    }

    // Set interactability for info and close/exit buttons: they should only be disabled if bonus or feature transition is active, or if popup is active (handled above)
    bool isInfoExitInteractable = !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
    if (infoButton) infoButton.interactable = isInfoExitInteractable;
    if (gameExitButton) gameExitButton.interactable = isInfoExitInteractable;

    // Bet buttons should be disabled while autoplay is on, or when spinning/features are active.
    bool isBetInteractable = !slotManager.IsSpinning && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin && !slotManager.IsBonus && !slotManager.IsFeatureTransitioning;
    if (totalBetPlusButton) totalBetPlusButton.interactable = isBetInteractable;
    if (totalBetMinusButton) totalBetMinusButton.interactable = isBetInteractable;

    if (slotManager.IsSpinning)
    {
      if (slotManager.IsAutoSpin)
      {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(true);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(!slotManager.AutoplayUntilFeature);
      }
      else
      {
        if (slotManager.IsAutoplayStoppedMidSpin)
        {
          if (slotStartButton)
          {
            slotStartButton.gameObject.SetActive(true);
            slotStartButton.interactable = false;
          }
          if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
          if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
          if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
        }
        else
        {
          if (slotStartButton) slotStartButton.gameObject.SetActive(false);
          if (stopSpinButton)
          {
            stopSpinButton.gameObject.SetActive(true);
            stopSpinButton.interactable = true;
          }
          if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
          if (autoplayCounterObject) autoplayCounterObject.SetActive(slotManager.IsBonus);
        }
      }
    }
    else
    {
      if (slotManager.IsAutoSpin)
      {
        if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(true);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(!slotManager.AutoplayUntilFeature);
      }
      else if (slotManager.IsFreeSpin)
      {
        if (slotStartButton) {
          slotStartButton.gameObject.SetActive(true);
          slotStartButton.interactable = false;
        }
        if (stopSpinButton)
        {
          stopSpinButton.gameObject.SetActive(true);
          stopSpinButton.interactable = false;
          if (slotStartButton) slotStartButton.gameObject.SetActive(false);
        }
        if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
        if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
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

  public void PerformStop()
  {
      if (slotManager == null) return;

      if (slotManager.IsBonus)
      {
          if (bonusManager != null && bonusManager.IsSpinning)
          {
              bonusManager.StopSpinToggle = true;
              // Immediately change stop button to disabled feature spin button during feature
              UpdateFeatureButtonsState(false, slotManager.LinkRespinsRemaining);
              if (featureSpinButton != null) featureSpinButton.interactable = false;
          }
      }
      else
      {
          if (slotManager.IsSpinning && !slotManager.StopSpinToggle)
          {
              slotManager.StopSpinToggle = true;
              // Immediately change stop button to disabled spin button for normal game
              ShowSpinButtonCooldown(true);
          }
      }
  }

  public static int GetDecimalPlaces(double value)
  {
      double rounded = Math.Round(value, 3);
      string s = rounded.ToString(System.Globalization.CultureInfo.InvariantCulture);
      int dotIndex = s.IndexOf('.');
      if (dotIndex < 0) return 0;
      return s.Length - dotIndex - 1;
  }

  public static string FormatStaticValue(double value)
  {
      if (value <= 0)
      {
          return "00.00";
      }
      return value.ToString("0.###");
  }

  public static string GetAnimationFormat(double resultAmount)
  {
      int decimals = GetDecimalPlaces(resultAmount);
      if (decimals <= 1)
      {
          return "0.0";
      }
      else if (decimals == 2)
      {
          return "0.00";
      }
      else
      {
          return "0.000";
      }
  }

  private string FormatSpriteText(string input)
  {
      string result = "";
      foreach (char c in input)
      {
          if (char.IsDigit(c))
          {
              result += $"<sprite={c - '0'}>";
          }
          else if (c == '.')
          {
              result += "<sprite=10>";
          }
          else if (c == ',')
          {
              result += "<sprite=11>";
          }
          else
          {
              result += c;
          }
      }
      return result;
  }

  public IEnumerator PlayFreeSpinTriggerSequence(FreeSpinResult fsResult, bool isRetrigger = false, bool fromBonusSlot = false)
  {
      if (midSumAnimObj != null) midSumAnimObj.SetActive(false);
      if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);

      int totalSpins = 0;
      if (fsResult != null && fsResult.triggerCoins != null && fsResult.triggerCoins.Count > 0)
      {
          foreach (var coin in fsResult.triggerCoins)
          {
              totalSpins += (int)coin.coinValue;
          }
      }
      else
      {
          totalSpins = fsResult != null ? fsResult.freeSpinCount : (slotManager != null ? slotManager.FreeSpinsCount : 0);
      }

      int previousRemaining = 0;
      int previousTotal = 0;

      if (fsResult != null)
      {
          previousRemaining = fsResult.freeSpinsRemaining - totalSpins;
          if (fsResult.totalFreeSpins > 0)
          {
              previousTotal = fsResult.totalFreeSpins - totalSpins;
          }
          else
          {
              previousTotal = totalFreeSpins; // fallback
          }
      }
      else
      {
          previousRemaining = slotManager != null ? slotManager.FreeSpinsCount : 0;
          previousTotal = totalFreeSpins;
      }

      int previousSpinsUsed = previousTotal - previousRemaining;
      if (previousSpinsUsed < 0) previousSpinsUsed = 0;

      if (isRetrigger)
      {
          if (spinCounterPanel != null) spinCounterPanel.SetActive(true);
          if (featureWinPanel != null) featureWinPanel.SetActive(true);
          if (spinCounterText != null)
          {
              spinCounterText.text = $"{previousSpinsUsed}/{previousTotal}";
              spinCounterText.gameObject.SetActive(true);
          }
          if (featureWinText != null)
          {
              featureWinText.text = FormatSpriteText(FormatStaticValue(accumulatedFreeSpinWin));
              featureWinText.gameObject.SetActive(true);
          }
      }
      else
      {
          accumulatedFreeSpinWin = 0f;

          if (normalBgCanvasGroup != null) normalBgCanvasGroup.DOFade(0f, 1f);
          if (freeSpinBgCanvasGroup != null) freeSpinBgCanvasGroup.DOFade(1f, 1f);

          if (spinCounterPanel != null) spinCounterPanel.SetActive(true);
          if (featureWinPanel != null) featureWinPanel.SetActive(true);

          if (spinCounterText != null) spinCounterText.gameObject.SetActive(false);
          if (featureWinText != null) featureWinText.gameObject.SetActive(false);
      }

      yield return new WaitForSeconds(0.5f);

      List<Vector3> coinWorldPositions = new List<Vector3>();
      List<string> coinTexts = new List<string>();
      List<TMP_Text> sourceTexts = new List<TMP_Text>();

      if (fsResult != null && fsResult.triggerCoins != null && fsResult.triggerCoins.Count > 0)
      {
          foreach (var coin in fsResult.triggerCoins)
          {
              int row = coin.position[0];
              int col = coin.position[1];

              if (fromBonusSlot)
              {
                  if (bonusManager != null && bonusManager.Slot != null && col < bonusManager.Slot.Count)
                  {
                      var rowTransforms = bonusManager.Slot[col].slotTransforms;
                      if (row < rowTransforms.Count)
                      {
                          var view = rowTransforms[row].GetChild(2).GetComponent<SlotSymbolView>();
                          if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                          {
                              coinWorldPositions.Add(view.losPolosValueText.transform.position);
                              coinTexts.Add(view.losPolosValueText.text);
                              sourceTexts.Add(view.losPolosValueText);
                          }
                      }
                  }
              }
              else
              {
                  if (slotManager != null && slotManager.ResultMatrix != null && row < slotManager.ResultMatrix.Count)
                  {
                      var rowImages = slotManager.ResultMatrix[row].slotImages;
                      if (col < rowImages.Count)
                      {
                          var view = rowImages[col].GetComponent<SlotSymbolView>();
                          if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                          {
                              coinWorldPositions.Add(view.losPolosValueText.transform.position);
                              coinTexts.Add(view.losPolosValueText.text);
                              sourceTexts.Add(view.losPolosValueText);
                          }
                      }
                  }
              }
          }
      }
      else
      {
          if (fromBonusSlot)
          {
              if (bonusManager != null && bonusManager.Slot != null)
              {
                  for (int col = 0; col < bonusManager.Slot.Count; col++)
                  {
                      var rowTransforms = bonusManager.Slot[col].slotTransforms;
                      for (int row = 0; row < rowTransforms.Count; row++)
                      {
                          var triggerResult = (slotManager != null && slotManager.OriginalFeatureTriggerResult != null)
                              ? slotManager.OriginalFeatureTriggerResult
                              : (socketManager != null ? socketManager.resultData : null);

                          if (triggerResult != null && triggerResult.matrix != null)
                          {
                              if (row < triggerResult.matrix.Count && col < triggerResult.matrix[row].Count)
                              {
                                  if (triggerResult.matrix[row][col] == "17")
                                  {
                                      var view = rowTransforms[row].GetChild(2).GetComponent<SlotSymbolView>();
                                      if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                                      {
                                          coinWorldPositions.Add(view.losPolosValueText.transform.position);
                                          coinTexts.Add(view.losPolosValueText.text);
                                          sourceTexts.Add(view.losPolosValueText);
                                      }
                                  }
                              }
                          }
                      }
                  }
              }
          }
          else
          {
              if (slotManager != null && slotManager.ResultMatrix != null)
              {
                  for (int r = 0; r < slotManager.ResultMatrix.Count; r++)
                  {
                      var rowImages = slotManager.ResultMatrix[r].slotImages;
                      for (int c = 0; c < rowImages.Count; c++)
                      {
                           var triggerResult = (slotManager != null && slotManager.OriginalFeatureTriggerResult != null)
                               ? slotManager.OriginalFeatureTriggerResult
                               : (socketManager != null ? socketManager.resultData : null);

                           if (triggerResult != null && triggerResult.matrix != null)
                           {
                               if (r < triggerResult.matrix.Count && c < triggerResult.matrix[r].Count)
                               {
                                   if (triggerResult.matrix[r][c] == "17")
                                   {
                                       var view = rowImages[c].GetComponent<SlotSymbolView>();
                                       if (view != null && view.losPolosValueText != null && view.losPolosValueText.gameObject.activeSelf)
                                       {
                                           coinWorldPositions.Add(view.losPolosValueText.transform.position);
                                           coinTexts.Add(view.losPolosValueText.text);
                                           sourceTexts.Add(view.losPolosValueText);
                                       }
                                   }
                               }
                           }
                      }
                  }
              }
          }
      }

      if (fsResult != null && fsResult.totalFreeSpins > 0)
      {
          totalFreeSpins = fsResult.totalFreeSpins;
      }
      else if (slotManager.ResultData != null && slotManager.ResultData.payload != null && slotManager.ResultData.payload.totalFreeSpins > 0)
      {
          totalFreeSpins = slotManager.ResultData.payload.totalFreeSpins;
      }
      else
      {
          if (isRetrigger)
          {
              totalFreeSpins += totalSpins;
          }
          else
          {
              totalFreeSpins = totalSpins;
          }
      }

      Transform spawnParent = flyingTextParent != null ? flyingTextParent : this.transform;
      List<TMP_Text> tempTexts = new List<TMP_Text>();
      List<Vector3> initialLocalScales = new List<Vector3>();

      for (int i = 0; i < coinWorldPositions.Count; i++)
      {
          TMP_Text tempText = Instantiate(losPolosTextPrefab, spawnParent);
          tempText.transform.position = coinWorldPositions[i];
          tempText.text = coinTexts[i];

          if (i < sourceTexts.Count && sourceTexts[i] != null)
          {
              Vector3 sourceLossyScale = sourceTexts[i].transform.lossyScale;
              Vector3 parentLossyScale = spawnParent.lossyScale;
              Vector3 matchedLocalScale = new Vector3(
                  parentLossyScale.x != 0 ? sourceLossyScale.x / parentLossyScale.x : 1f,
                  parentLossyScale.y != 0 ? sourceLossyScale.y / parentLossyScale.y : 1f,
                  parentLossyScale.z != 0 ? sourceLossyScale.z / parentLossyScale.z : 1f
              );
              tempText.transform.localScale = matchedLocalScale;
              initialLocalScales.Add(matchedLocalScale);

              RectTransform rectTrans = tempText.GetComponent<RectTransform>();
              RectTransform sourceRectTrans = sourceTexts[i].GetComponent<RectTransform>();
              if (rectTrans != null && sourceRectTrans != null)
              {
                  rectTrans.sizeDelta = sourceRectTrans.sizeDelta;
              }

              sourceTexts[i].gameObject.SetActive(false);
          }
          else
          {
              initialLocalScales.Add(Vector3.one);
          }

          tempText.gameObject.SetActive(true);
          tempTexts.Add(tempText);
      }

      yield return new WaitForSeconds(0.2f);

      Vector3 centerWorldPos = spawnParent.position;
      float moveDuration = 0.8f;
      foreach (var txt in tempTexts)
      {
          txt.transform.DOMove(centerWorldPos, moveDuration).SetEase(Ease.OutQuad);
      }

      yield return new WaitForSeconds(moveDuration);

      if (midSumAnimObj != null) midSumAnimObj.SetActive(true);

      TMP_Text sumTextPrefab = null;
      Vector3 sumInitialScale = Vector3.one;
      if (tempTexts.Count > 0)
      {
          sumTextPrefab = tempTexts[0];
          sumInitialScale = initialLocalScales[0];
          string sumSpriteText = "<sprite=10>";
          string totalSpinsStr = totalSpins.ToString();
          foreach (char c in totalSpinsStr)
          {
              if (char.IsDigit(c))
              {
                  sumSpriteText += $"<sprite={c - '0'}>";
              }
          }
          sumTextPrefab.text = sumSpriteText;

          for (int i = 1; i < tempTexts.Count; i++)
          {
              if (tempTexts[i] != null) Destroy(tempTexts[i].gameObject);
          }
          tempTexts.Clear();
      }

      if (sumTextPrefab != null)
      {
          sumTextPrefab.transform.DOScale(sumInitialScale * 1.8f, 0.4f).SetEase(Ease.OutBack);
          yield return new WaitForSeconds(0.4f);
          sumTextPrefab.transform.DOScale(sumInitialScale * 1.4f, 0.3f).SetEase(Ease.InQuad);
          yield return new WaitForSeconds(0.3f);

          yield return new WaitForSeconds(0.5f);

          Vector3 targetPos = spinCounterPanel != null ? spinCounterPanel.transform.position : centerWorldPos;
          if (spinCountSnapParent != null)
          {
              targetPos = spinCountSnapParent.position;
          }

          if (midSumAnimObj != null) midSumAnimObj.SetActive(false);

          sumTextPrefab.transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutQuad);
          yield return new WaitForSeconds(moveDuration);

          if (dissolveAnimObj != null) dissolveAnimObj.SetActive(true);

          if (spinCounterText != null)
          {
              int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
              int spinsUsed = totalFreeSpins - remaining;
              spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
              spinCounterText.gameObject.SetActive(true);
          }
          Destroy(sumTextPrefab.gameObject);

          yield return new WaitForSeconds(1.0f);
          if (dissolveAnimObj != null) dissolveAnimObj.SetActive(false);
      }
      else
      {
          if (spinCounterText != null)
          {
              int remaining = fsResult != null ? fsResult.freeSpinsRemaining : slotManager.FreeSpinsCount;
              int spinsUsed = totalFreeSpins - remaining;
              spinCounterText.text = $"{spinsUsed}/{totalFreeSpins}";
              spinCounterText.gameObject.SetActive(true);
          }
      }

      if (fromBonusSlot)
      {
          yield return bonusManager.TransitionFromBonusToNormalSlot();
      }

      if (!isRetrigger)
      {
          yield return new WaitForSeconds(0.5f);

          bool popupClicked = false;
          OpenFeaturePopup(() => {
              popupClicked = true;
          });

          yield return new WaitUntil(() => popupClicked);

           if (featureWinText != null)
           {
               featureWinText.text = FormatSpriteText("00.00");
               featureWinText.gameObject.SetActive(true);
           }
      }
  }
}

public class DropdownItemDisabler : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public List<int> indexesToDisable = new List<int>();
    public int SelectedIndexOverride = -1;

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        SelectedIndexOverride = -1;
        StartCoroutine(DisableItemsCoroutine());
    }

    private IEnumerator DisableItemsCoroutine()
    {
        yield return null;

        var dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null) yield break;

        Transform dropdownList = dropdown.transform.Find("Dropdown List");
        if (dropdownList == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                dropdownList = canvas.transform.Find("Dropdown List");
            }
        }

        if (dropdownList == null)
        {
            var go = GameObject.Find("Dropdown List");
            if (go != null)
            {
                dropdownList = go.transform;
            }
        }

        if (dropdownList != null)
        {
            Transform content = dropdownList.Find("Viewport/Content");
            if (content != null)
            {
                List<Toggle> optionToggles = new List<Toggle>();
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child.gameObject.activeSelf)
                    {
                        Toggle toggle = child.GetComponent<Toggle>();
                        if (toggle != null)
                        {
                            optionToggles.Add(toggle);
                        }
                    }
                }

                for (int i = 0; i < optionToggles.Count; i++)
                {
                    int index = i;
                    optionToggles[i].onValueChanged.AddListener((isOn) => {
                        if (isOn) {
                            SelectedIndexOverride = index;
                            StartCoroutine(SetDropdownValueCoroutine(dropdown, index));
                        }
                    });

                    if (indexesToDisable.Contains(i))
                    {
                        optionToggles[i].interactable = false;
                        var graphics = optionToggles[i].GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                        foreach (var graphic in graphics)
                        {
                            graphic.enabled = false;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator SetDropdownValueCoroutine(TMP_Dropdown dropdown, int index)
    {
        yield return null;
        dropdown.value = index;
    }
}
