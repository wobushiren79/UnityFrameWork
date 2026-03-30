using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AudioView : BaseMonoBehaviour
{
    public AudioSource baseAudioSource;

    public List<AudioClip> listClip;
    public List<AudioClip> listMusic;

    /// <summary>
    /// 根据名字播放。
    /// </summary>
    /// <param name="clipName"></param>
    /// <param name="playPosition">播放地点</param>
    /// <param name="volume">音量大小</param>
    public void PlayClip(string clipName,Vector3 playPosition,float volume)
    {
        if (listClip == null)
            return;
        for(int i = 0; i < listClip.Count; i++)
        {
            AudioClip itemClip= listClip[i];
            if (clipName.Equals(itemClip.name))
            {
                AudioSource.PlayClipAtPoint(itemClip, playPosition, volume);
                break;
            }
        }
    }
    public void PlayClip(string clipName, float volume)
    {
        PlayClip(clipName, transform.position, volume);
    }

    public void PlayMusic()
    {

    }
}