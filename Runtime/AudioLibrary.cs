using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library", order = 1)]
public class AudioLibrary : ScriptableObject
{
    [SerializeField] private List<Audio> audio;
    private Dictionary<string, Audio> audioClipDict;

    public List<Audio> AudioList => audio;

    public void Init()
    {
        audioClipDict = audio.ToDictionary(audio => audio.id, audio => audio);

    }
    public Audio GetAudioById(string id)
    {
        if (audioClipDict.TryGetValue(id, out Audio audio))
        {
            return audio;
        }
        Debug.LogWarning($"Audio with id {id} not found in AudioLibrary.");
        return null;
    }
}
