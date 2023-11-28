using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CShaderManager : BaseManager
{
    public Dictionary<string, ComputeShader> dicComputeShader = new Dictionary<string, ComputeShader>();

    protected static string pathCShader = "Assets/ComputeShader";

    public ComputeShader GetComputeShader(string shaderName)
    {
        return GetModelForAddressablesSync(dicComputeShader,$"{pathCShader}/{shaderName}.compute");
    }
}
