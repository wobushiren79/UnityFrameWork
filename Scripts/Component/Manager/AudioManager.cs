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

    protected static string PathMusic = "Assets/LoadResources/Audio/Music";
    protected static string PathSound = "Assets/LoadResources/Audio/Sound";
    protected static string PathEnvironment = "Assets/LoadResources/Audio/Environment";

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

    #region 连续音效池（多路并发循环，供 LoopSound 复用；通用，不与具体项目耦合）
    protected Transform _loopSoundRoot;
    protected Queue<AudioSource> queueLoopSourceIdle = new Queue<AudioSource>();
    protected int loopSourceCount = 0;
    //并发循环音效上限，防无限新建音源
    protected const int MaxLoopSource = 16;

    /// <summary>
    /// 连续音效音源的父容器（挂在常驻 Audio GameObject 下，随 DontDestroyOnLoad 保留）
    /// </summary>
    protected Transform loopSoundRoot
    {
        get
        {
            if (_loopSoundRoot == null)
            {
                GameObject obj = new GameObject("LoopSoundRoot");
                obj.transform.SetParent(audioListener.transform);
                obj.transform.localPosition = Vector3.zero;
                _loopSoundRoot = obj.transform;
            }
            return _loopSoundRoot;
        }
    }

    /// <summary>
    /// 从池中取一个空闲的循环音源；池空则新建（未超上限），超上限返回 null
    /// </summary>
    /// <returns>可用的 AudioSource；超上限时为 null</returns>
    public AudioSource DequeueLoopSource()
    {
        AudioSource source;
        if (queueLoopSourceIdle.Count > 0)
        {
            source = queueLoopSourceIdle.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }
        if (loopSourceCount >= MaxLoopSource)
        {
            LogUtil.LogWarning($"连续音效音源已达上限 {MaxLoopSource}，本次不再新建");
            return null;
        }
        GameObject obj = new GameObject("LoopSource");
        obj.transform.SetParent(loopSoundRoot);
        obj.transform.localPosition = Vector3.zero;
        source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        //第一期全局单路，2D 不吃位置（后续如需 3D 定位再按句柄扩展）
        source.spatialBlend = 0f;
        loopSourceCount++;
        return source;
    }

    /// <summary>
    /// 回收循环音源到池：先停止播放再清空 clip，禁用后入队复用
    /// </summary>
    /// <param name="source">要回收的音源</param>
    public void RecycleLoopSource(AudioSource source)
    {
        if (source == null)
            return;
        source.Stop();
        source.clip = null;
        source.loop = false;
        //复位播放速率，避免上一个变速循环音效(如加速走路声)污染下次复用
        source.pitch = 1f;
        source.gameObject.SetActive(false);
        queueLoopSourceIdle.Enqueue(source);
    }
    #endregion

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