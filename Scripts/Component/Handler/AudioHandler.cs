using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System;

public partial class AudioHandler : BaseHandler<AudioHandler, AudioManager>
{
    //重复播放时间检测
    protected float timeUpdateForRepeatPlay = 0;
    //重复音乐列表
    protected List<long> listMusicLoop = new List<long>();
    protected Coroutine coroutineForMusicLoop;

    public void Update()
    {
        if (Camera.main != null)
        {
            manager.audioListener.transform.position = Camera.main.transform.position;
        }
        if (timeUpdateForRepeatPlay > 0)
        {
            timeUpdateForRepeatPlay -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void InitAudio()
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        manager.audioSourceForMusic.volume = gameConfig.musicVolume;
        manager.audioSourceForSound.volume = gameConfig.soundVolume;
        manager.audioSourceForEnvironment.volume = gameConfig.environmentVolume;
        //刷新活跃的连续音效音量：跟随音效音量 soundVolume，并保留各自配置 volume_scale
        foreach (KeyValuePair<long, LoopSoundEntry> kv in dicLoopActive)
        {
            if (kv.Value.source != null)
                kv.Value.source.volume = gameConfig.soundVolume * kv.Value.volumeScale;
        }
    }

    #region  音乐播放
    /// <summary>
    ///  循环播放音乐-单曲
    /// </summary>
    public void PlayMusicForLoop(long musicId)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlayMusicForLoop(musicId, gameConfig.musicVolume);
    }

    /// <summary>
    /// 循环播放音乐
    /// </summary>
    /// <param name="audioMusic"></param>
    /// <param name="volumeScale"></param>
    public void PlayMusicForLoop(long musicId, float volumeScale)
    {
        StopMusicListLoop();
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(musicId);
        if (audioInfo == null)
            return;
        manager.GetMusicClip(audioInfo.name_res, (audioClip) =>
        {
            if (audioClip != null)
            {
                manager.audioSourceForMusic.clip = audioClip;
                manager.audioSourceForMusic.volume = volumeScale;
                manager.audioSourceForMusic.loop = true;
                manager.audioSourceForMusic.Play();
            }
        });
    }

    /// <summary>
    /// 循环播放音乐-列表
    /// </summary>
    public void PlayMusicListForLoop(List<long> musicIds)
    {
        StopMusicListLoop();
        listMusicLoop = musicIds;
        PlayMusicListForLoop();
    }

    private void PlayMusicListForLoop()
    {
        int musicIdRandomIndex = UnityEngine.Random.Range(0, listMusicLoop.Count);
        long musicIdRandom = listMusicLoop[musicIdRandomIndex];
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(musicIdRandom);
        if (audioInfo == null)
        {
            LogUtil.LogError($"循环播放音乐失败 没有找到ID {musicIdRandom}的音乐");
            return;
        }
        manager.GetMusicClip(audioInfo.name_res, (audioClip) =>
        {
            if (audioClip != null)
            {
                manager.audioSourceForMusic.clip = audioClip;
                manager.audioSourceForMusic.loop = false;
                manager.audioSourceForMusic.Play();
                coroutineForMusicLoop = StartCoroutine(CheckMusicForLoopListProgress(audioClip.length));
            }
        });
    }

    /// <summary>
    /// 携程-音乐列表循环
    /// </summary>
    /// <param name="musicLength"></param>
    /// <returns></returns>
    IEnumerator CheckMusicForLoopListProgress(float musicLength)
    {
        yield return new WaitForSeconds(musicLength);
        PlayMusicListForLoop();
    }

    #endregion

    #region  音效播放
    protected long lastPlaySoundId;//上一个播放的音效

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sound">音效</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlaySound(long soundId, Vector3 soundPosition, float volumeScale, AudioSource audioSource = null)
    {
        if (soundId == 0)
            return;
        //如果音效为0 则不播放
        if (volumeScale == 0)
            return;
        //如果上一个音效和这次播放的音效一样，并且间隔再 0.1s内，则不播放
        if (lastPlaySoundId == soundId && timeUpdateForRepeatPlay > 0)
            return;
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(soundId);
        if (audioInfo == null)
            return;
        //叠加配置表的音效音量缩放：volume_scale 为 0 或未填时视为 1（不缩放），否则在基础音量上 ×缩放系数
        if (audioInfo.volume_scale > 0)
        {
            volumeScale *= audioInfo.volume_scale;
        }
        manager.GetSoundClip(audioInfo.name_res, (audioClip) =>
        {
            if (audioClip != null)
            {
                StartCoroutine(CoroutineForPlayOneShot(audioSource, audioClip, volumeScale, soundPosition));
            }
            else
            {
                Debug.LogError($"没有名字为:{audioInfo.name_res} 的音效资源");
            }
        });
        timeUpdateForRepeatPlay = 0.1f;
        lastPlaySoundId = soundId;
    }

