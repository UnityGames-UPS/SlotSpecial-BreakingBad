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

  [Header("Spin & Autoplay Rework UI")]
  [SerializeField] private GameObject autoplaySelectionPanel;
  [SerializeField] private TMP_Dropdown autoplayOptionsDropdown;
  [SerializeField] private Button autoplayStartButton;
  [SerializeField] private GameObject autoplayCounterObject;
  [SerializeField] private TMP_Text autoplayCounterText;

  [Header("Jackpot UI")]
  [SerializeField] private List<TMP_Text> JackpotText;

  [Header("Win Info")]
  [SerializeField] private Transform BaseWinningsPosition;
  [SerializeField] private TMP_Text BaseWinnings_Text;
  [SerializeField] private TMP_Text CoinWinning_Text;
  [SerializeField] private Sprite TurboToggleSprite;



  [Header("UI Text Objects")]
  [SerializeField] private TMP_Text[] TopPayoutTextUI;

  [Header("RaycastBlocker")]
  [SerializeField] internal GameObject RaycastBlocker;

  [Header("Image Animations (Magnet)")]
  [SerializeField] private ImageAnimation LeftMagnetImageAnimation;
  [SerializeField] private ImageAnimation RightMagnetImageAnimation;
  [SerializeField] private ImageAnimation FreeGamesImageAnimation;

  [Header("Normal Slot Canvas Groups")]
  [SerializeField] private CanvasGroup FreeSpinsUI_Panel;
  [SerializeField] private CanvasGroup WinningsUI_Panel;
  [SerializeField] private CanvasGroup TopPayoutUI_CG;
  [SerializeField] private CanvasGroup CanvasGroup;
  [SerializeField] private CanvasGroup LinesUI;
  [SerializeField] private CanvasGroup TotalBetUI;
  [SerializeField] private CanvasGroup LineBetUI;
  [SerializeField] private RectTransform FreeSpinCountUIPositon;
  [SerializeField] private Transform AnimationParent;

  internal Coroutine BonusCoroutine;
  internal bool animationFinish = false;
  internal int multiplierCount = 0;

  // Registered Slot Buttons & Texts (from SlotManager)
  [Header("HUD Objects")]
  [SerializeField] private Button slotStartButton;
  [SerializeField] private Button autoSpinButton;
  [SerializeField] private Button autoSpinStopButton;
  [SerializeField] private Button totalBetPlusButton;
  [SerializeField] private Button totalBetMinusButton;
  [SerializeField] private Button lineBetPlusButton;
  [SerializeField] private Button lineBetMinusButton;
  [SerializeField] private Button turboButton;
  [SerializeField] private Button stopSpinButton;

  [SerializeField] private TMP_Text balanceText;
  [SerializeField] private TMP_Text totalBetText;
  [SerializeField] private TMP_Text lineBetText;
  [SerializeField] private TMP_Text totalWinText;
  [SerializeField] private TMP_Text fsNumText;
  


  private Tween BalanceTween;

  private void Start()
  {
    if (LeftMagnetImageAnimation != null) LeftMagnetImageAnimation.gameObject.SetActive(false);
    if (RightMagnetImageAnimation != null) RightMagnetImageAnimation.gameObject.SetActive(false);
    if (FreeGamesImageAnimation != null) FreeGamesImageAnimation.gameObject.SetActive(false);

    // Bind to Model Events
    slotManager.OnBalanceChanged += UpdateBalanceText;
    slotManager.OnLineBetChanged += UpdateLineBetText;
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
  }

  public void RegisterSlotElements(
      SlotManager manager,
      Button startBtn, Button autoBtn, Button autoStopBtn,
      Button betPlusBtn, Button betMinusBtn, Button lBetPlusBtn, Button lBetMinusBtn,
      Button trbBtn, Button stopBtn,
      TMP_Text balTxt, TMP_Text totBetTxt, TMP_Text lBetTxt, TMP_Text totWinTxt, TMP_Text fsTxt)
  {
      slotManager = manager;
      slotStartButton = startBtn;
      autoSpinButton = autoBtn;
      autoSpinStopButton = autoStopBtn;
      totalBetPlusButton = betPlusBtn;
      totalBetMinusButton = betMinusBtn;
      lineBetPlusButton = lBetPlusBtn;
      lineBetMinusButton = lBetMinusBtn;
      turboButton = trbBtn;
      stopSpinButton = stopBtn;

      balanceText = balTxt;
      totalBetText = totBetTxt;
      lineBetText = lBetTxt;
      totalWinText = totWinTxt;
      fsNumText = fsTxt;

      InitializeHUD();
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
              if (slotManager && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin && !slotManager.IsBonus) {
                  // Allow starting a new spin even if animations are playing
                  // ForceCleanupPreviousSpin inside StartSlots handles cleanup
                  slotManager.StartSlots();
                  CanCloseMenu();
              }
          });
          holdHandler.onLongPress.RemoveAllListeners();
          holdHandler.onLongPress.AddListener(() => {
              if (slotManager && !slotManager.IsSpinning && !slotManager.IsAutoSpin && !slotManager.IsBonus) {
                  OpenAutoplayPanel();
                  CanCloseMenu();
              }
          });
      }
      if (autoSpinButton) {
          autoSpinButton.onClick.RemoveAllListeners();
          autoSpinButton.onClick.AddListener(() => {
              OpenAutoplayPanel();
              CanCloseMenu();
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
      if (lineBetPlusButton) {
          lineBetPlusButton.onClick.RemoveAllListeners();
          lineBetPlusButton.gameObject.SetActive(false);
      }
      if (lineBetMinusButton) {
          lineBetMinusButton.onClick.RemoveAllListeners();
          lineBetMinusButton.gameObject.SetActive(false);
      }
      if (turboButton) {
          turboButton.onClick.RemoveAllListeners();
          turboButton.onClick.AddListener(() => { TurboToggle(); CanCloseMenu(); });
      }
      if (stopSpinButton) {
          stopSpinButton.onClick.RemoveAllListeners();
          stopSpinButton.onClick.AddListener(() => { 
              if (slotManager) slotManager.StopSpinToggle = true; 
          });
      }

      UpdateButtonsState();
  }

  public void RegisterBonusElements(TMP_Text counterTxt)
  {
  }

  public Transform GetAnimationParent() => AnimationParent;
  public RectTransform GetFreeSpinCountUIPositon() => FreeSpinCountUIPositon;
  public ImageAnimation GetLeftMagnetImageAnimation() => LeftMagnetImageAnimation;
  public ImageAnimation GetRightMagnetImageAnimation() => RightMagnetImageAnimation;
  public ImageAnimation GetFreeGamesImageAnimation() => FreeGamesImageAnimation;

  public void SetNormalSpinButtonActive(bool active)
  {
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
  }

  public void SetBonusSpinCounter(int count)
  {
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

  public void SetCoinWinningText(string text)
  {
      if (CoinWinning_Text) CoinWinning_Text.text = text;
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
          if (slotStartButton)
          {
              slotStartButton.interactable = true;
          }
      }
  }

  public void FadeWinningsPanel(float endVal, float duration, Action onComplete = null)
  {
      WinningsUI_Panel.DOFade(endVal, duration).OnComplete(() => onComplete?.Invoke());
  }

  public void FadeLinesUI(float endVal, float duration)
  {
      LinesUI.DOFade(endVal, duration).OnComplete(() => {
          LinesUI.interactable = endVal > 0;
          LinesUI.blocksRaycasts = endVal > 0;
      });
  }

  public void FadeTotalBetUI(float endVal, float duration)
  {
      TotalBetUI.DOFade(endVal, duration).OnComplete(() => {
          TotalBetUI.interactable = endVal > 0;
          TotalBetUI.blocksRaycasts = endVal > 0;
      });
  }

  public void FadeLineBetUI(float endVal, float duration)
  {
      LineBetUI.DOFade(endVal, duration).OnComplete(() => {
          LineBetUI.interactable = endVal > 0;
          LineBetUI.blocksRaycasts = endVal > 0;
      });
  }

  private void UpdateBalanceText(double val)
  {
      if (balanceText) balanceText.text = val.ToString("f3");
  }

  private void UpdateLineBetText(double val)
  {
      if (lineBetText) lineBetText.text = val.ToString();
  }

  private void UpdateTotalBetText(double val)
  {
      if (totalBetText) totalBetText.text = val.ToString();
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
      if (lineBetPlusButton) lineBetPlusButton.interactable = false;
      if (lineBetMinusButton) lineBetMinusButton.interactable = false;
      if (totalBetPlusButton) totalBetPlusButton.interactable = false;
      if (totalBetMinusButton) totalBetMinusButton.interactable = false;
  }

  public void SetButtonsInteractable(bool toggle)
  {
      if (slotStartButton && !slotManager.IsAutoSpin) slotStartButton.interactable = toggle;
      if (autoSpinButton && !slotManager.IsAutoSpin && !slotManager.IsFreeSpin) autoSpinButton.gameObject.SetActive(toggle);
      if (autoSpinButton && !slotManager.IsAutoSpin) autoSpinButton.interactable = toggle;
      if (lineBetPlusButton && !slotManager.IsAutoSpin) lineBetPlusButton.interactable = toggle;
      if (lineBetMinusButton && !slotManager.IsAutoSpin) lineBetMinusButton.interactable = toggle;
      if (totalBetPlusButton && !slotManager.IsAutoSpin) totalBetPlusButton.interactable = toggle;
      if (totalBetMinusButton && !slotManager.IsAutoSpin) totalBetMinusButton.interactable = toggle;
  }

  public void SetAutoSpinActive(bool active)
  {
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(active);
      if (autoSpinButton) autoSpinButton.gameObject.SetActive(!active);
  }

  private void TurboToggle()
  {
    if (slotManager.IsTurboOn)
    {
      slotManager.IsTurboOn = false;
      turboButton.GetComponent<ImageAnimation>().StopAnimation();
      turboButton.image.sprite = TurboToggleSprite;
    }
    else
    {
      slotManager.IsTurboOn = true;
      turboButton.GetComponent<ImageAnimation>().StartAnimation();
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
    TrailRenderer trail = TrailRendererGO.GetComponent<TrailRenderer>();
    TrailRendererGO.transform.parent.GetChild(textIndex).GetComponent<TMP_Text>().text = coinvalue.ToString() + "x";
    TrailRendererGO.gameObject.SetActive(true);
    Vector3 tempPosi = trail.transform.position;

    Vector3 DOMovePosition = BaseWinningsPosition.position;
    TMP_Text text = BaseWinnings_Text;

    yield return trail.transform.DOMove(DOMovePosition, .5f).OnComplete(() =>
    {
      trail.gameObject.SetActive(false);
      trail.transform.position = tempPosi;

      int multiplier = coinvalue;
      multiplierCount += multiplier;

      int start = int.Parse(text.text.Replace("x", ""));
      DOTween.To(() => start, (val) => start = val, multiplierCount, 0.3f).OnUpdate(() =>
      {
        text.text = start.ToString() + "x";
      }).WaitForCompletion();
    });
    yield return new WaitForSeconds(1f);
  }

  internal IEnumerator MidGameImageAnimation(ImageAnimation imageAnimation, double num = 0)
  {
    if (imageAnimation == null)
    {
      animationFinish = true;
      yield break;
    }
    animationFinish = false;
    if (imageAnimation.transform.parent != null) imageAnimation.transform.parent.gameObject.SetActive(true);
    imageAnimation.gameObject.SetActive(true);
    imageAnimation.StartAnimation();

    yield return new WaitUntil(() => imageAnimation.rendererDelegate != null && imageAnimation.rendererDelegate.sprite == imageAnimation.textureArray[^1]);

    imageAnimation.StopAnimation();
    if (imageAnimation.transform.parent != null) imageAnimation.transform.parent.gameObject.SetActive(false);
    animationFinish = true;
  }





  private void CallOnExitFunction()
  {
    slotManager.CallCloseSocket();
  }



  internal void SetJackpotText(Jackpot jackpot)
  {
    if (jackpot == null || jackpot.payout == null || JackpotText == null) return;

    for (int i = 0; i < jackpot.payout.Count; i++)
    {
      if (i >= JackpotText.Count) break;
      if (JackpotText[i] != null)
      {
        JackpotText[i].text = jackpot.payout[i].ToString();
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
    FreeSpinsUI_Panel.DOFade(1, 0.3f);
    
    if (LinesUI.alpha != 0) FadeLinesUI(0f, 0.3f);
    if (TotalBetUI.alpha != 0) FadeTotalBetUI(0f, 0.3f);
    if (LineBetUI.alpha != 0) FadeLineBetUI(0f, 0.3f);
  }

  internal void CloseFreeSpinsUI()
  {
    slotManager.IsFreeSpin = false;
    FreeSpinsUI_Panel.DOFade(0, 0.3f);
    if (fsNumText) fsNumText.text = "0";

    FadeLinesUI(1f, 0.3f);
    FadeTotalBetUI(1f, 0.3f);
    FadeLineBetUI(1f, 0.3f);
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
    if (trigger)
    {
      TopPayoutUI_CG.DOFade(0, 0.5f);
      WinningsUI_Panel.DOFade(1, 0.5f);
    }
    else
    {
      TopPayoutUI_CG.DOFade(1, 0.5f);
      WinningsUI_Panel.DOFade(0, 0.5f);
    }
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

    if (slotManager.IsAutoSpin)
    {
      if (slotStartButton) slotStartButton.gameObject.SetActive(false);
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(true);
    }
    else if (slotManager.IsFreeSpin)
    {
      if (slotStartButton) {
        slotStartButton.gameObject.SetActive(true);
        slotStartButton.interactable = false;
      }
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    }
    else if (slotManager.IsSpinning)
    {
      if (slotStartButton) slotStartButton.gameObject.SetActive(false);
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(true);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    }
    else
    {
      if (slotStartButton) {
        slotStartButton.gameObject.SetActive(true);
        slotStartButton.interactable = true;
      }
      if (stopSpinButton) stopSpinButton.gameObject.SetActive(false);
      if (autoSpinStopButton) autoSpinStopButton.gameObject.SetActive(false);
      if (autoplayCounterObject) autoplayCounterObject.SetActive(false);
    }
  }
}
