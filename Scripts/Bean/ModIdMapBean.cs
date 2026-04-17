using System;
using System.Collections.Generic;

[Serializable]
public class ModIdMapBean
{
    /// <summary>
    /// Mod名称到modId的映射
    /// </summary>
    public Dictionary<string, int> modIdMap = new Dictionary<string, int>();
}
