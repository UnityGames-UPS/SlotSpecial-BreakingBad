using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System;

public class UIManager : MonoBehaviour
{
  [Header("Script References")]
  [SerializeField] private AudioController audioController;
  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private SocketIOManager socketManager;

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

  [Header("Reconection Popup")]
  [SerializeField]
  private GameObject ReconectingPopup_Object;

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
  [Header("RaycastBlocker")]
  [SerializeField] internal GameObject DiamondPayoutObject;
  [SerializeField] internal GameObject DiamondPayoutParent;
  [SerializeField] internal DiamondPayout diamondPayout;
  [SerializeField] internal List<Sprite> diamondPaoutSprite;
  [SerializeField] internal Transform DiamondSlotFinalPosition;

  private bool isMusic = true;
  private bool isSound = true;
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
  private void Start()
  {
    if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
    if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(() => SkipWinAnim());

    if (RaycastLayerButton) RaycastLayerButton.onClick.RemoveAllListeners();
    if (RaycastLayerButton) RaycastLayerButton.onClick.AddListener(() => CanCloseMenu());

    if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
    if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(() => { CallOnExitFunction(); socketManager.closeSocketReactnativeCall(); });

    if (Sound_Slider)
    {
      Sound_Slider.onValueChanged.RemoveAllListeners();
      Sound_Slider.onValueChanged.AddListener((val) => { OnSoundChanged(val); });
    }

    if (Music_Slider)
    {
      Music_Slider.onValueChanged.RemoveAllListeners();
      Music_Slider.onValueChanged.AddListener((val) => { OnMusicChanged(val); });
    }

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
      if (!isMenu)
      {
        OpenCloseMenu(true);
      }
      else
      {
        OpenCloseMenu(false);
      }
    });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(OpenSettingsPanel);

    if (SettingsQuit_Button) SettingsQuit_Button.onClick.RemoveAllListeners();
    if (SettingsQuit_Button) SettingsQuit_Button.onClick.AddListener(delegate { ClosePopup(Settings_Object); });

    if (PaytableLeft_Button) PaytableLeft_Button.onClick.RemoveAllListeners();
    if (PaytableLeft_Button) PaytableLeft_Button.onClick.AddListener(() => ChangePage(false));

    if (PaytableRight_Button) PaytableRight_Button.onClick.RemoveAllListeners();
    if (PaytableRight_Button) PaytableRight_Button.onClick.AddListener(() => ChangePage(true));
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
    if (audioController) audioController.PlayButtonAudio();

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
    if (audioController) audioController.PlayButtonAudio();
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

  private void OnSoundChanged(float value)
  {
    audioController.OnVolumeChanged(value, "sound");
  }

  private void OnMusicChanged(float value)
  {
    audioController.OnVolumeChanged(value, "music");
  }

  private void OpenSettingsPanel()
  {
    if (audioController) audioController.PlayButtonAudio();

    if (MainPopup_Object) MainPopup_Object.SetActive(true);
    if (Settings_Object) Settings_Object.SetActive(true);
    CanCloseMenu();
  }

  private void OpenQuitPanel()
  {
    if (audioController) audioController.PlayButtonAudio();


    if (MainPopup_Object) MainPopup_Object.SetActive(true);
    if (QuitMenuObject) QuitMenuObject.SetActive(true);
    CanCloseMenu();
  }

  private void OpenPaytablePanel()
  {
    if (audioController) audioController.PlayButtonAudio();

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

    audioController.PlayWLAudio("bigwin");

    Winnings_ImageAnimation.StartAnimation();
    WinTextBgImage.DOScale(Vector3.one, .5f).SetEase(Ease.OutCirc);

    double start = 0;
    double winning = socketManager.resultData.payload.winAmount;
    TextTween = DOTween.To(() => start, (val) => start = val, socketManager.resultData.payload.winAmount, 0.8f).OnUpdate(() =>
    {
      Win_Text.text = start.ToString("F3");
    });

    yield return new WaitUntil(() => Winnings_ImageAnimation.textureArray[^1] == Winnings_ImageAnimation.rendererDelegate.sprite);
    Winnings_ImageAnimation.StopAnimation();
    audioController.StopWLAaudio();
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

  internal void InitialiseUIData(Paylines symbolsText)
  {

    PopulateSymbolsPayout(symbolsText);
    PopulateTopSymbolsPayout();
    audioController.PlayWLAudio("StartAudio");
    //add code to loop through top payout ui and change their payout values accordingly
  }

  internal void PopulateTopSymbolsPayout()
  {
    for (int i = 0; i < TopPayoutTextUI.Length; i++)
    {
      //TopPayoutTextUI[i].text = socketManager.initialData.Jackpot[i].ToString() + "x";              //V2 ###########
    }
  }

  internal int multiplierCount = 0;
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
      audioController.PlayWLAudio("CollectCoin");
      trail.gameObject.SetActive(false);
      trail.transform.position = tempPosi;

      // double currWin = 0;
      int multiplier = 0;
      try
      {
        // currWin = double.Parse(text.text);
        multiplier = int.Parse(TrailRendererGO.transform.parent.GetChild(textIndex).GetComponent<TMP_Text>().text.Replace("x", ""));
        multiplierCount += multiplier;
        // coin = slotManager.currentLineBet * multiplier * cashCollects;
      }
      catch (Exception e)
      {
        Debug.Log(e.Message);
      }
      int start = int.Parse(text.text.Replace("x", ""));
      // double Total = currWin + coin;
      DOTween.To(() => start, (val) => start = val, coinvalue, 0.3f).OnUpdate(() =>
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
    bool audio = false;
    if (imageAnimation.name == "FreeSpinsImageAnimation")
    {
      text = FreeSpinsText;
    }
    else if (imageAnimation.name == "BonusWonImageAnimation")
    {
      audio = true;
      audioController.PlayWLAudio("bigwin");
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
        if (useF2)
        {
          text.text = start.ToString("F3");
        }
        else
        {
          text.text = ((int)start).ToString();
        }
      });
      yield return TextTween2;
    }

    yield return new WaitUntil(() => imageAnimation.rendererDelegate.sprite == imageAnimation.textureArray[^1]);

    if (text != null) text.DOFade(0, 0.5f);
    if (audio) audioController.StopWLAaudio();
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
  internal IEnumerator ManageDiamondPayout(
    int resultIndex,
    string resultValue,
    Transform startPos
)
  {

    GameObject diamondObj = Instantiate(
        DiamondPayoutObject,
        startPos.position,
        Quaternion.identity,
        DiamondPayoutParent.transform
    );
    diamondPayout = diamondObj.GetComponent<DiamondPayout>();
    Transform diamondTr = diamondObj.transform;

    Vector3 startLocalPos = startPos.position;
    Vector3 startScale = diamondTr.localScale;

    yield return new WaitForSeconds(1f);


    Sequence moveScaleSeq = DOTween.Sequence();

    moveScaleSeq.Append(
        diamondTr.DOLocalMove(DiamondSlotFinalPosition.localPosition, 0.4f)
            .SetEase(Ease.OutQuad)
    );

    moveScaleSeq.Join(
        diamondTr.DOScale(startScale * 3f, 0.4f)
            .SetEase(Ease.OutBack)
    );

    yield return moveScaleSeq.WaitForCompletion();
    yield return new WaitForSeconds(1f);

    yield return StartCoroutine(
        diamondPayout.SlotTween(resultIndex, resultValue, diamondPaoutSprite[resultIndex])
    );
    yield return new WaitForSeconds(1f);


    Sequence returnSeq = DOTween.Sequence();

    returnSeq.Append(
        diamondTr.DOLocalMove(startLocalPos, 0.4f)
            .SetEase(Ease.InOutQuad)
    );

    returnSeq.Join(
        diamondTr.DOScale(startScale, 0.4f)
            .SetEase(Ease.InQuad)
    );

    yield return returnSeq.WaitForCompletion();

    yield return new WaitForSeconds(1f);

    Destroy(diamondObj);
  }

  private void PopulateSymbolsPayout(Paylines paylines)
  {
    double multiplyer = socketManager.initialData.gameData.bets[slotManager.BetCounter];
    for (int i = 0; i < SymbolsText.Count; i++)             // V2##############
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
    audioController.PlayButtonAudio();
    slotManager.CallCloseSocket();
  }

  private void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();

    if (Popup) Popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  private void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
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
}
