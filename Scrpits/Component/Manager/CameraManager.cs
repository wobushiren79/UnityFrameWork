using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public partial class CameraManager : BaseManager
{
    //主摄像头
    protected Camera _mainCamera;
    public Camera mainCamera
    {
        get
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    //ui摄像头
    protected Camera _uiCamera;
    public Camera uiCamera
    {
        get
        {
            if (_uiCamera == null)
            {
                //_uiCamera = FindWithTag<Camera>(TagInfo.Tag_UICamera);
                _uiCamera = Camera.main;
            }
            return _uiCamera;
        }
    }
}
