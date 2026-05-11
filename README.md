# FrameWork 框架说明文档

## 概述

本框架是为 Unity 项目设计的综合性游戏开发框架，采用 **MVC + 单例 + 组件化** 的架构设计，提供了一套完整的 UI 管理、资源加载、数据存储、事件系统和常用工具集，旨在提高开发效率、统一代码规范、降低模块间的耦合度。

---

## 目录结构

```
FrameWork/
├── Scripts/              # 核心脚本
│   ├── AI/               # AI 相关基类
│   ├── Base/             # 框架基础基类
│   ├── BaseSystem/       # 基础系统（事件、数据库、Steam）
│   ├── Bean/             # 数据模型（Data Model）
│   ├── CallBack/         # 回调接口定义
│   ├── Common/           # 通用脚本
│   ├── Component/        # 功能组件（Manager/Handler/UI/Effect）
│   ├── DataStorage/      # 数据存储基类
│   ├── Enums/            # 枚举定义
│   ├── Extension/        # 扩展方法
│   ├── MVC/              # MVC 相关
│   ├── Tools/            # 工具类
│   ├── Utils/            # 工具方法集
│   └── Web/              # 网络请求
├── Editor/               # 编辑器扩展工具
│   ├── Base/             # 编辑器基础类
│   ├── ScriptsTemplates/ # 脚本模板
│   └── Window/           # 编辑器窗口
├── Addons/               # 第三方插件
│   ├── DOTween/          # 动画补间插件
│   ├── Spine/            # Spine 骨骼动画
│   └── Rotary Heart/     # SerializableDictionary
├── Plugins/              # 原生插件
│   ├── Steamworks.NET/   # Steam API 封装
│   ├── EPPlus/           # Excel 读写
│   └── sqlite3/          # SQLite 数据库
├── Materials/            # 框架材质资源
├── Prefabs/              # 框架预设资源
└── Fbx/                  # 框架模型资源
```

---

## 核心架构

### 1. 基础基类 (Base)

| 类名 | 说明 |
|------|------|
| `BaseMonoBehaviour` | 所有 MonoBehaviour 的基类，提供对象查找、实例化、UI 自动绑定等通用方法 |
| `BaseSingleton` | 普通单例模式基类 |
| `BaseSingletonMonoBehaviour<T>` | MonoBehaviour 单例模式基类，自动处理 DontDestroyOnLoad |
| `BaseManager` | 管理器基类，封装资源加载（AssetBundle/Addressables/Resources）、SpriteAtlas 获取、数据查询等 |
| `BaseHandler<T,M>` | 处理器基类，采用 **Handler-Manager** 模式，Handler 对外提供接口，Manager 管理数据和资源 |
| `BaseUIInit` | UI 初始化基类，处理生命周期和输入系统注册 |
| `BaseUIComponent` | UI 组件基类，继承自 BaseUIInit，支持 OpenUI/CloseUI 及关闭类型控制 |
| `BaseUIView` | UI 视图基类，处理 RectTransform 和尺寸适配 |
| `BaseUIManager` | UI 管理器基类 |
| `BaseMVCService` | MVC 数据服务基类，封装 SQLite 的增删改查操作，支持主副表关联 |

### 2. 设计模式

#### 单例模式 (Singleton)
```csharp
// Handler 通常使用单例模式
public class UIHandler : BaseHandler<UIHandler, UIManager>
{
    // 通过 UIHandler.Instance 全局访问
}
```

#### Handler-Manager 模式
```csharp
// Handler: 对外提供业务接口
public class AudioHandler : BaseHandler<AudioHandler, AudioManager>
{
    public void PlayMusic(string name) { manager.PlayMusic(name); }
}

// Manager: 管理数据和资源加载
public class AudioManager : BaseManager
{
    public void PlayMusic(string name) { /* ... */ }
}
```

#### UI 层级结构
```
BaseUIInit (生命周期管理)
├── BaseUIView (视图层：RectTransform 适配)
└── BaseUIComponent (组件层：Open/Close 逻辑)
    └── 具体 UI (如：DialogView、ToastView、PopupShowView)
```

---

## 功能模块

### 1. UI 系统 (Component/UI)

提供完整的 UI 管理体系：

