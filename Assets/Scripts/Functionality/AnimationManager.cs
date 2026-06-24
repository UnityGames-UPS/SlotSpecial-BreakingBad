using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class SlotAnimation
{
    public List<ImageAnimation> slotAnimations = new List<ImageAnimation>();
}

public class AnimationManager : MonoBehaviour
{
    [Header("Model Reference")]
    

    [Header("Animation References")]
    [SerializeField] private List<SlotAnimation> inspectorAnimationGrid;

    private List<List<ImageAnimation>> animationGrid = new();
    private SlotManager slotManager;
    private List<Coroutine> activeLandingCoroutines = new();
    private int activeLandingAnimationsCount = 0;
    public bool AreLandingAnimationsPlaying => activeLandingAnimationsCount > 0;

    [Header("Dynamic Timing settings")]
    [SerializeField] private bool useDynamicFramerate = true;
    [SerializeField] private float winSymbolLoopDuration = 1.5f;

    public void Initialize(SlotManager manager)
    {
        slotManager = manager;
        CreateAnimationGrid();
    }

    private void Start()
    {
        if (inspectorAnimationGrid != null)
        {
            foreach (var col in inspectorAnimationGrid)
            {
                if (col != null && col.slotAnimations != null)
                {
                    foreach (var anim in col.slotAnimations)
                    {
                        if (anim != null)
                        {
                            anim.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void CreateAnimationGrid()
    {
        animationGrid.Clear();

        if (inspectorAnimationGrid != null && inspectorAnimationGrid.Count == 5)
        {
            // Map 5x3 (col x row) from inspector to 3x5 (row x col) in animationGrid
            for (int row = 0; row < 3; row++)
            {
                List<ImageAnimation> rowAnims = new();
                for (int col = 0; col < 5; col++)
                {
                    if (col < inspectorAnimationGrid.Count && row < inspectorAnimationGrid[col].slotAnimations.Count)
                    {
                        ImageAnimation imgAnim = inspectorAnimationGrid[col].slotAnimations[row];
                        if (imgAnim != null)
                        {
                            imgAnim.gameObject.SetActive(false);
                        }
                        rowAnims.Add(imgAnim);
                    }
                    else
                    {
                        Debug.LogError($"inspectorAnimationGrid is missing element at col {col}, row {row}");
                        rowAnims.Add(null);
                    }
                }
                animationGrid.Add(rowAnims);
            }
        }
        else
        {
            Debug.LogError("inspectorAnimationGrid is not configured! Expected 5 columns of slot animations.");
        }
    }

    public IEnumerator PlayWinningLineAnimations(List<LineWin> lineWins)
    {
        if (lineWins == null || lineWins.Count == 0) yield break;

        // Determine if it is free spin, autospin, or bonus/link feature flow
        bool isAutoOrFree = slotManager.IsFreeSpin || slotManager.IsAutoSpin || slotManager.IsBonus ||
            (slotManager.ResultData != null && slotManager.ResultData.payload != null && 
             (slotManager.ResultData.payload.isFreeSpinActive || 
              slotManager.ResultData.payload.isFreeSpinTriggered || 
              slotManager.ResultData.payload.freeSpinsRemaining > 0 ||
              slotManager.ResultData.payload.linkFeatureActive ||
              slotManager.ResultData.payload.isLinkTriggered));

        // Apply back tint on all main display cells initially
        slotManager.EnableAllBackTints(true, 0.85f);

        // 1. Play all winning lines together for 1 loop (skip if normal flow and only 1 win line)
        bool skipFirstPhase = !isAutoOrFree && lineWins.Count == 1;
        if (!skipFirstPhase)
        {
            List<ImageAnimation> activeAnims = new();

            foreach (var win in lineWins)
            {
                foreach (int col in win.positions)
                {
                    int row = win.pattern[col];
                    SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
                    if (symbolView == null) continue;

                    // Disable the corresponding main display symbol immediately
                    symbolView.DOKill();
                    if (symbolView.canvasGroup != null)
                    {
                        symbolView.canvasGroup.DOKill();
                        symbolView.canvasGroup.alpha = 0f;
                    }
                    else if (symbolView.mainImage != null)
                    {
                        symbolView.mainImage.DOKill();
                        symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 0f);
                    }
                    
                    // Hide its back tint since this cell is winning/highlighted
                    symbolView.SetBackTintActive(false);

                    // Enable matching animation object in the Animation Slot immediately
                    ImageAnimation animCell = animationGrid[row][col];
                    animCell.transform.position = symbolView.transform.position;
                    
                    animCell.DOKill();
                    CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
                    if (animCG != null)
                    {
                        animCG.DOKill();
                        animCG.alpha = 1f;
                    }
                    else if (animCell.rendererDelegate != null)
                    {
                        animCell.rendererDelegate.DOKill();
                        animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 1f);
                    }
                    animCell.gameObject.SetActive(true);

                    // Get the symbol ID
                    int symbolId = int.Parse(win.symbolId);
                    if (slotManager.ResultData != null && slotManager.ResultData.matrix != null &&
                        row < slotManager.ResultData.matrix.Count && col < slotManager.ResultData.matrix[row].Count)
                    {
                        symbolId = int.Parse(slotManager.ResultData.matrix[row][col]);
                    }

                    // Populate sprites for the animCell
                    slotManager.ConfigureAnimationSprites(animCell, symbolId);

                    // Configure dynamic timing properties
                    animCell.useDynamicFramerate = true;
                    animCell.dynamicLoopDuration = winSymbolLoopDuration;

                    // Sync and animate overlay text
                    AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
                    if (textHelper == null)
                    {
                        textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
                    }
                    textHelper.SetupFromHierarchy();
                    if (symbolId == 17 && symbolView.losPolosValueText != null && symbolView.losPolosValueText.gameObject.activeSelf)
                    {
                        textHelper.PlayTextAnimation(17, symbolView.losPolosValueText.text, winSymbolLoopDuration, true);
                    }
                    else if (symbolId == 15 && symbolView.goldCoinValueText != null && symbolView.goldCoinValueText.gameObject.activeSelf)
                    {
                        textHelper.PlayTextAnimation(15, symbolView.goldCoinValueText.text, winSymbolLoopDuration, true);
                    }
                    else if (symbolId == 13 && symbolView.multiplierValueText != null && symbolView.multiplierValueText.gameObject.activeSelf)
                    {
                        textHelper.PlayTextAnimation(13, symbolView.multiplierValueText.text, winSymbolLoopDuration, true);
                    }
                    else
                    {
                        textHelper.Clear();
                    }

                    // Play the animation
                    animCell.doLoopAnimation = true;
                    animCell.onLoopComplete = null;
                    animCell.StopAnimation();
                    animCell.StartAnimation();

                    if (!activeAnims.Contains(animCell))
                    {
                        activeAnims.Add(animCell);
                    }
                }
            }

            // Wait for one loop to complete
            int completedCount = 0;
            bool allDone = false;

            foreach (var anim in activeAnims)
            {
                anim.onLoopComplete = (_) =>
                {
                    completedCount++;
                    if (completedCount >= activeAnims.Count)
                        allDone = true;
                };
            }

            float timeout = 5f;
            float elapsed = 0f;
            while (!allDone && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Clean up loop complete events
            foreach (var anim in activeAnims)
            {
                anim.onLoopComplete = null;
            }

            // If it is free spin or autospin, stop here
            if (isAutoOrFree)
            {
                StopAllAnimations();
                yield break;
            }
        }

        // 2. Play individual winning lines one by one (for normal flow)
        int currentLineIndex = 0;
        while (true)
        {
            LineWin win = lineWins[currentLineIndex];
            string payoutString = win.payout.ToString("F3");

            // Reset all board symbols to dimmed state initially
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    SlotSymbolView symbolView = slotManager.GetSymbolView(r, c);
                    if (symbolView != null)
                    {
                        symbolView.DOKill();
                        if (symbolView.canvasGroup != null)
                        {
                            symbolView.canvasGroup.DOKill();
                            symbolView.canvasGroup.alpha = 1f;
                        }
                        else if (symbolView.mainImage != null)
                        {
                            symbolView.mainImage.DOKill();
                            symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 1f);
                        }
                        symbolView.SetBackTintActive(true);
                    }

                    ImageAnimation anim = animationGrid[r][c];
                    anim.DOKill();
                    CanvasGroup animCG = anim.GetComponent<CanvasGroup>();
                    if (animCG != null)
                    {
                        animCG.DOKill();
                        animCG.alpha = 0f;
                    }
                    else if (anim.rendererDelegate != null)
                    {
                        anim.rendererDelegate.DOKill();
                        anim.rendererDelegate.color = new Color(anim.rendererDelegate.color.r, anim.rendererDelegate.color.g, anim.rendererDelegate.color.b, 0f);
                    }

                    AnimationTextHelper textHelper = anim.GetComponent<AnimationTextHelper>();
                    if (textHelper != null)
                    {
                        textHelper.Clear();
                    }

                    if (anim.gameObject.activeSelf)
                    {
                        anim.onLoopComplete = null;
                        anim.StopAnimation();
                        anim.gameObject.SetActive(false);
                    }
                }
            }

            // Play animations only for the current line's symbols
            List<ImageAnimation> lineAnims = new();
            for (int pIdx = 0; pIdx < win.positions.Count; pIdx++)
            {
                int col = win.positions[pIdx];
                int row = win.pattern[col];
                SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
                if (symbolView == null) continue;

                // Disable the main symbol view immediately
                symbolView.DOKill();
                if (symbolView.canvasGroup != null)
                {
                    symbolView.canvasGroup.DOKill();
                    symbolView.canvasGroup.alpha = 0f;
                }
                else if (symbolView.mainImage != null)
                {
                    symbolView.mainImage.DOKill();
                    symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 0f);
                }

                // Hide back tint for this cell
                symbolView.SetBackTintActive(false);

                // Enable and position the animation cell immediately
                ImageAnimation animCell = animationGrid[row][col];
                animCell.transform.position = symbolView.transform.position;

                animCell.DOKill();
                CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
                if (animCG != null)
                {
                    animCG.DOKill();
                    animCG.alpha = 1f;
                }
                else if (animCell.rendererDelegate != null)
                {
                    animCell.rendererDelegate.DOKill();
                    animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 1f);
                }
                animCell.gameObject.SetActive(true);

                // Get symbol ID
                int symbolId = int.Parse(win.symbolId);
                if (slotManager.ResultData != null && slotManager.ResultData.matrix != null &&
                    row < slotManager.ResultData.matrix.Count && col < slotManager.ResultData.matrix[row].Count)
                {
                    symbolId = int.Parse(slotManager.ResultData.matrix[row][col]);
                }

                slotManager.ConfigureAnimationSprites(animCell, symbolId);
                animCell.useDynamicFramerate = true;
                animCell.dynamicLoopDuration = winSymbolLoopDuration;

                // Text animation setup
                AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
                if (textHelper == null)
                {
                    textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
                }
                textHelper.SetupFromHierarchy();

                if (symbolId == 17 && symbolView.losPolosValueText != null && symbolView.losPolosValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(17, symbolView.losPolosValueText.text, winSymbolLoopDuration, true);
                }
                else if (symbolId == 15 && symbolView.goldCoinValueText != null && symbolView.goldCoinValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(15, symbolView.goldCoinValueText.text, winSymbolLoopDuration, true);
                }
                else if (symbolId == 13 && symbolView.multiplierValueText != null && symbolView.multiplierValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(13, symbolView.multiplierValueText.text, winSymbolLoopDuration, true);
                }
                else
                {
                    textHelper.Clear();
                }

                // Show payout text ONLY on the last icon of that win line with a pop-up animation
                if (pIdx == win.positions.Count - 1)
                {
                    if (textHelper.payoutText != null)
                    {
                        textHelper.PlayPayoutTextAnimation(payoutString, winSymbolLoopDuration);
                    }
                }

                animCell.doLoopAnimation = true;
                animCell.onLoopComplete = null;
                animCell.StopAnimation();
                animCell.StartAnimation();

                if (!lineAnims.Contains(animCell))
                {
                    lineAnims.Add(animCell);
                }
            }

            // Wait for one loop of this win line to complete
            int lineCompletedCount = 0;
            bool lineAllDone = false;

            foreach (var anim in lineAnims)
            {
                anim.onLoopComplete = (_) =>
                {
                    lineCompletedCount++;
                    if (lineCompletedCount >= lineAnims.Count)
                        lineAllDone = true;
                };
            }

            float lineTimeout = 5f;
            float lineElapsed = 0f;
            while (!lineAllDone && lineElapsed < lineTimeout)
            {
                lineElapsed += Time.deltaTime;
                yield return null;
            }

            // Clean up loop complete events for this line
            foreach (var anim in lineAnims)
            {
                anim.onLoopComplete = null;
            }

            // Move to next win line
            currentLineIndex = (currentLineIndex + 1) % lineWins.Count;
        }
    }

