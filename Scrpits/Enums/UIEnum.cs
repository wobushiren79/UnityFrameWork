using UnityEngine;
using UnityEditor;

public enum UIEnum
{
    Loading = 1,
    BuildingEditorMain = 10,
    BuildingEditorCreate = 11,

    MainStart = 1001, //主界面-开始
    MainCreate = 1002,//主界面-创建游戏
    MainUserData = 1003,//主界面-继续游戏
    MainMaker = 1008,//主界面-制作人界面
    MainExit = 1009,//主界面-离开

    GameMain = 2001,//游戏-主界面
    GameUserDetails = 2002,//游戏-用户详情
    GameBook = 2003,//游戏-书籍
    GameSetting = 2004,//游戏-设置
    GameExit = 2009,//游戏-退出
    GameDead = 2010,//游戏-死亡
    GameMagicCore = 2011,//游戏-法术核心装备界面

    GameBox = 2101,//游戏-箱子
    GameFurnaces = 2102,//游戏-熔炉
    GameSign = 2103,//游戏-牌子
    GameItemsTransition = 2104,//游戏-道具转换
    GameMagicInstrumentAssembly = 2105,//法器组装台

    GodMain = 9001,//GM-主界面
}