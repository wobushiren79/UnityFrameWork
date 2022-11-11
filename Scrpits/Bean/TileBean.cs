using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System;
using RotaryHeart.Lib.SerializableDictionary;

[Serializable]
public class TileBean
{
    public string key;
    public TileBase value;
}

[Serializable]
public class TileBeanDictionary : SerializableDictionaryBase<string, TileBase>
{

}