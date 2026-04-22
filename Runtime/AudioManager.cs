using UnityEngine.Audio;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    private Dictionary<AudioSource, Coroutine> fadeCoroutines = new Dictionary<AudioSource, Coroutine>();

    // Audio Source Pooling
    private Dictionary<AudioMixerGroup, List<AudioSource>> audioSourcePools = new Dictionary<AudioMixerGroup, List<AudioSource>>();
    private Dictionary<AudioMixerGroup, List<Audio>> mixerGroupSounds = new Dictionary<AudioMixerGroup, List<Audio>>();
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

    public void MuteAll() { _audioMixer.SetFloat(masterVolExposed, minVolume);}
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
        if (masterVol >= -80) musicMixerGroup.audioMixer.SetFloat(masterVolExposed, masterVol);
        if (musicVol >= -80)
        {
            musicMixerGroup.audioMixer.SetFloat(musicVolExposed, musicVol);
        }
        if (sfxVol >= -80)
        {
            musicMixerGroup.audioMixer.SetFloat(sfxVolExposed, sfxVol);
        }
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
            audio.SetAudioSourceProvider(() => GetAvailableAudioSource(audioMixerGroup));
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

    private AudioSource GetAvailableAudioSource(AudioMixerGroup audioMixerGroup)
    {
        List<AudioSource> pool = audioSourcePools[audioMixerGroup];

        foreach (AudioSource source in pool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return CreatePooledAudioSource(audioMixerGroup);
    }

    #endregion

    #region Sfx
    public void PlaySfx(string id, float delay = 0f, bool reuseSource = false)
    {
        var audio = sfxLibrary.GetAudioById(id);

        if (delay <= 0f)
        {
            audio?.Play(reuseSource);
            return;
        }

        StartCoroutine(PlaySfxWithDelayC(audio, delay, reuseSource));
    }

    private IEnumerator PlaySfxWithDelayC(Audio audio, float delay, bool reuseSource)
    {
        yield return new WaitForSeconds(delay);
        audio?.Play(reuseSource);
    }

    public void StopSfx(string id) { sfxLibrary.GetAudioById(id)?.Stop(); }
    public void MuteSFXGlobal() { sfxMixerGroup.audioMixer.SetFloat(sfxVolExposed, minVolume); }
    public void UnmuteSFXGlobal() { sfxMixerGroup.audioMixer.SetFloat(sfxVolExposed, PlayerPrefs.GetFloat(sfxVolExposed, 0)); }

    public void FadeInSFX(string id, float time, float volume = -1, bool restartAudio = true)
    {
        var audio = sfxLibrary.GetAudioById(id);
        if (audio == null) return;
        volume = volume == -1 ? PlayerPrefs.GetFloat(sfxVolExposed, 0) : volume;
        FadeIn(audio.audioSource, time, volume, restartAudio);
    }

    public void FadeOutSFX(string id, float time, float toVolume = 0f, bool stopAfterFadeOut = true)
    {
        var audio = sfxLibrary.GetAudioById(id);
        if (audio == null) return;
        FadeOut(audio.audioSource, time, toVolume, stopAfterFadeOut);
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


    #endregion

    #region Music
    public void PlayMusic(string id, bool reuseSource = false) { musicLibrary.GetAudioById(id)?.Play(reuseSource); }
    public void StopMusic(string id) { musicLibrary.GetAudioById(id)?.Stop(); }
    public void MuteMusicGlobal() { musicMixerGroup.audioMixer.SetFloat(musicVolExposed, minVolume); }
    public void UnmuteMusicGlobal() { musicMixerGroup.audioMixer.SetFloat(musicVolExposed, PlayerPrefs.GetFloat(musicVolExposed, 0)); }

    public void FadeInMusic(string id, float time, float volume = -1, bool restartAudio = true)
    {
        var audio = musicLibrary.GetAudioById(id);
        if (audio == null) return;
        volume = volume == -1 ? PlayerPrefs.GetFloat(musicVolExposed, 0) : volume;
        FadeIn(audio.audioSource, time, volume, restartAudio);
    }

    public void FadeOutMusic(string id, float time, float toVolume = 0f, bool stopAfterFadeOut = true)
    {
        var audio = musicLibrary.GetAudioById(id);
        if (audio == null) return;
        FadeOut(audio.audioSource, time, toVolume, stopAfterFadeOut);
    }

    public void FadeInMusicAudioGroup(float time)
    {
        float toVolume = PlayerPrefs.GetFloat(musicVolExposed, 0);
        FadeInExposedVolume(time, toVolume, musicVolExposed, musicAudioGroupFadeCoroutine);
    }
    public void FadeOutMusicMusicGroup(float time)
    {
        FadeOutExposedVolume(time, musicVolExposed, musicAudioGroupFadeCoroutine);
    }
    #endregion

    #region Fade In/Out
    public void FadeIn(AudioSource audioSource, float time, float toVolume = 1f, bool restartAudio = true)
    {
        StopRunningFade(audioSource);
        Coroutine courotine = StartCoroutine(FadeInC(audioSource, time, toVolume, restartAudio));
        fadeCoroutines.Add(audioSource, courotine);
    }

    public void FadeOut(AudioSource audioSource, float time, float toVolume = 0f, bool stopAfterFadeOut = true)
    {
        StopRunningFade(audioSource);
        Coroutine courotine = StartCoroutine(FadeOutC(audioSource, time, toVolume, stopAfterFadeOut));
        fadeCoroutines.Add(audioSource, courotine);
    }

    private IEnumerator FadeInC(AudioSource audioSource, float duration, float targetVolume, bool restartAudio)
    {
        if (restartAudio) audioSource.Play();

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutines.Remove(audioSource);
    }

    private IEnumerator FadeOutC(AudioSource audioSource, float duration, float targetVolume, bool stopAfterFadeOut)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        if (stopAfterFadeOut) audioSource.Stop();
        fadeCoroutines.Remove(audioSource);
    }

    private void StopRunningFade(AudioSource audioSource)
    {
        //Check if there's already a fade coroutine running for this audio source and stop it
        if (fadeCoroutines.TryGetValue(audioSource, out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
            {
                StopCoroutine(existingCoroutine);
                fadeCoroutines.Remove(audioSource);
            }
        }
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

