using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CShaderManager : BaseManager
{
    public Dictionary<string, ComputeShader> dicComputeShader = new Dictionary<string, ComputeShader>();

    protected static string pathCShader = "Assets/ComputeShader";

    public void GetComputeShader(string shaderName,Action<ComputeShader> callBack)
    {      
        GetModelForAddressables(dicComputeShader, $"{pathCShader}/{shaderName}.compute", callBack);
    }
}
