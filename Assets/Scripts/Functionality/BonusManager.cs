using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;

public class BonusManager : MonoBehaviour
{
  [Header("Scripts References")]
  
  [SerializeField] private SlotManager slotManager;
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private StickySymbolManager staticSymbol;
  [SerializeField] private ImageAnimation BonusWinningsImageAnimation;

  [Header("Sprites References")]
  [SerializeField] private Sprite[] index9Sprites;
  [SerializeField] private Sprite coinFrame;
  [SerializeField] private Sprite CC_Sprite;
  [SerializeField] private Sprite Diamond_Sprite;

  [Header("UI Objects References")]
  [SerializeField] private CanvasGroup NormalSlot_CG;
  [SerializeField] private CanvasGroup BonusSlot_CG;

  [SerializeField] private Transform GrandPayoutTRTransform;
  [SerializeField] private Transform BonusWinningsPosition;
  [SerializeField] private CanvasGroup WinningsUI_Panel;
  [SerializeField] private CanvasGroup FreeSpinsCounterUI_Panel;
  [SerializeField] private CanvasGroup lines, lineBet, totalBet;

  [Header("Slot References")]
  [SerializeField] private List<SlotImage> TotalMiniSlotImages;     //class to store total images
  [SerializeField] private List<SlotTransform> Slot;

  private List<KeyValuePair<Transform, Tweener>> singleSlotTweens = new List<KeyValuePair<Transform, Tweener>>();
  private int IconSizeFactor = 202;
  private bool IsSpinning;
  private bool BonusEnd = false;
  private Coroutine BonusRoutine;
  private float SpinDelay = 0.2f;
  
