using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 图片资源处理工具
///
/// 用途：
/// 1. 批量管理项目中图片资源的导入参数（TextureImporter设置）
///    - 可对指定文件夹下的所有图片统一设置：纹理类型、压缩格式、WrapMode、最大尺寸、PixelsPerUnit等
///    - 支持创建多组不同的导入配置规则，分别应用于不同的资源目录
///    - 配置数据持久化保存，方便团队协作时统一资源标准
/// 
/// 2. 单张图片缩放处理
///    - 将指定图片压缩或放大到指定的宽高像素
///    - 支持多种缩放算法（Bilinear、Point、Trilinear）
///    - 输出新图片到指定目录
///
/// 典型使用场景：
/// 1. UI图片统一设置为 Sprite 类型 + 特定压缩质量
/// 2. 场景贴图统一设置最大尺寸和压缩方式
/// 3. 新增图片资源后一键刷新导入设置，保持项目资源规范一致
/// 4. 将大图缩放到指定尺寸用于特定用途（如缩略图、图标等）
/// 5. 将小图放大到高分辨率用于高DPI显示或打印
/// </summary>
public class ImageResWindow : EditorWindow
{
    #region 常量
    /// <summary>配置数据保存路径</summary>
    private const string PathSaveData = "Assets/Data/ImageRes";
    /// <summary>配置数据文件名</summary>
    private const string SaveDataFileName = "ImageResSaveData";
    /// <summary>窗口最小宽度</summary>
    private const float WindowMinWidth = 520f;
    /// <summary>窗口最小高度</summary>
    private const float WindowMinHeight = 500f;
    #endregion

    #region 静态字段
    [InitializeOnLoadMethod]
    static void EditorApplication_ProjectChanged()
    {
        // projectWindowChanged已过时
        // 全局监听Project视图下的资源是否发生变化（添加 删除 移动等）
        // EditorApplication.projectChanged += HandleForAssetsChange;
        // PrefabStage.prefabSaving += HandleForAssetsChange;
    }

    /// <summary>单例配置数据对象</summary>
    protected static ImageResBean ImageResSaveData;
    #endregion

    #region 实例字段 - UI状态
    /// <summary>滚动视图位置</summary>
    protected Vector2 ScrollPosition;
    /// <summary>折叠状态字典（用于配置规则列表）</summary>
    private Dictionary<ImageResBeanItemBean, bool> _foldoutStates = new Dictionary<ImageResBeanItemBean, bool>();
    /// <summary>样式是否已初始化</summary>
    private bool _stylesInitialized;
    #endregion

    #region 实例字段 - 样式
    /// <summary>标题样式</summary>
    private GUIStyle _headerStyle;
    /// <summary>盒子样式</summary>
    private GUIStyle _boxStyle;
    /// <summary>标签样式</summary>
    private GUIStyle _labelStyle;
    #endregion

    #region 实例字段 - 图片缩放
    /// <summary>源图片纹理</summary>
    private Texture2D _sourceTexture;
    /// <summary>源图片路径</summary>
    private string _sourceImagePath = "";
    /// <summary>目标宽度</summary>
    private int _targetWidth = 256;
    /// <summary>目标高度</summary>
    private int _targetHeight = 256;
    /// <summary>输出文件夹路径</summary>
    private string _outputFolder = "Assets/Output";
    /// <summary>输出文件名</summary>
    private string _outputFileName = "resized_image.png";
    /// <summary>是否显示缩放设置区域</summary>
    private bool _showResizeSection = true;
    /// <summary>缩放结果消息</summary>
    private string _resizeResultMessage = "";
    /// <summary>缩放结果消息类型</summary>
    private MessageType _resizeResultType = MessageType.Info;
    /// <summary>缩放算法类型</summary>
    private ScaleAlgorithm _scaleAlgorithm = ScaleAlgorithm.Bilinear;
    /// <summary>是否保持宽高比</summary>
    private bool _keepAspectRatio = true;
    #endregion

    #region 枚举
    /// <summary>
    /// 图片缩放算法
    /// </summary>
    public enum ScaleAlgorithm
    {
        /// <summary>双线性插值 - 适用于一般图片的放大和缩小，效果平滑</summary>
        Bilinear,
        /// <summary>点采样（最近邻） - 适用于像素风格图片，保持硬边缘</summary>
        Point,
        /// <summary>三线性插值 - 适用于需要高质量缩放的场景</summary>
        Trilinear
    }
    #endregion

