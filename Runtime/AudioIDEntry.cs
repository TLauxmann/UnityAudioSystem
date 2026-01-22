using System;
using UnityEngine;

namespace Thaudio
{
    [Serializable]
    public class AudioIDEntry
    {
        [SerializeField] private string fieldName;
        [SerializeField] private string idValue;

        public string FieldName => fieldName;
        public string IDValue => idValue;

        public AudioIDEntry(string fieldName, string idValue)
        {
            this.fieldName = fieldName;
            this.idValue = idValue;
        }
    }
}
