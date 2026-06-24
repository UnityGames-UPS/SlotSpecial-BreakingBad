using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the overlay "sticky" slot images used during the Link/Bonus feature.
/// Subscribes to GameModel for data access, separating concerns.
/// </summary>
public class StickySymbolManager : MonoBehaviour
{
    [Header("Script References")]
    
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private BonusManager bonusManager;

    [Header("Slots Reference")]
    [SerializeField] public List<SlotImage> Slot;

    [Header("Animation Sprites References")]
    [SerializeField] private Sprite[] LinkToGoldCoin_Animation;
    [SerializeField] private Sprite[] MegaLinkToGoldCoin_Animation;

    [SerializeField] internal List<Column> freezedLocations = new();
    [SerializeField] internal List<List<int>> Locations = new();

    private List<List<SlotSymbolView>> symbolViews = new();

    private void Awake()
    {
        InitializeSymbolViews();
    }

    private void Start()
    {
        Reset();
    }

    private void InitializeSymbolViews()
    {
        symbolViews.Clear();
        for (int i = 0; i < Slot.Count; i++)
        {
            List<SlotSymbolView> rowViews = new();
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                Image image = Slot[i].slotImages[j];
                SlotSymbolView view = image.GetComponent<SlotSymbolView>();
                if (view == null)
                {
                    view = image.gameObject.AddComponent<SlotSymbolView>();
                }
                view.SetupFromHierarchy();
                rowViews.Add(view);
            }
            symbolViews.Add(rowViews);
        }
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
            List<int> row = new List<int>(new int[Slot[i].slotImages.Count]);
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

    internal void TurnOnIndices(List<List<int>> loc)
    {
        List<List<int>> freezeMatrix = GenerateFreezeMatrix(loc);

        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                SlotSymbolView view = symbolViews[i][j];
                if (freezeMatrix[i][j] == 1)
                {
                    string matrixVal = socketManager.resultData.matrix[j][i];
                    int symbolId = int.Parse(matrixVal);

                    if (view != null) view.ClearValues();

                    if (matrixVal == "11") // Link -> coin transition
                    {
                        SetupAnimationOnSlot(i, j, LinkToGoldCoin_Animation);
                        AssignCoinText(i, j);
                    }
                    else if (matrixVal == "12") // MegaLink -> coin transition
                    {
                        SetupAnimationOnSlot(i, j, MegaLinkToGoldCoin_Animation);
                        AssignCoinText(i, j);
                    }

                    Slot[i].slotImages[j].sprite = slotManager.GetResultMatrixImage(j, i).sprite;
                    Slot[i].slotImages[j].gameObject.SetActive(true);

                    if (view != null)
                    {
                        slotManager.ConfigureSymbolView(view, symbolId);
                    }
                }
                else
                {
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
    }

    internal IEnumerator ChangeLinksToGoldCoin()
    {
        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                ImageAnimation anim = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
                if (anim == null || !anim.isAnim) continue;

                anim.AnimationSpeed = 17;
                anim.onLoopComplete = null;
                anim.StartAnimation();

                yield return new WaitUntil(() =>
                    anim.rendererDelegate != null &&
                    anim.textureArray.Count > 7 &&
                    anim.rendererDelegate.sprite == anim.textureArray[7]);

                anim.transform.GetChild(0).gameObject.SetActive(true);

                yield return new WaitUntil(() =>
                    anim.rendererDelegate != null &&
                    anim.textureArray.Count > 0 &&
                    anim.rendererDelegate.sprite == anim.textureArray[^1]);

                anim.StopAnimation();
                var slotView = slotManager.GetSymbolView(j, i);
                if (slotView != null)
                {
                    anim.rendererDelegate.sprite = slotView.mainImage.sprite;
                }
            }
        }

        yield return new WaitForSeconds(1f);
        bonusManager.OnInitialTransitionComplete();
    }

    internal void Reset()
    {
        freezedLocations.Clear();
        freezedLocations.TrimExcess();
        Locations.Clear();
        Locations.TrimExcess();

        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                SlotSymbolView view = symbolViews[i][j];
                view.ClearValues();

                Slot[i].slotImages[j].sprite = null;
                Slot[i].slotImages[j].gameObject.SetActive(false);

                var anim = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
                if (anim != null)
                {
                    anim.StopAnimation();
                    anim.isAnim = false;
                    anim.onLoopComplete = null;
                }
            }
        }
    }

    private void SetupAnimationOnSlot(int col, int row, Sprite[] sprites)
    {
        ImageAnimation anim = Slot[col].slotImages[row].GetComponent<ImageAnimation>();
        if (anim == null) return;

        anim.StopAnimation();
        anim.onLoopComplete = null;

        anim.isAnim = true;
        anim.textureArray.Clear();
        anim.textureArray.TrimExcess();

        foreach (Sprite s in sprites)
            anim.textureArray.Add(s);
    }

    private void AssignCoinText(int col, int row)
    {
        foreach (var coin in socketManager.resultData.payload.coinPositions)
        {
            if (coin.position[0] == row && coin.position[1] == col)
            {
                // Use the SlotSymbolView's gold coin text directly
                SlotSymbolView view = symbolViews[col][row];
                if (view != null)
                {
                    if (coin.symbolId == 13)
                    {
                        view.SetMultiplierCoinValue(coin.coinValue, slotManager.TotalBet);
                    }
                    else
                    {
                        view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
                    }
                }
                break;
            }
        }
    }
}
