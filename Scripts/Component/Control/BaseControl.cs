using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BaseControl : BaseMonoBehaviour
{
    public bool enabledControl;

    public virtual void EnabledControl(bool enabled)
    {
        this.enabledControl = enabled;
    }
}
