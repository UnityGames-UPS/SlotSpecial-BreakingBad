using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class JackpotManager : MonoBehaviour
{
    [Header("Jackpot Main View Components")]
    [SerializeField] private GameObject jackpotSlotMain;         
    [SerializeField] private RectTransform jackpotSlotMainRT;     
    [SerializeField] private CanvasGroup jackpotSlotMainCG;       
    [SerializeField] private GameObject shineObject;              

    [Header("Diamond Layer Visuals")]
    [SerializeField] private GameObject baseLayer;                
    [SerializeField] private GameObject secondaryLayer;           

    [Header("Mini Slot Spinning Config")]
    [SerializeField] private GameObject slotParent;               
    [SerializeField] private Image[] jackpotImages;               
    [Header("Jackpot Sprites (0: Mini, 1: Minor, 2: Major, 3: Grand)")]
    [SerializeField] private Sprite[] jackpotBackgroundSprites;
    [SerializeField] private Sprite[] jackpotTextSprites;
    [SerializeField] private TMP_Text resultText;                 
    [SerializeField] private GameObject winBlur;                  

    private int currentCycleIndex = 0;
    private List<int> currentPermutation = new List<int>();

    private int getPrizeIndex(string prizeType, int defaultIndex)
    {
        if (string.IsNullOrEmpty(prizeType)) return defaultIndex;
        string lower = prizeType.ToLower();
        if (lower.Contains("mini")) return 0;
        if (lower.Contains("minor")) return 1;
        if (lower.Contains("major")) return 2;
        if (lower.Contains("grand")) return 3;
        return defaultIndex;
    }

    private void Awake()
    {
        if (jackpotSlotMain != null)
        {
            jackpotSlotMain.SetActive(false);
        }
        if (winBlur != null)
        {
            winBlur.SetActive(false);
        }
        if (shineObject != null)
        {
            shineObject.SetActive(false);
        }
        if ((jackpotImages == null || jackpotImages.Length == 0) && slotParent != null)
        {
            List<Image> imgsList = new List<Image>();
            for (int i = 0; i < slotParent.transform.childCount; i++)
            {
                Image img = slotParent.transform.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    imgsList.Add(img);
                }
            }
            jackpotImages = imgsList.ToArray();
        }
    }

    private void SetJackpotItemSprite(Image bgImage, int prizeIndex)
    {
        if (bgImage == null) return;
        
        Sprite[] bgSprites = (jackpotBackgroundSprites != null && jackpotBackgroundSprites.Length > 0) ? jackpotBackgroundSprites : FindFirstObjectByType<SlotManager>()?.JackpotSlotSymbols;
        if (bgSprites != null && prizeIndex >= 0 && prizeIndex < bgSprites.Length)
        {
            bgImage.sprite = bgSprites[prizeIndex];
        }

        if (bgImage.transform.childCount > 0)
        {
            Image textImage = bgImage.transform.GetChild(0).GetComponent<Image>();
            if (textImage != null && jackpotTextSprites != null && prizeIndex >= 0 && prizeIndex < jackpotTextSprites.Length)
            {
                textImage.sprite = jackpotTextSprites[prizeIndex];
                textImage.gameObject.SetActive(true);
            }
        }
    }

    private void ShiftJackpotImagesDownCyclic()
    {
        for (int j = jackpotImages.Length - 1; j > 0; j--)
        {
            if (jackpotImages[j] != null && jackpotImages[j - 1] != null)
            {
                jackpotImages[j].sprite = jackpotImages[j - 1].sprite;
                
                if (jackpotImages[j].transform.childCount > 0 && jackpotImages[j - 1].transform.childCount > 0)
                {
                    Image dstText = jackpotImages[j].transform.GetChild(0).GetComponent<Image>();
                    Image srcText = jackpotImages[j - 1].transform.GetChild(0).GetComponent<Image>();
                    if (dstText != null && srcText != null)
                    {
                        dstText.sprite = srcText.sprite;
                        dstText.gameObject.SetActive(srcText.gameObject.activeSelf);
                    }
                }
            }
        }

        if (currentPermutation != null && currentPermutation.Count > 0)
        {
            currentCycleIndex = (currentCycleIndex - 1 + currentPermutation.Count) % currentPermutation.Count;
            SetJackpotItemSprite(jackpotImages[0], currentPermutation[currentCycleIndex]);
        }
    }

    private void CopyJackpotLayoutToCell(SlotSymbolView cellView, string resultValue)
    {
        if (cellView == null || cellView.jackpotObject == null) return;

        cellView.jackpotObject.SetActive(true);

        
        if (cellView.specialSymbolLayer != null)
        {
            cellView.specialSymbolLayer.gameObject.SetActive(false);
        }

        if (cellView.mainImage != null)
        {
            cellView.mainImage.enabled = true;
            cellView.mainImage.color = new Color(cellView.mainImage.color.r, cellView.mainImage.color.g, cellView.mainImage.color.b, 1f);
        }

        if (cellView.canvasGroup != null)
        {
            cellView.canvasGroup.alpha = 1f;
        }

        Transform cellStripParent = cellView.jackpotStripParent;
        if (cellStripParent != null && jackpotImages != null)
        {
            cellStripParent.gameObject.SetActive(true);
            int childCount = Mathf.Min(cellStripParent.childCount, jackpotImages.Length);
            for (int i = 0; i < childCount; i++)
            {
                Transform cellItem = cellStripParent.GetChild(i);
                cellItem.gameObject.SetActive(true);
                
                Image cellBgImg = cellItem.GetComponent<Image>();
                if (cellBgImg != null && jackpotImages[i] != null)
                {
                    cellBgImg.sprite = jackpotImages[i].sprite;
                }

                if (cellItem.childCount > 0 && jackpotImages[i].transform.childCount > 0)
                {
                    Image cellTextImg = cellItem.GetChild(0).GetComponent<Image>();
                    Image overlayTextImg = jackpotImages[i].transform.GetChild(0).GetComponent<Image>();
                    if (cellTextImg != null && overlayTextImg != null)
                    {
                        cellTextImg.sprite = overlayTextImg.sprite;
                        cellTextImg.gameObject.SetActive(overlayTextImg.gameObject.activeSelf);
                    }
                }
            }
        }

        if (cellView.jackpotResultText != null)
        {
            cellView.jackpotResultText.text = resultValue;
            cellView.jackpotResultText.gameObject.SetActive(true);
        }
    }

    public IEnumerator PlayJackpotSequence(SlotSymbolView triggeringSymbolView, string prizeType, int prizeTypeIndex, string prizeValue, Sprite prizeSprite)
    {
        if (triggeringSymbolView == null)
        {
            Debug.LogError("triggeringSymbolView is null!");
            yield break;
        }

        if (shineObject != null)
        {
            shineObject.SetActive(false);
        }

        int resolvedPrizeIndex = getPrizeIndex(prizeType, prizeTypeIndex);
        Vector3 triggerWorldPos = triggeringSymbolView.transform.position;

        
        jackpotSlotMainRT.position = triggerWorldPos;
        jackpotSlotMainRT.localScale = Vector3.one;
        jackpotSlotMainCG.alpha = 0f;
        jackpotSlotMain.SetActive(true);

        if (winBlur != null) winBlur.SetActive(false);
        if (resultText != null) resultText.text = "";

        
        if (baseLayer != null) baseLayer.SetActive(true);
        
        
        if (secondaryLayer != null)
        {
            secondaryLayer.SetActive(true);
        }

        
        if (slotParent != null)
        {
            slotParent.SetActive(false);
        }

        
        if (triggeringSymbolView.canvasGroup != null)
        {
            triggeringSymbolView.canvasGroup.DOFade(0f, 0.3f);
        }
        else if (triggeringSymbolView.mainImage != null)
        {
            triggeringSymbolView.mainImage.DOFade(0f, 0.3f);
        }
        yield return jackpotSlotMainCG.DOFade(1f, 0.3f).WaitForCompletion();

        
        
        Sequence moveScaleSeq = DOTween.Sequence();
        moveScaleSeq.Append(jackpotSlotMainRT.DOLocalMove(Vector3.zero, 1.0f).SetEase(Ease.OutQuad));
        moveScaleSeq.Join(jackpotSlotMainRT.DOScale(new Vector3(3.03f, 3.03f, 3.03f), 1.0f).SetEase(Ease.OutBack));

        yield return moveScaleSeq.WaitForCompletion();

        
        yield return new WaitForSeconds(0.5f);

        int k = 0;
        int startIdx = 0;

        
        Sprite[] bgSprites = (jackpotBackgroundSprites != null && jackpotBackgroundSprites.Length > 0)
            ? jackpotBackgroundSprites
            : FindFirstObjectByType<SlotManager>()?.JackpotSlotSymbols;

        if (jackpotImages != null && jackpotImages.Length > 0 && bgSprites != null && bgSprites.Length > 0)
        {
            
            currentPermutation = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < currentPermutation.Count; i++)
            {
                int temp = currentPermutation[i];
                int randomIndex = UnityEngine.Random.Range(i, currentPermutation.Count);
                currentPermutation[i] = currentPermutation[randomIndex];
                currentPermutation[randomIndex] = temp;
            }

            k = currentPermutation.IndexOf(resolvedPrizeIndex);
            if (k < 0) k = 0;

            do
            {
                startIdx = UnityEngine.Random.Range(0, currentPermutation.Count);
            } while ((startIdx + 2) % currentPermutation.Count == k);

            currentCycleIndex = startIdx;

            
            for (int i = 0; i < jackpotImages.Length; i++)
            {
                int prizeIdx = currentPermutation[(startIdx + i) % currentPermutation.Count];
                SetJackpotItemSprite(jackpotImages[i], prizeIdx);
                jackpotImages[i].gameObject.SetActive(true);
            }
        }

        
        if (secondaryLayer != null)
        {
            secondaryLayer.SetActive(false);
        }
        if (slotParent != null)
        {
            slotParent.SetActive(true);
        }

        
        yield return new WaitForSeconds(0.5f);

        
        if (jackpotImages != null && jackpotImages.Length > 0 && bgSprites != null && bgSprites.Length > 0 && slotParent != null)
        {
            Vector3 initialPos = slotParent.transform.localPosition;
            float cellHeight = 100f;
            if (jackpotImages.Length > 1)
            {
                cellHeight = Mathf.Abs(jackpotImages[1].transform.localPosition.y - jackpotImages[0].transform.localPosition.y);
                if (cellHeight == 0) cellHeight = 100f;
            }

            
            slotParent.transform.localPosition = initialPos;
            Sequence startSeq = DOTween.Sequence();
            startSeq.Append(slotParent.transform.DOLocalMoveY(initialPos.y + 20f, 0.15f).SetEase(Ease.OutQuad));
            startSeq.Append(slotParent.transform.DOLocalMoveY(initialPos.y, 0.08f).SetEase(Ease.InQuad));
            yield return startSeq.WaitForCompletion();

            
            int minCycles = 12;
            int extra = (startIdx - 1 - k) % currentPermutation.Count;
            if (extra < 0) extra += currentPermutation.Count;
            int spinCycles = minCycles + extra;
            float stepDuration = 0.14f;

            for (int step = 0; step < spinCycles; step++)
            {
                slotParent.transform.localPosition = initialPos;
                yield return slotParent.transform.DOLocalMoveY(initialPos.y - cellHeight, stepDuration).SetEase(Ease.Linear).WaitForCompletion();
                ShiftJackpotImagesDownCyclic();
            }

            
            slotParent.transform.localPosition = initialPos;
            yield return slotParent.transform.DOLocalMoveY(initialPos.y - cellHeight, stepDuration).SetEase(Ease.Linear).WaitForCompletion();
            ShiftJackpotImagesDownCyclic();

            
            slotParent.transform.localPosition = initialPos;
            yield return slotParent.transform.DOLocalMoveY(initialPos.y - cellHeight, stepDuration).SetEase(Ease.Linear).WaitForCompletion();
            ShiftJackpotImagesDownCyclic();

            
            slotParent.transform.localPosition = initialPos;
            yield return slotParent.transform.DOLocalMoveY(initialPos.y - cellHeight, stepDuration).SetEase(Ease.Linear).WaitForCompletion();
            ShiftJackpotImagesDownCyclic();

            
            slotParent.transform.localPosition = initialPos;
            Sequence stopSeq = DOTween.Sequence();
            stopSeq.Append(slotParent.transform.DOLocalMoveY(initialPos.y - 25f, 0.20f).SetEase(Ease.OutQuad));
            stopSeq.Append(slotParent.transform.DOLocalMoveY(initialPos.y, 0.30f).SetEase(Ease.InOutQuad));
            yield return stopSeq.WaitForCompletion();
        }

        if (winBlur != null) winBlur.SetActive(true);
        if (resultText != null) resultText.text = prizeValue;

        if (shineObject != null)
        {
            shineObject.SetActive(true);
        }

        yield return new WaitForSeconds(2.0f);

        if (shineObject != null)
        {
            shineObject.SetActive(false);
        }

        
        Sequence returnSeq = DOTween.Sequence();
        returnSeq.Append(jackpotSlotMainRT.DOMove(triggerWorldPos, 0.5f).SetEase(Ease.InOutQuad));
        returnSeq.Join(jackpotSlotMainRT.DOScale(Vector3.one, 0.5f).SetEase(Ease.InQuad));

        yield return returnSeq.WaitForCompletion();

        
        CopyJackpotLayoutToCell(triggeringSymbolView, prizeValue);

        
        Sequence fadeSeq = DOTween.Sequence();
        fadeSeq.Append(jackpotSlotMainCG.DOFade(0f, 0.3f));
        if (triggeringSymbolView.canvasGroup != null)
        {
            triggeringSymbolView.canvasGroup.alpha = 0f;
            fadeSeq.Join(triggeringSymbolView.canvasGroup.DOFade(1f, 0.3f));
        }
        yield return fadeSeq.WaitForCompletion();
        jackpotSlotMain.SetActive(false);

        if (winBlur != null) winBlur.SetActive(false);
        if (resultText != null) resultText.text = "";
        if (shineObject != null)
        {
            shineObject.SetActive(false);
        }
    }
}
