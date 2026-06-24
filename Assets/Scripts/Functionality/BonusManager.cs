using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class BonusManager : MonoBehaviour
{
  [Header("Scripts References")]
  
  [SerializeField] private SlotManager slotManager;
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private StickySymbolManager staticSymbol;


  [Header("Sprites References")]
  [SerializeField] private Sprite[] index9Sprites;
  [SerializeField] private Sprite coinFrame;
  [SerializeField] private Sprite CC_Sprite;
  [SerializeField] private Sprite Diamond_Sprite;

  [Header("UI Objects References")]
  [SerializeField] private CanvasGroup NormalSlot_CG;
  [SerializeField] private CanvasGroup BonusSlot_CG;



  [Header("Slot References")]
  [SerializeField] private List<SlotImage> TotalMiniSlotImages;     //class to store total images
  [SerializeField] internal List<SlotTransform> Slot;

  private Dictionary<Transform, Tween> activeTweens = new Dictionary<Transform, Tween>();
  private int IconSizeFactor = 202;
  internal bool IsSpinning;
  internal bool StopSpinToggle;
  private bool BonusEnd = false;
  private Coroutine BonusRoutine;
  private float SpinDelay = 0.2f;
  
  private List<CoinPosition> allcoinPositions = new List<CoinPosition>();

  private void Start()
  {
    ResetMatrix();
    if (NormalSlot_CG != null)
    {
      NormalSlot_CG.alpha = 1f;
      NormalSlot_CG.blocksRaycasts = true;
      NormalSlot_CG.interactable = true;
      NormalSlot_CG.gameObject.SetActive(true);
    }
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.alpha = 0f;
      BonusSlot_CG.blocksRaycasts = false;
      BonusSlot_CG.interactable = false;
      BonusSlot_CG.gameObject.SetActive(false);
    }

  }

  internal void StartBonus(int count)
  {
    uiManager.OpenFeaturePopup(() => {
        StartBonusGame(count);
    });
  }

  internal void StartBonusGame(int count)
  {
    if (NormalSlot_CG != null)
    {
      NormalSlot_CG.interactable = false;
      NormalSlot_CG.blocksRaycasts = false;
    }
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.gameObject.SetActive(true);
      BonusSlot_CG.interactable = false;
      BonusSlot_CG.blocksRaycasts = false;
    }


    double initialWin = 0;
    if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.linkFeatureResult != null)
    {
      initialWin = SocketManager.resultData.payload.linkFeatureResult.totalValue;
    }
    uiManager.OpenBonusUI(count, initialWin);

    ResetMatrix();

    // Place the initial coins on the board
    var initialCoins = SocketManager.resultData.payload.linkFeatureResult?.initialCoins;
    if (initialCoins == null)
    {
      initialCoins = new List<InitialCoin>();
      if (SocketManager.resultData.payload.coinPositions != null)
      {
        foreach (var coin in SocketManager.resultData.payload.coinPositions)
        {
          InitialCoin initCoin = new InitialCoin();
          initCoin.position = coin.position;
          initCoin.coinValue = coin.coinValue;
          initCoin.symbolId = coin.symbolId;
          initCoin.symbolName = coin.symbolName;
          initialCoins.Add(initCoin);
        }
      }
    }

    foreach (var coin in initialCoins)
    {
      int row = coin.position[0];
      int col = coin.position[1];
      Transform cell = Slot[col].slotTransforms[row];
      Image img = cell.GetChild(2).GetComponent<Image>();
      Sprite normalSlotSprite = slotManager.GetResultMatrixImage(row, col).sprite;
      img.sprite = normalSlotSprite;
      SlotSymbolView view = img.GetComponent<SlotSymbolView>();
      if (view != null)
      {
        view.ClearValues();
        slotManager.ConfigureSymbolView(view, coin.symbolId);
      }
    }

    // Place cashcollect symbol on its position
    for (int row = 0; row < SocketManager.resultData.matrix.Count; row++)
    {
      for (int col = 0; col < SocketManager.resultData.matrix[row].Count; col++)
      {
        if (SocketManager.resultData.matrix[row][col] == "14")
        {
          Transform cell = Slot[col].slotTransforms[row];
          Image img = cell.GetChild(2).GetComponent<Image>();
          img.sprite = CC_Sprite;
          SlotSymbolView view = img.GetComponent<SlotSymbolView>();
          if (view != null)
          {
            view.ClearValues();
            slotManager.ConfigureSymbolView(view, 14); // Configure as Cash Collect
          }
        }
      }
    }

    // Initialize the sticky symbol manager's frozen locations with initial coins and Cash Collect symbols
    List<List<int>> initialLocations = new List<List<int>>();
    if (initialCoins != null)
    {
      foreach (var coin in initialCoins)
      {
        initialLocations.Add(new List<int> { coin.position[0], coin.position[1] });
      }
    }
    var ccPositions = SocketManager.resultData.payload.linkFeatureResult?.cashCollectPositions;
    if (ccPositions != null)
    {
      try
      {
        foreach (var pos in ccPositions)
        {
          if (pos.Type == Newtonsoft.Json.Linq.JTokenType.Array)
          {
            initialLocations.Add(new List<int> { (int)pos[0], (int)pos[1] });
          }
        }
      }
      catch (System.Exception ex)
      {
        Debug.LogError("Error parsing cashCollectPositions: " + ex.Message);
      }
    }
    staticSymbol.GenerateFreezeMatrix(initialLocations);

    NormalSlot_CG.DOFade(0, 0.5f);
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.gameObject.SetActive(true);
    }
    BonusSlot_CG.DOFade(1, .5f).OnComplete(() =>
    {
      OnInitialTransitionComplete();
    });
  }

  internal IEnumerator StartBonusLoop()
  {
    BonusEnd = false;
    while (!BonusEnd)
    {
      StartBonusSlot();
      yield return BonusRoutine;
      yield return new WaitForSeconds(SpinDelay);
    }
  }

  internal void StartBonusSlot()
  {
    int spinCount = slotManager.LinkRespinsRemaining;
    spinCount -= 1;
    slotManager.SetLinkRespinsRemaining(spinCount);
    uiManager.SetBonusSpinCounter(spinCount);
    uiManager.UpdateFeatureButtonsState(true, spinCount);

    StopSpinToggle = false;
    BonusRoutine = StartCoroutine(BonusTweenRoutine());
  }

  private IEnumerator BonusTweenRoutine()
  {
    IsSpinning = true;

    // Initialize tweening for non-frozen slot animations
    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (staticSymbol.freezedLocations[col].index[row] == 0) // Only initialize non-frozen slots
        {
          InitializeSingleSlotTweening(Slot[col].slotTransforms[row]);
        }
      }
    }

    SocketManager.AccumulateResult(slotManager.BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    slotManager.UpdateFromSpinResult(SocketManager.resultData);

    if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.linkFeatureResult != null)
    {
      uiManager.SetFeatureWinText(SocketManager.resultData.payload.linkFeatureResult.totalValue);
    }

    PopulateSymbols();

    // Stop non-frozen slots in column-major order (col-by-col, top-to-bottom)
    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (staticSymbol.freezedLocations[col].index[row] == 0) // Stop only non-frozen slots
        {
          int flattenedIndex = col * Slot[col].slotTransforms.Count + row;
          yield return StopSingleSlotTweening(3, Slot[col].slotTransforms[row], flattenedIndex, StopSpinToggle);
        }
      }
    }

    KillAllTweens();

    staticSymbol.GenerateFreezeMatrix(GenerateFreezedLocations());

    if (SocketManager.resultData.payload.winAmount > 0)
    {
      BonusEnd = true;
      yield return new WaitForSeconds(0.5f);
      yield return new WaitForSeconds(1f);

      int ccCount = 0;
      for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
      {
        for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
        {
          if (SocketManager.resultData.matrix[i][j] == "14")
          { 
            ccCount++;
          }
        }
      }

      foreach (var coin in allcoinPositions)
      {
        if (coin.symbolId == 16)
        {
          Transform cell = Slot[coin.position[1]].slotTransforms[coin.position[0]];
          SlotSymbolView symbolView = cell.GetComponentInChildren<SlotSymbolView>();
          if (symbolView != null && slotManager != null && slotManager.jackpotManager != null)
          {
              Sprite prizeSprite = null;
              if (slotManager.JackpotSlotSymbols != null && slotManager.JackpotSlotSymbols.Length > (coin.prizeTypeIndex ?? 0))
              {
                  prizeSprite = slotManager.JackpotSlotSymbols[coin.prizeTypeIndex ?? 0];
              }
              double jackpotAmount = coin.coinValue * slotManager.TotalBet;
              yield return slotManager.jackpotManager.PlayJackpotSequence(symbolView, coin.prizeType, coin.prizeTypeIndex ?? 0, jackpotAmount.ToString("F2"), prizeSprite);
          }
        }
      }

      var ccResult = SocketManager.resultData.payload.cashCollectResult;
      if (ccResult != null && ccResult.triggered)
      {
        yield return uiManager.PlayCashCollectSequence(ccResult);
      }
      else
      {
          var customResult = new CashCollectResult();
          customResult.triggered = true;
          customResult.collectedCoins = new List<CollectedCoin>();
          foreach (var coin in allcoinPositions)
          {
              if (coin.symbolId == 15 || coin.symbolId == 16 || coin.symbolId == 17 || coin.symbolId == 13)
              {
                  customResult.collectedCoins.Add(new CollectedCoin
                  {
                      position = coin.position,
                      coinValue = coin.coinValue,
                      symbolId = coin.symbolId,
                      symbolName = coin.symbolName
                  });
              }
          }
          
          if (customResult.collectedCoins.Count > 0)
          {
              var ccPositionsList = new List<Newtonsoft.Json.Linq.JArray>();
              for (int r = 0; r < SocketManager.resultData.matrix.Count; r++)
              {
                  for (int c = 0; c < SocketManager.resultData.matrix[r].Count; c++)
                  {
                      if (SocketManager.resultData.matrix[r][c] == "14")
                      {
                          var arr = new Newtonsoft.Json.Linq.JArray { r, c };
                          ccPositionsList.Add(arr);
                      }
                  }
              }
              customResult.positions = new Newtonsoft.Json.Linq.JArray(ccPositionsList.ToArray());
              yield return uiManager.PlayCashCollectSequence(customResult);
          }
      }


      allcoinPositions.Clear();
      allcoinPositions.TrimExcess();

      IsSpinning = false;
      yield return new WaitForSeconds(2f);
      StartCoroutine(EndBonus());
  
      yield break;
    }

    int remaining = SocketManager.resultData.payload.linkRespinsRemaining;
    slotManager.SetLinkRespinsRemaining(remaining);
    uiManager.SetBonusSpinCounter(remaining);


    IsSpinning = false;
    if (StopSpinToggle && remaining > 0)
    {
      StopSpinToggle = false;
      DOVirtual.DelayedCall(0.5f, () => {
        uiManager.UpdateFeatureButtonsState(false, remaining);
      });
    }
    else
    {
      StopSpinToggle = false;
      uiManager.UpdateFeatureButtonsState(false, remaining);
    }
  }

  private void PopulateSymbols()
  {
    for (int j = 0; j < SocketManager.resultData.matrix.Count; j++)         
    {
      for (int i = 0; i < 5; i++)
      {
        if (staticSymbol.freezedLocations[i].index[j] == 1)
        {
          continue; // Skip frozen slot positions
        }

        Transform cell = Slot[i].slotTransforms[j];
        Image img = cell.GetChild(2).GetComponent<Image>();
        SlotSymbolView view = img.GetComponent<SlotSymbolView>();
        if (view != null) view.ClearValues();

        int symbolId = int.Parse(SocketManager.resultData.matrix[j][i]);

        if (SocketManager.resultData.matrix[j][i] == "9")
        {
          img.sprite = index9Sprites[Random.Range(0, index9Sprites.Length)];
        }
        else if (SocketManager.resultData.matrix[j][i] == "13")
        {
          foreach (var coins in SocketManager.resultData.payload.coinPositions)
          {
            if (coins.symbolId == 13 && coins.position[0] == j && coins.position[1] == i)
            {
              if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > symbolId)
              {
                img.sprite = slotManager.SlotSymbols[symbolId];
              }
              if (view != null) view.SetMultiplierCoinValue(coins.coinValue, slotManager.TotalBet);
              break;
            }
          }
        }
        else if (SocketManager.resultData.matrix[j][i] == "15")
        {
          foreach (var coins in SocketManager.resultData.payload.coinPositions)
          {
            if (coins.position[0] == j && coins.position[1] == i)
            {
              img.sprite = coinFrame;
              if (view != null) view.SetGoldCoinValue(coins.coinValue * slotManager.TotalBet);
              break;
            }
          }
        }
        else if (SocketManager.resultData.matrix[j][i] == "17")
        {
          if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > symbolId)
          {
            img.sprite = slotManager.SlotSymbols[symbolId];
          }
          bool found = false;
          foreach (var coins in SocketManager.resultData.payload.coinPositions)
          {
            if (coins.position[0] == j && coins.position[1] == i)
            {
              if (view != null) view.SetLosPolosValue(coins.coinValue);
              found = true;
              break;
            }
          }
          if (!found)
          {
            int[] tempIndex = { 2, 3, 4, 5, 7 };
            int randomIndex = tempIndex[Random.Range(0, tempIndex.Length)];
            if (view != null) view.SetLosPolosValue(randomIndex);
          }
        }
        else if (SocketManager.resultData.matrix[j][i] == "14")
        {
          img.sprite = CC_Sprite;
        }
        else if (SocketManager.resultData.matrix[j][i] == "16")
        {
          img.sprite = Diamond_Sprite;
        }
        else if (SocketManager.resultData.matrix[j][i] == "11" || SocketManager.resultData.matrix[j][i] == "12")
        {
          if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > symbolId)
          {
            img.sprite = slotManager.SlotSymbols[symbolId];
          }
        }

        if (view != null)
        {
          slotManager.ConfigureSymbolView(view, symbolId);
        }
      }
    }
    foreach (var item in SocketManager.resultData.payload.coinPositions)
    {
      allcoinPositions.Add(item);
    }
  }

  private IEnumerator EndBonus()
  {
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.interactable = false;
      BonusSlot_CG.blocksRaycasts = false;
    }
    slotManager.IsBonus = false;

    double winAmt = SocketManager.resultData.payload.winAmount;

    // Show Walter Stash Grand Prize Popup first if triggered
    if (SocketManager.resultData != null && 
        SocketManager.resultData.payload != null && 
        SocketManager.resultData.payload.linkFeatureResult != null && 
        SocketManager.resultData.payload.linkFeatureResult.isWalterStashGrandPrize)
    {
      double grandPrizeAmt = SocketManager.resultData.payload.linkFeatureResult.grandPrizeAmount;
      bool stashPopupClosed = false;
      uiManager.OpenWalterStashPopup(grandPrizeAmt, () => {
          stashPopupClosed = true;
      });
      yield return new WaitUntil(() => stashPopupClosed);
    }

    bool popupClosed = false;
    uiManager.OpenFeatureWinPopup(winAmt, () => {
        popupClosed = true;
    });

    yield return new WaitUntil(() => popupClosed);

    if (winAmt > 0)
    {
      uiManager.WinningsTextAnimation();
    }

    bool freeSpinTriggered = SocketManager.resultData.payload.isFreeSpinTriggered;
    if (freeSpinTriggered)
    {
      bool isRetrigger = slotManager.WasFreeSpinPaused || slotManager.IsFreeSpin;

      // 1. Build the feature queue so that FreeSpins is enqueued and ready
      slotManager.featureQueue.BuildFromResponse(SocketManager.resultData.payload, isRetrigger);

      // 2. Consume the FreeSpin/FreeSpinRetrigger feature since we are playing it manually now
      if (slotManager.featureQueue.HasPending && 
          (slotManager.featureQueue.Peek() == FeatureType.FreeSpin || slotManager.featureQueue.Peek() == FeatureType.FreeSpinRetrigger))
      {
          slotManager.featureQueue.Dequeue();
      }

      // 3. Play the Free Spin Trigger sequence with the fromBonusSlot = true flag!
      var fsResult = SocketManager.resultData.payload.freeSpinResult;
      yield return uiManager.PlayFreeSpinTriggerSequence(fsResult, isRetrigger, true);

      // 4. Mark Free Spin active on slotManager and update HUD buttons
      slotManager.IsFreeSpin = true;
      slotManager.WasFreeSpinPaused = false;
      slotManager.IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();

      int remainingSpins = SocketManager.resultData.payload.freeSpinsRemaining;
      slotManager.SetFreeSpinsCount(remainingSpins);
      yield return new WaitForSeconds(1f);

      slotManager.TriggerSpinState(false);
      slotManager.FreeSpin(slotManager.FreeSpinsCount);
      yield break;
    }

    // Default flow when Free Spins are NOT triggered:
    uiManager.CloseBonusUI();
    yield return null;

    BonusSlot_CG.DOFade(0, 0.5f);
    NormalSlot_CG.DOFade(1, 0.5f).OnComplete(() =>
    {
      if (NormalSlot_CG != null)
      {
        NormalSlot_CG.interactable = true;
        NormalSlot_CG.blocksRaycasts = true;
      }
      if (BonusSlot_CG != null)
      {
        BonusSlot_CG.gameObject.SetActive(false);
      }

      staticSymbol.Reset();
      ResetMatrix();

      // OnLinkFeatureCompleted now handles ALL return paths:
      // - Processing remaining features in the queue (e.g., FreeSpin after Link)
      // - Resuming paused free spins
      // - Restoring auto spin
      // - Re-enabling buttons
      uiManager.SetNormalSpinButtonActive(true);
      slotManager.OnLinkFeatureCompleted();
    });
  }

  public IEnumerator TransitionFromBonusToNormalSlot()
  {
      if (BonusSlot_CG != null)
      {
          BonusSlot_CG.interactable = false;
          BonusSlot_CG.blocksRaycasts = false;
      }
      
      bool transitionDone = false;
      BonusSlot_CG.DOFade(0, 0.5f);
      NormalSlot_CG.DOFade(1, 0.5f).OnComplete(() =>
      {
          if (NormalSlot_CG != null)
          {
              NormalSlot_CG.interactable = true;
              NormalSlot_CG.blocksRaycasts = true;
          }
          if (BonusSlot_CG != null)
          {
              BonusSlot_CG.gameObject.SetActive(false);
          }

          staticSymbol.Reset();
          ResetMatrix();
          transitionDone = true;
      });
      
      yield return new WaitUntil(() => transitionDone);
  }

  private void ResetMatrix()
  {
    for (int i = 0; i < TotalMiniSlotImages.Count; i++)
    {
      for (int j = 0; j < TotalMiniSlotImages[i].slotImages.Count; j++)
      {
        int randomIndex = Random.Range(0, index9Sprites.Length);
        TotalMiniSlotImages[i].slotImages[j].sprite = index9Sprites[randomIndex];
        
        SlotSymbolView view = TotalMiniSlotImages[i].slotImages[j].GetComponent<SlotSymbolView>();
        if (view != null) view.ClearValues();
      }
    }
  }

  // Bonus slot start: anticipation bounce before entering the spin loop
  private void InitializeSingleSlotTweening(Transform slotTransform, bool bonus = false)
  {
    float startY = 307f;
    float endY = -670f;
    float anticipationUp = 15f;
    float anticipationDuration = 0.10f;

    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, startY);

    if (activeTweens.ContainsKey(slotTransform) && activeTweens[slotTransform] != null)
    {
      activeTweens[slotTransform].Kill();
    }

    // Use a Sequence for wind-up bounce then continuous loop
    Sequence seq = DOTween.Sequence();
    seq.Append(slotTransform.DOLocalMoveY(startY + anticipationUp, anticipationDuration).SetEase(Ease.OutQuad));
    seq.Append(slotTransform.DOLocalMoveY(startY, anticipationDuration * 0.5f).SetEase(Ease.InQuad));
    seq.OnComplete(() =>
    {
      // After bounce, start continuous loop spin
      Tweener tweener = slotTransform.DOLocalMoveY(endY, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      tweener.Play();
      activeTweens[slotTransform] = tweener;
    });
    
    activeTweens[slotTransform] = seq;
    seq.Play();
  }

  // Bonus slot stop: overshoot + settle bounce for satisfying landing
  private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool quickStop = false)
  {
    if (!activeTweens.ContainsKey(slotTransform) || activeTweens[slotTransform] == null)
    {
      Debug.Log("Tween not found for the specified slotTransform.");
      yield break;
    }

    Tween activeTween = activeTweens[slotTransform];
    if (activeTween is Sequence)
    {
      yield return activeTween.WaitForCompletion();
      activeTween = activeTweens[slotTransform];
    }

    if (activeTween != null)
    {
      bool IsRegister = false;
      yield return activeTween.OnStepComplete(() => IsRegister = true);
      yield return new WaitUntil(() => IsRegister);

      activeTween.Pause();
    }

    float finalY = 307f;
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, finalY);
    float targetY = 0f;
    float overshootDistance = 15f;

    // Phase 1: Snap to slightly past target (overshoot)
    Sequence stopSeq = DOTween.Sequence();
    stopSeq.Append(slotTransform.DOLocalMoveY(targetY - overshootDistance, 0.08f).SetEase(Ease.InQuad));
    // Phase 2: Bounce settle back to exact target
    stopSeq.Append(slotTransform.DOLocalMoveY(targetY, 0.15f).SetEase(Ease.OutBack, 1.5f));

    yield return stopSeq.WaitForCompletion();
    
    if (activeTween != null)
    {
      activeTween.Kill();
    }
    activeTweens.Remove(slotTransform);

    // Small stagger before next slot stops
    if (!quickStop)
    {
        yield return new WaitForSeconds(0.06f);
    }
  }

  private void KillAllTweens()
  {
    foreach (var kvp in activeTweens)
    {
      if (kvp.Value != null)
      {
        kvp.Value.Kill();
      }
    }
    activeTweens.Clear();
  }

  internal void OnInitialTransitionComplete()
  {
      if (BonusSlot_CG != null)
      {
          BonusSlot_CG.interactable = true;
          BonusSlot_CG.blocksRaycasts = true;
      }
      StartCoroutine(ConvertInitialLinksToCoins());
  }

  private IEnumerator ConvertInitialLinksToCoins()
  {
      var initialCoins = SocketManager.resultData.payload.linkFeatureResult?.initialCoins;
      if (initialCoins == null)
      {
        initialCoins = new List<InitialCoin>();
        if (SocketManager.resultData.payload.coinPositions != null)
        {
          foreach (var coin in SocketManager.resultData.payload.coinPositions)
          {
            InitialCoin initCoin = new InitialCoin();
            initCoin.position = coin.position;
            initCoin.coinValue = coin.coinValue;
            initCoin.symbolId = coin.symbolId;
            initCoin.symbolName = coin.symbolName;
            initialCoins.Add(initCoin);
          }
        }
      }

      foreach (var coin in initialCoins)
      {
        int row = coin.position[0];
        int col = coin.position[1];
        Transform cell = Slot[col].slotTransforms[row];
        Image img = cell.GetChild(2).GetComponent<Image>();
        SlotSymbolView view = img.GetComponent<SlotSymbolView>();
        if (view != null)
        {
          if (view.specialSymbolLayer != null)
          {
            view.specialSymbolLayer.gameObject.SetActive(false);
          }

          if (coin.symbolId == 17)
          {
            if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 17)
            {
              img.sprite = slotManager.SlotSymbols[17];
            }
            view.SetLosPolosValue(coin.coinValue);
            slotManager.ConfigureSymbolView(view, 17);
          }
          else if (coin.symbolId == 16)
          {
            img.sprite = Diamond_Sprite;
            slotManager.ConfigureSymbolView(view, 16);
          }
          else if (coin.symbolId == 13)
          {
            if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 13)
            {
              img.sprite = slotManager.SlotSymbols[13];
            }
            view.SetMultiplierCoinValue(coin.coinValue, slotManager.TotalBet);
            slotManager.ConfigureSymbolView(view, 13);
          }
          else
          {
            img.sprite = coinFrame;
            view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
            slotManager.ConfigureSymbolView(view, 15);
          }
        }
      }

      yield return new WaitForSeconds(0.2f);
      uiManager.UpdateFeatureButtonsState(false, slotManager.LinkRespinsRemaining);
  }

  private List<List<int>> GenerateFreezedLocations()
  {
    List<List<int>> loc = new();
    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (staticSymbol.freezedLocations[col].index[row] == 0 &&
            (SocketManager.resultData.matrix[row][col] == "11" || SocketManager.resultData.matrix[row][col] == "12" || SocketManager.resultData.matrix[row][col] == "14" || SocketManager.resultData.matrix[row][col] == "15" || SocketManager.resultData.matrix[row][col] == "16" || SocketManager.resultData.matrix[row][col] == "17"))
        {
          List<int> rXc = new() { row, col };
          loc.Add(rXc);
        }
      }
    }
    return loc;
  }
}
