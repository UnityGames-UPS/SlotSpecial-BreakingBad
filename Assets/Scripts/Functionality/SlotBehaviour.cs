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
  internal List<SlotImage> Tempimages;     //class to store the result matrix
  internal List<SlotImage> ResultMatrix;

  [Header("Spin Settings")]
  [SerializeField] private float anticipationUpDistance = 30f;
  [SerializeField] private float anticipationUpDuration = 0.15f;
  [SerializeField] private float dropDownDistance       = 15f;
  [SerializeField] private float dropDownDuration       = 0.12f;
  [SerializeField] private float settleBounceDuration   = 0.18f;
  [SerializeField] private float stopOvershootDistance  = 50f;
  [SerializeField] private float stopOvershootDuration  = 0.15f;
  [SerializeField] private float stopBounceBackDistance = 15f;
  [SerializeField] private float stopBounceBackDuration = 0.25f;
  [SerializeField] private float stopSettleDuration     = 0.35f;
  [SerializeField] private float quickStopOvershoot     = 20f;
  [SerializeField] private float quickStopDuration      = 0.20f;
  [SerializeField] private float spinCycleDuration      = 0.05f;
  [SerializeField] private float symbolHeight           = 100f;

  private float[] initialYPositions;

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

  private List<Tween> alltweens = new List<Tween>();
  private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

  private bool IsAutoSpin = false;
  private bool IsFreeSpin = false;
  internal bool IsBonus = false;
  private bool WinAnimationFin = true;
  private bool IsSpinning = false;
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
    if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

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

    initialYPositions = new float[numberOfSlots];
    for (int i = 0; i < numberOfSlots; i++)
    {
      initialYPositions[i] = Slot_Transform[i].localPosition.y;
    }

    Tempimages = new List<SlotImage>();
    for (int col = 0; col < numberOfSlots; col++)
    {
      SlotImage colSlotImage = new SlotImage();
      colSlotImage.slotImages = new List<Image>();
      for (int row = 0; row < 3; row++)
      {
        colSlotImage.slotImages.Add(images[col].slotImages[2 + row]);
      }
      Tempimages.Add(colSlotImage);
    }

    ResultMatrix = new List<SlotImage>();
    for (int row = 0; row < 3; row++)
    {
      SlotImage rowSlotImage = new SlotImage();
      rowSlotImage.slotImages = new List<Image>();
      for (int col = 0; col < numberOfSlots; col++)
      {
        rowSlotImage.slotImages.Add(images[col].slotImages[2 + row]);
      }
      ResultMatrix.Add(rowSlotImage);
    }
  }
  void TurboToggle()
  {
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



  //function to populate animation sprites accordingly
  private void PopulateAnimationSprites(ImageAnimation animScript, int val, int LP = 0, string coin = null)
  {
    if (animScript == null) return;
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

  /// <summary>
  /// Plays the win-landing animation for all special symbols in the result matrix (5x3 grid),
  /// then stops every animation once each has completed its first full loop.
  ///
  /// Production pattern: uses onLoopComplete callback instead of polling
  /// rendererDelegate.sprite — reliable across all 17 icon types with
  /// different frame counts and animation speeds.
  ///
  /// Special symbol IDs: 11 (Link), 12 (MegaLink), 14 (CashCollect),
  ///                     15 (GoldCoin), 16 (Diamond), 17 (LosPollos)
  /// </summary>
  private IEnumerator StartSpecialSymbolAnimations()
  {
    System.Func<string, bool> isSpecial = id =>
        id == "11" || id == "12" || id == "14" ||
        id == "15" || id == "16" || id == "17";

    List<ImageAnimation> activeAnims = new();

    // Collect all special-symbol slots in the 5-column x 3-row result matrix
    for (int i = 0; i < ResultMatrix.Count; i++)
    {
      for (int j = 0; j < ResultMatrix[i].slotImages.Count; j++)
      {
        if (!isSpecial(SocketManager.resultData.matrix[i][j])) continue;

        ImageAnimation anim = ResultMatrix[i].slotImages[j].GetComponent<ImageAnimation>();
        if (anim == null || anim.textureArray == null || anim.textureArray.Count == 0) continue;

        // Reset cleanly: stop any previous run, clear stale callback
        anim.StopAnimation();
        anim.onLoopComplete = null;

        // One-shot: play through the sprite sheet once then hold on last frame
        anim.doLoopAnimation = false;

        activeAnims.Add(anim);
        anim.StartAnimation();
      }
    }

    if (activeAnims.Count == 0) yield break;

    // Use onLoopComplete callbacks to know when every animation finishes
    int completedCount = 0;
    bool allDone = false;

    foreach (ImageAnimation anim in activeAnims)
    {
      ImageAnimation captured = anim; // capture for lambda
      captured.onLoopComplete = (_) =>
      {
        completedCount++;
        if (completedCount >= activeAnims.Count)
          allDone = true;
      };
    }

    // Safety timeout: 5 seconds covers any reasonable animation length
    float timeout = 5f;
    float elapsed = 0f;
    while (!allDone && elapsed < timeout)
    {
      elapsed += Time.deltaTime;
      yield return null;
    }

    // Clean up: clear callbacks and stop all animations
    foreach (ImageAnimation anim in activeAnims)
    {
      anim.onLoopComplete = null;
      anim.StopAnimation();
    }

    yield return new WaitForSeconds(0.2f);
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

  private void PopulateResultMatrixForColumn(int col)
  {
    int CCcount = 0;
    for (int row = 0; row < 3; row++)
    {
      int resultNum = int.Parse(SocketManager.resultData.matrix[row][col]);
      if (Tempimages[col].slotImages[row])
      {
        Image SlotImage = Tempimages[col].slotImages[row];
        if (resultNum == 9) // BLANK SYMBOL - NEEDS RANDOM SYMBOL
        {
          int randomSymbolIndex = UnityEngine.Random.Range(0, 9); // 0 to 8 inclusive
          SlotImage.sprite = SlotSymbols[randomSymbolIndex];
          PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), randomSymbolIndex);
          continue;
        }
        if (resultNum == 17) // LP coin (LosPollos)
        {
          bool found = false;
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
            if (coin.symbolId == 17 && coin.position[0] == row && coin.position[1] == col)
            {
              SlotImage.sprite = losPollosSprites[coin.coinValue];
              PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), 17, coin.coinValue);
              found = true;
              break;
            }
          }
          if (!found)
          {
            int[] tempIndex = { 2, 3, 4, 5, 7 };
            int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
            SlotImage.sprite = losPollosSprites[randomIndex];
            PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), 17, randomIndex);
          }
        }
        else if (resultNum == 15) // gold coin
        {
          bool found = false;
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
            if (coin.position[0] == row && coin.position[1] == col)
            {
              SlotImage.sprite = SlotSymbols[resultNum];
              PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultNum, 0, coin.coinValue.ToString() + "x");
              found = true;
              break;
            }
          }
          if (!found)
          {
            SlotImage.sprite = SlotSymbols[resultNum];
            PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultNum);
          }
        }
        else
        {
          if (resultNum == 14)
          {
            CCcount++;
          }
          SlotImage.sprite = SlotSymbols[resultNum];
          PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultNum);
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

  private void PopulateResultMatrix()
  {
    for (int col = 0; col < numberOfSlots; col++)
    {
      PopulateResultMatrixForColumn(col);
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
      return;
    }

    TurnOnBlackBoxes();



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


  }
  /// <summary>
  /// Cycles through each winning payline, highlighting its symbols one line at a time.
  /// Production pattern:
  ///   - Black-boxes all non-highlighted symbols (dim overlay)
  ///   - Loops indefinitely until StopGameAnimation cancels it
  ///   - onLoopComplete is cleared on each stop to prevent stale callbacks
  /// </summary>
  private IEnumerator LineAnimationRoutine(List<LineWin> lineWins)
  {
    WinAnimationFin = false;

    yield return new WaitForSeconds(2f);

    while (true)
    {
      foreach (var win in lineWins)
      {
        // Highlight winning symbols for this line
        foreach (int col in win.positions)
        {
          int row = win.pattern[col];
          var img = Tempimages[col].slotImages[row];

          var anim = img.GetComponent<ImageAnimation>();
          if (anim != null && anim.isAnim)
          {
            var overlay = img.transform.GetChild(2).GetComponent<Image>();
            if (overlay != null) overlay.DOFade(0f, 0.2f);

            // Win-highlight: single-phase loop mode so it keeps playing
            anim.doLoopAnimation = true;
            anim.onLoopComplete = null;
            anim.StopAnimation();
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
        var overlay = Tempimages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>();
        if (overlay != null) overlay.DOFade(0.85f, 0.2f);

        ImageAnimation anim = Tempimages[i].slotImages[j].GetComponent<ImageAnimation>();
        if (anim != null)
        {
          anim.onLoopComplete = null;  // Clear before stopping
          anim.StopAnimation();
        }
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
  // Start the win-highlight animation on a slot icon.
  // Win-highlight: continuous loop until StopGameAnimation is called
  // until StopGameAnimation is called.
  private void StartGameAnimation(GameObject animObjects)
  {
    ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
    if (temp == null || temp.textureArray == null || temp.textureArray.Count == 0) return;

    // Win-highlight: single-phase, continuous loop until manually stopped
    temp.doLoopAnimation = true;
    temp.onLoopComplete = null;

    temp.StopAnimation();   // Reset state cleanly before restarting
    temp.StartAnimation();
    temp.isAnim = true;

    var overlay = animObjects.transform.GetChild(2).gameObject.GetComponent<Image>();
    if (overlay != null) overlay.DOFade(0f, 0.2f);
  }

  // Stop all win-highlight animations, cancel the line-animation coroutine,
  // and clear all onLoopComplete callbacks across the entire 5x3 grid.
  internal void StopGameAnimation()
  {
    if (changedSlots.Count > 0)
      ResetImages();

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
        var overlay = Tempimages[i].slotImages[j].transform.GetChild(2).gameObject.GetComponent<Image>();
        if (overlay != null) overlay.DOFade(0f, 0.2f);

        ImageAnimation anim = Tempimages[i].slotImages[j].GetComponent<ImageAnimation>();
        if (anim != null)
        {
          anim.onLoopComplete = null;   // Always clear callbacks before stopping
          anim.StopAnimation();
          anim.isAnim = false;
        }
      }
    }
  }

  #region TweeningCode
  private void InitializeTweening(Transform slotTransform, bool bonus = false)
  {
    int index = System.Array.IndexOf(Slot_Transform, slotTransform);
    if (index < 0) return;

    float middleY = initialYPositions[index];

    // Start Bounce sequence (overshoot up -> drop down -> settle bounce)
    Sequence seq = DOTween.Sequence();
    seq.Append(slotTransform.DOLocalMoveY(middleY + anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutCubic));
    seq.Append(slotTransform.DOLocalMoveY(middleY - dropDownDistance, dropDownDuration).SetEase(Ease.InCubic));
    seq.Append(slotTransform.DOLocalMoveY(middleY, settleBounceDuration).SetEase(Ease.OutBounce));
    seq.OnComplete(() =>
    {
      RunContinuousCycle(index, slotTransform, middleY);
    });

    alltweens.Add(seq);
  }

  private void RunContinuousCycle(int col, Transform slotTransform, float middleY)
  {
    if (!IsSpinning) return;

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middleY, 0f);

    Tween cycle = slotTransform.DOLocalMoveY(middleY - symbolHeight, spinCycleDuration)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
          if (!IsSpinning) return;
          CycleBufferSymbols(col);
          RunContinuousCycle(col, slotTransform, middleY);
        });

    alltweens[col] = cycle;
  }

  private void CycleBufferSymbols(int col)
  {
    var slotImgs = images[col].slotImages;
    if (slotImgs == null || slotImgs.Count == 0) return;

    int count = slotImgs.Count;
    for (int i = count - 1; i > 0; i--)
    {
      slotImgs[i].sprite = slotImgs[i - 1].sprite;
    }
    int randomSymbolIndex = UnityEngine.Random.Range(0, SlotSymbols.Length - 8);
    slotImgs[0].sprite = SlotSymbols[randomSymbolIndex];
  }

  private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool magnet = false, int CCloc = 0, bool isStop = false)
  {
    if (alltweens[index] != null)
    {
      alltweens[index].Kill();
      alltweens[index] = null;
    }

    float middleY = initialYPositions[index];
    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middleY, 0f);

    PopulateResultMatrixForColumn(index);

    if (!magnet)
    {
      Sequence seq = DOTween.Sequence();
      if (IsTurboOn || StopSpinToggle)
      {
        seq.Append(slotTransform.DOLocalMoveY(middleY - quickStopOvershoot, quickStopDuration * 0.3f).SetEase(Ease.InCubic));
        seq.Append(slotTransform.DOLocalMoveY(middleY, quickStopDuration * 0.7f).SetEase(Ease.OutBack, 1.2f));
      }
      else
      {
        seq.Append(slotTransform.DOLocalMoveY(middleY - stopOvershootDistance, stopOvershootDuration).SetEase(Ease.InCubic));
        seq.Append(slotTransform.DOLocalMoveY(middleY + stopBounceBackDistance, stopBounceBackDuration).SetEase(Ease.OutCubic));
        seq.Append(slotTransform.DOLocalMoveY(middleY, stopSettleDuration).SetEase(Ease.OutBounce));
      }
      alltweens[index] = seq;
      seq.Play();



      float stagger = (IsTurboOn || StopSpinToggle) ? 0.05f : 0.12f;
      if (index < numberOfSlots - 1)
      {
        yield return new WaitForSeconds(stagger);
      }
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
      if (anim != null)
      {
        anim.rendererDelegate.sprite = MagnetInSprites[0];
        anim.gameObject.SetActive(true);
      }

      if (CCloc == 0)
      {
        yield return slotTransform.DOLocalMoveY(1547.255f, 0.6f).SetEase(Ease.OutQuad).WaitForCompletion();
      }
      else if (CCloc == 1)
      {
        yield return slotTransform.DOLocalMoveY(1768.255f, 0.6f).SetEase(Ease.OutQuad).WaitForCompletion();
      }



      if (anim != null)
      {
        ClearAnimtionArray(anim);
        foreach (Sprite sprite in MagnetInSprites)
        {
          anim.textureArray.Add(sprite);
        }
        anim.doLoopAnimation = false;
        anim.onLoopComplete = null;
        anim.AnimationSpeed = 8;
        anim.StopAnimation();

        bool magnetInDone = false;
        anim.onLoopComplete = (_) => { magnetInDone = true; };
        anim.StartAnimation();
        yield return new WaitUntil(() => magnetInDone);
        anim.onLoopComplete = null;

        yield return new WaitForSeconds(1f);
        ClearAnimtionArray(anim);
        foreach (Sprite sprite in MagnetLightening_Sprites)
        {
          anim.textureArray.Add(sprite);
        }
        anim.AnimationSpeed = 17;
        anim.StopAnimation();


        anim.StartAnimation();
        yield return new WaitUntil(() =>
            anim.textureArray.Count >= 5 &&
            anim.rendererDelegate != null &&
            anim.textureArray[^5] == anim.rendererDelegate.sprite);
      }



      int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
      Tween stopTween = slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.6f).SetEase(Ease.OutCubic);
      alltweens[index] = stopTween;
      yield return stopTween.WaitForCompletion();

      if (anim != null)
      {
        StartCoroutine(CloseMagnet(anim));
      }
      yield return new WaitForSeconds(0.5f);
    }
  }

  private IEnumerator CloseMagnet(ImageAnimation anim)
  {
    // Wait for lightning animation to finish (onLoopComplete)
    bool lightningDone = false;
    anim.onLoopComplete = (_) => { lightningDone = true; };
    yield return new WaitUntil(() => lightningDone);
    anim.onLoopComplete = null;

    // Play MagnetIn sprites in reverse (closing animation)
    ClearAnimtionArray(anim);
    for (int i = MagnetInSprites.Length - 1; i >= 0; i--)
    {
      anim.textureArray.Add(MagnetInSprites[i]);
    }
    anim.doLoopAnimation = false;
    anim.AnimationSpeed = 8;
    anim.StopAnimation();

    bool closeDone = false;
    anim.onLoopComplete = (_) => { closeDone = true; };
    anim.StartAnimation();
    yield return new WaitUntil(() => closeDone);
    anim.onLoopComplete = null;

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
        if (alltweens[i] != null)
        {
          alltweens[i].Kill();
        }
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