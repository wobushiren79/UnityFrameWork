using UnityEditor;

/// <summary>
/// 编辑器菜单开关管理器：集中管理各类编辑器自定义菜单的显隐开关。
/// 开关值持久化到 EditorPrefs，跨会话保留。
/// 用法：在目标 MenuItem 的校验方法（[MenuItem(path, true)]）里返回值上叠加 IsEnabled(开关键)，
/// 开关关闭时该项即在右键上下文菜单中隐藏（顶部菜单栏则灰掉）。
/// 扩展：新增一类菜单开关时，只需在「开关键定义」补一个常量，并在下方仿照「像素图导入设置」补一段带勾选的切换菜单区域。
/// </summary>
public static class EditorMenuSwitch
{
    #region 开关键定义

    /// <summary>
    /// EditorPrefs 键前缀，避免与其他工程/插件的键名冲突。
    /// </summary>
    const string KEY_PREFIX = "DLR_EditorMenuSwitch_";

    /// <summary>
    /// 「像素图导入设置」右键菜单（像素图设置32 / 像素图设置16）的开关键。
    /// </summary>
    public const string PixelTextureSetting = "PixelTextureSetting";

    #endregion

    #region 开关读写

    /// <summary>
    /// 查询指定开关是否启用（默认启用）。供 MenuItem 校验方法调用。
    /// </summary>
    /// <param name="switchKey">开关键（取自「开关键定义」区域的常量）。</param>
    /// <returns>启用返回 true。</returns>
    public static bool IsEnabled(string switchKey)
    {
        return EditorPrefs.GetBool(KEY_PREFIX + switchKey, true);
    }

    /// <summary>
    /// 设置指定开关的启用状态并持久化。
    /// </summary>
    /// <param name="switchKey">开关键。</param>
    /// <param name="enabled">是否启用。</param>
    public static void SetEnabled(string switchKey, bool enabled)
    {
        EditorPrefs.SetBool(KEY_PREFIX + switchKey, enabled);
    }

    /// <summary>
    /// 翻转指定开关的启用状态。
    /// </summary>
    /// <param name="switchKey">开关键。</param>
    /// <returns>翻转后的新状态。</returns>
    public static bool Toggle(string switchKey)
    {
        bool newValue = !IsEnabled(switchKey);
        SetEnabled(switchKey, newValue);
        return newValue;
    }

    #endregion

    #region 切换菜单项 - 像素图导入设置

    /// <summary>
    /// 「像素图导入设置」开关的切换菜单路径。
    /// </summary>
    const string MENU_PIXEL_TEXTURE_SETTING = "Custom/菜单开关/像素图导入右键菜单";

    /// <summary>
    /// 菜单栏开关：切换「像素图导入设置」右键菜单的显隐。
    /// </summary>
    [MenuItem(MENU_PIXEL_TEXTURE_SETTING, false, 1000)]
    static void TogglePixelTextureSetting()
    {
        Toggle(PixelTextureSetting);
    }

    /// <summary>
    /// 同步「像素图导入设置」切换菜单项的勾选标记，反映当前开关状态。
    /// </summary>
    /// <returns>恒为 true（该切换项始终可点）。</returns>
    [MenuItem(MENU_PIXEL_TEXTURE_SETTING, true)]
    static bool TogglePixelTextureSettingValidate()
    {
        Menu.SetChecked(MENU_PIXEL_TEXTURE_SETTING, IsEnabled(PixelTextureSetting));
        return true;
    }

    #endregion
}
