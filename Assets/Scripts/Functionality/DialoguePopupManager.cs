using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public enum VideoScenario
{
    EarlyReveal = 0,
    MagnetHit = 1,
    FreeSpinStart = 2,
    FreeSpinEnd = 3,
    LinkFeatureStart = 4,
    LinkFeatureEnd = 5
}

[System.Serializable]
public struct VideoScenarioData
{
    [SerializeField] internal string name;
    [SerializeField] internal VideoClip videoClip;
    [SerializeField] internal AudioClip audioClip;
}

public enum DialogueType
{
    GameStart = 0,
    FreeSpinHit_1_5 = 1,
    FreeSpinHit_5_8 = 2,
    FreeSpinHit_Above_8 = 3,
    FreeSpinEnd_NoWin = 4,
    FreeSpinEnd_SmallWin_1 = 5,
    FreeSpinEnd_SmallWin_2 = 6,
    FreeSpinEnd_AverageWin_1 = 7,
    FreeSpinEnd_AverageWin_2 = 8,
    FreeSpinEnd_HugeWin = 9,
    CashCollectTrigger = 10,
    NearMiss_1 = 11,
    NearMiss_2 = 12,
    MagnetAppearance = 13,
    SpecialSymbolNoTrigger = 14,
    TooManySymbolsAndCashCollect = 15,
    LinkFeatureTrigger = 16,
    MegaLinkFeatureTrigger = 17
}

[System.Serializable]
public struct DialogueData
{
    [SerializeField] internal string name;
    [SerializeField] internal GameObject popupObject;
    [SerializeField] internal AudioClip audioClip;
}

public class DialoguePopupManager : MonoBehaviour
{
    internal static DialoguePopupManager Instance { get; private set; }

    internal bool IsDialogueActive => currentActiveDialogue != null || (dialogueParent != null && dialogueParent.activeSelf);

    [Header("UI Parent Configurations")]
    [SerializeField] private GameObject dialogueParent;

    [Header("Audio Configurations")]
    [SerializeField] private AudioSource dialogueAudioSource;

    [Header("Dialogue Lists")]
    [SerializeField] private DialogueData[] dialogueList;

    [Header("Video Configurations")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoDisplayPanel;
    [SerializeField] private VideoScenarioData[] videoScenarios;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private bool forceUrlPlayback = false;
    [SerializeField] private string videoUrlPrefix = "https://d1lerod2freygq.cloudfront.net/SL-BB/StreamingAssets/";

    [Header("Chances (%)")]
    [Range(0f, 100f)] [SerializeField] private float chanceGameStart = 100f;
    [Range(0f, 100f)] [SerializeField] private float chanceFreeSpinHit = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceMiniJackpot = 80f;
    [Range(0f, 100f)] [SerializeField] private float chanceFreeSpinEndNoWin = 100f;
    [Range(0f, 100f)] [SerializeField] private float chanceFreeSpinEndSmallWin = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceFreeSpinEndAverageWin = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceFreeSpinEndHugeWin = 95f;
    [Range(0f, 100f)] [SerializeField] private float chanceDiamondSlotEnd = 95f;
    [Range(0f, 100f)] [SerializeField] private float chanceCashCollectTrigger = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceNearMiss = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceMagnetAppearance = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceSpecialSymbolNoTrigger = 80f;
    [Range(0f, 100f)] [SerializeField] private float chanceTooManySymbolsAndCashCollect = 100f;
    [Range(0f, 100f)] [SerializeField] private float chanceLinkFeatureTrigger = 90f;
    [Range(0f, 100f)] [SerializeField] private float chanceMegaLinkFeatureTrigger = 90f;

    [Header("Duration Settings")]
    [SerializeField] private float minDuration = 1.0f;
    [SerializeField] private float maxDuration = 1.5f;

    [Header("Win Multiplier Thresholds")]
    [SerializeField] private float smallWinMaxMultiplier = 5f;
    [SerializeField] private float averageWinMaxMultiplier = 25f;

    private bool isDialogueSkipped = false;
    private GameObject currentActiveDialogue = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = GetComponent<AudioSource>();
            if (dialogueAudioSource == null)
            {
                dialogueAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (dialogueParent != null)
        {
            dialogueParent.SetActive(false);
            UnityEngine.UI.Button button = dialogueParent.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                button = dialogueParent.AddComponent<UnityEngine.UI.Button>();
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SkipDialogue);
        }
    }

