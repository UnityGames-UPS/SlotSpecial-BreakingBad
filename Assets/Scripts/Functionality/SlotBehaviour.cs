using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{
  [Header("Script References")]
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private StaticSymbolController staticSymbolController;
  [SerializeField] private AudioController audioController;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private BonusController _bonusManager;

  [Header("Sprites References")]
  [SerializeField] private Sprite[] SlotSymbols;  //images taken initially
  [SerializeField] private Sprite[] losPollosSprites;
  [SerializeField] private Sprite[] losPollosNumberSprites;
  [SerializeField] private Sprite losPollosNoNumberSprite;
  [SerializeField] private Sprite[] JackpotSlotSymbols;


  [Header("Slot References")]
  [SerializeField] private List<SlotImage> images;     //class to store total images
  [SerializeField] internal List<SlotImage> Tempimages;     //class to store the result matrix
  [SerializeField] internal List<SlotImage> ResultMatrix;

  [Header("Slots Transform Reference")]
  [SerializeField] private Transform[] Slot_Transform;

  [Header("UI Objects References")]
  [SerializeField] private Button SlotStart_Button;
  [SerializeField] private Button AutoSpin_Button;
  [SerializeField] private Button AutoSpinStop_Button;
  [SerializeField] private Button TotalBetPlus_Button;
  [SerializeField] private Button TotalBetMinus_Button;
  [SerializeField] private Button LineBetPlus_Button;
  [SerializeField] private Button LineBetMinus_Button;
  [SerializeField] private Button Turbo_Button;
  [SerializeField] private Button StopSpin_Button;

  [SerializeField] private TMP_Text Balance_text;
  [SerializeField] private TMP_Text TotalBet_text;
  [SerializeField] private TMP_Text LineBet_text;
  [SerializeField] private TMP_Text TotalWin_text;
  [SerializeField] private TMP_Text BigWin_Text;
  [SerializeField] private TMP_Text CoinWinning_Text;
  [SerializeField] private TMP_Text FSnum_text;

  [SerializeField] private ImageAnimation BonusImageAnimation;
  [SerializeField] private ImageAnimation FreeGamesImageAnimation;
  [SerializeField] private ImageAnimation LeftMagnetImageAnimation;
  [SerializeField] private ImageAnimation RightMagnetImageAnimation;

  [SerializeField] private CanvasGroup FreeSpinsUI_Panel;
  [SerializeField] private CanvasGroup WinningsUI_Panel;
  [SerializeField] private CanvasGroup TopPayoutUI_CG;
  [SerializeField] private CanvasGroup LinesUI;
  [SerializeField] private CanvasGroup TotalBetUI;
  [SerializeField] private CanvasGroup LineBetUI;
  [SerializeField] private RectTransform FreeSpinCountUIPositon;
  [SerializeField] private Transform AnimationParent;

  [Header("Animation Sprites References")]
  [SerializeField] private Sprite[] B_Sprites;
  [SerializeField] private Sprite[] C_Sprites;
  [SerializeField] private Sprite[] N_Sprites;
  [SerializeField] private Sprite[] O_Sprites;
  [SerializeField] private Sprite[] Link_Sprites;
  [SerializeField] private Sprite[] MegaLink_Sprites;
  [SerializeField] private Sprite[] Barrel_Sprites;
  [SerializeField] private Sprite[] Bus_Sprites;
  [SerializeField] private Sprite[] Blue_Sprites;
  [SerializeField] private Sprite[] Orange_Sprites;
  [SerializeField] private Sprite[] Purple_Sprites;
  [SerializeField] private Sprite[] Yellow_Sprites;
  [SerializeField] private Sprite[] Diamond_Sprites;
  [SerializeField] private Sprite[] CC_Sprites;
  [SerializeField] private Sprite[] LP2_Sprites;
  [SerializeField] private Sprite[] LP3_Sprites;
  [SerializeField] private Sprite[] LP4_Sprites;
  [SerializeField] private Sprite[] LP5_Sprites;
  [SerializeField] private Sprite[] LP7_Sprites;
  [SerializeField] private Sprite[] GoldCoin_Sprites;
  [SerializeField] private Sprite[] MagnetInSprites;
  [SerializeField] private Sprite[] MagnetLightening_Sprites;
  [SerializeField] Sprite TurboToggleSprite;

  private List<Tweener> alltweens = new List<Tweener>();
  private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

  private bool IsAutoSpin = false;
  private bool IsFreeSpin = false;
  internal bool IsBonus = false;
  private bool WinAnimationFin = true;
  private bool IsSpinning = false;
  private bool CheckSpinAudio = false;
  internal bool CheckPopups = false;

  private Coroutine AutoSpinRoutine = null;
  private Coroutine FreeSpinRoutine = null; //CAN BE REMOVED
  private Coroutine tweenroutine;
  private Coroutine LineAnimRoutine = null;

  int tweenHeight = 0;  //calculate the height at which tweening is done
  internal int BetCounter = 0;
  protected int Lines = 30;
  private int numberOfSlots = 5;          //number of columns
  private int freeSpinsCount;
  [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

  private double currentBalance = 0;
  private double currentTotalBet = 0;
  internal double currentLineBet = 0;
  private bool StopSpinToggle;
  private float SpinDelay = 0.2f;
  private bool IsTurboOn;
  internal bool WasAutoSpinOn;
  private Tween BalanceTween;


  private bool resumeFreeSpinAfterLink = false;
  private int savedFreeSpinCount = 0;


  private void Start()
  {
    IsAutoSpin = false;

    if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
    if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

    if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
    if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

    if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
    if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); uiManager.CanCloseMenu(); });

    if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.RemoveAllListeners();
    if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); uiManager.CanCloseMenu(); });

    if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.RemoveAllListeners();
    if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); uiManager.CanCloseMenu(); });

    if (LineBetPlus_Button) LineBetPlus_Button.onClick.RemoveAllListeners();
    if (LineBetPlus_Button) LineBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); uiManager.CanCloseMenu(); });

    if (LineBetMinus_Button) LineBetMinus_Button.onClick.RemoveAllListeners();
    if (LineBetMinus_Button) LineBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); uiManager.CanCloseMenu(); });

    if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
    if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(delegate { AutoSpin(); uiManager.CanCloseMenu(); });

    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(delegate { StopAutoSpin(); uiManager.CanCloseMenu(); });

    tweenHeight = (13 * IconSizeFactor) - 280;
  }
  void TurboToggle()
  {
    audioController.PlayButtonAudio();
    if (IsTurboOn)
    {
      IsTurboOn = false;
      Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
      Turbo_Button.image.sprite = TurboToggleSprite;
    }
    else
    {
      IsTurboOn = true;
      Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
    }
  }

  internal void FreeSpin(int spins)
  {
    IsFreeSpin = true;
    if (SlotStart_Button) SlotStart_Button.gameObject.SetActive(true);
    if (SlotStart_Button) SlotStart_Button.interactable = false;
    if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
    if (AutoSpin_Button) AutoSpin_Button.interactable = true;
    if (LineBetPlus_Button) LineBetPlus_Button.interactable = false;
    if (LineBetMinus_Button) LineBetMinus_Button.interactable = false;
    if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = false;
    if (TotalBetMinus_Button) TotalBetMinus_Button.interactable = false;

    if (FreeSpinRoutine != null)
    {
      StopCoroutine(FreeSpinRoutine);
      FreeSpinRoutine = null;
    }
    FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
  }

  private IEnumerator FreeSpinCoroutine(int spinchances)
  {
    int i = 0;
    while (i < spinchances)
    {
      StartSlots();
      yield return tweenroutine;
      // if(IsBonus){
      //     yield return new WaitUntil(()=> !IsBonus);
      // }
      yield return new WaitForSeconds(SpinDelay);
      i++;
    }
    if (WasAutoSpinOn)
    {
      yield return new WaitForSeconds(0.2f);
      AutoSpin();
    }
    else
    {
      ToggleButtonGrp(true);
    }
    IsFreeSpin = false;
  }

  #region Autospin
  internal void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      IsAutoSpin = true;
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
    }
  }

  private void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      IsAutoSpin = false;
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      StartSlots(IsAutoSpin);
      yield return tweenroutine;
      yield return new WaitForSeconds(SpinDelay);
    }
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    yield return new WaitUntil(() => !IsSpinning);
    if (AutoSpinRoutine != null || tweenroutine != null)
    {
      if (AutoSpinRoutine != null) StopCoroutine(AutoSpinRoutine);
      if (tweenroutine != null) StopCoroutine(tweenroutine);
      tweenroutine = null;
      AutoSpinRoutine = null;
      if (!IsBonus) ToggleButtonGrp(true);
    }
  }
  #endregion

  private void CompareBalance()
  {
    if (currentBalance < currentTotalBet)
    {
      uiManager.LowBalPopup();
    }
  }

  private void ChangeBet(bool IncDec)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (IncDec)
    {
      BetCounter++;
      if (BetCounter >= SocketManager.initialData.gameData.bets.Count)
      {
        BetCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      BetCounter--;
      if (BetCounter < 0)
      {
        BetCounter = SocketManager.initialData.gameData.bets.Count - 1; // Loop to the last bet
      }
    }

    if (LineBet_text) LineBet_text.text = SocketManager.initialData.gameData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.gameData.bets[BetCounter] * Lines).ToString();

    currentTotalBet = SocketManager.initialData.gameData.bets[BetCounter] * Lines;
    currentLineBet = SocketManager.initialData.gameData.bets[BetCounter];
    // uiManager.PopulateTopSymbolsPayout();
    // CompareBalance();
  }

  #region InitialFunctions
  internal void shuffleInitialMatrix()
  {
    for (int i = 0; i < images.Count; i++)
    {
      for (int j = 0; j < images[i].slotImages.Count; j++)
      {
        int randomIndex;
        do
        {
          randomIndex = UnityEngine.Random.Range(0, SlotSymbols.Length - 8);
        }
        while (randomIndex == 9);

        images[i].slotImages[j].sprite = SlotSymbols[randomIndex];
      }
    }
  }

  internal void SetInitialUI()
  {
    BetCounter = 0;
    if (LineBet_text) LineBet_text.text = SocketManager.initialData.gameData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.gameData.bets[BetCounter] * Lines).ToString();
    if (TotalWin_text) TotalWin_text.text = "0.000";
    if (Balance_text) Balance_text.text = SocketManager.playerdata.balance.ToString("f3");
    currentBalance = SocketManager.playerdata.balance;
    currentTotalBet = SocketManager.initialData.gameData.bets[BetCounter] * Lines;
    currentLineBet = SocketManager.initialData.gameData.bets[BetCounter];
    shuffleInitialMatrix();
    CompareBalance();
    uiManager.InitialiseUIData(SocketManager.initUIData.paylines);
    uiManager.SetJackpotText(SocketManager.initialData.features.jackpot);
  }
  #endregion

  private void ReorderImages()
  {
    for (int i = 0; i < Tempimages.Count; i++)
    {
      for (int j = 0; j < 3; j++)
      {
        if (Tempimages[i].slotImages[j].sprite == SlotSymbols[14]) //if the symbol is cash collect
        {
          // Store the original sibling index before changing it
          Transform slotTransform = Tempimages[i].slotImages[j].transform;
          int originalSiblingIndex = slotTransform.GetSiblingIndex();

          // Add the slot transform and its original sibling index to the list
          changedSlots.Add((slotTransform, originalSiblingIndex));

          // Now apply the changes
          SetUpAccordingToCC(slotTransform);
        }
      }
    }
  }

  private void SetUpAccordingToCC(Transform slotTransform)
  {
    Debug.Log("Here");
    slotTransform.SetSiblingIndex(17);

    slotTransform.GetComponent<Mask>().enabled = false;
    for (int i = 0; i < 2; i++)
    {
      var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
      if (animation != null)
      {
        animation.AnimationSpeed = 15;  // Change animation speed
        Image image = slotTransform.GetChild(i).GetComponent<Image>();
        image.DOFade(0, 0);
        image.gameObject.SetActive(true);  // Activate the animation object
        image.DOFade(1, 0.5f);
        animation.StartAnimation();     // Start animation
      }
    }
  }

  // Function to reset all changed slots
  private void ResetImages()
  {
    foreach (var (slotTransform, originalSiblingIndex) in changedSlots)
    {
      // Reset the sibling index to the original value
      slotTransform.SetSiblingIndex(originalSiblingIndex);

      slotTransform.GetComponent<Mask>().enabled = true;
      // Stop the animation and reset the state
      for (int i = 0; i < 2; i++)
      {
        var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
        if (animation != null)
        {
          animation.StopAnimation();  // Assuming you have a StopAnimation method
          animation.rendererDelegate.DOFade(1, 0.5f).OnComplete(() =>
          {
            animation.gameObject.SetActive(false);
          });
        }
      }
    }

    // Clear the list after resetting everything
    changedSlots.Clear();
  }

  private void OnApplicationFocus(bool focus)
  {
    audioController.CheckFocusFunction(focus, CheckSpinAudio);
  }

  //function to populate animation sprites accordingly
  private void PopulateAnimationSprites(ImageAnimation animScript, int val, int LP = 0, string coin = null)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    // animScript.doLoopAnimation=true;
    switch (val)
    {
      case 0:
        for (int i = 0; i < C_Sprites.Length; i++)
        {
          animScript.textureArray.Add(C_Sprites[i]);
        }
        animScript.AnimationSpeed = 19f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 1:
        for (int i = 0; i < O_Sprites.Length; i++)
        {
          animScript.textureArray.Add(O_Sprites[i]);
        }
        animScript.AnimationSpeed = 19f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 2:
        foreach (Sprite sprite in N_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 19f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 3:
        foreach (Sprite sprite in B_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 19f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 4:
        foreach (Sprite sprite in Barrel_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 5:
        foreach (Sprite sprite in Bus_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 6:
        foreach (Sprite sprite in Orange_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 7:
        foreach (Sprite sprite in Purple_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 8:
        foreach (Sprite sprite in Blue_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 10:
        foreach (Sprite sprite in Yellow_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 16f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 11:
        foreach (Sprite sprite in Link_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 22f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 12:
        foreach (Sprite sprite in MegaLink_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 12f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 14:
        foreach (Sprite sprite in CC_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 12f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 15:
        foreach (Sprite sprite in GoldCoin_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 22f;
        animScript.transform.GetChild(3).GetComponent<TMP_Text>().text = coin;
        animScript.transform.GetChild(3).gameObject.SetActive(true);
        break;
      case 16:
        foreach (Sprite sprite in Diamond_Sprites)
        {
          animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 17f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
      case 17:
        if (LP == 2)
        {
          foreach (Sprite sprite in LP2_Sprites)
          {
            animScript.textureArray.Add(sprite);
          }
        }
        else if (LP == 3)
        {
          foreach (Sprite sprite in LP3_Sprites)
          {
            animScript.textureArray.Add(sprite);
          }
        }
        else if (LP == 4)
        {
          foreach (Sprite sprite in LP4_Sprites)
          {
            animScript.textureArray.Add(sprite);
          }
        }
        else if (LP == 5)
        {
          foreach (Sprite sprite in LP5_Sprites)
          {
            animScript.textureArray.Add(sprite);
          }
        }
        else if (LP == 7)
        {
          foreach (Sprite sprite in LP7_Sprites)
          {
            animScript.textureArray.Add(sprite);
          }
        }
        else
        {
          Debug.LogError("LP index value sent was wrong");
        }
        animScript.AnimationSpeed = 12f;
        animScript.transform.GetChild(3).gameObject.SetActive(false);
        break;
    }
  }

  #region SlotSpin
  //starts the spin process
  private void StartSlots(bool autoSpin = false)
  {
    if (audioController) audioController.PlaySpinButtonAudio();

    if (IsFreeSpin)
    {
      freeSpinsCount -= 1;
      FSnum_text.text = freeSpinsCount.ToString();
    }

    if (!autoSpin)
    {
      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        StopCoroutine(tweenroutine);
        tweenroutine = null;
        AutoSpinRoutine = null;
      }
    }
    if (SlotStart_Button) SlotStart_Button.interactable = false;
    if (LineBetPlus_Button && LineBetPlus_Button.interactable != false) LineBetPlus_Button.interactable = false;
    if (LineBetMinus_Button && LineBetMinus_Button.interactable != false) LineBetMinus_Button.interactable = false;
    if (TotalBetPlus_Button && TotalBetPlus_Button.interactable != false) TotalBetPlus_Button.interactable = false;
    if (TotalBetMinus_Button && TotalBetMinus_Button.interactable != false) TotalBetMinus_Button.interactable = false;
    if (TotalWin_text) TotalWin_text.text = "0.000";

    StopGameAnimation(); //commented this line for testing

    tweenroutine = StartCoroutine(TweenRoutine());
  }

  //manage the Routine for spinning of the slots
  private IEnumerator TweenRoutine()
  {
    bool winningsDisplayed = false;
    if (currentBalance < currentTotalBet && !IsFreeSpin) // Check if balance is sufficient to place the bet
    {
      CompareBalance();
      StopAutoSpin();
      yield return new WaitForSeconds(1);
      ToggleButtonGrp(true);
      yield break;
    }

    CheckSpinAudio = true;
    IsSpinning = true;
    ToggleButtonGrp(false);

    if (!IsFreeSpin)
    {
      BalanceDeduction();
    }
    if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
    {
      StopSpin_Button.gameObject.SetActive(true);
    }
    for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
    {
      InitializeTweening(Slot_Transform[i]);
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    currentBalance = SocketManager.playerdata.balance;

    PopulateResultMatrix();

    bool magnetAnim = false;
    int slotIndex = 0;
    int ccIndex = 0;


    var ccResult = SocketManager.resultData.payload.cashCollectResult;

    bool isCCTriggered = ccResult != null && ccResult.triggered;


    if (!IsTurboOn && !IsAutoSpin)
    {
      if (SocketManager.resultData.payload.cashCollectResult != null &&
          UnityEngine.Random.Range(0f, 1f) >= 0.85f)
      {
        magnetAnim = false;

        // Only check reel 0 and 4
        int[] reelsToCheck = { 0, 4 };

        foreach (int reel in reelsToCheck)
        {
          for (int row = 0; row <= 1; row++)
          {
            if (Tempimages[reel].slotImages[row].sprite == SlotSymbols[14])
            {
              magnetAnim = true;
              slotIndex = reel;
              ccIndex = row;
              break;
            }
          }

          if (magnetAnim)
            break;
        }
      }
    }








    if (IsTurboOn)
    {
      yield return new WaitForSeconds(0.3f);
      StopSpinToggle = true;
    }
    else
    {
      for (int i = 0; i < 8; i++)
      {
        yield return new WaitForSeconds(0.1f);
        if (StopSpinToggle)
        {
          break;
        }
      }
      StopSpin_Button.gameObject.SetActive(false);
    }

    for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
    {
      if (!magnetAnim)
      {
        yield return StopTweening(5, Slot_Transform[i], i, false, 0, StopSpinToggle);
      }
      else
      {
        if (i == slotIndex)
          yield return StopTweening(5, Slot_Transform[i], i, magnetAnim, ccIndex);
        else
          yield return StopTweening(5, Slot_Transform[i], i);
      }
    }
    if (SocketManager.resultData.payload.winAmount > 0)
    {
      SpinDelay = 1.2f;
    }
    else
    {
      SpinDelay = 0.2f;
    }
    StopSpinToggle = false;
    yield return alltweens[^1].OnComplete(KillAllTweens);
    yield return new WaitForSeconds(0.2f);
    yield return StartSpecialSymbolAnimations();
    yield return new WaitForSeconds(0.3f);
    if (SocketManager.resultData.payload.winAmount > 0)
    {
      List<LineWin> winLine = new();
      foreach (var item in SocketManager.resultData.payload.lineWins)
      {
        winLine.Add(item);
      }
      CheckPayoutLineBackend(winLine);

    }

    if (IsAutoSpin || SocketManager.resultData.payload.isFreeSpinActive || SocketManager.resultData.payload.linkFeatureActive)
    {
      yield return new WaitUntil(() => WinAnimationFin);
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }

    // if (SocketManager.resultData.features.jackpot.payout != null) //jackpot removed
    // {
    //   bool Jackpottriggered = false;
    //   for (int i = 0; i < ResultMatrix.Count && !Jackpottriggered; i++)
    //   {
    //     for (int j = 0; j < ResultMatrix[i].slotImages.Count && !Jackpottriggered; j++)
    //     {
    //       if (SocketManager.resultData.matrix[i][j] == "15")
    //       {
    //         Jackpottriggered = true;
    //         yield return PlayJackpotAnimation(ResultMatrix[i].slotImages[j].rectTransform);
    //         break;
    //       }
    //     }
    //   }
    //   yield return new WaitForSeconds(.2f);
    // }

    if (isCCTriggered)
    {
      WinningsUI_Panel.DOFade(1, 0.3f);

      switchTopUI(true);
      uiManager.multiplierCount = 0;
      foreach (var item in SocketManager.resultData.payload.coinPositions)
      {
        if (item.symbolId == 15)
        {
          yield return uiManager.TrailRendererAnimation(ResultMatrix[item.position[0]].slotImages[item.position[1]].transform.GetChild(5).gameObject, 3, item.coinValue);
        }
      }
      // for (int i = 0; i < ResultMatrix.Count; i++)
      // {
      //   for (int j = 0; j < ResultMatrix[i].slotImages.Count; j++)
      //   {
      //     if (ResultMatrix[i].slotImages[j].sprite == SlotSymbols[15])
      //     {
      //       yield return uiManager.TrailRendererAnimation(ResultMatrix[i].slotImages[j].transform.GetChild(5).gameObject, 3, ccCount);
      //     }
      //   }
      // }
      yield return new WaitForSeconds(1.2f);
      switchTopUI(false);
      WinningsUI_Panel.DOFade(0, 0.3f).OnComplete(() => { CoinWinning_Text.text = "0"; });

      yield return new WaitForSeconds(.2f);
    }
    if (SocketManager.resultData.payload.isLinkTriggered)
    {
      IsBonus = true;

      // Pause FreeSpins if already running
      if (IsFreeSpin)
      {
        resumeFreeSpinAfterLink = true;
        savedFreeSpinCount = freeSpinsCount;

        if (FreeSpinRoutine != null)
        {
          StopCoroutine(FreeSpinRoutine);
          FreeSpinRoutine = null;
        }

        IsFreeSpin = false;
      }

      yield return ResetUI();

      uiManager.BonusCoroutine =
          StartCoroutine(uiManager.MidGameImageAnimation(BonusImageAnimation));

      yield return new WaitUntil(() => uiManager.animationFinish);

      staticSymbolController.TurnOnIndices(GenerateFreezedLocations());

      yield return new WaitForSeconds(0.5f);

      _bonusManager.StartBonus(
          SocketManager.resultData.payload.linkRespinsRemaining
      );

      IsSpinning = false;
      yield break;   // ✅ EXIT AFTER LINK STARTS
    }
    if (SocketManager.resultData.payload.isFreeSpinTriggered)
    {
      if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
      {
        winningsDisplayed = true;
        CheckPopups = true;
        WinningsTextAnimation();
        CheckWinPopups();

        yield return new WaitUntil(() => !CheckPopups);
        yield return new WaitForSeconds(.5f);
      }
      yield return ResetUI();

      OpenFreeSpinsUI();
      IsFreeSpin = true;

      int extraFreeSpin = 0;
      yield return new WaitForSeconds(.5f);
      if (SocketManager.resultData.payload.freeSpinsRemaining > freeSpinsCount)
      {
        yield return FreeSpinsSymbolAnimation();
        extraFreeSpin = SocketManager.resultData.payload.freeSpinsRemaining - freeSpinsCount;
      }

      freeSpinsCount = SocketManager.resultData.payload.freeSpinsRemaining;
      yield return new WaitForSeconds(1f); // Optional delay for UI stability

      if (extraFreeSpin != 0)
      {
        yield return uiManager.MidGameImageAnimation(FreeGamesImageAnimation, extraFreeSpin);
      }
      else
      {
        yield return uiManager.MidGameImageAnimation(FreeGamesImageAnimation, freeSpinsCount);
      }
      yield return new WaitForSeconds(0.5f);
      // if (!SocketManager.resultData.bonus.isBonus)
      // {
      IsSpinning = false;
      FreeSpin(freeSpinsCount);
      // yield break;
      //}
    }
    // bool callbonus = false;
    // if (SocketManager.resultData.payload.isLinkTriggered)
    // {
    //   callbonus = true;
    // }
    // if (callbonus)
    // {
    //   if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
    //   {
    //     winningsDisplayed = true;
    //     CheckPopups = true;
    //     WinningsTextAnimation();
    //     CheckWinPopups();

    //     yield return new WaitUntil(() => !CheckPopups);
    //     yield return new WaitForSeconds(.5f);
    //   }
    //   IsBonus = true;
    //   yield return ResetUI();
    //   if (IsFreeSpin)
    //   {
    //     resumeFreeSpinAfterLink = true;
    //     savedFreeSpinCount = freeSpinsCount;

    //     StopCoroutine(FreeSpinRoutine);
    //     FreeSpinRoutine = null;
    //     IsFreeSpin = false; // pause only
    //   }

    //   yield return new WaitForSeconds(.5f);
    //   // Only Bonus is awarded without Free Spins, directly trigger bonus round
    //   uiManager.BonusCoroutine = StartCoroutine(uiManager.MidGameImageAnimation(BonusImageAnimation));
    //   yield return new WaitUntil(() => uiManager.animationFinish);



    //   staticSymbolController.TurnOnIndices(GenerateFreezedLocations());
    //   yield return new WaitForSeconds(.5f);
    //   _bonusManager.StartBonus(SocketManager.resultData.payload.linkRespinsRemaining);
    //   IsSpinning = false;
    //   yield break;
    // }


    if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
    {
      winningsDisplayed = true;
      CheckPopups = true;
      WinningsTextAnimation();
      CheckWinPopups();

      yield return new WaitUntil(() => !CheckPopups);
      yield return new WaitForSeconds(.5f);
    }

    if (SocketManager.resultData.payload.winAmount <= 0)
    {
      audioController.PlayWLAudio("lose");
    }

    // // Post-bonus and free spins cleanup
    if (!IsFreeSpin)
    {
      ToggleButtonGrp(true);
    }

    if (freeSpinsCount <= 0 && SocketManager.resultData.payload.freeSpinsRemaining <= 0)
    {
      CloseFreeSpinsUI();
    }
    IsSpinning = false;
  }
  #endregion
  public void OnLinkFeatureCompleted()
  {
    if (resumeFreeSpinAfterLink && savedFreeSpinCount > 0)
    {
      resumeFreeSpinAfterLink = false;
      freeSpinsCount = savedFreeSpinCount;
      IsFreeSpin = true;

      OpenFreeSpinsUI();
      FreeSpin(freeSpinsCount);
      return;
    }

    ToggleButtonGrp(true);
  }
  private void switchTopUI(bool trigger)
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

  private IEnumerator ResetUI()
  {
    if (TotalWin_text)
    {
      yield return new WaitForSeconds(.5f);
      TotalWin_text.text = "0.000";
    }
    if (IsAutoSpin)
    {
      WasAutoSpinOn = true;
      StopAutoSpin();
    }
    StopGameAnimation();
  }

  private IEnumerator StartSpecialSymbolAnimations()
  {
    List<ImageAnimation> imageAnimations = new();
    for (int i = 0; i < ResultMatrix.Count; i++)
    {
      for (int j = 0; j < ResultMatrix[i].slotImages.Count; j++)                   // v2#################
      {
        if (SocketManager.resultData.matrix[i][j] == "11" ||
        SocketManager.resultData.matrix[i][j] == "12" ||
        SocketManager.resultData.matrix[i][j] == "17" ||
        SocketManager.resultData.matrix[i][j] == "14" ||
        SocketManager.resultData.matrix[i][j] == "15" ||
        SocketManager.resultData.matrix[i][j] == "16")
        {
          imageAnimations.Add(ResultMatrix[i].slotImages[j].GetComponent<ImageAnimation>());
          ResultMatrix[i].slotImages[j].GetComponent<ImageAnimation>().StartAnimation();
        }
      }
    }
    if (imageAnimations.Count > 0)
    {
      yield return new WaitUntil(() => imageAnimations[^1].textureArray[^1] == imageAnimations[^1].rendererDelegate.sprite);
      foreach (ImageAnimation animation in imageAnimations)
      {
        animation.StopAnimation();
      }
      yield return new WaitForSeconds(.2f);
    }

  }

  private List<List<int>> GenerateFreezedLocations()
  {
    List<List<int>> loc = new();
    // foreach (var coin in SocketManager.resultData.payload.coinPositions)
    // {
    //   List<int> rXc = new() { coin.position[0], coin.position[1] };
    //   loc.Add(rXc);
    // }

    for (int i = 0; i < ResultMatrix.Count; i++)
    {
      for (int j = 0; j < ResultMatrix[i].slotImages.Count; j++)
      {
        if (SocketManager.resultData.matrix[i][j] == "11" ||
            SocketManager.resultData.matrix[i][j] == "12" ||
            SocketManager.resultData.matrix[i][j] == "14")
        {
          List<int> rXc = new() { i, j };
          loc.Add(rXc);
        }
      }
    }

    // for (int i = 0; i < ResultMatrix.Count; i++)
    // {
    //   for (int j = 0; j < ResultMatrix[i].slotImages.Count; j++)
    //   {
    //     if (ResultMatrix[i].slotImages[j].sprite == SlotSymbols[11] ||
    //         ResultMatrix[i].slotImages[j].sprite == SlotSymbols[12] ||
    //         ResultMatrix[i].slotImages[j].sprite == SlotSymbols[14])
    //     {
    //       List<int> rXc = new() { i, j };
    //       loc.Add(rXc);
    //     }
    //   }
    // }
    return loc;
  }

  private IEnumerator PlayJackpotAnimation(RectTransform RectTransform)
  {
    Transform tempParent = RectTransform.parent;
    Vector3 tempPosition = RectTransform.localPosition;
    int TempSiblingIndex = RectTransform.GetSiblingIndex();
    Vector2 tempAnchorMin = RectTransform.anchorMin;
    Vector2 tempAnchorMax = RectTransform.anchorMax;

    RectTransform.SetParent(AnimationParent);
    Vector3 tempWorldPosi = RectTransform.position;
    RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    RectTransform.position = tempWorldPosi;
    RectTransform.DOLocalMove(Vector3.zero, 1.5f);
    yield return RectTransform.DOScale(2.5f, 1.5f).WaitForCompletion();

    yield return new WaitForSeconds(1f);
    Transform jackpotSlotTransform = RectTransform.GetChild(6);
    Image ResultImage = jackpotSlotTransform.GetChild(7).GetComponent<Image>();
    jackpotSlotTransform.GetComponent<CanvasGroup>().DOFade(1, 0.8f);

    jackpotSlotTransform.localPosition = new Vector2(jackpotSlotTransform.localPosition.x, 151f);
    audioController.PlaySpinButtonAudio();
    Tween JackpotTween = jackpotSlotTransform.DOLocalMoveY(-151f, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
    JackpotTween.Play();
    yield return new WaitForSeconds(2f);
    bool found = false;
    // for (int i = 0; i < SocketManager.initialData.features.jackpot.payout.Count; i++)                     // V2 Binding
    // {
    //   if (SocketManager.initialData.Jackpot[i] * currentLineBet == SocketManager.resultData.jackpot.payout && !found)
    //   {
    //     found = true;
    //     ResultImage.sprite = JackpotSlotSymbols[i];
    //   }
    // }
    if (!found)
    {
      Debug.Log("Error while finding payout");
    }
    bool IsFin = false;
    JackpotTween.OnStepComplete(() => { IsFin = true; });
    yield return new WaitUntil(() => IsFin);

    JackpotTween.Kill();
    yield return jackpotSlotTransform.DOLocalMoveY(65.86f, .3f).SetEase(Ease.OutQuad).WaitForCompletion();
    audioController.PlayWLAudio("megaWin");
    yield return new WaitForSeconds(1f);

    JackpotWinnings();
    // yield return uiManager.MidGameImageAnimation(YouWinImageAnimation, SocketManager.resultData.jackpot.payout);

    yield return new WaitForSeconds(1f);

    yield return jackpotSlotTransform.GetComponent<CanvasGroup>().DOFade(0, 0.5f).WaitForCompletion();
    yield return new WaitForSeconds(1f);
    yield return RectTransform.GetComponent<Image>().DOFade(0, 1f).WaitForCompletion();
    // RectTransform.GetComponent<Mask>().showMaskGraphic = true;
    RectTransform.DOScale(1f, 0f);
    RectTransform.SetParent(tempParent);
    RectTransform.DOLocalMove(tempPosition, 0f);
    RectTransform.SetSiblingIndex(TempSiblingIndex);
    RectTransform.anchorMin = tempAnchorMin;
    RectTransform.anchorMax = tempAnchorMax;
    RectTransform.position = tempWorldPosi;
    RectTransform.GetComponent<Image>().DOFade(1, 1f);
  }

  private void JackpotWinnings()
  {
    // double start = 0;                                                          //V2##########################
    // DOTween.To(() => start, (val) => start = val, SocketManager.resultData.jackpot.payout, 0.5f).OnUpdate(() =>
    // {
    //   TotalWin_text.text = start.ToString("F3");
    // });
  }

  private void PopulateResultMatrix()
  {
    int CCcount = 0;
    for (int j = 0; j < 3; j++) // Update slot images based on the results          //V2###########
    {

      List<int> resultnum = new List<int>();
      for (int k = 0; k < 5; k++)
      {
        int resultNum = int.Parse(SocketManager.resultData.matrix[j][k]);
        resultnum.Add(resultNum);
      }


      for (int i = 0; i < 5; i++) //Loop through each column
      {
        if (ResultMatrix[j].slotImages[i]) //Checking if the required object is not null
        {
          Image SlotImage = ResultMatrix[j].slotImages[i];
          //  Debug.Log("i" + i + ".   " + resultnum[i]);
          if (resultnum[i] == 9) // BLANK SYMBOL - NEEDS RANDOM SYMBOL
          {
            // Generate random symbol (0-8) excluding special symbols
            int randomSymbolIndex = UnityEngine.Random.Range(0, 9); // 0 to 8 inclusive
            SlotImage.sprite = SlotSymbols[randomSymbolIndex];
            PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), randomSymbolIndex);
            continue; // Skip the rest of the checks for this symbol
          }
          if (resultnum[i] == 17) // LP coin (LosPollos)
          {
            bool found = false;

            foreach (var coin in SocketManager.resultData.payload.coinPositions)
            {
              // Only LosPollos coins
              if (coin.symbolId == 17 &&
                  coin.position[0] == j &&
                  coin.position[1] == i)
              {
                SlotImage.sprite = losPollosSprites[coin.coinValue];
                PopulateAnimationSprites(
                    SlotImage.gameObject.GetComponent<ImageAnimation>(),
                    17,
                    coin.coinValue
                );
                found = true;
                break;
              }
            }

            // Fallback (same behavior you had earlier)
            if (!found)
            {
              int[] tempIndex = { 2, 3, 4, 5, 7 };
              int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];

              SlotImage.sprite = losPollosSprites[randomIndex];
              PopulateAnimationSprites(
                  SlotImage.gameObject.GetComponent<ImageAnimation>(),
                  17,
                  randomIndex
              );
            }
          }


          else if (resultnum[i] == 15) //gold coin
          {
            foreach (var coins in SocketManager.resultData.payload.coinPositions)
            {
              if (coins.position[0] == j && coins.position[1] == i)
              {
                SlotImage.sprite = SlotSymbols[resultnum[i]];
                PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i], 0, coins.coinValue.ToString() + "x");
                break;
              }
            }
          }
          else
          {
            if (resultnum[i] == 14)
            {
              CCcount++;
            }
            SlotImage.sprite = SlotSymbols[resultnum[i]];
            PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
          }
        }
      }
    }

    if (!IsTurboOn && !IsAutoSpin)
    {
      if (CCcount != 0)
      {
        ReorderImages();
      }
    }
  }

  internal void CheckWinPopups()
  {
    if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 5 && SocketManager.resultData.payload.winAmount < currentTotalBet * 10)
    {
      uiManager.PopulateWin(1);
    }
    else if (SocketManager.resultData.payload.winAmount >= currentTotalBet * 10)
    {
      uiManager.PopulateWin(2);
    }
    else
    {
      CheckPopups = false;
    }
  }

  private IEnumerator FreeSpinsSymbolAnimation()
  {
    yield return new WaitForSeconds(1.5f);
    for (int i = 0; i < SocketManager.resultData.payload.coinPositions.Count; i++)             //V2###############
    {
      Debug.Log("Looping throght lp");
      if (SocketManager.resultData.payload.coinPositions[i].symbolId == 17)
      {
        CoinPosition lp = SocketManager.resultData.payload.coinPositions[i];
        if (lp != null && ResultMatrix[lp.position[0]].slotImages[lp.position[1]] != null)
        {
          Debug.Log("Found lp on result matrix");
          Image LosPollosImage = ResultMatrix[lp.position[0]].slotImages[lp.position[1]];
          RectTransform freeSpinNumberTransform = LosPollosImage.transform.GetChild(4).GetComponent<RectTransform>();
          freeSpinNumberTransform.GetComponent<Image>().sprite = losPollosNumberSprites[lp.coinValue];
          freeSpinNumberTransform.gameObject.SetActive(true);
          LosPollosImage.sprite = losPollosNoNumberSprite;

          Vector3 tempPosi = freeSpinNumberTransform.localPosition;
          Transform tempParent = freeSpinNumberTransform.parent;
          int TempSiblingIndex = freeSpinNumberTransform.GetSiblingIndex();

          freeSpinNumberTransform.SetParent(AnimationParent);

          bool scale = false;
          yield return freeSpinNumberTransform.DOLocalMove(FreeSpinCountUIPositon.localPosition, .4f).SetEase(Ease.Linear).OnUpdate(() =>
          {
            if (Vector3.Distance(freeSpinNumberTransform.localPosition, FreeSpinCountUIPositon.localPosition) < 100f && !scale)
            {
              scale = true;
              freeSpinNumberTransform.DOScale(0, 0.2f);
            }
          }).WaitForCompletion();
          freeSpinNumberTransform.gameObject.SetActive(false);

          if (int.TryParse(FSnum_text.text, out int currFScount))
          {
            currFScount += lp.coinValue;
            FSnum_text.text = currFScount.ToString();
          }
          else
          {
            Debug.Log("Error while FS int conversion");
          }

          freeSpinNumberTransform.localPosition = tempPosi;
          freeSpinNumberTransform.SetParent(tempParent);
          freeSpinNumberTransform.SetSiblingIndex(TempSiblingIndex);
          freeSpinNumberTransform.DOScale(1, 0f);
          yield return new WaitForSeconds(1f);
        }
      }
    }
  }

  internal void OpenFreeSpinsUI()
  {
    FSnum_text.text = freeSpinsCount.ToString();
    FreeSpinsUI_Panel.DOFade(1, 0.3f);
    if (LinesUI.alpha != 0) LinesUI.DOFade(0, 0.3f).OnComplete(() => { LinesUI.interactable = false; LinesUI.blocksRaycasts = false; });
    if (TotalBetUI.alpha != 0) TotalBetUI.DOFade(0, 0.3f).OnComplete(() => { TotalBetUI.interactable = false; TotalBetUI.blocksRaycasts = false; });
    if (LineBetUI.alpha != 0) LineBetUI.DOFade(0, 0.3f).OnComplete(() => { LineBetUI.interactable = false; LineBetUI.blocksRaycasts = false; });
  }

  internal void CloseFreeSpinsUI()
  {
    IsFreeSpin = false;
    FreeSpinsUI_Panel.DOFade(0, 0.3f);
    FSnum_text.text = "0";
    LinesUI.DOFade(1, 0.3f).OnComplete(() => { LinesUI.interactable = true; LinesUI.blocksRaycasts = true; });
    TotalBetUI.DOFade(1, 0.3f).OnComplete(() => { TotalBetUI.interactable = true; TotalBetUI.blocksRaycasts = true; });
    LineBetUI.DOFade(1, 0.3f).OnComplete(() => { LineBetUI.interactable = true; LineBetUI.blocksRaycasts = true; });
  }

  internal void WinningsTextAnimation()
  {
    double winAmt = SocketManager.resultData.payload.winAmount;
    Debug.Log("Text Animation Value:" + SocketManager.resultData.payload.winAmount);
    if (!double.TryParse(Balance_text.text, out double currentBal))
    {
      Debug.Log("Error while converting string to double in current balance: " + Balance_text.text);
    }
    if (!double.TryParse(SocketManager.playerdata.balance.ToString("f3"), out double Balance))
    {
      Debug.Log("Error while converting string to double in new balance: " + SocketManager.playerdata.balance.ToString("F3"));
    }
    if (!double.TryParse(TotalWin_text.text, out double currentWin))
    {
      Debug.Log("Error while converting string to double in Totalwin:" + TotalWin_text.text);
    }
    DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
    {
      if (TotalWin_text) TotalWin_text.text = currentWin.ToString("f3");
    });
    BalanceTween?.Kill();
    DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
    {
      if (Balance_text) Balance_text.text = currentBal.ToString("f3");
    });
  }

  private void BalanceDeduction()
  {
    double bet = 0;
    double balance = 0;
    try
    {
      bet = double.Parse(TotalBet_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      balance = double.Parse(Balance_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }
    double initAmount = balance;

    balance = balance - bet;

    BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
    {
      if (Balance_text) Balance_text.text = initAmount.ToString("f3");
    });
  }


  private void CheckPayoutLineBackend(List<LineWin> lineWins, double jackpot = 0)
  {
    if (lineWins == null || lineWins.Count == 0)
    {
      if (audioController) audioController.StopWLAaudio();
      CheckSpinAudio = false;
      return;
    }

    TurnOnBlackBoxes();

    // Play correct audio
    if (audioController)
    {
      audioController.PlayWLAudio(jackpot > 0 ? "megaWin" : "win");
    }

    // Jackpot = animate everything
    if (jackpot > 0)
    {
      for (int col = 0; col < Tempimages.Count; col++)
      {
        for (int row = 0; row < Tempimages[col].slotImages.Count; row++)
        {
          StartGameAnimation(Tempimages[col].slotImages[row].gameObject);
        }
      }


    }
    else
    {
      // Animate symbols for all winning lines ONCE
      foreach (var win in lineWins)
      {
        int lineId = win.lineIndex;

        foreach (int col in win.positions)   // 🔥 ONLY winning columns
        {
          int row = win.pattern[col];      // row for that column

          StartGameAnimation(
              Tempimages[col].slotImages[row].gameObject
          );
        }
      }

      // Start looping line animation (same as old game)
      if (LineAnimRoutine != null)
        StopCoroutine(LineAnimRoutine);

      LineAnimRoutine = StartCoroutine(LineAnimationRoutine(lineWins));
    }


    CheckSpinAudio = false;
  }
  private IEnumerator LineAnimationRoutine(List<LineWin> lineWins)
  {
    WinAnimationFin = false;

    yield return new WaitForSeconds(2f);

    while (true)
    {
      foreach (var win in lineWins)
      {
        foreach (int col in win.positions)
        {
          int row = win.pattern[col];
          var img = Tempimages[col].slotImages[row];

          var anim = img.GetComponent<ImageAnimation>();
          if (anim != null && anim.isAnim)
          {
            img.transform.GetChild(2)
                .GetComponent<Image>()
                .DOFade(0f, 0.2f);

            anim.StartAnimation();
          }
        }

        yield return new WaitForSeconds(2f);
        TurnOnBlackBoxes();
        yield return new WaitForSeconds(1f);
      }

      WinAnimationFin = true;
    }
  }



  private void TurnOnBlackBoxes()
  {
    for (int i = 0; i < Tempimages.Count; i++)
    {
      for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
      {
        Tempimages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().DOFade(0.85f, 0.2f);
        Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().StopAnimation();
      }
    }
  }

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }

  internal void ToggleButtonGrp(bool toggle)
  {
    // if (SlotStart_Button && !IsAutoSpin && !SlotStart_Button.gameObject.activeInHierarchy) SlotStart_Button.gameObject.SetActive(toggle);
    if (SlotStart_Button && !IsAutoSpin) SlotStart_Button.interactable = toggle;
    if (AutoSpin_Button && !IsAutoSpin && !IsFreeSpin) AutoSpin_Button.gameObject.SetActive(toggle);
    if (AutoSpin_Button && !IsAutoSpin) AutoSpin_Button.interactable = toggle;
    if (LineBetPlus_Button && !IsAutoSpin) LineBetPlus_Button.interactable = toggle;
    if (LineBetMinus_Button && !IsAutoSpin) LineBetMinus_Button.interactable = toggle;
    if (TotalBetPlus_Button && !IsAutoSpin) TotalBetPlus_Button.interactable = toggle;
    if (TotalBetMinus_Button && !IsAutoSpin) TotalBetMinus_Button.interactable = toggle;
  }

  //Start the icons animation
  private void StartGameAnimation(GameObject animObjects)
  {
    ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
    if (temp.textureArray.Count > 0)
    {
      temp.StartAnimation();
      temp.isAnim = true;
      animObjects.transform.GetChild(2).gameObject.GetComponent<Image>().DOFade(0f, 0.2f);
    }
  }

  //Stop the icons animation
  internal void StopGameAnimation()
  {
    if (changedSlots.Count > 0)
    {
      ResetImages();
    }

    if (LineAnimRoutine != null)
    {
      StopCoroutine(LineAnimRoutine);
      LineAnimRoutine = null;
      WinAnimationFin = true;
    }

    for (int i = 0; i < Tempimages.Count; i++)
    {
      for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
      {
        Tempimages[i].slotImages[j].transform.GetChild(2).gameObject.GetComponent<Image>().DOFade(0f, 0.2f);
        Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().StopAnimation();
        Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().isAnim = false;
      }
    }
  }

  #region TweeningCode
  private void InitializeTweening(Transform slotTransform, bool bonus = false)
  {
    Tweener tweener = null;

    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, slotTransform.localPosition.y + 440);
    tweener = slotTransform.DOLocalMoveY(-1436, .6f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);

    tweener.Play();
    alltweens.Add(tweener);
  }

  private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool magnet = false, int CCloc = 0, bool isStop = false)
  {
    if (!isStop)
    {
      bool IsRegister = false;
      yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });
      yield return new WaitUntil(() => IsRegister);
    }

    alltweens[index].Pause();
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 1436);
    int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
    if (!magnet)
    {
      alltweens[index] = slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.6f).SetEase(Ease.OutQuad); //1789
      if (audioController) audioController.PlayWLAudio("spinStop");
      // yield return alltweens[index].WaitForCompletion();
      // alltweens[index].Kill();
    }
    else
    {
      ImageAnimation anim = null;
      if (slotTransform.name == "Slot")
      {
        anim = LeftMagnetImageAnimation;
      }
      else if (slotTransform.name == "Slot (4)")
      {
        anim = RightMagnetImageAnimation;
      }
      anim.rendererDelegate.sprite = MagnetInSprites[0];
      anim.gameObject.SetActive(true);

      if (CCloc == 0)
      {
        alltweens[index] = slotTransform.DOLocalMoveY(1547.255f, 0.6f).SetEase(Ease.OutQuad);
      }
      else if (CCloc == 1)
      {
        alltweens[index] = slotTransform.DOLocalMoveY(1768.255f, 0.6f).SetEase(Ease.OutQuad);
      }
      else
      {
        Debug.Log("wrong cc loc1");
      }
      if (audioController) audioController.PlayWLAudio("spinStop");
      yield return alltweens[index].WaitForCompletion();
      alltweens[index].Kill();

      ClearAnimtionArray(anim);
      foreach (Sprite sprite in MagnetInSprites)
      {
        anim.textureArray.Add(sprite);
      }
      anim.doLoopAnimation = false;
      anim.AnimationSpeed = 8;
      anim.StartAnimation();
      yield return new WaitUntil(() => anim.textureArray[^1] == anim.rendererDelegate.sprite);
      yield return new WaitForSeconds(1f);
      ClearAnimtionArray(anim);
      foreach (Sprite sprite in MagnetLightening_Sprites)
      {
        anim.textureArray.Add(sprite);
      }
      anim.AnimationSpeed = 17;
      anim.StopAnimation();
      audioController.PlayWLAudio("Electric");
      anim.StartAnimation();
      yield return new WaitUntil(() => anim.textureArray[^5] == anim.rendererDelegate.sprite);
      if (audioController) audioController.PlayWLAudio("spinStop");
      slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.6f).SetEase(Ease.OutCubic);
      StartCoroutine(CloseMagnet(anim));
      yield return new WaitForSeconds(0.5f);
    }
  }

  private IEnumerator CloseMagnet(ImageAnimation anim)
  {
    yield return new WaitUntil(() => anim.textureArray[^1] == anim.rendererDelegate.sprite);
    ClearAnimtionArray(anim);
    for (int i = MagnetInSprites.Length - 1; i >= 0; i--)
    {
      anim.textureArray.Add(MagnetInSprites[i]);
    }
    anim.AnimationSpeed = 8;
    anim.StopAnimation();
    anim.StartAnimation();
    yield return new WaitUntil(() => anim.textureArray[^1] == anim.rendererDelegate.sprite);
    anim.gameObject.SetActive(false);
    anim.StopAnimation();
    anim.rendererDelegate.sprite = MagnetInSprites[0];
  }

  private void ClearAnimtionArray(ImageAnimation imageAnimation)
  {
    imageAnimation.textureArray.Clear();
    imageAnimation.textureArray.TrimExcess();
  }

  private void KillAllTweens()
  {
    if (alltweens.Count > 0)
    {
      for (int i = 0; i < alltweens.Count; i++)
      {
        alltweens[i].Kill();
      }
      alltweens.Clear();
    }
  }
  #endregion

}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}

