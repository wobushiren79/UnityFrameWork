using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System;

public partial class AudioHandler : BaseHandler<AudioHandler, AudioManager>
{
    //重复播放时间检测
    protected float timeUpdateForRepeatPlay = 0;

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
    }

    /// <summary>
    ///  循环播放音乐
    /// </summary>
    /// <param name="audioMusic"></param>
    public void PlayMusicForLoop(int musicId)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlayMusicForLoop(musicId, gameConfig.musicVolume);
    }

    /// <summary>
    /// 循环播放音乐
    /// </summary>
    /// <param name="audioMusic"></param>
    /// <param name="volumeScale"></param>
    public void PlayMusicForLoop(int musicId, float volumeScale)
    {
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

    protected int lastPlaySoundId;//上一个播放的音效

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sound">音效</param>
    /// <param name="volumeScale">音量大小</param>
    public void PlaySound(int soundId, Vector3 soundPosition, float volumeScale, AudioSource audioSource = null)
    {
        //如果音效为0 则不播放
        if (volumeScale == 0)
            return;
        //如果上一个音效和这次播放的音效一样，并且间隔再 0.1s内，则不播放
        if(lastPlaySoundId == soundId && timeUpdateForRepeatPlay > 0)
            return;
        AudioInfoBean audioInfo = AudioInfoCfg.GetItemData(soundId);
        if (audioInfo == null)
            return;
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
    public void PlaySound(int soundId, AudioSource audioSource = null)
    {
        if (audioSource == null)
        {
            audioSource = manager.audioSourceForSound;
        }
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlaySound(soundId, Camera.main.transform.position, gameConfig.soundVolume, audioSource);
    }

    public void PlaySound(int soundId, Vector3 soundPosition, AudioSource audioSource = null)
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

    /// <summary>
    /// 播放环境音乐
    /// </summary>
    /// <param name="audioEnvironment"></param>
    public void PlayEnvironment(int environmentId, float volumeScale)
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

    public void PlayEnvironment(int environmentId)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PlayEnvironment(environmentId, gameConfig.environmentVolume);
    }


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
        manager.audioSourceForMusic.Play();
    }

}