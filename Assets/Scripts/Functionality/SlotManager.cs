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
  [SerializeField] private Sprite[] LP2_Sprites;
  [SerializeField] private Sprite[] LP3_Sprites;
  [SerializeField] private Sprite[] LP4_Sprites;
  [SerializeField] private Sprite[] LP5_Sprites;
  [SerializeField] private Sprite[] LP7_Sprites;
  [SerializeField] private Sprite[] GoldCoin_Sprites;

  private List<Tween> alltweens = new List<Tween>();
  private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

  private Coroutine tweenroutine;
  private Coroutine LineAnimRoutine = null;
  private bool isSettling = false; // cooldown flag after stop button
  internal bool IsFeatureTransitioning = false;

  int tweenHeight = 0;  //calculate the height at which tweening is done
  private int numberOfSlots = 5;          //number of columns
  [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing
  [SerializeField] private float stopCooldownDuration = 0.4f; // cooldown after stop before next spin

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
  public void StartSlots(bool autoSpin = false, bool reverse = false)
  {
    isSpinReverse = reverse;
    IsFeatureTransitioning = false;
    // Forcefully clean up any previous spin state to prevent glitches
    ForceCleanupPreviousSpin();

    if (IsFreeSpin)
    {
      SetFreeSpinsCount(FreeSpinsCount - 1);
    }

    uiManager.SetButtonsInteractable(false);
    uiManager.SetTotalWinText("0.000");

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

    // Kill all running tweens
    KillAllTweens();

    // Reset all slot transforms to their initial Y positions to prevent positional drift
    ResetSlotPositions();

    // Reset spin flags
    StopSpinToggle = false;
    isSettling = false;
    TriggerSpinState(false);
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
      yield return new WaitForSeconds(0.3f);
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

    // Start stopping each reel in parallel
    wasStopPressedGlobal = wasStopPressed;
    for (int i = 0; i < numberOfSlots; i++)
    {
      float delay = i * ((IsTurboOn || wasStopPressed) ? 0.05f : reelStopStagger);
      StartCoroutine(TriggerReelStopAfterDelay(i, delay));
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
    float stagger = (IsTurboOn || wasStopPressed) ? 0.05f : reelStopStagger;
    float cycleDuration = symbolHeight / spinSpeed;
    float longestStopTime = (IsTurboOn || wasStopPressed)
        ? ((numberOfSlots - 1) * stagger + 5f * cycleDuration + quickStopDuration)
        : ((numberOfSlots - 1) * stagger + 5f * cycleDuration + stopOvershootDuration + stopSettleDuration);

    yield return new WaitForSeconds(longestStopTime + 0.05f);
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

    if (SocketManager.resultData.payload.isLinkTriggered)
    {
      IsBonus = true;
      IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();

      // Pause FreeSpins if already running
      if (IsFreeSpin)
      {
        WasAutoSpinOn = true;
        IsFreeSpin = false;
      }

      yield return ResetUI();

      _bonusManager.StartBonus(SocketManager.resultData.payload.linkRespinsRemaining);
      TriggerSpinState(false);
      yield break;   // EXIT AFTER LINK STARTS
    }

    if (isCCTriggered)
    {
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
            yield return jackpotManager.PlayJackpotSequence(view, item.prizeTypeIndex ?? 0, item.coinValue.ToString("F2"), prizeSprite);
          }
        }
      }
      yield return new WaitForSeconds(1.2f);
      yield return new WaitForSeconds(.2f);

      if (!SocketManager.resultData.payload.isFreeSpinTriggered)
      {
        IsFeatureTransitioning = false;
        uiManager.UpdateButtonsState();
      }
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
      IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();

      int extraFreeSpin = 0;
      yield return new WaitForSeconds(.5f);
      if (SocketManager.resultData.payload.freeSpinsRemaining > FreeSpinsCount)
      {
        yield return FreeSpinsSymbolAnimation();
        extraFreeSpin = SocketManager.resultData.payload.freeSpinsRemaining - FreeSpinsCount;
      }

      SetFreeSpinsCount(SocketManager.resultData.payload.freeSpinsRemaining);
      yield return new WaitForSeconds(1f);

      // Mid-game image animations removed for now
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
    uiManager.SetTotalWinText("0.000");
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
        ReorderImages(col);
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
      if (SocketManager.resultData.payload.coinPositions[i].symbolId == 17)
      {
        CoinPosition lp = SocketManager.resultData.payload.coinPositions[i];
        uiManager.AddFreeSpinsText(lp.coinValue);
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

    while (alltweens.Count <= col) alltweens.Add(null);
    if (alltweens[col] != null) { alltweens[col].Kill(); alltweens[col] = null; }

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
    float cycleDuration = symbolHeight / spinSpeed;

    float targetY = isSpinReverse ? (restY + symbolHeight) : (restY - symbolHeight);

    Tween cycle = slotTransform
        .DOLocalMoveY(targetY, cycleDuration)
        .SetEase(Ease.Linear)
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

      int symbolToFeed = -1;
      int targetRowIndex = -1;
      if (stopStatus[col] >= 0)
      {
        if (stopStatus[col] == 0)
        {
          symbolToFeed = GetResultSymbolId(col, 0);
          targetRowIndex = 0;
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
        stopStatus[col]++;
      }

      int lastIdx = imgs.Count - 1;
      if (symbolToFeed != -1)
      {
        imgs[lastIdx].sprite = SlotSymbols[symbolToFeed];
        SlotSymbolView bottomView = imgs[lastIdx].GetComponent<SlotSymbolView>();
        if (bottomView != null)
        {
          bottomView.ClearValues();
          ConfigureSymbolView(bottomView, symbolToFeed);
        }
        ConfigureSpecialValues(col, targetRowIndex, symbolToFeed, imgs[lastIdx]);
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
        ConfigureSpecialValues(col, targetRowIndex, symbolToFeed, imgs[0]);
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
  }

  private int GetResultSymbolId(int col, int row)
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
            view.SetLosPolosValue(coin.coinValue);
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
    else if (symbolId == 15) // gold coin
    {
      if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.coinPositions != null)
      {
        foreach (var coin in SocketManager.resultData.payload.coinPositions)
        {
          if (coin.position[0] == row && coin.position[1] == col)
          {
            view.SetGoldCoinValue(coin.coinValue);
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
    if (alltweens[col] != null) { alltweens[col].Kill(); alltweens[col] = null; }

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
      float targetOvershoot = isSpinReverse ? (restY + stopOvershootDistance) : (restY - stopOvershootDistance);
      stopSeq.Append(slotTransform.DOLocalMoveY(targetOvershoot, stopOvershootDuration).SetEase(Ease.OutQuad));
      stopSeq.Append(slotTransform.DOLocalMoveY(restY,                         stopSettleDuration   ).SetEase(Ease.InOutQuad));
    }

    alltweens[col] = stopSeq;
    stopSeq.Play();
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
  }




  private void KillAllTweens()
  {
    foreach (var t in alltweens) t?.Kill();
    alltweens.Clear();
  }
  #endregion
}
