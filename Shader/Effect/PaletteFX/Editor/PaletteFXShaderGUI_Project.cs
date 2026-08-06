using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

// 确保在编辑器命名空间中
namespace UnityEditor
{
    // TabExampleShader的自定义编辑器
    public class PaletteFXShaderGUI_Project : TabShaderGUI_Project
    {
        // 静态缓存页签图片资源
        private static Texture2D tabImageA;
        private static Texture2D tabImageB;
        private static Texture2D tabImageC;
        private static Texture2D tabImageD;
        private static Texture2D tabImageE;
        private static Texture2D tabImageF;
        private static Texture2D tabImageG;
        private static Texture2D tabImageH;

        // GUI资源的基础路径，可以在运行时更改
        private static string guiBasePath = "Assets/FrameWork/Shader/Effect/PaletteFX/GUI/";

        // 常用UI元素的静态只读对象，减少GC压力
        private static readonly GUIContent dissolveLabel = new GUIContent("溶解度");
        private static readonly GUIContent strengthLabelContent = new GUIContent("方向强度");
        private static readonly GUIContent viewOffsetLabelContent = new GUIContent("视角偏移");
        private static readonly GUIContent coordinateSpaceLabel = new GUIContent("坐标空间", "选择顶点动画的坐标空间");

        // 预分配数组和列表
        private static readonly GUIContent[] blendModeOptions = new GUIContent[] {
            new GUIContent("不透明 (Opaque)"),
            new GUIContent("透明 (Transparent)"),
            new GUIContent("叠加 (Additive)"),
            new GUIContent("透明度裁剪 (Cutout)")
        };

        // 属性名到TabState的映射字典 - 用于批量更新页签状态
        private static readonly Dictionary<string, TabState> propertyTabStateMap = new Dictionary<string, TabState>()
        {
            { "_EnableAddTex", TabState.TabB },
            { "_EnableDissolve", TabState.TabC },
            { "_EnableDistort", TabState.TabD },
            { "_EnableFresnel", TabState.TabE },
            { "_EnableVertexAnim", TabState.TabF },
            { "_EnableCustomData", TabState.TabG },
            { "_EnableStencilTest", TabState.TabH }
        };

        // 静态构造函数，只执行一次
        static PaletteFXShaderGUI_Project()
        {
            // 不再在静态构造函数中加载所有图片，改为懒加载

            // 尝试自动检测GUI资源路径
            AutoDetectGUIPath();
        }

        // 设置GUI资源的基础路径
        public static void SetGUIBasePath(string path)
        {
            // 确保路径以斜杠结尾
            if (!string.IsNullOrEmpty(path) && !path.EndsWith("/"))
            {
                path += "/";
            }

            guiBasePath = path;

            // 清除缓存的图片，以便下次使用时重新加载
            ClearTabImages();
        }

        // 清除缓存的页签图片
        private static void ClearTabImages()
        {
            tabImageA = null;
            tabImageB = null;
            tabImageC = null;
            tabImageD = null;
            tabImageE = null;
            tabImageF = null;
            tabImageG = null;
            tabImageH = null;
        }

        // 自动检测GUI资源路径
        private static void AutoDetectGUIPath()
        {
            // 尝试几种可能的路径
            string[] possiblePaths = new string[]
            {
                "Assets/FrameWork/Shader/Effect/PaletteFX/GUI/",
                "Assets/FrameWork/Shader/Effect/PaletteFX/@GUI/",
                "Assets/GUI/",
                "Assets/@GUI/"
            };

            // 使用一个已知的图片文件名来测试路径
            string testFileName = "ZTT.png";

            foreach (string path in possiblePaths)
            {
                string fullPath = path + testFileName;
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath) != null)
                {
                    // 找到有效路径，静默设置
                    guiBasePath = path;
                    return;
                }
            }

