using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(Audio))]
public class AudioDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Hole die ID-Property
        SerializedProperty idProp = property.FindPropertyRelative("id");
        string displayName = string.IsNullOrEmpty(idProp.stringValue) ? "New Audio" : idProp.stringValue;

        // Zeige die Property mit der ID als Label
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, displayName, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // ID Field
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                idProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Clips Liste
            SerializedProperty clipsProp = property.FindPropertyRelative("clips");

            // Stelle sicher, dass die Liste nie null ist
            if (clipsProp.arraySize == 0 && clipsProp.isArray)
            {
                EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                    "Audio Clips", "(Keine Clips)");
            }
            else
            {
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(clipsProp)),
                    clipsProp, true);
            }
            yPos += EditorGUI.GetPropertyHeight(clipsProp) + EditorGUIUtility.standardVerticalSpacing;

            // Volume
            SerializedProperty volumeProp = property.FindPropertyRelative("volume");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                volumeProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Volume Variance
            SerializedProperty volumeVarianceProp = property.FindPropertyRelative("volumeVariance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                volumeVarianceProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Pitch
            SerializedProperty pitchProp = property.FindPropertyRelative("pitch");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                pitchProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Pitch Variance
            SerializedProperty pitchVarianceProp = property.FindPropertyRelative("pitchVariance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                pitchVarianceProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Loop
            SerializedProperty loopProp = property.FindPropertyRelative("loop");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                loopProp);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        SerializedProperty clipsProp = property.FindPropertyRelative("clips");
        float clipsHeight = EditorGUI.GetPropertyHeight(clipsProp);

        return EditorGUIUtility.singleLineHeight * 7 + clipsHeight + EditorGUIUtility.standardVerticalSpacing * 7;
    }
}