    /// <summary>
    /// 播放音效 （没有指定播放位置 所以当audioSource为null是 自动使用audioSourceForSound）
    /// </summary>
    public void PlaySound(long soundId, AudioSource audioSource = null)
    {
        if (audioSource == null)
        {
            audioSource = manager.audioSourceForSound;
        }
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        if (Camera.main != null)
        {
            PlaySound(soundId, Camera.main.transform.position, gameConfig.soundVolume, audioSource);
        }
    }

    public void PlaySound(long soundId, Vector3 soundPosition, AudioSource audioSource = null)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlaySound(soundId, soundPosition, gameConfig.soundVolume, audioSource);
    }

    /// <summary>
    /// 协程播放音效
    /// </summary>
    /// <param name="audioSource"></param>
    /// <param name="audioClip"></param>
    /// <param name="volumeScale"></param>
    /// <returns></returns>
    IEnumerator CoroutineForPlayOneShot(AudioSource audioSource, AudioClip audioClip, float volumeScale, Vector3 soundPosition)
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(audioClip, volumeScale);
        }
        else
        {
            AudioSource.PlayClipAtPoint(audioClip, soundPosition, volumeScale);
        }
        yield return new WaitForSeconds(audioClip.length);
    }

    #endregion

    #region  环境音效 
    /// <summary>
    /// 播放环境音乐
    /// </summary>
    /// <param name="audioEnvironment"></param>
    public void PlayEnvironment(long environmentId, float volumeScale)
    {
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(environmentId);
        if (audioInfo == null)
            return;
        manager.GetEnvironmentClip(audioInfo.name_res, (audioClip) =>
        {
            if (audioClip != null)
            {
                manager.audioSourceForEnvironment.volume = volumeScale;
                manager.audioSourceForEnvironment.clip = audioClip;
                manager.audioSourceForEnvironment.loop = true;
                manager.audioSourceForEnvironment.Play();
            }
        });
    }

    public void PlayEnvironment(long environmentId)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlayEnvironment(environmentId, gameConfig.environmentVolume);
    }
    #endregion

    #region 连续音效（多路并发循环，如走路/下雨；复用任意音频按其 audio_type 加载 clip）
    /// <summary>
    /// 连续音效通道：记录活跃循环音效的音源、配置音量缩放与异步竞态令牌
    /// </summary>
    protected class LoopSoundEntry
    {
        //循环播放的音源（clip 加载完成后才赋值）
        public AudioSource source;
        //配置表 volume_scale（音量刷新时用于重算，0或空视为1）
        public float volumeScale = 1f;
        //版本令牌，用于加载/停止竞态判定
        public int token;
        //加载回调到达前若被停止则置 true，回调据此丢弃
        public bool canceled;
        //播放速率/音调：1=原速，2=加快一倍（同时升调），供走路等需要变速的循环音效使用
        public float pitch = 1f;
    }
    //活跃连续音效：key=音频id（含加载中）
    protected Dictionary<long, LoopSoundEntry> dicLoopActive = new Dictionary<long, LoopSoundEntry>();
    //连续音效竞态令牌自增种子
    protected int loopTokenSeed = 0;
    //被暂停的连续音效id（PauseAllLoopSound 记录，RestoreAllLoopSound 精确恢复）
    protected List<long> listLoopPaused = new List<long>();

    /// <summary>
    /// 播放连续音效（按 id 单路循环）。同一 id 已在播（含加载中）则忽略，避免重复起源。
    /// 最终音量 = volumeScale × 配置 volume_scale。
    /// </summary>
    /// <param name="loopId">音频 id（复用普通音频配置，按其 audio_type 定位资源）</param>
    /// <param name="volumeScale">基础音量；&lt;0 时取音效音量 soundVolume（默认行为）</param>
    /// <param name="pitch">播放速率/音调，1=原速，2=加快一倍（同时升调）</param>
    public void PlayLoopSound(long loopId, float volumeScale = -1f, float pitch = 1f)
    {
        if (loopId == 0)
            return;
        //音量缺省(<0)时取音效音量 soundVolume
        if (volumeScale < 0f)
            volumeScale = GameDataHandler.Instance.manager.GetGameConfig().soundVolume;
        //去重：同 id 已活跃（播放中或加载中）直接返回
        if (dicLoopActive.ContainsKey(loopId))
            return;
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(loopId);
        if (audioInfo == null)
        {
            LogUtil.LogError($"播放连续音效失败 没有找到ID {loopId} 的音频配置");
            return;
        }
        //配置 volume_scale：0 或空视为 1（不缩放）
        float configScale = audioInfo.volume_scale > 0 ? audioInfo.volume_scale : 1f;
        int token = ++loopTokenSeed;
        LoopSoundEntry entry = new LoopSoundEntry { source = null, volumeScale = configScale, token = token, canceled = false, pitch = pitch };
        dicLoopActive[loopId] = entry;
        //复用现有按 audio_type 的加载路由：走路声(audio_type=0)从 Audio/Sound/ 取 clip
        manager.LoadClipDataByAddressbles((AuidoTypeEnum)audioInfo.audio_type, audioInfo.name_res, (audioClip) =>
        {
            //竞态防护：回调到达时若该 id 已被停止/替换/令牌失效则丢弃，防"停不掉的孤儿音源"
            if (!dicLoopActive.TryGetValue(loopId, out LoopSoundEntry curEntry) || curEntry != entry || curEntry.canceled || curEntry.token != token)
                return;
            if (audioClip == null)
            {
                dicLoopActive.Remove(loopId);
                LogUtil.LogError($"播放连续音效失败 没有名字为:{audioInfo.name_res} 的音频资源");
                return;
            }
            AudioSource source = manager.DequeueLoopSource();
            if (source == null)
            {
                //音源池已满，放弃本次播放并清理登记
                dicLoopActive.Remove(loopId);
                return;
            }
            source.clip = audioClip;
            source.loop = true;
            source.volume = volumeScale * configScale;
            source.pitch = entry.pitch;
            source.Play();
            entry.source = source;
        });
    }

    /// <summary>
    /// 停止指定连续音效（加载中的也能取消，回调到达时不会再起播）
    /// </summary>
    /// <param name="loopId">音频 id</param>
    public void StopLoopSound(long loopId)
    {
        if (!dicLoopActive.TryGetValue(loopId, out LoopSoundEntry entry))
            return;
        entry.canceled = true;
        if (entry.source != null)
            manager.RecycleLoopSource(entry.source);
        dicLoopActive.Remove(loopId);
    }

    /// <summary>
    /// 停止所有连续音效（场景切换/清理时调用，防常驻音源跨场景残留）
    /// </summary>
    public void StopAllLoopSound()
    {
        foreach (KeyValuePair<long, LoopSoundEntry> kv in dicLoopActive)
        {
            kv.Value.canceled = true;
            if (kv.Value.source != null)
                manager.RecycleLoopSource(kv.Value.source);
        }
        dicLoopActive.Clear();
        listLoopPaused.Clear();
    }

    /// <summary>
    /// 暂停所有正在播放的连续音效（仅暂停当前 isPlaying 的音源并记录，供精确恢复）
    /// </summary>
    public void PauseAllLoopSound()
    {
        listLoopPaused.Clear();
        foreach (KeyValuePair<long, LoopSoundEntry> kv in dicLoopActive)
        {
            if (kv.Value.source != null && kv.Value.source.isPlaying)
            {
                kv.Value.source.Pause();
                listLoopPaused.Add(kv.Key);
            }
        }
    }

    /// <summary>
    /// 恢复此前被 PauseAllLoopSound 暂停的连续音效（不误启期间新增/已停止的音源）
    /// </summary>
    public void RestoreAllLoopSound()
    {
        for (int i = 0; i < listLoopPaused.Count; i++)
        {
            if (dicLoopActive.TryGetValue(listLoopPaused[i], out LoopSoundEntry entry) && entry.source != null && !entry.canceled)
                entry.source.UnPause();
        }
        listLoopPaused.Clear();
    }

    /// <summary>
    /// 指定连续音效是否正在播放（加载中未起播返回 false）
    /// </summary>
    /// <param name="loopId">音频 id</param>
    /// <returns>正在播放为 true</returns>
    public bool IsLoopSoundPlaying(long loopId)
    {
        return dicLoopActive.TryGetValue(loopId, out LoopSoundEntry entry) && entry.source != null && entry.source.isPlaying;
    }
    #endregion

    #region 定时淡出音效（一次性播放+末段淡出，借用连续音效池独立音源）
    /// <summary>
    /// 播放一段"定时播放 + 末段淡出"的音效：借用连续音效池的独立音源，前 fadeStartTime 秒保持原音量，
    /// 从 fadeStartTime 到 playDuration 按曲线(默认线性)淡出到 0，到 playDuration 停止并自动回收音源。
    /// 用于"只取长音效的前一段并平滑收尾"的场景（如 10s 音效只用 5s、第 4s 起淡出）。
    /// 说明：为一次性播放，不登记到 dicLoopActive，故不参与全局 Pause/Stop/音量刷新，也暂不支持中途打断。
    /// </summary>
    /// <param name="soundId">音频 id（复用普通音频配置，按其 audio_type 定位资源）</param>
    /// <param name="playDuration">总播放时长（秒），到点停止；应 ≤ clip 实际长度</param>
    /// <param name="fadeStartTime">淡出起始时刻（秒），此前保持原音量；应 ≤ playDuration</param>
    /// <param name="volumeScale">基础音量；&lt;0 时取音效音量 soundVolume（默认行为）</param>
    public void PlaySoundTimedFade(long soundId, float playDuration, float fadeStartTime, float volumeScale = -1f)
    {
        if (soundId == 0)
            return;
        //音量缺省(<0)时取音效音量 soundVolume
        if (volumeScale < 0f)
            volumeScale = GameDataHandler.Instance.manager.GetGameConfig().soundVolume;
        //音量为0直接不播
        if (volumeScale <= 0f)
            return;
        //参数纠偏：淡出起点不早于0、不晚于总时长
        if (fadeStartTime < 0f)
            fadeStartTime = 0f;
        if (fadeStartTime > playDuration)
            fadeStartTime = playDuration;
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(soundId);
        if (audioInfo == null)
        {
            LogUtil.LogError($"定时淡出音效失败 没有找到ID {soundId} 的音频配置");
            return;
        }
        //配置 volume_scale：0 或空视为 1（不缩放），叠加得最终基础音量
        float configScale = audioInfo.volume_scale > 0 ? audioInfo.volume_scale : 1f;
        float baseVolume = volumeScale * configScale;
        //按 audio_type 复用现有加载路由取 clip
        manager.LoadClipDataByAddressbles((AuidoTypeEnum)audioInfo.audio_type, audioInfo.name_res, (audioClip) =>
        {
            if (audioClip == null)
            {
                LogUtil.LogError($"定时淡出音效失败 没有名字为:{audioInfo.name_res} 的音频资源");
                return;
            }
            AudioSource source = manager.DequeueLoopSource();
            //音源池已满则放弃本次播放
            if (source == null)
                return;
            source.clip = audioClip;
            source.loop = false;
            source.volume = baseVolume;
            source.Play();
            StartCoroutine(CoroutineForPlayTimedFade(source, baseVolume, playDuration, fadeStartTime));
        });
    }

    /// <summary>
    /// 协程-定时淡出：前 fadeStartTime 秒原音量，之后线性将音量淡到 0，到 playDuration 停止并回收音源。
    /// </summary>
    /// <param name="source">播放中的独立音源（来自连续音效池）</param>
    /// <param name="baseVolume">淡出前的基础音量</param>
    /// <param name="playDuration">总播放时长（秒）</param>
    /// <param name="fadeStartTime">淡出起始时刻（秒）</param>
    IEnumerator CoroutineForPlayTimedFade(AudioSource source, float baseVolume, float playDuration, float fadeStartTime)
    {
        //前段：保持原音量直到淡出起点
        if (fadeStartTime > 0f)
            yield return new WaitForSeconds(fadeStartTime);
        //淡出段时长
        float fadeDuration = playDuration - fadeStartTime;
        if (fadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration && source != null)
            {
                //归一化进度 0→1，线性淡出音量系数 1→0
                float progress = elapsed / fadeDuration;
                source.volume = baseVolume * (1f - progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        //到点归还音源到池（内部 Stop + 复位 volume/pitch）
        if (source != null)
            manager.RecycleLoopSource(source);
    }
    #endregion

    #region 停止相关
    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopEnvironment()
    {
        manager.audioSourceForEnvironment.clip = null;
        manager.audioSourceForEnvironment.Stop();
    }

    public void StopMusic()
    {
        manager.audioSourceForMusic.clip = null;
        manager.audioSourceForMusic.Stop();
        StopMusicListLoop();
    }

    /// <summary>
    /// 停止音乐列表循环
    /// </summary>
    public void StopMusicListLoop()
    {
        if (coroutineForMusicLoop != null)
            StopCoroutine(coroutineForMusicLoop);
    }

    /// <summary>
    /// 暂停环境音
    /// </summary>
    public void PauseEnvironment()
    {
        manager.audioSourceForEnvironment.Pause();
    }

    /// <summary>
    ///  暂停音乐
    /// </summary>
    public void PauseMusic()
    {
        StopMusicListLoop();
        manager.audioSourceForMusic.Pause();
    }

    /// <summary>
    /// 恢复环境音
    /// </summary>
    public void RestoreEnvironment()
    {
        manager.audioSourceForEnvironment.Play();
    }

    /// <summary>
    /// 恢复音乐
    /// </summary>
    public void RestoreMusic()
    {
        StopMusicListLoop();
        manager.audioSourceForMusic.Play();
    }
    #endregion
}