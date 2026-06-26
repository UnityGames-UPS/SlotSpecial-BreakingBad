using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageAnimation : MonoBehaviour
{
    public enum ImageState
    {
        NONE,
        PLAYING,
        PAUSED
    }

    // ─── Singleton (legacy compatibility) ───────────────────────────
    public static ImageAnimation Instance;

    // ─── Inspector fields ───────────────────────────────────────────
    public List<Sprite> textureArray = new List<Sprite>();
    public Image rendererDelegate;
    public bool useSharedMaterial = true;
    public bool doLoopAnimation = true;

    [Header("Dynamic Timing")]
    public bool useDynamicFramerate = false;
    public float dynamicLoopDuration = 2.0f;

    [Header("Startup")]
    [SerializeField] private bool StartOnAwake = false;
    [SerializeField] private bool StartonEnable = false;

    [Header("Speed / Timing")]
    public float AnimationSpeed = 5f;
    public float delayBetweenLoop = 0f;

    // ─── State ──────────────────────────────────────────────────────
    [HideInInspector] public ImageState currentAnimationState;

    [Header("Loop Range Settings")]
    public bool useLoopRange = false;
    public int loopRangeStart = 0;
    public int loopRangeEnd = 0;
    public bool exitLoopRange = false;
    public bool stopAtLastFrameOnEnd = false;

    /// <summary>True when this slot has win-animation sprites loaded (set by SlotBehaviour).</summary>
    internal bool isAnim = false;

    /// <summary>Fires every time a full loop completes. Passes the running loop count.</summary>
    public System.Action<int> onLoopComplete;

    /// <summary>Fires every time the frame index changes. Passes the current frame index.</summary>
    public System.Action<int> onFrameChanged;

    // ─── Private runtime ────────────────────────────────────────────
    internal int indexOfTexture;
    private float delayBetweenAnimation;
    private int currentLoopCount;

    private const float IdealFrameRate = 0.0416666679f;

    // ─── Unity lifecycle ────────────────────────────────────────────
    private void OnValidate()
    {
        if (rendererDelegate == null)
            rendererDelegate = GetComponent<Image>();
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (rendererDelegate == null)
            rendererDelegate = GetComponent<Image>();

        if (StartOnAwake)
            StartAnimation();
    }

    private void OnEnable()
    {
        if (StartonEnable)
            StartAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    // ─── Core loop ──────────────────────────────────────────────────
    private void AnimationProcess()
    {
        SetTextureOfIndex();
        onFrameChanged?.Invoke(indexOfTexture);
        indexOfTexture++;

        if (useLoopRange && !exitLoopRange)
        {
            if (indexOfTexture > loopRangeEnd)
            {
                indexOfTexture = loopRangeStart;
                currentLoopCount++;
                onLoopComplete?.Invoke(currentLoopCount);
                Invoke(nameof(AnimationProcess), delayBetweenAnimation + delayBetweenLoop);
                return;
            }
        }

        if (indexOfTexture >= textureArray.Count)
        {
            if (stopAtLastFrameOnEnd)
            {
                indexOfTexture = textureArray.Count - 1;
                currentAnimationState = ImageState.NONE;
                return;
            }

            indexOfTexture = 0;
            currentLoopCount++;
            onLoopComplete?.Invoke(currentLoopCount);

            if (doLoopAnimation)
                Invoke(nameof(AnimationProcess), delayBetweenAnimation + delayBetweenLoop);
        }
        else
        {
            Invoke(nameof(AnimationProcess), delayBetweenAnimation);
        }
    }

    // ─── Public API ─────────────────────────────────────────────────

    /// <summary>
    /// Start (or restart) the animation from frame 0.
    /// Safe to call even if already playing — cancels and restarts cleanly.
    /// </summary>
    public void StartAnimation()
    {
        if (textureArray == null || textureArray.Count == 0) return;

        CancelInvoke(nameof(AnimationProcess));
        indexOfTexture = 0;
        currentLoopCount = 0;

        currentAnimationState = ImageState.NONE;

        if (currentAnimationState == ImageState.NONE)
        {
            RevertToInitialState();
            if (useDynamicFramerate && textureArray != null && textureArray.Count > 0)
            {
                AnimationSpeed = (float)textureArray.Count / dynamicLoopDuration;
                delayBetweenAnimation = 1f / AnimationSpeed;
            }
            else
            {
                delayBetweenAnimation = IdealFrameRate * textureArray.Count / AnimationSpeed;
            }
            currentAnimationState = ImageState.PLAYING;
            Invoke(nameof(AnimationProcess), delayBetweenAnimation);
        }
    }

    /// <summary>
    /// Start (or restart) the animation from a specific frame index.
    /// </summary>
    public void StartAnimationFromFrame(int frameIndex)
    {
        if (textureArray == null || textureArray.Count == 0) return;

        CancelInvoke(nameof(AnimationProcess));
        indexOfTexture = frameIndex % textureArray.Count;
        currentLoopCount = 0;

        currentAnimationState = ImageState.NONE;

        if (currentAnimationState == ImageState.NONE)
        {
            SetTextureOfIndex();
            if (useDynamicFramerate && textureArray != null && textureArray.Count > 0)
            {
                AnimationSpeed = (float)textureArray.Count / dynamicLoopDuration;
                delayBetweenAnimation = 1f / AnimationSpeed;
            }
            else
            {
                delayBetweenAnimation = IdealFrameRate * textureArray.Count / AnimationSpeed;
            }
            currentAnimationState = ImageState.PLAYING;
            Invoke(nameof(AnimationProcess), delayBetweenAnimation);
        }
    }

    /// <summary>Pause mid-sequence. Resume with ResumeAnimation().</summary>
    public void PauseAnimation()
    {
        if (currentAnimationState == ImageState.PLAYING)
        {
            CancelInvoke(nameof(AnimationProcess));
            currentAnimationState = ImageState.PAUSED;
        }
    }

    /// <summary>Resume from where it was paused.</summary>
    public void ResumeAnimation()
    {
        if (currentAnimationState == ImageState.PAUSED && !IsInvoking(nameof(AnimationProcess)))
        {
            Invoke(nameof(AnimationProcess), delayBetweenAnimation);
            currentAnimationState = ImageState.PLAYING;
        }
    }

    /// <summary>Stop the animation and reset sprite to frame 0.</summary>
    public void StopAnimation()
    {
        if (currentAnimationState != ImageState.NONE)
        {
            CancelInvoke(nameof(AnimationProcess));
            currentAnimationState = ImageState.NONE;
            currentLoopCount = 0;

            if (textureArray != null && textureArray.Count > 0 && rendererDelegate != null)
                rendererDelegate.sprite = textureArray[0];
        }
    }

    /// <summary>Reset displayed sprite to frame 0 without changing playback state.</summary>
    public void RevertToInitialState()
    {
        indexOfTexture = 0;
        SetTextureOfIndex();
    }

    // ─── Private helpers ────────────────────────────────────────────
    private void SetTextureOfIndex()
    {
        if (textureArray == null || textureArray.Count == 0) return;
        if (indexOfTexture < 0 || indexOfTexture >= textureArray.Count) return;
        if (rendererDelegate == null) return;

        rendererDelegate.sprite = textureArray[indexOfTexture];
    }
}