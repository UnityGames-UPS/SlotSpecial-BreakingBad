using UnityEngine;
using TMPro;
using DG.Tweening;

public class AnimationTextHelper : MonoBehaviour
{
    [SerializeField] public TMP_Text losPolosText;
    [SerializeField] public TMP_Text goldCoinText;
    [SerializeField] public TMP_Text payoutText;

    private Tween activeTextTween;

    public void SetupFromHierarchy()
    {
        if (losPolosText != null && goldCoinText != null && payoutText != null) return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            string lowerName = txt.gameObject.name.ToLower();
            if (lowerName.Contains("polo") || lowerName.Contains("lp"))
            {
                losPolosText = txt;
            }
            else if (lowerName.Contains("gold") || lowerName.Contains("coin"))
            {
                goldCoinText = txt;
            }
            else if (lowerName.Contains("payout") || lowerName.Contains("win"))
            {
                payoutText = txt;
            }
        }

        // Fallback by child order of TMP_Text components
        if (losPolosText == null || goldCoinText == null || payoutText == null)
        {
            int index = 0;
            foreach (Transform child in transform)
            {
                TMP_Text txt = child.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    if (index == 0 && losPolosText == null) losPolosText = txt;
                    else if (index == 1 && goldCoinText == null) goldCoinText = txt;
                    else if (index == 2 && payoutText == null) payoutText = txt;
                    index++;
                }
            }
        }
    }

    private void Awake()
    {
        SetupFromHierarchy();
    }

    public void Clear()
    {
        KillTween();

        if (losPolosText != null)
        {
            losPolosText.text = "";
            losPolosText.gameObject.SetActive(false);
            losPolosText.transform.localScale = Vector3.one;
        }
        if (goldCoinText != null)
        {
            goldCoinText.text = "";
            goldCoinText.gameObject.SetActive(false);
            goldCoinText.transform.localRotation = Quaternion.identity;
        }
        if (payoutText != null)
        {
            payoutText.transform.DOKill();
            payoutText.text = "";
            payoutText.gameObject.SetActive(false);
            payoutText.transform.localScale = Vector3.one;
        }
    }

    public void PlayTextAnimation(int symbolId, string textContent, float duration, bool loopIndefinitely = true)
    {
        Clear();

        if (symbolId == 17) // Los Pollos
        {
            if (losPolosText != null)
            {
                losPolosText.text = textContent;
                losPolosText.gameObject.SetActive(true);
                
                // Scale animation: 1 -> 1.3 -> 1 with duration matching loop duration
                losPolosText.transform.localScale = Vector3.one;
                int loops = loopIndefinitely ? -1 : 2;
                activeTextTween = losPolosText.transform.DOScale(1.3f, duration * 0.5f)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad);
            }
        }
        else if (symbolId == 15) // Gold Coin
        {
            if (goldCoinText != null)
            {
                goldCoinText.text = textContent;
                goldCoinText.gameObject.SetActive(true);
                
                // Tilt in Y axis rotation: 0 -> 20 -> 0 -> -20 -> 0 over the loop duration
                goldCoinText.transform.localRotation = Quaternion.identity;
                Sequence seq = DOTween.Sequence();
                seq.Append(goldCoinText.transform.DOLocalRotate(new Vector3(0, 20f, 0), duration * 0.25f).SetEase(Ease.InOutSine));
                seq.Append(goldCoinText.transform.DOLocalRotate(new Vector3(0, -20f, 0), duration * 0.5f).SetEase(Ease.InOutSine));
                seq.Append(goldCoinText.transform.DOLocalRotate(Vector3.zero, duration * 0.25f).SetEase(Ease.InOutSine));
                if (loopIndefinitely)
                {
                    seq.SetLoops(-1);
                }
                activeTextTween = seq;
            }
        }
    }

    public void PlayPayoutTextAnimation(string textContent, float duration)
    {
        if (payoutText != null)
        {
            payoutText.text = textContent;
            payoutText.gameObject.SetActive(true);
            payoutText.transform.DOKill();
            payoutText.transform.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence();
            seq.Append(payoutText.transform.DOScale(1.2f, duration * 0.4f).SetEase(Ease.OutBack));
            seq.Append(payoutText.transform.DOScale(1.0f, duration * 0.3f).SetEase(Ease.OutQuad));
            activeTextTween = seq;
        }
    }

    public void KillTween()
    {
        if (activeTextTween != null)
        {
            activeTextTween.Kill();
            activeTextTween = null;
        }
        if (payoutText != null)
        {
            payoutText.transform.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void OnDisable()
    {
        KillTween();
    }
}
