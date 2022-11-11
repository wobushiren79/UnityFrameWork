using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public class SteamWebPlaySummariesBean 
{
    public SteamWebPlaySummariesResponse response;
}

[Serializable]
public class SteamWebPlaySummariesResponse
{
    public List<SteamWebPlaySummariesPlayerItem> players;
}

[Serializable]
public class SteamWebPlaySummariesPlayerItem
{
    public string steamid;
    public int communityvisibilitystate;
    public int profilestate;
    public string personaname;
    public long lastlogoff;
    public string profileurl;
    public string avatar;
    public string avatarmedium;
    public string avatarfull;
}