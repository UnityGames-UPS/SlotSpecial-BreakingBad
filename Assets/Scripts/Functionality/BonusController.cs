using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Linq;
using Unity.Burst.Intrinsics;

public class BonusController : MonoBehaviour
{
  [Header("Scripts References")]
  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private AudioController audioController;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private StaticSymbolController staticSymbol;
  [SerializeField] private ImageAnimation BonusWinningsImageAnimation;

  [Header("Sprites References")]
  [SerializeField] private Sprite[] index9Sprites;
  //[SerializeField] private Sprite[] losPollos;
  [SerializeField] private Sprite coinFrame;
  [SerializeField] private Sprite CC_Sprite;
  [SerializeField] private Sprite Diamond_Sprite;

  [Header("UI Objects References")]
  [SerializeField] private CanvasGroup NormalSlot_CG;
  [SerializeField] private CanvasGroup BonusSlot_CG;

  [SerializeField] private Button BonusSlotStart_Button;
  [SerializeField] private Button NormalSlotStart_Button;
  [SerializeField] private Button AutoSpin_Button;

  [SerializeField] private TMP_Text BonusSpinCounter_Text;
  [SerializeField] private TMP_Text BonusWinnings_Text;

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
    // if (BonusSlotStart_Button)
    // {
    //     BonusSlotStart_Button.onClick.RemoveAllListeners();
    //     BonusSlotStart_Button.onClick.AddListener(StartBonusSlot);
    // }

