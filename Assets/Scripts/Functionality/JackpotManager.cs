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
    [SerializeField] private float slotHeight = 671f;             // The scrolling height of the mini-slot
    [SerializeField] private float symbolOffset = -75f;           // Vertical spacing between mini-slot items
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
        if (secondaryLayer != null) secondaryLayer.SetActive(true);

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
        yield return new WaitForSeconds(0.5f);

        // 4. Spin the mini slot!
        if (slotParent != null)
        {
            Vector3 startPos = slotParent.transform.localPosition;
            
            // Force start at top
            slotParent.transform.localPosition = startPos;

            float scrollDuration = 0.35f; // speed (lower = faster)

            Tweener simpleTween = DOTween.To(
                () => slotParent.transform.localPosition.y,
                y => slotParent.transform.localPosition = new Vector3(startPos.x, y, startPos.z),
                startPos.y - slotHeight, // move DOWN
                scrollDuration
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnStepComplete(() =>
            {
                // INSTANT jump back to top
                slotParent.transform.localPosition = startPos;
            });

            // Spin for 3 seconds
            yield return new WaitForSeconds(3.0f);

            // Clean stop
            bool stepDone = false;
            simpleTween.OnStepComplete(() => stepDone = true);
            yield return new WaitUntil(() => stepDone);

            simpleTween.Kill();

            // Final settle position
            float finalY = startPos.y + (symbolOffset * prizeTypeIndex);
            yield return slotParent.transform
                .DOLocalMoveY(finalY, 0.25f)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }

        if (winBlur != null) winBlur.SetActive(true);
        if (resultText != null) resultText.text = prizeValue;

        yield return new WaitForSeconds(2.0f);

        // Return to start position and scale down
        Sequence returnSeq = DOTween.Sequence();
        returnSeq.Append(jackpotSlotMainRT.DOMove(triggerWorldPos, 0.5f).SetEase(Ease.InOutQuad));
        returnSeq.Join(jackpotSlotMainRT.DOScale(Vector3.one, 0.5f).SetEase(Ease.InQuad));

        yield return returnSeq.WaitForCompletion();

        // 5. Show jackpot result in the reel symbol
        triggeringSymbolView.ShowJackpotResult(prizeTypeIndex, symbolOffset, prizeValue);

        // 6. Both get fade transition (fade out the overlay)
        yield return jackpotSlotMainCG.DOFade(0f, 0.3f).WaitForCompletion();
        jackpotSlotMain.SetActive(false);

        if (winBlur != null) winBlur.SetActive(false);
        if (resultText != null) resultText.text = "";
    }
}
