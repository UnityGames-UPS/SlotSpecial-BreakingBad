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
  [SerializeField] internal List<Column> freezedLocations = new();
  [SerializeField] internal List<List<int>> Locations = new();


  [Header("Sprites References")]
  [SerializeField] private Sprite[] index9Sprites;
  [SerializeField] private Sprite coinFrame;
  [SerializeField] private Sprite CC_Sprite;
  [SerializeField] private Sprite Diamond_Sprite;
  [Header("Transition Animation Sprites")]
  [SerializeField] private Sprite[] LinkToGoldCoin_Animation;
  [SerializeField] private Sprite[] MegaLinkToGoldCoin_Animation;

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
    if (AudioController.Instance != null) AudioController.Instance.PlayBonusBg();
    ResetStaticSymbol();
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
          initCoin.prizeType = coin.prizeType;
          initCoin.prizeTypeIndex = coin.prizeTypeIndex;
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
      
      string matrixVal = SocketManager.resultData.matrix[row][col];
      if (matrixVal == "11" || matrixVal == "12")
      {
        Sprite normalSlotSprite = slotManager.GetResultMatrixImage(row, col).sprite;
        img.sprite = normalSlotSprite;
        if (view != null)
        {
          view.ClearValues();
        }
      }
      else
      {
        if (view != null) view.ClearValues();
        
        if (coin.symbolId == 17)
        {
          if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 17)
          {
            img.sprite = slotManager.SlotSymbols[17];
          }
          if (view != null) view.SetLosPolosValue((int)coin.coinValue);
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
          if (view != null) view.SetMultiplierCoinValue(coin.coinValue, slotManager.TotalBet);
          slotManager.ConfigureSymbolView(view, 13);
        }
        else
        {
          img.sprite = coinFrame;
          if (view != null) view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
          slotManager.ConfigureSymbolView(view, 15);
        }
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
    // staticSymbol.TurnOnIndices(initialLocations);
    GenerateFreezeMatrix(initialLocations);

    NormalSlot_CG.DOFade(0, 0.5f);
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.gameObject.SetActive(true);
    }
    BonusSlot_CG.DOFade(1, .5f).OnComplete(() =>
    {
      StartCoroutine(PlayTransitionAnimations());
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
        if (freezedLocations[col].index[row] == 0) // Only initialize non-frozen slots
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
        if (freezedLocations[col].index[row] == 0) // Stop only non-frozen slots
        {
          int flattenedIndex = col * Slot[col].slotTransforms.Count + row;
          yield return StopSingleSlotTweening(3, Slot[col].slotTransforms[row], flattenedIndex, StopSpinToggle);
        }
      }
    }

    KillAllTweens();

    GenerateFreezeMatrix(GenerateFreezedLocations());

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
              yield return slotManager.jackpotManager.PlayJackpotSequence(symbolView, coin.prizeType, coin.prizeTypeIndex ?? 0, jackpotAmount.ToString("0.###"), prizeSprite);
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
        if (freezedLocations[i].index[j] == 1)
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
              if (view != null) view.SetLosPolosValue((int)coins.coinValue);
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
      uiManager.WinningsTextAnimation(null, winAmt);
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

      ResetStaticSymbol();

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
    if (AudioController.Instance != null) AudioController.Instance.PlayMainBg();
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

      ResetStaticSymbol();
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

          ResetStaticSymbol();
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

    if (AudioController.Instance != null) AudioController.Instance.PlaySlotStop();

    // Phase 1: Snap to slightly past target (overshoot)
    Sequence stopSeq = DOTween.Sequence();
    stopSeq.Append(slotTransform.DOLocalMoveY(targetY - overshootDistance, 0.08f).SetEase(Ease.InQuad));
    // Phase 2: Bounce settle back to exact target
    stopSeq.Append(slotTransform.DOLocalMoveY(targetY, 0.15f).SetEase(Ease.OutBack, 1.5f));

    yield return stopSeq.WaitForCompletion();

    int col = index / 3;
    int row = index % 3;

    if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
        row < SocketManager.resultData.matrix.Count && col < SocketManager.resultData.matrix[row].Count)
    {
        string symbolIdStr = SocketManager.resultData.matrix[row][col];
        if (int.TryParse(symbolIdStr, out int symbolId))
        {
            if (symbolId == 15 || symbolId == 16) // Gold Coin / Prize Coin
            {
                if (AudioController.Instance != null) AudioController.Instance.PlayCashCoinLand();
            }
            else if (symbolId == 11 || symbolId == 12) // Link / MegaLink
            {
                if (AudioController.Instance != null) AudioController.Instance.PlayLinkLand();
            }
            else if (symbolId == 14 || symbolId == 17) // Cash Collect / Los Pollos
            {
                if (AudioController.Instance != null) AudioController.Instance.PlayCashCollectLand();
            }
        }
    }
    
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
            initCoin.prizeType = coin.prizeType;
            initCoin.prizeTypeIndex = coin.prizeTypeIndex;
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
            view.SetLosPolosValue((int)coin.coinValue);
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
        if (freezedLocations[col].index[row] == 0 &&
            (SocketManager.resultData.matrix[row][col] == "11" || SocketManager.resultData.matrix[row][col] == "12" || SocketManager.resultData.matrix[row][col] == "13" || SocketManager.resultData.matrix[row][col] == "14" || SocketManager.resultData.matrix[row][col] == "15" || SocketManager.resultData.matrix[row][col] == "16" || SocketManager.resultData.matrix[row][col] == "17"))
        {
          List<int> rXc = new() { row, col };
          loc.Add(rXc);
        }
      }
    }
    return loc;
  }

  private IEnumerator PlayTransitionAnimations()
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
            initCoin.prizeType = coin.prizeType;
            initCoin.prizeTypeIndex = coin.prizeTypeIndex;
            initialCoins.Add(initCoin);
          }
        }
      }

      List<ImageAnimation> runningAnims = new List<ImageAnimation>();
      List<bool> animCompleted = new List<bool>();

      foreach (var coin in initialCoins)
      {
          int row = coin.position[0];
          int col = coin.position[1];
          string matrixVal = SocketManager.resultData.matrix[row][col];

          int targetSymbolId = coin.symbolId;
          if (targetSymbolId == 11 || targetSymbolId == 12)
          {
              targetSymbolId = 15;
          }

          CoinPosition coinPos = new CoinPosition
          {
              position = coin.position,
              coinValue = coin.coinValue,
              symbolId = targetSymbolId,
              symbolName = coin.symbolName,
              prizeType = coin.prizeType,
              prizeTypeIndex = coin.prizeTypeIndex
          };
          allcoinPositions.Add(coinPos);

          if (matrixVal == "11" || matrixVal == "12")
          {
              Transform cell = Slot[col].slotTransforms[row];
              
              // Pre-configure static main cell and disable the image component (keep gameobject active so SlotManager can find view)
              Image mainImg = cell.GetChild(2).GetComponent<Image>();
              SlotSymbolView view = mainImg.GetComponent<SlotSymbolView>();
              if (view != null)
              {
                  view.ClearValues();
                  if (coin.symbolId == 17)
                  {
                      if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 17)
                      {
                          mainImg.sprite = slotManager.SlotSymbols[17];
                      }
                      view.SetLosPolosValue((int)coin.coinValue);
                      slotManager.ConfigureSymbolView(view, 17);
                  }
                  else if (coin.symbolId == 16)
                  {
                      mainImg.sprite = Diamond_Sprite;
                      slotManager.ConfigureSymbolView(view, 16);
                  }
                  else if (coin.symbolId == 13)
                  {
                      if (slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 13)
                      {
                          mainImg.sprite = slotManager.SlotSymbols[13];
                      }
                      view.SetMultiplierCoinValue(coin.coinValue, slotManager.TotalBet);
                      slotManager.ConfigureSymbolView(view, 13);
                  }
                  else
                  {
                      mainImg.sprite = coinFrame;
                      view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
                      slotManager.ConfigureSymbolView(view, 15);
                  }
              }

              // Get the animation cell from animationManager instead of slot icon children
              ImageAnimation anim = slotManager.animationManager.GetAnimationCell(row, col);

              if (anim != null)
              {
                  anim.transform.position = cell.position;
                  anim.DOKill();

                  CanvasGroup animCG = anim.GetComponent<CanvasGroup>();
                  if (animCG != null)
                  {
                      animCG.DOKill();
                      animCG.alpha = 1f;
                  }

                  if (anim.rendererDelegate != null)
                  {
                      anim.rendererDelegate.DOKill();
                      anim.rendererDelegate.color = Color.white;
                  }

                  anim.gameObject.SetActive(true);

                  anim.textureArray.Clear();
                  anim.textureArray.TrimExcess();
                  Sprite[] animSprites = (matrixVal == "11") ? LinkToGoldCoin_Animation : MegaLinkToGoldCoin_Animation;
                  if (animSprites != null)
                  {
                      foreach (Sprite s in animSprites)
                      {
                          anim.textureArray.Add(s);
                      }
                  }

                  // 1.5 - 2 sec duration (1.8 seconds is perfect)
                  anim.useDynamicFramerate = true;
                  anim.dynamicLoopDuration = 1.8f;
                  anim.doLoopAnimation = false;

                  int animIndex = runningAnims.Count;
                  animCompleted.Add(false);
                  anim.onLoopComplete = (loopCount) =>
                  {
                      animCompleted[animIndex] = true;
                  };

                  anim.StartAnimation();
                  runningAnims.Add(anim);

                  // Keep mainImg GameObject active so GetComponentInChildren does not return null, only disable the Image renderer component
                  mainImg.enabled = false;
              }
          }
      }

      if (runningAnims.Count > 0)
      {
          // Wait until they reach frame 46 (to show the text)
          yield return new WaitUntil(() => {
              foreach (var anim in runningAnims)
              {
                  if (anim != null && anim.rendererDelegate != null && anim.textureArray.Count > 46)
                  {
                      int currentFrame = anim.textureArray.IndexOf(anim.rendererDelegate.sprite);
                      if (currentFrame < 46)
                      {
                          return false; // Wait until all reach at least frame 46
                      }
                  }
              }
              return true;
          });

          // Enable text object with value in AnimationTextHelper on the animation cell
          foreach (var coin in initialCoins)
          {
              int row = coin.position[0];
              int col = coin.position[1];
              string matrixVal = SocketManager.resultData.matrix[row][col];
              if (matrixVal == "11" || matrixVal == "12")
              {
                  ImageAnimation anim = slotManager.animationManager.GetAnimationCell(row, col);
                  if (anim != null)
                  {
                      AnimationTextHelper textHelper = anim.GetComponent<AnimationTextHelper>();
                      if (textHelper == null)
                      {
                          textHelper = anim.gameObject.AddComponent<AnimationTextHelper>();
                      }
                      textHelper.SetupFromHierarchy();

                      string formattedText = "";
                      int symbolId = coin.symbolId;
                       if (symbolId == 17)
                       {
                           string valStr = coin.coinValue.ToString("0.###");
                           formattedText = "<sprite=10>";
                           foreach (char c in valStr)
                           {
                               if (char.IsDigit(c)) formattedText += $"<sprite={c - '0'}>";
                           }
                           textHelper.PlayTextAnimation(17, formattedText, 1f, false);
                       }
                       else if (symbolId == 13)
                       {
                           formattedText = "X" + coin.coinValue.ToString("0.###");
                           textHelper.PlayTextAnimation(13, formattedText, 1f, false);
                       }
                       else if (symbolId == 16)
                       {
                           textHelper.Clear();
                       }
                       else
                       {
                           double value = coin.coinValue * slotManager.TotalBet;
                           string valStr = value.ToString("0.###");
                          formattedText = "";
                          foreach (char c in valStr)
                          {
                              if (char.IsDigit(c))
                              {
                                  formattedText += $"<sprite={c - '0'}>";
                              }
                              else if (c == '.')
                              {
                                  formattedText += "<sprite=10>";
                              }
                          }
                          textHelper.PlayTextAnimation(15, formattedText, 1f, false);
                      }
                  }
              }
          }

          // Wait until the single loop animation ends
          yield return new WaitUntil(() => {
              foreach (bool completed in animCompleted)
              {
                  if (!completed) return false;
              }
              return true;
          });

          // Stop animations, hide animation cells, and clear animation texts
          foreach (var anim in runningAnims)
          {
              if (anim != null)
              {
                  anim.StopAnimation();
                  anim.onLoopComplete = null;

                  AnimationTextHelper textHelper = anim.GetComponent<AnimationTextHelper>();
                  if (textHelper != null)
                  {
                      textHelper.Clear();
                  }

                  anim.gameObject.SetActive(false);
              }
          }

          // Enable main slot cells and configure the sticky symbol overlay
          foreach (var coin in initialCoins)
          {
              int row = coin.position[0];
              int col = coin.position[1];
              string matrixVal = SocketManager.resultData.matrix[row][col];
              if (matrixVal == "11" || matrixVal == "12")
              {
                  // Removed StickySymbolManager activations since we freeze the main slots directly
                  Transform cell = Slot[col].slotTransforms[row];
                  Image mainImg = cell.GetChild(2).GetComponent<Image>();
                  mainImg.enabled = true;
              }
          }
      }


      yield return new WaitForSeconds(0.5f);
      OnInitialTransitionComplete();
  }

  internal List<List<int>> GenerateFreezeMatrix(List<List<int>> loc, bool dontReturn = false)
  {
      for (int i = 0; i < loc.Count; i++)
      {
          if (!Locations.Contains(loc[i]))
              Locations.Add(loc[i]);
      }

      // Build matrix of zeros
      List<List<int>> freezeMatrix = new List<List<int>>();
      for (int i = 0; i < Slot.Count; i++)
      {
          List<int> row = new List<int>(new int[Slot[i].slotTransforms.Count]);
          freezeMatrix.Add(row);
      }

      // Mark frozen positions
      foreach (List<int> indexPair in Locations)
      {
          if (indexPair.Count == 2)
          {
              int row = indexPair[0];
              int column = indexPair[1];
              if (column >= 0 && column < freezeMatrix.Count &&
                  row >= 0 && row < freezeMatrix[column].Count)
              {
                  freezeMatrix[column][row] = 1;
              }
          }
      }

      // Sync freezedLocations
      freezedLocations.Clear();
      foreach (var row in freezeMatrix)
      {
          Column column = new() { index = new List<int>(row) };
          freezedLocations.Add(column);
      }

      return dontReturn ? null : freezeMatrix;
  }

  internal void ResetStaticSymbol()
  {
      freezedLocations.Clear();
      freezedLocations.TrimExcess();
      Locations.Clear();
      Locations.TrimExcess();
  }
}
