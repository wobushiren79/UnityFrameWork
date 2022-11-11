using UnityEditor;
using UnityEngine;

public class BaseEffectView : BaseMonoBehaviour
{
    private void OnParticleSystemStopped()
    {
        Destroy(gameObject);
    }
}