    private void Start()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (slotManager == null) slotManager = FindFirstObjectByType<SlotManager>();
        ApplySFXVolume();
    }

    private void SkipDialogue()
    {
        if (currentActiveDialogue != null)
        {
            isDialogueSkipped = true;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        int numTypes = System.Enum.GetNames(typeof(DialogueType)).Length;
        if (dialogueList == null || dialogueList.Length != numTypes)
        {
            System.Array.Resize(ref dialogueList, numTypes);
        }

        for (int i = 0; i < numTypes; i++)
        {
            dialogueList[i].name = ((DialogueType)i).ToString();
        }

        int numVideoTypes = System.Enum.GetNames(typeof(VideoScenario)).Length;
        if (videoScenarios == null || videoScenarios.Length != numVideoTypes)
        {
            System.Array.Resize(ref videoScenarios, numVideoTypes);
        }

        for (int i = 0; i < numVideoTypes; i++)
        {
            videoScenarios[i].name = ((VideoScenario)i).ToString();
        }
    }
#endif

    private bool RollDice(float chancePercent)
    {
        return UnityEngine.Random.Range(0f, 100f) < chancePercent;
    }

    private void ApplySFXVolume()
    {
        if (AudioController.Instance != null)
        {
            UpdateVolume(AudioController.Instance.SfxVolume);
        }
    }

    internal void UpdateVolume(float val)
    {
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.volume = val;
        }
        if (videoPlayer != null)
        {
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume(i, val);
            }
        }
    }

    public IEnumerator PlayDialogue(DialogueType type)
    {
        int index = (int)type;
        if (index < 0 || index >= dialogueList.Length) yield break;

        DialogueData data = dialogueList[index];
        if (data.popupObject == null)
        {
            Debug.LogWarning($"[DialoguePopupManager] Dialogue {type} popupObject is not assigned.");
            yield break;
        }

        if (uiManager != null && uiManager.AutoSpinStopButton != null)
        {
            uiManager.AutoSpinStopButton.interactable = false;
        }

        

        
        if (dialogueParent != null)
        {
            dialogueParent.SetActive(true);
        }
        data.popupObject.SetActive(true);

        
        if (data.audioClip != null && dialogueAudioSource != null)
        {
            ApplySFXVolume();
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = data.audioClip;
            dialogueAudioSource.Play();
        }

        
        float duration = (data.audioClip != null) ? data.audioClip.length : UnityEngine.Random.Range(minDuration, maxDuration);
        float elapsed = 0f;
        isDialogueSkipped = false;
        currentActiveDialogue = data.popupObject;
        while (elapsed < duration && !isDialogueSkipped)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentActiveDialogue = null;

        
        data.popupObject.SetActive(false);
        if (dialogueParent != null)
        {
            dialogueParent.SetActive(false);
        }

        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }

        if (uiManager != null)
        {
            uiManager.UpdateButtonsState();
        }
    }

    

    public IEnumerator PlayGameStartDialogue()
    {
        if (RollDice(chanceGameStart))
        {
            yield return PlayDialogue(DialogueType.GameStart);
        }
    }

    public IEnumerator PlayFreeSpinHitDialogue(int freeSpinsCount)
    {
        if (!RollDice(chanceFreeSpinHit)) yield break;

        DialogueType type;
        if (freeSpinsCount <= 5)
        {
            type = DialogueType.FreeSpinHit_1_5;
        }
        else if (freeSpinsCount <= 8)
        {
            type = DialogueType.FreeSpinHit_5_8;
        }
        else
        {
            type = DialogueType.FreeSpinHit_Above_8;
        }

        yield return PlayDialogue(type);
    }

    public IEnumerator PlayMiniJackpotDialogue()
    {
        if (!RollDice(chanceMiniJackpot)) yield break;

        DialogueType[] options = new DialogueType[]
        {
            DialogueType.FreeSpinHit_1_5,
            DialogueType.FreeSpinHit_5_8,
            DialogueType.FreeSpinHit_Above_8
        };
        DialogueType chosen = options[UnityEngine.Random.Range(0, options.Length)];

        yield return PlayDialogue(chosen);
    }

    public IEnumerator PlayFreeSpinEndDialogue(double winAmt, double totalBet)
    {
        if (winAmt == 0)
        {
            if (RollDice(chanceFreeSpinEndNoWin))
            {
                yield return PlayDialogue(DialogueType.FreeSpinEnd_NoWin);
            }
        }
        else
        {
            double multiplier = winAmt / totalBet;
            if (multiplier < smallWinMaxMultiplier)
            {
                if (RollDice(chanceFreeSpinEndSmallWin))
                {
                    DialogueType chosen = UnityEngine.Random.Range(0, 2) == 0 ? DialogueType.FreeSpinEnd_SmallWin_1 : DialogueType.FreeSpinEnd_SmallWin_2;
                    yield return PlayDialogue(chosen);
                }
            }
            else if (multiplier < averageWinMaxMultiplier)
            {
                if (RollDice(chanceFreeSpinEndAverageWin))
                {
                    DialogueType chosen = UnityEngine.Random.Range(0, 2) == 0 ? DialogueType.FreeSpinEnd_AverageWin_1 : DialogueType.FreeSpinEnd_AverageWin_2;
                    yield return PlayDialogue(chosen);
                }
            }
            else
            {
                if (RollDice(chanceFreeSpinEndHugeWin))
                {
                    yield return PlayDialogue(DialogueType.FreeSpinEnd_HugeWin);
                }
            }
        }
    }

    public IEnumerator PlayDiamondSlotEndDialogue(double winAmt, double totalBet)
    {
        if (!RollDice(chanceDiamondSlotEnd)) yield break;

        double multiplier = winAmt / totalBet;
        if (multiplier < smallWinMaxMultiplier)
        {
            DialogueType chosen = UnityEngine.Random.Range(0, 2) == 0 ? DialogueType.FreeSpinEnd_SmallWin_1 : DialogueType.FreeSpinEnd_SmallWin_2;
            yield return PlayDialogue(chosen);
        }
        else
        {
            DialogueType chosen = UnityEngine.Random.Range(0, 2) == 0 ? DialogueType.FreeSpinEnd_AverageWin_1 : DialogueType.FreeSpinEnd_AverageWin_2;
            yield return PlayDialogue(chosen);
        }
    }

    public IEnumerator PlayCashCollectTriggerDialogue()
    {
        if (RollDice(chanceCashCollectTrigger))
        {
            yield return PlayDialogue(DialogueType.CashCollectTrigger);
        }
    }

    public IEnumerator PlayNearMissDialogue()
    {
        if (!RollDice(chanceNearMiss)) yield break;

        DialogueType chosen = UnityEngine.Random.Range(0, 2) == 0 ? DialogueType.NearMiss_1 : DialogueType.NearMiss_2;
        yield return PlayDialogue(chosen);
    }

    public IEnumerator PlayMagnetAppearanceDialogue()
    {
        if (RollDice(chanceMagnetAppearance))
        {
            yield return PlayDialogue(DialogueType.MagnetAppearance);
        }
    }

    public IEnumerator PlaySpecialSymbolNoTriggerDialogue()
    {
        if (RollDice(chanceSpecialSymbolNoTrigger))
        {
            yield return PlayDialogue(DialogueType.SpecialSymbolNoTrigger);
        }
    }

    public IEnumerator PlayTooManySymbolsAndCashCollectDialogue()
    {
        if (RollDice(chanceTooManySymbolsAndCashCollect))
        {
            yield return PlayDialogue(DialogueType.TooManySymbolsAndCashCollect);
        }
    }

    public IEnumerator PlayLinkFeatureTriggerDialogue()
    {
        if (RollDice(chanceLinkFeatureTrigger))
        {
            yield return PlayDialogue(DialogueType.LinkFeatureTrigger);
        }
    }

    public IEnumerator PlayMegaLinkFeatureTriggerDialogue()
    {
        if (RollDice(chanceMegaLinkFeatureTrigger))
        {
            yield return PlayDialogue(DialogueType.MegaLinkFeatureTrigger);
        }
    }

    public IEnumerator PlayVideoScenario(VideoScenario scenario)
    {
        int index = (int)scenario;
        if (videoScenarios == null || index < 0 || index >= videoScenarios.Length) yield break;

        VideoScenarioData data = videoScenarios[index];
        if (data.videoClip == null)
        {
            Debug.LogWarning($"[DialoguePopupManager] Video clip for scenario {scenario} is not assigned.");
            yield break;
        }

        if (uiManager != null)
        {
            uiManager.SetVideoPlaybackState(true, scenario);
        }

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.SetActive(true);
        }

        if (videoPlayer != null)
        {
            bool playFromUrl = forceUrlPlayback;
#if UNITY_WEBGL && !UNITY_EDITOR
            playFromUrl = true;
#endif
            if (playFromUrl)
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = videoUrlPrefix + data.videoClip.name + ".mp4";
            }
            else
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = data.videoClip;
            }
            videoPlayer.Play();
        }

        if (data.audioClip != null && dialogueAudioSource != null)
        {
            ApplySFXVolume();
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = data.audioClip;
            dialogueAudioSource.Play();
        }

        float duration = (float)data.videoClip.length;
        yield return new WaitForSeconds(duration);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.SetActive(false);
        }

        if (uiManager != null)
        {
            uiManager.SetVideoPlaybackState(false, scenario);
        }
    }
}
