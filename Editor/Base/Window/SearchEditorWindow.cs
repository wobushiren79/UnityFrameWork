using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SearchEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/查找资源被哪些prefab引用")]
    static void DoSearchRefrence()
    {
        var window = GetWindow<SearchEditorWindow>(false, "资源引用查找器", true);
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private Object searchObject;
    private List<Object> result = new List<Object>();
    private Vector2 scrollPosition;
    private bool isSearching = false;
    private string searchStatus = "";
    private int totalFilesToCheck = 0;
    private int checkedFiles = 0;
    private IEnumerator searchCoroutine;
    
    // 支持查找的资源类型
    private static readonly string[] SupportedExtensions = 
    {
        ".prefab",
        ".mat",
        ".asset",
        ".controller",
        ".anim",
        ".unity",
        ".rendertexture",
        ".spriteatlas",
        ".shadergraph",
        ".shadervariants",
        ".lighting"
    };

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // 搜索区域
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            GUILayout.Label("目标资源:", GUILayout.Width(60));
            searchObject = EditorGUILayout.ObjectField(
                searchObject, 
                typeof(Object), 
                false, 
                GUILayout.ExpandWidth(true));
            
            EditorGUI.BeginDisabledGroup(searchObject == null || isSearching);
            if (GUILayout.Button("开始查找", GUILayout.Width(80)))
            {
                StartSearch();
            }
            EditorGUI.EndDisabledGroup();
            
            if (isSearching && GUILayout.Button("取消", GUILayout.Width(60)))
            {
                CancelSearch();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 状态显示
        if (!string.IsNullOrEmpty(searchStatus))
        {
            EditorGUILayout.HelpBox(searchStatus, MessageType.Info);
            
            if (totalFilesToCheck > 0)
            {
                float progress = Mathf.Clamp01(checkedFiles / (float)totalFilesToCheck);
                Rect progressRect = GUILayoutUtility.GetRect(200, 20);
                EditorGUI.ProgressBar(progressRect, progress, $"检查进度: {checkedFiles}/{totalFilesToCheck}");
            }
        }
        
        EditorGUILayout.Space(10);
        
        // 结果显示区域
        GUILayout.Label($"找到 {result.Count} 个引用:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            if (result.Count == 0 && !isSearching)
            {
                EditorGUILayout.HelpBox("没有找到引用或未开始查找", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < result.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        // 显示序号
                        GUILayout.Label($"{i + 1}.", GUILayout.Width(30));
                        
                        // 显示资源
                        EditorGUILayout.ObjectField(result[i], typeof(Object), true);
                        
                        // 添加一个快速选择按钮
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.PingObject(result[i]);
                        }
                        
                        // 添加一个在Project窗口中定位的按钮
                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            Selection.activeObject = result[i];
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void StartSearch()
    {
        if (searchObject == null) return;
        
        result.Clear();
        isSearching = true;
        searchStatus = "正在准备查找资源引用...";
        checkedFiles = 0;
        
        string assetPath = AssetDatabase.GetAssetPath(searchObject);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        
        // 获取所有需要检查的文件
        var allGuids = new List<string>();
        foreach (var extension in SupportedExtensions)
        {
            string searchType = GetSearchTypeFromExtension(extension);
            if (!string.IsNullOrEmpty(searchType))
            {
                var guids = AssetDatabase.FindAssets(searchType);
                allGuids.AddRange(guids);
            }
        }
        
        totalFilesToCheck = allGuids.Count;
        searchStatus = $"正在检查 {totalFilesToCheck} 个资源文件...";
        
        // 启动协程进行查找
        searchCoroutine = FindReferencesCoroutine(allGuids, assetGuid);
        EditorApplication.update += UpdateSearchCoroutine;
    }

    private IEnumerator FindReferencesCoroutine(List<string> guids, string targetGuid)
    {
        result.Clear();
        checkedFiles = 0;
        
        // 每帧检查的文件数，避免阻塞UI
        int filesPerFrame = 50;
        
        for (int i = 0; i < guids.Count; i++)
        {
            if (!isSearching) break;
            
            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            
            if (CheckFileForGuid(filePath, targetGuid))
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
                if (obj != null)
                {
                    result.Add(obj);
                }
            }
            
            checkedFiles++;
            
            // 每检查一定数量的文件后，让出一帧
            if (i % filesPerFrame == 0)
            {
                yield return null;
            }
        }
        
        searchStatus = $"查找完成！共找到 {result.Count} 个引用";
        isSearching = false;
        searchCoroutine = null;
        
        Repaint();
    }

    private bool CheckFileForGuid(string filePath, string targetGuid)
    {
        try
        {
            // 跳过不存在的文件
            if (!File.Exists(filePath)) return false;
            
            // 检查文件大小，避免检查过大的文件
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 1024 * 1024) // 大于1MB的文件，使用更高效的方法
            {
                // 对于大文件，使用流式读取，避免一次性加载整个文件
                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    char[] buffer = new char[4096];
                    int charsRead;
                    while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string chunk = new string(buffer, 0, charsRead);
                        if (chunk.Contains(targetGuid))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                // 对于小文件，直接读取全部内容
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                return content.Contains(targetGuid);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"检查文件 {filePath} 时出错: {ex.Message}");
        }
        
        return false;
    }

    private void UpdateSearchCoroutine()
    {
        if (searchCoroutine != null)
        {
            bool hasNext = searchCoroutine.MoveNext();
            if (!hasNext)
            {
                EditorApplication.update -= UpdateSearchCoroutine;
                searchCoroutine = null;
            }
        }
        
        // 强制刷新UI以显示进度
        Repaint();
    }

    private void CancelSearch()
    {
        isSearching = false;
        searchStatus = "查找已取消";
        
        if (searchCoroutine != null)
        {
            EditorApplication.update -= UpdateSearchCoroutine;
            searchCoroutine = null;
        }
        
        Repaint();
    }

    private string GetSearchTypeFromExtension(string extension)
    {
        return extension switch
        {
            ".prefab" => "t:Prefab",
            ".mat" => "t:Material",
            ".asset" => "t:ScriptableObject",
            ".controller" => "t:AnimatorController",
            ".anim" => "t:AnimationClip",
            ".unity" => "t:Scene",
            ".rendertexture" => "t:RenderTexture",
            ".spriteatlas" => "t:SpriteAtlas",
            ".shadergraph" => "t:Shader",
            ".shadervariants" => "t:ShaderVariantCollection",
            ".lighting" => "t:LightingSettings",
            _ => ""
        };
    }

    // 添加一个静态方法供其他地方调用
    public static List<Object> FindReferencesToAsset(Object targetAsset)
    {
        if (targetAsset == null) return new List<Object>();
        
        string assetPath = AssetDatabase.GetAssetPath(targetAsset);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        
        var result = new List<Object>();
        var allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (var guid in allPrefabs)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guid);
            if (File.ReadAllText(filePath).Contains(assetGuid))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
                if (obj != null) result.Add(obj);
            }
        }
        
        return result;
    }

    private void OnDestroy()
    {
        // 清理协程监听
        if (searchCoroutine != null)
        {
            EditorApplication.update -= UpdateSearchCoroutine;
        }
    }

    // 添加右键菜单功能
    [MenuItem("Assets/查找引用", false, 30)]
    private static void FindReferencesContextMenu()
    {
        var selected = Selection.activeObject;
        if (selected != null)
        {
            var window = GetWindow<SearchEditorWindow>(false, "资源引用查找器", true);
            window.searchObject = selected;
            window.StartSearch();
        }
    }

    [MenuItem("Assets/查找引用", true)]
    private static bool ValidateFindReferencesContextMenu()
    {
        return Selection.activeObject != null && AssetDatabase.Contains(Selection.activeObject);
    }
}