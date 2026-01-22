using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Thaudio.Editor
{
    public class AudioIDsWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchText = "";
        private Dictionary<AudioIDCategory, bool> categoryFoldouts = new Dictionary<AudioIDCategory, bool>();
        private AudioIDCategory[] allCategories;

        [MenuItem("Tools/Thaudio/Show Audio IDs", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioIDsWindow>("Audio IDs");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadAllCategories();
        }

        private void OnFocus()
        {
            LoadAllCategories();
        }

        private void LoadAllCategories()
        {
            allCategories = AudioIDCodeGenerator.FindAllCategories();
            
            foreach (var category in allCategories)
            {
                if (!categoryFoldouts.ContainsKey(category))
                {
                    categoryFoldouts[category] = true;
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (allCategories == null || allCategories.Length == 0)
            {
                DrawEmptyState();
            }
            else
            {
                foreach (var category in allCategories)
                {
                    if (category != null)
                    {
                        DrawCategorySection(category);
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Audio ID Reference", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate Code", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                AudioIDCodeGenerator.GenerateAllCategories();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadAllCategories();
                Repaint();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            GUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.Space(50);
            EditorGUILayout.HelpBox(
                "No Audio ID Categories found!\n\n" +
                "Create one via:\n" +
                "Right-click in Project ? Create ? Thaudio ? Audio ID Category",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create Audio ID Category", GUILayout.Height(30)))
            {
                CreateNewCategory();
            }
        }

        private void DrawCategorySection(AudioIDCategory category)
        {
            GUILayout.Space(5);

            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = category.CategoryColor;

            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal();

            if (!categoryFoldouts.ContainsKey(category))
                categoryFoldouts[category] = true;

            categoryFoldouts[category] = EditorGUILayout.Foldout(
                categoryFoldouts[category],
                $"{category.CategoryName} ({category.Entries.Count} IDs)",
                true,
                EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                ShowAddIDDialog(category);
            }

            if (GUILayout.Button("?", GUILayout.Width(25)))
            {
                Selection.activeObject = category;
                EditorGUIUtility.PingObject(category);
            }

            EditorGUILayout.EndHorizontal();

            if (categoryFoldouts[category])
            {
                DrawCategoryEntries(category);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryEntries(AudioIDCategory category)
        {
            if (category.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No IDs available. Click '+' to add an ID.", MessageType.Info);
                return;
            }

            var filteredEntries = category.Entries.Where(entry =>
            {
                if (string.IsNullOrEmpty(searchText))
                    return true;

                string search = searchText.ToLower();
                return entry.FieldName.ToLower().Contains(search) ||
                       entry.IDValue.ToLower().Contains(search);
            }).ToList();

            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No matching IDs found.", MessageType.Info);
                return;
            }

            foreach (var entry in filteredEntries)
            {
                DrawIDEntry(category, entry);
            }
        }

        private void DrawIDEntry(AudioIDCategory category, AudioIDEntry entry)
        {
            GUILayout.BeginHorizontal("box");

            GUILayout.Label(entry.FieldName, GUILayout.Width(180));

            var style = new GUIStyle(EditorStyles.textField);
            style.normal.textColor = new Color(0.7f, 0.9f, 1f);
            EditorGUILayout.SelectableLabel(entry.IDValue, style, GUILayout.Height(18));

            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = entry.IDValue;
                Debug.Log($"Copied: {entry.IDValue}");
            }

            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete ID",
                    $"Delete '{entry.FieldName}' from {category.CategoryName}?",
                    "Delete", "Cancel"))
                {
                    category.RemoveEntry(entry.FieldName);
                    Repaint();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void ShowAddIDDialog(AudioIDCategory category)
        {
            var window = GetWindow<AddAudioIDWindow>(true, "Add New Audio ID", true);
            window.minSize = new Vector2(400, 150);
            window.maxSize = new Vector2(400, 150);
            window.parentWindow = this;
            window.targetCategory = category;
            window.Show();
        }

        private void CreateNewCategory()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Audio ID Category",
                "NewAudioCategory",
                "asset",
                "Choose a location for the new Audio ID Category");

            if (!string.IsNullOrEmpty(path))
            {
                var category = CreateInstance<AudioIDCategory>();
                AssetDatabase.CreateAsset(category, path);
                AssetDatabase.SaveAssets();
                LoadAllCategories();
                Selection.activeObject = category;
            }
        }
    }

    public class AddAudioIDWindow : EditorWindow
    {
        public AudioIDsWindow parentWindow;
        public AudioIDCategory targetCategory;

        private string fieldName = "";
        private string idValue = "";

        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField($"Add New ID to: {targetCategory.CategoryName}", EditorStyles.boldLabel);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Field Name (e.g. ButtonClick):");
            fieldName = EditorGUILayout.TextField(fieldName);

            GUILayout.Space(5);

            EditorGUILayout.LabelField("ID Value (e.g. ui-button-click):");
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
                targetCategory.AddEntry(fieldName.Trim(), idValue.Trim());
                parentWindow.Repaint();
                Close();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }
}