    #region 菜单项
    /// <summary>
    /// 创建窗口菜单项
    /// </summary>
    [MenuItem("Custom/工具弹窗/图片资源处理")]
    static void CreateWindows()
    {
        var window = GetWindow<ImageResWindow>();
        window.titleContent = new GUIContent("图片资源处理");
        window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
    }
    #endregion

    #region Unity生命周期
    /// <summary>
    /// 窗口启用时初始化数据
    /// </summary>
    public void OnEnable()
    {
        InitData();
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        _boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(4, 4, 4, 4)
        };

        _labelStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true
        };

        _stylesInitialized = true;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitData()
    {
        string dataSave = FileUtil.LoadTextFile($"{PathSaveData}/{SaveDataFileName}");
        if (dataSave.IsNull())
        {
            ImageResSaveData = new ImageResBean();
        }
        else
        {
            ImageResSaveData = JsonUtil.FromJsonByNet<ImageResBean>(dataSave);
        }
    }
    #endregion

    #region GUI绘制
    /// <summary>
    /// 绘制窗口GUI
    /// </summary>
    public void OnGUI()
    {
        InitStyles();

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
        GUILayout.BeginVertical();

        DrawHeader();
        GUILayout.Space(4);
        DrawImageResizeSection();
        GUILayout.Space(6);
        DrawToolbar();
        GUILayout.Space(6);
        DrawConfigList();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制窗口标题区域
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("图片资源处理工具", _headerStyle);
        EditorGUILayout.HelpBox(
            "功能1: 对指定文件夹下的图片资源统一设置导入参数（纹理类型、压缩、尺寸等）。\n" +
            "功能2: 将单张图片缩放到指定像素尺寸并输出到新目录。（支持放大和缩小）",
            MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制图片缩放处理区域
    /// </summary>
    private void DrawImageResizeSection()
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        // 折叠标题
        _showResizeSection = EditorGUILayout.Foldout(_showResizeSection, "📐 图片缩放处理", true, EditorStyles.foldoutHeader);

        if (_showResizeSection)
        {
            GUILayout.Space(4);
            DrawSourceImageSelector();
            GUILayout.Space(8);
            DrawResizeSettings();
            GUILayout.Space(8);
            DrawOutputSettings();
            GUILayout.Space(8);
            DrawResizeButtons();
            DrawResizeResult();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制源图片选择器
    /// </summary>
    private void DrawSourceImageSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("源图片:", GUILayout.Width(70));
        
        // 显示已选择的图片对象
        Texture2D newTexture = EditorGUILayout.ObjectField(_sourceTexture, typeof(Texture2D), false, GUILayout.Height(64), GUILayout.Width(64)) as Texture2D;
        if (newTexture != _sourceTexture)
        {
            _sourceTexture = newTexture;
            if (_sourceTexture != null)
            {
                OnSourceImageChanged(_sourceTexture);
            }
        }

        // 显示图片信息
        DrawSourceImageInfo();

        // 选择按钮
        if (GUILayout.Button("选择图片", GUILayout.Width(80), GUILayout.Height(24)))
        {
            SelectSourceImageFromDialog();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 源图片变更时的处理
    /// </summary>
    private void OnSourceImageChanged(Texture2D texture)
    {
        // 获取图片路径
        _sourceImagePath = AssetDatabase.GetAssetPath(texture);
        
        // 初始化目标尺寸为原图尺寸
        if (_keepAspectRatio)
        {
            _targetWidth = texture.width;
            _targetHeight = texture.height;
        }
        
        // 使用源图片名称作为默认输出文件名
        if (!string.IsNullOrEmpty(texture.name))
        {
            _outputFileName = $"{texture.name}.png";
        }
    }

    /// <summary>
    /// 绘制源图片信息
    /// </summary>
    private void DrawSourceImageInfo()
    {
        if (_sourceTexture != null)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"名称: {_sourceTexture.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"当前尺寸: {_sourceTexture.width} x {_sourceTexture.height}", EditorStyles.miniLabel);
            
            // 显示缩放比例信息
            float scaleX = (float)_targetWidth / _sourceTexture.width;
            float scaleY = (float)_targetHeight / _sourceTexture.height;
            string scaleInfo = scaleX > 1 || scaleY > 1 ? "放大" : "缩小";
            float avgScale = (scaleX + scaleY) / 2f;
            EditorGUILayout.LabelField($"缩放比例: {avgScale:P0} ({scaleInfo})", EditorStyles.miniLabel);
            
            EditorGUILayout.LabelField($"路径: {_sourceImagePath}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.LabelField("点击右侧按钮或拖拽图片到上方框中", EditorStyles.miniLabel);
        }
    }

    /// <summary>
    /// 从文件对话框选择源图片
    /// </summary>
    private void SelectSourceImageFromDialog()
    {
        string selectedPath = EditorUtility.OpenFilePanelWithFilters(
            "选择图片", 
            "Assets", 
            new[] { "Image files", "png,jpg,jpeg,bmp,tga,psd" });
        
        if (string.IsNullOrEmpty(selectedPath)) return;

        // 如果是项目内的资源，使用AssetDatabase加载
        if (selectedPath.Contains("Assets"))
        {
            string relativePath = "Assets" + selectedPath.Substring(selectedPath.IndexOf("Assets") + 6);
            relativePath = relativePath.Replace("\\", "/");
            _sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
            _sourceImagePath = relativePath;
        }
        else
        {
            // 项目外的资源，直接读取文件
            LoadTextureFromFile(selectedPath);
        }
        
        if (_sourceTexture != null)
        {
            _targetWidth = _sourceTexture.width;
            _targetHeight = _sourceTexture.height;
            
            // 使用源图片名称作为默认输出文件名
            if (!string.IsNullOrEmpty(_sourceTexture.name))
            {
                _outputFileName = $"{_sourceTexture.name}.png";
            }
        }
    }

    /// <summary>
    /// 从文件加载纹理
    /// </summary>
    private void LoadTextureFromFile(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        _sourceTexture = new Texture2D(2, 2);
        _sourceTexture.LoadImage(fileData);
        _sourceImagePath = filePath;
        
        // 从文件路径提取文件名作为默认输出文件名
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        if (!string.IsNullOrEmpty(fileName))
        {
            _outputFileName = $"{fileName}.png";
        }
    }

    /// <summary>
    /// 绘制缩放设置
    /// </summary>
    private void DrawResizeSettings()
    {
        // 保持宽高比
        EditorGUILayout.BeginHorizontal();
        _keepAspectRatio = EditorGUILayout.Toggle("保持宽高比", _keepAspectRatio);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);

        // 目标尺寸设置
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("目标宽度:", GUILayout.Width(70));
        int newWidth = EditorGUILayout.IntField(_targetWidth, GUILayout.Width(80));
        
        if (newWidth != _targetWidth)
        {
            _targetWidth = newWidth;
            if (_keepAspectRatio && _sourceTexture != null)
            {
                _targetHeight = Mathf.RoundToInt(_targetWidth * ((float)_sourceTexture.height / _sourceTexture.width));
            }
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("目标高度:", GUILayout.Width(70));
        int newHeight = EditorGUILayout.IntField(_targetHeight, GUILayout.Width(80));
        
        if (newHeight != _targetHeight)
        {
            _targetHeight = newHeight;
            if (_keepAspectRatio && _sourceTexture != null)
            {
                _targetWidth = Mathf.RoundToInt(_targetHeight * ((float)_sourceTexture.width / _sourceTexture.height));
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 缩放算法选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("缩放算法:", GUILayout.Width(70));
        _scaleAlgorithm = (ScaleAlgorithm)EditorGUILayout.EnumPopup(_scaleAlgorithm, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        // 显示算法说明
        string algorithmTip = GetAlgorithmDescription(_scaleAlgorithm);
        EditorGUILayout.HelpBox(algorithmTip, MessageType.None);
    }

    /// <summary>
    /// 获取缩放算法描述
    /// </summary>
    private string GetAlgorithmDescription(ScaleAlgorithm algorithm)
    {
        return algorithm switch
        {
            ScaleAlgorithm.Bilinear => "双线性插值：适用于一般图片的放大和缩小，效果平滑，推荐使用",
            ScaleAlgorithm.Point => "点采样（最近邻）：适用于像素风格图片，保持硬边缘，放大时会有马赛克效果",
            ScaleAlgorithm.Trilinear => "三线性插值：适用于需要高质量缩放的场景，计算量稍大",
            _ => ""
        };
    }

    /// <summary>
    /// 绘制输出设置
    /// </summary>
    private void DrawOutputSettings()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出目录:", GUILayout.Width(70));
        _outputFolder = EditorGUILayout.TextField(_outputFolder);
        if (GUILayout.Button("选择", GUILayout.Width(50), GUILayout.Height(18)))
        {
            SelectOutputFolder();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出文件名:", GUILayout.Width(70));
        _outputFileName = EditorGUILayout.TextField(_outputFileName);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 选择输出文件夹
    /// </summary>
    private void SelectOutputFolder()
    {
        string selected = EditorUtility.OpenFolderPanel("选择输出目录", "Assets", "");
        if (string.IsNullOrEmpty(selected)) return;

        // 转换为相对于项目的路径
        if (selected.Contains("Assets"))
        {
            _outputFolder = "Assets" + selected.Substring(selected.IndexOf("Assets") + 6);
            _outputFolder = _outputFolder.Replace("\\", "/");
        }
        else
        {
            _outputFolder = selected;
        }
    }

    /// <summary>
    /// 绘制缩放按钮
    /// </summary>
    private void DrawResizeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("🖼️ 缩放并保存", GUILayout.Width(120), GUILayout.Height(28)))
        {
            ResizeAndSaveImage();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制缩放结果消息
    /// </summary>
    private void DrawResizeResult()
    {
        if (string.IsNullOrEmpty(_resizeResultMessage)) return;

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(_resizeResultMessage, _resizeResultType);
    }
    #endregion

    #region 图片处理
    /// <summary>
    /// 缩放并保存图片
    /// 支持放大和缩小，使用RenderTexture进行高质量缩放
    /// </summary>
    private void ResizeAndSaveImage()
    {
        if (!ValidateResizeInput(out string errorMessage))
        {
            _resizeResultMessage = errorMessage;
            _resizeResultType = MessageType.Error;
            return;
        }

        try
        {
            // 确保输出目录存在
            EnsureOutputDirectoryExists();

            // 获取源图片的可读数据
            Texture2D sourceToProcess = GetReadableSourceTexture();
            if (sourceToProcess == null)
            {
                _resizeResultMessage = "无法读取源图片数据！";
                _resizeResultType = MessageType.Error;
                return;
            }

            // 执行缩放
            Texture2D targetTexture = ScaleTexture(sourceToProcess, _targetWidth, _targetHeight, _scaleAlgorithm);

            // 编码并保存图片
            string outputPath = SaveTextureToFile(targetTexture, _outputFolder, _outputFileName);

            // 清理临时对象
            CleanupTextures(sourceToProcess, targetTexture);

            // 刷新AssetDatabase（如果是项目内路径）
            if (outputPath.StartsWith("Assets/"))
            {
                AssetDatabase.Refresh();
            }

            _resizeResultMessage = $"图片缩放成功！\n输出路径: {outputPath}\n目标尺寸: {_targetWidth} x {_targetHeight}";
            _resizeResultType = MessageType.Info;

            Debug.Log($"[图片资源处理] 图片缩放完成: {outputPath}");
        }
        catch (Exception ex)
        {
            _resizeResultMessage = $"处理失败: {ex.Message}";
            _resizeResultType = MessageType.Error;
            Debug.LogError($"[图片资源处理] 图片缩放失败: {ex}");
        }
    }

    /// <summary>
    /// 验证缩放输入参数
    /// </summary>
    private bool ValidateResizeInput(out string errorMessage)
    {
        if (_sourceTexture == null)
        {
            errorMessage = "请先选择源图片！";
            return false;
        }

        if (_targetWidth <= 0 || _targetHeight <= 0)
        {
            errorMessage = "目标宽度和高度必须大于0！";
            return false;
        }

        // 限制最大尺寸，防止内存溢出
        const int maxDimension = 8192;
        if (_targetWidth > maxDimension || _targetHeight > maxDimension)
        {
            errorMessage = $"目标尺寸不能超过 {maxDimension} x {maxDimension}！";
            return false;
        }

        if (string.IsNullOrEmpty(_outputFolder))
        {
            errorMessage = "请选择输出目录！";
            return false;
        }

        if (string.IsNullOrEmpty(_outputFileName))
        {
            errorMessage = "请输入输出文件名！";
            return false;
        }

        errorMessage = "";
        return true;
    }

    /// <summary>
    /// 确保输出目录存在
    /// </summary>
    private void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(_outputFolder))
        {
            Directory.CreateDirectory(_outputFolder);
        }
    }

    /// <summary>
    /// 获取可读的源纹理
    /// </summary>
    private Texture2D GetReadableSourceTexture()
    {
        // 如果是项目内资源，需要从文件读取以确保可以获取像素数据
        if (!string.IsNullOrEmpty(_sourceImagePath) && _sourceImagePath.StartsWith("Assets/"))
        {
            string fullPath = Path.Combine(Application.dataPath, "..", _sourceImagePath).Replace("/", "\\");
            fullPath = Path.GetFullPath(fullPath);
            
            if (File.Exists(fullPath))
            {
                byte[] fileData = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                return texture;
            }
            else
            {
                // 尝试直接复制纹理
                return CopyTextureToReadable(_sourceTexture);
            }
        }
        else
        {
            // 已经是内存中的纹理
            return _sourceTexture;
        }
    }

    /// <summary>
    /// 将不可读纹理复制为可读纹理
    /// </summary>
    private Texture2D CopyTextureToReadable(Texture2D source)
    {
        // 使用 sRGB 颜色空间保持一致性，避免颜色变亮
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        
        return readable;
    }

    /// <summary>
    /// 缩放纹理到指定尺寸
    /// </summary>
    /// <param name="source">源纹理</param>
    /// <param name="targetWidth">目标宽度</param>
    /// <param name="targetHeight">目标高度</param>
    /// <param name="algorithm">缩放算法</param>
    /// <returns>缩放后的纹理</returns>
    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight, ScaleAlgorithm algorithm)
    {
        // Point 算法使用 CPU 采样以确保精确的最近邻插值
        if (algorithm == ScaleAlgorithm.Point)
        {
            return ScaleTexturePoint(source, targetWidth, targetHeight);
        }
        
        // Bilinear 和 Trilinear 使用 GPU 渲染
        return ScaleTextureGPU(source, targetWidth, targetHeight, algorithm);
    }

    /// <summary>
    /// 使用最近邻插值（Point）缩放纹理 - CPU 实现确保精确
    /// </summary>
    private Texture2D ScaleTexturePoint(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D targetTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        
        Color[] sourcePixels = source.GetPixels();
        Color[] targetPixels = new Color[targetWidth * targetHeight];
        
        float xRatio = (float)source.width / targetWidth;
        float yRatio = (float)source.height / targetHeight;
        
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                // 最近邻采样
                int sourceX = Mathf.FloorToInt(x * xRatio);
                int sourceY = Mathf.FloorToInt(y * yRatio);
                
                // 边界检查
                sourceX = Mathf.Clamp(sourceX, 0, source.width - 1);
                sourceY = Mathf.Clamp(sourceY, 0, source.height - 1);
                
                int sourceIndex = sourceY * source.width + sourceX;
                int targetIndex = y * targetWidth + x;
                
                targetPixels[targetIndex] = sourcePixels[sourceIndex];
            }
        }
        
        targetTexture.SetPixels(targetPixels);
        targetTexture.Apply();
        
        return targetTexture;
    }

    /// <summary>
    /// 使用 GPU 渲染缩放纹理（Bilinear/Trilinear）
    /// </summary>
    private Texture2D ScaleTextureGPU(Texture2D source, int targetWidth, int targetHeight, ScaleAlgorithm algorithm)
    {
        Texture2D targetTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

        // 临时设置源纹理的 filter mode
        FilterMode originalFilterMode = source.filterMode;
        source.filterMode = GetFilterMode(algorithm);
        
        // 使用 RenderTexture 进行高质量的缩放
        // 使用 ARGB32 格式并启用 sRGB 读写，保持颜色空间一致避免变亮
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.filterMode = source.filterMode;
        
        // 保存当前的 active RenderTexture
        RenderTexture previousRT = RenderTexture.active;
        
        // 使用 Graphics.Blit 进行绘制，它会尊重纹理的 filter mode
        Graphics.Blit(source, rt);
        
        // 读取像素到目标纹理
        RenderTexture.active = rt;
        targetTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        targetTexture.Apply();
        
        // 恢复状态
        RenderTexture.active = previousRT;
        source.filterMode = originalFilterMode;
        RenderTexture.ReleaseTemporary(rt);

        return targetTexture;
    }

    /// <summary>
    /// 获取FilterMode
    /// </summary>
    private FilterMode GetFilterMode(ScaleAlgorithm algorithm)
    {
        return algorithm switch
        {
            ScaleAlgorithm.Point => FilterMode.Point,
            ScaleAlgorithm.Bilinear => FilterMode.Bilinear,
            ScaleAlgorithm.Trilinear => FilterMode.Trilinear,
            _ => FilterMode.Bilinear
        };
    }

    /// <summary>
    /// 将纹理保存为文件
    /// </summary>
    private string SaveTextureToFile(Texture2D texture, string folder, string fileName)
    {
        // 编码为PNG
        byte[] bytes = texture.EncodeToPNG();

        // 确保文件名有正确的扩展名
        string finalFileName = EnsureFileExtension(fileName);

        string outputPath = Path.Combine(folder, finalFileName);
        outputPath = outputPath.Replace("\\", "/");

        // 保存文件
        File.WriteAllBytes(outputPath, bytes);

        return outputPath;
    }

    /// <summary>
    /// 确保文件名有正确的扩展名
    /// </summary>
    private string EnsureFileExtension(string fileName)
    {
        string lowerName = fileName.ToLowerInvariant();
        if (lowerName.EndsWith(".png") || lowerName.EndsWith(".jpg") || 
            lowerName.EndsWith(".jpeg") || lowerName.EndsWith(".bmp") ||
            lowerName.EndsWith(".tga"))
        {
            return fileName;
        }
        return fileName + ".png";
    }

    /// <summary>
    /// 清理临时纹理对象
    /// </summary>
    private void CleanupTextures(Texture2D sourceToProcess, Texture2D targetTexture)
    {
        if (sourceToProcess != _sourceTexture)
        {
            DestroyImmediate(sourceToProcess);
        }
        DestroyImmediate(targetTexture);
    }
    #endregion

    #region 批量处理
    /// <summary>
    /// 绘制工具栏按钮
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        GUILayout.Label("批量导入设置", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("+ 添加配置规则", 150, 24))
        {
            ImageResSaveData.listSaveData.Add(new ImageResBeanItemBean());
            SaveAllData();
        }
        GUILayout.FlexibleSpace();
        if (EditorUI.GUIButton("刷新数据", 100, 24))
        {
            InitData();
        }
        if (EditorUI.GUIButton("保存所有数据", 120, 24))
        {
            SaveAllData();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("刷新所有图片", 150, 24))
        {
            RefreshAllImages();
        }
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (EditorUI.GUIButton("清除所有数据", 120, 24))
        {
            if (EditorUI.GUIDialog("确认", "是否清除所有配置数据？此操作不可撤销。"))
            {
                FileUtil.DeleteFile($"{PathSaveData}/{SaveDataFileName}");
                InitData();
                _foldoutStates.Clear();
                EditorUtil.RefreshAsset();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // 显示当前配置数量
        int count = ImageResSaveData?.listSaveData?.Count ?? 0;
        EditorGUILayout.LabelField($"当前配置规则数: {count}", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制配置规则列表
    /// </summary>
    private void DrawConfigList()
    {
        if (ImageResSaveData == null || ImageResSaveData.listSaveData.IsNull())
            return;

        for (int i = 0; i < ImageResSaveData.listSaveData.Count; i++)
        {
            DrawConfigItem(i, ImageResSaveData.listSaveData[i]);
        }
    }

    /// <summary>
    /// 绘制单条配置规则UI
    /// </summary>
    private void DrawConfigItem(int index, ImageResBeanItemBean itemData)
    {
        if (!_foldoutStates.ContainsKey(itemData))
            _foldoutStates[itemData] = true;

        EditorGUILayout.BeginVertical(_boxStyle);

        // 标题栏：折叠 + 序号 + 路径预览 + 操作按钮
        EditorGUILayout.BeginHorizontal();

        string displayName = string.IsNullOrEmpty(itemData.pathRes) ? "(未设置路径)" : itemData.pathRes;
        _foldoutStates[itemData] = EditorGUILayout.Foldout(_foldoutStates[itemData], $"[{index + 1}] {displayName}", true, EditorStyles.foldoutHeader);

        GUILayout.FlexibleSpace();

        if (EditorUI.GUIButton("刷新资源", 80, 20))
        {
            RefreshImage(itemData);
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (EditorUI.GUIButton("删除", 50, 20))
        {
            if (EditorUI.GUIDialog("确认", $"是否删除配置规则 [{index + 1}]？"))
            {
                _foldoutStates.Remove(itemData);
                ImageResSaveData.listSaveData.Remove(itemData);
                SaveAllData();
                GUIUtility.ExitGUI();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // 折叠内容
        if (_foldoutStates.ContainsKey(itemData) && _foldoutStates[itemData])
        {
            DrawConfigItemDetails(itemData);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制配置规则详情
    /// </summary>
    private void DrawConfigItemDetails(ImageResBeanItemBean itemData)
    {
        GUILayout.Space(4);
        EditorGUI.indentLevel++;

        // 路径设置
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("图片路径:", GUILayout.Width(70));
        itemData.pathRes = EditorGUILayout.TextField(itemData.pathRes);
        if (GUILayout.Button("选择", GUILayout.Width(50), GUILayout.Height(18)))
        {
            SelectConfigItemPath(itemData);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);

        // 导入参数
        itemData.textureImporterType = (int)EditorUI.GUIEnum<TextureImporterType>("纹理类型:", itemData.textureImporterType, 350);
        itemData.textureImporterCompression = (int)EditorUI.GUIEnum<TextureImporterCompression>("压缩质量:", itemData.textureImporterCompression, 350);
        itemData.wrapMode = (int)EditorUI.GUIEnum<TextureWrapMode>("Wrap Mode:", itemData.wrapMode, 350);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("最大尺寸:", GUILayout.Width(70));
        itemData.maxTextureSize = EditorGUILayout.IntField(itemData.maxTextureSize, GUILayout.Width(80));
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Pixels Per Unit:", GUILayout.Width(100));
        itemData.spritePixelsPerUnit = EditorGUILayout.IntField(itemData.spritePixelsPerUnit, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 选择配置规则的路径
    /// </summary>
    private void SelectConfigItemPath(ImageResBeanItemBean itemData)
    {
        string selected = EditorUI.GetFolderPanel("选择图片资源目录", "Assets");
        if (string.IsNullOrEmpty(selected)) return;

        // 转换为相对于项目的路径
        if (selected.Contains("Assets"))
        {
            itemData.pathRes = "Assets" + selected.Substring(selected.IndexOf("Assets") + 6);
        }
        else
        {
            itemData.pathRes = selected;
        }
        SaveAllData();
    }

    /// <summary>
    /// 刷新所有配置规则对应的图片
    /// </summary>
    private void RefreshAllImages()
    {
        if (ImageResSaveData.listSaveData.IsNull())
            return;

        int totalGroups = ImageResSaveData.listSaveData.Count;
        for (int i = 0; i < totalGroups; i++)
        {
            var data = ImageResSaveData.listSaveData[i];
            EditorUI.GUIShowProgressBar("刷新图片资源",
                $"正在处理 ({i + 1}/{totalGroups}): {data.pathRes}",
                (float)i / totalGroups);
            RefreshImage(data);
        }
        EditorUI.GUIHideProgressBar();
    }

    /// <summary>
    /// 刷新单条规则对应目录的图片导入设置
    /// </summary>
    protected void RefreshImage(ImageResBeanItemBean data)
    {
        if (string.IsNullOrEmpty(data.pathRes))
            return;

        FileInfo[] arrayFile = FileUtil.GetFilesByPath(data.pathRes);
        if (arrayFile == null || arrayFile.Length == 0)
            return;

        for (int i = 0; i < arrayFile.Length; i++)
        {
            FileInfo fileInfo = arrayFile[i];
            if (fileInfo.Name.Contains(".meta"))
                continue;
            EditorUtil.SetTextureData($"{data.pathRes}/{fileInfo.Name}",
                spritePixelsPerUnit: data.spritePixelsPerUnit,
                wrapMode: (TextureWrapMode)data.wrapMode,
                textureImporterType: (TextureImporterType)data.textureImporterType,
                textureImporterCompression: (TextureImporterCompression)data.textureImporterCompression,
                maxTextureSize: data.maxTextureSize);
        }
        EditorUtil.RefreshAsset();
    }

    /// <summary>
    /// 保存所有数据
    /// </summary>
    protected void SaveAllData()
    {
        string saveData = JsonUtil.ToJsonByNet(ImageResSaveData);
        FileUtil.CreateTextFile(PathSaveData, SaveDataFileName, saveData);
        EditorUtil.RefreshAsset();
    }
    #endregion
}
