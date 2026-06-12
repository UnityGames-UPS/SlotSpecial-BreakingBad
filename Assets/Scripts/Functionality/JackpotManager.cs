using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class JackpotManager : MonoBehaviour
{
    [Header("Jackpot Main View Components")]
    [SerializeField] private GameObject jackpotSlotMain;         // Single object for Jackpot
    [SerializeField] private RectTransform jackpotSlotMainRT;     // RectTransform of the main jackpot slot
    [SerializeField] private CanvasGroup jackpotSlotMainCG;       // CanvasGroup for fade control

    [Header("Diamond Layer Visuals")]
    [SerializeField] private GameObject baseLayer;                // Diamond base layer
    [SerializeField] private GameObject secondaryLayer;           // Diamond secondary layer

    [Header("Mini Slot Spinning Config")]
    [SerializeField] private GameObject slotParent;               // The scrolling mini-slot container
    [SerializeField] private Image[] jackpotImages;               // The 4 images inside the jackpot parent
    [SerializeField] private Sprite[] jackpotSprites;             // The sprites for Grand, Major, Minor, Mini
    [SerializeField] private TMP_Text resultText;                 // Text component inside the jackpot main overlay
    [SerializeField] private GameObject winBlur;                  // Win blur overlay inside the jackpot main overlay

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
        if ((jackpotImages == null || jackpotImages.Length == 0) && slotParent != null)
        {
            jackpotImages = slotParent.GetComponentsInChildren<Image>(true);
        }
    }

    public IEnumerator PlayJackpotSequence(SlotSymbolView triggeringSymbolView, int prizeTypeIndex, string prizeValue, Sprite prizeSprite)
    {
        if (triggeringSymbolView == null)
        {
            Debug.LogError("triggeringSymbolView is null!");
            yield break;
        }

        Vector3 triggerWorldPos = triggeringSymbolView.transform.position;

        // 1. Diamond appears at triggering symbol position
        jackpotSlotMainRT.position = triggerWorldPos;
        jackpotSlotMainRT.localScale = Vector3.one;
        jackpotSlotMainCG.alpha = 0f;
        jackpotSlotMain.SetActive(true);

        if (winBlur != null) winBlur.SetActive(false);
        if (resultText != null) resultText.text = "";

        // Ensure visuals are enabled
        if (baseLayer != null) baseLayer.SetActive(true);
        
        // Initial setup for secondaryLayer: starts transparent/hidden, fades in later
        CanvasGroup secondaryCG = null;
        if (secondaryLayer != null)
        {
            secondaryCG = secondaryLayer.GetComponent<CanvasGroup>();
            if (secondaryCG == null) secondaryCG = secondaryLayer.AddComponent<CanvasGroup>();
            secondaryCG.alpha = 0f;
            secondaryLayer.SetActive(true);
        }

        // Fade out triggering symbol's main image and fade in the jackpot overlay
        if (triggeringSymbolView.mainImage != null)
        {
            triggeringSymbolView.mainImage.DOFade(0f, 0.3f);
        }
        yield return jackpotSlotMainCG.DOFade(1f, 0.3f).WaitForCompletion();

        // 2. Moves smoothly to Position = (0,0,0) (center of screen/parent)
        // 3. Scales smoothly to (3.03, 3.03, 3.03)
        Sequence moveScaleSeq = DOTween.Sequence();
        moveScaleSeq.Append(jackpotSlotMainRT.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuad));
        moveScaleSeq.Join(jackpotSlotMainRT.DOScale(new Vector3(3.03f, 3.03f, 3.03f), 0.5f).SetEase(Ease.OutBack));

        yield return moveScaleSeq.WaitForCompletion();

        // Diamond 2nd layer fade transition
        if (secondaryCG != null)
        {
            yield return secondaryCG.DOFade(1f, 0.5f).WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 4. Spin the mini slot!
        Sprite[] spritesToUse = (jackpotSprites != null && jackpotSprites.Length > 0)
            ? jackpotSprites
            : FindFirstObjectByType<SlotManager>()?.JackpotSlotSymbols;

        if (jackpotImages != null && jackpotImages.Length > 0 && spritesToUse != null && spritesToUse.Length > 0)
        {
            float spinDuration = 2.0f;
            float interval = 0.06f;
            int steps = Mathf.RoundToInt(spinDuration / interval);

            for (int step = 0; step < steps; step++)
            {
                for (int i = 0; i < jackpotImages.Length; i++)
                {
                    int randIdx = UnityEngine.Random.Range(0, spritesToUse.Length);
                    jackpotImages[i].sprite = spritesToUse[randIdx];
                    jackpotImages[i].gameObject.SetActive(true);
                }
                yield return new WaitForSeconds(interval);
            }

            // Settle on final result: 1st image shows the winning prize sprite
            if (jackpotImages.Length > 1)
            {
                jackpotImages[1].sprite = prizeSprite;
            }

            // Other images (0, 2, 3) show buffer/other sprites
            for (int i = 0; i < jackpotImages.Length; i++)
            {
                if (i == 1) continue;

                Sprite bufferSprite = null;
                if (spritesToUse.Length > 1)
                {
                    do
                    {
                        bufferSprite = spritesToUse[UnityEngine.Random.Range(0, spritesToUse.Length)];
                    } while (bufferSprite == prizeSprite);
                }
                else if (spritesToUse.Length > 0)
                {
                    bufferSprite = spritesToUse[0];
                }
                jackpotImages[i].sprite = bufferSprite;
            }
        }

        if (winBlur != null) winBlur.SetActive(true);
        if (resultText != null) resultText.text = prizeValue;

        yield return new WaitForSeconds(2.0f);

        // Return to start position and scale down
        Sequence returnSeq = DOTween.Sequence();
        returnSeq.Append(jackpotSlotMainRT.DOMove(triggerWorldPos, 0.5f).SetEase(Ease.InOutQuad));
        returnSeq.Join(jackpotSlotMainRT.DOScale(Vector3.one, 0.5f).SetEase(Ease.InQuad));

        yield return returnSeq.WaitForCompletion();

        // 5. Copy the final sprites and display result on the main slot icon jackpot object
        if (jackpotImages != null)
        {
            Sprite[] finalSprites = new Sprite[jackpotImages.Length];
            for (int i = 0; i < jackpotImages.Length; i++)
            {
                finalSprites[i] = jackpotImages[i].sprite;
            }
            triggeringSymbolView.ShowJackpotResult(finalSprites, prizeValue);
        }

        // 6. Both get fade transition (fade out the overlay)
        yield return jackpotSlotMainCG.DOFade(0f, 0.3f).WaitForCompletion();
        jackpotSlotMain.SetActive(false);

        if (winBlur != null) winBlur.SetActive(false);
        if (resultText != null) resultText.text = "";
    }
}
