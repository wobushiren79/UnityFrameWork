using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonAudio : BaseMonoBehaviour
{
    public Button button;
    public List<AudioClip> clickClip;
    public float volume = 1;

    private void Start()
    {
        if (button != null)
            button.onClick.AddListener(PlayClip);
    }

    public virtual void PlayClip()
    {
        if (clickClip.IsNull())
            return;
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        AudioClip audioClip = RandomUtil.GetRandomDataByList(clickClip);
        AudioSource.PlayClipAtPoint(audioClip, button.transform.position, gameConfig.soundVolume);
    }

}