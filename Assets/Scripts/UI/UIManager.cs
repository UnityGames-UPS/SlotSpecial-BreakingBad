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

  [Header("Popus UI")]
  [SerializeField] private GameObject MainPopup_Object;
  
  [Header("Jackpot UI")]
  [SerializeField] private List<TMP_Text> JackpotText;

  [Header("Win Popup")]
  [SerializeField] private GameObject WinPopup_Object;
  [SerializeField] private ImageAnimation Winnings_ImageAnimation;
  [SerializeField] private RectTransform WinTextBgImage;
  [SerializeField] private TMP_Text Win_Text;
  [SerializeField] private Sprite[] BigWin_Sprites, MegaWin_Sprites, BonusWinnings_Sprites;
  [SerializeField] private Transform BonusWinningsPosition;
  [SerializeField] private TMP_Text BonusWinnings_Text;
  [SerializeField] private Transform BaseWinningsPosition;
  [SerializeField] private TMP_Text BaseWinnings_Text;
  [SerializeField] private Button SkipWinAnimation;
  [SerializeField] private TMP_Text CoinWinning_Text;
  [SerializeField] private Sprite TurboToggleSprite;

  [Header("Reconection Popup")]
  [SerializeField] private GameObject ReconectingPopup_Object;

  [Header("Disconnection Popup")]
  [SerializeField] private Button CloseDisconnect_Button;
  [SerializeField] private GameObject DisconnectPopup_Object;

  [Header("AnotherDevice Popup")]
  [SerializeField] private GameObject ADPopup_Object;

  [Header("LowBalance Popup")]
  [SerializeField] private GameObject LBPopup_Object;
  [SerializeField] private Button LBExit_Button;

  [Header("Audio Objects")]
  [SerializeField] private GameObject Settings_Object;
  [SerializeField] private Button SettingsQuit_Button;
  [SerializeField] private Slider Sound_Slider;
  [SerializeField] private Slider Music_Slider;

  [Header("Paytable Objects")]
  [SerializeField] private GameObject PaytableMenuObject;
  [SerializeField] private Button Paytable_Button;
  [SerializeField] private Button PaytableClose_Button;
  [SerializeField] private Button PaytableLeft_Button;
  [SerializeField] private Button PaytableRight_Button;
  [SerializeField] private List<GameObject> GameRulesPages = new();
  private int PageIndex;

  [Header("Paytable Slot Text")]
  [SerializeField] private List<TMP_Text> SymbolsText = new();
  [SerializeField] private List<TMP_Text> SpecialSymbolsText = new();
  [SerializeField] private Button RaycastLayerButton;

  [Header("Game Quit Objects")]
  [SerializeField] private GameObject QuitMenuObject;
  [SerializeField] private Button Quit_Button;
  [SerializeField] private Button QuitYes_Button;
  [SerializeField] private Button QuitNo_Button;

  [Header("Menu Objects")]
  [SerializeField] private Button Menu_Button;
  [SerializeField] private Button Info_Button;
  [SerializeField] private Button Settings_Button;
  [SerializeField] private RectTransform Info_BttnTransform;
  [SerializeField] private RectTransform Settings_BttnTransform;

  [Header("MidGame UI Text Objects")]
  [SerializeField] private TMP_Text FreeSpinsText;
  [SerializeField] private TMP_Text BonusGameWinningsText;

  [Header("UI Text Objects")]
  [SerializeField] private TMP_Text[] TopPayoutTextUI;

  [Header("RaycastBlocker")]
  [SerializeField] internal GameObject RaycastBlocker;

  [Header("Image Animations (Magnet)")]
  [SerializeField] private ImageAnimation LeftMagnetImageAnimation;
  [SerializeField] private ImageAnimation RightMagnetImageAnimation;
  [SerializeField] private ImageAnimation BonusImageAnimation;
  [SerializeField] private ImageAnimation FreeGamesImageAnimation;

  [Header("Normal Slot Canvas Groups")]
  [SerializeField] private CanvasGroup FreeSpinsUI_Panel;
  [SerializeField] private CanvasGroup WinningsUI_Panel;
  [SerializeField] private CanvasGroup TopPayoutUI_CG;
  [SerializeField] private CanvasGroup LinesUI;
  [SerializeField] private CanvasGroup TotalBetUI;
  [SerializeField] private CanvasGroup LineBetUI;
  [SerializeField] private RectTransform FreeSpinCountUIPositon;
  [SerializeField] private Transform AnimationParent;

  private bool isExit = false;
  private bool isMenu = false;
  private ImageAnimation ImageAnimation;
  private Coroutine PopupAnimCoroutine;
  internal Coroutine BonusCoroutine;
  private Tween TextTween;
  private Tween TextTween2;
  private Tween scaleTween;
  internal Coroutine BonusWinningsCoroutine;
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
  
  // Registered Bonus Buttons & Texts (from BonusManager)
  [Header("Bonus HUD Objects")]
  [SerializeField] private Button bonusStartButton;
  [SerializeField] private TMP_Text bonusSpinCounterText;
  [SerializeField] private TMP_Text bonusWinningsText;

  private Tween BalanceTween;

  private void Start()
  {
    if (LeftMagnetImageAnimation != null) LeftMagnetImageAnimation.gameObject.SetActive(false);
    if (RightMagnetImageAnimation != null) RightMagnetImageAnimation.gameObject.SetActive(false);
    if (BonusImageAnimation != null) BonusImageAnimation.gameObject.SetActive(false);
    if (FreeGamesImageAnimation != null) FreeGamesImageAnimation.gameObject.SetActive(false);
    if (Winnings_ImageAnimation != null) Winnings_ImageAnimation.gameObject.SetActive(false);

    if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
    if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(() => SkipWinAnim());

    if (RaycastLayerButton) RaycastLayerButton.onClick.RemoveAllListeners();
    if (RaycastLayerButton) RaycastLayerButton.onClick.AddListener(() => CanCloseMenu());

    if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
    if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(() => { CallOnExitFunction(); socketManager.closeSocketReactnativeCall(); });

    if (Quit_Button) Quit_Button.onClick.RemoveAllListeners();
    if (Quit_Button) Quit_Button.onClick.AddListener(OpenQuitPanel);

    if (QuitNo_Button) QuitNo_Button.onClick.RemoveAllListeners();
    if (QuitNo_Button) QuitNo_Button.onClick.AddListener(delegate { ClosePopup(QuitMenuObject); });

    if (QuitYes_Button) QuitYes_Button.onClick.RemoveAllListeners();
    if (QuitYes_Button) QuitYes_Button.onClick.AddListener(CallOnExitFunction);

    if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
    if (Paytable_Button) Paytable_Button.onClick.AddListener(OpenPaytablePanel);

    if (PaytableClose_Button) PaytableClose_Button.onClick.RemoveAllListeners();
    if (PaytableClose_Button) PaytableClose_Button.onClick.AddListener(delegate { ClosePopup(PaytableMenuObject); });

    if (Menu_Button) Menu_Button.onClick.RemoveAllListeners();
    if (Menu_Button) Menu_Button.onClick.AddListener(delegate
    {
      if (!isMenu) OpenCloseMenu(true);
      else OpenCloseMenu(false);
    });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(OpenSettingsPanel);

    if (SettingsQuit_Button) SettingsQuit_Button.onClick.RemoveAllListeners();
    if (SettingsQuit_Button) SettingsQuit_Button.onClick.AddListener(delegate { ClosePopup(Settings_Object); });

    if (PaytableLeft_Button) PaytableLeft_Button.onClick.RemoveAllListeners();
    if (PaytableLeft_Button) PaytableLeft_Button.onClick.AddListener(() => ChangePage(false));

    if (PaytableRight_Button) PaytableRight_Button.onClick.RemoveAllListeners();
    if (PaytableRight_Button) PaytableRight_Button.onClick.AddListener(() => ChangePage(true));

    // Bind to Model Events
    slotManager.OnBalanceChanged += UpdateBalanceText;
    slotManager.OnLineBetChanged += UpdateLineBetText;
    slotManager.OnTotalBetChanged += UpdateTotalBetText;
    slotManager.OnFreeSpinsChanged += UpdateFreeSpinsText;
    slotManager.OnLinkRespinsChanged += SetBonusSpinCounter;

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
              if (slotManager && !slotManager.IsSpinning && !slotManager.IsAutoSpin) {
                  slotManager.StartSlots();
                  CanCloseMenu();
              }
          });
          holdHandler.onLongPress.RemoveAllListeners();
          holdHandler.onLongPress.AddListener(() => {
              if (slotManager && !slotManager.IsSpinning && !slotManager.IsAutoSpin) {
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
          lineBetPlusButton.onClick.AddListener(() => { if (slotManager) slotManager.ChangeBet(true); CanCloseMenu(); });
      }
      if (lineBetMinusButton) {
          lineBetMinusButton.onClick.RemoveAllListeners();
          lineBetMinusButton.onClick.AddListener(() => { if (slotManager) slotManager.ChangeBet(false); CanCloseMenu(); });
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

  public void RegisterBonusElements(Button startBtn, TMP_Text counterTxt, TMP_Text winningsTxt)
  {
      bonusStartButton = startBtn;
      bonusSpinCounterText = counterTxt;
      bonusWinningsText = winningsTxt;
  }

  public Button GetBonusStartButton() => bonusStartButton;
  public Transform GetAnimationParent() => AnimationParent;
  public RectTransform GetFreeSpinCountUIPositon() => FreeSpinCountUIPositon;
  public ImageAnimation GetLeftMagnetImageAnimation() => LeftMagnetImageAnimation;
  public ImageAnimation GetRightMagnetImageAnimation() => RightMagnetImageAnimation;
  public ImageAnimation GetBonusImageAnimation() => BonusImageAnimation;
  public ImageAnimation GetFreeGamesImageAnimation() => FreeGamesImageAnimation;

  public void SetNormalSpinButtonActive(bool active)
  {
      if (slotStartButton) slotStartButton.gameObject.SetActive(active);
  }

  public void SetBonusButtonActive(bool active)
  {
      if (bonusStartButton) bonusStartButton.gameObject.SetActive(active);
  }

  public void SetBonusButtonInteractable(bool interactable)
  {
      if (bonusStartButton) bonusStartButton.interactable = interactable;
  }

  public void SetBonusSpinCounter(int count)
  {
      if (bonusSpinCounterText) bonusSpinCounterText.text = count.ToString();
  }

  public void SetBonusWinningsText(string val)
  {
      if (bonusWinningsText) bonusWinningsText.text = val;
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

  void SkipWinAnim()
  {
    slotManager.CheckPopups = false;
    if (MainPopup_Object.activeInHierarchy) MainPopup_Object.SetActive(false);
    if (WinPopup_Object.activeInHierarchy) WinPopup_Object.SetActive(false);
    if (ImageAnimation?.currentAnimationState == ImageAnimation.ImageState.PLAYING) ImageAnimation?.StopAnimation();
    if (PopupAnimCoroutine != null)
      StopCoroutine(PopupAnimCoroutine);
    if (BonusCoroutine != null)
      StopCoroutine(BonusCoroutine);
    TextTween?.Kill();
    TextTween2.Kill();
    scaleTween?.Kill();
    if (BonusWinningsCoroutine != null)
    {
      StopCoroutine(BonusWinningsCoroutine);
    }
    animationFinish = true;
  }

  internal void CanCloseMenu()
  {
    if (isMenu)
    {
      OpenCloseMenu(false);
    }
  }

  private void ChangePage(bool IncDec)
  {
    if (IncDec)
    {
      if (PageIndex < GameRulesPages.Count - 1)
      {
        PageIndex++;
      }
      if (PageIndex == GameRulesPages.Count - 1)
      {
        if (PaytableRight_Button) PaytableRight_Button.interactable = false;
      }
      if (PageIndex > 0)
      {
        if (PaytableLeft_Button) PaytableLeft_Button.interactable = true;
      }
    }
    else
    {
      if (PageIndex > 0)
      {
        PageIndex--;
      }
      if (PageIndex == 0)
      {
        if (PaytableLeft_Button) PaytableLeft_Button.interactable = false;
      }
      if (PageIndex < GameRulesPages.Count - 1)
      {
        if (PaytableRight_Button) PaytableRight_Button.interactable = true;
      }
    }
    foreach (GameObject g in GameRulesPages)
    {
      g.SetActive(false);
    }
    if (GameRulesPages[PageIndex]) GameRulesPages[PageIndex].SetActive(true);
  }

  private void OpenCloseMenu(bool toggle)
  {
    if (toggle)
    {
      isMenu = true;
      if (Info_Button) Info_Button.gameObject.SetActive(true);
      if (Settings_Button) Settings_Button.gameObject.SetActive(true);

      DOTween.To(() => Info_BttnTransform.anchoredPosition, (val) => Info_BttnTransform.anchoredPosition = val, new Vector2(Info_BttnTransform.anchoredPosition.x + 150, Info_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
      {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
      });

      DOTween.To(() => Settings_BttnTransform.anchoredPosition, (val) => Settings_BttnTransform.anchoredPosition = val, new Vector2(Settings_BttnTransform.anchoredPosition.x + 300, Settings_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
      {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_BttnTransform);
      });
    }
    else
    {
      isMenu = false;
      DOTween.To(() => Info_BttnTransform.anchoredPosition, (val) => Info_BttnTransform.anchoredPosition = val, new Vector2(Info_BttnTransform.anchoredPosition.x - 150, Info_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
      {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
      });

      DOTween.To(() => Settings_BttnTransform.anchoredPosition, (val) => Settings_BttnTransform.anchoredPosition = val, new Vector2(Settings_BttnTransform.anchoredPosition.x - 300, Settings_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
      {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_BttnTransform);
      });

      DOVirtual.DelayedCall(0.1f, () =>
      {
        if (Info_Button) Info_Button.gameObject.SetActive(false);
        if (Settings_Button) Settings_Button.gameObject.SetActive(false);
      });
    }
  }

  private void OpenSettingsPanel()
  {
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
    if (Settings_Object) Settings_Object.SetActive(true);
    CanCloseMenu();
  }

  private void OpenQuitPanel()
  {
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
    if (QuitMenuObject) QuitMenuObject.SetActive(true);
    CanCloseMenu();
  }

  private void OpenPaytablePanel()
  {
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
    PageIndex = 0;
    foreach (GameObject g in GameRulesPages)
    {
      g.SetActive(false);
    }
    GameRulesPages[0].SetActive(true);
    if (PaytableLeft_Button) PaytableLeft_Button.interactable = false;
    if (PaytableRight_Button) PaytableRight_Button.interactable = true;
    if (PaytableMenuObject) PaytableMenuObject.SetActive(true);
    CanCloseMenu();
  }

  internal void LowBalPopup()
  {
    CanCloseMenu();
    OpenPopup(LBPopup_Object);
  }

  internal void DisconnectionPopup(bool isReconnection)
  {
    if (!isExit)
    {
      CanCloseMenu();
      OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void PopulateWin(int value)
  {
    Winnings_ImageAnimation.textureArray.Clear();
    Winnings_ImageAnimation.textureArray.TrimExcess();
    switch (value)
    {
      case 1:
        foreach (Sprite s in BigWin_Sprites)
        {
          Winnings_ImageAnimation.textureArray.Add(s);
          Winnings_ImageAnimation.AnimationSpeed = 25;
        }
        break;
      case 2:
        foreach (Sprite s in MegaWin_Sprites)
        {
          Winnings_ImageAnimation.textureArray.Add(s);
          Winnings_ImageAnimation.AnimationSpeed = 40;
        }
        break;
    }
    PopupAnimCoroutine = StartCoroutine(StartPopupAnim());
  }

  private IEnumerator StartPopupAnim()
  {
    if (WinPopup_Object) WinPopup_Object.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);

    Winnings_ImageAnimation.StartAnimation();
    WinTextBgImage.DOScale(Vector3.one, .5f).SetEase(Ease.OutCirc);

    double start = 0;
    TextTween = DOTween.To(() => start, (val) => start = val, socketManager.resultData.payload.winAmount, 0.8f).OnUpdate(() =>
    {
      Win_Text.text = start.ToString("F3");
    });

    yield return new WaitUntil(() => Winnings_ImageAnimation.textureArray[^1] == Winnings_ImageAnimation.rendererDelegate.sprite);
    Winnings_ImageAnimation.StopAnimation();
    scaleTween = WinTextBgImage.DOScale(Vector3.zero, .5f).SetEase(Ease.InBack).OnComplete(() =>
    {
      slotManager.CheckPopups = false;
      ClosePopup(WinPopup_Object);
    });
  }

  internal void ADfunction()
  {
    OpenPopup(ADPopup_Object);
  }

  internal void InitialiseUIData(PaylineData symbolsText)
  {
    PopulateSymbolsPayout(symbolsText);
    PopulateTopSymbolsPayout();
  }

  internal void PopulateTopSymbolsPayout()
  {
    for (int i = 0; i < TopPayoutTextUI.Length; i++)
    {
      // Assign payouts if needed
    }
  }

  internal IEnumerator TrailRendererAnimation(GameObject TrailRendererGO, int textIndex, int coinvalue, bool IsBonus = false)
  {
    TrailRenderer trail = TrailRendererGO.GetComponent<TrailRenderer>();
    TrailRendererGO.transform.parent.GetChild(textIndex).GetComponent<TMP_Text>().text = coinvalue.ToString() + "x";
    TrailRendererGO.gameObject.SetActive(true);
    Vector3 tempPosi = trail.transform.position;

    Vector3 DOMovePosition = new();
    TMP_Text text = null;
    if (IsBonus)
    {
      DOMovePosition = BonusWinningsPosition.position;
      text = BonusWinnings_Text;
    }
    else
    {
      DOMovePosition = BaseWinningsPosition.position;
      text = BaseWinnings_Text;
    }
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
    animationFinish = false;
    if (imageAnimation.name == "FreeSpinsImageAnimation")
    {
      imageAnimation.transform.parent.gameObject.SetActive(true);
    }
    else if (imageAnimation.name == "BonusWonImageAnimation")
    {
      MainPopup_Object.SetActive(true);
      WinPopup_Object.SetActive(true);
    }
    else
    {
      imageAnimation.transform.parent.gameObject.SetActive(true);
    }
    imageAnimation.gameObject.SetActive(true);
    imageAnimation.StartAnimation();
    ImageAnimation = imageAnimation;

    TMP_Text text = null;
    bool useF2 = false;
    if (imageAnimation.name == "FreeSpinsImageAnimation")
    {
      text = FreeSpinsText;
    }
    else if (imageAnimation.name == "BonusWonImageAnimation")
    {
      text = BonusGameWinningsText;
      useF2 = true;
    }

    if (text != null)
    {
      text.text = "0";
      text.DOFade(1, 0.5f);

      double start = 0;
      TextTween2 = DOTween.To(() => start, (val) => start = val, num, 0.8f).OnUpdate(() =>
      {
        if (useF2) text.text = start.ToString("F3");
        else text.text = ((int)start).ToString();
      });
      yield return TextTween2;
    }

    yield return new WaitUntil(() => imageAnimation.rendererDelegate.sprite == imageAnimation.textureArray[^1]);

    if (text != null) text.DOFade(0, 0.5f);
    imageAnimation.StopAnimation();
    ImageAnimation = null;
    if (imageAnimation.name == "FreeSpinsImageAnimation")
    {
      imageAnimation.transform.parent.gameObject.SetActive(false);
    }
    else if (imageAnimation.name == "BonusWonImageAnimation")
    {
      MainPopup_Object.SetActive(false);
      WinPopup_Object.SetActive(false);
    }
    else
    {
      imageAnimation.transform.parent.gameObject.SetActive(false);
    }
    animationFinish = true;
  }



  private void PopulateSymbolsPayout(PaylineData paylines)
  {
    double multiplyer = socketManager.initialData.gameData.bets[slotManager.BetCounter];
    for (int i = 0; i < SymbolsText.Count; i++)
    {
      string text = null;
      if (paylines.symbols[i].multiplier[0] != 0)
      {
        text += "5x - " + paylines.symbols[i].multiplier[0] * multiplyer;
      }
      if (paylines.symbols[i].multiplier[1] != 0)
      {
        text += "\n4x - " + paylines.symbols[i].multiplier[1] * multiplyer;
      }
      if (paylines.symbols[i].multiplier[2] != 0)
      {
        text += "\n3x - " + paylines.symbols[i].multiplier[2] * multiplyer;
      }
      if (SymbolsText[i]) SymbolsText[i].text = text;
    }

    int j = 0;
    for (int i = 10; i <= 16; i++)
    {
      SpecialSymbolsText[j].text = paylines.symbols[i].description.ToString();
      j++;
    }
  }

  private void CallOnExitFunction()
  {
    isExit = true;
    slotManager.CallCloseSocket();
  }

  private void OpenPopup(GameObject Popup)
  {
    if (Popup) Popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  private void ClosePopup(GameObject Popup)
  {
    if (Popup) Popup.SetActive(false);
    if (!DisconnectPopup_Object.activeSelf)
    {
      if (MainPopup_Object) MainPopup_Object.SetActive(false);
    }
  }

  internal void SetJackpotText(Jackpot jackpot)
  {
    for (int i = 0; i < jackpot.payout.Count; i++)
    {
      JackpotText[i].text = jackpot.payout[i].ToString();
    }
  }

  internal void DisconnectionPopup()
  {
    if (!isExit)
    {
      OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void CheckAndClosePopups()
  {
    if (ReconectingPopup_Object.activeInHierarchy)
    {
      ClosePopup(ReconectingPopup_Object);
    }
    if (DisconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(DisconnectPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    OpenPopup(ReconectingPopup_Object);
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
