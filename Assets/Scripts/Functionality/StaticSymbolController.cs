using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaticSymbolController : MonoBehaviour
{
  [Header("Scripts References")]
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

  internal List<List<int>> GenerateFreezeMatrix(List<List<int>> loc, bool dontReturn = false)
  {
    for (int i = 0; i < loc.Count; i++)
    {
      if (!Locations.Contains(loc[i]))
      {
        Locations.Add(loc[i]);
      }
    }
    // Initialize matrix with 0s
    List<List<int>> freezeMatrix = new List<List<int>>();

    for (int i = 0; i < Slot.Count; i++)
    {
      List<int> row = new List<int>(new int[Slot[i].slotImages.Count]);
      freezeMatrix.Add(row);
    }

    // Set 1s for frozen slots based on loc
    foreach (List<int> indexPair in Locations)
    {
      if (indexPair.Count == 2)
      {
        int row = indexPair[0];
        int column = indexPair[1];

        // Check bounds
        if (row >= 0 && row < freezeMatrix.Count &&
            column >= 0 && column < freezeMatrix[row].Count)
        {
          freezeMatrix[row][column] = 1;
        }
      }
    }

    // Update freezedLocations with data from freezeMatrix
    freezedLocations.Clear();
    foreach (var row in freezeMatrix)
    {
      Column column = new() { index = new List<int>(row) };
      freezedLocations.Add(column);
    }

    if (!dontReturn)
    {
      return freezeMatrix;
    }
    else
    {
      return null;
    }
  }

  // Method to apply the freeze effect to SlotImages based on the freeze matrix
  internal void TurnOnIndices(List<List<int>> loc)
  {
    // Generate the freeze matrix
    List<List<int>> freezeMatrix = GenerateFreezeMatrix(loc);

    // Apply the freeze effect based on the freeze matrix
    for (int i = 0; i < Slot.Count; i++)
    {
      for (int j = 0; j < Slot[i].slotImages.Count; j++)
      {
        if (freezeMatrix[i][j] == 1)
        {
          // Apply effect to frozen slots
          if (socketManager.resultData.matrix[i][j] == "11") //Checking if the frozen slot is a link and turning it to a coin
          {
            Debug.Log("Plaing link to coin");

            ImageAnimation imageAnimation = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
            imageAnimation.isAnim = true;
            imageAnimation.textureArray.Clear();
            imageAnimation.textureArray.TrimExcess();
            foreach (Sprite s in LinkToGoldCoin_Animation)
            {
              imageAnimation.textureArray.Add(s);
            }

            //  Add Value of the coins text component
            foreach (var coin in socketManager.resultData.payload.coinPositions)                                 //V2
            {
              if (coin.position[0] == i && coin.position[1] == j)
              {
                Slot[i].slotImages[j].transform.GetChild(0).GetComponent<TMP_Text>().text = coin.coinValue.ToString() + "x";
                break;
              }
            }
          }
          else if (socketManager.resultData.matrix[i][j] == "12")
          { //checking if its megalink
            ImageAnimation imageAnimation = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
            imageAnimation.isAnim = true;
            imageAnimation.textureArray.Clear();
            imageAnimation.textureArray.TrimExcess();
            foreach (Sprite s in MegaLinkToGoldCoin_Animation)
            {
              imageAnimation.textureArray.Add(s);
            }

            //  Add Value of the coins text component
            foreach (var coin in socketManager.resultData.payload.coinPositions)                                 //V2
            {
              if (coin.position[0] == i && coin.position[1] == j)
              {
                imageAnimation.transform.GetChild(0).GetComponent<TMP_Text>().text = coin.coinValue.ToString() + "x";
                break;
              }
            }
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

  internal IEnumerator ChangeLinksToGoldCoin(Button button)
  {
    for (int i = 0; i < Slot.Count; i++)
    {
      for (int j = 0; j < Slot[i].slotImages.Count; j++)
      {
        ImageAnimation anim = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
        if (anim.isAnim)
        {
          anim.AnimationSpeed = 17;
          anim.StartAnimation();
          yield return new WaitUntil(() => anim.rendererDelegate.sprite == anim.textureArray[7]);
          anim.transform.GetChild(0).gameObject.SetActive(true);
          yield return new WaitUntil(() => anim.rendererDelegate.sprite == anim.textureArray[^1]);
          anim.StopAnimation();
          anim.rendererDelegate.sprite = images[15];
        }
      }
    }

    yield return new WaitForSeconds(1f);
    StartCoroutine(bonusController.StartBonusLoop());
    // button.interactable = false;
  }

  internal void Reset()
  {
    freezedLocations.Clear();
    freezedLocations.TrimExcess();
    Locations.Clear();
    Locations.TrimExcess();
    for (int i = 0; i < Slot.Count; i++)
    {
      foreach (var j in Slot[i].slotImages)
      {
        j.gameObject.SetActive(false);
        j.sprite = null;
        j.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = "";
        j.transform.GetChild(0).gameObject.SetActive(false);
        j.GetComponent<ImageAnimation>().isAnim = false;
      }
    }
  }
}

[Serializable]
public class Column
{
  public List<int> index = new();
}
