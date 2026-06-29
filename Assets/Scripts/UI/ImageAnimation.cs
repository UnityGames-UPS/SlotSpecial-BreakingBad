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

    
    internal static ImageAnimation Instance;

    
    [SerializeField] internal List<Sprite> textureArray = new List<Sprite>();
    [SerializeField] internal Image rendererDelegate;
    [SerializeField] internal bool useSharedMaterial = true;
    [SerializeField] internal bool doLoopAnimation = true;

    [Header("Dynamic Timing")]
    [SerializeField] internal bool useDynamicFramerate = false;
    [SerializeField] internal float dynamicLoopDuration = 2.0f;

    [Header("Startup")]
    [SerializeField] private bool StartOnAwake = false;
    [SerializeField] private bool StartonEnable = false;

    [Header("Speed / Timing")]
    [SerializeField] internal float AnimationSpeed = 5f;
    [SerializeField] internal float delayBetweenLoop = 0f;

    
    [HideInInspector] [SerializeField] internal ImageState currentAnimationState;

    [Header("Loop Range Settings")]
    [SerializeField] internal bool useLoopRange = false;
    [SerializeField] internal int loopRangeStart = 0;
    [SerializeField] internal int loopRangeEnd = 0;
    [SerializeField] internal bool exitLoopRange = false;
    [SerializeField] internal bool stopAtLastFrameOnEnd = false;

    
    internal bool isAnim = false;

    
    internal System.Action<int> onLoopComplete;

    
    internal System.Action<int> onFrameChanged;

    
    internal int indexOfTexture;
    private float delayBetweenAnimation;
    private int currentLoopCount;

    private const float IdealFrameRate = 0.0416666679f;

    
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

    
    public void PauseAnimation()
    {
        if (currentAnimationState == ImageState.PLAYING)
        {
            CancelInvoke(nameof(AnimationProcess));
            currentAnimationState = ImageState.PAUSED;
        }
    }

    
    public void ResumeAnimation()
    {
        if (currentAnimationState == ImageState.PAUSED && !IsInvoking(nameof(AnimationProcess)))
        {
            Invoke(nameof(AnimationProcess), delayBetweenAnimation);
            currentAnimationState = ImageState.PLAYING;
        }
    }

    
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

    
    public void RevertToInitialState()
    {
        indexOfTexture = 0;
        SetTextureOfIndex();
    }

    
    private void SetTextureOfIndex()
    {
        if (textureArray == null || textureArray.Count == 0) return;
        if (indexOfTexture < 0 || indexOfTexture >= textureArray.Count) return;
        if (rendererDelegate == null) return;

        rendererDelegate.sprite = textureArray[indexOfTexture];
    }
}