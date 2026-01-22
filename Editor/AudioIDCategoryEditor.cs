using UnityEditor;
using UnityEngine;

namespace Gaudio.Editor
{
    [CustomEditor(typeof(AudioIDCategory))]
    public class AudioIDCategoryEditor : UnityEditor.Editor
    {
        private SerializedProperty categoryNameProp;
        private SerializedProperty categoryColorProp;
        private SerializedProperty entriesProp;

        private void OnEnable()
        {
            categoryNameProp = serializedObject.FindProperty("categoryName");
            categoryColorProp = serializedObject.FindProperty("categoryColor");
            entriesProp = serializedObject.FindProperty("entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(categoryNameProp, new GUIContent("Category Name"));
            EditorGUILayout.PropertyField(categoryColorProp, new GUIContent("Category Color"));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField($"Audio IDs ({entriesProp.arraySize})", EditorStyles.boldLabel);

            if (entriesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No IDs defined yet.\n\n" +
                    "Use Tools ? Gaudio ? Show Audio IDs to manage IDs with a better UI!",
                    MessageType.Info);
            }
            else
            {
                for (int i = 0; i < entriesProp.arraySize; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var fieldNameProp = entry.FindPropertyRelative("fieldName");
                    var idValueProp = entry.FindPropertyRelative("idValue");

                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField(fieldNameProp.stringValue, GUILayout.Width(150));
                    EditorGUILayout.LabelField(idValueProp.stringValue);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Audio IDs Window", GUILayout.Height(30)))
            {
                AudioIDsWindow.ShowWindow();
            }

            if (GUILayout.Button("Generate Code for This Category", GUILayout.Height(25)))
            {
                AudioIDCodeGenerator.GenerateCodeForCategory((AudioIDCategory)target);
                AssetDatabase.Refresh();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
