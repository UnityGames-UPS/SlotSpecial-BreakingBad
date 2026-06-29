using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StickySymbolManager : MonoBehaviour
{
    [Header("Script References")]
    
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private BonusManager bonusManager;

    [Header("Slots Reference")]
    [SerializeField] internal List<SlotImage> Slot;

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
            bool exists = false;
            foreach (var existing in Locations)
            {
                if (existing != null && existing.Count == 2 && loc[i] != null && loc[i].Count == 2)
                {
                    if (existing[0] == loc[i][0] && existing[1] == loc[i][1])
                    {
                        exists = true;
                        break;
                    }
                }
            }
            if (!exists)
                Locations.Add(loc[i]);
        }

        
        List<List<int>> freezeMatrix = new List<List<int>>();
        for (int i = 0; i < Slot.Count; i++)
        {
            List<int> row = new List<int>(new int[Slot[i].slotImages.Count]);
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

                    if (matrixVal == "11" || matrixVal == "12") 
                    {
                        Slot[i].slotImages[j].gameObject.SetActive(false);
                    }
                    else
                    {
                        if (slotManager != null && slotManager.SlotSymbols != null && symbolId >= 0 && symbolId < slotManager.SlotSymbols.Length)
                        {
                            Slot[i].slotImages[j].sprite = slotManager.SlotSymbols[symbolId];
                        }
                        else
                        {
                            Slot[i].slotImages[j].sprite = slotManager.GetResultMatrixImage(j, i).sprite;
                        }
                        Slot[i].slotImages[j].gameObject.SetActive(true);

                        if (view != null)
                        {
                            slotManager.ConfigureSymbolView(view, symbolId);

                            if (socketManager.resultData != null && socketManager.resultData.payload != null &&
                                socketManager.resultData.payload.coinPositions != null)
                            {
                                if (symbolId == 17)
                                {
                                    bool found = false;
                                    foreach (var coin in socketManager.resultData.payload.coinPositions)
                                    {
                                        if (coin.symbolId == 17 && coin.position[0] == j && coin.position[1] == i)
                                        {
                                            view.SetLosPolosValue((int)coin.coinValue);
                                            found = true;
                                            break;
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
                                    foreach (var coin in socketManager.resultData.payload.coinPositions)
                                    {
                                        if (coin.symbolId == 13 && coin.position[0] == j && coin.position[1] == i)
                                        {
                                            view.SetMultiplierCoinValue(coin.coinValue, slotManager.TotalBet);
                                            break;
                                        }
                                    }
                                }
                                else if (symbolId == 15)
                                {
                                    foreach (var coin in socketManager.resultData.payload.coinPositions)
                                    {
                                        if (coin.position[0] == j && coin.position[1] == i)
                                        {
                                            view.SetGoldCoinValue(coin.coinValue * slotManager.TotalBet);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
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

    internal void UpdateLockedCashCollects(List<LockedCashCollect> lockedList)
    {
        

        int[,] lockedMatrix = new int[Slot.Count, Slot[0].slotImages.Count];
        int[,] remainingSpinsMatrix = new int[Slot.Count, Slot[0].slotImages.Count];

        if (lockedList != null)
        {
            foreach (var item in lockedList)
            {
                if (item.position != null && item.position.Count == 2)
                {
                    int row = item.position[0];
                    int col = item.position[1];
                    
                    if (col >= 0 && col < Slot.Count && row >= 0 && row < Slot[col].slotImages.Count)
                    {
                        if (item.spinsRemaining > 0)
                        {
                            lockedMatrix[col, row] = 1;
                            remainingSpinsMatrix[col, row] = item.spinsRemaining;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < Slot.Count; i++) 
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++) 
            {
                SlotSymbolView view = symbolViews[i][j];
                if (lockedMatrix[i, j] == 1)
                {
                    
                    if (view != null) view.ClearValues();

                    if (slotManager != null && slotManager.SlotSymbols != null && slotManager.SlotSymbols.Length > 14)
                    {
                        Slot[i].slotImages[j].sprite = slotManager.SlotSymbols[14];
                    }
                    Slot[i].slotImages[j].gameObject.SetActive(true);

                    if (view != null)
                    {
                        slotManager.ConfigureSymbolView(view, 14);
                        view.SetCountValue(remainingSpinsMatrix[i, j]);
                    }
                }
                else
                {
                    
                    if (Slot[i].slotImages[j].gameObject.activeSelf)
                    {
                        
                    }
                    if (view != null)
                    {
                        view.ClearValues();
                    }
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
    }

    internal bool IsPositionLocked(int col, int row)
    {
        if (col >= 0 && col < Slot.Count && row >= 0 && row < Slot[col].slotImages.Count)
        {
            return Slot[col].slotImages[row].gameObject.activeSelf;
        }
        return false;
    }

    internal SlotSymbolView GetLockedSymbolView(int col, int row)
    {
        if (col >= 0 && col < Slot.Count && row >= 0 && row < Slot[col].slotImages.Count)
        {
            return symbolViews[col][row];
        }
        return null;
    }
}
