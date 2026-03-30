using UnityEngine;
using UnityEditor;
using RotaryHeart.Lib.SerializableDictionary;
using System;

public class AnimBean 
{
    public string key;
    public AnimationClip value;
}


[Serializable]
public class AnimBeanDictionary : SerializableDictionaryBase<string, AnimationClip>
{

}