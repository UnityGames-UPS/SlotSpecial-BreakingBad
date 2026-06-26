using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

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
  public bool WasFreeSpinPaused { get; set; }  // Separate from WasAutoSpinOn — tracks free spin pause state
  public bool StopSpinToggle { get; set; }
  public bool CheckPopups { get; set; }
  public int AutoplayCount { get; private set; }
  public bool AutoplayUntilFeature { get; private set; }
  public bool IsAutoplayStoppedMidSpin { get; set; }

  private Coroutine autoplayRoutine;

  // Feature queue for ordered execution of triggered features
  internal FeatureQueue featureQueue = new FeatureQueue();

  public InitData InitialData { get; private set; }
  public ServerSpinResponse ResultData { get; private set; }
  public ServerSpinResponse OriginalFeatureTriggerResult { get; private set; }
  public ServerPlayer PlayerData { get; private set; }

  // --- Dynamic Derived Properties ---
  public double Balance => PlayerData != null ? PlayerData.balance : 0;
  
  public double LineBet => (InitialData != null && InitialData.gameData != null && BetCounter < InitialData.gameData.bets.Count) 
      ? InitialData.gameData.bets[BetCounter] : 0;
  
  public double TotalBet => LineBet * (InitialData != null && InitialData.gameData != null ? InitialData.gameData.totalLines : 0);
  
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
  [SerializeField] internal StickySymbolManager stickySymbolManager;
  private List<LockedCashCollect> savedLockedCashCollects;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private BonusManager _bonusManager;
  [SerializeField] internal AnimationManager animationManager;
  [SerializeField] public JackpotManager jackpotManager;
  [SerializeField] private PopupManager popupManager;

  [Header("Sprites References")]
  [SerializeField] internal Sprite[] SlotSymbols;  //images taken initially
  [SerializeField] public Sprite[] JackpotSlotSymbols;
  [SerializeField] private Sprite[] SpecialLayerSymbols; // 0: Link, 1: MegaLink, 2: CashCollect, 3: Diamond, 4: LosPollos

  [Header("Slot References")]
  [SerializeField] private List<SlotImage> images;     //class to store total images
  internal List<SlotImage> Tempimages;     //class to store the result matrix
  internal List<SlotImage> ResultMatrix;

  [Header("Spin Settings")]
  [SerializeField] private float anticipationUpDistance = 20f;
  [SerializeField] private float anticipationUpDuration = 0.12f;
  [SerializeField] private float stopOvershootDistance  = 50f;
  [SerializeField] private float stopOvershootDuration  = 0.20f;
  [SerializeField] private float stopSettleDuration     = 0.30f;
  [SerializeField] private float quickStopOvershoot     = 20f;
  [SerializeField] private float quickStopDuration      = 0.20f;
  [SerializeField] private float symbolHeight           = 100f;
  [SerializeField] private float reelStopStagger        = 0.12f;
  [SerializeField] private float minimumSpinDuration    = 2.0f;
  [SerializeField] private float spinSpeed              = 2000f; // pixels per second during continuous spin

  private float[] initialYPositions;
  private float spinStartTime;

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
  [SerializeField] private Sprite[] LP_Sprites;
  [SerializeField] private Sprite[] GoldCoin_Sprites;

  private List<Tween> alltweens = new List<Tween>();
  private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

  private Coroutine tweenroutine;
  private Coroutine LineAnimRoutine = null;
  private bool isSettling = false; // cooldown flag after stop button
  internal bool IsFeatureTransitioning = false;

  [Header("Magnet Scenario References")]
  [SerializeField] private GameObject leftTopMagnet;
  [SerializeField] private GameObject leftBottomMagnet;
  [SerializeField] private GameObject rightTopMagnet;
  [SerializeField] private GameObject rightBottomMagnet;
  [SerializeField] private float magnetNudgeDuration = 0.5f;
  [SerializeField] private float magnetAnimDuration = 1.0f;
  [SerializeField] [Range(0f, 1f)] private float magnetTriggerChance = 0.5f;
  [SerializeField] [Range(0f, 1f)] private float nearMissChance = 0.3f;
  [SerializeField] private bool testFakeScenarios = false;
  [SerializeField] private bool testCol0CashCollect = false;
  private static int lastTestScenario = 0;

  // Runtime State for Magnet Scenario
  private bool isMagnetScenarioActive = false;
  private int magnetCol = -1;
  private int magnetRow = -1;
  private float magnetNudgeDir = 0f; // -1 for pull down, 1 for pull up

  // Runtime State for Cash Collect Near Miss
  private bool isNearMissActive = false;
  private int nearMissCol = -1;
  private int nearMissType = -1; // 0 for top near miss, 1 for bottom near miss
  [SerializeField] private float nearMissExtraSpinDuration = 1.5f; // extra spin duration for anticipation
  [SerializeField] private float col0CCExtraSpinDuration = 3.5f; // extra spin duration for columns 1-4 when Cash Collect lands in column 0
  [SerializeField] private float col0CCSpeedMultiplier = 1.5f; // speed multiplier for columns 1-4 when Column 0 has Cash Collect
  [SerializeField] private float col0CCSpeedUpDuration = 1.0f; // Phase 1: duration to smoothly ramp up the spin speed
  [SerializeField] private float col0CCFastSpinDuration = 1.5f; // Phase 2: duration to spin at fast speed
  [SerializeField] private float col0CCSlowDownDuration = 1.0f; // Phase 3: duration to smoothly ramp down the spin speed
  [SerializeField] [Range(0f, 1f)] private float col0CCTriggerChance = 0.5f; // probability (0-1) that Column 0 Cash Collect triggers anticipation transition
  private bool col0HasCC = false;
  private bool col0Stopped = false;
  private float col0StopTime = 0f;

  int tweenHeight = 0;  //calculate the height at which tweening is done
  private int numberOfSlots = 5;          //number of columns
  [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing
  [SerializeField] private float stopCooldownDuration = 0.4f; // cooldown after stop before next spin
  [SerializeField] private float specialSymbolStaggerIncrease = 0.75f; // extra delay added to next reels if special symbol lands

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
      if (IsBonus && _bonusManager != null)
      {
          if (_bonusManager.Slot != null && col >= 0 && col < _bonusManager.Slot.Count)
          {
              if (_bonusManager.Slot[col].slotTransforms != null && row >= 0 && row < _bonusManager.Slot[col].slotTransforms.Count)
              {
                  return _bonusManager.Slot[col].slotTransforms[row].GetComponentInChildren<SlotSymbolView>();
              }
          }
          return null;
      }

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

  public void SetColumnBackTintActive(int col, bool active, float alpha = 0.85f)
  {
      if (col < 0 || col >= images.Count) return;
      var imgs = images[col].slotImages;
      if (imgs == null) return;
      for (int i = 0; i < imgs.Count; i++)
      {
          SlotSymbolView view = imgs[i].GetComponent<SlotSymbolView>();
          if (view != null)
          {
              view.SetBackTintActive(active, alpha);
          }
      }
  }

  private void Start()
  {
    if (leftTopMagnet != null) leftTopMagnet.SetActive(false);
    if (leftBottomMagnet != null) leftBottomMagnet.SetActive(false);
    if (rightTopMagnet != null) rightTopMagnet.SetActive(false);
    if (rightBottomMagnet != null) rightBottomMagnet.SetActive(false);

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
    if (AudioController.Instance != null) AudioController.Instance.PlayMainBg();
  }

  public float SwipeThresholdValue => swipeThreshold;

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

    if (savedLockedCashCollects != null && stickySymbolManager != null)
    {
      Debug.Log($"[LockedCashCollect] Restoring savedLockedCashCollects inside FreeSpin method. count: {savedLockedCashCollects.Count}");
      stickySymbolManager.UpdateLockedCashCollects(savedLockedCashCollects);
      savedLockedCashCollects = null;
    }

    StartSlots();
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

      autoplayRoutine = StartCoroutine(AutoSpinCoroutine());
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
      
      if (IsSpinning)
      {
        IsAutoplayStoppedMidSpin = true;
      }
      else
      {
        if (autoplayRoutine != null)
        {
          StopCoroutine(autoplayRoutine);
          autoplayRoutine = null;
        }
        if (!IsFeatureTransitioning && !IsBonus)
        {
          uiManager.SetButtonsInteractable(true);
        }
      }
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

      // Check balance before starting next autoplay spin
      if (Balance < TotalBet && !IsFreeSpin)
      {
        StopAutoSpin();
        if (popupManager != null)
        {
          popupManager.ShowInsufficientFundsError();
        }
        yield break;
      }

      if (!AutoplayUntilFeature)
      {
        AutoplayCount--;
        OnAutoplayCountChanged?.Invoke(AutoplayCount, AutoplayUntilFeature);
      }

      StartSlots(true);
      yield return tweenroutine;

      if (AutoplayUntilFeature && ResultData != null && ResultData.payload != null &&
          (ResultData.payload.isFreeSpinTriggered || ResultData.payload.isLinkTriggered))
      {
        StopAutoSpin();
        yield break;
      }

      if (!IsAutoSpin)
      {
        autoplayRoutine = null;
        yield break;
      }

      yield return new WaitForSeconds(SpinDelay);
    }
    autoplayRoutine = null;
  }
  #endregion

  private void CompareBalance()
  {
    if (Balance < TotalBet)
    {
      if (popupManager != null)
      {
        popupManager.ShowInsufficientFundsError();
      }
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
    UpdateCashCollectIndicatorsForAllColumns();
    CompareBalance();
    uiManager.InitialiseUIData(SocketManager.initUIData.paylines);
    uiManager.SetJackpotText(SocketManager.initialData.features.jackpot);
  }
  #endregion

  private void ReorderImages(int targetCol)
  {
    // Only check the specific column that was just populated
    // (other columns may still be spinning with random symbols)
    for (int j = 0; j < 3; j++)
    {
      if (Tempimages[targetCol].slotImages[j].sprite == SlotSymbols[14]) //if the symbol is cash collect
      {
        // Store the original sibling index before changing it
        Transform slotTransform = Tempimages[targetCol].slotImages[j].transform;
        int originalSiblingIndex = slotTransform.GetSiblingIndex();

        // Add the slot transform and its original sibling index to the list
        changedSlots.Add((slotTransform, originalSiblingIndex));

        // Now apply the changes
        SetUpAccordingToCC(slotTransform);
      }
    }
  }

  private void SetUpAccordingToCC(Transform slotTransform)
  {
    if (slotTransform == null) return;
    Debug.Log("Here");

    // Add Canvas to override sorting order to 10 so it renders on top of everything
    // without changing the sibling index which shifts layout positions.
    Canvas canvas = slotTransform.GetComponent<Canvas>();
    if (canvas == null)
    {
      canvas = slotTransform.gameObject.AddComponent<Canvas>();
    }
    canvas.overrideSorting = true;
    canvas.sortingOrder = 10;

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

      // Remove the added Canvas component to restore normal sorting
      Canvas canvas = slotTransform.GetComponent<Canvas>();
      if (canvas != null)
      {
        Destroy(canvas);
      }
      
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
    animScript.AnimationSpeed = 30f;
    
    switch (val)
    {
      case 0:
        foreach (Sprite s in C_Sprites) animScript.textureArray.Add(s);
        break;
      case 1:
        foreach (Sprite s in O_Sprites) animScript.textureArray.Add(s);
        break;
      case 2:
        foreach (Sprite sprite in B_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 3:
        foreach (Sprite sprite in N_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 4:
        foreach (Sprite sprite in Barrel_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 5:
        foreach (Sprite sprite in Bus_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 6:
        foreach (Sprite sprite in Orange_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 7:
        foreach (Sprite sprite in Purple_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 8:
        foreach (Sprite sprite in Blue_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 10:
        foreach (Sprite sprite in Yellow_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 11:
        foreach (Sprite sprite in Link_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 12:
        foreach (Sprite sprite in MegaLink_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 14:
        foreach (Sprite sprite in CC_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 13:
      case 15:
        foreach (Sprite sprite in GoldCoin_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 16:
        foreach (Sprite sprite in Diamond_Sprites) animScript.textureArray.Add(sprite);
        break;
      case 17:
        foreach (Sprite sprite in LP_Sprites) animScript.textureArray.Add(sprite);
        break;
    }
  }

  #region SlotSpin
  //starts the spin process
  public void StartSlots(bool autoSpin = false, bool reverse = false)
  {
    isSpinReverse = reverse;
    IsFeatureTransitioning = false;
    // Forcefully clean up any previous spin state to prevent glitches
    ForceCleanupPreviousSpin();

    if (!IsFreeSpin && stickySymbolManager != null)
    {
      stickySymbolManager.Reset();
    }

    IsAutoplayStoppedMidSpin = false;

    // Reset scenario states
    isMagnetScenarioActive = false;
    magnetCol = -1;
    magnetRow = -1;
    magnetNudgeDir = 0f;
    isNearMissActive = false;
    nearMissCol = -1;
    nearMissType = -1;

    if (IsFreeSpin)
    {
      SetFreeSpinsCount(FreeSpinsCount - 1);
    }

    uiManager.SetButtonsInteractable(false);
    uiManager.SetTotalWinText("00.00");

    StopGameAnimation(); 

    tweenroutine = StartCoroutine(TweenRoutine());
  }

  // Forcefully cleans up all state from a previous spin so a fresh spin can start cleanly
  private void ForceCleanupPreviousSpin()
  {
    // Stop the previous spin coroutine if still running
    if (tweenroutine != null)
    {
      StopCoroutine(tweenroutine);
      tweenroutine = null;
    }

    IsAutoplayStoppedMidSpin = false;

    // Kill all running tweens
    KillAllTweens();

    // Reset all slot transforms to their initial Y positions to prevent positional drift
    ResetSlotPositions();

    // Reset spin flags
    StopSpinToggle = false;
    isSettling = false;
    TriggerSpinState(false);

    // Reset trigger flags on ResultData so they don't persist into the next spin
    if (ResultData != null && ResultData.payload != null)
    {
        ResultData.payload.isFreeSpinTriggered = false;
        ResultData.payload.isLinkTriggered = false;
        ResultData.payload.isPrizeCoinTriggered = false;
        if (ResultData.payload.freeSpinResult != null)
        {
            ResultData.payload.freeSpinResult.triggered = false;
        }
    }
  }

  // Resets all slot column transforms to their initial resting positions
  private void ResetSlotPositions()
  {
    if (initialYPositions == null || Slot_Transform == null) return;
    for (int i = 0; i < numberOfSlots; i++)
    {
      if (Slot_Transform[i] != null && i < initialYPositions.Length)
      {
        Slot_Transform[i].localPosition = new Vector3(
          Slot_Transform[i].localPosition.x,
          initialYPositions[i],
          0f);
      }
    }
  }

  private void DetermineFakeScenarios()
  {
    isMagnetScenarioActive = false;
    magnetCol = -1;
    magnetRow = -1;
    magnetNudgeDir = 0f;

    isNearMissActive = false;
    nearMissCol = -1;
    nearMissType = -1;

    col0HasCC = false;

    if (SocketManager.resultData == null || SocketManager.resultData.matrix == null) return;

    var matrix = SocketManager.resultData.matrix;
    var payload = SocketManager.resultData.payload;

    // Check if Column 0 has a Cash Collect symbol (14)
    bool hasCCInCol0 = false;
    for (int r = 0; r < matrix.Count; r++)
    {
      if (matrix[r] != null && matrix[r].Count > 0)
      {
        if (matrix[r][0] == "14")
        {
          hasCCInCol0 = true;
          break;
        }
      }
    }

    if (hasCCInCol0)
    {
      float roll = UnityEngine.Random.value;
      if (roll < col0CCTriggerChance)
      {
        col0HasCC = true;
        Debug.Log($"[FakeScenario] Column 0 Cash Collect landed! Triggering transition (roll: {roll:F2} < chance: {col0CCTriggerChance:F2})");
      }
      else
      {
        Debug.Log($"[FakeScenario] Column 0 Cash Collect landed, but trigger chance failed (roll: {roll:F2} >= chance: {col0CCTriggerChance:F2})");
      }
    }

    if (testCol0CashCollect)
    {
      col0HasCC = true;
      Debug.Log("[FakeScenario TEST] Column 0 Cash Collect FORCED via testCol0CashCollect!");
    }

    if (testFakeScenarios)
    {
      isNearMissActive = true;
      if (lastTestScenario == 0)
      {
        nearMissCol = 4;
        nearMissType = 0;
        lastTestScenario = 1;
      }
      else
      {
        nearMissCol = 4;
        nearMissType = 1;
        lastTestScenario = 0;
      }
      Debug.Log($"[FakeScenario TEST] Cash Collect Near-Miss FORCED! Reel {nearMissCol}, Type {(nearMissType == 0 ? "Top" : "Bottom")}");
      return;
    }

    bool isCCTriggered = payload != null && payload.cashCollectResult != null && payload.cashCollectResult.triggered;
    bool isLinkTriggered = payload != null && payload.isLinkTriggered;
    bool featureTriggered = isCCTriggered || isLinkTriggered;

    if (featureTriggered)
    {
      int[] checkCols = { 0, 4 };
      int[] checkRows = { 0, 2 };

      List<(int col, int row)> candidates = new List<(int, int)>();
      foreach (int c in checkCols)
      {
        foreach (int r in checkRows)
        {
          if (r < matrix.Count && c < matrix[r].Count)
          {
            if (matrix[r][c] == "14")
            {
              if (stickySymbolManager != null && stickySymbolManager.IsPositionLocked(c, r))
              {
                continue;
              }
              candidates.Add((c, r));
            }
          }
        }
      }

      if (candidates.Count > 0)
      {
        float roll = UnityEngine.Random.value;
        if (roll < magnetTriggerChance)
        {
          var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
          isMagnetScenarioActive = true;
          magnetCol = selected.col;
          magnetRow = selected.row;
          magnetNudgeDir = (magnetRow == 0) ? -1f : 1f;
          Debug.Log($"[FakeScenario] Magnet Scenario Selected! Reel {magnetCol}, Row {magnetRow}, NudgeDir {magnetNudgeDir}");
          return;
        }
      }
    }

    bool hasCCInMatrix = false;
    for (int r = 0; r < matrix.Count; r++)
    {
      for (int c = 0; c < matrix[r].Count; c++)
      {
        if (matrix[r][c] == "14")
        {
          hasCCInMatrix = true;
          break;
        }
      }
      if (hasCCInMatrix) break;
    }

    if (!hasCCInMatrix)
    {
      bool hasSpecialInCol0 = false;
      for (int r = 0; r < matrix.Count; r++)
      {
        if (matrix[r] != null && matrix[r].Count > 0)
        {
          string symId = matrix[r][0];
          if (symId == "11" || symId == "12" || symId == "13" || symId == "14" || symId == "15" || symId == "16" || symId == "17")
          {
            hasSpecialInCol0 = true;
            break;
          }
          if (int.TryParse(symId, out int symbolId))
          {
            if (IsSpecialSymbol(symbolId))
            {
              hasSpecialInCol0 = true;
              break;
            }
          }
        }
      }

      if (hasSpecialInCol0)
      {
        float roll = UnityEngine.Random.value;
        if (roll < nearMissChance)
        {
          isNearMissActive = true;
          nearMissCol = 4;
          nearMissType = (UnityEngine.Random.value < 0.5f) ? 0 : 1;
          Debug.Log($"[FakeScenario] Cash Collect Near-Miss Selected! Reel {nearMissCol}, Type {(nearMissType == 0 ? "Top" : "Bottom")} (due to special icon in column 0)");
        }
      }
    }
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
    spinStartTime = Time.time;

    if (!IsFreeSpin)
    {
      uiManager.DeductBalanceUI();
    }
    
    if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
    {
      uiManager.ShowStopButton(true);
    }
    
    // All reels start together simultaneously
    for (int i = 0; i < numberOfSlots; i++)
    {
      InitializeTweening(Slot_Transform[i]);
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    UpdateFromSpinResult(SocketManager.resultData);
    OriginalFeatureTriggerResult = SocketManager.resultData;
    DetermineFakeScenarios();
    bool isCCTriggered = false;
    if (SocketManager.resultData != null && SocketManager.resultData.payload != null)
    {
      var spinCcResult = SocketManager.resultData.payload.cashCollectResult;
      isCCTriggered = spinCcResult != null && spinCcResult.triggered;

      if (SocketManager.resultData.payload.isLinkTriggered || 
          SocketManager.resultData.payload.isFreeSpinTriggered || 
          isCCTriggered)
      {
        IsFeatureTransitioning = true;
        uiManager.UpdateButtonsState();
      }
    }

    // NOTE: Do NOT populate the result matrix here — the display rows would flash the
    // final result while reels are still spinning. Each column's result is populated
    // inside StopTweening → PopulateResultMatrixForColumn, right before that reel lands.

    // Enforce minimum spin duration for classic casino feel
    if (IsTurboOn)
    {
      yield return new WaitForSeconds(0.2f);
      StopSpinToggle = true;
    }
    else
    {
      // Wait until minimum spin duration has elapsed (or stop is pressed)
      float elapsed = Time.time - spinStartTime;
      float remaining = minimumSpinDuration - elapsed;
      if (remaining > 0f)
      {
        float waited = 0f;
        while (waited < remaining && !StopSpinToggle)
        {
          yield return new WaitForSeconds(0.1f);
          waited += 0.1f;
        }
      }
    }

    // Wait until all reels have completed the minimum cycles (unless stop was pressed or turbo is on)
    if (!StopSpinToggle && !IsTurboOn)
    {
      while (true)
      {
        bool allReelsReady = true;
        for (int i = 0; i < numberOfSlots; i++)
        {
          if (reelCycleCount[i] < MinCyclesBeforeStop)
          {
            allReelsReady = false;
            break;
          }
        }
        if (allReelsReady) break;
        yield return null;
      }
    }

    bool wasStopPressed = StopSpinToggle;
    StopSpinToggle = false;

    // Start stopping each reel in parallel with dynamic stagger increase for special symbols
    wasStopPressedGlobal = wasStopPressed;
    float baseStagger = IsTurboOn ? 0.03f : ((wasStopPressed) ? 0.05f : reelStopStagger);
    
    System.Func<string, bool> isSpecialSymbol = id =>
        id == "11" || id == "12" || id == "13" || id == "14" ||
        id == "15" || id == "16" || id == "17";

    float currentDelay = 0f;
    for (int i = 0; i < numberOfSlots; i++)
    {
      if (i > 0)
      {
        // Check if the previous reel (i - 1) contains any special symbols
        bool prevHasSpecial = false;
        if (SocketManager.resultData != null && SocketManager.resultData.matrix != null)
        {
          for (int r = 0; r < 3; r++)
          {
            if (r < SocketManager.resultData.matrix.Count && (i - 1) < SocketManager.resultData.matrix[r].Count)
            {
              string symId = SocketManager.resultData.matrix[r][i - 1];
              if (isSpecialSymbol(symId))
              {
                prevHasSpecial = true;
                break;
              }
            }
          }
        }

        float currentStagger = baseStagger;
        /*
        if (prevHasSpecial && !IsTurboOn && !wasStopPressed)
        {
          currentStagger += specialSymbolStaggerIncrease;
        }
        */
        currentDelay += currentStagger;
      }
      float finalDelay = currentDelay;
      if (col0HasCC && i > 0)
      {
        finalDelay += col0CCExtraSpinDuration;
      }
      if (isNearMissActive && i == nearMissCol)
      {
        finalDelay += nearMissExtraSpinDuration;
      }
      StartCoroutine(TriggerReelStopAfterDelay(i, finalDelay));
    }

    if (SocketManager.resultData.payload.winAmount > 0)
    {
      SpinDelay = 1.2f;
    }
    else
    {
      SpinDelay = 0.2f;
    }
    
    // Wait for the stop sequences to complete (staggered delay + 5 scrolling cycles + bounce duration)
    float speed = IsTurboOn ? 3500f : spinSpeed;
    float cycleDuration = symbolHeight / speed;
    float longestStopTime = (IsTurboOn || wasStopPressed)
        ? (currentDelay + 5f * cycleDuration + quickStopDuration)
        : (currentDelay + 5f * cycleDuration + stopOvershootDuration + stopSettleDuration);

    if (col0HasCC)
    {
      longestStopTime += col0CCExtraSpinDuration;
    }
    if (isNearMissActive)
    {
      longestStopTime += nearMissExtraSpinDuration;
      if (nearMissType == 0) longestStopTime += 4.5f;
      else if (nearMissType == 1) longestStopTime += 2.5f;
    }

    yield return new WaitForSeconds(longestStopTime + 0.05f);

    if (col0HasCC)
    {
      for (int c = 1; c < numberOfSlots; c++)
      {
        SetColumnBackTintActive(c, false);
      }
    }

    if (isMagnetScenarioActive)
    {
      yield return PlayMagnetSequence();
    }

    KillAllTweens();
    ResetSlotPositions();
    TriggerSpinState(false);

    // Cooldown after stop button: show spin button but non-interactable so slots settle
    if (wasStopPressed && !IsAutoSpin && !IsFreeSpin)
    {
      isSettling = true;
      uiManager.ShowSpinButtonCooldown(true);
      yield return new WaitForSeconds(stopCooldownDuration);
      isSettling = false;
      uiManager.ShowSpinButtonCooldown(false);
    }
    else
    {
      yield return new WaitForSeconds(0.2f);
    }
    
    // Wait for all landing animations to settle completely before starting win lines or features
    yield return new WaitUntil(() => !animationManager.AreLandingAnimationsPlaying);
    
    yield return new WaitForSeconds(0.3f);

    // Update locked cash collect overlays
    if (IsFreeSpin && stickySymbolManager != null && SocketManager.resultData != null && SocketManager.resultData.payload != null)
    {
        Debug.Log($"[LockedCashCollect] Calling UpdateLockedCashCollects from TweenRoutine. list count: {(SocketManager.resultData.payload.lockedCashCollects != null ? SocketManager.resultData.payload.lockedCashCollects.Count.ToString() : "null")}");
        stickySymbolManager.UpdateLockedCashCollects(SocketManager.resultData.payload.lockedCashCollects);
    }

    // --- FEATURE QUEUE: Build first to check for Prize Coin / Cash Coin flows ---
    featureQueue.BuildFromResponse(SocketManager.resultData.payload, IsFreeSpin);

    // Check if prize coin jackpot or cash collect flow is triggered
    bool hasPrizeOrCashFlow = featureQueue.Contains(FeatureType.PrizeCoinJackpot) || featureQueue.Contains(FeatureType.CashCollect);

    if (SocketManager.resultData.payload.winAmount > 0 && !hasPrizeOrCashFlow)
    {
      List<LineWin> winLine = new();
      foreach (var item in SocketManager.resultData.payload.lineWins)
      {
        winLine.Add(item);
      }
      CheckPayoutLineBackend(winLine);
    }

    if (!hasPrizeOrCashFlow && (IsAutoSpin || SocketManager.resultData.payload.isFreeSpinActive || SocketManager.resultData.payload.linkFeatureActive))
    {
      yield return new WaitUntil(() => animationManager.gameObject.activeSelf || !CheckPopups); // wait for popups
      if (LineAnimRoutine != null)
      {
        yield return LineAnimRoutine;
      }
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }

    // Process the feature queue sequentially
    yield return ProcessFeatureQueue(winningsDisplayed);

    // If we bypassed starting line animations initially because of prize coin or cash collect flow,
    // and those features have now finished, start the line animations and wait for them in AutoSpin/FreeSpin/Link.
    if (hasPrizeOrCashFlow && (IsAutoSpin || SocketManager.resultData.payload.isFreeSpinActive || SocketManager.resultData.payload.linkFeatureActive))
    {
      yield return new WaitUntil(() => animationManager.gameObject.activeSelf || !CheckPopups); // wait for popups
      if (LineAnimRoutine != null)
      {
        yield return LineAnimRoutine;
      }
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }
  }
  #endregion
  
  /// <summary>
  /// Called by BonusManager.EndBonus() when the Cash+Link feature finishes.
  /// Checks if there are remaining features in the queue (e.g., FreeSpin after Link)
  /// or if free spins were paused and need to resume.
  /// </summary>
  public void OnLinkFeatureCompleted()
  {
    // Check if there are remaining queued features (e.g., FreeSpin was queued after Link in Case 1)
    if (featureQueue.HasPending)
    {
      Debug.Log("[FeatureQueue] OnLinkFeatureCompleted: Processing remaining features");
      StartCoroutine(ProcessRemainingFeaturesAfterLink());
      return;
    }

    // Resume paused free spins (Case 2: Link triggered during Free Spins)
    if (WasFreeSpinPaused && FreeSpinsCount > 0)
    {
      Debug.Log("[FeatureQueue] OnLinkFeatureCompleted: Resuming paused Free Spins");
      WasFreeSpinPaused = false;
      IsFreeSpin = true;

      uiManager.OpenFreeSpinsUI();
      FreeSpin(FreeSpinsCount);
      return;
    }

    // Restore auto spin if it was on before the feature
    if (WasAutoSpinOn)
    {
      WasAutoSpinOn = false;
      AutoSpin();
      return;
    }

    uiManager.SetButtonsInteractable(true);
  }

  /// <summary>
  /// Processes remaining features in the queue after the Link/Bonus feature completes.
  /// This handles Case 1 where FreeSpin is queued after CashCollectAndLink.
  /// </summary>
  private IEnumerator ProcessRemainingFeaturesAfterLink()
  {
    while (featureQueue.HasPending)
    {
      FeatureType feature = featureQueue.Dequeue();

      switch (feature)
      {
        case FeatureType.FreeSpin:
          yield return HandleFreeSpinTrigger(false);
          yield break; // FreeSpin starts its own loop

        case FeatureType.FreeSpinRetrigger:
          yield return HandleFreeSpinRetrigger();
          // After retrigger, continue free spin loop
          if (IsFreeSpin && FreeSpinsCount > 0)
          {
            FreeSpin(FreeSpinsCount);
            yield break;
          }
          break;

        default:
          Debug.LogWarning($"[FeatureQueue] Unexpected feature after Link: {feature}");
          break;
      }
    }

    // No more queued features — check if we need to resume free spins
    if (WasFreeSpinPaused && FreeSpinsCount > 0)
    {
      WasFreeSpinPaused = false;
      IsFreeSpin = true;
      uiManager.OpenFreeSpinsUI();
      FreeSpin(FreeSpinsCount);
      yield break;
    }

    if (WasAutoSpinOn)
    {
      WasAutoSpinOn = false;
      AutoSpin();
      yield break;
    }

    uiManager.SetButtonsInteractable(true);
  }

  /// <summary>
  /// Main feature queue processor. Called after reels stop and animations play.
  /// Processes features in order: PrizeCoinJackpot → CashCollectAndLink → FreeSpin.
  /// </summary>
  private IEnumerator ProcessFeatureQueue(bool winningsAlreadyDisplayed)
  {
    bool winningsDisplayed = winningsAlreadyDisplayed;

    while (featureQueue.HasPending)
    {
      FeatureType feature = featureQueue.Dequeue();
      Debug.Log($"[FeatureQueue] Processing: {feature}");

      switch (feature)
      {
        case FeatureType.PrizeCoinJackpot:
          yield return HandlePrizeCoinJackpot();
          break;

        case FeatureType.CashCollect:
          yield return HandleCashCollectSequence();
          break;

        case FeatureType.CashCollectAndLink:
          // Show winnings before transitioning to bonus
          if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
          {
            winningsDisplayed = true;
            CheckPopups = true;
            uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
            yield return new WaitUntil(() => !CheckPopups);
            yield return new WaitForSeconds(.5f);
          }
          if (IsFreeSpin && SocketManager.resultData != null && SocketManager.resultData.payload != null)
          {
              savedLockedCashCollects = SocketManager.resultData.payload.lockedCashCollects;
          }
          yield return HandleCashCollectAndLink();
          yield break; // EXIT — BonusManager takes over, OnLinkFeatureCompleted will continue queue

        case FeatureType.FreeSpin:
          // Show winnings before starting free spins
          if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
          {
            winningsDisplayed = true;
            CheckPopups = true;
            uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
            yield return new WaitUntil(() => !CheckPopups);
            yield return new WaitForSeconds(.5f);
          }
          yield return HandleFreeSpinTrigger(false);
          yield break; // EXIT — FreeSpin loop takes over

        case FeatureType.FreeSpinRetrigger:
          // Show winnings before retrigger sequence
          if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
          {
            winningsDisplayed = true;
            CheckPopups = true;
            uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
            yield return new WaitUntil(() => !CheckPopups);
            yield return new WaitForSeconds(.5f);
          }
          yield return HandleFreeSpinRetrigger();
          // After retrigger, continue the free spin loop
          TriggerSpinState(false);
          FreeSpin(FreeSpinsCount);
          yield break;
      }
    }

    // --- No features triggered (or only PrizeCoinJackpot which completed inline) ---

    // Show winnings if not yet displayed
    if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
    {
      winningsDisplayed = true;
      CheckPopups = true;
      uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
      yield return new WaitUntil(() => !CheckPopups);
      yield return new WaitForSeconds(.5f);
    }

    // Post-spin cleanup
    IsAutoplayStoppedMidSpin = false;
    TriggerSpinState(false);

    if (IsFreeSpin)
    {
      if (FreeSpinsCount > 0 && SocketManager.resultData.payload.freeSpinsRemaining > 0)
      {
        yield return new WaitForSeconds(0.5f);
        StartSlots();
      }
      else
      {
        // Free spins ended
        bool winPopupClosed = false;
        double totalFeatureWin = 0;
        if (ResultData != null && ResultData.features != null)
        {
          totalFeatureWin = ResultData.features.featureWin;
        }
        else
        {
          totalFeatureWin = uiManager.AccumulatedFreeSpinWin;
        }
        uiManager.OpenFeatureWinPopup(totalFeatureWin, () => {
            winPopupClosed = true;
        });
        yield return new WaitUntil(() => winPopupClosed);

        double enableThreshold = (uiManager != null && uiManager.winTypePopup != null) ? uiManager.winTypePopup.EnableWinThreshold : 3.0;
        if (totalFeatureWin >= LineBet * enableThreshold)
        {
            bool winTypePopupClosed = false;
            uiManager.WinningsTextAnimation(() => {
                winTypePopupClosed = true;
            }, totalFeatureWin);
            yield return new WaitUntil(() => winTypePopupClosed);
        }

        IsFreeSpin = false;
        uiManager.CloseFreeSpinsUI();
        if (stickySymbolManager != null)
        {
          stickySymbolManager.Reset();
        }
        if (WasAutoSpinOn)
        {
          WasAutoSpinOn = false;
          AutoSpin();
        }
        else
        {
          uiManager.SetButtonsInteractable(true);
        }
      }
    }
    else
    {
      uiManager.SetButtonsInteractable(true);
    }
  }

  /// <summary>
  /// Handles PrizeCoin Jackpot feature: plays the mini jackpot slot animation
  /// for each PrizeCoin on the grid. Plays inline (doesn't transition to another screen).
  /// </summary>
  private IEnumerator HandlePrizeCoinJackpot()
  {
    Debug.Log("[FeatureQueue] HandlePrizeCoinJackpot: Playing jackpot animations");
    uiManager.multiplierCount = 0;

    foreach (var item in SocketManager.resultData.payload.coinPositions)
    {
      if (item.symbolId == 16)
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
          double jackpotAmount = item.coinValue * TotalBet;
          yield return jackpotManager.PlayJackpotSequence(view, item.prizeType, item.prizeTypeIndex ?? 0, jackpotAmount.ToString("0.###"), prizeSprite);
        }
      }
    }

    yield return new WaitForSeconds(1.2f);
    yield return new WaitForSeconds(.2f);

    // Only clear transitioning if no more features are queued
    if (!featureQueue.HasPending)
    {
      IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();
    }
  }

  /// <summary>
  /// Handles Cash Collect & Link feature: pauses free spins if active,
  /// transitions to BonusManager. BonusManager.EndBonus() will call
  /// OnLinkFeatureCompleted() when done.
  /// </summary>
  private IEnumerator HandleCashCollectAndLink()
  {
    Debug.Log("[FeatureQueue] HandleCashCollectAndLink: Starting bonus");

    IsBonus = true;
    IsFeatureTransitioning = false;
    uiManager.UpdateButtonsState();

    // Pause FreeSpins if currently running (Case 2)
    if (IsFreeSpin)
    {
      Debug.Log("[FeatureQueue] Pausing Free Spins for Cash+Link");
      WasFreeSpinPaused = true;
      IsFreeSpin = false;
    }

    yield return ResetUI();

    _bonusManager.StartBonus(SocketManager.resultData.payload.linkRespinsRemaining);
    TriggerSpinState(false);
    // yield break is handled by the caller
  }

  private IEnumerator HandleCashCollectSequence()
  {
    Debug.Log("[FeatureQueue] HandleCashCollectSequence: Starting cash collect sequence");
    
    IsFeatureTransitioning = true;
    uiManager.UpdateButtonsState();

    // Stop winning line animations during cash collect sequence
    StopGameAnimation();
    yield return new WaitForSeconds(0.2f);

    var ccResult = SocketManager.resultData.payload.cashCollectResult;
    if (ccResult != null && ccResult.triggered)
    {
      yield return uiManager.PlayCashCollectSequence(ccResult);
    }

    // Restart winning line animations if they were active
    if (SocketManager.resultData.payload.lineWins != null && SocketManager.resultData.payload.lineWins.Count > 0)
    {
      List<LineWin> winLine = new();
      foreach (var item in SocketManager.resultData.payload.lineWins)
      {
        winLine.Add(item);
      }
      CheckPayoutLineBackend(winLine);
    }

    IsFeatureTransitioning = false;
    uiManager.UpdateButtonsState();
  }

  /// <summary>
  /// Handles Free Spin trigger: plays the trigger animation sequence and starts
  /// the free spin loop.
  /// </summary>
  private IEnumerator HandleFreeSpinTrigger(bool isRetrigger)
  {
    Debug.Log($"[FeatureQueue] HandleFreeSpinTrigger: isRetrigger={isRetrigger}");

    if (!isRetrigger && !IsFreeSpin)
    {
      yield return ResetUI();
    }

    var fsResult = (OriginalFeatureTriggerResult != null && OriginalFeatureTriggerResult.payload != null)
        ? OriginalFeatureTriggerResult.payload.freeSpinResult
        : SocketManager.resultData.payload.freeSpinResult;

    yield return uiManager.PlayFreeSpinTriggerSequence(
        fsResult, isRetrigger || IsFreeSpin);

    IsFreeSpin = true;
    IsFeatureTransitioning = false;
    uiManager.UpdateButtonsState();

    int remainingSpins = (OriginalFeatureTriggerResult != null && OriginalFeatureTriggerResult.payload != null)
        ? OriginalFeatureTriggerResult.payload.freeSpinsRemaining
        : SocketManager.resultData.payload.freeSpinsRemaining;
    SetFreeSpinsCount(remainingSpins);
    yield return new WaitForSeconds(1f);

    TriggerSpinState(false);
    FreeSpin(FreeSpinsCount);
  }

  /// <summary>
  /// Handles Free Spin retrigger: plays the retrigger animation and adds spins.
  /// Does NOT start the free spin loop — the caller is responsible for that.
  /// </summary>
  private IEnumerator HandleFreeSpinRetrigger()
  {
    Debug.Log("[FeatureQueue] HandleFreeSpinRetrigger: Adding spins");

    var fsResult = (OriginalFeatureTriggerResult != null && OriginalFeatureTriggerResult.payload != null)
        ? OriginalFeatureTriggerResult.payload.freeSpinResult
        : SocketManager.resultData.payload.freeSpinResult;

    yield return uiManager.PlayFreeSpinTriggerSequence(
        fsResult, true);

    IsFreeSpin = true;
    IsFeatureTransitioning = false;
    uiManager.UpdateButtonsState();

    int remainingSpins = (OriginalFeatureTriggerResult != null && OriginalFeatureTriggerResult.payload != null)
        ? OriginalFeatureTriggerResult.payload.freeSpinsRemaining
        : SocketManager.resultData.payload.freeSpinsRemaining;
    SetFreeSpinsCount(remainingSpins);
    yield return new WaitForSeconds(0.5f);
  }

  private IEnumerator ResetUI()
  {
    uiManager.SetTotalWinText("00.00");
    if (IsAutoSpin)
    {
      WasAutoSpinOn = !AutoplayUntilFeature;
      StopAutoSpin();
    }
    StopGameAnimation();
    yield return null;
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

  internal ServerSymbolInfo GetSymbolInfo(int symbolId)
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
      if (symbolId == 13 || symbolId == 14 || symbolId == 15) return false;
      var sym = GetSymbolInfo(symbolId);
      if (sym != null && sym.name != null)
      {
          return sym.name.ToLower().Contains("wild");
      }
      return symbolId == 10;
  }

  internal bool IsSpecialSymbol(int symbolId)
  {
      if (symbolId == 13) return false;
      var sym = GetSymbolInfo(symbolId);
      if (sym != null && sym.name != null)
      {
          string lowerName = sym.name.ToLower();
          return lowerName.Contains("link") || lowerName.Contains("collect") || lowerName.Contains("diamond") || lowerName.Contains("prize") || lowerName.Contains("pollos") || lowerName.Contains("lp");
      }
      return symbolId == 11 || symbolId == 12 || symbolId == 14 || symbolId == 16 || symbolId == 17;
  }

  private Sprite GetSpecialLayerSprite(int symbolId)
  {
      if (symbolId == 13) return null;
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
          else if (lowerName.Contains("pollos") || lowerName.Contains("lp"))
          {
              if (SpecialLayerSymbols.Length > 4) return SpecialLayerSymbols[4];
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
          case 17: // Los Pollos
              return SpecialLayerSymbols.Length > 4 ? SpecialLayerSymbols[4] : null;
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
              if (view != null) view.SetLosPolosValue((int)coin.coinValue);
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
        else if (resultNum == 13) // multiplier coin
        {
          bool found = false;
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
            if (coin.symbolId == 13 && coin.position[0] == row && coin.position[1] == col)
            {
              SlotImage.sprite = SlotSymbols[resultNum];
              if (view != null) view.SetMultiplierCoinValue(coin.coinValue, TotalBet);
              found = true;
              break;
            }
          }
          if (!found)
          {
            SlotImage.sprite = SlotSymbols[resultNum];
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
              if (view != null) view.SetGoldCoinValue(coin.coinValue * TotalBet);
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
        ReorderImages(col);
      }
    }
    UpdateCashCollectIndicators(col);
  }

  private void PopulateResultMatrix()
  {
    for (int col = 0; col < numberOfSlots; col++)
    {
      PopulateResultMatrixForColumn(col);
    }
  }

  private void UpdateCashCollectIndicators(int col)
  {
    if (col < 0 || col >= images.Count) return;
    var imgs = images[col].slotImages;
    if (imgs == null) return;

    // There are 3 visible rows, corresponding to indices 2, 3, 4 of images[col].slotImages
    for (int row = 0; row < 3; row++)
    {
      int fullIndex = 2 + row;
      if (fullIndex >= imgs.Count) continue;
      if (imgs[fullIndex] == null) continue;

      SlotSymbolView view = imgs[fullIndex].GetComponent<SlotSymbolView>();
      if (view == null) continue;

      bool isAbove = false;
      if (fullIndex - 1 >= 0 && imgs[fullIndex - 1] != null)
      {
        isAbove = (imgs[fullIndex - 1].sprite == SlotSymbols[14]);
      }

      bool isBelow = false;
      if (fullIndex + 1 < imgs.Count && imgs[fullIndex + 1] != null)
      {
        isBelow = (imgs[fullIndex + 1].sprite == SlotSymbols[14]);
      }

      if (view.cashCollectAboveObject != null)
      {
        view.cashCollectAboveObject.SetActive(isAbove);
      }
      if (view.cashCollectBelowObject != null)
      {
        view.cashCollectBelowObject.SetActive(isBelow);
      }
    }
  }

  private void UpdateCashCollectIndicatorsForAllColumns()
  {
    for (int col = 0; col < images.Count; col++)
    {
      UpdateCashCollectIndicators(col);
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
      if (SocketManager.resultData.payload.coinPositions[i].symbolId == 17)
      {
        CoinPosition lp = SocketManager.resultData.payload.coinPositions[i];
        uiManager.AddFreeSpinsText((int)lp.coinValue);
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
    SocketManager.isExiting = true;
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

  public void PerformStop()
  {
    if (uiManager != null)
    {
      uiManager.PerformStop();
    }
  }

  #region TweeningCode
  private int[] reelCycleCount = new int[5];
  private const int MinCyclesBeforeStop = 3;
  private int[] stopStatus = { -1, -1, -1, -1, -1 };
  private bool wasStopPressedGlobal = false;
  private bool isSpinReverse = false;

  [SerializeField] private float swipeThreshold = 50f;

  // Classic casino start: brief upward wind-up then immediately into fast continuous spin
  private void InitializeTweening(Transform slotTransform)
  {
    int col = System.Array.IndexOf(Slot_Transform, slotTransform);
    if (col < 0) return;

    reelCycleCount[col] = 0;
    stopStatus[col] = -1;
    float restY = initialYPositions[col];

    SetColumnBackTintActive(col, false);

    if (col == 0)
    {
      col0Stopped = false;
      col0StopTime = 0f;
    }

    while (alltweens.Count <= col) alltweens.Add(null);
    if (alltweens[col] != null) { alltweens[col].Kill(); alltweens[col] = null; }

    if (IsTurboOn)
    {
      RunContinuousCycle(col, slotTransform, restY);
      return;
    }

    // Simple wind-up: small upward/downward bounce then drop straight into continuous spin
    Sequence startSeq = DOTween.Sequence();
    if (isSpinReverse)
    {
      // Reverse wind-up: small downward bounce then drop straight into upward continuous spin
      startSeq.Append(slotTransform.DOLocalMoveY(restY - anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutQuad));
      startSeq.Append(slotTransform.DOLocalMoveY(restY, anticipationUpDuration * 0.5f).SetEase(Ease.InQuad));
    }
    else
    {
      // Normal wind-up: small upward bounce then drop straight into downward continuous spin
      startSeq.Append(slotTransform.DOLocalMoveY(restY + anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutQuad));
      startSeq.Append(slotTransform.DOLocalMoveY(restY, anticipationUpDuration * 0.5f).SetEase(Ease.InQuad));
    }
    startSeq.OnComplete(() => { if (IsSpinning) RunContinuousCycle(col, slotTransform, restY); });
    alltweens[col] = startSeq;
    startSeq.Play();
  }

  // Smooth, fast continuous scrolling — moves multiple symbol heights per tick for blur effect
  private void RunContinuousCycle(int col, Transform slotTransform, float restY)
  {
    if (!IsSpinning) return;

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);

    // Calculate duration from speed: time = distance / speed
    float speed = IsTurboOn ? 3500f : spinSpeed;
    if (col0HasCC && col0Stopped && col > 0)
    {
      float t = Time.time - col0StopTime;
      float currentMultiplier = 1.0f;
      if (t < col0CCSpeedUpDuration)
      {
        float lerpPct = Mathf.Clamp01(t / col0CCSpeedUpDuration);
        currentMultiplier = Mathf.Lerp(1.0f, col0CCSpeedMultiplier, lerpPct);
      }
      else if (t < col0CCSpeedUpDuration + col0CCFastSpinDuration)
      {
        currentMultiplier = col0CCSpeedMultiplier;
      }
      else if (t < col0CCSpeedUpDuration + col0CCFastSpinDuration + col0CCSlowDownDuration)
      {
        float lerpPct = Mathf.Clamp01((t - (col0CCSpeedUpDuration + col0CCFastSpinDuration)) / col0CCSlowDownDuration);
        currentMultiplier = Mathf.Lerp(col0CCSpeedMultiplier, 1.0f, lerpPct);
      }
      else
      {
        currentMultiplier = 1.0f;
      }
      speed *= currentMultiplier;
    }
    float cycleDuration = symbolHeight / speed;
    Ease cycleEase = Ease.Linear;

    if (isNearMissActive && col == nearMissCol)
    {
      if (stopStatus[col] == 0) speed = spinSpeed * 0.8f;
      else if (stopStatus[col] == 1) speed = spinSpeed * 0.65f;
      else if (stopStatus[col] == 2) speed = spinSpeed * 0.5f;
      else if (stopStatus[col] == 3) speed = spinSpeed * 0.35f;
      else if (stopStatus[col] == 4) speed = spinSpeed * 0.25f;

      bool isSlowCycle = (nearMissType == 1 && stopStatus[col] == 4);
      if (isSlowCycle)
      {
        cycleDuration = 2.2f;
        cycleEase = Ease.OutCubic;
      }
      else
      {
        cycleDuration = symbolHeight / speed;
      }
    }

    float targetY = isSpinReverse ? (restY + symbolHeight) : (restY - symbolHeight);

    Tween cycle = slotTransform
        .DOLocalMoveY(targetY, cycleDuration)
        .SetEase(cycleEase)
        .OnComplete(() =>
        {
          if (!IsSpinning) return;
          CycleBufferSymbols(col);
          reelCycleCount[col]++;

          if (stopStatus[col] >= 5)
          {
            PlayStopBounceSequence(col, slotTransform, restY);
          }
          else
          {
            RunContinuousCycle(col, slotTransform, restY);
          }
        });

    alltweens[col] = cycle;
  }

  private void CycleBufferSymbols(int col)
  {
    var imgs = images[col].slotImages;
    if (imgs == null || imgs.Count == 0) return;

    if (isSpinReverse)
    {
      // Shift sprites AND view states UP together so special layers stay in sync
      for (int i = 0; i < imgs.Count - 1; i++)
      {
        imgs[i].sprite = imgs[i + 1].sprite;

        // Sync the SlotSymbolView layers from the source image to the destination
        SlotSymbolView srcView = imgs[i + 1].GetComponent<SlotSymbolView>();
        SlotSymbolView dstView = imgs[i].GetComponent<SlotSymbolView>();
        if (srcView != null && dstView != null)
        {
          CopyViewState(srcView, dstView);
        }
      }

      int lastIdx = imgs.Count - 1;
      int symbolToFeed = -1;
      int targetRowIndex = -1;
      if (stopStatus[col] >= 0)
      {
        if (stopStatus[col] == 0)
        {
          symbolToFeed = GetResultSymbolId(col, 0);
          targetRowIndex = 0;

          if (isNearMissActive && col == nearMissCol && nearMissType == 1)
          {
            int ccIdx = lastIdx - 1;
            if (ccIdx >= 0 && ccIdx < imgs.Count)
            {
              imgs[ccIdx].sprite = SlotSymbols[14];
              SlotSymbolView view = imgs[ccIdx].GetComponent<SlotSymbolView>();
              if (view != null)
              {
                view.ClearValues();
                ConfigureSymbolView(view, 14);
              }
            }
          }
        }
        else if (stopStatus[col] == 1)
        {
          symbolToFeed = GetResultSymbolId(col, 1);
          targetRowIndex = 1;
        }
        else if (stopStatus[col] == 2)
        {
          symbolToFeed = GetResultSymbolId(col, 2);
          targetRowIndex = 2;
        }
        else if (stopStatus[col] == 3 && isNearMissActive && col == nearMissCol && nearMissType == 0)
        {
          symbolToFeed = 14;
        }
        stopStatus[col]++;
      }
      if (symbolToFeed != -1)
      {
        imgs[lastIdx].sprite = SlotSymbols[symbolToFeed];
        SlotSymbolView bottomView = imgs[lastIdx].GetComponent<SlotSymbolView>();
        if (bottomView != null)
        {
          bottomView.ClearValues();
          ConfigureSymbolView(bottomView, symbolToFeed);
        }
        ConfigureSpecialValues(col, GetSourceRowIndex(col, targetRowIndex), symbolToFeed, imgs[lastIdx]);
      }
      else
      {
        // Set a new random (non-special) symbol on the bottom buffer slot
        int r;
        do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
        imgs[lastIdx].sprite = SlotSymbols[r];

        // Clear and configure the view for the new bottom symbol
        SlotSymbolView bottomView = imgs[lastIdx].GetComponent<SlotSymbolView>();
        if (bottomView != null)
        {
          bottomView.ClearValues();
          ConfigureSymbolView(bottomView, r);
        }
      }
    }
    else
    {
      // Shift sprites AND view states down together so special layers stay in sync
      for (int i = imgs.Count - 1; i > 0; i--)
      {
        imgs[i].sprite = imgs[i - 1].sprite;

        // Sync the SlotSymbolView layers from the source image to the destination
        SlotSymbolView srcView = imgs[i - 1].GetComponent<SlotSymbolView>();
        SlotSymbolView dstView = imgs[i].GetComponent<SlotSymbolView>();
        if (srcView != null && dstView != null)
        {
          CopyViewState(srcView, dstView);
        }
      }

      int symbolToFeed = -1;
      int targetRowIndex = -1;
      if (stopStatus[col] >= 0)
      {
        if (stopStatus[col] == 0)
        {
          symbolToFeed = GetResultSymbolId(col, 2);
          targetRowIndex = 2;

          if (isNearMissActive && col == nearMissCol && nearMissType == 1)
          {
            imgs[1].sprite = SlotSymbols[14];
            SlotSymbolView view = imgs[1].GetComponent<SlotSymbolView>();
            if (view != null)
            {
              view.ClearValues();
              ConfigureSymbolView(view, 14);
            }
          }
        }
        else if (stopStatus[col] == 1)
        {
          symbolToFeed = GetResultSymbolId(col, 1);
          targetRowIndex = 1;
        }
        else if (stopStatus[col] == 2)
        {
          symbolToFeed = GetResultSymbolId(col, 0);
          targetRowIndex = 0;
        }
        else if (stopStatus[col] == 3 && isNearMissActive && col == nearMissCol && nearMissType == 0)
        {
          symbolToFeed = 14;
        }
        stopStatus[col]++;
      }

      if (symbolToFeed != -1)
      {
        imgs[0].sprite = SlotSymbols[symbolToFeed];
        SlotSymbolView topView = imgs[0].GetComponent<SlotSymbolView>();
        if (topView != null)
        {
          topView.ClearValues();
          ConfigureSymbolView(topView, symbolToFeed);
        }
        ConfigureSpecialValues(col, GetSourceRowIndex(col, targetRowIndex), symbolToFeed, imgs[0]);
      }
      else
      {
        // Set a new random (non-special) symbol on the top buffer slot
        int r;
        do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
        imgs[0].sprite = SlotSymbols[r];

        // Clear and configure the view for the new top symbol
        SlotSymbolView topView = imgs[0].GetComponent<SlotSymbolView>();
        if (topView != null)
        {
          topView.ClearValues();
          ConfigureSymbolView(topView, r);
        }
      }
    }

    // Apply near-miss or magnet buffer override on the final stop cycle
    if (stopStatus[col] == 5)
    {
      // 1. Top Near Miss / Magnet Pull Down (stops at top buffer, i.e., index 1)
      bool isNearMissTopActive = isNearMissActive && col == nearMissCol && 
                                 (isSpinReverse ? (nearMissType == 1) : (nearMissType == 0));
      bool shouldShowAtTop = (isMagnetScenarioActive && col == magnetCol && magnetRow == 0) || isNearMissTopActive;
      if (shouldShowAtTop)
      {
        imgs[1].sprite = SlotSymbols[14];
        SlotSymbolView topView = imgs[1].GetComponent<SlotSymbolView>();
        if (topView != null)
        {
          topView.ClearValues();
          ConfigureSymbolView(topView, 14);
        }
      }

      // 2. Bottom Near Miss / Magnet Pull Up (stops at bottom buffer, i.e., index 5)
      bool isNearMissBottomActive = isNearMissActive && col == nearMissCol && 
                                    (isSpinReverse ? (nearMissType == 0) : (nearMissType == 1));
      bool shouldShowAtBottom = (isMagnetScenarioActive && col == magnetCol && magnetRow == 2) || isNearMissBottomActive;
      if (shouldShowAtBottom && imgs.Count > 5)
      {
        imgs[5].sprite = SlotSymbols[14];
        SlotSymbolView bottomView = imgs[5].GetComponent<SlotSymbolView>();
        if (bottomView != null)
        {
          bottomView.ClearValues();
          ConfigureSymbolView(bottomView, 14);
        }
      }
    }
  }

  private int GetSourceRowIndex(int col, int targetRowIndex)
  {
    if (isMagnetScenarioActive && col == magnetCol)
    {
      if (magnetRow == 0) // top row, nudge down
      {
        if (targetRowIndex == 0) return 1;
        if (targetRowIndex == 1) return 2;
      }
      else if (magnetRow == 2) // bottom row, nudge up
      {
        if (targetRowIndex == 1) return 0;
        if (targetRowIndex == 2) return 1;
      }
    }
    return targetRowIndex;
  }

  private int GetResultSymbolIdInternal(int col, int row)
  {
    if (SocketManager.resultData == null || SocketManager.resultData.matrix == null)
      return UnityEngine.Random.Range(0, 9);
    int resultNum = int.Parse(SocketManager.resultData.matrix[row][col]);
    if (resultNum == 9) // BLANK SYMBOL - NEEDS RANDOM SYMBOL
    {
      return UnityEngine.Random.Range(0, 9); // 0 to 8 inclusive
    }
    return resultNum;
  }

  private int GetResultSymbolId(int col, int row)
  {
    if (isMagnetScenarioActive && col == magnetCol)
    {
      if (magnetRow == 0) // top row, nudge down
      {
        if (row == 0) return GetResultSymbolIdInternal(col, 1);
        if (row == 1) return GetResultSymbolIdInternal(col, 2);
        if (row == 2)
        {
          int r;
          do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
          return r;
        }
      }
      else if (magnetRow == 2) // bottom row, nudge up
      {
        if (row == 0)
        {
          int r;
          do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
          return r;
        }
        if (row == 1) return GetResultSymbolIdInternal(col, 0);
        if (row == 2) return GetResultSymbolIdInternal(col, 1);
      }
    }

    return GetResultSymbolIdInternal(col, row);
  }

  private void ConfigureSpecialValues(int col, int row, int symbolId, Image img)
  {
    SlotSymbolView view = img.GetComponent<SlotSymbolView>();
    if (view == null) return;

    if (symbolId == 17) // LP coin
    {
      bool found = false;
      if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.coinPositions != null)
      {
        foreach (var coin in SocketManager.resultData.payload.coinPositions)
        {
          if (coin.symbolId == 17 && coin.position[0] == row && coin.position[1] == col)
          {
            view.SetLosPolosValue((int)coin.coinValue);
            found = true;
            break;
          }
        }
      }
      if (!found)
      {
        int[] tempIndex = { 2, 3, 4, 5, 7 };
        int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
        view.SetLosPolosValue(randomIndex);
      }
    }
    else if (symbolId == 13) // multiplier coin
    {
      if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.coinPositions != null)
      {
        foreach (var coin in SocketManager.resultData.payload.coinPositions)
        {
          if (coin.symbolId == 13 && coin.position[0] == row && coin.position[1] == col)
          {
            view.SetMultiplierCoinValue(coin.coinValue, TotalBet);
            break;
          }
        }
      }
    }
    else if (symbolId == 15) // gold coin
    {
      if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.coinPositions != null)
      {
        foreach (var coin in SocketManager.resultData.payload.coinPositions)
        {
          if (coin.position[0] == row && coin.position[1] == col)
          {
            view.SetGoldCoinValue(coin.coinValue * TotalBet);
            break;
          }
        }
      }
    }
  }

  private IEnumerator TriggerReelStopAfterDelay(int col, float delay)
  {
    if (delay > 0)
    {
      yield return new WaitForSeconds(delay);
    }
    stopStatus[col] = 0;
  }

  private void PlayStopBounceSequence(int col, Transform slotTransform, float restY)
  {
    if (AudioController.Instance != null) AudioController.Instance.PlaySlotStop();
    if (alltweens[col] != null) { alltweens[col].Kill(); alltweens[col] = null; }

    // For bottom near-miss: snap to restY immediately since the slow glide of cycle 4 already completed the stop
    if (isNearMissActive && col == nearMissCol && nearMissType == 1)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);
      OnReelStopped(col);
      return;
    }

    // For top near-miss: slowly glide down from restY to restY - 0.65f * symbolHeight,
    // then smoothly reverse/glide UP back to restY when it reaches 65% of the symbol height.
    if (isNearMissActive && col == nearMissCol && nearMissType == 0)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);
      Sequence glideSeq = DOTween.Sequence();
      float targetBounceY = isSpinReverse ? (restY + 0.65f * symbolHeight) : (restY - 0.65f * symbolHeight);
      glideSeq.Append(slotTransform.DOLocalMoveY(targetBounceY, 1.1f).SetEase(Ease.OutQuad));
      glideSeq.Append(slotTransform.DOLocalMoveY(restY, 1.3f).SetEase(Ease.InOutQuad));
      glideSeq.OnComplete(() => OnReelStopped(col));
      alltweens[col] = glideSeq;
      glideSeq.Play();
      return;
    }

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);

    Sequence stopSeq = DOTween.Sequence();

    if (IsTurboOn || wasStopPressedGlobal)
    {
      // Quick stop: small overshoot then snap back
      float targetOvershoot = isSpinReverse ? (restY + quickStopOvershoot) : (restY - quickStopOvershoot);
      stopSeq.Append(slotTransform.DOLocalMoveY(targetOvershoot, quickStopDuration * 0.3f).SetEase(Ease.OutQuad));
      stopSeq.Append(slotTransform.DOLocalMoveY(restY,                      quickStopDuration * 0.7f).SetEase(Ease.InOutQuad));
    }
    else
    {
      // Classic casino stop: overshoot then smooth settle back to rest
      float overshootDistance = stopOvershootDistance;
      float overshootDuration = stopOvershootDuration;
      float settleDuration = stopSettleDuration;

      float targetOvershoot = isSpinReverse ? (restY + overshootDistance) : (restY - overshootDistance);
      stopSeq.Append(slotTransform.DOLocalMoveY(targetOvershoot, overshootDuration).SetEase(Ease.OutQuad));
      stopSeq.Append(slotTransform.DOLocalMoveY(restY,                         settleDuration   ).SetEase(Ease.InOutQuad));
    }

    stopSeq.OnComplete(() => OnReelStopped(col));
    alltweens[col] = stopSeq;
    stopSeq.Play();
  }

  private IEnumerator PlayMagnetSequence()
  {
    if (!isMagnetScenarioActive || magnetCol < 0 || magnetCol >= Slot_Transform.Length) yield break;

    GameObject activeMagnet = null;
    if (magnetCol == 0) // Left
    {
      activeMagnet = (magnetRow == 0) ? leftBottomMagnet : leftTopMagnet;
    }
    else if (magnetCol == 4) // Right
    {
      activeMagnet = (magnetRow == 0) ? rightBottomMagnet : rightTopMagnet;
    }

    Vector3 initialMagnetPos = Vector3.zero;
    if (activeMagnet != null)
    {
      if (AudioController.Instance != null) AudioController.Instance.PlayMagnetOn();
      initialMagnetPos = activeMagnet.transform.localPosition;
      activeMagnet.SetActive(true);
    }

    Debug.Log($"[FakeScenario] Magnet sequence playing! Activating magnet on Reel {magnetCol}, Opposite row {((magnetRow == 0) ? "Bottom" : "Top")}");

    yield return new WaitForSeconds(magnetAnimDuration);

    Transform reelTrans = Slot_Transform[magnetCol];
    float startY = initialYPositions[magnetCol];
    float targetY = startY + (magnetNudgeDir * symbolHeight);

    bool nudgeComplete = false;

    // Move the active magnet backward during the nudge to simulate pulling tension, matching the slot's movement
    if (activeMagnet != null)
    {
      float pullbackOffset = magnetNudgeDir * symbolHeight;
      activeMagnet.transform.DOLocalMoveY(initialMagnetPos.y + pullbackOffset, magnetNudgeDuration)
        .SetEase(Ease.OutBack);
    }

    reelTrans.DOLocalMoveY(targetY, magnetNudgeDuration)
      .SetEase(Ease.OutBack)
      .OnComplete(() => {
        nudgeComplete = true;
      });

    yield return new WaitUntil(() => nudgeComplete);

    if (activeMagnet != null)
    {
      if (AudioController.Instance != null) AudioController.Instance.StopMagnetOn();
      activeMagnet.SetActive(false);
      activeMagnet.transform.localPosition = initialMagnetPos; // Restore position for future use
    }

    isMagnetScenarioActive = false;

    PopulateResultMatrixForColumn(magnetCol);
    reelTrans.localPosition = new Vector3(reelTrans.localPosition.x, startY, 0f);

    Debug.Log($"[FakeScenario] Magnet nudge complete! Reel {magnetCol} populated with final correct server symbols.");

    // Play all special landing animations on the magnet column now that it has settled!
    System.Func<string, bool> isSpecial = id =>
        id == "11" || id == "12" || id == "13" || id == "14" ||
        id == "15" || id == "16" || id == "17";

    for (int row = 0; row < 3; row++)
    {
        if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
            row < SocketManager.resultData.matrix.Count && magnetCol < SocketManager.resultData.matrix[row].Count)
        {
            string symbolIdStr = SocketManager.resultData.matrix[row][magnetCol];
            if (isSpecial(symbolIdStr))
            {
                animationManager.PlaySpecialAnimationForCell(row, magnetCol);
            }
        }
    }

    yield return new WaitForSeconds(0.2f);
  }

  private void OnReelStopped(int col)
  {
      if (isMagnetScenarioActive && col == magnetCol)
      {
          // Delay all landing animations on this column until the magnet nudge is complete!
          return;
      }

      if (col == 0)
      {
          col0Stopped = true;
          col0StopTime = Time.time;
      }

      if (col0HasCC)
      {
          if (col == 0)
          {
              for (int c = 1; c < numberOfSlots; c++)
              {
                  SetColumnBackTintActive(c, true);
              }
              if (AudioController.Instance != null) AudioController.Instance.PlayMultiTension();
          }
      }

      // Single reel nearmiss tension builder triggers
      if (isNearMissActive && col == nearMissCol - 1)
      {
          if (AudioController.Instance != null) AudioController.Instance.PlaySingleTension();
      }
      if (isNearMissActive && col == nearMissCol)
      {
          if (AudioController.Instance != null) AudioController.Instance.StopSingleTension();
      }

      // 4 slot tension builder stop trigger
      if (col == numberOfSlots - 1 && col0HasCC)
      {
          if (AudioController.Instance != null) AudioController.Instance.StopMultiTension();
      }

      System.Func<string, bool> isSpecial = id =>
          id == "11" || id == "12" || id == "13" || id == "14" ||
          id == "15" || id == "16" || id == "17";

      for (int row = 0; row < 3; row++)
      {
          if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
              row < SocketManager.resultData.matrix.Count && col < SocketManager.resultData.matrix[row].Count)
          {
              string symbolIdStr = SocketManager.resultData.matrix[row][col];
              if (int.TryParse(symbolIdStr, out int symbolId))
              {
                  if (symbolId == 15 || symbolId == 16) // Gold Coin / Cash Coin or Prize Coin
                  {
                      if (AudioController.Instance != null) AudioController.Instance.PlayCashCoinLand();
                  }
                  else if (symbolId == 11 || symbolId == 12) // Link / MegaLink
                  {
                      if (AudioController.Instance != null) AudioController.Instance.PlayLinkLand();
                  }
                  else if (symbolId == 14 || symbolId == 17) // Cash Collect or Los Pollos
                  {
                      if (AudioController.Instance != null) AudioController.Instance.PlayCashCollectLand();
                  }
              }

              if (isSpecial(symbolIdStr))
              {
                  animationManager.PlaySpecialAnimationForCell(row, col);
              }
          }
      }
      UpdateCashCollectIndicators(col);
  }

  // Copies the visual state (special layer, hat, value texts) from one view to another
  private void CopyViewState(SlotSymbolView src, SlotSymbolView dst)
  {
    // Special symbol layer
    if (dst.specialSymbolLayer != null && src.specialSymbolLayer != null)
    {
      dst.specialSymbolLayer.gameObject.SetActive(src.specialSymbolLayer.gameObject.activeSelf);
      dst.specialSymbolLayer.sprite = src.specialSymbolLayer.sprite;
    }
    else if (dst.specialSymbolLayer != null)
    {
      dst.specialSymbolLayer.gameObject.SetActive(false);
    }

    // Hat / wild object
    if (dst.hatObject != null && src.hatObject != null)
    {
      dst.hatObject.SetActive(src.hatObject.activeSelf);
    }
    else if (dst.hatObject != null)
    {
      dst.hatObject.SetActive(false);
    }

    // Gold coin value text
    if (dst.goldCoinValueText != null && src.goldCoinValueText != null)
    {
      dst.goldCoinValueText.gameObject.SetActive(src.goldCoinValueText.gameObject.activeSelf);
      dst.goldCoinValueText.text = src.goldCoinValueText.text;
    }
    else if (dst.goldCoinValueText != null)
    {
      dst.goldCoinValueText.gameObject.SetActive(false);
    }

    // Multiplier coin value text
    if (dst.multiplierValueText != null && src.multiplierValueText != null)
    {
      dst.multiplierValueText.gameObject.SetActive(src.multiplierValueText.gameObject.activeSelf);
      dst.multiplierValueText.text = src.multiplierValueText.text;
    }
    else if (dst.multiplierValueText != null)
    {
      dst.multiplierValueText.gameObject.SetActive(false);
    }

    // Los Polos value text
    if (dst.losPolosValueText != null && src.losPolosValueText != null)
    {
      dst.losPolosValueText.gameObject.SetActive(src.losPolosValueText.gameObject.activeSelf);
      dst.losPolosValueText.text = src.losPolosValueText.text;
    }
    else if (dst.losPolosValueText != null)
    {
      dst.losPolosValueText.gameObject.SetActive(false);
    }

    // Jackpot object
    if (dst.jackpotObject != null && src.jackpotObject != null)
    {
      dst.jackpotObject.SetActive(src.jackpotObject.activeSelf);
    }
    else if (dst.jackpotObject != null)
    {
      dst.jackpotObject.SetActive(false);
    }

    // Jackpot result text
    if (dst.jackpotResultText != null && src.jackpotResultText != null)
    {
      dst.jackpotResultText.gameObject.SetActive(src.jackpotResultText.gameObject.activeSelf);
      dst.jackpotResultText.text = src.jackpotResultText.text;
    }
    else if (dst.jackpotResultText != null)
    {
      dst.jackpotResultText.gameObject.SetActive(false);
    }

    // Jackpot strip parent children
    if (dst.jackpotStripParent != null && src.jackpotStripParent != null)
    {
      dst.jackpotStripParent.gameObject.SetActive(src.jackpotStripParent.gameObject.activeSelf);
      int childCount = Mathf.Min(dst.jackpotStripParent.childCount, src.jackpotStripParent.childCount);
      for (int i = 0; i < childCount; i++)
      {
        Transform dstItem = dst.jackpotStripParent.GetChild(i);
        Transform srcItem = src.jackpotStripParent.GetChild(i);
        if (dstItem != null && srcItem != null)
        {
          dstItem.gameObject.SetActive(srcItem.gameObject.activeSelf);
          
          Image dstBgImg = dstItem.GetComponent<Image>();
          Image srcBgImg = srcItem.GetComponent<Image>();
          if (dstBgImg != null && srcBgImg != null)
          {
            dstBgImg.sprite = srcBgImg.sprite;
          }

          if (dstItem.childCount > 0 && srcItem.childCount > 0)
          {
            Image dstTextImg = dstItem.GetChild(0).GetComponent<Image>();
            Image srcTextImg = srcItem.GetChild(0).GetComponent<Image>();
            if (dstTextImg != null && srcTextImg != null)
            {
              dstTextImg.sprite = srcTextImg.sprite;
              dstTextImg.gameObject.SetActive(srcTextImg.gameObject.activeSelf);
            }
          }
        }
      }
    }
    else if (dst.jackpotStripParent != null)
    {
      dst.jackpotStripParent.gameObject.SetActive(false);
    }

    // Cash Collect Above
    if (dst.cashCollectAboveObject != null && src.cashCollectAboveObject != null)
    {
      dst.cashCollectAboveObject.SetActive(src.cashCollectAboveObject.activeSelf);
    }
    else if (dst.cashCollectAboveObject != null)
    {
      dst.cashCollectAboveObject.SetActive(false);
    }

    // Cash Collect Below
    if (dst.cashCollectBelowObject != null && src.cashCollectBelowObject != null)
    {
      dst.cashCollectBelowObject.SetActive(src.cashCollectBelowObject.activeSelf);
    }
    else if (dst.cashCollectBelowObject != null)
    {
      dst.cashCollectBelowObject.SetActive(false);
    }

    // Canvas group alpha
    if (dst.canvasGroup != null && src.canvasGroup != null)
    {
      dst.canvasGroup.alpha = src.canvasGroup.alpha;
    }
    else if (dst.canvasGroup != null)
    {
      dst.canvasGroup.alpha = 1f;
    }
  }




  private void KillAllTweens()
  {
    foreach (var t in alltweens) t?.Kill();
    alltweens.Clear();
  }
  #endregion
}