            // 如果没有找到，保留默认路径（不输出警告，因为默认路径通常是正确的）
        }

        // 使用懒加载获取页签图片
        private static Texture2D GetTabImage(ref Texture2D image, string fileName)
        {
            if (image == null)
            {
                // 确保路径正确拼接
                string fullPath = guiBasePath;
                if (!guiBasePath.EndsWith("/"))
                {
                    fullPath += "/";
                }
                fullPath += fileName;

                // 尝试加载图片
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);

                if (image == null)
                {
                    Debug.LogError($"无法加载图片: {fullPath}");
                }
            }
            return image;
        }

        // 构造函数
        public PaletteFXShaderGUI_Project()
        {
            // 添加所有页签，使用懒加载的图片，只传递文件名而不是完整路径
            AddTab("A", GetTabImage(ref tabImageA, "ZTT.png"), DrawMainTab);
            AddTab("B", GetTabImage(ref tabImageB, "FTT.png"), DrawNormalTab);
            AddTab("C", GetTabImage(ref tabImageC, "RJTT.png"), DrawEmissionTab);
            AddTab("D", GetTabImage(ref tabImageD, "NQTT.png"), DrawAdvancedTab);
            AddTab("E", GetTabImage(ref tabImageE, "FNE.png"), DrawEffectTab);
            AddTab("F", GetTabImage(ref tabImageF, "DDDH.png"), DrawDevelopingTabF);
            AddTab("G", GetTabImage(ref tabImageG, "ZDYSJ.png"), DrawDevelopingTabG);
            AddTab("H", GetTabImage(ref tabImageH, "MBJC.png"), DrawDevelopingTabH);
        }

        // 创建辅助方法绘制常见UI元素组合
        private void DrawPropertyWithSlider(MaterialProperty property, string label, float min, float max)
        {
            if (property != null)
            {
                property.floatValue = EditorGUILayout.Slider(label, property.floatValue, min, max);
            }
        }

        // 绘制开关的通用方法 - 优化版本，移除对布尔变量的引用
        private bool DrawToggleWithLabel(MaterialProperty property, string label, TabState tabState = TabState.None)
        {
            EditorGUILayout.BeginHorizontal();

            bool isEnabled = property.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            if (isEnabled != newIsEnabled)
            {
                property.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 如果指定了TabState，则更新它
                if (tabState != TabState.None)
                {
                    SetTabState(tabState, newIsEnabled);
                }

                // 获取当前材质
                Material material = materialEditor.target as Material;
                if (material != null)
                {
                    EditorUtility.SetDirty(material);
                }
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            return newIsEnabled;
        }

        // 绘制Vector3字段的通用方法
        private void DrawVector3Field(GUIContent label, Vector3 value, Action<Vector3> onChange)
        {
            Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            // 使用固定比例分配空间
            float labelWidth = EditorGUIUtility.labelWidth;
            Rect labelRect = new Rect(lineRect.x, lineRect.y, labelWidth, lineRect.height);
            Rect fieldRect = new Rect(lineRect.x + labelWidth, lineRect.y, lineRect.width - labelWidth, lineRect.height);

            // 绘制标签
            EditorGUI.LabelField(labelRect, label);

            // 绘制Vector3字段并处理变更
            EditorGUI.BeginChangeCheck();
            Vector3 newValue = EditorGUI.Vector3Field(fieldRect, GUIContent.none, value);
            if (EditorGUI.EndChangeCheck())
            {
                onChange(newValue);
            }
        }

        // 成员变量存储编辑器引用
        private MaterialEditor materialEditor;
        private MaterialProperty[] properties;

        // 优化的查找属性方法，使用基类的缓存机制
        private MaterialProperty FindCachedProperty(string name)
        {
            return FindCachedProperty(name, properties);
        }

        // 检查属性值是否为true (>0.5f)
        private bool IsPropertyEnabled(string propertyName)
        {
            return IsPropertyEnabled(propertyName, properties);
        }

        // 确保关键字状态与属性值一致
        private void SyncKeywordWithProperty(Material material, string propertyName, string keyword)
        {
            bool isEnabled = IsPropertyEnabled(propertyName);
            bool keywordEnabled = material.IsKeywordEnabled(keyword);

            if (isEnabled && !keywordEnabled)
            {
                material.EnableKeyword(keyword);
            }
            else if (!isEnabled && keywordEnabled)
            {
                material.DisableKeyword(keyword);
            }
        }

        // 批量更新所有关键字
        private void UpdateMaterialKeywords(Material material)
        {
            // 使用字典存储属性名和对应的关键字
            Dictionary<string, string> propertyKeywordMap = new Dictionary<string, string>()
            {
                { "_EnableMask", "_MASK_ON" },
                { "_VACoordinateSpace", "_VAWORLDSPACE_ON" },
                { "_EnableAddTex", "_ADDTEX_ON" },
                { "_EnableAddMask", "_ADDMASK_ON" },
                { "_EnableDissolve", "_DISSOLVE_ON" },
                { "_EnableDissolveAdd", "_DISSOLVEADD_ON" },
                { "_EnableDistort", "_DISTORT_ON" },
                { "_EnableDistortMask", "_DISTORTMASK_ON" },
                { "_EnableFlowMap", "_FLOWMAP_ON" },
                { "_EnableFresnel", "_FRESNEL_ON" },
                { "_EnableVertexAnim", "_VERTEXANIM_ON" },
                { "_EnableVATexMask", "_VATEXMASK_ON" },
                { "_EnableCustomData", "_CUSTOMDATA_ON" },
                { "_EnableStencilTest", "_STENCIL_ON" }
            };

            // 一次性更新所有关键字
            foreach (var pair in propertyKeywordMap)
            {
                SyncKeywordWithProperty(material, pair.Key, pair.Value);
            }
        }

        // 混合模式设置结构体
        private struct BlendModeSettings
        {
            public string renderType;
            public int srcBlend;
            public int dstBlend;
            public int renderQueue;
            public bool alphaTestEnabled;
        }

        // 混合模式设置数组
        private static readonly BlendModeSettings[] blendModeSettingsArray = new BlendModeSettings[]
        {
            // Opaque (0)
            new BlendModeSettings {
                renderType = "",
                srcBlend = (int)UnityEngine.Rendering.BlendMode.One,
                dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry,
                alphaTestEnabled = false
            },
            // Transparent (1)
            new BlendModeSettings {
                renderType = "Transparent",
                srcBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha,
                dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent,
                alphaTestEnabled = false
            },
            // Additive (2)
            new BlendModeSettings {
                renderType = "Transparent",
                srcBlend = (int)UnityEngine.Rendering.BlendMode.One,
                dstBlend = (int)UnityEngine.Rendering.BlendMode.One,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent,
                alphaTestEnabled = false
            },
            // Cutout (3)
            new BlendModeSettings {
                renderType = "TransparentCutout",
                srcBlend = (int)UnityEngine.Rendering.BlendMode.One,
                dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest,
                alphaTestEnabled = true
            }
        };

        // 应用混合模式设置
        // 应用混合模式设置 - 简化版本，移除自定义渲染队列控制
        private void ApplyBlendMode(Material material, int blendMode)
        {
            if (blendMode >= 0 && blendMode < blendModeSettingsArray.Length)
            {
                var settings = blendModeSettingsArray[blendMode];

                material.SetOverrideTag("RenderType", settings.renderType);
                material.SetInt("_BlendMode", blendMode);
                material.SetInt("_SrcBlend", settings.srcBlend);
                material.SetInt("_DstBlend", settings.dstBlend);

                if (EditorGUI.EndChangeCheck())
                {
                    // 自动处理 keyword 切换
                    SetKeyword(material, "_BLENDMODE_CUTOUT", blendMode == 3);
                    SetKeyword(material, "_BLENDMODE_ADDITIVE", blendMode == 2);
                }

                if (settings.alphaTestEnabled)
                    material.EnableKeyword("_ALPHATEST_ON");
                else
                    material.DisableKeyword("_ALPHATEST_ON");
            }
        }

        // 查找属性
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.materialEditor = materialEditor;
            this.properties = properties;

            // 获取当前材质
            Material material = materialEditor.target as Material;
            if (material == null) return;

            // 使用批量更新方法初始化所有页签的状态
            BatchUpdateTabStates(material, propertyTabStateMap);

            // 先绘制渲染模式选项
            DrawRenderModeOptions(materialEditor, properties);

            // 设置混合模式
            int blendMode = material.GetInt("_BlendMode");

            // 批量更新所有关键字
            UpdateMaterialKeywords(material);

            // 使用优化的混合模式设置方法
            ApplyBlendMode(material, blendMode);

            // 再调用基类的OnGUI方法绘制页签
            base.OnGUI(materialEditor, properties);

            // 将渲染选项移动到页签内容区下方
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("渲染选项", EditorStyles.boldLabel);

            // 绘制全局透明度控制 - 使用滑杆
            MaterialProperty globalAlphaProp = FindCachedProperty("_GlobalAlpha");
            if (globalAlphaProp != null)
            {
                globalAlphaProp.floatValue = EditorGUILayout.Slider("全局透明度", globalAlphaProp.floatValue, 0.0f, 1.0f);
            }

            // 绘制全局饱和度控制 - 使用滑杆
            MaterialProperty globalSaturationProp = FindCachedProperty("_GlobalSaturation");
            if (globalSaturationProp != null)
            {
                globalSaturationProp.floatValue = EditorGUILayout.Slider("全局饱和度", globalSaturationProp.floatValue, 0.0f, 2.0f);
            }
            MaterialProperty globalUIClipRectProp = FindCachedProperty("_GlobalUIClipRect");
            if (globalUIClipRectProp != null)
            {
                materialEditor.ShaderProperty(globalUIClipRectProp, "开启UIMask裁剪");
                EditorGUILayout.HelpBox("UIMask是用于UI裁剪，一般有滑条界面会用到Mask或者RectMask2D。\n" +
                                        "- 所以用在UI组件上一般建议开启。\n" +
                                        "- 使用Mask而非RectMask2D功能需要开启模板测试，将比较值设为Equal,模板ID设为1，模板操作设为Keep，掩码为255。\n" +
                                        "- 如果是3D物体就不要勾选，勾上就看不到。", MessageType.Info);
            }

            EditorGUILayout.Space(3);

            // 安全调用RenderQueueField，避免Unity编辑器bug
            try
            {
                materialEditor.RenderQueueField();
            }
            catch (System.NullReferenceException)
            {
                // Unity编辑器已知bug的临时解决方案
                EditorGUILayout.LabelField("渲染队列: " + material.renderQueue.ToString());
            }

            materialEditor.EnableInstancingField();
        }

        // 绘制渲染模式选项
        // 在 DrawRenderModeOptions 方法中添加自定义渲染队列的控制
        private void DrawRenderModeOptions(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染模式", EditorStyles.boldLabel);

            // 绘制混合模式
            MaterialProperty blendModeProp = FindCachedProperty("_BlendMode");
            if (blendModeProp == null) return;

            // 获取当前材质
            Material material = materialEditor.target as Material;
            if (material == null) return;

            // 记录修改前的混合模式值
            int oldBlendMode = (int)blendModeProp.floatValue;

            // 显示混合模式控件
            materialEditor.ShaderProperty(blendModeProp, "透明模式");


            // 检查混合模式是否发生变化
            int newBlendMode = (int)blendModeProp.floatValue;
            if (oldBlendMode != newBlendMode)
            {
                // 根据新的混合模式设置推荐的深度写入值
                bool isOpaqueOrCutout = (newBlendMode == 0 || newBlendMode == 3); // Opaque或Cutout
                material.SetInt("_ZWrite", isOpaqueOrCutout ? 1 : 0); // 设置为ON或OFF

                // 标记材质为已修改
                EditorUtility.SetDirty(material);
            }

            // 如果是Cutout模式，显示透明度裁剪滑杆
            if (newBlendMode == 3) // Cutout
            {
                EditorGUI.indentLevel++;
                MaterialProperty cutoffProp = FindCachedProperty("_Cutoff");
                if (cutoffProp != null)
                {
                    materialEditor.ShaderProperty(cutoffProp, "透明度裁剪阈值");
                }
                EditorGUI.indentLevel--;
            }

            // 绘制深度设置
            MaterialProperty zWriteProp = FindCachedProperty("_ZWrite");
            if (zWriteProp != null)
            {
                materialEditor.ShaderProperty(zWriteProp, "深度写入");
            }

            MaterialProperty zTestProp = FindCachedProperty("_ZTest");
            if (zTestProp != null)
            {
                materialEditor.ShaderProperty(zTestProp, "深度测试");
            }
            MaterialProperty DepthOffsetFactor = FindCachedProperty("_DepthOffsetFactor");
            if (DepthOffsetFactor != null)
            {
                materialEditor.ShaderProperty(DepthOffsetFactor, "深度偏移因子");
            }
            MaterialProperty DepthOffsetUnits = FindCachedProperty("_DepthOffsetUnits");
            if (DepthOffsetUnits != null)
            {
                materialEditor.ShaderProperty(DepthOffsetUnits, "深度偏移单位");
            }

            // 绘制剔除模式
            MaterialProperty cullProp = FindCachedProperty("_Cull");
            if (cullProp != null)
            {
                materialEditor.ShaderProperty(cullProp, "剔除模式");
            }

            EditorGUILayout.Space(15);
        }
        void SetKeyword(Material mat, string keyword, bool enable)
        {
            if (enable)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
        // 绘制主要页签
        private void DrawMainTab()
        {

            // 使用缓存获取所有属性
            MaterialProperty uvMode = FindCachedProperty("_UVMode");
            MaterialProperty mainTexSampleUV = FindCachedProperty("_MainTexSampleUV");

            MaterialProperty mainTexUVMode_Polar = FindCachedProperty("_MainTexUVMode_Polar");
            MaterialProperty mainTexUVMode_Swirl = FindCachedProperty("_MainTexUVMode_Swirl");

            MaterialProperty swirlFactor = FindCachedProperty("_SwirlFactor");
            MaterialProperty mainTex = FindCachedProperty("_MainTex");
            MaterialProperty color = FindCachedProperty("_Color");
            MaterialProperty uspeed = FindCachedProperty("_Uspeed");
            MaterialProperty vspeed = FindCachedProperty("_Vspeed");
            MaterialProperty rotateAngle = FindCachedProperty("_RotateAngle");
            MaterialProperty brightness = FindCachedProperty("_Brightness");
            MaterialProperty clampU = FindCachedProperty("_ClampU");
            MaterialProperty clampV = FindCachedProperty("_ClampV");
            MaterialProperty mainAcceptDistort = FindCachedProperty("_MainAcceptDistort");
            MaterialProperty mainTexChannel = FindCachedProperty("_MainTexChannel");

            // 遮罩相关属性
            MaterialProperty enableMask = FindCachedProperty("_EnableMask");
            MaterialProperty diffuseMaskTex = FindCachedProperty("_DiffuseMaskTex");
            MaterialProperty mainTexMaskSampleUV = FindCachedProperty("_MainTexMaskSampleUV");
            MaterialProperty isReverseMask = FindCachedProperty("_isReverseMask");
            MaterialProperty uSpeedDiffuseM = FindCachedProperty("_USpeed_diffusem");
            MaterialProperty vSpeedDiffuseM = FindCachedProperty("_VSpeed_diffusem");
            MaterialProperty diffuseMaskAng = FindCachedProperty("_DiffuseMaskAng");
            MaterialProperty maskChannel = FindCachedProperty("_MaskChannel");
            MaterialProperty maskUVMode = FindCachedProperty("_MaskUVMode");
            MaterialProperty mainTexMaskUVMode_Polar = FindCachedProperty("_MainTexMaskUVMode_Polar");
            MaterialProperty mainTexMaskUVMode_Swirl = FindCachedProperty("_MainTexMaskUVMode_Swirl");

            MaterialProperty maskSwirlFactor = FindCachedProperty("_MaskSwirlFactor");
            MaterialProperty maskClampU = FindCachedProperty("_MaskClampU");
            MaterialProperty maskClampV = FindCachedProperty("_MaskClampV");

            // 获取当前材质
            Material material = materialEditor.target as Material;
            if (material == null || enableMask == null)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            // ===== 主主贴图设置区域 =====
            EditorGUILayout.LabelField("主贴图设置", EditorStyles.boldLabel);

            if (mainTex != null)
            {
                materialEditor.TextureProperty(mainTex, "主贴图");
            }
            // 遮罩通道
            if (mainTexChannel != null)
            {
                materialEditor.ShaderProperty(mainTexChannel, "通道选择");
            }
            // 采样UV设置
            if (mainTexSampleUV != null)
            {
                materialEditor.ShaderProperty(mainTexSampleUV, "采样UV");
            }

            // UV模式设置
            if (uvMode != null)
            {
                //materialEditor.ShaderProperty(uvMode, "UV模式");
                // 记录旧值以识别变更
                float oldMode = uvMode.floatValue;
                materialEditor.ShaderProperty(uvMode, uvMode.displayName);

                if (EditorGUI.EndChangeCheck())
                {
                    // 自动处理 keyword 切换
                    SetKeyword(material, "_MAINTEXUVMODE_POLAR", uvMode.floatValue == 1);
                    SetKeyword(material, "_MAINTEXUVMODE_SWIRL", uvMode.floatValue == 2);
                }
                // 根据当前选择 UV 模式显示额外参数
                {
                    /*
                    switch ((int)uvMode.floatValue)
                    {
                       case 1: // Polar
                           EditorGUILayout.HelpBox("使用极坐标投影方式。", MessageType.Info);
                           break;

                       case 2: // Swirl
                           EditorGUILayout.HelpBox("使用漩涡UV变换。", MessageType.Info);
                           if (swirlFactor != null)
                               materialEditor.ShaderProperty(swirlFactor, swirlFactor.displayName);
                           break;

                       case 0: // Normal
                       default:
                           EditorGUILayout.HelpBox("使用普通UV动画方式。", MessageType.None);
                           break;
                    }*/
                }
                // 在相应的UV模式设置后添加中心点控制
                if (uvMode.floatValue == 1)
                {
                    MaterialProperty polarCenter = FindCachedProperty("_MainTexPolarCenter");
                    if (polarCenter != null)
                    {
                        Vector4 center = polarCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                        polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                }
                if (uvMode.floatValue == 2) // 漩涡模式
                {
                    MaterialProperty swirlCenter = FindCachedProperty("_MainTexSwirlCenter");
                    if (swirlCenter != null)
                    {
                        Vector4 center = swirlCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                        swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                    if (swirlFactor != null)
                    {
                        materialEditor.ShaderProperty(swirlFactor, "漩涡强度");
                    }
                }
            }

            // 旋转设置
            if (rotateAngle != null)
            {
                materialEditor.ShaderProperty(rotateAngle, "旋转角度");
            }

            // UV限制设置
            if (clampU != null)
            {
                materialEditor.ShaderProperty(clampU, "UClamp");
            }

            if (clampV != null)
            {
                materialEditor.ShaderProperty(clampV, "VClamp");
            }

            // 颜色和亮度
            if (color != null)
            {
                materialEditor.ShaderProperty(color, "颜色");
            }

            if (brightness != null)
            {
                materialEditor.ShaderProperty(brightness, "亮度");
            }

            // UV动画
            if (uspeed != null)
            {
                materialEditor.ShaderProperty(uspeed, "USpeed");
            }

            if (vspeed != null)
            {
                materialEditor.ShaderProperty(vspeed, "VSpeed");
            }
            if (mainAcceptDistort != null)
            {
                materialEditor.ShaderProperty(mainAcceptDistort, "接受扭曲");
            }
            // ===== 遮罩设置区域 =====
            EditorGUILayout.Space();

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 使用开关控件替代默认的ShaderProperty
            bool isEnabled = enableMask.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isEnabled != newIsEnabled)
            {
                // 先更新属性值
                enableMask.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsEnabled)
                    {
                        material.EnableKeyword("_MASK_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_MASK_ON");
                    }

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用遮罩", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 只有启用遮罩时才显示其他选项
            if (isEnabled)
            {
                // 遮罩贴图
                if (diffuseMaskTex != null)
                {
                    materialEditor.TextureProperty(diffuseMaskTex, "遮罩贴图");
                }

                // 遮罩通道
                if (maskChannel != null)
                {
                    materialEditor.ShaderProperty(maskChannel, "通道选择");
                }

                // 遮罩采样UV设置
                if (mainTexMaskSampleUV != null)
                {
                    materialEditor.ShaderProperty(mainTexMaskSampleUV, "采样UV");
                }

                // 遮罩UV模式
                if (maskUVMode != null)
                {
                    //materialEditor.ShaderProperty(uvMode, "UV模式");
                    materialEditor.ShaderProperty(maskUVMode, maskUVMode.displayName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 自动处理 keyword 切换
                        SetKeyword(material, "_MAINTEXMASKUVMODE_POLAR", maskUVMode.floatValue == 1);
                        SetKeyword(material, "_MAINTEXMASKUVMODE_SWIRL", maskUVMode.floatValue == 2);
                    }
                    // 在相应的UV模式设置后添加中心点控制
                    if (maskUVMode.floatValue == 1)
                    {
                        MaterialProperty polarCenter = FindCachedProperty("_MainTexMaskPolarCenter");
                        if (polarCenter != null)
                        {
                            Vector4 center = polarCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                            polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                    }

                    if (maskUVMode.floatValue == 2) // 漩涡模式
                    {
                        MaterialProperty swirlCenter = FindCachedProperty("_MainTexMaskSwirlCenter");
                        if (swirlCenter != null)
                        {
                            Vector4 center = swirlCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                            swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                        if (maskSwirlFactor != null)
                        {
                            materialEditor.ShaderProperty(maskSwirlFactor, "漩涡强度");
                        }
                    }
                }

                // 遮罩旋转
                if (diffuseMaskAng != null)
                {
                    materialEditor.ShaderProperty(diffuseMaskAng, "旋转角度");
                }

                // 遮罩UV限制
                if (maskClampU != null)
                {
                    materialEditor.ShaderProperty(maskClampU, "UClamp");
                }

                if (maskClampV != null)
                {
                    materialEditor.ShaderProperty(maskClampV, "VClamp");
                }

                // 使用滑杆控件显示反转属性
                if (isReverseMask != null)
                {
                    isReverseMask.floatValue = EditorGUILayout.Slider("反转程度", isReverseMask.floatValue, 0.0f, 1.0f);
                }

                // 接受扭曲开关
                MaterialProperty maskAcceptDistort = FindCachedProperty("_MaskAcceptDistort");
                if (maskAcceptDistort != null)
                {
                    materialEditor.ShaderProperty(maskAcceptDistort, "接受扭曲");
                }

                // 遮罩UV动画
                if (uSpeedDiffuseM != null)
                {
                    materialEditor.ShaderProperty(uSpeedDiffuseM, "USpeed");
                }

                if (vSpeedDiffuseM != null)
                {
                    materialEditor.ShaderProperty(vSpeedDiffuseM, "VSpeed");
                }
            }
        }

        // 绘制附加图页签
        private void DrawNormalTab()
        {
            // 获取所有属性
            MaterialProperty enableAddTex = FindProperty("_EnableAddTex", properties);
            MaterialProperty addTexChannel = FindProperty("_AddTexChannel", properties);
            MaterialProperty addUVMode = FindProperty("_AddUVMode", properties);
            MaterialProperty addMulMainAlpha = FindProperty("_AddMulMainAlpha", properties);
            MaterialProperty addTexUVMode_Polar = FindProperty("_AddTexUVMode_Polar", properties);
            MaterialProperty addTexUVMode_Swirl = FindProperty("_AddTexUVMode_Swirl", properties);
            MaterialProperty addSwirlFactor = FindProperty("_AddSwirlFactor", properties);

            MaterialProperty addTex = FindProperty("_AddTex", properties);
            MaterialProperty addTexSampleUV = FindProperty("_AddTexSampleUV", properties);
            MaterialProperty addColor = FindProperty("_AddColor", properties);
            MaterialProperty addUspeed = FindProperty("_AddUspeed", properties);
            MaterialProperty addVspeed = FindProperty("_AddVspeed", properties);
            MaterialProperty addRotateAngle = FindProperty("_AddRotateAngle", properties);
            MaterialProperty addBrightness = FindProperty("_AddBrightness", properties);

            MaterialProperty addClampU = FindProperty("_AddClampU", properties);
            MaterialProperty addClampV = FindProperty("_AddClampV", properties);
            MaterialProperty addBlendMode = FindProperty("_AddBlendMode", properties);
            MaterialProperty addBlendMode_Additive = FindProperty("_AddBlendMode_Additive", properties);
            MaterialProperty addBlendMode_Multiply = FindProperty("_AddBlendMode_Multiply", properties);


            // 遮罩相关属性
            MaterialProperty enableAddMask = FindProperty("_EnableAddMask", properties);
            MaterialProperty addTexMask = FindProperty("_AddTexMask", properties);
            MaterialProperty addTexMaskSampleUV = FindProperty("_AddTexMaskSampleUV", properties);
            MaterialProperty addIsReverseMask = FindProperty("_AddIsReverseMask", properties);
            MaterialProperty addUSpeedMask = FindProperty("_AddUSpeed_mask", properties);
            MaterialProperty addVSpeedMask = FindProperty("_AddVSpeed_mask", properties);
            MaterialProperty addMaskRotateAngle = FindProperty("_AddMaskRotateAngle", properties);
            MaterialProperty addMaskChannel = FindProperty("_AddMaskChannel", properties);
            MaterialProperty addMaskUVMode = FindProperty("_AddMaskUVMode", properties);
            MaterialProperty addTexMaskUVMode_Polar = FindProperty("_AddTexMaskUVMode_Polar", properties);
            MaterialProperty addTexMaskUVMode_Swirl = FindProperty("_AddTexMaskUVMode_Swirl", properties);
            MaterialProperty addMaskSwirlFactor = FindProperty("_AddMaskSwirlFactor", properties);
            MaterialProperty addMaskUseMainTexR = FindProperty("_AddMaskUseMainTexR", properties, false);
            MaterialProperty addMaskContrast = FindProperty("_AddMaskContrast", properties, false);
            MaterialProperty addMaskClampU = FindProperty("_AddMaskClampU", properties);
            MaterialProperty addMaskClampV = FindProperty("_AddMaskClampV", properties);

            // ===== 附加图设置区域 =====
            DrawBlockTitle("附加图");

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 获取当前材质
            Material material = materialEditor.target as Material;

            // 使用开关控件替代默认的ShaderProperty
            bool isEnabled = enableAddTex.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isEnabled != newIsEnabled)
            {
                enableAddTex.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabB, newIsEnabled);

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsEnabled)
                    {
                        material.EnableKeyword("_ADDTEX_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_ADDTEX_ON");

                        // 当关闭一级开关时，同时关闭二级开关（附加图遮罩）
                        if (enableAddMask.floatValue > 0.5f)
                        {
                            enableAddMask.floatValue = 0.0f;
                            material.DisableKeyword("_ADDMASK_ON");
                        }
                    }

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用附加图", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在附加图未启用时禁用控件
            bool isAddTexEnabled = enableAddTex.floatValue > 0.5f;

            EditorGUI.BeginDisabledGroup(!isAddTexEnabled);

            // 混合模式
            materialEditor.ShaderProperty(addBlendMode, "混合模式");
            if (EditorGUI.EndChangeCheck())
            {
                // 自动处理 keyword 切换
                SetKeyword(material, "_ADDBLENDMODE_ADDITIVE", addBlendMode.floatValue == 1);
                SetKeyword(material, "_ADDBLENDMODE_MULTIPLY", addBlendMode.floatValue == 2);
            }
            if (addMulMainAlpha != null)
            {
                materialEditor.ShaderProperty(addMulMainAlpha, "跟随主透明度");
            }

            materialEditor.TextureProperty(addTex, "附加图");

            // 附加图采样UV设置
            if (addTexSampleUV != null)
            {
                materialEditor.ShaderProperty(addTexSampleUV, "采样UV");
            }
            if (addTexChannel != null)
            {
                materialEditor.ShaderProperty(addTexChannel, "通道选择");
            }
            // UV模式设置
            if (addUVMode != null)
            {
                materialEditor.ShaderProperty(addUVMode, addUVMode.displayName);

                if (EditorGUI.EndChangeCheck())
                {
                    // 自动处理 keyword 切换
                    SetKeyword(material, "_ADDTEXUVMODE_POLAR", addUVMode.floatValue == 1);
                    SetKeyword(material, "_ADDTEXUVMODE_SWIRL", addUVMode.floatValue == 2);
                }
                // 在相应的UV模式设置后添加中心点控制
                if (addUVMode.floatValue == 1)
                {
                    MaterialProperty polarCenter = FindCachedProperty("_AddTexPolarCenter");
                    if (polarCenter != null)
                    {
                        Vector4 center = polarCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                        polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                }
                if (addUVMode.floatValue == 2) // 漩涡模式
                {
                    MaterialProperty swirlCenter = FindCachedProperty("_AddTexSwirlCenter");
                    if (swirlCenter != null)
                    {
                        Vector4 center = swirlCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                        swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                    if (addSwirlFactor != null)
                    {
                        materialEditor.ShaderProperty(addSwirlFactor, "漩涡强度");
                    }
                }
            }

            // 旋转设置
            materialEditor.ShaderProperty(addRotateAngle, "旋转角度");

            // UV限制设置
            materialEditor.ShaderProperty(addClampU, "UClamp");
            materialEditor.ShaderProperty(addClampV, "VClamp");

            // 颜色和亮度
            materialEditor.ShaderProperty(addColor, "颜色");
            materialEditor.ShaderProperty(addBrightness, "亮度");

            // Extra alpha mask (multiply by main texture R)
            if (addMaskUseMainTexR != null)
            {
                materialEditor.ShaderProperty(addMaskUseMainTexR, "叠加主帖图R通道");
                if (addMaskUseMainTexR.floatValue > 0.5f && addMaskContrast != null)
                {
                    materialEditor.ShaderProperty(addMaskContrast, "遮罩对比度(色阶)");
                }
            }

            // 接受扭曲开关
            MaterialProperty addAcceptDistort = FindProperty("_AddAcceptDistort", properties);
            materialEditor.ShaderProperty(addAcceptDistort, "接受扭曲");

            // UV动画
            materialEditor.ShaderProperty(addUspeed, "USpeed");
            materialEditor.ShaderProperty(addVspeed, "VSpeed");

            // ===== 遮罩设置区域 =====
            EditorGUILayout.Space();

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 使用开关控件替代默认的ShaderProperty
            bool isMaskEnabled = enableAddMask.floatValue > 0.5f;
            bool newIsMaskEnabled = EditorGUILayout.Toggle(isMaskEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isMaskEnabled != newIsMaskEnabled)
            {
                enableAddMask.floatValue = newIsMaskEnabled ? 1.0f : 0.0f;

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsMaskEnabled)
                        material.EnableKeyword("_ADDMASK_ON");
                    else
                        material.DisableKeyword("_ADDMASK_ON");

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用附加图遮罩", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 只有在遮罩启用时才显示遮罩相关控件
            if (isMaskEnabled)
            {
                // 遮罩贴图
                materialEditor.TextureProperty(addTexMask, "遮罩贴图");

                // 遮罩通道
                materialEditor.ShaderProperty(addMaskChannel, "通道选择");

                // 遮罩采样UV设置
                if (addTexMaskSampleUV != null)
                {
                    materialEditor.ShaderProperty(addTexMaskSampleUV, "采样UV");
                }

                // 遮罩UV模式
                if (addMaskUVMode != null)
                {
                    materialEditor.ShaderProperty(addMaskUVMode, addMaskUVMode.displayName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 自动处理 keyword 切换
                        SetKeyword(material, "_ADDTEXMASKUVMODE_POLAR", addMaskUVMode.floatValue == 1);
                        SetKeyword(material, "_ADDTEXMASKUVMODE_SWIRL", addMaskUVMode.floatValue == 2);
                    }
                    // 在相应的UV模式设置后添加中心点控制
                    if (addMaskUVMode.floatValue == 1)
                    {
                        MaterialProperty polarCenter = FindCachedProperty("_AddTexMaskPolarCenter");
                        if (polarCenter != null)
                        {
                            Vector4 center = polarCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                            polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }

                    }
                    if (addMaskUVMode.floatValue == 2) // 漩涡模式
                    {
                        MaterialProperty swirlCenter = FindCachedProperty("_AddTexMaskSwirlCenter");
                        if (swirlCenter != null)
                        {
                            Vector4 center = swirlCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                            swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                        if (addMaskSwirlFactor != null)
                        {
                            materialEditor.ShaderProperty(addMaskSwirlFactor, "漩涡强度");
                        }
                    }
                }


                // 遮罩旋转
                materialEditor.ShaderProperty(addMaskRotateAngle, "旋转角度");

                // 遮罩UV限制
                materialEditor.ShaderProperty(addMaskClampU, "UClamp");
                materialEditor.ShaderProperty(addMaskClampV, "VClamp");

                // 使用滑杆控件显示反转属性
                addIsReverseMask.floatValue = EditorGUILayout.Slider("反转程度", addIsReverseMask.floatValue, 0.0f, 1.0f);

                // 接受扭曲开关
                MaterialProperty addMaskAcceptDistort = FindProperty("_AddMaskAcceptDistort", properties);
                materialEditor.ShaderProperty(addMaskAcceptDistort, "接受扭曲");

                // 遮罩UV动画
                materialEditor.ShaderProperty(addUSpeedMask, "USpeed");
                materialEditor.ShaderProperty(addVSpeedMask, "VSpeed");
            }

            // 结束附加图相关控件的禁用组
            EditorGUI.EndDisabledGroup();
        }

        // 绘制溶解页签
        private void DrawEmissionTab()
        {
            DrawBlockTitle("溶解");

            // 获取溶解相关属性
            MaterialProperty enableDissolve = FindProperty("_EnableDissolve", properties);
            MaterialProperty dissolveTex = FindProperty("_DissolveTex", properties);
            MaterialProperty dissolveTexSampleUV = FindProperty("_DissolveTexSampleUV", properties);
            MaterialProperty dissolveUVMode = FindProperty("_DissolveUVMode", properties);
            MaterialProperty dissolveTexUVMode_Polar = FindProperty("_DissolveTexUVMode_Polar", properties);
            MaterialProperty dissolveTexUVMode_Swirl = FindProperty("_DissolveTexUVMode_Swirl", properties);

            MaterialProperty dissolveSwirlFactor = FindProperty("_DissolveSwirlFactor", properties);

            MaterialProperty dissolveRotateAngle = FindProperty("_DissolveRotateAngle", properties);
            MaterialProperty dissolveClampU = FindProperty("_DissolveClampU", properties);
            MaterialProperty dissolveClampV = FindProperty("_DissolveClampV", properties);
            MaterialProperty dissolveColor = FindProperty("_DissolveColor", properties);
            MaterialProperty dissolveEdgeIntensity = FindProperty("_DissolveEdgeIntensity", properties);
            MaterialProperty dissolveTexChannel = FindProperty("_DissolveTexChannel", properties);
            MaterialProperty dissolveUspeed = FindProperty("_DissolveUspeed", properties);
            MaterialProperty dissolveVspeed = FindProperty("_DissolveVspeed", properties);
            MaterialProperty dissolveInvert = FindProperty("_DissolveInvert", properties);
            MaterialProperty dissolveFactor = FindProperty("_DissolveFactor", properties);
            MaterialProperty dissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", properties);
            MaterialProperty dissolveEdgeSmoothness = FindProperty("_DissolveEdgeSmoothness", properties);

            // 溶解附加图属性
            MaterialProperty enableDissolveAdd = FindProperty("_EnableDissolveAdd", properties);
            MaterialProperty dissolveAddTex = FindProperty("_DissolveAddTex", properties);
            MaterialProperty dissolveAddTexSampleUV = FindProperty("_DissolveAddTexSampleUV", properties);
            MaterialProperty dissolveAddUVMode = FindProperty("_DissolveAddUVMode", properties);
            MaterialProperty dissolveTexAddUVMode_Polar = FindProperty("_DissolveTexAddUVMode_Polar", properties);
            MaterialProperty dissolveTexAddUVMode_Swirl = FindProperty("_DissolveTexAddUVMode_Swirl", properties);
            MaterialProperty dissolveAddSwirlFactor = FindProperty("_DissolveAddSwirlFactor", properties);

            MaterialProperty dissolveAddRotateAngle = FindProperty("_DissolveAddRotateAngle", properties);
            MaterialProperty dissolveAddClampU = FindProperty("_DissolveAddClampU", properties);
            MaterialProperty dissolveAddClampV = FindProperty("_DissolveAddClampV", properties);
            MaterialProperty dissolveAddTexChannel = FindProperty("_DissolveAddTexChannel", properties);
            MaterialProperty dissolveAddUspeed = FindProperty("_DissolveAddUspeed", properties);
            MaterialProperty dissolveAddVspeed = FindProperty("_DissolveAddVspeed", properties);
            MaterialProperty dissolveAddInvert = FindProperty("_DissolveAddInvert", properties);

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 获取当前材质
            Material material = materialEditor.target as Material;

            // 使用开关控件替代默认的ShaderProperty
            bool isEnabled = enableDissolve.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isEnabled != newIsEnabled)
            {
                enableDissolve.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabC, newIsEnabled);

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsEnabled)
                    {
                        material.EnableKeyword("_DISSOLVE_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_DISSOLVE_ON");

                        // 当关闭一级开关时，同时关闭二级开关（启用溶解附加图）
                        if (enableDissolveAdd.floatValue > 0.5f)
                        {
                            enableDissolveAdd.floatValue = 0.0f;
                            material.DisableKeyword("_DISSOLVEADD_ON");
                        }
                    }

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用溶解", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在溶解未启用时禁用控件
            bool isDissolveEnabled = enableDissolve.floatValue > 0.5f;

            EditorGUI.BeginDisabledGroup(!isDissolveEnabled);

            // 溶解贴图
            materialEditor.TextureProperty(dissolveTex, "溶解贴图");

            materialEditor.ShaderProperty(dissolveTexChannel, "通道选择");

            // 溶解贴图采样UV设置
            if (dissolveTexSampleUV != null)
            {
                materialEditor.ShaderProperty(dissolveTexSampleUV, "采样UV");
            }

            // UV模式设置
            if (dissolveUVMode != null)
            {
                materialEditor.ShaderProperty(dissolveUVMode, dissolveUVMode.displayName);

                if (EditorGUI.EndChangeCheck())
                {
                    // 自动处理 keyword 切换
                    SetKeyword(material, "_DISSOLVETEXUVMODE_POLAR", dissolveUVMode.floatValue == 1);
                    SetKeyword(material, "_DISSOLVETEXUVMODE_SWIRL", dissolveUVMode.floatValue == 2);
                }
                // 在相应的UV模式设置后添加中心点控制
                if (dissolveUVMode.floatValue == 1)
                {
                    MaterialProperty polarCenter = FindCachedProperty("_DissolveTexPolarCenter");
                    if (polarCenter != null)
                    {
                        Vector4 center = polarCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                        polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                }
                if (dissolveUVMode.floatValue == 2) // 漩涡模式
                {
                    MaterialProperty swirlCenter = FindCachedProperty("_DissolveTexSwirlCenter");
                    if (swirlCenter != null)
                    {
                        Vector4 center = swirlCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                        swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                    if (dissolveSwirlFactor != null)
                    {
                        materialEditor.ShaderProperty(dissolveSwirlFactor, "漩涡强度");
                    }
                }
            }

            // 旋转设置
            materialEditor.ShaderProperty(dissolveRotateAngle, "旋转角度");

            // UV限制设置
            materialEditor.ShaderProperty(dissolveClampU, "UClamp");
            materialEditor.ShaderProperty(dissolveClampV, "VClamp");

            // 反转程度
            dissolveInvert.floatValue = EditorGUILayout.Slider("反转程度", dissolveInvert.floatValue, 0.0f, 1.0f);

            // UV动画
            materialEditor.ShaderProperty(dissolveUspeed, "USpeed");
            materialEditor.ShaderProperty(dissolveVspeed, "VSpeed");

            // 溶解参数 - 精简后的参数
            materialEditor.ShaderProperty(dissolveColor, "边缘颜色");
            materialEditor.ShaderProperty(dissolveEdgeIntensity, "边缘亮度");

            // 使用自定义方法绘制溶解度控件，保留拖拽功能但不限制范围
            Rect controlRect = EditorGUILayout.GetControlRect();

            // 使用MaterialEditor的内部方法绘制浮点数字段
            // 这会保留Unity标准输入框的所有功能，包括鼠标拖拽
            materialEditor.ShaderProperty(controlRect, dissolveFactor, dissolveLabel.text);

            materialEditor.ShaderProperty(dissolveEdgeWidth, "边缘宽度");
            materialEditor.ShaderProperty(dissolveEdgeSmoothness, "边缘平滑度");



            // ===== 溶解附加图设置区域 =====
            EditorGUILayout.Space();

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 使用开关控件替代默认的ShaderProperty
            bool isAddEnabled = enableDissolveAdd.floatValue > 0.5f;
            bool newIsAddEnabled = EditorGUILayout.Toggle(isAddEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isAddEnabled != newIsAddEnabled)
            {
                // 先更新属性值
                enableDissolveAdd.floatValue = newIsAddEnabled ? 1.0f : 0.0f;

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsAddEnabled)
                        material.EnableKeyword("_DISSOLVEADD_ON");
                    else
                        material.DisableKeyword("_DISSOLVEADD_ON");

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用溶解附加图", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 只有在溶解附加图启用时才显示相关控件
            bool isDissolveAddEnabled = enableDissolveAdd.floatValue > 0.5f;
            if (isDissolveAddEnabled)
            {
                // 溶解附加图
                materialEditor.TextureProperty(dissolveAddTex, "溶解附加图");

                materialEditor.ShaderProperty(dissolveAddTexChannel, "通道选择");

                // 溶解附加图采样UV设置
                if (dissolveAddTexSampleUV != null)
                {
                    materialEditor.ShaderProperty(dissolveAddTexSampleUV, "采样UV");
                }

                // UV模式设置
                if (dissolveAddUVMode != null)
                {
                    materialEditor.ShaderProperty(dissolveAddUVMode, dissolveAddUVMode.displayName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 自动处理 keyword 切换
                        SetKeyword(material, "_DISSOLVETEXADDUVMODE_POLAR", dissolveAddUVMode.floatValue == 1);
                        SetKeyword(material, "_DISSOLVETEXADDUVMODE_SWIRL", dissolveAddUVMode.floatValue == 2);
                    }
                    // 在相应的UV模式设置后添加中心点控制
                    if (dissolveAddUVMode.floatValue == 1)
                    {
                        MaterialProperty polarCenter = FindCachedProperty("_DissolveAddTexPolarCenter");
                        if (polarCenter != null)
                        {
                            Vector4 center = polarCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                            polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }

                    }
                    if (dissolveAddUVMode.floatValue == 2) // 漩涡模式
                    {
                        MaterialProperty swirlCenter = FindCachedProperty("_DissolveAddTexSwirlCenter");
                        if (swirlCenter != null)
                        {
                            Vector4 center = swirlCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                            swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                        if (dissolveAddSwirlFactor != null)
                        {
                            materialEditor.ShaderProperty(dissolveAddSwirlFactor, "漩涡强度");
                        }
                    }
                }

                // 旋转设置
                materialEditor.ShaderProperty(dissolveAddRotateAngle, "旋转角度");

                // UV限制设置
                materialEditor.ShaderProperty(dissolveAddClampU, "UClamp");
                materialEditor.ShaderProperty(dissolveAddClampV, "VClamp");

                // 反转程度
                dissolveAddInvert.floatValue = EditorGUILayout.Slider("反转程度", dissolveAddInvert.floatValue, 0.0f, 1.0f);

                // UV动画
                materialEditor.ShaderProperty(dissolveAddUspeed, "USpeed");
                materialEditor.ShaderProperty(dissolveAddVspeed, "VSpeed");
            }

            // 结束溶解相关控件的禁用组
            EditorGUI.EndDisabledGroup();
        }

        // 绘制高级页签
        private void DrawAdvancedTab()
        {
            // 获取所有扭曲相关属性
            MaterialProperty enableDistort = FindProperty("_EnableDistort", properties);
            MaterialProperty distortTex = FindProperty("_DistortTex", properties);
            MaterialProperty distortTexSampleUV = FindProperty("_DistortTexSampleUV", properties);
            MaterialProperty distortTexChannel = FindProperty("_DistortTexChannel", properties);
            MaterialProperty distortUVMode = FindProperty("_DistortUVMode", properties);
            MaterialProperty distortTexUVMode_Polar = FindProperty("_DistortTexUVMode_Polar", properties);
            MaterialProperty distortTexUVMode_Swirl = FindProperty("_DistortTexUVMode_Swirl", properties);


            MaterialProperty distortSwirlFactor = FindProperty("_DistortSwirlFactor", properties);

            MaterialProperty distortRotateAngle = FindProperty("_DistortRotateAngle", properties);
            MaterialProperty distortClampU = FindProperty("_DistortClampU", properties);
            MaterialProperty distortClampV = FindProperty("_DistortClampV", properties);
            MaterialProperty distortUspeed = FindProperty("_DistortUspeed", properties);
            MaterialProperty distortVspeed = FindProperty("_DistortVspeed", properties);
            MaterialProperty distortInvert = FindProperty("_DistortInvert", properties);
            MaterialProperty distortStrengthX = FindProperty("_DistortStrengthX", properties);
            MaterialProperty distortStrengthY = FindProperty("_DistortStrengthY", properties);

            // 扭曲遮罩相关属性
            MaterialProperty enableDistortMask = FindProperty("_EnableDistortMask", properties);
            MaterialProperty distortMaskTex = FindProperty("_DistortMaskTex", properties);
            MaterialProperty distortMaskTexSampleUV = FindProperty("_DistortMaskTexSampleUV", properties);
            MaterialProperty distortMaskChannel = FindProperty("_DistortMaskChannel", properties);
            MaterialProperty distortMaskUVMode = FindProperty("_DistortMaskUVMode", properties);
            MaterialProperty distortTexMaskUVMode_Polar = FindProperty("_DistortTexMaskUVMode_Polar", properties);
            MaterialProperty distortTexMaskUVMode_Swirl = FindProperty("_DistortTexMaskUVMode_Swirl", properties);
            MaterialProperty distortMaskSwirlFactor = FindProperty("_DistortMaskSwirlFactor", properties);

            MaterialProperty distortMaskRotateAngle = FindProperty("_DistortMaskRotateAngle", properties);
            MaterialProperty distortMaskClampU = FindProperty("_DistortMaskClampU", properties);
            MaterialProperty distortMaskClampV = FindProperty("_DistortMaskClampV", properties);
            MaterialProperty distortMaskUspeed = FindProperty("_DistortMaskUspeed", properties);
            MaterialProperty distortMaskVspeed = FindProperty("_DistortMaskVspeed", properties);
            MaterialProperty distortMaskInvert = FindProperty("_DistortMaskInvert", properties);

            // FlowMap相关属性
            MaterialProperty enableFlowMap = FindProperty("_EnableFlowMap", properties);
            MaterialProperty flowMapIntensity = FindProperty("_FlowMapIntensity", properties);
            MaterialProperty flowMapSpeed = FindProperty("_FlowMapSpeed", properties);
            MaterialProperty enableFlowMapLTG = FindProperty("_EnableFlowMapLTG", properties);

            // ===== 扭曲贴图设置区域 =====
            DrawBlockTitle("扭曲效果");

            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 获取当前材质
            Material material = materialEditor.target as Material;

            // 使用开关控件替代默认的ShaderProperty
            bool isEnabled = enableDistort.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isEnabled != newIsEnabled)
            {
                enableDistort.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabD, newIsEnabled);

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsEnabled)
                    {
                        material.EnableKeyword("_DISTORT_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_DISTORT_ON");

                        // 当关闭一级开关时，同时关闭二级开关（启用扭曲遮罩）
                        if (enableDistortMask.floatValue > 0.5f)
                        {
                            enableDistortMask.floatValue = 0.0f;
                            material.DisableKeyword("_DISTORTMASK_ON");
                        }

                        // 同时关闭FlowMap模式
                        if (enableFlowMap.floatValue > 0.5f)
                        {
                            enableFlowMap.floatValue = 0.0f;
                            material.DisableKeyword("_FLOWMAP_ON");
                        }
                    }

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用扭曲效果", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在扭曲未启用时禁用控件
            bool isDistortEnabled = enableDistort.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isDistortEnabled);

            // 扭曲贴图
            materialEditor.TextureProperty(distortTex, "扭曲贴图");

            // 获取FlowMap模式的状态
            bool isFlowMapEnabled = enableFlowMap.floatValue > 0.5f;

            // 只有在FlowMap模式未启用时显示这些控件
            if (!isFlowMapEnabled)
            {
                // 扭曲贴图通道选择
                materialEditor.ShaderProperty(distortTexChannel, "通道选择");

                // 扭曲贴图采样UV设置
                if (distortTexSampleUV != null)
                {
                    materialEditor.ShaderProperty(distortTexSampleUV, "采样UV");
                }

                // UV模式设置
                if (distortUVMode != null)
                {
                    materialEditor.ShaderProperty(distortUVMode, distortUVMode.displayName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 自动处理 keyword 切换
                        SetKeyword(material, "_DISTORTTEXUVMODE_POLAR", distortUVMode.floatValue == 1);
                        SetKeyword(material, "_DISTORTTEXUVMODE_SWIRL", distortUVMode.floatValue == 2);
                    }
                    // 在相应的UV模式设置后添加中心点控制
                    if (distortUVMode.floatValue == 1)
                    {
                        MaterialProperty polarCenter = FindCachedProperty("_DistortTexPolarCenter");
                        if (polarCenter != null)
                        {
                            Vector4 center = polarCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                            polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                    }
                    if (distortUVMode.floatValue == 2) // 漩涡模式
                    {
                        MaterialProperty swirlCenter = FindCachedProperty("_DistortTexSwirlCenter");
                        if (swirlCenter != null)
                        {
                            Vector4 center = swirlCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                            swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                        if (distortSwirlFactor != null)
                        {
                            materialEditor.ShaderProperty(distortSwirlFactor, "漩涡强度");
                        }
                    }
                }

                // 旋转设置
                materialEditor.ShaderProperty(distortRotateAngle, "旋转角度");

                // UV限制设置
                materialEditor.ShaderProperty(distortClampU, "UClamp");
                materialEditor.ShaderProperty(distortClampV, "VClamp");


                // UV动画
                materialEditor.ShaderProperty(distortUspeed, "USpeed");
                materialEditor.ShaderProperty(distortVspeed, "VSpeed");

                // 使用滑杆控件显示反转程度属性
                distortInvert.floatValue = EditorGUILayout.Slider("反转程度", distortInvert.floatValue, 0.0f, 1.0f);

                // 扭曲强度
                materialEditor.ShaderProperty(distortStrengthX, "X方向强度");
                materialEditor.ShaderProperty(distortStrengthY, "Y方向强度");
            }

            // FlowMap设置 - 移到右侧作为功能性开关
            EditorGUILayout.Space();

            // 记录FlowMap模式的当前状态
            bool oldFlowMapEnabled = enableFlowMap.floatValue > 0.5f;

            // 显示FlowMap模式开关
            materialEditor.ShaderProperty(enableFlowMap, "FlowMap模式");

            // 获取FlowMap模式的新状态
            bool newFlowMapEnabled = enableFlowMap.floatValue > 0.5f;

            // 如果FlowMap模式状态发生变化
            if (oldFlowMapEnabled != newFlowMapEnabled)
            {
                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 如果启用了FlowMap模式，则关闭扭曲遮罩
                    if (newFlowMapEnabled && enableDistortMask.floatValue > 0.5f)
                    {
                        enableDistortMask.floatValue = 0.0f;
                        material.DisableKeyword("_DISTORTMASK_ON");
                    }

                    // 设置或禁用关键字
                    if (newFlowMapEnabled)
                        material.EnableKeyword("_FLOWMAP_ON");
                    else
                        material.DisableKeyword("_FLOWMAP_ON");

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            // 更新FlowMap模式状态变量
            isFlowMapEnabled = newFlowMapEnabled;

            if (isFlowMapEnabled)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(flowMapIntensity, "强度");
                materialEditor.ShaderProperty(flowMapSpeed, "流动速度");
                materialEditor.ShaderProperty(enableFlowMapLTG, "启用伽马校正");
                EditorGUI.indentLevel--;
            }

            // ===== 扭曲遮罩设置区域 =====
            EditorGUILayout.Space();

            // 只有在FlowMap模式未启用时才显示扭曲遮罩相关控件
            if (!isFlowMapEnabled)
            {
                // 使用水平布局，将开关放在左侧
                EditorGUILayout.BeginHorizontal();

                // 使用开关控件替代默认的ShaderProperty
                bool isMaskEnabled = enableDistortMask.floatValue > 0.5f;
                bool newIsMaskEnabled = EditorGUILayout.Toggle(isMaskEnabled, GUILayout.Width(20));

                // 如果状态改变，更新属性和关键字
                if (isMaskEnabled != newIsMaskEnabled)
                {
                    // 先更新属性值
                    enableDistortMask.floatValue = newIsMaskEnabled ? 1.0f : 0.0f;

                    // 使用延迟执行来避免闪烁
                    EditorApplication.delayCall += () => {
                        // 设置或禁用关键字
                        if (newIsMaskEnabled)
                            material.EnableKeyword("_DISTORTMASK_ON");
                        else
                            material.DisableKeyword("_DISTORTMASK_ON");

                        // 标记材质为已修改
                        EditorUtility.SetDirty(material);
                    };
                }

                EditorGUILayout.LabelField("启用扭曲遮罩", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                // 只有在扭曲遮罩启用时才显示相关控件
                if (enableDistortMask.floatValue > 0.5f)
                {
                    // 扭曲遮罩贴图
                    materialEditor.TextureProperty(distortMaskTex, "扭曲遮罩贴图");

                    materialEditor.ShaderProperty(distortMaskChannel, "通道选择");

                    // 扭曲遮罩采样UV设置
                    if (distortMaskTexSampleUV != null)
                    {
                        materialEditor.ShaderProperty(distortMaskTexSampleUV, "采样UV");
                    }

                    // 遮罩UV模式设置
                    if (distortMaskUVMode != null)
                    {
                        materialEditor.ShaderProperty(distortMaskUVMode, distortMaskUVMode.displayName);

                        if (EditorGUI.EndChangeCheck())
                        {
                            // 自动处理 keyword 切换
                            SetKeyword(material, "_DISTORTTEXMASKUVMODE_POLAR", distortMaskUVMode.floatValue == 1);
                            SetKeyword(material, "_DISTORTTEXMASKUVMODE_SWIRL", distortMaskUVMode.floatValue == 2);
                        }
                        // 在相应的UV模式设置后添加中心点控制
                        if (distortMaskUVMode.floatValue == 1)
                        {
                            MaterialProperty polarCenter = FindCachedProperty("_DistortMaskTexPolarCenter");
                            if (polarCenter != null)
                            {
                                Vector4 center = polarCenter.vectorValue;
                                Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                                polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                            }
                        }
                        if (distortMaskUVMode.floatValue == 2) // 漩涡模式
                        {
                            MaterialProperty swirlCenter = FindCachedProperty("_DistortMaskTexSwirlCenter");
                            if (swirlCenter != null)
                            {
                                Vector4 center = swirlCenter.vectorValue;
                                Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                                swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                            }
                            if (distortMaskSwirlFactor != null)
                            {
                                materialEditor.ShaderProperty(distortMaskSwirlFactor, "漩涡强度");
                            }
                        }
                    }

                    // 遮罩旋转设置
                    materialEditor.ShaderProperty(distortMaskRotateAngle, "旋转角度");

                    // 遮罩UV限制设置
                    materialEditor.ShaderProperty(distortMaskClampU, "UClamp");
                    materialEditor.ShaderProperty(distortMaskClampV, "VClamp");

                    // 遮罩UV动画
                    materialEditor.ShaderProperty(distortMaskUspeed, "USpeed");
                    materialEditor.ShaderProperty(distortMaskVspeed, "VSpeed");

                    // 使用滑杆控件显示反转程度属性
                    distortMaskInvert.floatValue = EditorGUILayout.Slider("反转程度", distortMaskInvert.floatValue, 0.0f, 1.0f);
                }
            }
            else
            {
                // 在FlowMap模式下显示提示信息
                EditorGUILayout.HelpBox("FlowMap模式下不支持扭曲遮罩功能", MessageType.Info);
            }

            // 结束扭曲相关控件的禁用组
            EditorGUI.EndDisabledGroup();
        }

        // 绘制特效页签
        private void DrawEffectTab()
        {
            // 保存原来的标签宽度
            float oldLabelWidth = EditorGUIUtility.labelWidth;

            // 设置为适合当前区域的宽度
            EditorGUIUtility.labelWidth = 120f;

            // 获取菲涅尔相关属性
            MaterialProperty enableFresnel = FindProperty("_EnableFresnel", properties);
            MaterialProperty fresnelColor = FindProperty("_FresnelColor", properties);
            MaterialProperty fresnelRange = FindProperty("_FresnelRange", properties);
            MaterialProperty fresnelIntensity = FindProperty("_FresnelIntensity", properties);
            MaterialProperty fresnelEdgeHardness = FindProperty("_FresnelEdgeHardness", properties);
            MaterialProperty fresnelInvert = FindProperty("_FresnelInvert", properties);

            MaterialProperty fresnelViewOffsetX = FindProperty("_FresnelViewOffsetX", properties);
            MaterialProperty fresnelViewOffsetY = FindProperty("_FresnelViewOffsetY", properties);
            MaterialProperty fresnelViewOffsetZ = FindProperty("_FresnelViewOffsetZ", properties);

            DrawBlockTitle("菲涅尔");

            // 获取当前材质
            Material material = materialEditor.target as Material;

            // 使用优化后的通用方法绘制开关，直接使用TabState枚举
            bool isFresnelEnabled = DrawToggleWithLabel(enableFresnel, "启用菲涅尔效果", TabState.TabE);

            // 更新关键字
            if (isFresnelEnabled)
                material.EnableKeyword("_FRESNEL_ON");
            else
                material.DisableKeyword("_FRESNEL_ON");

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在菲涅尔未启用时禁用控件
            // 直接使用上面已经定义的isFresnelEnabled变量
            EditorGUI.BeginDisabledGroup(!isFresnelEnabled);

            // 菲涅尔颜色
            materialEditor.ShaderProperty(fresnelColor, "颜色");

            // 菲涅尔亮度
            MaterialProperty fresnelBrightness = FindProperty("_FresnelBrightness", properties);
            materialEditor.ShaderProperty(fresnelBrightness, "亮度");

            // 菲涅尔范围
            materialEditor.ShaderProperty(fresnelRange, "范围");

            // 菲涅尔强度
            materialEditor.ShaderProperty(fresnelIntensity, "菲涅尔强度");


            // 菲涅尔边缘硬度
            materialEditor.ShaderProperty(fresnelEdgeHardness, "边缘硬度");

            // 反转菲涅尔 - 使用通用滑杆方法
            DrawPropertyWithSlider(fresnelInvert, "反转程度", 0.0f, 1.0f);

            // 视角偏移 - 使用通用Vector3Field绘制方法
            Vector3 fresnelViewOffset = new Vector3(
                fresnelViewOffsetX.floatValue,
                fresnelViewOffsetY.floatValue,
                fresnelViewOffsetZ.floatValue
            );

            DrawVector3Field(viewOffsetLabelContent, fresnelViewOffset, (newValue) => {
                fresnelViewOffsetX.floatValue = newValue.x;
                fresnelViewOffsetY.floatValue = newValue.y;
                fresnelViewOffsetZ.floatValue = newValue.z;
            });


            // 菲涅尔因子作为透明度的开关
            MaterialProperty fresnelAsAlpha = FindProperty("_FresnelAsAlpha", properties);
            materialEditor.ShaderProperty(fresnelAsAlpha, "菲涅尔透明");

            // 如果启用了菲涅尔因子作为透明度，显示提示信息
            if (fresnelAsAlpha.floatValue > 0.5f)
            {
                EditorGUILayout.HelpBox("注意：启用此选项后，菲涅尔因子将影响整个材质的透明度。\n请确保材质的混合模式设置为透明混合。", MessageType.Info);
            }




            // 结束菲涅尔相关控件的禁用组
            EditorGUI.EndDisabledGroup();

            // 恢复原来的标签宽度
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // 绘制顶点动画页签
        private void DrawDevelopingTabF()
        {

            // 获取顶点动画相关属性
            MaterialProperty enableVertexAnim = FindProperty("_EnableVertexAnim", properties);
            MaterialProperty vaPos = FindProperty("_VAPos", properties);
            MaterialProperty vaNormalInfluence = FindProperty("_VANormalInfluence", properties);

            // 顶点动画贴图相关属性
            MaterialProperty vaTex = FindProperty("_VATex", properties);
            MaterialProperty vaTexSampleUV = FindProperty("_VATexSampleUV", properties);
            MaterialProperty vaTexChannelR = FindProperty("_VATexChannelR", properties);
            MaterialProperty vaTexChannelG = FindProperty("_VATexChannelG", properties);
            MaterialProperty vaTexChannelB = FindProperty("_VATexChannelB", properties);
            MaterialProperty vaTexUVMode = FindProperty("_VATexUVMode", properties);
            MaterialProperty vATexUVMode_Polar = FindProperty("_VATexUVMode_Polar", properties);
            MaterialProperty vATexUVMode_Swirl = FindProperty("_VATexUVMode_Swirl", properties);

            MaterialProperty vaTexSwirlFactor = FindProperty("_VATexSwirlFactor", properties);

            MaterialProperty vaTexRotateAngle = FindProperty("_VATexRotateAngle", properties);
            MaterialProperty vaTexUVClampU = FindProperty("_VATexUVClampU", properties);
            MaterialProperty vaTexUVClampV = FindProperty("_VATexUVClampV", properties);
            MaterialProperty vaTexUspeed = FindProperty("_VATexUspeed", properties);
            MaterialProperty vaTexVspeed = FindProperty("_VATexVspeed", properties);
            MaterialProperty vaTexInvert = FindProperty("_VATexInvert", properties);

            // 顶点动画贴图遮罩相关属性
            MaterialProperty enableVATexMask = FindProperty("_EnableVATexMask", properties);
            MaterialProperty vaTexMask = FindProperty("_VATexMask", properties);
            MaterialProperty vaTexMaskSampleUV = FindProperty("_VATexMaskSampleUV", properties);
            MaterialProperty vaTexMaskChannel = FindProperty("_VATexMaskChannel", properties);
            MaterialProperty vaTexMaskUVMode = FindProperty("_VATexMaskUVMode", properties);
            MaterialProperty vATexMaskUVMode_Polar = FindProperty("_VATexMaskUVMode_Polar", properties);
            MaterialProperty vATexMaskUVMode_Swirl = FindProperty("_VATexMaskUVMode_Swirl", properties);
            MaterialProperty vaTexMaskSwirlFactor = FindProperty("_VATexMaskSwirlFactor", properties);

            MaterialProperty vaTexMaskUVClampU = FindProperty("_VATexMaskUVClampU", properties);
            MaterialProperty vaTexMaskUVClampV = FindProperty("_VATexMaskUVClampV", properties);
            MaterialProperty vaTexMaskUspeed = FindProperty("_VATexMaskUspeed", properties);
            MaterialProperty vaTexMaskVspeed = FindProperty("_VATexMaskVspeed", properties);
            MaterialProperty vaTexMaskRotateAngle = FindProperty("_VATexMaskRotateAngle", properties);
            MaterialProperty vaTexMaskInvert = FindProperty("_VATexMaskInvert", properties);



            DrawBlockTitle("顶点动画");
            // 使用水平布局，将开关放在左侧
            EditorGUILayout.BeginHorizontal();

            // 获取当前材质
            Material material = materialEditor.target as Material;

            // 使用开关控件替代默认的ShaderProperty
            bool isEnabled = enableVertexAnim.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isEnabled != newIsEnabled)
            {
                enableVertexAnim.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabF, newIsEnabled);

                // 直接更新关键字
                if (newIsEnabled)
                {
                    material.EnableKeyword("_VERTEXANIM_ON");
                }
                else
                {
                    material.DisableKeyword("_VERTEXANIM_ON");

                    // 当关闭一级开关时，同时关闭二级开关（顶点动画贴图遮罩）
                    if (enableVATexMask.floatValue > 0.5f)
                    {
                        enableVATexMask.floatValue = 0.0f;
                        material.DisableKeyword("_VATEXMASK_ON");
                    }
                }

                // 标记材质为已修改
                EditorUtility.SetDirty(material);
            }


            EditorGUILayout.LabelField("启用顶点动画", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // 使用EditorGUI.BeginDisabledGroup在顶点动画未启用时禁用控件
            bool isVertexAnimEnabled = enableVertexAnim.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isVertexAnimEnabled);

            // 顶点动画贴图
            materialEditor.TextureProperty(vaTex, "顶点动画贴图");

            // 通道选择 - RGB Toggle Buttons
            Rect lineRectVATex = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            GUIContent labelVATex = new GUIContent("通道选择");
            Rect controlAreaVATex = EditorGUI.PrefixLabel(lineRectVATex, labelVATex);

            float totalButtonWidth = controlAreaVATex.width;
            float spacing = 2f; // Spacing between buttons
            int buttonCount = 0;
            if (vaTexChannelR != null) buttonCount++;
            if (vaTexChannelG != null) buttonCount++;
            if (vaTexChannelB != null) buttonCount++;

            if (buttonCount > 0)
            {
                float individualButtonWidth = (totalButtonWidth - (spacing * (buttonCount - 1))) / buttonCount;
                float currentX = controlAreaVATex.x;

                if (vaTexChannelR != null)
                {
                    Rect rButtonRect = new Rect(currentX, controlAreaVATex.y, individualButtonWidth, controlAreaVATex.height);
                    vaTexChannelR.floatValue = GUI.Toggle(rButtonRect, vaTexChannelR.floatValue > 0.5f, "R", GUI.skin.button) ? 1.0f : 0.0f;
                    currentX += individualButtonWidth + spacing;
                }
                if (vaTexChannelG != null)
                {
                    Rect gButtonRect = new Rect(currentX, controlAreaVATex.y, individualButtonWidth, controlAreaVATex.height);
                    vaTexChannelG.floatValue = GUI.Toggle(gButtonRect, vaTexChannelG.floatValue > 0.5f, "G", GUI.skin.button) ? 1.0f : 0.0f;
                    currentX += individualButtonWidth + spacing;
                }
                if (vaTexChannelB != null)
                {
                    Rect bButtonRect = new Rect(currentX, controlAreaVATex.y, individualButtonWidth, controlAreaVATex.height);
                    vaTexChannelB.floatValue = GUI.Toggle(bButtonRect, vaTexChannelB.floatValue > 0.5f, "B", GUI.skin.button) ? 1.0f : 0.0f;
                }
            }

            // 顶点动画贴图采样UV设置
            if (vaTexSampleUV != null)
            {
                materialEditor.ShaderProperty(vaTexSampleUV, "采样UV");
            }

            // UV模式设置
            if (vaTexUVMode != null)
            {
                materialEditor.ShaderProperty(vaTexUVMode, vaTexUVMode.displayName);

                if (EditorGUI.EndChangeCheck())
                {
                    // 自动处理 keyword 切换
                    SetKeyword(material, "_VATEXUVMODE_POLAR", vaTexUVMode.floatValue == 1);
                    SetKeyword(material, "_VATEXUVMODE_SWIRL", vaTexUVMode.floatValue == 2);
                }
                // 在相应的UV模式设置后添加中心点控制
                if (vaTexUVMode.floatValue == 1)
                {
                    MaterialProperty polarCenter = FindCachedProperty("_VATexPolarCenter");
                    if (polarCenter != null)
                    {
                        Vector4 center = polarCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                        polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                }
                if (vaTexUVMode.floatValue == 2) // 漩涡模式
                {
                    MaterialProperty swirlCenter = FindCachedProperty("_VATexSwirlCenter");
                    if (swirlCenter != null)
                    {
                        Vector4 center = swirlCenter.vectorValue;
                        Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                        swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                    }
                    if (vaTexSwirlFactor != null)
                    {
                        materialEditor.ShaderProperty(vaTexSwirlFactor, "漩涡强度");
                    }
                }
            }

            // 旋转设置
            materialEditor.ShaderProperty(vaTexRotateAngle, "旋转角度");



            // UV限制设置
            materialEditor.ShaderProperty(vaTexUVClampU, "UClamp");
            materialEditor.ShaderProperty(vaTexUVClampV, "VClamp");

            // 反转程度 - 使用滑杆控件
            vaTexInvert.floatValue = EditorGUILayout.Slider("反转程度", vaTexInvert.floatValue, 0.0f, 1.0f);

            // UV动画
            materialEditor.ShaderProperty(vaTexUspeed, "USpeed");
            materialEditor.ShaderProperty(vaTexVspeed, "VSpeed");

            // 坐标空间选择
            MaterialProperty vaCoordinateSpace = FindProperty("_VACoordinateSpace", properties);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(coordinateSpaceLabel);

            // 记录当前坐标空间设置
            float oldCoordinateSpace = vaCoordinateSpace.floatValue;

            // 显示下拉菜单
            vaCoordinateSpace.floatValue = (float)EditorGUILayout.Popup(
                (int)vaCoordinateSpace.floatValue,
                new GUIContent[] {
                    new GUIContent("模型空间"),
                    new GUIContent("世界空间")
                }
            );
            EditorGUILayout.EndHorizontal();

            // 如果坐标空间设置发生变化，更新关键字
            if (oldCoordinateSpace != vaCoordinateSpace.floatValue)
            {
                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 根据选择设置或禁用关键字
                    if (vaCoordinateSpace.floatValue > 0.5f) // 世界空间
                    {
                        material.EnableKeyword("_VAWORLDSPACE_ON");
                    }
                    else // 模型空间
                    {
                        material.DisableKeyword("_VAWORLDSPACE_ON");
                    }

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            // 法线影响程度 - 使用滑杆控件
            vaNormalInfluence.floatValue = EditorGUILayout.Slider("法线影响", vaNormalInfluence.floatValue, 0.0f, 1.0f);

            // 方向强度 - 使用手动布局以实现右对齐固定
            Rect strengthLineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            GUIContent strengthLabelContent = new GUIContent("方向强度");

            // 定义每个浮点输入字段的最小期望宽度和它们之间的间距
            float minFloatFieldWidthStrength = 30f;
            float spacingBetweenFieldsStrength = 4f; // X, Y, Z 输入框之间的间距
            // Vector3Field 有3个字段和2个字段间距
            float totalMinFieldsWidthStrength = (3 * minFloatFieldWidthStrength) + (2 * spacingBetweenFieldsStrength);
            float standardSpacingStrength = EditorGUIUtility.standardVerticalSpacing; // 标签和字段之间的间距

            // 计算标签的可用宽度
            float availableWidthForLabelStrength = strengthLineRect.width - totalMinFieldsWidthStrength - standardSpacingStrength;
            float actualLabelWidthStrength = Mathf.Min(EditorGUIUtility.labelWidth, Mathf.Max(0, availableWidthForLabelStrength));

            // 定义标签的矩形区域
            Rect strengthLabelRect = new Rect(strengthLineRect.x, strengthLineRect.y, actualLabelWidthStrength, strengthLineRect.height);

            // 定义输入字段的矩形区域
            float strengthFieldsX = strengthLineRect.x + actualLabelWidthStrength + standardSpacingStrength;
            float strengthFieldsWidth = strengthLineRect.width - actualLabelWidthStrength - standardSpacingStrength;
            Rect strengthFieldsRect = new Rect(strengthFieldsX, strengthLineRect.y, Mathf.Max(0, strengthFieldsWidth), strengthLineRect.height);

            // 绘制标签
            EditorGUI.LabelField(strengthLabelRect, strengthLabelContent);

            // 获取当前的 Vector3 值 (vaPos 是 Vector4)
            Vector4 currentStrengthVec = vaPos.vectorValue;
            Vector3 currentStrengthValue = new Vector3(currentStrengthVec.x, currentStrengthVec.y, currentStrengthVec.z);

            EditorGUI.BeginChangeCheck();
            // 在计算出的 strengthFieldsRect 中绘制 Vector3Field (仅输入框部分)
            Vector3 newStrengthValue = EditorGUI.Vector3Field(strengthFieldsRect, GUIContent.none, currentStrengthValue);
            if (EditorGUI.EndChangeCheck())
            {
                // 当值改变时，更新属性，保留原始的w分量
                vaPos.vectorValue = new Vector4(newStrengthValue.x, newStrengthValue.y, newStrengthValue.z, currentStrengthVec.w);
            }





            EditorGUILayout.Space();

            // 顶点动画贴图遮罩设置
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // 使用开关控件替代默认的ShaderProperty
            bool isVATexMaskEnabled = enableVATexMask.floatValue > 0.5f;
            bool newIsVATexMaskEnabled = EditorGUILayout.Toggle(isVATexMaskEnabled, GUILayout.Width(20));

            // 如果状态改变，更新属性和关键字
            if (isVATexMaskEnabled != newIsVATexMaskEnabled)
            {
                // 先更新属性值
                enableVATexMask.floatValue = newIsVATexMaskEnabled ? 1.0f : 0.0f;

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsVATexMaskEnabled)
                        material.EnableKeyword("_VATEXMASK_ON");
                    else
                        material.DisableKeyword("_VATEXMASK_ON");

                    // 标记材质为已修改
                    EditorUtility.SetDirty(material);
                };
            }

            EditorGUILayout.LabelField("启用遮罩", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // 只有在启用遮罩时才显示遮罩相关选项
            if (isVATexMaskEnabled)
            {
                EditorGUI.indentLevel++;

                // 顶点动画贴图遮罩
                materialEditor.TextureProperty(vaTexMask, "顶点动画贴图遮罩");

                // 通道选择 - 使用与其他模块一致的枚举下拉菜单
                materialEditor.ShaderProperty(vaTexMaskChannel, "通道选择");

                // 顶点动画贴图遮罩采样UV设置
                if (vaTexMaskSampleUV != null)
                {
                    materialEditor.ShaderProperty(vaTexMaskSampleUV, "采样UV");
                }

                // UV模式设置
                if (vaTexMaskUVMode != null)
                {
                    materialEditor.ShaderProperty(vaTexMaskUVMode, vaTexMaskUVMode.displayName);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // 自动处理 keyword 切换
                        SetKeyword(material, "_VATEXMASKUVMODE_POLAR", vaTexMaskUVMode.floatValue == 1);
                        SetKeyword(material, "_VATEXMASKUVMODE_SWIRL", vaTexMaskUVMode.floatValue == 2);
                    }
                    // 在相应的UV模式设置后添加中心点控制
                    if (vaTexMaskUVMode.floatValue == 1)
                    {
                        MaterialProperty polarCenter = FindCachedProperty("_VATexMaskPolarCenter");
                        if (polarCenter != null)
                        {
                            Vector4 center = polarCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("极坐标中心", new Vector2(center.x, center.y));
                            polarCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                    }
                    if (vaTexMaskUVMode.floatValue == 2) // 漩涡模式
                    {
                        MaterialProperty swirlCenter = FindCachedProperty("_VATexMaskSwirlCenter");
                        if (swirlCenter != null)
                        {
                            Vector4 center = swirlCenter.vectorValue;
                            Vector2 newCenter = EditorGUILayout.Vector2Field("漩涡中心", new Vector2(center.x, center.y));
                            swirlCenter.vectorValue = new Vector4(newCenter.x, newCenter.y, center.z, center.w);
                        }
                        if (vaTexMaskSwirlFactor != null)
                        {
                            materialEditor.ShaderProperty(vaTexMaskSwirlFactor, "漩涡强度");
                        }
                    }
                }

                // UV限制设置
                materialEditor.ShaderProperty(vaTexMaskUVClampU, "UClamp");
                materialEditor.ShaderProperty(vaTexMaskUVClampV, "VClamp");

                // 旋转设置
                materialEditor.ShaderProperty(vaTexMaskRotateAngle, "旋转角度");

                // UV动画
                materialEditor.ShaderProperty(vaTexMaskUspeed, "USpeed");
                materialEditor.ShaderProperty(vaTexMaskVspeed, "VSpeed");

                // 反转程度
                vaTexMaskInvert.floatValue = EditorGUILayout.Slider("反转程度", vaTexMaskInvert.floatValue, 0.0f, 1.0f);

                EditorGUI.indentLevel--;
            }

            // 结束顶点动画相关控件的禁用组
            EditorGUI.EndDisabledGroup();
        }

        // 绘制自定义数据页签G
        private void DrawDevelopingTabG()
        {
            // 获取自定义数据相关属性
            MaterialProperty enableCustomData = FindProperty("_EnableCustomData", properties);
            MaterialProperty customData1X = FindProperty("_CustomData1X", properties);
            MaterialProperty customData1Y = FindProperty("_CustomData1Y", properties);
            MaterialProperty customData1Z = FindProperty("_CustomData1Z", properties);
            MaterialProperty customData1W = FindProperty("_CustomData1W", properties);
            MaterialProperty customData2X = FindProperty("_CustomData2X", properties);
            MaterialProperty customData2Y = FindProperty("_CustomData2Y", properties);
            MaterialProperty customData2Z = FindProperty("_CustomData2Z", properties);
            MaterialProperty customData2W = FindProperty("_CustomData2W", properties);


            DrawBlockTitle("CustomData");

            // 绘制自定义数据开关
            EditorGUILayout.BeginHorizontal();
            bool isEnabled = enableCustomData.floatValue > 0.5f;
            bool newIsEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
            if (isEnabled != newIsEnabled)
            {
                enableCustomData.floatValue = newIsEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabG, newIsEnabled);

                // 获取当前材质
                Material material = materialEditor.target as Material;

                // 使用延迟执行来避免闪烁
                EditorApplication.delayCall += () => {
                    // 设置或禁用关键字
                    if (newIsEnabled)
                        material.EnableKeyword("_CUSTOMDATA_ON");
                    else
                        material.DisableKeyword("_CUSTOMDATA_ON");

                    EditorUtility.SetDirty(material);
                };
            }
            EditorGUILayout.LabelField("启用CustomData", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在自定义数据未启用时禁用控件
            bool isCustomDataEnabled = enableCustomData.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(!isCustomDataEnabled);

            // 当前环境提示
            bool isParticleSystem = false;
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                ParticleSystem particleSystem = selectedObject.GetComponent<ParticleSystem>();
                ParticleSystemRenderer renderer = selectedObject.GetComponent<ParticleSystemRenderer>();
                isParticleSystem = (particleSystem != null && renderer != null);
            }

            if (!isParticleSystem)
            {
                EditorGUILayout.HelpBox("当前环境: 预览的对象不是粒子系统", MessageType.Warning);
            }
            else
            {
                // 使用更明亮的绿色背景和绿色文字的成功消息
                GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f); // 更亮的背景色
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 创建更明亮的绿色文字样式
                GUIStyle brightGreenTextStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }, // 更亮的绿色文字
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };

                EditorGUILayout.LabelField("当前环境: 已选中粒子系统", brightGreenTextStyle);
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white; // 恢复默认背景色
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("粒子系统顶点流设置");
            if (GUILayout.Button("自动设置", GUILayout.Width(100)))
            {
                SetupParticleSystemVertexStreams();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // CustomData使用规则
            EditorGUILayout.LabelField("CustomData使用规则", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Custom1 (texcoord1):");
            EditorGUILayout.LabelField("    customData1.x -> 主贴图U偏移");
            EditorGUILayout.LabelField("    customData1.y -> 主贴图V偏移");
            EditorGUILayout.LabelField("    customData1.z -> 主贴图遮罩U偏移");
            EditorGUILayout.LabelField("    customData1.w -> 主贴图遮罩V偏移");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Custom2 (texcoord2):");
            EditorGUILayout.LabelField("    customData2.x -> 溶解度");
            EditorGUILayout.LabelField("    customData2.y -> 溶解边缘宽度");
            EditorGUILayout.LabelField("    customData2.z -> 扭曲X");
            EditorGUILayout.LabelField("    customData2.w -> 扭曲Y");

            // 隐藏实际的输入框，因为在原始设计中这些值是由粒子系统设置的
            // 但我们仍然保留这些属性，只是不在UI中显示
            customData1X.floatValue = 0;
            customData1Y.floatValue = 0;
            customData1Z.floatValue = 0;
            customData1W.floatValue = 0;
            customData2X.floatValue = 0;
            customData2Y.floatValue = 0;
            customData2Z.floatValue = 0;
            customData2W.floatValue = 0;

            EditorGUI.EndDisabledGroup();
        }

        // 设置粒子系统的顶点流
        private void SetupParticleSystemVertexStreams()
        {
            // 获取当前选中的游戏对象
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个粒子系统对象", "确定");
                return;
            }

            Debug.Log("当前选中对象: " + selectedObject.name);

            // 先检查是否有ParticleSystem组件
            ParticleSystem particleSystem = selectedObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                Debug.LogError("无法获取ParticleSystem组件，对象类型: " + selectedObject.GetType());

                // 列出所有组件
                Component[] components = selectedObject.GetComponents<Component>();
                Debug.Log("对象上的组件数量: " + components.Length);
                foreach (Component comp in components)
                {
                    Debug.Log(" - " + comp.GetType().Name);
                }

                EditorUtility.DisplayDialog("错误", "选中的对象不是粒子系统", "确定");
                return;
            }

            Debug.Log("成功获取ParticleSystem组件");

            // 获取粒子系统渲染器组件 - 在同一个游戏对象上
            ParticleSystemRenderer renderer = selectedObject.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                Debug.LogError("无法获取ParticleSystemRenderer组件");
                EditorUtility.DisplayDialog("错误", "无法获取粒子系统渲染器组件", "确定");
                return;
            }

            Debug.Log("成功获取ParticleSystemRenderer组件");

            // 构建需要的顶点流列表
            List<ParticleSystemVertexStream> streams = new List<ParticleSystemVertexStream>();
            streams.Add(ParticleSystemVertexStream.Position);
            streams.Add(ParticleSystemVertexStream.Normal);
            streams.Add(ParticleSystemVertexStream.Color);
            streams.Add(ParticleSystemVertexStream.UV);
            streams.Add(ParticleSystemVertexStream.UV2);  // 添加UV2(TEXCOORD0.zw)

            // 始终添加Custom1XYZW，因为我们的着色器需要它
            streams.Add(ParticleSystemVertexStream.Custom1XYZW);

            // 如果需要使用Custom2XYZW，也添加它
            streams.Add(ParticleSystemVertexStream.Custom2XYZW);

            // 设置粒子系统的顶点流
            renderer.SetActiveVertexStreams(streams);

            // 显示成功消息
            EditorUtility.DisplayDialog("成功", "已成功设置粒子系统的顶点流", "确定");
            Debug.Log("已成功设置粒子系统的顶点流");
        }

        // 绘制开发中的H页签 - 模板测试功能
        private void DrawDevelopingTabH()
        {
            DrawBlockTitle("模板测试");

            Material material = materialEditor.target as Material;

            // 查找模板测试相关属性
            MaterialProperty enableStencilTest = FindProperty("_EnableStencilTest", properties);
            MaterialProperty stencilComp = FindProperty("_StencilComp", properties);
            MaterialProperty stencil = FindProperty("_Stencil", properties);
            MaterialProperty stencilOp = FindProperty("_StencilOp", properties);
            MaterialProperty stencilWriteMask = FindProperty("_StencilWriteMask", properties);
            MaterialProperty stencilReadMask = FindProperty("_StencilReadMask", properties);

            // 模板测试启用开关
            EditorGUILayout.BeginHorizontal();
            bool isStencilEnabled = enableStencilTest.floatValue > 0.5f;
            bool newIsStencilEnabled = EditorGUILayout.Toggle(isStencilEnabled, GUILayout.Width(20));
            if (isStencilEnabled != newIsStencilEnabled)
            {
                enableStencilTest.floatValue = newIsStencilEnabled ? 1.0f : 0.0f;

                // 直接更新TabState枚举值
                SetTabState(TabState.TabH, newIsStencilEnabled);
                // 如果关闭模板测试，将比较函数设置为“总是通过”
                if (!newIsStencilEnabled)
                {
                    stencilComp.floatValue = 8; // Always
                }
                EditorUtility.SetDirty(material);
            }
            EditorGUILayout.LabelField("启用模板测试", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 使用EditorGUI.BeginDisabledGroup在模板测试未启用时禁用控件
            EditorGUI.BeginDisabledGroup(!newIsStencilEnabled);


            // 创建模板测试比较函数选项
            GUIContent stencilCompLabel = new GUIContent("模板比较函数", "设置模板测试的比较函数");
            stencilComp.floatValue = (float)EditorGUILayout.Popup(stencilCompLabel, (int)stencilComp.floatValue,
                new GUIContent[] {
                    new GUIContent("禁用 (Disabled)"),
                    new GUIContent("永不通过 (Never)"),
                    new GUIContent("小于 (Less)"),
                    new GUIContent("等于 (Equal)"),
                    new GUIContent("小于等于 (LEqual)"),
                    new GUIContent("大于 (Greater)"),
                    new GUIContent("不等于 (NotEqual)"),
                    new GUIContent("大于等于 (GEqual)"),
                    new GUIContent("总是通过 (Always)")
                });

            // 模板ID
            materialEditor.ShaderProperty(stencil, new GUIContent("模板ID", "设置模板测试的参考值"));

            // 模板操作选项
            GUIContent stencilOpLabel = new GUIContent("模板操作", "设置模板测试通过后的操作");
            stencilOp.floatValue = (float)EditorGUILayout.Popup(stencilOpLabel, (int)stencilOp.floatValue,
                new GUIContent[] {
                    new GUIContent("保持 (Keep)"),
                    new GUIContent("归零 (Zero)"),
                    new GUIContent("替换 (Replace)"),
                    new GUIContent("递增 (Increment)"),
                    new GUIContent("递减 (Decrement)"),
                    new GUIContent("递增饱和 (IncrementSaturate)"),
                    new GUIContent("递减饱和 (DecrementSaturate)"),
                    new GUIContent("反转 (Invert)")
                });

            // 模板写入掩码和读取掩码
            materialEditor.ShaderProperty(stencilWriteMask, new GUIContent("写入掩码", "设置模板缓冲区的写入掩码"));
            materialEditor.ShaderProperty(stencilReadMask, new GUIContent("读取掩码", "设置模板缓冲区的读取掩码"));

            EditorGUILayout.Space();



            EditorGUILayout.Space();

            // 添加使用说明
            EditorGUILayout.HelpBox("模板测试可用于创建特殊的渲染效果，如镂空、遮罩等。\n" +
                                  "- 模板比较函数：决定像素是否通过模板测试\n" +
                                  "- 模板ID：用于比较的参考值\n" +
                                  "- 模板操作：通过测试后对模板缓冲区的操作\n" +
                                  "- 写入/读取掩码：控制哪些位可以被写入或读取", MessageType.Info);

            EditorGUI.EndDisabledGroup();
        }
    }
}
