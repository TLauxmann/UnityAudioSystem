using System;
using UnityEngine;

public class AudioIDAttribute : PropertyAttribute
{
    public Type IDClassType { get; private set; }

    public AudioIDAttribute(Type idClassType)
    {
        IDClassType = idClassType;
    }
}