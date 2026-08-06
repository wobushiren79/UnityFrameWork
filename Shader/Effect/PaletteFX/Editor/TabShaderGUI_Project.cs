using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

// 确保在编辑器命名空间中
namespace UnityEditor
{
    // 页签式 Shader GUI编辑器
    public class TabShaderGUI_Project : ShaderGUI
    {
        // 属性缓存，避免重复查找
        protected Dictionary<string, MaterialProperty> propertyCache = new Dictionary<string, MaterialProperty>();

        // 当前选中的页签索引
        private int selectedTabIndex = 0;

        // 用于存储页签索引的EditorPrefs键名前缀
        private const string TAB_INDEX_PREF_KEY = "TabShaderGUI_SelectedTab_";

        // EditorPrefs缓存 - 使用静态字典缓存EditorPrefs值，减少读取操作
        private static Dictionary<string, int> editorPrefsCache = new Dictionary<string, int>();

        // 页签名称列表
        private List<string> tabNames = new List<string>();

        // 页签图片列表
        private List<Texture> tabImages = new List<Texture>();

        // 预计算的标签页矩形区域
        private Rect[] tabRects;

        // 页签状态枚举
        [Flags]
        protected enum TabState
        {
            None = 0,
            TabA = 1 << 0, // 始终启用
            TabB = 1 << 1, // 附加贴图
            TabC = 1 << 2, // 溶解
            TabD = 1 << 3, // 高级
            TabE = 1 << 4, // 特效
            TabF = 1 << 5, // 顶点动画
            TabG = 1 << 6, // 自定义数据
            TabH = 1 << 7  // 模板测试
        }

        // 启用的页签状态
        protected TabState enabledTabs = TabState.TabA; // 默认只启用A页签

        // 移除冗余的布尔变量，完全使用TabState枚举管理状态

        // 页签绘制委托
        protected delegate void DrawTabDelegate();
        private List<DrawTabDelegate> tabDrawers = new List<DrawTabDelegate>();

        // 样式
        private GUIStyle tabButtonStyle;
        private GUIStyle tabButtonSelectedStyle;
        private GUIStyle boxStyle;
        private GUIStyle tabContainerStyle; // 页签容器样式

        // 纹理缓存
        private Texture2D tabButtonTexture;
        private Texture2D tabButtonSelectedTexture;
        private Texture2D boxTexture;
        private Texture2D tabContainerTexture;

        // 样式是否已初始化
        private bool stylesInitialized = false;

        // 静态纹理缓存 - 使用颜色的哈希值作为键
        private static readonly Dictionary<int, Texture2D> textureCache = new Dictionary<int, Texture2D>();

        // 常用颜色预定义，避免重复创建
        private static readonly Color tabButtonColor = new Color(0.2f, 0.2f, 0.2f, 0.0f);
        private static readonly Color tabButtonSelectedColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
        private static readonly Color boxColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
        private static readonly Color tabContainerColor = new Color(0.17f, 0.17f, 0.17f, 1.0f);

        // 检查纹理是否有效
        private bool IsTextureValid(Texture2D texture)
        {
            return texture != null && texture.width > 0 && texture.height > 0;
        }

