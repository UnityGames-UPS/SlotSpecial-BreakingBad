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
  [SerializeField] private List<SlotImage> TotalMiniSlotImages;     
  [SerializeField] internal List<SlotTransform> Slot;

  private Dictionary<Transform, Tween> activeTweens = new Dictionary<Transform, Tween>();
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

  internal IEnumerator StartBonus(int count, bool isFromNormalSpin)
  {
    if (isFromNormalSpin)
    {
      if (DialoguePopupManager.Instance != null)
      {
        yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.LinkFeatureStart);
      }
    }

    bool popupClicked = false;
    uiManager.OpenFeaturePopup(() => {
        popupClicked = true;
    });
    yield return new WaitUntil(() => popupClicked);

    StartBonusGame(count);
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
            slotManager.ConfigureSymbolView(view, 14); 

            int spinsRemaining = 0;
            bool isLocked = false;
            if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.lockedCashCollects != null)
            {
                foreach (var locked in SocketManager.resultData.payload.lockedCashCollects)
                {
                    if (locked.position != null && locked.position.Count == 2 && locked.position[0] == row && locked.position[1] == col)
                    {
                        spinsRemaining = locked.spinsRemaining;
                        isLocked = true;
                        break;
                    }
                }
            }
            if (isLocked)
            {
                view.SetCountValue(spinsRemaining);
            }
          }
        }
      }
    }

    
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

    
    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (freezedLocations[col].index[row] == 0) 
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

    
    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (freezedLocations[col].index[row] == 0) 
        {
          int flattenedIndex = col * Slot[col].slotTransforms.Count + row;
          yield return StopSingleSlotTweening(3, Slot[col].slotTransforms[row], flattenedIndex, StopSpinToggle);
        }
      }
    }

    KillAllTweens();

    GenerateFreezeMatrix(GenerateFreezedLocations());

    for (int col = 0; col < Slot.Count; col++)
    {
      for (int row = 0; row < Slot[col].slotTransforms.Count; row++)
      {
        if (SocketManager.resultData.matrix[row][col] == "14")
        {
          Transform cell = Slot[col].slotTransforms[row];
          Image img = cell.GetChild(2).GetComponent<Image>();
          SlotSymbolView view = img.GetComponent<SlotSymbolView>();
          if (view != null)
          {
            int spinsRemaining = 0;
            bool isLocked = false;
            if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.lockedCashCollects != null)
            {
                foreach (var locked in SocketManager.resultData.payload.lockedCashCollects)
                {
                    if (locked.position != null && locked.position.Count == 2 && locked.position[0] == row && locked.position[1] == col)
                    {
                        spinsRemaining = locked.spinsRemaining;
                        isLocked = true;
                        break;
                    }
                }
            }
            if (isLocked)
            {
                view.SetCountValue(spinsRemaining);
            }
            else
            {
                view.SetCountValue(0);
            }
          }
        }
      }
    }

    // Play multiplier conversion animation for newly landed symbol 13
    if (SocketManager.resultData.payload.coinPositions != null)
    {
      List<Coroutine> activeCoroutines = new List<Coroutine>();
      foreach (var coins in SocketManager.resultData.payload.coinPositions)
      {
        if (coins.symbolId == 13)
        {
          int row = coins.position[0];
          int col = coins.position[1];
          Transform cell = Slot[col].slotTransforms[row];
          SlotSymbolView view = cell.GetChild(2).GetComponent<SlotSymbolView>();
          if (view != null)
          {
            activeCoroutines.Add(StartCoroutine(view.PlayMultiplierConversion(coins.coinValue, slotManager.TotalBet)));
          }
        }
      }
      foreach (var coroutine in activeCoroutines)
      {
        yield return coroutine;
      }
    }

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
          continue; 
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
          int spinsRemaining = 0;
          bool isLocked = false;
          if (SocketManager.resultData != null && SocketManager.resultData.payload != null && SocketManager.resultData.payload.lockedCashCollects != null)
          {
              foreach (var locked in SocketManager.resultData.payload.lockedCashCollects)
              {
                  if (locked.position != null && locked.position.Count == 2 && locked.position[0] == j && locked.position[1] == i)
                  {
                      spinsRemaining = locked.spinsRemaining;
                      isLocked = true;
                      break;
                  }
              }
          }
          if (view != null)
          {
              if (isLocked)
              {
                  view.SetCountValue(spinsRemaining);
              }
              else
              {
                  view.SetCountValue(0);
              }
          }
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

    if (slotManager.WasFreeSpinPaused)
    {
      uiManager.AddToAccumulatedFreeSpinWin(winAmt);
    }

    
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

    bool wasTriggeredInNormalSpin = !slotManager.WasFreeSpinPaused;
    if (wasTriggeredInNormalSpin)
    {
      if (DialoguePopupManager.Instance != null)
      {
        yield return DialoguePopupManager.Instance.PlayVideoScenario(VideoScenario.LinkFeatureEnd);
      }
    }

    if (DialoguePopupManager.Instance != null)
    {
      yield return DialoguePopupManager.Instance.PlayDiamondSlotEndDialogue(winAmt, slotManager.TotalBet);
    }

    bool popupClosed = false;
    uiManager.OpenFeatureWinPopup(winAmt, () => {
        popupClosed = true;
    });

    yield return new WaitUntil(() => popupClosed);

    if (winAmt > 0)
    {
      bool winningsAnimClosed = false;
      uiManager.WinningsTextAnimation(() => {
          winningsAnimClosed = true;
      }, winAmt);
      yield return new WaitUntil(() => winningsAnimClosed);
    }

    bool freeSpinTriggered = SocketManager.resultData.payload.isFreeSpinTriggered;
    if (freeSpinTriggered)
    {
      bool isRetrigger = slotManager.WasFreeSpinPaused || slotManager.IsFreeSpin;

      
      slotManager.featureQueue.BuildFromResponse(SocketManager.resultData.payload, isRetrigger);

      
      if (slotManager.featureQueue.HasPending && 
          (slotManager.featureQueue.Peek() == FeatureType.FreeSpin || slotManager.featureQueue.Peek() == FeatureType.FreeSpinRetrigger))
      {
          slotManager.featureQueue.Dequeue();
      }

      
      var fsResult = SocketManager.resultData.payload.freeSpinResult;
      yield return uiManager.PlayFreeSpinTriggerSequence(fsResult, isRetrigger, true);

      ResetStaticSymbol();

      
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

    
    if (AudioController.Instance != null) AudioController.Instance.PlayMainBg();
    uiManager.CloseBonusUI();
    yield return null;

    slotManager.IsFeatureTransitioning = true;
    uiManager.UpdateButtonsState();

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

      slotManager.IsFeatureTransitioning = false;
      uiManager.UpdateButtonsState();

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

    
    Sequence seq = DOTween.Sequence();
    seq.Append(slotTransform.DOLocalMoveY(startY + anticipationUp, anticipationDuration).SetEase(Ease.OutQuad));
    seq.Append(slotTransform.DOLocalMoveY(startY, anticipationDuration * 0.5f).SetEase(Ease.InQuad));
    seq.OnComplete(() =>
    {
      
      Tweener tweener = slotTransform.DOLocalMoveY(endY, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      tweener.Play();
      activeTweens[slotTransform] = tweener;
    });
    
    activeTweens[slotTransform] = seq;
    seq.Play();
  }

  
  private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool quickStop = false)
  {
    if (!activeTweens.ContainsKey(slotTransform) || activeTweens[slotTransform] == null)
    {
      
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

    int rowCount = 3;
    if (Slot != null && Slot.Count > 0 && Slot[0].slotTransforms != null)
    {
        rowCount = Slot[0].slotTransforms.Count;
    }
    int col = index / rowCount;
    int row = index % rowCount;

    bool isJammedStop = false;
    if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
        row < SocketManager.resultData.matrix.Count && col < SocketManager.resultData.matrix[row].Count)
    {
        string symbolIdStr = SocketManager.resultData.matrix[row][col];
        if (symbolIdStr != "9")
        {
            isJammedStop = true;
        }
    }

    float finalY = 307f;
    float originalX = slotTransform.localPosition.x;
    slotTransform.localPosition = new Vector2(originalX, finalY);
    float targetY = 0f;

    if (AudioController.Instance != null) AudioController.Instance.PlaySlotStop();

    Sequence stopSeq = DOTween.Sequence();
    if (isJammedStop)
    {
        float slowDuration = 0.8f;
        
        // Slow move down to target Y
        stopSeq.Append(slotTransform.DOLocalMoveY(targetY, slowDuration).SetEase(Ease.OutQuad));
        
        yield return stopSeq.WaitForCompletion();
        slotTransform.localPosition = new Vector2(originalX, targetY);
    }
    else
    {
        float overshootDistance = 15f;
        stopSeq.Append(slotTransform.DOLocalMoveY(targetY - overshootDistance, 0.08f).SetEase(Ease.InQuad));
        stopSeq.Append(slotTransform.DOLocalMoveY(targetY, 0.15f).SetEase(Ease.OutBack, 1.5f));
        yield return stopSeq.WaitForCompletion();
    }

    if (SocketManager.resultData != null && SocketManager.resultData.matrix != null &&
        row < SocketManager.resultData.matrix.Count && col < SocketManager.resultData.matrix[row].Count)
    {
        string symbolIdStr = SocketManager.resultData.matrix[row][col];
        if (int.TryParse(symbolIdStr, out int symbolId))
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
    
    if (activeTween != null)
    {
      activeTween.Kill();
    }
    activeTweens.Remove(slotTransform);

    
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

      List<Coroutine> activeCoroutines = new List<Coroutine>();
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
            slotManager.ConfigureSymbolView(view, 13);
            activeCoroutines.Add(StartCoroutine(view.PlayMultiplierConversion(coin.coinValue, slotManager.TotalBet)));
          }
          else
          {
            img.sprite = coinFrame;
            view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
            slotManager.ConfigureSymbolView(view, 15);
          }
        }
      }

      foreach (var coroutine in activeCoroutines)
      {
        yield return coroutine;
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

                  
                  mainImg.enabled = false;
              }
          }
      }

      if (runningAnims.Count > 0)
      {
          if (AudioController.Instance != null) AudioController.Instance.PlayLinkToCoinTransition();

          yield return new WaitUntil(() => {
              foreach (var anim in runningAnims)
              {
                  if (anim != null && anim.rendererDelegate != null && anim.textureArray.Count > 46)
                  {
                      int currentFrame = anim.textureArray.IndexOf(anim.rendererDelegate.sprite);
                      if (currentFrame < 46)
                      {
                          return false; 
                      }
                  }
              }
              return true;
          });

          
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

          
          yield return new WaitUntil(() => {
              foreach (bool completed in animCompleted)
              {
                  if (!completed) return false;
              }
              return true;
          });

          
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

          
          foreach (var coin in initialCoins)
          {
              int row = coin.position[0];
              int col = coin.position[1];
              string matrixVal = SocketManager.resultData.matrix[row][col];
              if (matrixVal == "11" || matrixVal == "12")
              {
                  
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

      
      List<List<int>> freezeMatrix = new List<List<int>>();
      for (int i = 0; i < Slot.Count; i++)
      {
          List<int> row = new List<int>(new int[Slot[i].slotTransforms.Count]);
          freezeMatrix.Add(row);
      }

      
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
