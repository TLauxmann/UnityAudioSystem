using System.Collections.Generic;
using UnityEngine;

namespace Gaudio
{
    [CreateAssetMenu(fileName = "New Audio ID Category", menuName = "Gaudio/Audio ID Category", order = 1)]
    public class AudioIDCategory : ScriptableObject
    {
        [SerializeField] private string categoryName;
        [SerializeField] private Color categoryColor = Color.white;
        [SerializeField] private List<AudioIDEntry> entries = new List<AudioIDEntry>();

        public string CategoryName
        {
            get => string.IsNullOrEmpty(categoryName) ? name : categoryName;
            set => categoryName = value;
        }

        public Color CategoryColor
        {
            get => categoryColor;
            set => categoryColor = value;
        }

        public List<AudioIDEntry> Entries => entries;

        public void AddEntry(string fieldName, string idValue)
        {
            if (entries.Exists(e => e.FieldName == fieldName))
            {
                Debug.LogWarning($"ID '{fieldName}' already exists in category '{CategoryName}'!");
                return;
            }

            entries.Add(new AudioIDEntry(fieldName, idValue));
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void RemoveEntry(string fieldName)
        {
            entries.RemoveAll(e => e.FieldName == fieldName);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public string GetID(string fieldName)
        {
            var entry = entries.Find(e => e.FieldName == fieldName);
            return entry?.IDValue;
        }

        public bool HasEntry(string fieldName)
        {
            return entries.Exists(e => e.FieldName == fieldName);
        }
    }
}
