using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SlotManager : MonoBehaviour
{
  [Header("Script References")]
  // --- Session Runtime State variables ---
  public int BetCounter { get; private set; }
  public bool IsSpinning { get; set; }
  public bool IsAutoSpin { get; set; }
  public bool IsFreeSpin { get; set; }
  public bool IsBonus { get; set; }
  public bool IsTurboOn { get; set; }
  public bool WasAutoSpinOn { get; set; }
  public bool StopSpinToggle { get; set; }
  public bool CheckPopups { get; set; }
  public int AutoplayCount { get; private set; }
  public bool AutoplayUntilFeature { get; private set; }

  public InitData InitialData { get; private set; }
  public ServerSpinResponse ResultData { get; private set; }
  public ServerPlayer PlayerData { get; private set; }

  // --- Dynamic Derived Properties ---
  public double Balance => PlayerData != null ? PlayerData.balance : 0;
  
  public double LineBet => (InitialData != null && InitialData.gameData != null && BetCounter < InitialData.gameData.bets.Count) 
      ? InitialData.gameData.bets[BetCounter] : 0;
  
  public double TotalBet => LineBet * 30; // 30 paylines
  
  public int FreeSpinsCount => ResultData != null && ResultData.payload != null ? ResultData.payload.freeSpinsRemaining : 0;
  
  public int LinkRespinsRemaining => ResultData != null && ResultData.payload != null ? ResultData.payload.linkRespinsRemaining : 0;
  
  public double WinAmount => ResultData != null && ResultData.payload != null ? ResultData.payload.winAmount : 0;

  // --- Events ---
  public event Action<double> OnBalanceChanged;
  public event Action<double> OnTotalBetChanged;
  public event Action<double> OnLineBetChanged;
  public event Action<int> OnFreeSpinsChanged;
  public event Action<int> OnLinkRespinsChanged;
  public event Action OnBetChanged;
  public event Action<bool> OnSpinStateChanged;
  public event Action<bool> OnAutoSpinStateChanged;
  public event Action<int, bool> OnAutoplayCountChanged;
  public event Action OnAutoplayStopped;

  public void Initialize(InitData data)
  {
      InitialData = data;
      PlayerData = data.player;
      UpdateBalance(data.player.balance);
      SetBetIndex(0);
  }

  public void UpdateBalance(double newBalance)
  {
      if (PlayerData == null) PlayerData = new ServerPlayer();
      PlayerData.balance = newBalance;
      OnBalanceChanged?.Invoke(Balance);
  }

  public void SetBetIndex(int index)
  {
      if (InitialData == null || InitialData.gameData == null || InitialData.gameData.bets == null) return;
      
      var bets = InitialData.gameData.bets;
      if (index < 0 || index >= bets.Count) return;

      BetCounter = index;

      OnLineBetChanged?.Invoke(LineBet);
      OnTotalBetChanged?.Invoke(TotalBet);
      OnBetChanged?.Invoke();
  }

  public void SetFreeSpinsCount(int count)
  {
      if (ResultData != null && ResultData.payload != null)
      {
          ResultData.payload.freeSpinsRemaining = count;
      }
      OnFreeSpinsChanged?.Invoke(FreeSpinsCount);
  }

  public void SetLinkRespinsRemaining(int count)
  {
      if (ResultData != null && ResultData.payload != null)
      {
          ResultData.payload.linkRespinsRemaining = count;
      }
      OnLinkRespinsChanged?.Invoke(LinkRespinsRemaining);
  }

  public void UpdateFromSpinResult(ServerSpinResponse result)
  {
      ResultData = result;
      if (result.player != null)
      {
          if (PlayerData == null) PlayerData = new ServerPlayer();
          PlayerData.balance = result.player.balance ?? PlayerData.balance;
      }
      
      OnBalanceChanged?.Invoke(Balance);
      
      if (result.payload.isFreeSpinActive || result.payload.isFreeSpinTriggered)
      {
          OnFreeSpinsChanged?.Invoke(FreeSpinsCount);
      }
      if (result.payload.linkFeatureActive || result.payload.isLinkTriggered)
      {
          OnLinkRespinsChanged?.Invoke(LinkRespinsRemaining);
      }
  }

  public void TriggerSpinState(bool isSpinning)
  {
      IsSpinning = isSpinning;
      OnSpinStateChanged?.Invoke(IsSpinning);
  }

  public void TriggerAutoSpinState(bool isAutoSpin)
  {
      IsAutoSpin = isAutoSpin;
      OnAutoSpinStateChanged?.Invoke(IsAutoSpin);
  }
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private StickySymbolManager stickySymbolManager;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private BonusManager _bonusManager;
  [SerializeField] private AnimationManager animationManager;
  [SerializeField] public JackpotManager jackpotManager;

  [Header("Sprites References")]
  [SerializeField] internal Sprite[] SlotSymbols;  //images taken initially
  [SerializeField] public Sprite[] JackpotSlotSymbols;
  [SerializeField] private Sprite[] SpecialLayerSymbols; // 0: Link, 1: MegaLink, 2: CashCollect, 3: Diamond

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

  private Coroutine tweenroutine;
  private Coroutine LineAnimRoutine = null;

  int tweenHeight = 0;  //calculate the height at which tweening is done
  private int numberOfSlots = 5;          //number of columns
  [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

  private float SpinDelay = 0.2f;

  private List<List<SlotSymbolView>> symbolViews = new();

  private void Awake()
  {
      InitializeSymbolViews();
  }

  private void InitializeSymbolViews()
  {
      symbolViews.Clear();
      for (int col = 0; col < images.Count; col++)
      {
          List<SlotSymbolView> colViews = new List<SlotSymbolView>();
          for (int row = 0; row < images[col].slotImages.Count; row++)
          {
              Image img = images[col].slotImages[row];
              SlotSymbolView view = img.GetComponent<SlotSymbolView>();
              if (view == null) view = img.gameObject.AddComponent<SlotSymbolView>();
              view.SetupFromHierarchy();
              colViews.Add(view);
          }
          symbolViews.Add(colViews);
      }
  }

  public SlotSymbolView GetSymbolView(int row, int col)
  {
      // Row 2,3,4 are display rows. Since ResultMatrix has row first and col second:
      if (ResultMatrix != null && row >= 0 && row < ResultMatrix.Count)
      {
          if (col >= 0 && col < ResultMatrix[row].slotImages.Count)
          {
              return ResultMatrix[row].slotImages[col].GetComponent<SlotSymbolView>();
          }
      }
      return null;
  }

  public Image GetResultMatrixImage(int row, int col)
  {
      return ResultMatrix[row].slotImages[col];
  }

  public CoinPosition GetCoinPosition(int row, int col)
  {
      if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.coinPositions != null)
      {
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
              if (coin.position[0] == row && coin.position[1] == col)
              {
                  return coin;
              }
          }
      }
      return null;
  }

  public void EnableAllBackTints(bool active, float alpha = 0.85f)
  {
      for (int row = 0; row < ResultMatrix.Count; row++)
      {
          for (int col = 0; col < ResultMatrix[row].slotImages.Count; col++)
          {
              SlotSymbolView view = GetSymbolView(row, col);
              if (view != null)
              {
                  view.SetBackTintActive(active, alpha);
              }
          }
      }
  }

  private void Start()
  {
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

    animationManager.Initialize(this);
  }

  internal void FreeSpin(int spins)
  {
    IsFreeSpin = true;
    uiManager.SetFreeSpinsActive(true);

    if (FreeSpinsCount != spins)
    {
        SetFreeSpinsCount(spins);
    }

    if (LineAnimRoutine != null)
    {
      StopCoroutine(LineAnimRoutine);
      LineAnimRoutine = null;
    }

    StartCoroutine(FreeSpinCoroutine(spins));
  }

  private IEnumerator FreeSpinCoroutine(int spinchances)
  {
    int i = 0;
    while (i < spinchances)
    {
      StartSlots();
      yield return tweenroutine;
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
      uiManager.SetButtonsInteractable(true);
    }
    IsFreeSpin = false;
  }

  #region Autospin
  public void StartAutoplay(int count, bool untilFeature)
  {
    if (!IsAutoSpin)
    {
      AutoplayCount = count;
      AutoplayUntilFeature = untilFeature;
      TriggerAutoSpinState(true);
      OnAutoplayCountChanged?.Invoke(AutoplayCount, AutoplayUntilFeature);

      StartCoroutine(AutoSpinCoroutine());
    }
  }

  internal void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      StartAutoplay(AutoplayCount, AutoplayUntilFeature);
    }
  }

  public void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      TriggerAutoSpinState(false);
      OnAutoplayStopped?.Invoke();
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      if (!AutoplayUntilFeature && AutoplayCount <= 0)
      {
        StopAutoSpin();
        yield break;
      }

      StartSlots(true);
      yield return tweenroutine;

      if (!AutoplayUntilFeature)
      {
        AutoplayCount--;
        OnAutoplayCountChanged?.Invoke(AutoplayCount, AutoplayUntilFeature);
      }

      if (AutoplayUntilFeature && ResultData != null && ResultData.payload != null &&
          (ResultData.payload.isFreeSpinTriggered || ResultData.payload.isLinkTriggered))
      {
        StopAutoSpin();
        yield break;
      }

      if (!IsAutoSpin) yield break;

      yield return new WaitForSeconds(SpinDelay);
    }
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    yield return new WaitUntil(() => !IsSpinning);
    if (tweenroutine != null)
    {
      StopCoroutine(tweenroutine);
      tweenroutine = null;
    }
    if (!IsBonus) uiManager.SetButtonsInteractable(true);
  }
  #endregion

  private void CompareBalance()
  {
    if (Balance < TotalBet)
    {
      uiManager.LowBalPopup();
    }
  }

  public void ChangeBet(bool IncDec)
  {
    int counter = BetCounter;
    if (IncDec)
    {
      counter++;
      if (counter >= SocketManager.initialData.gameData.bets.Count)
      {
        counter = 0; // Loop back to the first bet
      }
    }
    else
    {
      counter--;
      if (counter < 0)
      {
        counter = SocketManager.initialData.gameData.bets.Count - 1; // Loop to the last bet
      }
    }
    SetBetIndex(counter);
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
        
        // Clear value texts on initialization
        SlotSymbolView view = images[i].slotImages[j].GetComponent<SlotSymbolView>();
        if (view != null)
        {
          view.ClearValues();
          ConfigureSymbolView(view, randomIndex);
        }
      }
    }
  }

  internal void SetInitialUI()
  {
    Initialize(SocketManager.initialData);
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
    if (slotTransform == null) return;
    Debug.Log("Here");
    slotTransform.SetSiblingIndex(17);

    int childCount = slotTransform.childCount;
    for (int i = 0; i < Mathf.Min(childCount, 2); i++)
    {
      var child = slotTransform.GetChild(i);
      var animation = child.GetComponent<ImageAnimation>();
      if (animation != null)
      {
        animation.AnimationSpeed = 15;  // Change animation speed
        Image image = child.GetComponent<Image>();
        if (image != null)
        {
          image.DOFade(0, 0);
          image.gameObject.SetActive(true);  // Activate the animation object
          image.DOFade(1, 0.5f);
        }
        animation.StartAnimation();     // Start animation
      }
    }
  }

  // Function to reset all changed slots
  private void ResetImages()
  {
    foreach (var (slotTransform, originalSiblingIndex) in changedSlots)
    {
      if (slotTransform == null) continue;
      // Reset the sibling index to the original value
      slotTransform.SetSiblingIndex(originalSiblingIndex);
      
      // Stop the animation and reset the state
      int childCount = slotTransform.childCount;
      for (int i = 0; i < Mathf.Min(childCount, 2); i++)
      {
        var child = slotTransform.GetChild(i);
        var animation = child.GetComponent<ImageAnimation>();
        if (animation != null)
        {
          animation.StopAnimation();  // Assuming you have a StopAnimation method
          if (animation.rendererDelegate != null)
          {
            animation.rendererDelegate.DOFade(1, 0.5f).OnComplete(() =>
            {
              animation.gameObject.SetActive(false);
            });
          }
          else
          {
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
              image.DOFade(1, 0.5f).OnComplete(() =>
              {
                child.gameObject.SetActive(false);
              });
            }
            else
            {
              child.gameObject.SetActive(false);
            }
          }
        }
      }
    }

    // Clear the list after resetting everything
    changedSlots.Clear();
  }

  //function to populate animation sprites accordingly
  internal void ConfigureAnimationSprites(ImageAnimation animScript, int val, int LP = 0, string coin = null)
  {
    if (animScript == null) return;
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    
    switch (val)
    {
      case 0:
        foreach (Sprite s in C_Sprites) animScript.textureArray.Add(s);
        animScript.AnimationSpeed = 19f;
        break;
      case 1:
        foreach (Sprite s in O_Sprites) animScript.textureArray.Add(s);
        animScript.AnimationSpeed = 19f;
        break;
      case 2:
        foreach (Sprite sprite in N_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 19f;
        break;
      case 3:
        foreach (Sprite sprite in B_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 19f;
        break;
      case 4:
        foreach (Sprite sprite in Barrel_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 5:
        foreach (Sprite sprite in Bus_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 6:
        foreach (Sprite sprite in Orange_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 7:
        foreach (Sprite sprite in Purple_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 8:
        foreach (Sprite sprite in Blue_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 10:
        foreach (Sprite sprite in Yellow_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 16f;
        break;
      case 11:
        foreach (Sprite sprite in Link_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 22f;
        break;
      case 12:
        foreach (Sprite sprite in MegaLink_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 12f;
        break;
      case 14:
        foreach (Sprite sprite in CC_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 12f;
        break;
      case 15:
        foreach (Sprite sprite in GoldCoin_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 22f;
        break;
      case 16:
        foreach (Sprite sprite in Diamond_Sprites) animScript.textureArray.Add(sprite);
        animScript.AnimationSpeed = 17f;
        break;
      case 17:
        if (LP == 2)
        {
          foreach (Sprite sprite in LP2_Sprites) animScript.textureArray.Add(sprite);
        }
        else if (LP == 3)
        {
          foreach (Sprite sprite in LP3_Sprites) animScript.textureArray.Add(sprite);
        }
        else if (LP == 4)
        {
          foreach (Sprite sprite in LP4_Sprites) animScript.textureArray.Add(sprite);
        }
        else if (LP == 5)
        {
          foreach (Sprite sprite in LP5_Sprites) animScript.textureArray.Add(sprite);
        }
        else if (LP == 7)
        {
          foreach (Sprite sprite in LP7_Sprites) animScript.textureArray.Add(sprite);
        }
        animScript.AnimationSpeed = 12f;
        break;
    }
  }

  #region SlotSpin
  //starts the spin process
  public void StartSlots(bool autoSpin = false)
  {
    if (IsFreeSpin)
    {
      SetFreeSpinsCount(FreeSpinsCount - 1);
    }

    uiManager.SetButtonsInteractable(false);
    uiManager.SetTotalWinText("0.000");

    StopGameAnimation(); 

    tweenroutine = StartCoroutine(TweenRoutine());
  }

  //manage the Routine for spinning of the slots
  private IEnumerator TweenRoutine()
  {
    bool winningsDisplayed = false;
    if (Balance < TotalBet && !IsFreeSpin) // Check if balance is sufficient to place the bet
    {
      CompareBalance();
      StopAutoSpin();
      yield return new WaitForSeconds(1);
      uiManager.SetButtonsInteractable(true);
      yield break;
    }

    TriggerSpinState(true);

    if (!IsFreeSpin)
    {
      uiManager.DeductBalanceUI();
    }
    
    if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
    {
      uiManager.ShowStopButton(true);
    }
    
    for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
    {
      InitializeTweening(Slot_Transform[i]);
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    UpdateFromSpinResult(SocketManager.resultData);

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
    
    // Wait for the last reel stop sequence to finish, then clean up
    float stopWait = (IsTurboOn || StopSpinToggle)
        ? (quickStopDuration + numberOfSlots * 0.05f + 0.05f)
        : (stopOvershootDuration + stopBounceBackDuration + stopSettleDuration + numberOfSlots * 0.12f + 0.1f);
    yield return new WaitForSeconds(stopWait);
    KillAllTweens();
    TriggerSpinState(false);
    yield return new WaitForSeconds(0.2f);
    
    // Play winning symbol animations through the AnimationManager
    System.Func<string, bool> isSpecial = id =>
        id == "11" || id == "12" || id == "14" ||
        id == "15" || id == "16" || id == "17";
    
    yield return animationManager.PlaySpecialSymbolAnimations(isSpecial, SocketManager.resultData.matrix);
    
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
      yield return new WaitUntil(() => animationManager.gameObject.activeSelf || !CheckPopups); // wait for popups
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }

    if (isCCTriggered)
    {
      uiManager.FadeWinningsPanel(1f, 0.3f);
      uiManager.SwitchTopUI(true);
      uiManager.multiplierCount = 0;
      foreach (var item in SocketManager.resultData.payload.coinPositions)
      {
        if (item.symbolId == 15)
        {
          yield return uiManager.TrailRendererAnimation(ResultMatrix[item.position[0]].slotImages[item.position[1]].transform.GetChild(5).gameObject, 3, item.coinValue);
        }
        else if (item.symbolId == 16)
        {
          Image slotImage = ResultMatrix[item.position[0]].slotImages[item.position[1]];
          SlotSymbolView view = slotImage.GetComponent<SlotSymbolView>();
          if (view != null && jackpotManager != null)
          {
            Sprite prizeSprite = null;
            if (JackpotSlotSymbols != null && JackpotSlotSymbols.Length > (item.prizeTypeIndex ?? 0))
            {
              prizeSprite = JackpotSlotSymbols[item.prizeTypeIndex ?? 0];
            }
            yield return jackpotManager.PlayJackpotSequence(view, item.prizeTypeIndex ?? 0, item.coinValue.ToString(), prizeSprite);
          }
        }
      }
      yield return new WaitForSeconds(1.2f);
      uiManager.SwitchTopUI(false);
      uiManager.FadeWinningsPanel(0f, 0.3f, () => { uiManager.SetCoinWinningText("0"); });

      yield return new WaitForSeconds(.2f);
    }
    
    if (SocketManager.resultData.payload.isLinkTriggered)
    {
      IsBonus = true;

      // Pause FreeSpins if already running
      if (IsFreeSpin)
      {
        WasAutoSpinOn = true;
        IsFreeSpin = false;
      }

      yield return ResetUI();



      stickySymbolManager.TurnOnIndices(GenerateFreezedLocations());
      yield return new WaitForSeconds(0.5f);

      _bonusManager.StartBonus(SocketManager.resultData.payload.linkRespinsRemaining);
      TriggerSpinState(false);
      yield break;   // EXIT AFTER LINK STARTS
    }
    
    if (SocketManager.resultData.payload.isFreeSpinTriggered)
    {
      if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
      {
        winningsDisplayed = true;
        CheckPopups = true;
        uiManager.WinningsTextAnimation();
        CheckWinPopups();

        yield return new WaitUntil(() => !CheckPopups);
        yield return new WaitForSeconds(.5f);
      }
      yield return ResetUI();

      uiManager.OpenFreeSpinsUI();
      IsFreeSpin = true;

      int extraFreeSpin = 0;
      yield return new WaitForSeconds(.5f);
      if (SocketManager.resultData.payload.freeSpinsRemaining > FreeSpinsCount)
      {
        yield return FreeSpinsSymbolAnimation();
        extraFreeSpin = SocketManager.resultData.payload.freeSpinsRemaining - FreeSpinsCount;
      }

      SetFreeSpinsCount(SocketManager.resultData.payload.freeSpinsRemaining);
      yield return new WaitForSeconds(1f);

      if (extraFreeSpin != 0)
      {
        yield return uiManager.MidGameImageAnimation(uiManager.GetFreeGamesImageAnimation(), extraFreeSpin);
      }
      else
      {
        yield return uiManager.MidGameImageAnimation(uiManager.GetFreeGamesImageAnimation(), FreeSpinsCount);
      }
      yield return new WaitForSeconds(0.5f);
      TriggerSpinState(false);
      FreeSpin(FreeSpinsCount);
    }

    if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
    {
      winningsDisplayed = true;
      CheckPopups = true;
      uiManager.WinningsTextAnimation();
      CheckWinPopups();

      yield return new WaitUntil(() => !CheckPopups);
      yield return new WaitForSeconds(.5f);
    }

    // Post-bonus and free spins cleanup
    if (!IsFreeSpin)
    {
      uiManager.SetButtonsInteractable(true);
    }

    if (FreeSpinsCount <= 0 && SocketManager.resultData.payload.freeSpinsRemaining <= 0)
    {
      uiManager.CloseFreeSpinsUI();
    }
    TriggerSpinState(false);
  }
  #endregion
  
  public void OnLinkFeatureCompleted()
  {
    if (WasAutoSpinOn && FreeSpinsCount > 0)
    {
      WasAutoSpinOn = false;
      IsFreeSpin = true;

      uiManager.OpenFreeSpinsUI();
      FreeSpin(FreeSpinsCount);
      return;
    }

    uiManager.SetButtonsInteractable(true);
  }

  private IEnumerator ResetUI()
  {
    yield return new WaitForSeconds(.5f);
    uiManager.SetTotalWinText("0.000");
    if (IsAutoSpin)
    {
      WasAutoSpinOn = !AutoplayUntilFeature;
      StopAutoSpin();
    }
    StopGameAnimation();
  }

  private List<List<int>> GenerateFreezedLocations()
  {
    List<List<int>> loc = new();
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
    return loc;
  }

  private ServerSymbolInfo GetSymbolInfo(int symbolId)
  {
      if (SocketManager != null && SocketManager.initialData != null && SocketManager.initialData.uiData != null && SocketManager.initialData.uiData.paylines != null)
      {
          var symbols = SocketManager.initialData.uiData.paylines.symbols;
          if (symbols != null)
          {
              return symbols.Find(s => s.id == symbolId);
          }
      }
      return null;
  }

  internal bool IsWildSymbol(int symbolId)
  {
      var sym = GetSymbolInfo(symbolId);
      if (sym != null && sym.name != null)
      {
          return sym.name.ToLower().Contains("wild");
      }
      return symbolId == 10 || symbolId == 13 || symbolId == 14 || symbolId == 15;
  }

  internal bool IsSpecialSymbol(int symbolId)
  {
      var sym = GetSymbolInfo(symbolId);
      if (sym != null && sym.name != null)
      {
          string lowerName = sym.name.ToLower();
          return lowerName.Contains("link") || lowerName.Contains("collect") || lowerName.Contains("diamond") || lowerName.Contains("prize");
      }
      return symbolId == 11 || symbolId == 12 || symbolId == 14 || symbolId == 16;
  }

  private Sprite GetSpecialLayerSprite(int symbolId)
  {
      if (SpecialLayerSymbols == null || SpecialLayerSymbols.Length == 0) return null;

      var sym = GetSymbolInfo(symbolId);
      if (sym != null && sym.name != null)
      {
          string lowerName = sym.name.ToLower();
          if (lowerName.Contains("megalink") || lowerName.Contains("mega_link"))
          {
              if (SpecialLayerSymbols.Length > 1) return SpecialLayerSymbols[1];
          }
          else if (lowerName.Contains("link"))
          {
              if (SpecialLayerSymbols.Length > 0) return SpecialLayerSymbols[0];
          }
          else if (lowerName.Contains("collect"))
          {
              if (SpecialLayerSymbols.Length > 2) return SpecialLayerSymbols[2];
          }
          else if (lowerName.Contains("diamond") || lowerName.Contains("prize"))
          {
              if (SpecialLayerSymbols.Length > 3) return SpecialLayerSymbols[3];
          }
      }

      switch (symbolId)
      {
          case 11: // Link
              return SpecialLayerSymbols.Length > 0 ? SpecialLayerSymbols[0] : null;
          case 12: // MegaLink
              return SpecialLayerSymbols.Length > 1 ? SpecialLayerSymbols[1] : null;
          case 14: // CashCollect
              return SpecialLayerSymbols.Length > 2 ? SpecialLayerSymbols[2] : null;
          case 16: // Diamond
              return SpecialLayerSymbols.Length > 3 ? SpecialLayerSymbols[3] : null;
          default:
              return null;
      }
  }

  internal void ConfigureSymbolView(SlotSymbolView view, int symbolId)
  {
      if (view == null) return;

      if (IsSpecialSymbol(symbolId))
      {
          if (view.specialSymbolLayer != null)
          {
              view.specialSymbolLayer.gameObject.SetActive(true);
              Sprite specialSprite = GetSpecialLayerSprite(symbolId);
              if (specialSprite != null)
              {
                  view.specialSymbolLayer.sprite = specialSprite;
              }
          }
      }

      if (IsWildSymbol(symbolId))
      {
          if (view.hatObject != null)
          {
              view.hatObject.SetActive(true);
          }
      }
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
        SlotSymbolView view = SlotImage.GetComponent<SlotSymbolView>();
        if (view != null) view.ClearValues();

        if (resultNum == 9) // BLANK SYMBOL - NEEDS RANDOM SYMBOL
        {
          int randomSymbolIndex = UnityEngine.Random.Range(0, 9); // 0 to 8 inclusive
          SlotImage.sprite = SlotSymbols[randomSymbolIndex];
          if (view != null) ConfigureSymbolView(view, randomSymbolIndex);
          continue;
        }
        if (resultNum == 17) // LP coin (LosPollos)
        {
          bool found = false;
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
            if (coin.symbolId == 17 && coin.position[0] == row && coin.position[1] == col)
            {
              SlotImage.sprite = SlotSymbols[resultNum];
              if (view != null) view.SetLosPolosValue(coin.coinValue);
              found = true;
              break;
            }
          }
          if (!found)
          {
            int[] tempIndex = { 2, 3, 4, 5, 7 };
            int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
            SlotImage.sprite = SlotSymbols[resultNum];
            if (view != null) view.SetLosPolosValue(randomIndex);
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
              if (view != null) view.SetGoldCoinValue(coin.coinValue);
              found = true;
              break;
            }
          }
          if (!found)
          {
            SlotImage.sprite = SlotSymbols[resultNum];
          }
        }
        else
        {
          if (resultNum == 14)
          {
            CCcount++;
          }
          SlotImage.sprite = SlotSymbols[resultNum];
        }

        if (view != null)
        {
          ConfigureSymbolView(view, resultNum);
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
    CheckPopups = false;
  }

  private IEnumerator FreeSpinsSymbolAnimation()
  {
    yield return new WaitForSeconds(1.5f);
    for (int i = 0; i < SocketManager.resultData.payload.coinPositions.Count; i++)
    {
      Debug.Log("Looping through lp");
      if (SocketManager.resultData.payload.coinPositions[i].symbolId == 17)
      {
        CoinPosition lp = SocketManager.resultData.payload.coinPositions[i];
        if (lp != null && ResultMatrix[lp.position[0]].slotImages[lp.position[1]] != null)
        {
          Debug.Log("Found lp on result matrix");
          Image LosPollosImage = ResultMatrix[lp.position[0]].slotImages[lp.position[1]];
          SlotSymbolView view = LosPollosImage.GetComponent<SlotSymbolView>();
          if (view != null && view.losPolosValueText != null)
          {
              RectTransform freeSpinNumberTransform = view.losPolosValueText.GetComponent<RectTransform>();
              LosPollosImage.sprite = SlotSymbols[17];

              Vector3 tempPosi = freeSpinNumberTransform.localPosition;
              Transform tempParent = freeSpinNumberTransform.parent;
              int TempSiblingIndex = freeSpinNumberTransform.GetSiblingIndex();

              freeSpinNumberTransform.SetParent(uiManager.GetAnimationParent());

              bool scale = false;
              yield return freeSpinNumberTransform.DOLocalMove(uiManager.GetFreeSpinCountUIPositon().localPosition, .4f).SetEase(Ease.Linear).OnUpdate(() =>
              {
                if (Vector3.Distance(freeSpinNumberTransform.localPosition, uiManager.GetFreeSpinCountUIPositon().localPosition) < 100f && !scale)
                {
                  scale = true;
                  freeSpinNumberTransform.DOScale(0, 0.2f);
                }
              }).WaitForCompletion();
              freeSpinNumberTransform.gameObject.SetActive(false);

              uiManager.AddFreeSpinsText(lp.coinValue);

              freeSpinNumberTransform.localPosition = tempPosi;
              freeSpinNumberTransform.SetParent(tempParent);
              freeSpinNumberTransform.SetSiblingIndex(TempSiblingIndex);
              freeSpinNumberTransform.DOScale(1, 0f);
              yield return new WaitForSeconds(1f);
          }
        }
      }
    }
  }

  private void CheckPayoutLineBackend(List<LineWin> lineWins, double jackpot = 0)
  {
    if (lineWins == null || lineWins.Count == 0)
    {
      return;
    }

    if (jackpot > 0)
    {
      for (int col = 0; col < Tempimages.Count; col++)
      {
        for (int row = 0; row < Tempimages[col].slotImages.Count; row++)
        {
           // Start generic animation
        }
      }
    }
    else
    {
      // Delegate to AnimationManager to show winning highlight cycles
      if (LineAnimRoutine != null)
         StopCoroutine(LineAnimRoutine);

      LineAnimRoutine = StartCoroutine(animationManager.PlayWinningLineAnimations(lineWins));
    }
  }

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }

  internal void StopGameAnimation()
  {
    if (changedSlots.Count > 0)
      ResetImages();

    if (LineAnimRoutine != null)
    {
      StopCoroutine(LineAnimRoutine);
      LineAnimRoutine = null;
    }

    animationManager.StopAllAnimations();
    EnableAllBackTints(false);
  }

  #region TweeningCode
  private int[] reelCycleCount = new int[5];
  private const int MinCyclesBeforeStop = 3;

  private void InitializeTweening(Transform slotTransform)
  {
    int col = System.Array.IndexOf(Slot_Transform, slotTransform);
    if (col < 0) return;

    reelCycleCount[col] = 0;
    float restY = initialYPositions[col];

    while (alltweens.Count <= col) alltweens.Add(null);
    if (alltweens[col] != null) { alltweens[col].Kill(); alltweens[col] = null; }

    Sequence startSeq = DOTween.Sequence();
    startSeq.Append(slotTransform.DOLocalMoveY(restY + anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutCubic));
    startSeq.Append(slotTransform.DOLocalMoveY(restY - dropDownDistance,       dropDownDuration      ).SetEase(Ease.InCubic));
    startSeq.Append(slotTransform.DOLocalMoveY(restY,                          settleBounceDuration  ).SetEase(Ease.OutBounce));
    startSeq.OnComplete(() => { if (IsSpinning) RunContinuousCycle(col, slotTransform, restY); });
    alltweens[col] = startSeq;
    startSeq.Play();
  }

  private void RunContinuousCycle(int col, Transform slotTransform, float restY)
  {
    if (!IsSpinning) return;

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);

    Tween cycle = slotTransform
        .DOLocalMoveY(restY - symbolHeight, spinCycleDuration)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
          if (!IsSpinning) return;
          CycleBufferSymbols(col);
          reelCycleCount[col]++;
          RunContinuousCycle(col, slotTransform, restY);
        });

    alltweens[col] = cycle;
  }

  private void CycleBufferSymbols(int col)
  {
    var imgs = images[col].slotImages;
    if (imgs == null || imgs.Count == 0) return;

    for (int i = imgs.Count - 1; i > 0; i--)
      imgs[i].sprite = imgs[i - 1].sprite;

    int r;
    do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
    imgs[0].sprite = SlotSymbols[r];
  }

  private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index,
                                   bool magnet = false, int CCloc = 0, bool isStop = false)
  {
    if (!isStop && !IsTurboOn)
    {
      yield return new WaitUntil(() => reelCycleCount[index] >= MinCyclesBeforeStop);
    }

    if (alltweens[index] != null) { alltweens[index].Kill(); alltweens[index] = null; }

    float restY = initialYPositions[index];

    PopulateResultMatrixForColumn(index);
    FillNonDisplayBufferRows(index);

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);

    if (!magnet)
    {
      Sequence stopSeq = DOTween.Sequence();

      if (IsTurboOn || isStop)
      {
        stopSeq.Append(slotTransform.DOLocalMoveY(restY - quickStopOvershoot, quickStopDuration * 0.3f).SetEase(Ease.InCubic));
        stopSeq.Append(slotTransform.DOLocalMoveY(restY,                      quickStopDuration * 0.7f).SetEase(Ease.OutBack, 1.2f));
      }
      else
      {
        stopSeq.Append(slotTransform.DOLocalMoveY(restY - stopOvershootDistance, stopOvershootDuration ).SetEase(Ease.InCubic));
        stopSeq.Append(slotTransform.DOLocalMoveY(restY + stopBounceBackDistance, stopBounceBackDuration).SetEase(Ease.OutCubic));
        stopSeq.Append(slotTransform.DOLocalMoveY(restY,                          stopSettleDuration    ).SetEase(Ease.OutBounce));
      }

      alltweens[index] = stopSeq;
      stopSeq.Play();

      if (index < numberOfSlots - 1)
      {
        float gap = (IsTurboOn || isStop) ? 0.05f : 0.12f;
        yield return new WaitForSeconds(gap);
      }
      else
      {
        yield return stopSeq.WaitForCompletion();
      }
    }
    else
    {
      ImageAnimation anim = slotTransform.name == "Slot"     ? uiManager.GetLeftMagnetImageAnimation()
                          : slotTransform.name == "Slot (4)" ? uiManager.GetRightMagnetImageAnimation()
                          : null;

      if (anim != null)
      {
        anim.rendererDelegate.sprite = MagnetInSprites[0];
        anim.gameObject.SetActive(true);
      }

      float magnetY = (CCloc == 0) ? 1547.255f : 1768.255f;
      yield return slotTransform.DOLocalMoveY(magnetY, 0.6f).SetEase(Ease.OutQuad).WaitForCompletion();

      if (anim != null)
      {
        anim.textureArray.Clear();
        foreach (Sprite s in MagnetInSprites) anim.textureArray.Add(s);
        anim.doLoopAnimation = false;
        anim.onLoopComplete  = null;
        anim.AnimationSpeed  = 8;
        anim.StopAnimation();

        bool magnetInDone = false;
        anim.onLoopComplete = (_) => { magnetInDone = true; };
        anim.StartAnimation();
        yield return new WaitUntil(() => magnetInDone);
        anim.onLoopComplete = null;

        yield return new WaitForSeconds(1f);

        anim.textureArray.Clear();
        foreach (Sprite s in MagnetLightening_Sprites) anim.textureArray.Add(s);
        anim.AnimationSpeed = 17;
        anim.StopAnimation();
        anim.StartAnimation();

        yield return new WaitUntil(() =>
            anim.textureArray.Count >= 5 &&
            anim.rendererDelegate != null &&
            anim.rendererDelegate.sprite == anim.textureArray[^5]);
      }

      int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
      Tween stopTween = slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.6f).SetEase(Ease.OutCubic);
      alltweens[index] = stopTween;
      yield return stopTween.WaitForCompletion();

      if (anim != null) StartCoroutine(CloseMagnet(anim));
      yield return new WaitForSeconds(0.5f);
    }
  }

  private void FillNonDisplayBufferRows(int col)
  {
    var imgs = images[col].slotImages;
    int count = imgs.Count; // 7

    for (int i = 0; i < count; i++)
    {
      if (i == 2 || i == 3 || i == 4) continue; // display rows already written
      int r;
      do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
      imgs[i].sprite = SlotSymbols[r];
    }
  }

  private IEnumerator CloseMagnet(ImageAnimation anim)
  {
    bool lightningDone = false;
    anim.onLoopComplete = (_) => { lightningDone = true; };
    yield return new WaitUntil(() => lightningDone);
    anim.onLoopComplete = null;

    anim.textureArray.Clear();
    for (int i = MagnetInSprites.Length - 1; i >= 0; i--)
      anim.textureArray.Add(MagnetInSprites[i]);

    anim.doLoopAnimation = false;
    anim.AnimationSpeed  = 8;
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

  private void KillAllTweens()
  {
    foreach (var t in alltweens) t?.Kill();
    alltweens.Clear();
  }
  #endregion
}
