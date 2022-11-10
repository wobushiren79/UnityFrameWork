using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class ImageResBean 
{
    public List<ImageResBeanItemBean> listSaveData = new List<ImageResBeanItemBean>();
}

[Serializable]
public class ImageResBeanItemBean
{
    //资源路径
    public string pathRes;
    public int maxTextureSize = 32;
    public int textureImporterCompression = 0;
    public int textureImporterType;
    public int spritePixelsPerUnit;
    public int wrapMode;
}