using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgSource;      // Music channel
    [SerializeField] private AudioSource sfxSource;     // SFX channel
    [SerializeField] private AudioSource spareSource;   // Spare/looping SFX channel

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clipMainBg;
    [SerializeField] private AudioClip clipBonusBg;
    [SerializeField] private AudioClip clipSpinBtn;
    [SerializeField] private AudioClip clipNormalBtn;
    [SerializeField] private AudioClip clipTurboToggle;
    [SerializeField] private AudioClip clipCashCoinLand;
    [SerializeField] private AudioClip clipLinkLand;
    [SerializeField] private AudioClip clipCashCollectLand;
    [SerializeField] private AudioClip clipSlotStop;
    [SerializeField] private AudioClip clipMagnetOn;
    [SerializeField] private AudioClip clipSingleTension;
    [SerializeField] private AudioClip clipMultiTension;
    [SerializeField] private AudioClip clipTrailStart;
    [SerializeField] private AudioClip clipWinTypeLoop;
    [SerializeField] private AudioClip clipWinLine;

    private float musicVolume = 0.7f;
    private float sfxVolume = 0.7f;

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("music_volume", musicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("sfx_volume", sfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load volume settings from PlayerPrefs
        musicVolume = PlayerPrefs.GetFloat("music_volume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("sfx_volume", 0.7f);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgSource != null) bgSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (spareSource != null) spareSource.volume = sfxVolume;
    }

    // --- BG MUSIC ---
    public void PlayMainBg()
    {
        PlayBgClip(clipMainBg);
    }

    public void PlayBonusBg()
    {
        PlayBgClip(clipBonusBg);
    }

    private void PlayBgClip(AudioClip clip)
    {
        if (bgSource == null || clip == null) return;
        if (bgSource.isPlaying && bgSource.clip == clip) return;
        bgSource.clip = clip;
        bgSource.loop = true;
        bgSource.Play();
    }

    public void StopBg()
    {
        if (bgSource != null) bgSource.Stop();
    }

    // --- SFX ONE SHOTS ---
    public void PlaySpinBtn()
    {
        PlaySfxOneShot(clipSpinBtn);
    }

    public void PlayNormalBtn()
    {
        PlaySfxOneShot(clipNormalBtn);
    }

    public void PlayTurboToggle()
    {
        PlaySfxOneShot(clipTurboToggle);
    }

    public void PlayCashCoinLand()
    {
        PlaySfxOneShot(clipCashCoinLand);
    }

    public void PlayLinkLand()
    {
        PlaySfxOneShot(clipLinkLand);
    }

    public void PlayCashCollectLand()
    {
        PlaySfxOneShot(clipCashCollectLand);
    }

    public void PlaySlotStop()
    {
        PlaySfxOneShot(clipSlotStop);
    }

    public void PlayMagnetOn()
    {
        PlaySpareLoop(clipMagnetOn);
    }

    public void StopMagnetOn()
    {
        StopSpareLoop(clipMagnetOn);
    }

    public void PlayTrailStart()
    {
        PlaySfxOneShot(clipTrailStart);
    }

    public void PlayWinLine()
    {
        PlaySfxOneShot(clipWinLine);
    }

    // --- TENSION BUILDERS & LOOPS ---
    public void PlaySingleTension()
    {
        PlaySpareLoop(clipSingleTension);
    }

    public void StopSingleTension()
    {
        StopSpareLoop(clipSingleTension);
    }

    public void PlayMultiTension()
    {
        PlaySpareLoop(clipMultiTension);
    }

    public void StopMultiTension()
    {
        StopSpareLoop(clipMultiTension);
    }

    public void PlayWinTypeLoop()
    {
        PlaySpareLoop(clipWinTypeLoop);
    }

    public void StopWinTypeLoop()
    {
        StopSpareLoop(clipWinTypeLoop);
    }

    private void PlaySfxOneShot(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    private void PlaySpareLoop(AudioClip clip)
    {
        if (spareSource == null || clip == null) return;
        if (spareSource.isPlaying && spareSource.clip == clip) return;
        spareSource.clip = clip;
        spareSource.loop = true;
        spareSource.Play();
    }

    private void StopSpareLoop(AudioClip expectedClip)
    {
        if (spareSource != null && spareSource.clip == expectedClip)
        {
            spareSource.Stop();
            spareSource.clip = null;
            spareSource.loop = false;
        }
    }

    // --- APPLICATION FOCUS ---
    private void OnApplicationFocus(bool hasFocus)
    {
        HandleFocus(hasFocus);
    }

    private void OnApplicationPause(bool isPaused)
    {
        HandleFocus(!isPaused);
    }

    private void HandleFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (bgSource != null) bgSource.Pause();
            if (spareSource != null) spareSource.Pause();
            AudioListener.volume = 0f;
        }
        else
        {
            if (bgSource != null) bgSource.UnPause();
            if (spareSource != null) spareSource.UnPause();
            AudioListener.volume = 1f;
        }
    }
}
