using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AudioSettingsWindow : EditorWindow
{
    private AudioLibrary targetLibrary;
    private Vector2 scrollPosition;
    private Vector2 clipsScrollPosition;
    private string searchText = "";
    private Audio selectedAudio;
    private int selectedIndex = -1;

    [MenuItem("Tools/Thaudio/Audio Settings", priority = 101)]
    public static void ShowWindow()
    {
        ShowWindow(null);
    }

    public static void ShowWindow(AudioLibrary library)
    {
        var window = GetWindow<AudioSettingsWindow>("Audio Settings");
        window.minSize = new Vector2(600, 400);
        window.targetLibrary = library;
        window.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (targetLibrary == null)
        {
            DrawNoLibrarySelected();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawAudioList();
        DrawAudioDetails();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Audio Library:", GUILayout.Width(90));

        AudioLibrary newLibrary = (AudioLibrary)EditorGUILayout.ObjectField(
            targetLibrary, typeof(AudioLibrary), false, GUILayout.Width(200));

        if (newLibrary != targetLibrary)
        {
            targetLibrary = newLibrary;
            selectedAudio = null;
            selectedIndex = -1;
        }

        GUILayout.FlexibleSpace();

        // Audio IDs Button
        if (GUILayout.Button("Audio IDs", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            AudioIDsWindow.ShowWindow();
        }

        GUILayout.Space(10);

        // Search
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawNoLibrarySelected()
    {
        EditorGUILayout.HelpBox("Please select an AudioLibrary from the dropdown menu or from the Project window.", MessageType.Info);

        if (GUILayout.Button("Create AudioLibrary", GUILayout.Height(30)))
        {
            CreateNewAudioLibrary();
        }
    }

    private void DrawAudioList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));

        EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Add New Audio"))
        {
            AddNewAudio();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        List<Audio> audioList = targetLibrary.AudioList;
        for (int i = 0; i < audioList.Count; i++)
        {
            Audio audio = audioList[i];

            // Filter
            if (!string.IsNullOrEmpty(searchText) &&
                !audio.id.ToLower().Contains(searchText.ToLower()))
            {
                continue;
            }

            bool isSelected = selectedIndex == i;

            EditorGUILayout.BeginHorizontal("box");

            if (GUILayout.Button(
                string.IsNullOrEmpty(audio.id) ? $"Audio {i}" : audio.id,
                isSelected ? EditorStyles.boldLabel : EditorStyles.label))
            {
                selectedAudio = audio;
                selectedIndex = i;
            }

            if (GUILayout.Button("x", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Audio",
                    $"Do you really want to delete '{audio.id}'?", "Yes", "No"))
                {
                    RemoveAudio(i);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawAudioDetails()
    {
        EditorGUILayout.BeginVertical();

        if (selectedAudio == null)
        {
            EditorGUILayout.HelpBox("Select an audio element from the list to edit it.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        // Make sure clips list is never null
        if (selectedAudio.clips == null)
        {
            selectedAudio.clips = new List<AudioClip>();
            EditorUtility.SetDirty(targetLibrary);
        }

        EditorGUILayout.LabelField("Audio Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ID
        EditorGUILayout.LabelField("ID", EditorStyles.miniBoldLabel);
        selectedAudio.id = EditorGUILayout.TextField(selectedAudio.id);
        EditorGUILayout.Space();

        // Clips List
        EditorGUILayout.LabelField($"Audio Clips ({selectedAudio.clips.Count})", EditorStyles.miniBoldLabel);

        if (selectedAudio.clips.Count > 1)
        {
            string playbackMode = selectedAudio.playAll ? "all clips simultaneously" :
                                 selectedAudio.playSequential ? "clips sequentially" :
                                 "a random clip";
            EditorGUILayout.HelpBox($"With multiple clips, {playbackMode} will be played.", MessageType.Info);
        }

        clipsScrollPosition = EditorGUILayout.BeginScrollView(clipsScrollPosition, GUILayout.Height(150));

        for (int i = 0; i < selectedAudio.clips.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");

            selectedAudio.clips[i] = (AudioClip)EditorGUILayout.ObjectField(
                $"Clip {i + 1}", selectedAudio.clips[i], typeof(AudioClip), false);

            if (GUILayout.Button("Play", GUILayout.Width(40)))
            {
                PlayPreviewClip(selectedAudio.clips[i]);
            }

            if (GUILayout.Button("x", GUILayout.Width(25)))
            {
                selectedAudio.clips.RemoveAt(i);
                EditorUtility.SetDirty(targetLibrary);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add Clip"))
        {
            selectedAudio.clips.Add(null);
            EditorUtility.SetDirty(targetLibrary);
        }

        EditorGUILayout.Space();

        // Playback Mode
        EditorGUILayout.LabelField("Playback Mode", EditorStyles.miniBoldLabel);

        bool newPlaySequential = EditorGUILayout.Toggle("Play Sequential", selectedAudio.playSequential);
        bool newPlayAll = EditorGUILayout.Toggle("Play All", selectedAudio.playAll);

        // Ensure only one mode is active at a time
        if (newPlaySequential != selectedAudio.playSequential && newPlaySequential)
        {
            selectedAudio.playSequential = true;
            selectedAudio.playAll = false;
        }
        else if (newPlayAll != selectedAudio.playAll && newPlayAll)
        {
            selectedAudio.playAll = true;
            selectedAudio.playSequential = false;
        }
        else if (!newPlaySequential && !newPlayAll)
        {
            selectedAudio.playSequential = false;
            selectedAudio.playAll = false;
        }

        // Show current mode
        string currentMode = selectedAudio.playAll ? "Play All (Simultaneous)" :
                            selectedAudio.playSequential ? "Play Sequential" :
                            "Play Random (Default)";
        EditorGUILayout.HelpBox($"Current Mode: {currentMode}", MessageType.None);

        EditorGUILayout.Space();

        // Volume
        EditorGUILayout.LabelField($"Volume: {selectedAudio.volume:F2}", EditorStyles.miniBoldLabel);
        selectedAudio.volume = EditorGUILayout.Slider(selectedAudio.volume, 0f, 1f);

        EditorGUILayout.LabelField($"Volume Variance: {selectedAudio.volumeVariance:F2}", EditorStyles.miniBoldLabel);
        selectedAudio.volumeVariance = EditorGUILayout.Slider(selectedAudio.volumeVariance, 0f, 1f);
        EditorGUILayout.Space();

        // Pitch
        EditorGUILayout.LabelField($"Pitch: {selectedAudio.pitch:F2}", EditorStyles.miniBoldLabel);
        selectedAudio.pitch = EditorGUILayout.Slider(selectedAudio.pitch, 0.1f, 3f);

        EditorGUILayout.LabelField($"Pitch Variance: {selectedAudio.pitchVariance:F2}", EditorStyles.miniBoldLabel);
        selectedAudio.pitchVariance = EditorGUILayout.Slider(selectedAudio.pitchVariance, 0f, 1f);
        EditorGUILayout.Space();

        // Loop
        selectedAudio.loop = EditorGUILayout.Toggle("Loop", selectedAudio.loop);
        EditorGUILayout.Space();

        // Preview (only Random and All modes)
        if (selectedAudio.clips.Count > 0 && selectedAudio.clips.Exists(c => c != null))
        {
            EditorGUILayout.BeginHorizontal();
            
            // Show preview based on mode, but only support Random and All in preview
            string previewMode = selectedAudio.playAll ? "Play All" : "Play Random";
            
            if (GUILayout.Button($"Play Preview ({previewMode})", GUILayout.Height(30)))
            {
                PlayPreview();
            }
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                StopPreview();
            }
            EditorGUILayout.EndHorizontal();
            
            if (selectedAudio.playSequential)
            {
                EditorGUILayout.HelpBox("Sequential mode preview plays as Random in editor.", MessageType.Info);
            }
        }

        EditorGUILayout.EndVertical();

        // Mark the asset as dirty
        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetLibrary);
        }
    }

    private void AddNewAudio()
    {
        Undo.RecordObject(targetLibrary, "Add Audio");

        Audio newAudio = new Audio
        {
            id = "",
            clips = new List<AudioClip>(),
            volume = 0.75f,
            volumeVariance = 0f,
            pitch = 1f,
            pitchVariance = 0f,
            loop = false,
            playSequential = false,
            playAll = false
        };

        targetLibrary.AudioList.Add(newAudio);
        selectedAudio = newAudio;
        selectedIndex = targetLibrary.AudioList.Count - 1;

        EditorUtility.SetDirty(targetLibrary);
    }

    private void RemoveAudio(int index)
    {
        Undo.RecordObject(targetLibrary, "Remove Audio");
        targetLibrary.AudioList.RemoveAt(index);

        if (selectedIndex == index)
        {
            selectedAudio = null;
            selectedIndex = -1;
        }
        else if (selectedIndex > index)
        {
            selectedIndex--;
        }

        EditorUtility.SetDirty(targetLibrary);
    }

    private void PlayPreview()
    {
        if (selectedAudio?.clips != null && selectedAudio.clips.Count > 0)
        {
            // Filter null clips
            var validClips = selectedAudio.clips.FindAll(c => c != null);
            if (validClips.Count == 0) return;

            StopPreview();

            if (selectedAudio.playAll)
            {
                // Play all clips
                for (int i = 0; i < validClips.Count; i++)
                {
                    PlayPreviewClipInternal(validClips[i], i);
                }
            }
            else
            {
                // Play random clip (works for both Random and Sequential modes in preview)
                AudioClip clipToPlay = validClips[Random.Range(0, validClips.Count)];
                PlayPreviewClipInternal(clipToPlay, 0);
            }
        }
    }

    private void PlayPreviewClip(AudioClip clip)
    {
        if (clip == null) return;
        StopPreview();
        PlayPreviewClipInternal(clip, 0);
    }

    private void PlayPreviewClipInternal(AudioClip clip, int index)
    {
        if (clip == null) return;

        // Calculate random variance
        float volumeWithVariance = selectedAudio.volume +
            Random.Range(-selectedAudio.volumeVariance, selectedAudio.volumeVariance);
        float pitchWithVariance = selectedAudio.pitch +
            Random.Range(-selectedAudio.pitchVariance, selectedAudio.pitchVariance);

        GameObject previewObj = GameObject.Find($"Audio Preview {index}");
        if (previewObj == null)
        {
            previewObj = EditorUtility.CreateGameObjectWithHideFlags(
                $"Audio Preview {index}", HideFlags.HideAndDontSave, typeof(AudioSource));
        }

        AudioSource previewSource = previewObj.GetComponent<AudioSource>();
        previewSource.clip = clip;
        previewSource.volume = Mathf.Clamp01(volumeWithVariance);
        previewSource.pitch = pitchWithVariance;
        previewSource.loop = selectedAudio.loop;
        previewSource.Play();

        Debug.Log($"Playing clip: {clip.name} (Volume: {previewSource.volume:F2}, Pitch: {previewSource.pitch:F2})");
    }

    private void StopPreview()
    {
        // Stop all preview objects
        for (int i = 0; i < 10; i++)
        {
            var previewObject = GameObject.Find($"Audio Preview {i}");
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
            }
        }
    }

    private void CreateNewAudioLibrary()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create AudioLibrary",
            "NewAudioLibrary",
            "asset",
            "Please choose a save location");

        if (!string.IsNullOrEmpty(path))
        {
            AudioLibrary newLibrary = CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(newLibrary, path);
            AssetDatabase.SaveAssets();
            targetLibrary = newLibrary;
            EditorGUIUtility.PingObject(newLibrary);
        }
    }

    private void OnDestroy()
    {
        StopPreview();
    }
}