    public void StopAllAnimations()
    {
        foreach (var c in activeLandingCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        activeLandingCoroutines.Clear();
        activeLandingAnimationsCount = 0;

        // Stop and disable all overlay animation cells
        for (int row = 0; row < animationGrid.Count; row++)
        {
            for (int col = 0; col < animationGrid[row].Count; col++)
            {
                ImageAnimation anim = animationGrid[row][col];
                
                anim.DOKill();
                CanvasGroup animCG = anim.GetComponent<CanvasGroup>();
                if (animCG != null)
                {
                    animCG.DOKill();
                    animCG.alpha = 0f;
                }
                else if (anim.rendererDelegate != null)
                {
                    anim.rendererDelegate.DOKill();
                    anim.rendererDelegate.color = new Color(anim.rendererDelegate.color.r, anim.rendererDelegate.color.g, anim.rendererDelegate.color.b, 0f);
                }

                AnimationTextHelper textHelper = anim.GetComponent<AnimationTextHelper>();
                if (textHelper != null)
                {
                    textHelper.Clear();
                }

                if (anim.gameObject.activeSelf)
                {
                    anim.onLoopComplete = null;
                    anim.StopAnimation();
                    anim.gameObject.SetActive(false);
                }

                // Restore main display symbols
                SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
                if (symbolView != null)
                {
                    symbolView.DOKill();
                    if (symbolView.canvasGroup != null)
                    {
                        symbolView.canvasGroup.DOKill();
                        symbolView.canvasGroup.alpha = 1f;
                    }
                    else if (symbolView.mainImage != null)
                    {
                        symbolView.mainImage.DOKill();
                        symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 1f);
                    }
                    symbolView.SetBackTintActive(false);
                }
            }
        }
    }

