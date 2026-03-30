using UnityEngine;
using UnityEditor;
using System;
using Steamworks;
using System.Collections.Generic;

[Serializable]
public class SteamWorkshopUpdateBean
{
    //标题
    public string title;
    //介绍
    public string description;
    //所属标签
    public List<string> tags;
    //上传文件所在文件夹绝对路径 F:\\Test
    public string content;
    //上传文件浏览图片的绝对路径  F:\\Test\\A.jpg  需要 JPG, PNG  或者 GIF.
    public string preview;

    //原数据
    public string metadata = "";
    //公布状态  默认公开
    public ERemoteStoragePublishedFileVisibility visibility= ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
    //语言 具体参数请参考steam文献  默认english
    public string updateLanguage="english";
}