| 类型 | 说明 | 基类 |
|------|------|------|
| UI | 普通界面 | `BaseUIComponent` |
| Dialog | 模态弹窗 | `DialogView` |
| Toast | 提示气泡 | `ToastView` |
| Popup | 悬浮气泡 | `PopupShowView` |

**核心功能：**
- UI 创建/销毁/层级管理
- Dialog 队列和焦点管理
- Toast 自动销毁
- Popup 缓存管理
- UI 按钮点击锁定 (`CanClickUIButtons`)

**常用 UI 组件：**
- `ScrollGridHorizontal/Vertical` - 无限滚动列表
- `RadioButtonView/RadioGroupView` - 单选按钮组
- `ProgressView` - 进度条
- `ButtonExtendView` - 扩展按钮
- `LongPressButton` - 长按按钮
- `PopupButtonView` - 弹窗按钮
- `UITextLanguageView` - 多语言文本

### 2. 资源加载系统 (Utils)

支持多种资源加载方式：

| 工具类 | 加载方式 | 适用场景 |
|--------|----------|----------|
| `LoadResourcesUtil` | Resources.Load | 小型项目、编辑器模式 |
| `LoadAssetBundleUtil` | AssetBundle | 传统资源包管理 |
| `LoadAddressablesUtil` | Addressables | 现代资源管理（推荐） |
| `LoadAssetUtil` | AssetDatabase (编辑器) | 编辑器开发加速 |

**BaseManager 封装的方法：**
```csharp
// Addressables 异步加载
GetModelForAddressables<T>(dic, keyName, callback);

// Addressables 同步加载
GetModelForAddressablesSync<T>(dic, keyName);

// Resources 加载
GetModelForResources<T>(dic, resPath);

// SpriteAtlas 获取
GetSpriteByName(dicIcon, ref spriteAtlas, resName, name, callback);
```

### 3. 数据存储系统 (BaseSystem/Sqlite)

基于 SQLite 的本地数据库系统：

```csharp
// Service 层继承 BaseMVCService
public class ItemService : BaseMVCService
{
    public ItemService() : base("item_main", "item_details") { }
    
    // 基础查询
    var list = BaseQueryAllData<ItemBean>();
    var item = BaseQueryData<ItemBean>("id", "1001");
    
    // 关联查询
    var details = BaseQueryAllData<ItemBean>("item_id");
}
```

**特性：**
- 支持主副表关联（Left Join）
- 自动反射生成 SQL 语句
- 支持增删改查完整操作
- 封装在 `BaseMVCService` 中，子类只需关注业务逻辑

### 4. 事件系统 (BaseSystem/Event)

```csharp
// 事件实体
EventEntity eventData = new EventEntity(EventEnum.GameStart);
EventHandler.TriggerEvent(eventData);

// 事件监听
EventHandler.Instance.RegisterEvent(EventEnum.GameStart, OnGameStart);
```

### 5. Steam 集成 (BaseSystem/Steam)

通过 Steamworks.NET 提供：
- 成就系统 (`SteamUserStatsHandle`)
- 排行榜 (`SteamLeaderboardImpl`)
- 创意工坊 (`SteamWorkshopHandle`)
- Steam Web API 调用

### 6. 扩展方法 (Extension)

为 C# 和 Unity 原生类型提供大量扩展：

| 扩展类 | 目标类型 | 示例 |
|--------|----------|------|
| `GameObjectExtension` | GameObject | `obj.AddComponentEX<T>()` |
| `ComponentExtension` | Component | `cpt.GetComponentInChildrenEX<T>()` |
| `ListArrayDicExtension` | List/Dictionary | `list.IsNull()`, `dic.TryGetValueEx()` |
| `StringExtension` | string | `str.IsNull()`, `str.ToInt()` |
| `ColorExtension` | Color | `color.ToHex()` |
| `VectorExtension` | Vector3 | `v3.Round()` |
| `MonoExtension` | MonoBehaviour | 协程扩展 |

### 7. 工具类 (Utils/Tools)