    public IEnumerator PlaySpecialSymbolAnimations(System.Func<string, bool> isSpecialCondition, List<List<string>> matrix)
    {
        List<ImageAnimation> activeAnims = new();
        List<SlotSymbolView> fadedViews = new();

        slotManager.EnableAllBackTints(true, 0.85f);

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                string symbolStr = matrix[row][col];
                if (!isSpecialCondition(symbolStr)) continue;

                SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
                if (symbolView == null) continue;

                // 1. Fade out the corresponding main display symbol
                symbolView.DOKill();
                if (symbolView.canvasGroup != null)
                {
                    symbolView.canvasGroup.DOKill();
                    symbolView.canvasGroup.DOFade(0f, 0.1f);
                }
                else if (symbolView.mainImage != null)
                {
                    symbolView.mainImage.DOKill();
                    symbolView.mainImage.DOFade(0f, 0.1f);
                }
                symbolView.SetBackTintActive(false);
                fadedViews.Add(symbolView);

                // 2. Enable matching animation object on the Animation Slot
                ImageAnimation animCell = animationGrid[row][col];
                animCell.transform.position = symbolView.transform.position;
                
                animCell.DOKill();
                CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
                if (animCG != null)
                {
                    animCG.DOKill();
                    animCG.alpha = 0f;
                    animCell.gameObject.SetActive(true);
                    animCG.DOFade(1f, 0.1f);
                }
                else if (animCell.rendererDelegate != null)
                {
                    animCell.rendererDelegate.DOKill();
                    animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 0f);
                    animCell.gameObject.SetActive(true);
                    animCell.rendererDelegate.DOFade(1f, 0.1f);
                }
                else
                {
                    animCell.gameObject.SetActive(true);
                }

