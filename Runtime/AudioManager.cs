using UnityEngine.Audio;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
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

    [HideInInspector]
    public AudioSource musicAudioSource;
    [HideInInspector]
    public AudioSource sfxAudioSource;

    private float minVolume = -80f;
    private Coroutine sfxFadeCoroutine;
    private Coroutine musicFadeCoroutine;

    // Audio Source Pooling
    private Dictionary<AudioMixerGroup, List<AudioSource>> audioSourcePools = new Dictionary<AudioMixerGroup, List<AudioSource>>();
    private Dictionary<AudioMixerGroup, List<Audio>> mixerGroupSounds = new Dictionary<AudioMixerGroup, List<Audio>>();


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

    public void IncreaseVolume(float volume)
    {
        _audioMixer.GetFloat(masterVolExposed, out float currentVolume);
        _audioMixer.SetFloat(masterVolExposed, currentVolume + volume);
    }

    public void DecreaseVolume(float volume)
    {
        _audioMixer.GetFloat(masterVolExposed, out float currentVolume);
        _audioMixer.SetFloat(masterVolExposed, currentVolume - volume);
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

    public void StopSfx(string id)
    {
        var audio = sfxLibrary.GetAudioById(id);
        audio?.Stop();
    }

    public void PlayMusic(string id)
    {
        musicLibrary.GetAudioById(id)?.Play();
    }

    public void FadeIn(AudioSource audioSource, float time, float toVolume = 1f, bool replay = true)
    {
        StartCoroutine(FadeInC(audioSource, time, toVolume, replay));
    }


    public void FadeOut(AudioSource audioSource, float time, float toVolume = 0f, bool replay = true)
    {
        StartCoroutine(FadeOutC(audioSource, time, toVolume, replay));
    }

    private IEnumerator FadeInC(AudioSource audioSource, float duration, float targetVolume, bool replay)
    {
        if (replay) audioSource.Play();

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOutC(AudioSource audioSource, float duration, float targetVolume, bool stop)
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
        if (stop) audioSource.Stop();
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

    public void FadeInSFX(float time)
    {
        float toVolume = PlayerPrefs.GetFloat(sfxVolExposed, 0);
        FadeInExposedVolume(time, toVolume, sfxVolExposed, sfxFadeCoroutine);
    }

    public void FadeOutSFX(float time)
    {
        FadeOutExposedVolume(time, sfxVolExposed, sfxFadeCoroutine);
    }

    public void FadeInMusic(float time)
    {
        float toVolume = PlayerPrefs.GetFloat(musicVolExposed, 0);
        FadeInExposedVolume(time, toVolume, musicVolExposed, musicFadeCoroutine);
    }
    public void FadeOutMusic(float time)
    {
        FadeOutExposedVolume(time, musicVolExposed, musicFadeCoroutine);
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
}

