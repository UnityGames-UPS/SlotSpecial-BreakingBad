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
  
  internal int BetCounter { get; private set; }
  internal bool IsSpinning { get; set; }
  internal bool IsAutoSpin { get; set; }
  internal bool IsFreeSpin { get; set; }
  internal bool IsBonus { get; set; }
  internal bool IsTurboOn { get; set; }
  internal bool WasAutoSpinOn { get; set; }
  internal bool WasFreeSpinPaused { get; set; }  
  internal bool StopSpinToggle { get; set; }
  internal bool CheckPopups { get; set; }
  internal int AutoplayCount { get; private set; }
  internal bool AutoplayUntilFeature { get; private set; }
  internal bool IsAutoplayStoppedMidSpin { get; set; }

  private Coroutine autoplayRoutine;

  
  internal FeatureQueue featureQueue = new FeatureQueue();

  internal InitData InitialData { get; private set; }
  internal ServerSpinResponse ResultData { get; private set; }
  internal ServerSpinResponse OriginalFeatureTriggerResult { get; private set; }
  internal ServerPlayer PlayerData { get; private set; }

  
  internal double Balance => PlayerData != null ? PlayerData.balance : 0;
  
  internal double LineBet => (InitialData != null && InitialData.gameData != null && BetCounter < InitialData.gameData.bets.Count) 
      ? InitialData.gameData.bets[BetCounter] : 0;
  
  internal double TotalBet => LineBet * (InitialData != null && InitialData.gameData != null ? InitialData.gameData.totalLines : 0);
  
  internal int FreeSpinsCount => ResultData != null && ResultData.payload != null ? ResultData.payload.freeSpinsRemaining : 0;
  
  internal int LinkRespinsRemaining => ResultData != null && ResultData.payload != null ? ResultData.payload.linkRespinsRemaining : 0;
  
  internal double WinAmount => ResultData != null && ResultData.payload != null ? ResultData.payload.winAmount : 0;

  
  internal event Action<double> OnBalanceChanged;
  internal event Action<double> OnTotalBetChanged;
  internal event Action<double> OnLineBetChanged;
  internal event Action<int> OnFreeSpinsChanged;
  internal event Action<int> OnLinkRespinsChanged;
  internal event Action OnBetChanged;
  internal event Action<bool> OnSpinStateChanged;
  internal event Action<bool> OnAutoSpinStateChanged;
  internal event Action<int, bool> OnAutoplayCountChanged;
  internal event Action OnAutoplayStopped;

  internal void Initialize(InitData data)
  {
      InitialData = data;
      PlayerData = data.player;
      UpdateBalance(data.player.balance);
      SetBetIndex(0);
  }

  internal void UpdateBalance(double newBalance)
  {
      if (PlayerData == null) PlayerData = new ServerPlayer();
      PlayerData.balance = newBalance;
      OnBalanceChanged?.Invoke(Balance);
  }

  internal void SetBetIndex(int index)
  {
      if (InitialData == null || InitialData.gameData == null || InitialData.gameData.bets == null) return;
      
      var bets = InitialData.gameData.bets;
      if (index < 0 || index >= bets.Count) return;

      BetCounter = index;

      OnLineBetChanged?.Invoke(LineBet);
      OnTotalBetChanged?.Invoke(TotalBet);
      OnBetChanged?.Invoke();
  }

  internal void SetFreeSpinsCount(int count)
  {
      if (ResultData != null && ResultData.payload != null)
      {
          ResultData.payload.freeSpinsRemaining = count;
      }
      OnFreeSpinsChanged?.Invoke(FreeSpinsCount);
  }

  internal void SetLinkRespinsRemaining(int count)
  {
      if (ResultData != null && ResultData.payload != null)
      {
          ResultData.payload.linkRespinsRemaining = count;
      }
      OnLinkRespinsChanged?.Invoke(LinkRespinsRemaining);
  }

  internal void UpdateFromSpinResult(ServerSpinResponse result)
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

  internal void TriggerSpinState(bool isSpinning)
  {
      IsSpinning = isSpinning;
      OnSpinStateChanged?.Invoke(IsSpinning);
  }

  internal void TriggerAutoSpinState(bool isAutoSpin)
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
  [SerializeField] internal JackpotManager jackpotManager;
  [SerializeField] private PopupManager popupManager;

  [Header("Sprites References")]
  [SerializeField] internal Sprite[] SlotSymbols;  
  [SerializeField] internal Sprite[] JackpotSlotSymbols;
  [SerializeField] private Sprite[] SpecialLayerSymbols; 

  [Header("Slot References")]
  [SerializeField] private List<SlotImage> images;     
  internal List<SlotImage> Tempimages;     
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
  [SerializeField] private float spinSpeed              = 2000f; 

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
  private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  

  private Coroutine tweenroutine;
  private Coroutine LineAnimRoutine = null;
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

  
  private bool isMagnetScenarioActive = false;
  private int magnetCol = -1;
  private int magnetRow = -1;
  private float magnetNudgeDir = 0f; 

  
  private bool isNearMissActive = false;
  private int nearMissCol = -1;
  private int nearMissType = -1; 
  [SerializeField] private float nearMissExtraSpinDuration = 1.5f; 
  [SerializeField] private float col0CCExtraSpinDuration = 3.5f; 
  [SerializeField] private float col0CCSpeedMultiplier = 1.5f; 
  [SerializeField] private float col0CCSpeedUpDuration = 1.0f; 
  [SerializeField] private float col0CCFastSpinDuration = 1.5f; 
  [SerializeField] private float col0CCSlowDownDuration = 1.0f; 
  [SerializeField] [Range(0f, 1f)] private float col0CCTriggerChance = 0.5f; 
  private bool col0HasCC = false;
  private bool col0Stopped = false;
  private float col0StopTime = 0f;

  [Header("Early Reveal Scenario References")]
  [SerializeField] private float earlyRevealExtraSpinDuration = 3.0f;
  [SerializeField] private bool testEarlyRevealScenario = false;
  [SerializeField] [Range(0f, 1f)] private float specialEarlyRevealChance = 0.45f;
  [SerializeField] private Sprite[] smokeEffectSprites;
  [SerializeField] private Sprite[] iceBreakingEffectSprites;
  private bool isEarlyRevealActive = false;
  private List<List<int>> earlyRevealPositions = new List<List<int>>();

  int tweenHeight = 0;  
  private int numberOfSlots = 5;          
  [SerializeField] private int IconSizeFactor = 100;       
  [SerializeField] private float stopCooldownDuration = 0.4f; 

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

  internal SlotSymbolView GetSymbolView(int row, int col)
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

      
      if (ResultMatrix != null && row >= 0 && row < ResultMatrix.Count)
      {
          if (col >= 0 && col < ResultMatrix[row].slotImages.Count)
          {
              return ResultMatrix[row].slotImages[col].GetComponent<SlotSymbolView>();
          }
      }
      return null;
  }

  internal Image GetResultMatrixImage(int row, int col)
  {
      return ResultMatrix[row].slotImages[col];
  }

  internal CoinPosition GetCoinPosition(int row, int col)
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

  internal void EnableAllBackTints(bool active, float alpha = 0.85f)
  {
      for (int col = 0; col < images.Count; col++)
      {
          if (images[col] == null || images[col].slotImages == null) continue;
          for (int row = 0; row < images[col].slotImages.Count; row++)
          {
              Image img = images[col].slotImages[row];
              if (img != null)
              {
                  SlotSymbolView view = img.GetComponent<SlotSymbolView>();
                  if (view != null)
                  {
                      view.SetBackTintActive(active, alpha);
                  }
              }
          }
      }
  }

  internal void SetColumnBackTintActive(int col, bool active, float alpha = 0.85f)
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

  internal float SwipeThresholdValue => swipeThreshold;

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
      
      stickySymbolManager.UpdateLockedCashCollects(savedLockedCashCollects);
      savedLockedCashCollects = null;
    }

    StartSlots();
  }

  #region Autospin
  internal void StartAutoplay(int count, bool untilFeature)
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

  internal void StopAutoSpin()
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

  internal void ChangeBet(bool IncDec)
  {
    int counter = BetCounter;
    if (IncDec)
    {
      counter++;
      if (counter >= SocketManager.initialData.gameData.bets.Count)
      {
        counter = 0; 
      }
    }
    else
    {
      counter--;
      if (counter < 0)
      {
        counter = SocketManager.initialData.gameData.bets.Count - 1; 
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

    if (DialoguePopupManager.Instance != null)
    {
      StartCoroutine(DialoguePopupManager.Instance.PlayGameStartDialogue());
    }
  }
  #endregion

  private void ReorderImages(int targetCol)
  {
    
    
    for (int j = 0; j < 3; j++)
    {
      if (Tempimages[targetCol].slotImages[j].sprite == SlotSymbols[14]) 
      {
        
        Transform slotTransform = Tempimages[targetCol].slotImages[j].transform;
        int originalSiblingIndex = slotTransform.GetSiblingIndex();

        
        changedSlots.Add((slotTransform, originalSiblingIndex));

        
        SetUpAccordingToCC(slotTransform);
      }
    }
  }

  private void SetUpAccordingToCC(Transform slotTransform)
  {
    if (slotTransform == null) return;
    

    
    
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
        animation.AnimationSpeed = 15;  
        Image image = child.GetComponent<Image>();
        if (image != null)
        {
          image.DOFade(0, 0);
          image.gameObject.SetActive(true);  
          image.DOFade(1, 0.5f);
        }
        animation.StartAnimation();     
      }
    }
  }

  
  private void ResetImages()
  {
    foreach (var (slotTransform, originalSiblingIndex) in changedSlots)
    {
      if (slotTransform == null) continue;

      
      Canvas canvas = slotTransform.GetComponent<Canvas>();
      if (canvas != null)
      {
        Destroy(canvas);
      }
      
      
      int childCount = slotTransform.childCount;
      for (int i = 0; i < Mathf.Min(childCount, 2); i++)
      {
        var child = slotTransform.GetChild(i);
        var animation = child.GetComponent<ImageAnimation>();
        if (animation != null)
        {
          animation.StopAnimation();  
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

    
    changedSlots.Clear();
  }

  
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
  
  internal void StartSlots(bool autoSpin = false, bool reverse = false)
  {
    isSpinReverse = reverse;
    IsFeatureTransitioning = false;
    
    ForceCleanupPreviousSpin();

    if (!IsFreeSpin && stickySymbolManager != null)
    {
      stickySymbolManager.Reset();
    }

    IsAutoplayStoppedMidSpin = false;

    
    isMagnetScenarioActive = false;
    magnetCol = -1;
    magnetRow = -1;
    magnetNudgeDir = 0f;
    isNearMissActive = false;
    nearMissCol = -1;
    nearMissType = -1;
    isEarlyRevealActive = false;
    earlyRevealPositions.Clear();

    if (IsFreeSpin)
    {
      SetFreeSpinsCount(FreeSpinsCount - 1);
    }

    uiManager.SetButtonsInteractable(false);
    uiManager.SetTotalWinText("00.00");

    StopGameAnimation(); 

    tweenroutine = StartCoroutine(TweenRoutine());
  }

  
  private void ForceCleanupPreviousSpin()
  {
    
    if (tweenroutine != null)
    {
      StopCoroutine(tweenroutine);
      tweenroutine = null;
    }

    IsAutoplayStoppedMidSpin = false;

    
    KillAllTweens();

    
    ResetSlotPositions();

    
    StopSpinToggle = false;
    TriggerSpinState(false);

    
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

    isEarlyRevealActive = false;
    earlyRevealPositions.Clear();

    if (SocketManager.resultData == null || SocketManager.resultData.matrix == null) return;

    var matrix = SocketManager.resultData.matrix;
    var payload = SocketManager.resultData.payload;

    if (testEarlyRevealScenario && !IsFreeSpin && !IsBonus)
    {
      isEarlyRevealActive = true;
      earlyRevealPositions.Add(new List<int> { 0, 1 });
      earlyRevealPositions.Add(new List<int> { 1, 1 });
      earlyRevealPositions.Add(new List<int> { 2, 1 });
      earlyRevealPositions.Add(new List<int> { 0, 3 });
      earlyRevealPositions.Add(new List<int> { 1, 3 });
      earlyRevealPositions.Add(new List<int> { 2, 3 });
      return;
    }

    if (!IsFreeSpin && !IsBonus)
    {
      if (matrix.Count >= 3)
      {
        int numCols = matrix[0].Count;
        List<int> wildCols = new List<int>();
        for (int col = 0; col < numCols; col++)
        {
          bool allWilds = true;
          for (int row = 0; row < 3; row++)
          {
            if (row >= matrix.Count || col >= matrix[row].Count)
            {
              allWilds = false;
              break;
            }
            string symbolStr = matrix[row][col];
            if (int.TryParse(symbolStr, out int symbolId))
            {
              if (!IsWildSymbol(symbolId))
              {
                allWilds = false;
                break;
              }
            }
            else
            {
              allWilds = false;
              break;
            }
          }
          if (allWilds)
          {
            wildCols.Add(col);
          }
        }

        if (wildCols.Count >= 1)
        {
          isEarlyRevealActive = true;
          foreach (int col in wildCols)
          {
            earlyRevealPositions.Add(new List<int> { 0, col });
            earlyRevealPositions.Add(new List<int> { 1, col });
            earlyRevealPositions.Add(new List<int> { 2, col });
          }
          return;
        }

        
        List<List<int>> specialPositions = new List<List<int>>();
        for (int r = 0; r < matrix.Count; r++)
        {
          for (int c = 0; c < matrix[r].Count; c++)
          {
            string symId = matrix[r][c];
            if (symId == "11" || symId == "12" || symId == "14")
            {
              specialPositions.Add(new List<int> { r, c });
            }
          }
        }

        if (specialPositions.Count > 0)
        {
          if (UnityEngine.Random.value < specialEarlyRevealChance)
          {
            isEarlyRevealActive = true;
            earlyRevealPositions = specialPositions;
            return;
          }
        }
      }
    }

    
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

    if (hasCCInCol0 && !IsFreeSpin)
    {
      float roll = UnityEngine.Random.value;
      if (roll < col0CCTriggerChance)
      {
        col0HasCC = true;
        
      }
      else
      {
        
      }
    }

    if (testCol0CashCollect && !IsFreeSpin)
    {
      col0HasCC = true;
      
    }

    if (testFakeScenarios && !IsFreeSpin && !IsBonus)
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
      
      return;
    }

    bool isCCTriggered = payload != null && payload.cashCollectResult != null && payload.cashCollectResult.triggered;
    bool isLinkTriggered = payload != null && payload.isLinkTriggered;
    bool featureTriggered = isCCTriggered || isLinkTriggered;

    if (!IsFreeSpin && !IsBonus && featureTriggered)
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

    if (!IsFreeSpin && !IsBonus && !hasCCInMatrix)
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
          
        }
      }
    }
  }

  
  private IEnumerator TweenRoutine()
  {
    bool winningsDisplayed = false;
    if (Balance < TotalBet && !IsFreeSpin) 
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
    
    
    for (int i = 0; i < numberOfSlots; i++)
    {
      InitializeTweening(Slot_Transform[i]);
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    UpdateFromSpinResult(SocketManager.resultData);
    if (IsFreeSpin && stickySymbolManager != null && SocketManager.resultData != null && SocketManager.resultData.payload != null)
    {
      stickySymbolManager.UpdateLockedCashCollectsStart(SocketManager.resultData.payload.lockedCashCollects);
    }
    OriginalFeatureTriggerResult = SocketManager.resultData;
    DetermineFakeScenarios();
    if (isEarlyRevealActive)
    {
      EnableAllBackTints(true, 0.85f);
      yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f));
      if (DialoguePopupManager.Instance != null)
      {
        yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.EarlyReveal);
      }
    }
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

    
    
    

    
    if (IsTurboOn)
    {
      yield return new WaitForSeconds(0.2f);
      StopSpinToggle = true;
    }
    else
    {
      
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

    if (isEarlyRevealActive)
    {
      yield return PlayEarlyRevealSequence();
    }

    bool wasStopPressed = StopSpinToggle;
    StopSpinToggle = false;

    
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
        float currentStagger = baseStagger;
        
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
    EnableAllBackTints(false);

    
    if (wasStopPressed && !IsAutoSpin && !IsFreeSpin)
    {
      uiManager.ShowSpinButtonCooldown(true);
      yield return new WaitForSeconds(stopCooldownDuration);
      uiManager.ShowSpinButtonCooldown(false);
    }
    else
    {
      yield return new WaitForSeconds(0.2f);
    }
    
    
    yield return new WaitUntil(() => !animationManager.AreLandingAnimationsPlaying);
    
    yield return new WaitForSeconds(0.3f);

    
    if (IsFreeSpin && stickySymbolManager != null && SocketManager.resultData != null && SocketManager.resultData.payload != null)
    {
        
        stickySymbolManager.UpdateLockedCashCollects(SocketManager.resultData.payload.lockedCashCollects);
    }

    
    featureQueue.BuildFromResponse(SocketManager.resultData.payload, IsFreeSpin);

    
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
      yield return new WaitUntil(() => animationManager.gameObject.activeSelf || !CheckPopups); 
      if (LineAnimRoutine != null)
      {
        yield return LineAnimRoutine;
      }
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }

    
    yield return ProcessDialoguePopups();
    yield return ProcessFeatureQueue(winningsDisplayed);

    
    
    if (hasPrizeOrCashFlow && (IsAutoSpin || SocketManager.resultData.payload.isFreeSpinActive || SocketManager.resultData.payload.linkFeatureActive))
    {
      yield return new WaitUntil(() => animationManager.gameObject.activeSelf || !CheckPopups); 
      if (LineAnimRoutine != null)
      {
        yield return LineAnimRoutine;
      }
      StopGameAnimation();
      yield return new WaitForSeconds(.2f);
    }
  }
  #endregion
  
  
  
  
  
  
  internal void OnLinkFeatureCompleted()
  {
    
    if (featureQueue.HasPending)
    {
      
      StartCoroutine(ProcessRemainingFeaturesAfterLink());
      return;
    }

    
    if (WasFreeSpinPaused && FreeSpinsCount > 0)
    {
      
      WasFreeSpinPaused = false;
      IsFreeSpin = true;

      if (SocketManager.resultData != null && SocketManager.resultData.payload != null)
      {
          savedLockedCashCollects = SocketManager.resultData.payload.lockedCashCollects;
      }

      uiManager.OpenFreeSpinsUI();
      FreeSpin(FreeSpinsCount);
      return;
    }

    
    if (WasAutoSpinOn)
    {
      WasAutoSpinOn = false;
      AutoSpin();
      return;
    }

    uiManager.SetButtonsInteractable(true);
  }

  
  
  
  
  private IEnumerator ProcessRemainingFeaturesAfterLink()
  {
    while (featureQueue.HasPending)
    {
      FeatureType feature = featureQueue.Dequeue();

      switch (feature)
      {
        case FeatureType.FreeSpin:
          yield return HandleFreeSpinTrigger(false);
          yield break; 

        case FeatureType.FreeSpinRetrigger:
          yield return HandleFreeSpinRetrigger();
          
          if (!featureQueue.HasPending && IsFreeSpin && FreeSpinsCount > 0)
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

  
  
  
  
  private IEnumerator ProcessFeatureQueue(bool winningsAlreadyDisplayed)
  {
    bool winningsDisplayed = winningsAlreadyDisplayed;

    while (featureQueue.HasPending)
    {
      FeatureType feature = featureQueue.Dequeue();
      

      switch (feature)
      {
        case FeatureType.PrizeCoinJackpot:
          yield return HandlePrizeCoinJackpot();
          break;

        case FeatureType.CashCollect:
          yield return HandleCashCollectSequence();
          break;

        case FeatureType.CashCollectAndLink:
          
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
          yield break; 

        case FeatureType.FreeSpin:
          
          if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
          {
            winningsDisplayed = true;
            CheckPopups = true;
            uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
            yield return new WaitUntil(() => !CheckPopups);
            yield return new WaitForSeconds(.5f);
          }
          yield return HandleFreeSpinTrigger(false);
          yield break; 

        case FeatureType.FreeSpinRetrigger:
          
          if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
          {
            winningsDisplayed = true;
            CheckPopups = true;
            uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
            yield return new WaitUntil(() => !CheckPopups);
            yield return new WaitForSeconds(.5f);
          }
          yield return HandleFreeSpinRetrigger();
          
          if (!featureQueue.HasPending)
          {
            TriggerSpinState(false);
            FreeSpin(FreeSpinsCount);
            yield break;
          }
          break;
      }
    }

    

    
    if (SocketManager.resultData.payload.winAmount > 0 && !winningsDisplayed)
    {
      winningsDisplayed = true;
      CheckPopups = true;
      uiManager.WinningsTextAnimation(() => { CheckPopups = false; });
      yield return new WaitUntil(() => !CheckPopups);
      yield return new WaitForSeconds(.5f);
    }

    
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
        if (DialoguePopupManager.Instance != null)
        {
          yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.FreeSpinEnd);
        }

        if (DialoguePopupManager.Instance != null)
        {
          yield return DialoguePopupManager.Instance.PlayFreeSpinEndDialogue(totalFeatureWin, TotalBet);
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

  
  
  
  
  private IEnumerator HandlePrizeCoinJackpot()
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
          double jackpotAmount = item.coinValue * TotalBet;
          yield return jackpotManager.PlayJackpotSequence(view, item.prizeType, item.prizeTypeIndex ?? 0, jackpotAmount.ToString("0.###"), prizeSprite);
        }
      }
    }

    yield return new WaitForSeconds(1.2f);
    yield return new WaitForSeconds(.2f);

    
    if (!featureQueue.HasPending)
    {
      IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();
    }
  }

  
  
  
  
  
  private IEnumerator HandleCashCollectAndLink()
  {
    bool isShiftFromFreeSpin = IsFreeSpin;
    if (!isShiftFromFreeSpin && stickySymbolManager != null)
    {
      stickySymbolManager.Reset();
    }

    IsBonus = true;
    IsFeatureTransitioning = true;
    uiManager.UpdateButtonsState();

    bool triggeredFromNormal = !IsFreeSpin;

    if (IsFreeSpin)
    {
      WasFreeSpinPaused = true;
      IsFreeSpin = false;
    }

    yield return ResetUI();

    yield return _bonusManager.StartBonus(SocketManager.resultData.payload.linkRespinsRemaining, triggeredFromNormal);
    IsFeatureTransitioning = false;
    uiManager.UpdateButtonsState();
    TriggerSpinState(false);
  }

  private IEnumerator HandleCashCollectSequence()
  {
    
    
    IsFeatureTransitioning = true;
    uiManager.UpdateButtonsState();

    
    StopGameAnimation();
    yield return new WaitForSeconds(0.2f);

    var ccResult = SocketManager.resultData.payload.cashCollectResult;
    if (ccResult != null && ccResult.triggered)
    {
      yield return uiManager.PlayCashCollectSequence(ccResult);
    }

    
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

  
  
  
  
  private IEnumerator HandleFreeSpinTrigger(bool isRetrigger)
  {
    

    if (!isRetrigger && !IsFreeSpin)
    {
      if (stickySymbolManager != null)
      {
        stickySymbolManager.Reset();
      }
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

  
  
  
  
  private IEnumerator HandleFreeSpinRetrigger()
  {
    

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
          case 11: 
              return SpecialLayerSymbols.Length > 0 ? SpecialLayerSymbols[0] : null;
          case 12: 
              return SpecialLayerSymbols.Length > 1 ? SpecialLayerSymbols[1] : null;
          case 14: 
              return SpecialLayerSymbols.Length > 2 ? SpecialLayerSymbols[2] : null;
          case 16: 
              return SpecialLayerSymbols.Length > 3 ? SpecialLayerSymbols[3] : null;
          case 17: 
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

        if (resultNum == 9) 
        {
          int randomSymbolIndex = UnityEngine.Random.Range(0, 9); 
          SlotImage.sprite = SlotSymbols[randomSymbolIndex];
          if (view != null) ConfigureSymbolView(view, randomSymbolIndex);
          continue;
        }
        if (resultNum == 17) 
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
        else if (resultNum == 13) 
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
        else if (resultNum == 15) 
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

  private bool IsBlockedFlameSprite(Sprite sprite)
  {
    if (sprite == null || SlotSymbols == null) return false;
    int index = System.Array.IndexOf(SlotSymbols, sprite);
    if (index == -1) return false;
    return index == 11 || index == 12 || index == 13 || index == 14 || index == 15 || index == 16 || index == 17;
  }

  private void UpdateCashCollectIndicators(int col)
  {
    if (col < 0 || col >= images.Count) return;
    var imgs = images[col].slotImages;
    if (imgs == null) return;

    
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

      if (IsBlockedFlameSprite(imgs[fullIndex].sprite) || IsFreeSpin)
      {
        isAbove = false;
        isBelow = false;
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
           
        }
      }
    }
    else
    {
      
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

  internal void PerformStop()
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

    
    Sequence startSeq = DOTween.Sequence();
    if (isSpinReverse)
    {
      
      startSeq.Append(slotTransform.DOLocalMoveY(restY - anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutQuad));
      startSeq.Append(slotTransform.DOLocalMoveY(restY, anticipationUpDuration * 0.5f).SetEase(Ease.InQuad));
    }
    else
    {
      
      startSeq.Append(slotTransform.DOLocalMoveY(restY + anticipationUpDistance, anticipationUpDuration).SetEase(Ease.OutQuad));
      startSeq.Append(slotTransform.DOLocalMoveY(restY, anticipationUpDuration * 0.5f).SetEase(Ease.InQuad));
    }
    startSeq.OnComplete(() => { if (IsSpinning) RunContinuousCycle(col, slotTransform, restY); });
    alltweens[col] = startSeq;
    startSeq.Play();
  }

  
  private void RunContinuousCycle(int col, Transform slotTransform, float restY)
  {
    if (!IsSpinning) return;

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);

    
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
      
      for (int i = 0; i < imgs.Count - 1; i++)
      {
        imgs[i].sprite = imgs[i + 1].sprite;

        
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
        
        int r;
        do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
        imgs[lastIdx].sprite = SlotSymbols[r];

        
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
      
      for (int i = imgs.Count - 1; i > 0; i--)
      {
        imgs[i].sprite = imgs[i - 1].sprite;

        
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
        
        int r;
        do { r = UnityEngine.Random.Range(0, SlotSymbols.Length - 8); } while (r == 9);
        imgs[0].sprite = SlotSymbols[r];

        
        SlotSymbolView topView = imgs[0].GetComponent<SlotSymbolView>();
        if (topView != null)
        {
          topView.ClearValues();
          ConfigureSymbolView(topView, r);
        }
      }
    }

    
    if (stopStatus[col] == 5)
    {
      
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
      if (magnetRow == 0) 
      {
        if (targetRowIndex == 0) return 1;
        if (targetRowIndex == 1) return 2;
      }
      else if (magnetRow == 2) 
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
    if (resultNum == 9) 
    {
      return UnityEngine.Random.Range(0, 9); 
    }
    return resultNum;
  }

  private int GetResultSymbolId(int col, int row)
  {
    if (isMagnetScenarioActive && col == magnetCol)
    {
      if (magnetRow == 0) 
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
      else if (magnetRow == 2) 
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

    if (symbolId == 17) 
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
    else if (symbolId == 13) 
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
    else if (symbolId == 15) 
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

    if (SocketManager.resultData != null && SocketManager.resultData.matrix != null)
    {
      bool isAboveCC = false;
      bool isBelowCC = false;

      bool isBlocked = (symbolId == 11 || symbolId == 12 || symbolId == 13 || symbolId == 14 || symbolId == 15 || symbolId == 16 || symbolId == 17);
      if (!isBlocked && !IsFreeSpin)
      {
        if (row == 0)
        {
          bool isNearMissTopActive = isNearMissActive && col == nearMissCol && 
                                     (isSpinReverse ? (nearMissType == 1) : (nearMissType == 0));
          bool shouldShowAtTop = (isMagnetScenarioActive && col == magnetCol && magnetRow == 0) || isNearMissTopActive;
          if (shouldShowAtTop)
          {
            isAboveCC = true;
          }
        }
        else if (row - 1 >= 0 && row - 1 < SocketManager.resultData.matrix.Count)
        {
          if (SocketManager.resultData.matrix[row - 1][col] == "14")
          {
            isAboveCC = true;
          }
        }

        if (row == 2)
        {
          bool isNearMissBottomActive = isNearMissActive && col == nearMissCol && 
                                        (isSpinReverse ? (nearMissType == 0) : (nearMissType == 1));
          bool shouldShowAtBottom = (isMagnetScenarioActive && col == magnetCol && magnetRow == 2) || isNearMissBottomActive;
          if (shouldShowAtBottom)
          {
            isBelowCC = true;
          }
        }
        else if (row + 1 >= 0 && row + 1 < SocketManager.resultData.matrix.Count)
        {
          if (SocketManager.resultData.matrix[row + 1][col] == "14")
          {
            isBelowCC = true;
          }
        }
      }

      if (view.cashCollectAboveObject != null)
      {
        view.cashCollectAboveObject.SetActive(isAboveCC);
      }
      if (view.cashCollectBelowObject != null)
      {
        view.cashCollectBelowObject.SetActive(isBelowCC);
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

    
    if (isNearMissActive && col == nearMissCol && nearMissType == 1)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, restY, 0f);
      OnReelStopped(col);
      return;
    }

    
    
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
      
      float targetOvershoot = isSpinReverse ? (restY + quickStopOvershoot) : (restY - quickStopOvershoot);
      stopSeq.Append(slotTransform.DOLocalMoveY(targetOvershoot, quickStopDuration * 0.3f).SetEase(Ease.OutQuad));
      stopSeq.Append(slotTransform.DOLocalMoveY(restY,                      quickStopDuration * 0.7f).SetEase(Ease.InOutQuad));
    }
    else
    {
      
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

    if (DialoguePopupManager.Instance != null)
    {
      yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.MagnetHit);
    }

    GameObject activeMagnet = null;
    if (magnetCol == 0) 
    {
      activeMagnet = (magnetRow == 0) ? leftBottomMagnet : leftTopMagnet;
    }
    else if (magnetCol == 4) 
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

    

    yield return new WaitForSeconds(magnetAnimDuration);

    Transform reelTrans = Slot_Transform[magnetCol];
    float startY = initialYPositions[magnetCol];
    float targetY = startY + (magnetNudgeDir * symbolHeight);

    bool nudgeComplete = false;

    
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
      activeMagnet.transform.localPosition = initialMagnetPos; 
    }

    isMagnetScenarioActive = false;

    PopulateResultMatrixForColumn(magnetCol);
    reelTrans.localPosition = new Vector3(reelTrans.localPosition.x, startY, 0f);

    

    
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

    if (DialoguePopupManager.Instance != null)
    {
      yield return DialoguePopupManager.Instance.PlayMagnetAppearanceDialogue();
    }
  }

  private void OnReelStopped(int col)
  {
      if (isMagnetScenarioActive && col == magnetCol)
      {
          
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

      
      if (isNearMissActive && col == nearMissCol - 1)
      {
          if (AudioController.Instance != null) AudioController.Instance.PlaySingleTension();
      }
      if (isNearMissActive && col == nearMissCol)
      {
          if (AudioController.Instance != null) AudioController.Instance.StopSingleTension();
      }

      
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
              bool isLockedCC = false;
              if (IsFreeSpin && stickySymbolManager != null)
              {
                  isLockedCC = stickySymbolManager.IsPositionLocked(col, row);
              }

              if (int.TryParse(symbolIdStr, out int symbolId))
              {
                  if (!isLockedCC)
                  {
                      if (symbolId == 15 || symbolId == 16) 
                      {
                          if (AudioController.Instance != null) AudioController.Instance.PlayCashCoinLand();
                      }
                      else if (symbolId == 11 || symbolId == 12) 
                      {
                          if (AudioController.Instance != null) AudioController.Instance.PlayLinkLand();
                      }
                      else if (symbolId == 14 || symbolId == 17) 
                      {
                          if (AudioController.Instance != null) AudioController.Instance.PlayCashCollectLand();
                      }
                  }
              }

              if (isSpecial(symbolIdStr) && !isLockedCC)
              {
                  animationManager.PlaySpecialAnimationForCell(row, col);
              }
          }
      }
      UpdateCashCollectIndicators(col);
  }

  
  private void CopyViewState(SlotSymbolView src, SlotSymbolView dst)
  {
    
    if (dst.specialSymbolLayer != null && src.specialSymbolLayer != null)
    {
      dst.specialSymbolLayer.gameObject.SetActive(src.specialSymbolLayer.gameObject.activeSelf);
      dst.specialSymbolLayer.sprite = src.specialSymbolLayer.sprite;
    }
    else if (dst.specialSymbolLayer != null)
    {
      dst.specialSymbolLayer.gameObject.SetActive(false);
    }

    
    if (dst.hatObject != null && src.hatObject != null)
    {
      dst.hatObject.SetActive(src.hatObject.activeSelf);
    }
    else if (dst.hatObject != null)
    {
      dst.hatObject.SetActive(false);
    }

    
    if (dst.goldCoinValueText != null && src.goldCoinValueText != null)
    {
      dst.goldCoinValueText.gameObject.SetActive(src.goldCoinValueText.gameObject.activeSelf);
      dst.goldCoinValueText.text = src.goldCoinValueText.text;
    }
    else if (dst.goldCoinValueText != null)
    {
      dst.goldCoinValueText.gameObject.SetActive(false);
    }

    
    if (dst.multiplierValueText != null && src.multiplierValueText != null)
    {
      dst.multiplierValueText.gameObject.SetActive(src.multiplierValueText.gameObject.activeSelf);
      dst.multiplierValueText.text = src.multiplierValueText.text;
    }
    else if (dst.multiplierValueText != null)
    {
      dst.multiplierValueText.gameObject.SetActive(false);
    }

    
    if (dst.losPolosValueText != null && src.losPolosValueText != null)
    {
      dst.losPolosValueText.gameObject.SetActive(src.losPolosValueText.gameObject.activeSelf);
      dst.losPolosValueText.text = src.losPolosValueText.text;
    }
    else if (dst.losPolosValueText != null)
    {
      dst.losPolosValueText.gameObject.SetActive(false);
    }

    
    if (dst.jackpotObject != null && src.jackpotObject != null)
    {
      dst.jackpotObject.SetActive(src.jackpotObject.activeSelf);
    }
    else if (dst.jackpotObject != null)
    {
      dst.jackpotObject.SetActive(false);
    }

    
    if (dst.jackpotResultText != null && src.jackpotResultText != null)
    {
      dst.jackpotResultText.gameObject.SetActive(src.jackpotResultText.gameObject.activeSelf);
      dst.jackpotResultText.text = src.jackpotResultText.text;
    }
    else if (dst.jackpotResultText != null)
    {
      dst.jackpotResultText.gameObject.SetActive(false);
    }

    
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

    
    if (dst.cashCollectAboveObject != null && src.cashCollectAboveObject != null)
    {
      dst.cashCollectAboveObject.SetActive(src.cashCollectAboveObject.activeSelf);
    }
    else if (dst.cashCollectAboveObject != null)
    {
      dst.cashCollectAboveObject.SetActive(false);
    }

    
    if (dst.cashCollectBelowObject != null && src.cashCollectBelowObject != null)
    {
      dst.cashCollectBelowObject.SetActive(src.cashCollectBelowObject.activeSelf);
    }
    else if (dst.cashCollectBelowObject != null)
    {
      dst.cashCollectBelowObject.SetActive(false);
    }

    
    if (dst.canvasGroup != null && src.canvasGroup != null)
    {
      dst.canvasGroup.alpha = src.canvasGroup.alpha;
    }
  }

  private IEnumerator ProcessDialoguePopups()
  {
    if (DialoguePopupManager.Instance == null || IsFreeSpin) yield break;

    
    int specialSymbolCount = GetSpecialSymbolsCountOnGrid();
    bool isCCTriggered = SocketManager.resultData != null && 
                         SocketManager.resultData.payload != null && 
                         SocketManager.resultData.payload.cashCollectResult != null && 
                         SocketManager.resultData.payload.cashCollectResult.triggered;

    if (isCCTriggered && specialSymbolCount >= 4) 
    {
      yield return DialoguePopupManager.Instance.PlayTooManySymbolsAndCashCollectDialogue();
      yield break; 
    }

    
    bool isFreeSpinTriggered = SocketManager.resultData != null && 
                               SocketManager.resultData.payload != null && 
                               SocketManager.resultData.payload.isFreeSpinTriggered;
    if (isFreeSpinTriggered)
    {
      int spins = SocketManager.resultData.payload.freeSpinsRemaining;
      yield return DialoguePopupManager.Instance.PlayFreeSpinHitDialogue(spins);
      yield break;
    }

    
    bool isLinkTriggered = SocketManager.resultData != null && 
                           SocketManager.resultData.payload != null && 
                           SocketManager.resultData.payload.isLinkTriggered;
    if (isLinkTriggered)
    {
      if (HasMegaLinkOnGrid())
      {
        yield return DialoguePopupManager.Instance.PlayMegaLinkFeatureTriggerDialogue();
      }
      else
      {
        yield return DialoguePopupManager.Instance.PlayLinkFeatureTriggerDialogue();
      }
      yield break;
    }

    
    bool isJackpotTriggered = featureQueue != null && featureQueue.Contains(FeatureType.PrizeCoinJackpot);
    if (isJackpotTriggered)
    {
      yield return DialoguePopupManager.Instance.PlayMiniJackpotDialogue();
      yield break;
    }

    
    if (isCCTriggered)
    {
      yield return DialoguePopupManager.Instance.PlayCashCollectTriggerDialogue();
      yield break;
    }

    
    if (isNearMissActive)
    {
      yield return DialoguePopupManager.Instance.PlayNearMissDialogue();
      yield break;
    }

    
    if (specialSymbolCount >= 2 && (featureQueue == null || !featureQueue.HasPending))
    {
      yield return DialoguePopupManager.Instance.PlaySpecialSymbolNoTriggerDialogue();
      yield break;
    }
  }

  private int GetSpecialSymbolsCountOnGrid()
  {
    int count = 0;
    if (SocketManager == null || SocketManager.resultData == null || SocketManager.resultData.matrix == null) return 0;
    for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
    {
      for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
      {
        int symbolId;
        if (int.TryParse(SocketManager.resultData.matrix[i][j], out symbolId))
        {
          if (IsSpecialSymbol(symbolId))
          {
            count++;
          }
        }
      }
    }
    return count;
  }

  private bool HasMiniJackpotOnGrid()
  {
    if (SocketManager == null || SocketManager.resultData == null || SocketManager.resultData.payload == null || SocketManager.resultData.payload.coinPositions == null) return false;
    foreach (var item in SocketManager.resultData.payload.coinPositions)
    {
      if (item.symbolId == 16) 
      {
        string pType = item.prizeType;
        if (!string.IsNullOrEmpty(pType) && pType.ToLower().Contains("mini"))
        {
          return true;
        }
      }
    }
    return false;
  }

  private bool HasMegaLinkOnGrid()
  {
    if (SocketManager == null || SocketManager.resultData == null || SocketManager.resultData.matrix == null) return false;
    for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
    {
      for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
      {
        if (SocketManager.resultData.matrix[i][j] == "12")
        {
          return true;
        }
      }
    }
    return false;
  }

  private IEnumerator PlayEarlyRevealSequence()
  {
      if (animationManager == null || earlyRevealPositions == null || earlyRevealPositions.Count == 0) yield break;

      
      bool useSmoke = UnityEngine.Random.value < 0.5f;

      List<Coroutine> revealCoroutines = new List<Coroutine>();

      for (int i = 0; i < earlyRevealPositions.Count; i++)
      {
          int row = earlyRevealPositions[i][0];
          int col = earlyRevealPositions[i][1];
          revealCoroutines.Add(StartCoroutine(PlayCellEarlyReveal(row, col, useSmoke)));
          yield return new WaitForSeconds(0.15f);
      }

      
      foreach (var coroutine in revealCoroutines)
      {
          yield return coroutine;
      }

      isEarlyRevealActive = false;
      earlyRevealPositions.Clear();
  }

  private IEnumerator PlayCellEarlyReveal(int row, int col, bool useSmoke)
  {
      SlotSymbolView symbolView = GetSymbolView(row, col);
      if (symbolView == null) yield break;

      ImageAnimation animCell = animationManager.GetAnimationCell(row, col);
      if (animCell == null) yield break;

      // Reset animCell animation state and clear its textureArray to prevent previous symbols from showing
      animCell.StopAnimation();
      animCell.isAnim = false;
      animCell.onLoopComplete = null;
      animCell.textureArray.Clear();
      animCell.textureArray.TrimExcess();
      if (animCell.rendererDelegate != null && animationManager != null)
      {
          animCell.rendererDelegate.sprite = animationManager.idleSprite;
      }

      // Reset the sticky symbol slot object (slot image and animation) at the same position
      if (stickySymbolManager != null)
      {
          if (col >= 0 && col < stickySymbolManager.Slot.Count && row >= 0 && row < stickySymbolManager.Slot[col].slotImages.Count)
          {
              var stickyImage = stickySymbolManager.Slot[col].slotImages[row];
              if (stickyImage != null)
              {
                  stickyImage.sprite = null;
                  stickyImage.gameObject.SetActive(false);

                  var stickyAnim = stickyImage.GetComponent<ImageAnimation>();
                  if (stickyAnim != null)
                  {
                      stickyAnim.StopAnimation();
                      stickyAnim.isAnim = false;
                      stickyAnim.onLoopComplete = null;
                      stickyAnim.textureArray.Clear();
                      stickyAnim.textureArray.TrimExcess();
                  }
              }

              var stickyView = stickySymbolManager.GetLockedSymbolView(col, row);
              if (stickyView != null)
              {
                  stickyView.ClearValues();
              }
          }
      }

      animCell.gameObject.SetActive(true);
      animCell.DOKill();
      CanvasGroup cellCG = animCell.GetComponent<CanvasGroup>();
      if (cellCG != null)
      {
          cellCG.DOKill();
          cellCG.alpha = 1f;
      }
      else if (animCell.rendererDelegate != null)
      {
          animCell.rendererDelegate.DOKill();
          animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 1f);
      }

      AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
      if (textHelper == null)
      {
          textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
          textHelper.SetupFromHierarchy();
      }
      if (textHelper.revealEffectAnimation == null)
      {
          foreach (Transform child in animCell.transform)
          {
              ImageAnimation childAnim = child.GetComponent<ImageAnimation>();
              if (childAnim != null && child.name.ToLower().Contains("reveal"))
              {
                  textHelper.revealEffectAnimation = childAnim;
                  break;
              }
          }
          if (textHelper.revealEffectAnimation == null)
          {
              foreach (Transform child in animCell.transform)
              {
                  ImageAnimation childAnim = child.GetComponent<ImageAnimation>();
                  if (childAnim != null && childAnim != animCell)
                  {
                      textHelper.revealEffectAnimation = childAnim;
                      break;
                  }
              }
          }
      }
      ImageAnimation effectAnim = textHelper.revealEffectAnimation;
      if (effectAnim != null)
      {
          effectAnim.textureArray.Clear();
          effectAnim.textureArray.TrimExcess();
          Sprite[] targetSprites = useSmoke ? smokeEffectSprites : iceBreakingEffectSprites;
          if (targetSprites != null)
          {
              foreach (Sprite s in targetSprites)
              {
                  effectAnim.textureArray.Add(s);
              }
          }

          effectAnim.gameObject.SetActive(true);
          effectAnim.doLoopAnimation = false;
          effectAnim.onLoopComplete = null;
          effectAnim.StopAnimation();
          effectAnim.StartAnimation();

          if (AudioController.Instance != null)
          {
              if (useSmoke)
              {
                  AudioController.Instance.PlaySmokeReveal();
              }
              else
              {
                  AudioController.Instance.PlayIceBreakingReveal();
              }
          }

          bool effectDone = false;
          effectAnim.onLoopComplete = (_) => { effectDone = true; };

          float expectedDuration = 1.5f;
          if (effectAnim.textureArray != null && effectAnim.textureArray.Count > 0)
          {
              if (effectAnim.useDynamicFramerate)
              {
                  expectedDuration = effectAnim.dynamicLoopDuration;
              }
              else if (effectAnim.AnimationSpeed > 0)
              {
                  expectedDuration = (effectAnim.textureArray.Count / effectAnim.AnimationSpeed) * 0.0416666679f;
              }
          }

           float elapsed = 0f;
          float timeout = expectedDuration + 0.5f;
          bool stickyEnabled = false;
          float halfDuration = expectedDuration / 2f;
          while (!effectDone && elapsed < timeout)
          {
              elapsed += Time.deltaTime;
              if (!stickyEnabled && elapsed >= halfDuration)
              {
                  stickyEnabled = true;
                  symbolView.SetBackTintActive(false);
                  if (stickySymbolManager != null)
                  {
                      List<List<int>> loc = new List<List<int>> { new List<int> { row, col } };
                      stickySymbolManager.TurnOnIndices(loc);
                  }
              }
              yield return null;
          }

          if (!stickyEnabled)
          {
              symbolView.SetBackTintActive(false);
              if (stickySymbolManager != null)
              {
                  List<List<int>> loc = new List<List<int>> { new List<int> { row, col } };
                  stickySymbolManager.TurnOnIndices(loc);
              }
          }

          effectAnim.onLoopComplete = null;
          effectAnim.StopAnimation();
          if (effectAnim.rendererDelegate != null && animationManager != null)
          {
              effectAnim.rendererDelegate.sprite = animationManager.idleSprite;
          }
          effectAnim.gameObject.SetActive(false);
      }
      else
      {
          yield return new WaitForSeconds(1.0f);
          symbolView.SetBackTintActive(false);
          if (stickySymbolManager != null)
          {
              List<List<int>> loc = new List<List<int>> { new List<int> { row, col } };
              stickySymbolManager.TurnOnIndices(loc);
          }
      }

      
      string symbolIdStr = "10"; 
      if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
          row < SocketManager.resultData.matrix.Count && col < SocketManager.resultData.matrix[row].Count)
      {
          symbolIdStr = SocketManager.resultData.matrix[row][col];
      }

      int symbolId;
      if (int.TryParse(symbolIdStr, out symbolId))
      {
          ConfigureAnimationSprites(animCell, symbolId);

          float expectedDuration = 1.5f;
          if (animCell.textureArray != null && animCell.textureArray.Count > 0)
          {
              if (animCell.useDynamicFramerate)
              {
                  expectedDuration = animCell.dynamicLoopDuration;
              }
              else if (animCell.AnimationSpeed > 0)
              {
                  expectedDuration = (animCell.textureArray.Count / animCell.AnimationSpeed) * 0.0416666679f;
              }
          }

          AnimationTextHelper mainTextHelper = animCell.GetComponent<AnimationTextHelper>();
          if (mainTextHelper == null)
          {
              mainTextHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
              mainTextHelper.SetupFromHierarchy();
          }

          CoinPosition coinPos = GetCoinPosition(row, col);
          string textContent = "";
          if (symbolId == 17)
          {
              int lpVal = 0;
              if (coinPos != null)
              {
                  lpVal = (int)coinPos.coinValue;
              }
              else
              {
                  int[] tempIndex = { 2, 3, 4, 5, 7 };
                  lpVal = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
              }
              string valStr = lpVal.ToString();
              textContent = "<sprite=10>"; 
              foreach (char c in valStr)
              {
                  if (char.IsDigit(c)) textContent += $"<sprite={c - '0'}>";
              }
          }
          else if (symbolId == 15 && coinPos != null)
          {
              string valStr = (coinPos.coinValue * TotalBet).ToString("0.###");
              textContent = "";
              foreach (char c in valStr)
              {
                  if (char.IsDigit(c)) textContent += $"<sprite={c - '0'}>";
                  else if (c == '.') textContent += "<sprite=10>";
              }
          }
          else if (symbolId == 13 && coinPos != null)
          {
              textContent = "X" + coinPos.coinValue.ToString();
          }

          if (!string.IsNullOrEmpty(textContent))
          {
              mainTextHelper.PlayTextAnimation(symbolId, textContent, expectedDuration, false);
          }
          else
          {
              mainTextHelper.Clear();
          }

          animCell.doLoopAnimation = false;
          animCell.onLoopComplete = null;
          animCell.StopAnimation();
          animCell.StartAnimation();

          bool mainDone = false;
          animCell.onLoopComplete = (_) => { mainDone = true; };

          float elapsed = 0f;
          float timeout = expectedDuration + 0.5f;
          while (!mainDone && elapsed < timeout)
          {
              elapsed += Time.deltaTime;
              yield return null;
          }

          animCell.onLoopComplete = null;
          animCell.StopAnimation();
          if (mainTextHelper != null)
          {
              mainTextHelper.Clear();
          }
          if (animCell.rendererDelegate != null && animationManager != null)
          {
              animCell.rendererDelegate.sprite = animationManager.idleSprite;
          }
      }

      animCell.gameObject.SetActive(false);
  }

  private void KillAllTweens()
  {
    foreach (var t in alltweens) t?.Kill();
    alltweens.Clear();
  }
  #endregion
}
