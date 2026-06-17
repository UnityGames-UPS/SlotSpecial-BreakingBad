using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotSymbolView : MonoBehaviour
{
    [Header("Symbol Layers")]
    [SerializeField] public Image mainImage;
    [SerializeField] public CanvasGroup canvasGroup;
    [SerializeField] public Image backTint;
    [SerializeField] public Image specialSymbolLayer;
    [SerializeField] public GameObject hatObject;
    [SerializeField] public TMP_Text losPolosValueText;
    [SerializeField] public TMP_Text goldCoinValueText;
    [SerializeField] public GameObject jackpotObject;
    [SerializeField] public Transform jackpotStripParent;
    [SerializeField] public TMP_Text jackpotResultText;

    public void SetupFromHierarchy()
    {
        // Try to set references dynamically if not assigned in the inspector
        if (mainImage == null) mainImage = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // We map based on standard child index mapping to remain compatible with the scene structure
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
                // Disable any legacy Image component on this object to prevent overlap
                var oldImg = child4.GetComponent<Image>();
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
            mainImage.gameObject.SetActive(true);
            mainImage.enabled = true;
            mainImage.color = new Color(mainImage.color.r, mainImage.color.g, mainImage.color.b, 1f);
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
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
        
        // Sprite Asset Mapping: Index 0-9 -> Digits 0-9, Index 10 -> "+" sign
        string valStr = value.ToString();
        string formattedText = "<sprite=10>"; // "+" sign
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

        // Sprite Asset Mapping: Index 0-9 -> Digits 0-9, Index 10 -> decimal point "."
        // Rule: Show decimals only when provided by the data
        string valStr = value.ToString("F2");
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
