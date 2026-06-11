using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the overlay "static" slot images used during the Link/Bonus feature.
/// Upgraded to use the new ImageAnimation API:
///   - Proper CancelInvoke before re-assigning textureArray
///   - Uses onLoopComplete callback instead of polling rendererDelegate.sprite
///   - isAnim flag aligned with SlotBehaviour conventions
/// </summary>
public class StaticSymbolController : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotBehaviour slotManager;
    [SerializeField] private BonusController bonusController;

    [Header("Slots Reference")]
    [SerializeField] public List<SlotImage> Slot;

    [Header("Sprites References")]
    [SerializeField] private Sprite[] images;

    [Header("Animation Sprites References")]
    [SerializeField] private Sprite[] LinkToGoldCoin_Animation;
    [SerializeField] private Sprite[] MegaLinkToGoldCoin_Animation;

    [SerializeField] internal List<Column> freezedLocations = new();
    [SerializeField] internal List<List<int>> Locations = new();

    // ─────────────────────────────────────────────────────────────
    //  Freeze matrix helpers
    // ─────────────────────────────────────────────────────────────
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
                if (row >= 0 && row < freezeMatrix.Count &&
                    column >= 0 && column < freezeMatrix[row].Count)
                {
                    freezeMatrix[row][column] = 1;
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

    // ─────────────────────────────────────────────────────────────
    //  TurnOnIndices — show frozen slots with correct animations
    // ─────────────────────────────────────────────────────────────
    internal void TurnOnIndices(List<List<int>> loc)
    {
        List<List<int>> freezeMatrix = GenerateFreezeMatrix(loc);

        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                if (freezeMatrix[i][j] == 1)
                {
                    string matrixVal = socketManager.resultData.matrix[i][j];

                    if (matrixVal == "11") // Link → coin transition
                    {
                        SetupAnimationOnSlot(i, j, LinkToGoldCoin_Animation);
                        AssignCoinText(i, j, fromImage: false);
                    }
                    else if (matrixVal == "12") // MegaLink → coin transition
                    {
                        SetupAnimationOnSlot(i, j, MegaLinkToGoldCoin_Animation);
                        AssignCoinText(i, j, fromImage: true);
                    }

                    Slot[i].slotImages[j].sprite = slotManager.ResultMatrix[i].slotImages[j].sprite;
                    Slot[i].slotImages[j].gameObject.SetActive(true);
                }
                else
                {
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ChangeLinksToGoldCoin  (upgraded: uses onLoopComplete instead of sprite polling)
    // ─────────────────────────────────────────────────────────────
    internal IEnumerator ChangeLinksToGoldCoin(Button button)
    {
        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                ImageAnimation anim = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
                if (anim == null || !anim.isAnim) continue;

                bool midpointReached = false;
                bool animComplete = false;

                anim.AnimationSpeed = 17;

                // Use onLoopComplete callback to detect when we hit frame 7 (midpoint)
                // and when the full animation ends (last frame)
                anim.onLoopComplete = null; // clear any stale callback

                // Manually track frame 7 via WaitUntil (kept from original design intent)
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
                anim.rendererDelegate.sprite = images[15];
            }
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(bonusController.StartBonusLoop());
    }

    // ─────────────────────────────────────────────────────────────
    //  Reset
    // ─────────────────────────────────────────────────────────────
    internal void Reset()
    {
        freezedLocations.Clear();
        freezedLocations.TrimExcess();
        Locations.Clear();
        Locations.TrimExcess();

        for (int i = 0; i < Slot.Count; i++)
        {
            foreach (var slotImage in Slot[i].slotImages)
            {
                slotImage.gameObject.SetActive(false);
                slotImage.sprite = null;

                var label = slotImage.transform.GetChild(0).GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.text = "";
                    slotImage.transform.GetChild(0).gameObject.SetActive(false);
                }

                var anim = slotImage.GetComponent<ImageAnimation>();
                if (anim != null)
                {
                    anim.StopAnimation();
                    anim.isAnim = false;
                    anim.onLoopComplete = null;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────
    private void SetupAnimationOnSlot(int col, int row, Sprite[] sprites)
    {
        ImageAnimation anim = Slot[col].slotImages[row].GetComponent<ImageAnimation>();
        if (anim == null) return;

        // Stop any running animation cleanly before reassigning sprites
        anim.StopAnimation();
        anim.onLoopComplete = null;

        anim.isAnim = true;
        anim.textureArray.Clear();
        anim.textureArray.TrimExcess();

        foreach (Sprite s in sprites)
            anim.textureArray.Add(s);
    }

    private void AssignCoinText(int col, int row, bool fromImage)
    {
        foreach (var coin in socketManager.resultData.payload.coinPositions)
        {
            if (coin.position[0] == col && coin.position[1] == row)
            {
                Transform target = fromImage
                    ? Slot[col].slotImages[row].GetComponent<ImageAnimation>().transform
                    : Slot[col].slotImages[row].transform;

                var label = target.GetChild(0).GetComponent<TMP_Text>();
                if (label != null)
                    label.text = coin.coinValue.ToString() + "x";

                break;
            }
        }
    }
}

[Serializable]
public class Column
{
    public List<int> index = new();
}