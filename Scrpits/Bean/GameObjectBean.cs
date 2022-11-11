using UnityEngine;
using UnityEditor;
using System;
using RotaryHeart.Lib.SerializableDictionary;

public class GameObjectBean
{

}

[Serializable]
public class GameObjectDictionary : SerializableDictionaryBase<string, GameObject>
{

}