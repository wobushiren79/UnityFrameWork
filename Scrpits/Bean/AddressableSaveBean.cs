using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class AddressableSaveBean
{
    public Dictionary<string, AddressableSaveItemBean> dicSaveData = new Dictionary<string, AddressableSaveItemBean>();
}

[Serializable]
public class AddressableSaveItemBean
{
    public List<string> listPathSave = new List<string>();//保存路径

    public List<string> listLabel = new List<string>();//标记
}