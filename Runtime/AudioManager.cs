using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    #region Variables
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<AudioManager>();
            return _instance;
        }
    }

    public readonly string masterVolExposed = "MasterVol";
    public readonly string musicVolExposed = "MusicVol";
    public readonly string sfxVolExposed = "SfxVol";

    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioLibrary sfxLibrary;

    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioLibrary musicLibrary;

    // Can be used to play quick OneShots
    [HideInInspector]
    public AudioSource musicAudioSource;
    [HideInInspector]
    public AudioSource sfxAudioSource;

    private float minVolume = -80f;
    private Coroutine sfxAudioGroupFadeCoroutine;
    private Coroutine musicAudioGroupFadeCoroutine;
    private Coroutine beatMatchingtransitionCoroutine;

    // Audio Source Pooling
    private Dictionary<AudioMixerGroup, List<AudioSource>> audioSourcePools = new Dictionary<AudioMixerGroup, List<AudioSource>>();
    private Dictionary<AudioMixerGroup, List<Audio>> mixerGroupSounds = new Dictionary<AudioMixerGroup, List<Audio>>();
    private HashSet<AudioSource> reservedSources = new HashSet<AudioSource>();

    #endregion

    #region Setup and utils
    protected void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.outputAudioMixerGroup = musicMixerGroup;
        musicLibrary.Init();
        AddSoundToMixerGroup(musicMixerGroup, musicLibrary.AudioList);

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxLibrary.Init();
        AddSoundToMixerGroup(sfxMixerGroup, sfxLibrary.AudioList);
    }

    private void Start()
    {
        LoadPlayerPrefs();
    }

    public void MuteAll() { _audioMixer.SetFloat(masterVolExposed, minVolume); }
    public void UnmuteAll() { _audioMixer.SetFloat(masterVolExposed, PlayerPrefs.GetFloat(masterVolExposed, 0)); }

    public void IncreaseVolume(string exposedVol, float volume)
    {
        _audioMixer.GetFloat(exposedVol, out float currentVolume);
        _audioMixer.SetFloat(exposedVol, currentVolume + volume);
    }

    public void DecreaseVolume(string exposedVol, float volume)
    {
        _audioMixer.GetFloat(exposedVol, out float currentVolume);
        _audioMixer.SetFloat(exposedVol, currentVolume - volume);
    }

    private void LoadPlayerPrefs()
    {
        //Set volume if Playerprefs are set, else use mixersettings
        float masterVol = PlayerPrefs.GetFloat(masterVolExposed, -100);
        float musicVol = PlayerPrefs.GetFloat(musicVolExposed, -100);
        float sfxVol = PlayerPrefs.GetFloat(sfxVolExposed, -100);
        if (masterVol >= -80) _audioMixer.SetFloat(masterVolExposed, masterVol);
        if (musicVol >= -80) { musicMixerGroup.audioMixer.SetFloat(musicVolExposed, musicVol); }
        if (sfxVol >= -80) { sfxMixerGroup.audioMixer.SetFloat(sfxVolExposed, sfxVol); }
    }

    private void AddSoundToMixerGroup(AudioMixerGroup audioMixerGroup, List<Audio> sounds)
    {
        if (!audioSourcePools.ContainsKey(audioMixerGroup))
        {
            audioSourcePools[audioMixerGroup] = new List<AudioSource>();
        }

        mixerGroupSounds[audioMixerGroup] = sounds;

        // Initial
        CreatePooledAudioSource(audioMixerGroup);

        // Set Callback
        foreach (Audio audio in sounds)
        {
            audio.SetAudioSourceProvider(() => GetAvailableAudioSource(audioMixerGroup, audio));
        }
    }

    private AudioSource CreatePooledAudioSource(AudioMixerGroup audioMixerGroup)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = audioMixerGroup;
        source.playOnAwake = false;
        audioSourcePools[audioMixerGroup].Add(source);
        return source;
    }

    private AudioSource GetAvailableAudioSource(AudioMixerGroup audioMixerGroup, Audio requestingAudio)
    {
        List<AudioSource> pool = audioSourcePools[audioMixerGroup];
        AudioSource availableSource = null;

        foreach (AudioSource source in pool)
        {
            if (!source.isPlaying && !reservedSources.Contains(source))
            {
                availableSource = source;
                break;
            }
        }

        if (availableSource == null)
        {
            availableSource = CreatePooledAudioSource(audioMixerGroup);
        }

        //reserve the source, so that it won't be used by another audio
        if (requestingAudio.reuseSource)
        {
            reservedSources.Add(availableSource);
        }
        return availableSource;
    }
    public void SetStartingPoint(string id, float time)
    {
        var audio = musicLibrary.GetAudioById(id);
        if (audio == null) { audio = sfxLibrary.GetAudioById(id); }
        if (audio == null) return;
        audio.SetAudioPos(time);
    }

    #endregion

    #region Sfx
    public void PlaySfx(string id, float delay = 0f)
    {
        var audio = sfxLibrary.GetAudioById(id);

        if (delay <= 0f)
        {
            audio?.Play();
            return;
        }

        StartCoroutine(PlaySfxWithDelayC(audio, delay));
    }

    private IEnumerator PlaySfxWithDelayC(Audio audio, float delay)
    {
        yield return new WaitForSeconds(delay);
        audio?.Play();
    }

    public void StopSfx(string id) { sfxLibrary.GetAudioById(id)?.Stop(); }
    public void MuteSFXGlobal() { sfxMixerGroup.audioMixer.SetFloat(sfxVolExposed, minVolume); }
    public void UnmuteSFXGlobal() { sfxMixerGroup.audioMixer.SetFloat(sfxVolExposed, PlayerPrefs.GetFloat(sfxVolExposed, 0)); }
    public void FadeInSFX(string id, float time, float volume = 1, bool restartAudio = true)
    {
        sfxLibrary.GetAudioById(id)?.FadeIn(this, time, volume, restartAudio);
    }

    /// <summary>
    /// Be carful with playing multiple SFX and fading them out, 
    /// because currently only the last audioSource of the given ID will be faded out
    /// </summary>
    public void FadeOutSFX(string id, float time, float toVolume = 0f, bool stopAfterFadeOut = true)
    {
        sfxLibrary.GetAudioById(id)?.FadeOut(this, time, toVolume, stopAfterFadeOut);
    }

    #endregion

    #region Music

    public void PlayMusic(string id) { musicLibrary.GetAudioById(id)?.Play(); }
    public void StopMusic(string id) { musicLibrary.GetAudioById(id)?.Stop(); }
    public void MuteMusicGlobal() { musicMixerGroup.audioMixer.SetFloat(musicVolExposed, minVolume); }
    public void UnmuteMusicGlobal() { musicMixerGroup.audioMixer.SetFloat(musicVolExposed, PlayerPrefs.GetFloat(musicVolExposed, 0)); }

    public void FadeInMusic(string id, float time, float volume = 1, bool restartAudio = true)
    {
        musicLibrary.GetAudioById(id)?.FadeIn(this, time, volume, restartAudio);
    }

    public void FadeOutMusic(string id, float time, float toVolume = 0f, bool stopAfterFadeOut = true)
    {
        musicLibrary.GetAudioById(id)?.FadeOut(this, time, toVolume, stopAfterFadeOut);
    }

    public void BeatMatchingMusicSwitch(string currentTrack, string newTrack, float fadeDuration = 0.5f)
    {
        Audio currentAudio = musicLibrary.GetAudioById(currentTrack);
        Audio newAudio = musicLibrary.GetAudioById(newTrack);
        if (currentAudio == null || newAudio == null) return;

        if (beatMatchingtransitionCoroutine != null) { StopCoroutine(beatMatchingtransitionCoroutine); }
        beatMatchingtransitionCoroutine = StartCoroutine(BeatMatchingTransition(currentAudio, newAudio, fadeDuration));
    }

    private IEnumerator BeatMatchingTransition(Audio currentTrack, Audio newTrack, float fadeDuration)
    {
        if (currentTrack.IsPlaying())
        {
            float timeToWait = currentTrack.GetTimeToNextBar();

            // Wait until the end of the beat
            float delayBeforeFadeOut = timeToWait - fadeDuration;
            if (delayBeforeFadeOut > 0)
            {
                yield return new WaitForSeconds(delayBeforeFadeOut);
            }

            currentTrack.FadeOut(this, fadeDuration, 0f, true);

            // wait for fade out
            if (delayBeforeFadeOut > 0)
            {
                yield return new WaitForSeconds(fadeDuration);
            }
            else
            {
                // fallback
                yield return new WaitForSeconds(timeToWait);
            }
        }

        newTrack.FadeIn(this, fadeDuration, newTrack.volume, true);

        beatMatchingtransitionCoroutine = null;
    }

    public void SubscribeToMusicPlayAction(string id, Action action) { musicLibrary.GetAudioById(id).OnStartPlay += action; }
    public void UnsubscribeFromMusicPlayAction(string id, Action action) { musicLibrary.GetAudioById(id).OnStartPlay -= action; }
    public void SubscribeToMusicStopAction(string id, Action action) { musicLibrary.GetAudioById(id).OnStopPlay += action; }
    public void UnsubscribeFromMusicStopAction(string id, Action action) { musicLibrary.GetAudioById(id).OnStopPlay -= action; }
    public float GetCurrentPos(string id) { return musicLibrary.GetAudioById(id)?.GetAudioPos() ?? 0f; }


    #endregion

    #region MixerGroupFade
    public void FadeInMusicAudioGroup(float time)
    {
        float toVolume = PlayerPrefs.GetFloat(musicVolExposed, 0);
        FadeInExposedVolume(time, toVolume, musicVolExposed, musicAudioGroupFadeCoroutine);
    }
    public void FadeOutMusicMusicGroup(float time)
    {
        FadeOutExposedVolume(time, musicVolExposed, musicAudioGroupFadeCoroutine);
    }

    public void FadeInSFXAudioGroup(float time)
    {
        float toVolume = PlayerPrefs.GetFloat(sfxVolExposed, 0);
        FadeInExposedVolume(time, toVolume, sfxVolExposed, sfxAudioGroupFadeCoroutine);
    }

    public void FadeOutSFXAudioGroup(float time)
    {
        FadeOutExposedVolume(time, sfxVolExposed, sfxAudioGroupFadeCoroutine);
    }

    private void FadeInExposedVolume(float time, float toVolume, string exposedParam, Coroutine coroutine)
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(FadeInMixerGroup(exposedParam, time, toVolume));
    }

    private void FadeOutExposedVolume(float time, string exposedParam, Coroutine coroutine)
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(FadeOutMixerGroup(exposedParam, time));
    }

    private IEnumerator FadeOutMixerGroup(string exposedName, float time)
    {
        float elapsedTime = 0;
        _audioMixer.GetFloat(exposedName, out float currentVol);
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            _audioMixer.SetFloat(exposedName, Mathf.Lerp(currentVol, minVolume, elapsedTime / time));
            yield return null;
        }

    }

    private IEnumerator FadeInMixerGroup(string exposedName, float time, float toVolume)
    {
        float elapsedTime = 0;
        _audioMixer.GetFloat(exposedName, out float currentVol);
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            _audioMixer.SetFloat(exposedName, Mathf.Lerp(currentVol, toVolume, elapsedTime / time));
            yield return null;
        }

    }
    #endregion

}

