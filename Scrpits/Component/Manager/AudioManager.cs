using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public partial class AudioManager : BaseManager
{
    protected AudioListener _audioListener;
    protected AudioSource _audioSourceForMusic;
    protected AudioSource _audioSourceForSound;
    protected AudioSource _audioSourceForEnvironment;
    public AudioListener audioListener
    {
        get
        {
            if (_audioListener == null)
            {
                _audioListener = FindWithTag<AudioListener>(TagInfo.Tag_Audio);
                if (_audioListener == null)
                {
                    GameObject obj = new GameObject("Audio");
                    DontDestroyOnLoad(obj);
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = Vector3.zero;
                    obj.tag = TagInfo.Tag_Audio;
                    _audioListener = obj.AddComponentEX<AudioListener>();
                }
            }
            return _audioListener;
        }
    }

    public AudioSource audioSourceForMusic
    {
        get
        {
            if (_audioSourceForMusic == null)
            {
                _audioSourceForMusic = audioListener.gameObject.AddComponent<AudioSource>();
            }
            return _audioSourceForMusic;
        }
    }

    public AudioSource audioSourceForSound
    {
        get
        {
            if (_audioSourceForSound == null)
            {
                _audioSourceForSound = audioListener.gameObject.AddComponent<AudioSource>();
            }
            return _audioSourceForSound;
        }
    }

    public AudioSource audioSourceForEnvironment
    {
        get
        {
            if (_audioSourceForEnvironment == null)
            {
                _audioSourceForEnvironment = audioListener.gameObject.AddComponent<AudioSource>();
            }
            return _audioSourceForEnvironment;
        }
    }

    protected Dictionary<string, AudioClip> dicMusicData = new Dictionary<string, AudioClip>();
    protected Dictionary<string, AudioClip> dicSoundData = new Dictionary<string, AudioClip>();
    protected Dictionary<string, AudioClip> dicEnvironmentData = new Dictionary<string, AudioClip>();

    protected static string PathMusic = "Assets/Audio/Music";
    protected static string PathSound = "Assets/Audio/Sound";
    protected static string PathEnvironment = "Assets/Audio/Environment";

    /// <summary>
    /// 根据名字获取音乐
    /// </summary>
    /// <param name="name"></param>
    /// <param name="completeAction"></param>
    public void GetMusicClip(string name, Action<AudioClip> completeAction)
    {
        LoadClipDataByAddressbles(AuidoTypeEnum.Music, name, completeAction);
    }

    /// <summary>
    /// 根据名字获取音效
    /// </summary>
    /// <param name="name"></param>
    /// <param name="completeAction"></param>
    public void GetSoundClip(string name, Action<AudioClip> completeAction)
    {
        LoadClipDataByAddressbles(AuidoTypeEnum.Sound, name, completeAction);
    }

    /// <summary>
    /// 根据名字获取环境音乐
    /// </summary>
    /// <param name="name"></param>
    /// <param name="completeAction"></param>
    public void GetEnvironmentClip(string name, Action<AudioClip> completeAction)
    {
        LoadClipDataByAddressbles(AuidoTypeEnum.Environment, name, completeAction);
    }

    /// <summary>
    /// 加载音频资源
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="completeAction"></param>
    public void LoadClipDataByAddressbles(AuidoTypeEnum audioType, string name, Action<AudioClip> completeAction)
    {
        Dictionary<string, AudioClip> dicAudioData;
        string pathData;
        switch (audioType)
        {
            case AuidoTypeEnum.Music:
                pathData = PathMusic;
                dicAudioData = dicMusicData;
                break;
            case AuidoTypeEnum.Sound:
                pathData = PathSound;
                dicAudioData = dicSoundData;
                break;
            case AuidoTypeEnum.Environment:
                pathData = PathEnvironment;
                dicAudioData = dicEnvironmentData;
                break;
            default:
                return;
        }
        string allPathData = $"{pathData}/{name}";
        if (dicAudioData.TryGetValue(allPathData, out AudioClip audioClip))
        {
            completeAction?.Invoke(audioClip);
            return;
        }
        LoadAddressablesUtil.LoadAssetAsync<AudioClip>(allPathData, (data) =>
        {
            if (data.Result != null)
            {
                if (dicAudioData.TryGetValue(allPathData, out AudioClip audioClip))
                {
                    completeAction?.Invoke(audioClip);
                    return;
                }
                dicAudioData.Add(allPathData, data.Result);
                completeAction?.Invoke(data.Result);
                return;
            }
            completeAction?.Invoke(null);
        });
    }

    #region 获取数据回调
    public void GetAudioInfoSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void GetAudioInfoFail(string failMsg, Action action)
    {
    }
    #endregion
}