                // Configure and run
                int symbolId = int.Parse(symbolStr);
                
                // Get coin text if applicable
                int lpVal = 0;
                string coinTxt = null;
                var coinPos = slotManager.GetCoinPosition(row, col);
                if (coinPos != null)
                {
                    lpVal = coinPos.coinValue;
                    if (symbolId == 15) coinTxt = (coinPos.coinValue * slotManager.TotalBet).ToString() + "x";
                    else if (symbolId == 13) coinTxt = "X" + coinPos.coinValue.ToString();
                    else if (symbolId == 17) lpVal = coinPos.coinValue;
                }

                slotManager.ConfigureAnimationSprites(animCell, symbolId, lpVal, coinTxt);

                // Configure dynamic timing properties
                animCell.useDynamicFramerate = true; // Force true for synchronization
                animCell.dynamicLoopDuration = winSymbolLoopDuration;

                // Sync and animate overlay text
                AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
                if (textHelper == null)
                {
                    textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
                }
                textHelper.SetupFromHierarchy();
                if (symbolId == 17 && symbolView.losPolosValueText != null && symbolView.losPolosValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(17, symbolView.losPolosValueText.text, winSymbolLoopDuration, false);
                }
                else if (symbolId == 15 && symbolView.goldCoinValueText != null && symbolView.goldCoinValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(15, symbolView.goldCoinValueText.text, winSymbolLoopDuration, false);
                }
                else if (symbolId == 13 && symbolView.multiplierValueText != null && symbolView.multiplierValueText.gameObject.activeSelf)
                {
                    textHelper.PlayTextAnimation(13, symbolView.multiplierValueText.text, winSymbolLoopDuration, false);
                }
                else
                {
                    textHelper.Clear();
                }

