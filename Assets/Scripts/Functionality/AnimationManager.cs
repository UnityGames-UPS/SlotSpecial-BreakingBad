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

        // Apply back tint on all main display cells initially
        slotManager.EnableAllBackTints(true, 0.85f);

        // Track active animations
        List<ImageAnimation> activeAnims = new();

        foreach (var win in lineWins)
        {
            foreach (int col in win.positions)
            {
                int row = win.pattern[col];
                SlotSymbolView symbolView = slotManager.GetSymbolView(row, col);
                if (symbolView == null) continue;

                // 1. Fade out the corresponding main display symbol
                symbolView.DOKill();
                if (symbolView.canvasGroup != null)
                {
                    symbolView.canvasGroup.DOKill();
                    symbolView.canvasGroup.DOFade(0f, 0.2f);
                }
                else if (symbolView.mainImage != null)
                {
                    symbolView.mainImage.DOKill();
                    symbolView.mainImage.DOFade(0f, 0.2f);
                }
                
                // Hide its back tint since this cell is winning/highlighted
                symbolView.SetBackTintActive(false);

                // 2. Enable matching animation object in the Animation Slot
                ImageAnimation animCell = animationGrid[row][col];
                animCell.transform.position = symbolView.transform.position;
                
                animCell.DOKill();
                CanvasGroup animCG = animCell.GetComponent<CanvasGroup>();
                if (animCG != null)
                {
                    animCG.DOKill();
                    animCG.alpha = 0f;
                    animCell.gameObject.SetActive(true);
                    animCG.DOFade(1f, 0.2f);
                }
                else if (animCell.rendererDelegate != null)
                {
                    animCell.rendererDelegate.DOKill();
                    animCell.rendererDelegate.color = new Color(animCell.rendererDelegate.color.r, animCell.rendererDelegate.color.g, animCell.rendererDelegate.color.b, 0f);
                    animCell.gameObject.SetActive(true);
                    animCell.rendererDelegate.DOFade(1f, 0.2f);
                }
                else
                {
                    animCell.gameObject.SetActive(true);
                }

                // Get the symbol ID
                int symbolId = int.Parse(win.symbolId);

                // Populate sprites for the animCell
                slotManager.ConfigureAnimationSprites(animCell, symbolId);

                // 3. Play the animation
                animCell.doLoopAnimation = true;
                animCell.onLoopComplete = null;
                animCell.StopAnimation();
                animCell.StartAnimation();

                activeAnims.Add(animCell);
            }
        }

        // Wait for user interaction or spin duration
        // (Typically, slot line animations cycle indefinitely until the next spin)
        yield return null;
    }

    public void StopAllAnimations()
    {
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
                    else if (symbolId == 17) lpVal = coinPos.coinValue;
                }

                slotManager.ConfigureAnimationSprites(animCell, symbolId, lpVal, coinTxt);

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
}
