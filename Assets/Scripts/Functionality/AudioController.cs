using UnityEngine;

public class AudioController : MonoBehaviour
{
  [SerializeField] private AudioSource bg_adudio;
  [SerializeField] internal AudioSource audioPlayer_wl;
  [SerializeField] internal AudioSource audioPlayer_button;
  [SerializeField] internal AudioSource audioSpin_button;
  [SerializeField] private AudioClip[] clips;
  [SerializeField] private AudioSource bg_audioBonus;
  [SerializeField] private AudioSource audioPlayer_Bonus;

  private void Start()
  {
    if (bg_adudio) bg_adudio.Play();
    audioPlayer_button.clip = clips[clips.Length - 1];
    audioSpin_button.clip = clips[clips.Length - 2];
  }

  internal void CheckFocusFunction(bool focus, bool IsSpinning)
  {
    if (!focus)
    {
      bg_adudio.Pause();
      audioPlayer_wl.Pause();
      audioPlayer_button.Pause();
    }
    else
    {
      if (!bg_adudio.mute) bg_adudio.UnPause();
      if (IsSpinning)
      {
        if (!audioPlayer_wl.mute) audioPlayer_wl.UnPause();
      }
      else
      {
        StopWLAaudio();
      }
      if (!audioPlayer_button.mute) audioPlayer_button.UnPause();
    }
  }

  internal void SwitchBGSound(bool isbonus)
  {
    if (isbonus)
    {
      if (bg_audioBonus) bg_audioBonus.enabled = true;
      if (bg_adudio) bg_adudio.enabled = false;
    }
    else
    {
      if (bg_audioBonus) bg_audioBonus.enabled = false;
      if (bg_adudio) bg_adudio.enabled = true;
    }
  }

  internal void PlayWLAudio(string type)
  {
    audioPlayer_wl.loop = false;
    int index = 0;
    switch (type)
    {
      case "bigwin":
        index = 0;
        break;
      case "win":
        index = 1;
        break;
      case "lose":
        index = 2;
        break;
      case "spinStop":
        index = 3;
        break;
      case "megaWin":
        index = 4;
        break;
      case "CollectCoin":
        index = 5;
        break;
      case "StartAudio":
        index = 6;
        break;
      case "Electric":
        index = 7;
        break;
    }
    StopWLAaudio();
    audioPlayer_wl.clip = clips[index];
    audioPlayer_wl.Play();
  }

  internal void PlayButtonAudio()
  {
    audioPlayer_button.Play();
  }

  internal void PlaySpinButtonAudio()
  {
    audioSpin_button.Play();
  }

  internal void StopWLAaudio()
  {
    audioPlayer_wl.Stop();
    audioPlayer_wl.loop = false;
  }

  internal void ToggleMute(bool toggle, string type)
  {
    switch (type)
    {
      case "music":
        bg_adudio.mute = toggle;
        bg_audioBonus.mute = toggle;
        break;
      case "sound":
        audioPlayer_button.mute = toggle;
        audioPlayer_wl.mute = toggle;
        audioSpin_button.mute = toggle;
        audioPlayer_Bonus.mute = toggle;
        break;
    }
  }

  internal void OnVolumeChanged(float value, string type)
  {
    switch (type)
    {
      case "music":
        bg_adudio.volume = value;
        bg_audioBonus.volume = value;
        break;
      case "sound":
        audioPlayer_button.volume = value;
        audioPlayer_wl.volume = value;
        audioSpin_button.volume = value;
        audioPlayer_Bonus.volume = value;
        break;
    }
  }
}
