using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace Thaudio.Editor
{
    public class AudioIDMigrationTool : EditorWindow
    {
        [MenuItem("Tools/Thaudio/Migration/Import from Legacy IDs", priority = 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioIDMigrationTool>("Import Legacy IDs");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Import Legacy Audio IDs", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool helps migrate from old static ID classes (UISfxIDs, MusicIDs, GameSfxIDs) " +
                "to the new ScriptableObject-based system.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Import UISfxIDs", GUILayout.Height(30)))
            {
                ImportLegacyType("UISfxIDs", "UI Sounds");
            }

            if (GUILayout.Button("Import MusicIDs", GUILayout.Height(30)))
            {
                ImportLegacyType("MusicIDs", "Music");
            }

            if (GUILayout.Button("Import GameSfxIDs", GUILayout.Height(30)))
            {
                ImportLegacyType("GameSfxIDs", "Game SFX");
            }
        }

        private void ImportLegacyType(string typeName, string categoryName)
        {
            Type legacyType = Type.GetType(typeName);

            if (legacyType == null)
            {
                EditorUtility.DisplayDialog("Type Not Found",
                    $"Could not find type '{typeName}'. Make sure it exists in your project.",
                    "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {categoryName} Category",
                categoryName.Replace(" ", ""),
                "asset",
                $"Choose location for {categoryName} category");

            if (string.IsNullOrEmpty(path))
                return;

            var category = CreateInstance<AudioIDCategory>();
            category.CategoryName = categoryName;

            var fields = legacyType.GetFields(BindingFlags.Public | BindingFlags.Static);
            int importedCount = 0;

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    string value = (string)field.GetValue(null);
                    category.AddEntry(field.Name, value);
                    importedCount++;
                }
            }

            AssetDatabase.CreateAsset(category, path);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Import Complete",
                $"Successfully imported {importedCount} IDs from {typeName}!",
                "OK");

            Selection.activeObject = category;
        }
    }
}
