using UnityEngine;
using UnityEditor;
using System;
using RotaryHeart.Lib.SerializableDictionary;

[Serializable]
public class AudioBean
{
    public string key;
    public AudioClip value;
}

[Serializable]
public class AudioBeanDictionary : SerializableDictionaryBase<string, AudioClip>
{

}