using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseControl : BaseMonoBehaviour
{
    public bool enabledControl;

    /// <summary>
    /// ���ؿ���
    /// </summary>
    /// <param name="enabled"></param>
    public virtual void EnabledControl(bool enabled)
    {
        this.enabledControl = enabled;
    }
}
