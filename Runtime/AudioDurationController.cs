using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDurationController
{
    private class DurationPlayback
    {
        public MonoBehaviour runner;
        public Coroutine coroutine;
        public AudioSource activeSource;
        public List<AudioSource> additionalSources;
    }

    private readonly List<DurationPlayback> activePlaybacks = new List<DurationPlayback>();
    private readonly Stack<List<AudioSource>> captureStack = new Stack<List<AudioSource>>();
    private readonly Action onStopPlay;

    public AudioDurationController(Action onStopPlay = null)
    {
        this.onStopPlay = onStopPlay;
    }

    public void BeginCapture()
    {
        captureStack.Push(new List<AudioSource>());
    }

    public void Capture(AudioSource source)
    {
        if (captureStack.Count > 0)
        {
            captureStack.Peek().Add(source);
        }
    }

    public List<AudioSource> EndCapture()
    {
        if (captureStack.Count == 0) return new List<AudioSource>();
        return captureStack.Pop();
    }

    public void PlayForDuration(MonoBehaviour runner, float duration, List<AudioSource> startedSources, bool stopExisting)
    {
        if (runner == null || startedSources == null || startedSources.Count == 0) return;

        if (stopExisting)
        {
            StopAll(runner);
        }

        DurationPlayback playback = new DurationPlayback
        {
            runner = runner,
            activeSource = startedSources[0],
            additionalSources = new List<AudioSource>()
        };

        for (int i = 1; i < startedSources.Count; i++)
        {
            playback.additionalSources.Add(startedSources[i]);
        }

        playback.coroutine = runner.StartCoroutine(StopAfterDuration(duration, playback));
        activePlaybacks.Add(playback);
    }

    public void StopAll(MonoBehaviour runner)
    {
        foreach (var playback in activePlaybacks)
        {
            if (playback.coroutine != null && runner != null)
            {
                runner.StopCoroutine(playback.coroutine);
            }
            StopPlaybackSources(playback);
        }
        activePlaybacks.Clear();
    }

    public void ClearAll()
    {
        foreach (var playback in activePlaybacks)
        {
            if (playback.coroutine != null && playback.runner != null)
            {
                playback.runner.StopCoroutine(playback.coroutine);
            }
        }
        activePlaybacks.Clear();
    }

    private IEnumerator StopAfterDuration(float duration, DurationPlayback playback)
    {
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        if (activePlaybacks.Contains(playback))
        {
            StopPlaybackSources(playback);
            activePlaybacks.Remove(playback);
        }
    }

    private void StopPlaybackSources(DurationPlayback playback)
    {
        if (playback == null) return;

        if (playback.activeSource != null)
        {
            playback.activeSource.Stop();
            onStopPlay?.Invoke();
        }

        foreach (var source in playback.additionalSources)
        {
            source?.Stop();
        }
    }
}
