using UnityEngine;
using UnityEditor;
using System;
using Steamworks;

[Serializable]
public class SteamWorkshopQueryInstallInfoBean 
{
    //文件大小
    public ulong punSizeOnDisk;
    
    //文件路径
    public string pchFolder;

    //文件上传时间
    public uint punTimeStamp;

    //缩略图地址
    public string previewUrl;

    //原数据
    public string metaData;

    //详细信息
    public SteamUGCDetails_t detailsInfo;
}