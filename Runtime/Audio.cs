using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class Audio
{
    public string id;

    public List<AudioClip> clips = new List<AudioClip>();

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

    private AudioSource audioSource;
    private List<AudioSource> additionalSources = new List<AudioSource>();
    private Func<AudioSource> audioSourceProvider;
    private int currentClipIndex = 0;
    private bool singleClipMode = false;

    public void SetAudioSourceProvider(Func<AudioSource> provider)
    {
        audioSourceProvider = provider;
    }

    public void Play()
    {
        audioSource = audioSourceProvider.Invoke();
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

    private void PlayClipOnSource(AudioSource audioSource, AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = volume + Random.Range(-volumeVariance, volumeVariance);
        audioSource.pitch = pitch + Random.Range(-pitchVariance, pitchVariance);
        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null && clips.Contains(audioSource.clip))
        {
            audioSource.Stop();
        }

        foreach (var additionalSource in additionalSources)
        {
            if (additionalSource != null && clips.Contains(additionalSource.clip))
            {
                additionalSource.Stop();
            }
        }
    }

    public void ResetSequentialIndex()
    {
        currentClipIndex = 0;
    }

}
