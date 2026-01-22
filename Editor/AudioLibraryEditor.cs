using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(AudioLibrary))]
public class AudioLibraryEditor : Editor
{
    private ReorderableList audioList;
    private SerializedProperty audioProp;

    private void OnEnable()
    {
        audioProp = serializedObject.FindProperty("audio");

        audioList = new ReorderableList(serializedObject, audioProp, true, true, true, true);

        audioList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Audio Clips");
        };

        audioList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = audioList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty idProp = element.FindPropertyRelative("id");

            string displayName = string.IsNullOrEmpty(idProp.stringValue)
                ? $"Audio {index}"
                : idProp.stringValue;

            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element,
                new GUIContent(displayName),
                true
            );
        };

        audioList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = audioList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element) + 4;
        };

        audioList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            
            // Setze Standardwerte
            element.FindPropertyRelative("id").stringValue = "";
            
            // Initialisiere clips-Liste
            SerializedProperty clipsProp = element.FindPropertyRelative("clips");
            clipsProp.ClearArray();
            
            element.FindPropertyRelative("volume").floatValue = 0.75f;
            element.FindPropertyRelative("volumeVariance").floatValue = 0.1f;
            element.FindPropertyRelative("pitch").floatValue = 1f;
            element.FindPropertyRelative("pitchVariance").floatValue = 0.1f;
            element.FindPropertyRelative("loop").boolValue = false;

            serializedObject.ApplyModifiedProperties();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open Audio Settings Window", GUILayout.Height(30)))
        {
            AudioSettingsWindow.ShowWindow((AudioLibrary)target);
        }

        EditorGUILayout.Space(10);

        audioList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}