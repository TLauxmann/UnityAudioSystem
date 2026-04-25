using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class Audio
{
    public string id;
    public Action OnStartPlay;
    public Action OnStopPlay;

    public List<AudioClip> clips = new List<AudioClip>();

    public bool reuseSource = false;
    public int bpm;
    public int beatsPerBar = 4;

    [Range(0f, 1f)]
    public float volume = .75f;
    [Range(0f, 1f)]
    public float volumeVariance = 0f;

    [Range(.1f, 3f)]
    public float pitch = 1f;
    [Range(0f, 1f)]
    public float pitchVariance = 0f;

    public bool loop = false;

    [Header("Playback Mode")]
    public bool playSequential = false;
    public bool playAll = false;

    private List<AudioSource> additionalSources = new List<AudioSource>();
    private Func<AudioSource> audioSourceProvider;
    private int currentClipIndex = 0;
    private bool singleClipMode = false;
    private float startingPos = 0f;
    private Coroutine activeFadeCoroutine;

    private AudioSource activeAudioSource;

    private AudioSource audioSource
    {
        get
        {
            if (!reuseSource || activeAudioSource == null)
            {
                activeAudioSource = audioSourceProvider.Invoke();
                activeAudioSource.clip = clips[0];
            }
            return activeAudioSource;
        }
    }

    public void SetAudioSourceProvider(Func<AudioSource> provider)
    {
        audioSourceProvider = provider;
    }

    public void Play()
    {
        if (singleClipMode)
        {
            PlayClipOnSource(clips[0]);
        }
        else if (playAll)
        {
            PlayAll();
        }
        else if (playSequential)
        {
            PlaySequential();
        }
        else
        {
            PlayRandom();
        }
    }

    private void PlayRandom()
    {
        AudioClip clipToPlay = clips[Random.Range(0, clips.Count)];
        PlayClipOnSource(clipToPlay);
    }

    private void PlaySequential()
    {
        AudioClip clipToPlay = clips[currentClipIndex];
        PlayClipOnSource(clipToPlay);
        currentClipIndex = (currentClipIndex + 1) % clips.Count;
    }

    private void PlayAll()
    {
        if (clips[0] != null)
        {
            PlayClipOnSource(clips[0]);
        }

        for (int i = 1; i < clips.Count; i++)
        {
            AudioSource additionalSource = audioSourceProvider.Invoke();
            additionalSources.Add(additionalSource);
            PlayClipOnSource(additionalSource, clips[i]);
        }
    }

    private void PlayClipOnSource(AudioClip clip)
    {
        PlayClipOnSource(audioSource, clip);
    }

    private void PlayClipOnSource(AudioSource aSource, AudioClip clip)
    {
        aSource.clip = clip;
        SetAudioSettings(aSource);
        aSource.Play();
        OnStartPlay?.Invoke();
    }

    private void SetAudioSettings(AudioSource aSource)
    {
        aSource.loop = loop;
        aSource.volume = volume + Random.Range(-volumeVariance, volumeVariance);
        aSource.pitch = pitch + Random.Range(-pitchVariance, pitchVariance);
        aSource.time = startingPos;
        startingPos = 0; // reset starting position after applying it to the clip
    }

    public void Stop()
    {
        if (activeAudioSource != null && clips.Contains(activeAudioSource.clip))
        {
            activeAudioSource.Stop();
            OnStopPlay?.Invoke();
        }

        foreach (var additionalSource in additionalSources)
        {
            if (additionalSource != null && clips.Contains(additionalSource.clip))
            {
                additionalSource.Stop();
            }
        }
    }

    public void ResetSequentialIndex() { currentClipIndex = 0; }
    public void SetAudioPos(float pos) { startingPos = pos; }
    public float GetAudioPos() { return activeAudioSource != null ? activeAudioSource.time : 0f; }
    public bool IsPlaying() { return activeAudioSource != null && activeAudioSource.isPlaying; }

    public void FadeIn(MonoBehaviour runner, float time, float toVolume = 1f, bool restartAudio = true)
    {
        Debug.Log($"Audio: Fading in audio {id} over {time} seconds to volume {toVolume}. Restart audio: {restartAudio}");
        if (restartAudio)
        {
            Play();
            activeAudioSource.volume = 0f;
        }

        StartFade(runner, time, toVolume, false);
    }

    public void FadeOut(MonoBehaviour runner, float time, float toVolume, bool stopAfterFadeOut = true)
    {
        if (activeAudioSource == null || !activeAudioSource.isPlaying) return;
        StartFade(runner, time, toVolume, stopAfterFadeOut);
    }

    private void StartFade(MonoBehaviour runner, float duration, float targetVolume, bool stopAudioAtEnd)
    {
        Debug.Log($"Audio: Starting fade on audio {id} to volume {targetVolume} over {duration} seconds. Stop audio at end: {stopAudioAtEnd}");
        if (activeFadeCoroutine != null) { runner.StopCoroutine(activeFadeCoroutine); }
        activeFadeCoroutine = runner.StartCoroutine(FadeAudioCoroutine(duration, targetVolume, stopAudioAtEnd));
    }

    private IEnumerator FadeAudioCoroutine(float duration, float targetVol, bool stopAudioAtEnd)
    {
        if (activeAudioSource == null) yield break;

        float startVol = activeAudioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            if (activeAudioSource == null) yield break;

            activeAudioSource.volume = Mathf.Lerp(startVol, targetVol, t);
            yield return null;
        }

        if (activeAudioSource != null)
        {
            activeAudioSource.volume = targetVol;
            if (stopAudioAtEnd) { activeAudioSource.Stop(); 
            Debug.Log($"Audio: Fade completed on audio {id}. Audio stopped: {stopAudioAtEnd}");
            }
        }

        activeFadeCoroutine = null;
    }

    public float GetCurrentPlaybackTime()
    {
        if (activeAudioSource != null && activeAudioSource.isPlaying)
        {
            return activeAudioSource.time;
        }
        return 0f;
    }

    public float GetTimeToNextBar()
    {
        if (bpm <= 0f) return 0f;

        float currentPosition = GetCurrentPlaybackTime();
        float beatDuration = 60f / bpm;
        float barDuration = beatDuration * beatsPerBar;

        float timeInCurrentBar = currentPosition % barDuration;
        return barDuration - timeInCurrentBar;
    }

}
