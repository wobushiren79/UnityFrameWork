using UnityEngine;
using UnityEditor;

public class BaseHandler<T,M> : BaseSingletonMonoBehaviour<T> 
    where M : BaseManager
    where T : BaseMonoBehaviour
{
    private M _manager;

    public virtual void Awake()
    {

    }

    public M manager
    {
        get
        {
            if (_manager == null)
            {
                _manager = gameObject.AddComponentEX<M>();
            }
            return _manager;
        }
    }
}