    ResetMatrix();
  }

  internal void StartBonus(int count)
  {
    if (FreeSpinsCounterUI_Panel.alpha != 0)
    {
      FreeSpinsCounterUI_Panel.DOFade(1, 0.3f);
    }
    if (lines.alpha != 0) lines.DOFade(0, 0.3f).OnComplete(() => { lines.interactable = false; lines.blocksRaycasts = false; });
    if (totalBet.alpha != 0) totalBet.DOFade(0, 0.3f).OnComplete(() => { totalBet.interactable = false; totalBet.blocksRaycasts = false; });
    if (lineBet.alpha != 0) lineBet.DOFade(0, 0.3f).OnComplete(() => { lineBet.interactable = false; lineBet.blocksRaycasts = false; });
    NormalSlotStart_Button.gameObject.SetActive(false);
    AutoSpin_Button.gameObject.SetActive(false);
    BonusSlotStart_Button.interactable = false;
    BonusSlotStart_Button.gameObject.SetActive(true);
    BonusSpinCounter_Text.text = count.ToString();
    WinningsUI_Panel.DOFade(1, 0.3f);

    NormalSlot_CG.DOFade(0, 0.5f);
    audioController.SwitchBGSound(true);
    BonusSlot_CG.DOFade(1, .5f).OnComplete(() =>
    {
      StartCoroutine(staticSymbol.ChangeLinksToGoldCoin(BonusSlotStart_Button));
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
    if (audioController) audioController.PlaySpinButtonAudio();

    if (BonusSlotStart_Button) BonusSlotStart_Button.interactable = false;

    if (!int.TryParse(BonusSpinCounter_Text.text, out int spinCount))
    {
      Debug.Log("Conversion error");
    }
    else
    {
      spinCount -= 1;
      BonusSpinCounter_Text.text = spinCount.ToString();
    }

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

    //yield return new WaitForSeconds(2f);
    SocketManager.AccumulateResult(slotManager.BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);

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
      // if (SocketManager.resultData.bonus.isWalterStash)
      // {
      //   Debug.Log("Triggering walter stash payout");
      //   Vector3 tempPosi = GrandPayoutTRTransform.localPosition;
      //   GrandPayoutTRTransform.gameObject.SetActive(true);
      //   yield return GrandPayoutTRTransform.DOLocalMove(BonusWinningsPosition.localPosition, 0.5f).OnComplete(() =>
      //   {
      //     int start = 0;
      //     int MajorJackpotWinning = (int)SocketManager.initialData.Jackpot[0];
      //     DOTween.To(() => start, (val) => start = val, MajorJackpotWinning, 0.3f).OnUpdate(() =>
      //     {
      //       BonusWinnings_Text.text = start.ToString() + "x";
      //     }).WaitForCompletion();
      //   })
      //   .WaitForCompletion();
      //   GrandPayoutTRTransform.gameObject.SetActive(false);
      //   GrandPayoutTRTransform.localPosition = tempPosi;
      // }

      yield return new WaitForSeconds(1f);

      int ccCount = 0;
      for (int i = 0; i < SocketManager.resultData.matrix.Count; i++)
      {
        for (int j = 0; j < SocketManager.resultData.matrix[i].Count; j++)
        {
          if (SocketManager.resultData.matrix[i][j] == "14")
          { //ask or check if this is true 
            ccCount++;
          }
        }
      }

      // v2 change  // uiManager.multiplierCount = !SocketManager.resultData.bonus.isWalterStash ? 0 : int.Parse(BonusWinnings_Text.text.Replace("x", ""));
      foreach (var coin in allcoinPositions)
      {
        if (coin.symbolId == 16)
        {
          yield return uiManager.ManageDiamondPayout(coin.prizeTypeIndex ?? 0, coin.coinValue.ToString(), Slot[coin.position[0]].slotTransforms[coin.position[1]].gameObject.transform);
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
      // for (int i = 0; i < Slot.Count; i++)
      // {
      //   for (int j = 0; j < Slot[i].slotTransforms.Count; j++)
      //   {
      //     if (Slot[i].slotTransforms[j].GetChild(3).GetComponent<Image>().sprite == coinFrame)
      //     {
      //       yield return uiManager.TrailRendererAnimation(Slot[i].slotTransforms[j].GetChild(3).GetChild(1).gameObject, 0, ccCount, true);
      //     }
      //   }
      // }

      IsSpinning = false;
      yield return new WaitForSeconds(2f);
      StartCoroutine(EndBonus());
      BonusWinnings_Text.text = "0";
      yield break;
    }

    if (int.TryParse(BonusSpinCounter_Text.text, out int spinCount))
    {
      if (spinCount != SocketManager.resultData.payload.linkRespinsRemaining)
      {
        BonusSpinCounter_Text.text = SocketManager.resultData.payload.linkRespinsRemaining.ToString();
      }
    }



    BonusSlotStart_Button.interactable = false;
    IsSpinning = false;
  }

  private void PopulateSymbols()
  {
    for (int j = 0; j < SocketManager.resultData.matrix.Count; j++)         //V2############
    {
      for (int i = 0; i < 5; i++)
      {
        if (SocketManager.resultData.matrix[j][i] == "9")
        {
          // Debug.Log("loc: " + i + j + " is 9");
          Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = index9Sprites[Random.Range(0, index9Sprites.Count())];
        }
        else if (SocketManager.resultData.matrix[j][i] == "15")
        {
          // Debug.Log("loc: " + i + j + " is 14");
          //run a loop to find the value of the coin and set the coin and its text
          foreach (var coins in SocketManager.resultData.payload.coinPositions)
          {
            if (coins.position[0] == j && coins.position[1] == i)
            {
              // Debug.Log("Setting coin frame");
              Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = coinFrame;
              Slot[j].slotTransforms[i].GetChild(3).GetChild(0).gameObject.SetActive(true);
              Slot[j].slotTransforms[i].GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = coins.coinValue.ToString() + "x";
              break;
            }
          }
        }
        else if (SocketManager.resultData.matrix[j][i] == "14")
        {
          // Debug.Log("loc: " + i + j + " is 13");
          Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = CC_Sprite;
        }
        else if (SocketManager.resultData.matrix[j][i] == "16")
        {
          // Debug.Log("loc: " + i + j + " is 13");
          Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = Diamond_Sprite;
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
    BonusSlotStart_Button.gameObject.SetActive(false);
    BonusSlotStart_Button.interactable = false;

    if (SocketManager.resultData.payload.winAmount > 0)
    {
      uiManager.BonusWinningsCoroutine = StartCoroutine(uiManager.MidGameImageAnimation(BonusWinningsImageAnimation, SocketManager.resultData.payload.winAmount));
      Debug.Log("Animation value:" + SocketManager.resultData.payload.winAmount);
      yield return new WaitUntil(() => uiManager.animationFinish);
      slotManager.WinningsTextAnimation();
    }
    WinningsUI_Panel.DOFade(0, 0.3f);

    // DOTween.To(() => BonusSlot_CG.alpha, (val) => BonusSlot_CG.alpha = val, 0, .5f);
    BonusSlot_CG.DOFade(0, 0.5f);
    audioController.SwitchBGSound(false);
    NormalSlot_CG.DOFade(1, 0.5f).OnComplete(() =>
 {

   slotManager.OnLinkFeatureCompleted();

   if (SocketManager.resultData.payload.linkRespinsRemaining <= 0)
   {
     slotManager.CloseFreeSpinsUI();
     if (slotManager.WasAutoSpinOn)
     {
       DOVirtual.DelayedCall(0.2f, () =>
      {
        NormalSlotStart_Button.interactable = false;
        NormalSlotStart_Button.gameObject.SetActive(true);
        slotManager.AutoSpin();
      });
     }
     else
     {
       NormalSlotStart_Button.interactable = false;
       NormalSlotStart_Button.gameObject.SetActive(true);
       slotManager.ToggleButtonGrp(true);
     }
   }
   else
   {
     DOVirtual.DelayedCall(0.5f, () =>
    {
      slotManager.OpenFreeSpinsUI();
      slotManager.FreeSpin(SocketManager.resultData.payload.linkRespinsRemaining);
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
        if (j == 3)
          TotalMiniSlotImages[i].slotImages[j].transform.GetChild(0).gameObject.SetActive(false);
      }
    }
  }

  private void InitializeSingleSlotTweening(Transform slotTransform, bool bonus = false)
  {
    Tweener tweener = null;

    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 307f);
    tweener = slotTransform.DOLocalMoveY(-670, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);

    tweener.Play();
    //singleSlotTweens.Add(slotTransform, tweener);
    singleSlotTweens.Add(new KeyValuePair<Transform, Tweener>(slotTransform, tweener));
  }

  private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool bonus = false)
  {
    // Find the corresponding KeyValuePair entry in singleSlotTweens for the given slotTransform
    var tweenPair = singleSlotTweens.Find(pair => pair.Key == slotTransform);

    // Check if the tween was found
    if (tweenPair.Value == null)
    {
      Debug.Log("Tween not found for the specified slotTransform.");
      yield break;
    }

    bool IsRegister = false;

    // Register to complete the current loop
    yield return tweenPair.Value.OnStepComplete(() => IsRegister = true);
    yield return new WaitUntil(() => IsRegister);

    // Pause the tween
    tweenPair.Value.Pause();

    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 307f);
    // Calculate the position and stop tweening at the required position
    int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
    Tweener stopTween = slotTransform.DOLocalMoveY(tweenpos - 290.5f, 0.1f);

    if (audioController) audioController.PlayWLAudio("spinStop");

    yield return stopTween.WaitForCompletion();

    // Kill the original tween after it has completed
    tweenPair.Value.Kill();
  }

  // private IEnumerator TriggerJackpot(){
  //     yield return null;

  //     for(int i=0; i<)
  // }

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
        Sprite sprite = Slot[i].slotTransforms[j].GetChild(3).GetComponent<Image>().sprite;
        if (
     staticSymbol.freezedLocations[i].index[j] == 0 &&
     (SocketManager.resultData.matrix[i][j] == "11" || SocketManager.resultData.matrix[i][j] == "12" || SocketManager.resultData.matrix[i][j] == "14" || SocketManager.resultData.matrix[i][j] == "15" || SocketManager.resultData.matrix[i][j] == "16")
 )
        {
          List<int> rXc = new() { i, j };
          loc.Add(rXc);
        }

      }
    }
    return loc;
  }
}

[System.Serializable]
public class SlotTransform
{
  public List<Transform> slotTransforms = new List<Transform>();
}