| 工具类 | 功能 |
|--------|------|
| `FileUtil` | 文件读写、路径管理 |
| `JsonUtil` | JSON 序列化/反序列化 |
| `ExcelUtil` | Excel 读写（基于 EPPlus） |
| `TimeUtil` | 时间格式化、倒计时 |
| `MathUtil` | 数学计算辅助 |
| `RandomUtil` | 随机数生成 |
| `LogUtil` | 日志输出封装 |
| `ReflexUtil` | 反射工具（自动 UI 绑定） |
| `SceneUtil` | 场景加载管理 |
| `UGUIUtil` | UGUI 辅助方法 |
| `CreateTools` | 运行时对象创建 |
| `DataTools` | 数据处理工具 |

---

## 编辑器工具 (Editor)

### 脚本模板
提供标准化代码模板，通过编辑器窗口快速创建：
- MVC Bean / Service
- UI (BaseUI / Dialog / Popup / Toast)
- UIComponent / UIView

### 编辑器窗口
| 窗口 | 功能 |
|------|------|
| `UIEditorWindow` | UI 创建与管理 |
| `MVCEditorWindow` | MVC 代码生成 |
| `ExcelEditorWindow` | Excel 数据导入 |
| `AddressableWindow` | Addressables 配置工具 |
| `AnimSearchWindow` | 动画资源搜索 |
| `SearchEditorWindow` | 项目资源搜索 |

### Inspector 扩展
- `InspectorBaseUIComponent` - UI 组件 Inspector 增强
- `InspectorBaseUIView` - UI 视图 Inspector 增强

---

## 使用示例

### 创建一个 UI

```csharp
public class UIMain : BaseUIComponent
{
    // UI 控件会自动绑定（命名前缀 ui_）
    public Button ui_BtnStart;
    public Text ui_TxtTitle;
    
    public override void Awake()
    {
        base.Awake();
        AutoLinkUI(); // 自动反射绑定 UI 控件
    }
    
    public override void Start()
    {
        base.Start();
        ui_BtnStart.onClick.AddListener(OnClickStart);
    }
    
    private void OnClickStart()
    {
        // 打开另一个 UI
        UIHandler.Instance.OpenUI<UIGame>();
    }
}
```

### 创建一个 Handler

```csharp
public class GameDataHandler : BaseHandler<GameDataHandler, GameDataManager>
{
    // 对外提供数据访问接口
    public PlayerInfoBean GetPlayerInfo()
    {
        return manager.GetPlayerInfo();
    }
}
```

### 使用 Addressables 加载资源

```csharp
public class ItemManager : BaseManager
{
    private Dictionary<string, GameObject> dicItemModel = new Dictionary<string, GameObject>();
    
    public void GetItemModel(string itemName, Action<GameObject> callback)
    {
        GetModelForAddressables(dicItemModel, itemName, callback);
    }
}
```

---

## 第三方插件

| 插件 | 用途 |
|------|------|
| **DOTween** | 动画补间（位置、旋转、缩放、颜色等） |
| **Spine** | 2D 骨骼动画支持 |
| **SerializableDictionary** | 支持序列化的 Dictionary（Inspector 可显示） |
| **Steamworks.NET** | Steam 平台 API 封装 |
| **EPPlus** | Excel 文件读写 |
| **SQLite** | 本地轻量级数据库 |

---

## 开发规范

1. **命名规范**
   - UI 控件命名前缀：`ui_`（如 `ui_BtnSubmit`）
   - 使用 `AutoLinkUI()` 自动绑定 UI 控件

2. **资源加载**
   - 优先使用 `Addressables` 进行资源管理
   - 使用 `BaseManager` 封装的加载方法以支持缓存

3. **UI 开发**
   - 普通 UI 继承 `BaseUIComponent`
   - 弹窗继承 `DialogView`
   - 提示继承 `ToastView`

4. **数据存储**
   - 数据库操作统一放在 Service 层
   - Service 继承 `BaseMVCService`

5. **单例使用**
   - Handler 层使用单例模式
   - 通过 `XXXHandler.Instance` 访问

---

## 注意事项

1. **AutoLinkUI 依赖命名规范**：需要绑定的 UI 控件必须按 `ui_类型名` 命名（如 `ui_BtnStart`）
2. **Addressables 配置**：使用 Addressables 前需在 Unity 中正确配置 Addressables Groups
3. **SQLite 平台支持**：Android 平台需要对应平台的 `libsqlite3.so` 库
4. **Steam 初始化**：使用 Steam 功能前需确保 `SteamManager` 正确初始化
