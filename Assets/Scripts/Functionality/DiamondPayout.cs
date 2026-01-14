using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class DiamondPayout : MonoBehaviour
{

    [SerializeField] private GameObject winBlur;
    [SerializeField] private GameObject SlotParent;
    [SerializeField] private GameObject DiamondImage;
    [SerializeField] private Image ResultImage;
    [SerializeField] private TMP_Text ResultText;

    double offset = -75;



    private Tweener simpleTween;

    internal IEnumerator SlotTween(int resultIndex, string resultValue, Sprite resultSprite)
    {
        offset = offset * resultIndex;
        Transform slotTransform = SlotParent.transform;

        Vector3 startPos = slotTransform.localPosition;

        yield return new WaitForSeconds(2f);
        DiamondImage.SetActive(false);
        yield return new WaitForSeconds(2f);

        float slotHeight = 671f;        // slot length
        float scrollDuration = 0.35f;   // speed (lower = faster)

        // Force start at top
        slotTransform.localPosition = startPos;

        // 🔥 REAL SLOT SCROLL TWEEN
        simpleTween = DOTween.To(
            () => slotTransform.localPosition.y,
            y => slotTransform.localPosition = new Vector3(startPos.x, y, startPos.z),
            startPos.y - slotHeight,     // move DOWN
            scrollDuration
        )
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart) // 👈 KEY PART
        .OnStepComplete(() =>
        {
            // INSTANT jump back to top
            slotTransform.localPosition = startPos;
        });

        // Run scrolling
        yield return new WaitForSeconds(7f);

        // Clean stop
        bool stepDone = false;
        simpleTween.OnStepComplete(() => stepDone = true);
        yield return new WaitUntil(() => stepDone);

        simpleTween.Kill();

        // Final settle position
        yield return slotTransform
            .DOLocalMoveY(startPos.y + (float)offset, 0.25f)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

        yield return new WaitForSeconds(1f);

        ResultImage.sprite = resultSprite;
        winBlur.SetActive(true);
        ResultText.text = resultValue;
    }

}
