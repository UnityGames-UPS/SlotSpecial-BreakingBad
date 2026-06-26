using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class WinTypePopup : MonoBehaviour
{
    [Header("UI Objects")]
    [SerializeField] private GameObject winDisplayObject;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private GameObject winTextObject;
    [SerializeField] private GameObject winTypeObject;
    [SerializeField] private ImageAnimation winTypeImageAnimation;
    [SerializeField] private GameObject dollarObject;
    [SerializeField] private Button fullScreenButton;

    [Header("Win Type Sprite Lists")]
    [SerializeField] private List<Sprite> bigWinSprites;
    [SerializeField] private List<Sprite> hugeWinSprites;
    [SerializeField] private List<Sprite> megaWinSprites;

    [Header("Settings")]
    [SerializeField] private float minCountDuration = 1.0f;
    [SerializeField] private float maxCountDuration = 6.0f;
    [SerializeField] private float autoCloseDelay = 2.0f;

    [Header("Threshold Settings (Multipliers of Bet)")]
    [SerializeField] private double enableWinThreshold = 3.0;
    [SerializeField] private double bigWinThreshold = 5.0;
    [SerializeField] private double dollarThreshold = 10.0;
    [SerializeField] private double hugeWinThreshold = 25.0;
    [SerializeField] private double megaWinThreshold = 50.0;

    public double EnableWinThreshold => enableWinThreshold;

    private double finalWinAmount;
    private double currentWinCount;
    private double totalBet;
    private bool isCounting;
    private bool isAutoMode;
    private Action onCompleteCallback;
    private Coroutine countCoroutine;
    private Coroutine autoCloseCoroutine;

    private int activePhase = 0; // 0: Normal, 1: Big, 2: Huge, 3: Mega
    private ImageAnimation dollarImageAnimation;

    private void Awake()
    {
        if (fullScreenButton != null)
        {
            fullScreenButton.onClick.RemoveAllListeners();
            fullScreenButton.onClick.AddListener(OnScreenClicked);
        }
        else
        {
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnScreenClicked);
            }
        }
        if (winDisplayObject != null) winDisplayObject.SetActive(false);
        if (winTypeObject != null) winTypeObject.SetActive(false);
        if (dollarObject != null)
        {
            dollarImageAnimation = dollarObject.GetComponent<ImageAnimation>();
            dollarObject.SetActive(false);
        }
    }

    private Transform GetWinTextObjectTransform()
    {
        if (winTextObject != null) return winTextObject.transform;
        if (winText != null) return winText.transform;
        return null;
    }

    public void StartPopup(double winAmount, double totalBetAmount, bool isAuto, Action onComplete)
    {
        if (AudioController.Instance != null) AudioController.Instance.PlayWinTypeLoop();
        finalWinAmount = winAmount;
        totalBet = totalBetAmount;
        isAutoMode = isAuto;
        onCompleteCallback = onComplete;
        isCounting = true;
        currentWinCount = 0;
        activePhase = 0;

        // Reset UI elements
        if (winText != null) winText.text = "0.00";
        if (winDisplayObject != null)
        {
            winDisplayObject.SetActive(true);
        }
        Transform textT = GetWinTextObjectTransform();
        if (textT != null)
        {
            textT.localScale = Vector3.one;
        }
        if (winTypeObject != null) winTypeObject.SetActive(false);
        if (dollarObject != null) dollarObject.SetActive(false);

        if (winTypeImageAnimation != null)
        {
            winTypeImageAnimation.StopAnimation();
        }

        // Cancel any running coroutines
        if (countCoroutine != null) StopCoroutine(countCoroutine);
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        countCoroutine = StartCoroutine(CountSequence());
    }

    private IEnumerator CountSequence()
    {
        float elapsed = 0f;
        double maxMultiplier = finalWinAmount / totalBet;
        float t = Mathf.InverseLerp((float)enableWinThreshold, (float)megaWinThreshold, (float)maxMultiplier);
        float duration = Mathf.Lerp(minCountDuration, maxCountDuration, t);

        // Smooth decimal formatting:
        // Detect how many decimals the final amount has.
        // We will default to 2, or use 3 if final amount has 3 decimals.
        int decimals = 2;
        string sVal = finalWinAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int dot = sVal.IndexOf('.');
        if (dot >= 0)
        {
            int count = sVal.Length - dot - 1;
            if (count > 2) decimals = 3;
        }
        string formatStr = decimals == 3 ? "F3" : "F2";

        Vector3 startScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.2f, 1.2f, 1.2f);

        Transform textT = GetWinTextObjectTransform();
        if (textT != null)
        {
            textT.localScale = startScale;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            currentWinCount = dvalueLerp(0, finalWinAmount, progress);
            
            if (winText != null)
            {
                winText.text = currentWinCount.ToString(formatStr);
            }

            if (textT != null)
            {
                textT.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            UpdatePhases(currentWinCount);

            yield return null;
        }

        CompleteCounting();
        autoCloseCoroutine = StartCoroutine(AutoCloseSequence());
    }

    private double dvalueLerp(double start, double end, float t)
    {
        return start + (end - start) * t;
    }

    private void UpdatePhases(double currentAmount)
    {
        double multiplier = currentAmount / totalBet;

        // Phase 1: Big Win (>= bigWinThreshold)
        if (multiplier >= bigWinThreshold && activePhase < 1)
        {
            activePhase = 1;
            EnableWinType(bigWinSprites);
        }
 
        // Dollar object enable (>= dollarThreshold)
        if (multiplier >= dollarThreshold && dollarObject != null && !dollarObject.activeSelf)
        {
            dollarObject.SetActive(true);
            if (dollarImageAnimation != null)
            {
                dollarImageAnimation.useLoopRange = true;
                dollarImageAnimation.loopRangeStart = 4; // 5th element (index 4)
                dollarImageAnimation.loopRangeEnd = 12;  // 13th element (index 12)
                dollarImageAnimation.exitLoopRange = false;
                dollarImageAnimation.stopAtLastFrameOnEnd = true;
                dollarImageAnimation.StartAnimation();
            }
        }
 
        // Phase 2: Huge Win (>= hugeWinThreshold)
        if (multiplier >= hugeWinThreshold && activePhase < 2)
        {
            activePhase = 2;
            EnableWinType(hugeWinSprites);
        }
 
        // Phase 3: Mega Win (>= megaWinThreshold)
        if (multiplier >= megaWinThreshold && activePhase < 3)
        {
            activePhase = 3;
            EnableWinType(megaWinSprites);
        }
    }

    private void EnableWinType(List<Sprite> sprites)
    {
        if (winTypeObject == null || winTypeImageAnimation == null) return;

        if (!winTypeObject.activeSelf)
        {
            winTypeObject.SetActive(true);
        }

        int leftoverIndex = 0;
        if (winTypeImageAnimation.currentAnimationState == ImageAnimation.ImageState.PLAYING)
        {
            leftoverIndex = winTypeImageAnimation.indexOfTexture;
        }

        winTypeImageAnimation.StopAnimation();
        winTypeImageAnimation.textureArray = sprites;
        if (sprites != null && sprites.Count > 0)
        {
            winTypeImageAnimation.StartAnimationFromFrame(leftoverIndex);
        }
    }

    private void CompleteCounting()
    {
        isCounting = false;
        currentWinCount = finalWinAmount;
        if (winText != null)
        {
            winText.text = UIManager.FormatStaticValue(finalWinAmount);
        }
        Transform textT = GetWinTextObjectTransform();
        if (textT != null)
        {
            textT.DOKill();
            textT.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
        UpdatePhases(finalWinAmount);

        if (dollarImageAnimation != null)
        {
            dollarImageAnimation.exitLoopRange = true;
        }
    }

    private IEnumerator AutoCloseSequence()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        ClosePopup();
    }

    public void OnScreenClicked()
    {
        if (isCounting)
        {
            if (countCoroutine != null) StopCoroutine(countCoroutine);
            CompleteCounting();

            if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseSequence());
        }
        else
        {
            ClosePopup();
        }
    }

    private void ClosePopup()
    {
        if (AudioController.Instance != null) AudioController.Instance.StopWinTypeLoop();
        if (countCoroutine != null) StopCoroutine(countCoroutine);
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        if (winTypeImageAnimation != null)
        {
            winTypeImageAnimation.StopAnimation();
        }

        if (dollarImageAnimation != null)
        {
            dollarImageAnimation.StopAnimation();
        }

        if (winDisplayObject != null)
        {
            winDisplayObject.SetActive(false);
        }
        onCompleteCallback?.Invoke();
    }
}