                animCell.doLoopAnimation = false;
                animCell.onLoopComplete = null;
                animCell.StopAnimation();
                animCell.StartAnimation();

                activeAnims.Add(animCell);
            }
        }

        if (activeAnims.Count == 0)
        {
            slotManager.EnableAllBackTints(false);
            yield break;
        }

        // Wait for loop complete
        int completedCount = 0;
        bool allDone = false;

        foreach (var anim in activeAnims)
        {
            anim.onLoopComplete = (_) =>
            {
                completedCount++;
                if (completedCount >= activeAnims.Count)
                    allDone = true;
            };
        }

        float timeout = 5f;
        float elapsed = 0f;
        while (!allDone && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up: stop animations, restore faded views, disable tint
        Sequence cleanupSeq = DOTween.Sequence();

        foreach (var anim in activeAnims)
        {
            anim.onLoopComplete = null;
            anim.StopAnimation();

            AnimationTextHelper textHelper = anim.GetComponent<AnimationTextHelper>();
            if (textHelper != null)
            {
                textHelper.Clear();
            }
            
            CanvasGroup animCG = anim.GetComponent<CanvasGroup>();
            if (animCG != null)
            {
                cleanupSeq.Join(animCG.DOFade(0f, 0.1f).OnComplete(() => anim.gameObject.SetActive(false)));
            }
            else if (anim.rendererDelegate != null)
            {
                cleanupSeq.Join(anim.rendererDelegate.DOFade(0f, 0.1f).OnComplete(() => anim.gameObject.SetActive(false)));
            }
            else
            {
                anim.gameObject.SetActive(false);
            }
        }

        foreach (var view in fadedViews)
        {
            if (view.canvasGroup != null)
            {
                cleanupSeq.Join(view.canvasGroup.DOFade(1f, 0.1f));
            }
            else if (view.mainImage != null)
            {
                cleanupSeq.Join(view.mainImage.DOFade(1f, 0.1f));
            }
        }

        yield return cleanupSeq.WaitForCompletion();

        slotManager.EnableAllBackTints(false);
    }

    public void PlaySpecialAnimationForCell(int row, int col)
    {
        Coroutine c = StartCoroutine(PlaySpecialAnimationForCellRoutine(row, col));
        activeLandingCoroutines.Add(c);
    }

    private IEnumerator PlaySpecialAnimationForCellRoutine(int row, int col)
    {
        if (slotManager.ResultData == null || slotManager.ResultData.matrix == null) yield break;
        if (row < 0 || row >= 3 || col < 0 || col >= 5) yield break;

        string symbolStr = slotManager.ResultData.matrix[row][col];
        int symbolId = int.Parse(symbolStr);

        SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
        if (symbolView == null) yield break;

        activeLandingAnimationsCount++;
        try
        {
            // 1. Fade out the corresponding main display symbol
            symbolView.DOKill();
            if (symbolView.canvasGroup != null)
            {
                symbolView.canvasGroup.DOKill();
                symbolView.canvasGroup.DOFade(0f, 0.1f);
            }
            else if (symbolView.mainImage != null)
            {
                symbolView.mainImage.DOKill();
                symbolView.mainImage.DOFade(0f, 0.1f);
            }

            // 2. Enable matching animation object on the Animation Slot
            ImageAnimation animCell = animationGrid[row][col];
            animCell.transform.position = symbolView.transform.position;
            
            animCell.DOKill();
            CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
            if (animCG != null)
            {
                animCG.DOKill();
                animCG.alpha = 0f;
                animCell.gameObject.SetActive(true);
                animCG.DOFade(1f, 0.1f);
            }
            else if (animCell.rendererDelegate != null)
            {
                animCell.rendererDelegate.DOKill();
                animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 0f);
                animCell.gameObject.SetActive(true);
                animCell.rendererDelegate.DOFade(1f, 0.1f);
            }
            else
            {
                animCell.gameObject.SetActive(true);
            }

            // Configure and run
            int lpVal = 0;
            string coinTxt = null;
            var coinPos = slotManager.GetCoinPosition(row, col);
            if (coinPos != null)
            {
                lpVal = coinPos.coinValue;
                if (symbolId == 15) coinTxt = (coinPos.coinValue * slotManager.TotalBet).ToString() + "x";
                else if (symbolId == 13) coinTxt = "X" + coinPos.coinValue.ToString();
                else if (symbolId == 17) lpVal = coinPos.coinValue;
            }

            slotManager.ConfigureAnimationSprites(animCell, symbolId, lpVal, coinTxt);

            animCell.useDynamicFramerate = true;
            animCell.dynamicLoopDuration = winSymbolLoopDuration / 2f;

            // Sync and animate overlay text
            AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
            if (textHelper == null)
            {
                textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
            }
            textHelper.SetupFromHierarchy();
            if (symbolId == 17 && symbolView.losPolosValueText != null && symbolView.losPolosValueText.gameObject.activeSelf)
            {
                textHelper.PlayTextAnimation(17, symbolView.losPolosValueText.text, winSymbolLoopDuration / 2f, false);
            }
            else if (symbolId == 15 && symbolView.goldCoinValueText != null && symbolView.goldCoinValueText.gameObject.activeSelf)
            {
                textHelper.PlayTextAnimation(15, symbolView.goldCoinValueText.text, winSymbolLoopDuration / 2f, false);
            }
            else if (symbolId == 13 && symbolView.multiplierValueText != null && symbolView.multiplierValueText.gameObject.activeSelf)
            {
                textHelper.PlayTextAnimation(13, symbolView.multiplierValueText.text, winSymbolLoopDuration / 2f, false);
            }
            else
            {
                textHelper.Clear();
            }

            animCell.doLoopAnimation = false;
            animCell.onLoopComplete = null;
            animCell.StopAnimation();
            animCell.StartAnimation();

            // Wait for loop complete
            bool done = false;
            animCell.onLoopComplete = (_) => { done = true; };

            float timeout = (winSymbolLoopDuration / 2f) + 0.25f;
            float elapsed = 0f;
            while (!done && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Clean up
            animCell.onLoopComplete = null;
            animCell.StopAnimation();
            if (textHelper != null)
            {
                textHelper.Clear();
            }

            Sequence cleanupSeq = DOTween.Sequence();
            if (animCG != null)
            {
                cleanupSeq.Join(animCG.DOFade(0f, 0.1f).OnComplete(() => animCell.gameObject.SetActive(false)));
            }
            else if (animCell.rendererDelegate != null)
            {
                cleanupSeq.Join(animCell.rendererDelegate.DOFade(0f, 0.1f).OnComplete(() => animCell.gameObject.SetActive(false)));
            }
            else
            {
                animCell.gameObject.SetActive(false);
            }

            if (symbolView.canvasGroup != null)
            {
                cleanupSeq.Join(symbolView.canvasGroup.DOFade(1f, 0.1f));
            }
            else if (symbolView.mainImage != null)
            {
                cleanupSeq.Join(symbolView.mainImage.DOFade(1f, 0.1f));
            }

            yield return cleanupSeq.WaitForCompletion();
        }
        finally
        {
            activeLandingAnimationsCount = Mathf.Max(0, activeLandingAnimationsCount - 1);
        }
    }

    public void StartSymbolAnimationLoop(int row, int col)
    {
        if (slotManager == null || slotManager.ResultData == null || slotManager.ResultData.matrix == null) return;
        if (row < 0 || row >= 3 || col < 0 || col >= 5) return;

        string symbolStr = slotManager.ResultData.matrix[row][col];
        int symbolId = int.Parse(symbolStr);

        SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
        if (symbolView == null) return;

        // 1. Fade out the corresponding main display symbol
        symbolView.DOKill();
        if (symbolView.canvasGroup != null)
        {
            symbolView.canvasGroup.DOKill();
            symbolView.canvasGroup.alpha = 0f;
        }
        else if (symbolView.mainImage != null)
        {
            symbolView.mainImage.DOKill();
            symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 0f);
        }
        symbolView.SetBackTintActive(false);

        // 2. Enable matching animation object on the Animation Slot
        ImageAnimation animCell = animationGrid[row][col];
        animCell.transform.position = symbolView.transform.position;
        
        animCell.DOKill();
        CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
        if (animCG != null)
        {
            animCG.DOKill();
            animCG.alpha = 1f;
        }
        else if (animCell.rendererDelegate != null)
        {
            animCell.rendererDelegate.DOKill();
            animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 1f);
        }
        animCell.gameObject.SetActive(true);

        // Configure and run
        int lpVal = 0;
        string coinTxt = null;
        var coinPos = slotManager.GetCoinPosition(row, col);
        if (coinPos != null)
        {
            lpVal = coinPos.coinValue;
            if (symbolId == 15) coinTxt = (coinPos.coinValue * slotManager.TotalBet).ToString() + "x";
            else if (symbolId == 13) coinTxt = "X" + coinPos.coinValue.ToString();
            else if (symbolId == 17) lpVal = coinPos.coinValue;
        }

        slotManager.ConfigureAnimationSprites(animCell, symbolId, lpVal, coinTxt);

        animCell.useDynamicFramerate = true;
        animCell.dynamicLoopDuration = winSymbolLoopDuration;

        // Sync and animate overlay text
        AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
        if (textHelper == null)
        {
            textHelper = animCell.gameObject.AddComponent<AnimationTextHelper>();
        }
        textHelper.SetupFromHierarchy();
        if (symbolId == 17 && symbolView.losPolosValueText != null && symbolView.losPolosValueText.gameObject.activeSelf)
        {
            textHelper.PlayTextAnimation(17, symbolView.losPolosValueText.text, winSymbolLoopDuration, false);
        }
        else if (symbolId == 15 && symbolView.goldCoinValueText != null && symbolView.goldCoinValueText.gameObject.activeSelf)
        {
            textHelper.PlayTextAnimation(15, symbolView.goldCoinValueText.text, winSymbolLoopDuration, false);
        }
        else if (symbolId == 13 && symbolView.multiplierValueText != null && symbolView.multiplierValueText.gameObject.activeSelf)
        {
            textHelper.PlayTextAnimation(13, symbolView.multiplierValueText.text, winSymbolLoopDuration, false);
        }
        else
        {
            textHelper.Clear();
        }

        animCell.doLoopAnimation = true;
        animCell.onLoopComplete = null;
        animCell.StopAnimation();
        animCell.StartAnimation();
    }

    public void StopSymbolAnimationLoop(int row, int col)
    {
        if (row < 0 || row >= 3 || col < 0 || col >= 5) return;
        ImageAnimation animCell = animationGrid[row][col];
        animCell.DOKill();
        animCell.onLoopComplete = null;
        animCell.StopAnimation();

        AnimationTextHelper textHelper = animCell.GetComponent<AnimationTextHelper>();
        if (textHelper != null)
        {
            textHelper.Clear();
        }

        CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
        if (animCG != null)
        {
            animCG.DOFade(0f, 0.1f).OnComplete(() => animCell.gameObject.SetActive(false));
        }
        else if (animCell.rendererDelegate != null)
        {
            animCell.rendererDelegate.DOFade(0f, 0.1f).OnComplete(() => animCell.gameObject.SetActive(false));
        }
        else
        {
            animCell.gameObject.SetActive(false);
        }

        SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
        if (symbolView != null)
        {
            symbolView.DOKill();
            if (symbolView.canvasGroup != null)
            {
                symbolView.canvasGroup.DOKill();
                symbolView.canvasGroup.alpha = 1f;
            }
            else if (symbolView.mainImage != null)
            {
                symbolView.mainImage.DOKill();
                symbolView.mainImage.color = new Color(symbolView.mainImage.color.r, symbolView.mainImage.color.g, symbolView.mainImage.color.b, 1f);
            }
        }
    }
}
