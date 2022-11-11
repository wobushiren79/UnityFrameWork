using UnityEditor;
using UnityEngine;

public static class EventsInfo
{
    public static readonly string TEST = "TEST";

    public static readonly string UIViewSynthesis_SetSelect = "UIViewSynthesis_SetSelect";//合成界面 设置选择
    public static readonly string UIViewSynthesis_SetInitData = "UIViewSynthesis_SetInitData";//合成界面 设置初始化数据

    public static readonly string UIViewItemContainer_ItemChange = "UIViewItemContainer_ItemChange";//道具容器，道具修改

    public static readonly string ItemsBean_MetaChange = "ItemsBean_MetaChange";//道具数据发生改变

    public static readonly string UIGameBook_MapItemChange = "UIGameBook_MapItemChange";//模块描述改变
    public static readonly string UIGameBook_MapItemClean = "UIGameBook_MapItemClean";//模块描述清空
    public static readonly string UIGameBook_MapItemRefresh = "UIGameBook_MapItemRefresh";//模块刷新
    public static readonly string UIGameBook_RefreshLabels = "UIGameBook_RefreshLabels";//刷新标签

    public static readonly string CharacterStatus_StatusChange = "CharacterStatus_StatusChange";//角色状态修改


    public static readonly string UIBuildingEditorCreate_SelectChange = "UIBuildingEditorCreate_SelectChange";//建造模式 选择方块

    public static readonly string BlockTypeCrucible_UpdateElemental = "BlockTypeCrucible_UpdateElemental";//更新元素

    public static readonly string UIViewShortcutsMagic_InitData = "UIViewShortcutsMagic_InitData";//法术快捷栏 初始化数据 刷新UI
    

}