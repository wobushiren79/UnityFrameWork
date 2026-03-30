using UnityEngine;
using UnityEditor;
using System;
using RotaryHeart.Lib.SerializableDictionary;

[Serializable]
public class IconBean 
{

    public string key;
    public Sprite value;
}

[Serializable]
public class IconBeanDictionary : SerializableDictionaryBase<string, Sprite>
{

}