        // 初始化样式 - 只在需要时调用
        private void InitStyles()
        {
            // 如果样式已初始化且纹理有效，则不需要重新初始化
            if (stylesInitialized &&
                IsTextureValid(tabButtonTexture) &&
                IsTextureValid(tabButtonSelectedTexture) &&
                IsTextureValid(boxTexture) &&
                IsTextureValid(tabContainerTexture))
            {
                return;
            }

            // 使用预定义的颜色常量，避免重复创建

            // 检查并重新创建纹理
            if (!IsTextureValid(tabButtonTexture))
            {
                if (tabButtonTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(tabButtonTexture);
                }
                tabButtonTexture = GetCachedTexture(2, 2, tabButtonColor);
            }

            if (!IsTextureValid(tabButtonSelectedTexture))
            {
                if (tabButtonSelectedTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(tabButtonSelectedTexture);
                }
                tabButtonSelectedTexture = GetCachedTexture(2, 2, tabButtonSelectedColor);
            }

            if (!IsTextureValid(boxTexture))
            {
                if (boxTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(boxTexture);
                }
                boxTexture = GetCachedTexture(2, 2, boxColor);
            }

            if (!IsTextureValid(tabContainerTexture))
            {
                if (tabContainerTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(tabContainerTexture);
                }
                tabContainerTexture = GetCachedTexture(2, 2, tabContainerColor);
            }

            // 创建或更新样式
            if (tabButtonStyle == null)
            {
                // 创建扁平化的标签页样式
                tabButtonStyle = new GUIStyle();
                // 调暗未选中标签页的文字颜色
                tabButtonStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                tabButtonStyle.fontStyle = FontStyle.Normal;
                tabButtonStyle.alignment = TextAnchor.MiddleCenter;
                tabButtonStyle.margin = new RectOffset(0, 0, 0, 0);
                tabButtonStyle.padding = new RectOffset(10, 10, 5, 5);
                tabButtonStyle.border = new RectOffset(0, 0, 0, 0);
            }
            // 更新背景纹理
            tabButtonStyle.normal.background = tabButtonTexture;

            if (tabButtonSelectedStyle == null)
            {
                // 创建扁平化的选中标签页样式
                tabButtonSelectedStyle = new GUIStyle(tabButtonStyle);
                tabButtonSelectedStyle.normal.textColor = Color.white;
                tabButtonSelectedStyle.fontStyle = FontStyle.Bold;
            }
            // 更新背景纹理
            tabButtonSelectedStyle.normal.background = tabButtonSelectedTexture;

            if (boxStyle == null)
            {
                // 创建扁平化的内容区域样式
                boxStyle = new GUIStyle();
                boxStyle.padding = new RectOffset(10, 10, 20, 10);
            }
            // 更新背景纹理
            boxStyle.normal.background = boxTexture;

            if (tabContainerStyle == null)
            {
                // 创建页签容器样式
                tabContainerStyle = new GUIStyle();
            }
            // 更新背景纹理
            tabContainerStyle.normal.background = tabContainerTexture;

            // 标记样式已初始化
            stylesInitialized = true;
        }

        // 从缓存获取纹理，如果不存在则创建
        private Texture2D GetCachedTexture(int width, int height, Color color)
        {
            // 使用颜色的哈希值作为键
            int colorHash = color.GetHashCode();

            // 检查缓存中是否已存在
            if (textureCache.TryGetValue(colorHash, out Texture2D cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            // 创建新纹理
            Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            newTexture.hideFlags = HideFlags.HideAndDontSave; // 防止纹理被保存到场景中

            // 填充颜色 - 优化为一次性设置所有像素
            Color32[] pixels = new Color32[width * height];
            Color32 col32 = color;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = col32;

            newTexture.SetPixels32(pixels);
            newTexture.Apply(false, true); // 不生成mipmap，标记为只读

            // 添加到缓存
            textureCache[colorHash] = newTexture;

            return newTexture;
        }

        // 注意：MakeTex方法已被优化的GetCachedTexture方法替代

        // 添加页签
        protected void AddTab(string tabName, DrawTabDelegate drawer)
        {
            tabNames.Add(tabName);
            tabDrawers.Add(drawer);
            tabImages.Add(null); // 添加空图片占位符
        }

        // 添加图片页签
        protected void AddTab(string tabName, Texture image, DrawTabDelegate drawer)
        {
            tabNames.Add(tabName);
            tabDrawers.Add(drawer);
            tabImages.Add(image);
        }

        // 强制刷新UI
        private void ForceRefreshUI()
        {
            // 清理现有纹理
            CleanupTextures();

            // 重置样式引用，强制在下次绘制时重新创建
            tabButtonStyle = null;
            tabButtonSelectedStyle = null;
            boxStyle = null;
            tabContainerStyle = null;
        }

        // 页签颜色缓存
        private static readonly Color[] tabColors = new Color[] {
            new Color(0.012f, 0.663f, 0.957f, 1.0f), // A页签 #03A9F4
            new Color(0.804f, 0.863f, 0.224f, 1.0f), // B页签 #CDDC39
            new Color(1.0f, 0.757f, 0.027f, 1.0f),   // C页签 #FFC107
            new Color(1.0f, 0.341f, 0.133f, 1.0f),   // D页签 #FF5722
            new Color(0.0f, 0.737f, 0.831f, 1.0f),   // E页签 #00BCD4
            new Color(1.0f, 0.922f, 0.231f, 1.0f),   // F页签 #FFEB3B
            new Color(0.612f, 0.153f, 0.69f, 1.0f),  // G页签 #9C27B0
            new Color(1.0f, 0.322f, 0.322f, 1.0f)    // H页签 #FF5252
        };

        // 页签索引到TabState的映射表，用于快速查找
        private static readonly TabState[] tabStateMap = new TabState[] {
            TabState.TabA, // 索引0 -> TabA
            TabState.TabB, // 索引1 -> TabB
            TabState.TabC, // 索引2 -> TabC
            TabState.TabD, // 索引3 -> TabD
            TabState.TabE, // 索引4 -> TabE
            TabState.TabF, // 索引5 -> TabF
            TabState.TabG, // 索引6 -> TabG
            TabState.TabH  // 索引7 -> TabH
        };

        // 默认颜色
        private static readonly Color selectedTabColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);
        private static readonly Color unselectedTabColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);

