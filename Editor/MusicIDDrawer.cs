using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(AudioIDAttribute))]
public class AudioIDDrawer : PropertyDrawer
{
    // Cache the strings per type so we only perform reflection once per class
    private static Dictionary<Type, string[]> _optionsCache = new Dictionary<Type, string[]>();

    private string[] GetOptions(Type type)
    {
        if (_optionsCache.TryGetValue(type, out var options))
        {
            return options;
        }

        // Get all public, static constants from the given class
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

        options = fields.Select(x => (string)x.GetRawConstantValue()).ToArray();
        _optionsCache[type] = options;

        return options;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [AudioID] only on strings.");
            return;
        }

        // Read the attribute to get the type (the target class)
        var audioIdAttribute = attribute as AudioIDAttribute;
        if (audioIdAttribute == null || audioIdAttribute.IDClassType == null)
        {
            EditorGUI.LabelField(position, label.text, "Missing class in attribute.");
            return;
        }

        string[] options = GetOptions(audioIdAttribute.IDClassType);

        if (options == null || options.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, $"No IDs in {audioIdAttribute.IDClassType.Name}.");
            return;
        }

        int selectedIndex = Array.IndexOf(options, property.stringValue);
        if (selectedIndex < 0) selectedIndex = 0;

        // Draw dropdown
        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, options);
        property.stringValue = options[selectedIndex];
    }
}