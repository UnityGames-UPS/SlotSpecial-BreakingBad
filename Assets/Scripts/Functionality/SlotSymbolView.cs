using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SlotSymbolView : MonoBehaviour
{
    [Header("Symbol Layers")]
    [SerializeField] internal Image mainImage;
    [SerializeField] internal CanvasGroup canvasGroup;
    [SerializeField] internal Image backTint;
    [SerializeField] internal Image specialSymbolLayer;
    [SerializeField] internal GameObject hatObject;
    [SerializeField] internal TMP_Text losPolosValueText;
    [SerializeField] internal TMP_Text goldCoinValueText;
    [SerializeField] internal TMP_Text multiplierValueText;
    [SerializeField] internal GameObject jackpotObject;
    [SerializeField] internal Transform jackpotStripParent;
    [SerializeField] internal TMP_Text jackpotResultText;

    [Header("Locked Cash Collect")]
    [SerializeField] internal Image countImage;
    [SerializeField] internal Sprite[] countSprites;

    [Header("Cash Collect Indicators")]
    [SerializeField] internal GameObject cashCollectAboveObject;
    [SerializeField] internal GameObject cashCollectBelowObject;

    public void SetCountValue(int count)
    {
        if (countImage == null) return;
        if (countSprites != null && count >= 1 && count <= countSprites.Length)
        {
            countImage.sprite = countSprites[count - 1];
            countImage.gameObject.SetActive(true);
        }
        else
        {
            countImage.gameObject.SetActive(false);
        }
    }

    public void SetupFromHierarchy()
    {
        
        if (mainImage == null) mainImage = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        
        if (transform.childCount > 0 && specialSymbolLayer == null)
        {
            specialSymbolLayer = transform.GetChild(0).GetComponent<Image>();
        }
        if (transform.childCount > 1 && hatObject == null)
        {
            hatObject = transform.GetChild(1).gameObject;
        }
        if (transform.childCount > 2 && backTint == null)
        {
            backTint = transform.GetChild(2).GetComponent<Image>();
        }
        if (transform.childCount > 3 && goldCoinValueText == null)
        {
            goldCoinValueText = transform.GetChild(3).GetComponent<TMP_Text>();
        }
        if (transform.childCount > 4 && losPolosValueText == null)
        {
            var child4 = transform.GetChild(4);
            losPolosValueText = child4.GetComponent<TMP_Text>();
            if (losPolosValueText == null)
            {
                losPolosValueText = child4.gameObject.AddComponent<TMP_Text>();
                
                var oldImg = child4.GetComponent<Image>();
                if (oldImg != null) oldImg.enabled = false;
            }
        }
        if (transform.childCount > 5 && multiplierValueText == null)
        {
            var child5 = transform.GetChild(5);
            multiplierValueText = child5.GetComponent<TMP_Text>();
            if (multiplierValueText == null)
            {
                multiplierValueText = child5.gameObject.AddComponent<TMP_Text>();
                
                var oldImg = child5.GetComponent<Image>();
                if (oldImg != null) oldImg.enabled = false;
            }
        }
        if (transform.childCount > 6 && jackpotObject == null)
        {
            jackpotObject = transform.GetChild(6).gameObject;
        }

        if (jackpotObject != null)
        {
            if (jackpotStripParent == null && jackpotObject.transform.childCount > 0)
            {
                jackpotStripParent = jackpotObject.transform.GetChild(0);
            }
            if (jackpotResultText == null)
            {
                jackpotResultText = jackpotObject.GetComponentInChildren<TMP_Text>(true);
            }
        }
    }

    private void Awake()
    {
        SetupFromHierarchy();
    }

    public void ClearValues()
    {
        if (losPolosValueText != null)
        {
            losPolosValueText.text = "";
            losPolosValueText.gameObject.SetActive(false);
        }
        if (goldCoinValueText != null)
        {
            goldCoinValueText.text = "";
            goldCoinValueText.gameObject.SetActive(false);
        }
        if (multiplierValueText != null)
        {
            multiplierValueText.text = "";
            multiplierValueText.gameObject.SetActive(false);
        }
        if (specialSymbolLayer != null)
        {
            specialSymbolLayer.gameObject.SetActive(false);
        }
        if (hatObject != null)
        {
            hatObject.SetActive(false);
        }
        if (jackpotObject != null)
        {
            jackpotObject.SetActive(false);
        }
        if (jackpotResultText != null)
        {
            jackpotResultText.text = "";
            jackpotResultText.gameObject.SetActive(false);
        }
        if (mainImage != null)
        {
            
            mainImage.enabled = true;
            mainImage.color = new Color(mainImage.color.r, mainImage.color.g, mainImage.color.b, 1f);
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        if (countImage != null)
        {
            countImage.gameObject.SetActive(false);
        }
        if (cashCollectAboveObject != null)
        {
            cashCollectAboveObject.SetActive(false);
        }
        if (cashCollectBelowObject != null)
        {
            cashCollectBelowObject.SetActive(false);
        }
    }

    public void ShowJackpotResult(Sprite[] finalSprites, string resultValue)
    {
        if (jackpotObject != null)
        {
            jackpotObject.SetActive(true);
        }
        if (specialSymbolLayer != null)
        {
            specialSymbolLayer.gameObject.SetActive(false);
        }
        if (jackpotStripParent != null)
        {
            Image[] slotSymbolJackpotImages = jackpotStripParent.GetComponentsInChildren<Image>(true);
            if (slotSymbolJackpotImages != null && finalSprites != null)
            {
                for (int i = 0; i < Mathf.Min(slotSymbolJackpotImages.Length, finalSprites.Length); i++)
                {
                    slotSymbolJackpotImages[i].sprite = finalSprites[i];
                    slotSymbolJackpotImages[i].gameObject.SetActive(true);
                }
            }
        }
        if (jackpotResultText != null)
        {
            jackpotResultText.text = resultValue;
            jackpotResultText.gameObject.SetActive(true);
        }
        if (mainImage != null)
        {
            mainImage.enabled = false;
        }
    }

    public void SetLosPolosValue(int value)
    {
        if (losPolosValueText == null) return;
        
        
        string valStr = value.ToString();
        string formattedText = "<sprite=10>"; 
        foreach (char c in valStr)
        {
            if (char.IsDigit(c))
            {
                formattedText += $"<sprite={c - '0'}>";
            }
        }

        losPolosValueText.text = formattedText;
        losPolosValueText.gameObject.SetActive(true);
    }

    public void SetGoldCoinValue(double value)
    {
        if (goldCoinValueText == null) return;

        
        
        string valStr = value.ToString("0.###");
        string formattedText = "";
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

        goldCoinValueText.text = formattedText;
        goldCoinValueText.gameObject.SetActive(true);
    }

    public void SetMultiplierCoinValue(double multiplierValue, double totalBet)
    {
        if (multiplierValueText == null) return;

        multiplierValueText.text = "X" + multiplierValue.ToString();
        multiplierValueText.gameObject.SetActive(true);
    }

    public IEnumerator PlayMultiplierConversion(double multiplierValue, double totalBet)
    {
        // Step 1: Show multiplier text "X25"
        if (multiplierValueText != null)
        {
            multiplierValueText.text = "X" + multiplierValue.ToString();
            multiplierValueText.gameObject.SetActive(true);
            multiplierValueText.transform.localScale = Vector3.one;
        }

        // Hide gold coin text initially
        if (goldCoinValueText != null)
        {
            goldCoinValueText.gameObject.SetActive(false);
        }

        // Wait for player to see the multiplier
        yield return new WaitForSeconds(0.5f);

        // Step 2: Scale multiplier text to 0
        if (multiplierValueText != null)
        {
            bool scaleDownDone = false;
            multiplierValueText.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() => scaleDownDone = true);
            yield return new WaitUntil(() => scaleDownDone);

            multiplierValueText.gameObject.SetActive(false);
            multiplierValueText.transform.localScale = Vector3.one;
        }

        // Step 3: Set gold coin value text with the calculated value
        double finalValue = multiplierValue * totalBet;
        if (goldCoinValueText != null)
        {
            string valStr = finalValue.ToString("0.###");
            string formattedText = "";
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
            goldCoinValueText.text = formattedText;
            goldCoinValueText.transform.localScale = Vector3.zero;
            goldCoinValueText.gameObject.SetActive(true);

            // Step 4: Scale gold coin text from 0 to 1
            bool scaleUpDone = false;
            goldCoinValueText.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => scaleUpDone = true);
            yield return new WaitUntil(() => scaleUpDone);
        }
    }

    public void SetBackTintActive(bool active, float alpha = 0.85f)
    {
        if (backTint == null) return;
        backTint.gameObject.SetActive(active);
        if (active)
        {
            backTint.color = new Color(backTint.color.r, backTint.color.g, backTint.color.b, alpha);
        }
    }
}