  [SerializeField] private List<CoinPosition> allcoinPositions = new List<CoinPosition>();

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
    if (BonusWinningsImageAnimation != null)
    {
      BonusWinningsImageAnimation.gameObject.SetActive(false);
    }
  }

  internal void StartBonus(int count)
  {
    if (FreeSpinsCounterUI_Panel.alpha != 0)
    {
      FreeSpinsCounterUI_Panel.DOFade(1, 0.3f);
    }
    
    // Hide standard panel menus
    uiManager.FadeLinesUI(0f, 0.3f);
    uiManager.FadeTotalBetUI(0f, 0.3f);
    uiManager.FadeLineBetUI(0f, 0.3f);

    uiManager.SetBonusButtonActive(true);
    uiManager.SetBonusButtonInteractable(false);
    uiManager.SetBonusSpinCounter(count);
    
    WinningsUI_Panel.DOFade(1, 0.3f);

    NormalSlot_CG.DOFade(0, 0.5f);
    if (BonusSlot_CG != null)
    {
      BonusSlot_CG.gameObject.SetActive(true);
    }
    BonusSlot_CG.DOFade(1, .5f).OnComplete(() =>
    {
      StartCoroutine(staticSymbol.ChangeLinksToGoldCoin(uiManager.GetBonusStartButton()));
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

  private void StartBonusSlot()
  {
    uiManager.SetBonusButtonInteractable(false);

    int spinCount = slotManager.LinkRespinsRemaining;
    spinCount -= 1;
    slotManager.SetLinkRespinsRemaining(spinCount);
    uiManager.SetBonusSpinCounter(spinCount);

    BonusRoutine = StartCoroutine(BonusTweenRoutine());
  }

  private IEnumerator BonusTweenRoutine()
  {
    IsSpinning = true;

    // Initialize tweening for non-frozen slot animations
    for (int row = 0; row < Slot.Count; row++)
    {
      for (int col = 0; col < Slot[row].slotTransforms.Count; col++)
      {
        if (staticSymbol.freezedLocations[row].index[col] == 0) // Only initialize non-frozen slots
        {
          InitializeSingleSlotTweening(Slot[row].slotTransforms[col]);
        }
      }
    }

    SocketManager.AccumulateResult(slotManager.BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);
    slotManager.UpdateFromSpinResult(SocketManager.resultData);

    // Create a list of all slot indices for randomization
    List<(int row, int col)> indices = new List<(int, int)>();
    for (int row = 0; row < Slot.Count; row++)
    {
      for (int col = 0; col < Slot[row].slotTransforms.Count; col++)
      {
        indices.Add((row, col));
      }
    }

    // Shuffle the list to get random indices
    System.Random random = new System.Random();
    indices = indices.OrderBy(x => random.Next()).ToList();
    PopulateSymbols();

    foreach (var (row, col) in indices)
    {
      if (staticSymbol.freezedLocations[row].index[col] == 0) // Stop only non-frozen slots
      {
        int flattenedIndex = row * Slot[row].slotTransforms.Count + col;
        yield return StopSingleSlotTweening(3, Slot[row].slotTransforms[col], flattenedIndex);
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
          Transform cell = Slot[coin.position[0]].slotTransforms[coin.position[1]];
          SlotSymbolView symbolView = cell.GetComponentInChildren<SlotSymbolView>();
          if (symbolView != null && slotManager != null && slotManager.jackpotManager != null)
          {
              Sprite prizeSprite = null;
              if (slotManager.JackpotSlotSymbols != null && slotManager.JackpotSlotSymbols.Length > (coin.prizeTypeIndex ?? 0))
              {
                  prizeSprite = slotManager.JackpotSlotSymbols[coin.prizeTypeIndex ?? 0];
              }
              yield return slotManager.jackpotManager.PlayJackpotSequence(symbolView, coin.prizeTypeIndex ?? 0, coin.coinValue.ToString(), prizeSprite);
          }
        }
      }
      foreach (var coin in allcoinPositions)
      {
        if (coin.symbolId == 15 || coin.symbolId == 11 || coin.symbolId == 12)
        {
          yield return uiManager.TrailRendererAnimation(Slot[coin.position[0]].slotTransforms[coin.position[1]].GetChild(3).GetChild(1).gameObject, 0, coin.coinValue, true);
        }
      }

      allcoinPositions.Clear();
      allcoinPositions.TrimExcess();

      IsSpinning = false;
      yield return new WaitForSeconds(2f);
      StartCoroutine(EndBonus());
      uiManager.SetBonusWinningsText("0");
      yield break;
    }

    int remaining = SocketManager.resultData.payload.linkRespinsRemaining;
    slotManager.SetLinkRespinsRemaining(remaining);
    uiManager.SetBonusSpinCounter(remaining);

    uiManager.SetBonusButtonInteractable(false);
    IsSpinning = false;
  }

  private void PopulateSymbols()
  {
    for (int j = 0; j < SocketManager.resultData.matrix.Count; j++)         
    {
      for (int i = 0; i < 5; i++)
      {
        Transform cell = Slot[j].slotTransforms[i];
        Image img = cell.GetChild(3).GetComponent<Image>();
        SlotSymbolView view = img.GetComponent<SlotSymbolView>();
        if (view != null) view.ClearValues();

        int symbolId = int.Parse(SocketManager.resultData.matrix[j][i]);

        if (SocketManager.resultData.matrix[j][i] == "9")
        {
          img.sprite = index9Sprites[Random.Range(0, index9Sprites.Length)];
        }
        else if (SocketManager.resultData.matrix[j][i] == "15")
        {
          foreach (var coins in SocketManager.resultData.payload.coinPositions)
          {
            if (coins.position[0] == j && coins.position[1] == i)
            {
              img.sprite = coinFrame;
              if (view != null) view.SetGoldCoinValue(coins.coinValue);
              break;
            }
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
    slotManager.IsBonus = false;
    uiManager.SetBonusButtonActive(false);

    if (SocketManager.resultData.payload.winAmount > 0)
    {
      uiManager.BonusWinningsCoroutine = StartCoroutine(uiManager.MidGameImageAnimation(BonusWinningsImageAnimation, SocketManager.resultData.payload.winAmount));
      yield return new WaitUntil(() => uiManager.animationFinish);
      uiManager.WinningsTextAnimation();
    }
    WinningsUI_Panel.DOFade(0, 0.3f);

    BonusSlot_CG.DOFade(0, 0.5f);
    NormalSlot_CG.DOFade(1, 0.5f).OnComplete(() =>
    {
      if (BonusSlot_CG != null)
      {
        BonusSlot_CG.gameObject.SetActive(false);
      }
      slotManager.OnLinkFeatureCompleted();

      if (slotManager.LinkRespinsRemaining <= 0)
      {
        uiManager.CloseFreeSpinsUI();
        if (slotManager.WasAutoSpinOn)
        {
          DOVirtual.DelayedCall(0.2f, () =>
          {
            uiManager.SetNormalSpinButtonActive(true);
            slotManager.AutoSpin();
          });
        }
        else
        {
          uiManager.SetNormalSpinButtonActive(true);
          uiManager.SetButtonsInteractable(true);
        }
      }
      else
      {
        DOVirtual.DelayedCall(0.5f, () =>
        {
          uiManager.OpenFreeSpinsUI();
          slotManager.FreeSpin(slotManager.LinkRespinsRemaining);
        });
      }

      staticSymbol.Reset();
      ResetMatrix();
    });
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
    Tweener tweener = null;
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 307f);
    tweener = slotTransform.DOLocalMoveY(-670, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
    tweener.Play();
    singleSlotTweens.Add(new KeyValuePair<Transform, Tweener>(slotTransform, tweener));
  }

  private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool bonus = false)
  {
    var tweenPair = singleSlotTweens.Find(pair => pair.Key == slotTransform);
    if (tweenPair.Value == null)
    {
      Debug.Log("Tween not found for the specified slotTransform.");
      yield break;
    }

    bool IsRegister = false;
    yield return tweenPair.Value.OnStepComplete(() => IsRegister = true);
    yield return new WaitUntil(() => IsRegister);

    tweenPair.Value.Pause();

    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 307f);
    int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
    Tweener stopTween = slotTransform.DOLocalMoveY(tweenpos - 290.5f, 0.1f);

    yield return stopTween.WaitForCompletion();
    tweenPair.Value.Kill();
  }

  private void KillAllTweens()
  {
    if (singleSlotTweens.Count > 0)
    {
      for (int i = 0; i < singleSlotTweens.Count; i++)
      {
        singleSlotTweens[i].Value.Kill();
      }
      singleSlotTweens.Clear();
    }
  }

  private List<List<int>> GenerateFreezedLocations()
  {
    List<List<int>> loc = new();
    for (int i = 0; i < Slot.Count; i++)
    {
      for (int j = 0; j < Slot[i].slotTransforms.Count; j++)
      {
        if (staticSymbol.freezedLocations[i].index[j] == 0 &&
            (SocketManager.resultData.matrix[i][j] == "11" || SocketManager.resultData.matrix[i][j] == "12" || SocketManager.resultData.matrix[i][j] == "14" || SocketManager.resultData.matrix[i][j] == "15" || SocketManager.resultData.matrix[i][j] == "16"))
        {
          List<int> rXc = new() { i, j };
          loc.Add(rXc);
        }
      }
    }
    return loc;
  }
}