        // 更新标签页矩形区域
        private void UpdateTabRects()
        {
            if (tabRects == null || tabRects.Length != tabNames.Count)
            {
                tabRects = new Rect[tabNames.Count];
            }

            float totalWidth = EditorGUIUtility.currentViewWidth - 36;
            float tabWidth = totalWidth / tabNames.Count;
            float currentX = 0;

            for (int i = 0; i < tabNames.Count; i++)
            {
                tabRects[i] = new Rect(currentX, 0, tabWidth, 36);
                currentX += tabWidth;
            }
        }

        // 绘制页签栏
        private void DrawTabs()
        {
            // 检查是否需要初始化样式
            if (!stylesInitialized ||
                (tabContainerStyle != null && tabContainerStyle.normal.background == null))
            {
                InitStyles();
            }

            // 计算每个页签的宽度，实现等分
            float totalWidth = EditorGUIUtility.currentViewWidth - 36; // 减去一点余量防止溢出
            float tabWidth = totalWidth / tabNames.Count; // 等分宽度

            // 更新标签页矩形区域
            UpdateTabRects();

            // 使用带背景的水平布局容器
            GUILayout.BeginHorizontal(tabContainerStyle, GUILayout.Width(totalWidth), GUILayout.ExpandWidth(false));

            // 获取当前事件
            Event currentEvent = Event.current;

            // 只在鼠标按下事件时执行点击检测逻辑
            bool isMouseDown = currentEvent.type == EventType.MouseDown;

            for (int i = 0; i < tabNames.Count; i++)
            {
                GUIStyle style = (i == selectedTabIndex) ? tabButtonSelectedStyle : tabButtonStyle;

                // 使用无边距的按钮，高度适中
                if (tabImages[i] != null)
                {
                    // 获取按钮区域，使用等分宽度
                    Rect rect = GUILayoutUtility.GetRect(tabWidth, 36, GUILayout.Width(tabWidth), GUILayout.Height(36));

                    // 绘制按钮背景
                    GUI.Box(rect, "", (i == selectedTabIndex) ? tabButtonSelectedStyle : tabButtonStyle);

                    // 设置颜色和透明度
                    Color oldColor = GUI.color;

                    // 使用查找表替代if-else链，提高效率
                    bool isEnabled = (i == 0) || (i < tabStateMap.Length && IsTabEnabled(tabStateMap[i]));

                    // 设置颜色
                    if (i < tabColors.Length)
                    {
                        if (isEnabled)
                            GUI.color = tabColors[i];
                        else if (i == selectedTabIndex)
                            GUI.color = selectedTabColor;
                        else
                            GUI.color = unselectedTabColor;
                    }
                    else if (i == selectedTabIndex)
                    {
                        GUI.color = selectedTabColor;
                    }
                    else
                    {
                        GUI.color = unselectedTabColor;
                    }

                    // 绘制图片，居中显示
                    float iconSize = 20;
                    GUI.DrawTexture(new Rect(rect.x + (rect.width - iconSize) / 2, rect.y + (rect.height - iconSize) / 2, iconSize, iconSize), tabImages[i]);

                    // 恢复原来的颜色
                    GUI.color = oldColor;

                    // 检测点击 - 只在鼠标按下事件时检查
                    if (isMouseDown && rect.Contains(currentEvent.mousePosition))
                    {
                        if (selectedTabIndex != i)
                        {
                            selectedTabIndex = i;
                            // 只有当页签真正改变时才标记GUI已更改
                            GUI.changed = true;
                            // 标记内容需要刷新
                            tabContentNeedsRefresh = true;
                        }
                        currentEvent.Use();
                        break; // 找到点击的标签页后立即退出循环
                    }
                }
                else
                {
                    // 否则显示文字按钮
                    if (GUILayout.Button(tabNames[i], style, GUILayout.Height(36)))
                    {
                        if (selectedTabIndex != i)
                        {
                            selectedTabIndex = i;
                            // 只有当页签真正改变时才标记GUI已更改
                            GUI.changed = true;
                            // 标记内容需要刷新
                            tabContentNeedsRefresh = true;
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        // 上次渲染的页签索引
        private int lastRenderedTabIndex = -1;

        // 页签内容是否需要刷新
        private bool tabContentNeedsRefresh = true;

        // 绘制当前选中的页签内容
        private void DrawSelectedTabContent()
        {
            if (selectedTabIndex >= 0 && selectedTabIndex < tabDrawers.Count)
            {
                // 检查是否需要刷新内容
                if (lastRenderedTabIndex != selectedTabIndex || tabContentNeedsRefresh)
                {
                    // 准备当前页签的内容
                    PrepareTabContent(selectedTabIndex);
                    lastRenderedTabIndex = selectedTabIndex;
                    tabContentNeedsRefresh = false;
                }

                // 使用无边距、无背景的垂直布局
                GUILayout.BeginVertical(boxStyle);
                tabDrawers[selectedTabIndex]();
                GUILayout.EndVertical();
            }
        }

        // 准备页签内容
        private void PrepareTabContent(int tabIndex)
        {
            // 这里可以根据页签索引准备内容
            // 例如加载特定的纹理、计算布局等
            // 目前只是一个占位方法，可以根据需要扩展
        }

        // 绘制块标题
        protected void DrawBlockTitle(string title)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        // 清理纹理资源
        private void CleanupTextures()
        {
            // 简化纹理清理逻辑，只需要清空引用
            // 由于我们使用了缓存，实际的纹理对象会在缓存中保留
            tabButtonTexture = null;
            tabButtonSelectedTexture = null;
            boxTexture = null;
            tabContainerTexture = null;

            // 重置样式初始化标志
            stylesInitialized = false;
        }

        // 清理静态缓存
        private static void CleanupStaticCache()
        {
            // 清理纹理缓存
            foreach (var texture in textureCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            textureCache.Clear();

            // 清理EditorPrefs缓存
            CleanupEditorPrefsCache();
        }

        // 上次焦点状态
        private static bool lastFocusState = true;

        // 焦点检测计数器
        private int focusCheckCounter = 0;

        // 初始化标志
        private bool isInitialized = false;

        // 重写OnGUI方法
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // 清空属性缓存
            propertyCache.Clear();

            // 获取当前材质的唯一标识符
            Material material = materialEditor.target as Material;
            if (material == null) return;

            string materialId = AssetDatabase.GetAssetPath(material);

            // 尝试从EditorPrefs中恢复上次选择的页签索引
            string prefKey = TAB_INDEX_PREF_KEY + materialId;

            // 只在布局事件中加载一次
            if (Event.current.type == EventType.Layout)
            {
                // 只在第一次加载或焦点变化时执行
                if (!isInitialized)
                {
                    // 使用缓存的EditorPrefs值
                    selectedTabIndex = GetCachedEditorPrefsInt(prefKey, selectedTabIndex);
                    isInitialized = true;
                }

                // 减少焦点检测频率，只在需要时检查
                focusCheckCounter++;
                if (focusCheckCounter >= 10) // 每10帧检查一次
                {
                    focusCheckCounter = 0;
                    bool hasFocus = EditorWindow.focusedWindow != null;
                    if (hasFocus && !lastFocusState)
                    {
                        // 编辑器从失焦状态恢复，强制刷新UI
                        ForceRefreshUI();
                    }
                    lastFocusState = hasFocus;
                }
            }

            // 保存当前选中的页签索引
            int oldSelectedTabIndex = selectedTabIndex;

            // 绘制页签和内容
            DrawTabs();
            DrawSelectedTabContent();

            // 如果页签索引发生变化，保存到EditorPrefs（使用缓存）
            if (oldSelectedTabIndex != selectedTabIndex)
            {
                SetCachedEditorPrefsInt(prefKey, selectedTabIndex);
            }

            // 检测编辑器退出事件
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Quit")
            {
                CleanupTextures();
            }
        }

        // 析构函数，确保资源被释放
        ~TabShaderGUI_Project()
        {
            CleanupTextures();
        }

        // 静态构造函数，确保在编辑器退出时清理静态资源
        static TabShaderGUI_Project()
        {
            // 注册编辑器退出事件
            EditorApplication.quitting += CleanupStaticCache;
        }

        // 从EditorPrefs获取值，优先使用缓存
        private int GetCachedEditorPrefsInt(string key, int defaultValue)
        {
            // 检查缓存中是否已存在
            if (editorPrefsCache.TryGetValue(key, out int cachedValue))
            {
                return cachedValue;
            }

            // 从EditorPrefs读取值
            int value = EditorPrefs.GetInt(key, defaultValue);

            // 添加到缓存
            editorPrefsCache[key] = value;

            return value;
        }

        // 设置EditorPrefs值，同时更新缓存
        private void SetCachedEditorPrefsInt(string key, int value)
        {
            // 更新EditorPrefs
            EditorPrefs.SetInt(key, value);

            // 更新缓存
            editorPrefsCache[key] = value;
        }

        // 清理EditorPrefs缓存
        private static void CleanupEditorPrefsCache()
        {
            editorPrefsCache.Clear();
        }

        // 优化的查找属性方法，使用缓存
        protected MaterialProperty FindCachedProperty(string name, MaterialProperty[] properties)
        {
            // 检查缓存中是否已存在
            if (propertyCache.TryGetValue(name, out MaterialProperty property))
            {
                return property;
            }

            // 如果不存在，使用基类方法查找
            property = FindProperty(name, properties, false);

            // 如果找到了，添加到缓存
            if (property != null)
            {
                propertyCache[name] = property;
            }

            return property;
        }

        // 检查属性值是否为true (>0.5f)
        protected bool IsPropertyEnabled(string propertyName, MaterialProperty[] properties)
        {
            MaterialProperty property = FindCachedProperty(propertyName, properties);
            return property != null && property.floatValue > 0.5f;
        }

        // 检查页签是否启用
        protected bool IsTabEnabled(TabState tab)
        {
            return (enabledTabs & tab) != 0;
        }

        // 设置单个页签状态
        protected void SetTabState(TabState tab, bool enabled)
        {
            if (enabled)
                enabledTabs |= tab;
            else
                enabledTabs &= ~tab;
        }

        // 批量更新页签状态 - 接受一个字典，键为属性名，值为对应的TabState
        protected void BatchUpdateTabStates(Material material, Dictionary<string, TabState> propertyTabStateMap)
        {
            if (material == null || propertyTabStateMap == null)
                return;

            // 重置TabState，保留TabA
            enabledTabs = TabState.TabA;

            // 遍历字典，根据属性值设置TabState
            foreach (var pair in propertyTabStateMap)
            {
                string propertyName = pair.Key;
                TabState tabState = pair.Value;

                // 检查属性是否启用
                if (material.HasProperty(propertyName) && material.GetFloat(propertyName) > 0.5f)
                {
                    // 启用对应的页签
                    SetTabState(tabState, true);
                }
            }
        }
    }
}
