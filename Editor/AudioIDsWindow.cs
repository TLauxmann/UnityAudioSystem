using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AudioIDsWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string searchText = "";
    private Dictionary<System.Type, bool> sectionFoldouts = new Dictionary<System.Type, bool>();

    [MenuItem("Tools/Thaudio/Show Audio IDs", priority = 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioIDsWindow>("Audio IDs");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        // Initialize Foldouts
        sectionFoldouts[typeof(UISfxIDs)] = true;
        sectionFoldouts[typeof(MusicIDs)] = true;
        sectionFoldouts[typeof(GameSfxIDs)] = true;
    }

    private void OnGUI()
    {
        GUILayout.Label("Audio ID Reference", EditorStyles.boldLabel);

        // Search bar
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // SfxIDs Section
        DrawSection("UI Sound Effects (UISfxIDs)", typeof(UISfxIDs));

        // MusicIDs Section
        DrawSection("Music (MusicIDs)", typeof(MusicIDs));

        // GameSfxIDs Section
        DrawSection("Game Sound Effects (GameSfxIDs)", typeof(GameSfxIDs));

        GUILayout.EndScrollView();
    }

    private void DrawSection(string sectionName, System.Type type)
    {
        GUILayout.Space(5);

        // Foldout Header with Add/Remove Buttons
        EditorGUILayout.BeginHorizontal("box");

        if (!sectionFoldouts.ContainsKey(type))
            sectionFoldouts[type] = true;

        sectionFoldouts[type] = EditorGUILayout.Foldout(sectionFoldouts[type], sectionName, true, EditorStyles.foldoutHeader);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            ShowAddIDDialog(type);
        }

        EditorGUILayout.EndHorizontal();

        if (!sectionFoldouts[type])
            return;

        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (fields.Length == 0)
        {
            EditorGUILayout.HelpBox("No IDs available. Click '+' to add an ID.", MessageType.Info);
            return;
        }

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(string))
            {
                var value = (string)field.GetValue(null);

                // Filter by search
                if (!string.IsNullOrEmpty(searchText) &&
                   !field.Name.ToLower().Contains(searchText.ToLower()) &&
               !value.ToLower().Contains(searchText.ToLower()))
                {
                    continue;
                }

                GUILayout.BeginHorizontal("box");

                // Field name
                GUILayout.Label(field.Name, GUILayout.Width(150));

                // Value (ID string)
                var style = new GUIStyle(EditorStyles.textField);
                style.normal.textColor = new Color(0.7f, 0.9f, 1f);
                EditorGUILayout.SelectableLabel(value, style, GUILayout.Height(18));

                if (GUILayout.Button("Copy ID", GUILayout.Width(60)))
                {
                    GUIUtility.systemCopyBuffer = value;
                    Debug.Log($"Copied ID: {value}");
                }

                // Delete button
                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete ID",
                         $"Do you really want to delete '{field.Name}'?", "Yes", "No"))
                    {
                        RemoveID(type, field.Name);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }
    }

    private void ShowAddIDDialog(System.Type type)
    {
        var window = GetWindow<AddAudioIDWindow>(true, "Add New Audio ID", true);
        window.minSize = new Vector2(400, 150);
        window.maxSize = new Vector2(400, 150);
        window.parentWindow = this;
        window.targetType = type;
        window.Show();
    }

    public void AddID(System.Type type, string fieldName, string idValue)
    {
        string filePath = GetFilePathForType(type);

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError($"File for {type.Name} not found!");
            return;
        }

        // Read the file
        string[] lines = File.ReadAllLines(filePath);
        List<string> newLines = new List<string>();

        bool added = false;
        for (int i = 0; i < lines.Length; i++)
        {
            newLines.Add(lines[i]);

            // Add the new ID before the closing brace
            if (lines[i].Trim() == "}" && i == lines.Length - 1 && !added)
            {
                // Remove the last line (closing brace)
                newLines.RemoveAt(newLines.Count - 1);

                // Add the new ID
                newLines.Add($"    public const string {fieldName} = \"{idValue}\";");

                // Add the closing brace back
                newLines.Add("}");
                added = true;
            }
        }

        // Write the file back
        File.WriteAllLines(filePath, newLines);
        AssetDatabase.Refresh();

        Debug.Log($"ID '{fieldName}' has been added to {type.Name}!");
    }

    private void RemoveID(System.Type type, string fieldName)
    {
        string filePath = GetFilePathForType(type);

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError($"File for {type.Name} not found!");
            return;
        }

        // Read the file
        string[] lines = File.ReadAllLines(filePath);
        List<string> newLines = new List<string>();

        foreach (string line in lines)
        {
            // Skip the line with the ID to be deleted
            if (line.Contains($"public const string {fieldName} ="))
            {
                continue;
            }
            newLines.Add(line);
        }

        // Write the file back
        File.WriteAllLines(filePath, newLines);
        AssetDatabase.Refresh();

        Debug.Log($"ID '{fieldName}' has been removed from {type.Name}!");
    }

    private string GetFilePathForType(System.Type type)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Script {type.Name}");

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return Path.GetFullPath(path);
        }

        return null;
    }
}

// Separate Window for adding IDs
public class AddAudioIDWindow : EditorWindow
{
    public AudioIDsWindow parentWindow;
    public System.Type targetType;

    private string fieldName = "";
    private string idValue = "";

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField($"Add New ID for {targetType.Name}", EditorStyles.boldLabel);

        GUILayout.Space(10);

        // Field Name (C# Property Name)
        EditorGUILayout.LabelField("Field Name (e.g. ButtonClick):");
        fieldName = EditorGUILayout.TextField(fieldName);

        GUILayout.Space(5);

        // ID Value (String Value)
        EditorGUILayout.LabelField("ID Value (e.g. button-click):");
        idValue = EditorGUILayout.TextField(idValue);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
        {
            Close();
        }

        GUI.enabled = !string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(idValue);

        if (GUILayout.Button("Add", GUILayout.Width(100)))
        {
            parentWindow.AddID(targetType, fieldName.Trim(), idValue.Trim());
            Close();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }
}
