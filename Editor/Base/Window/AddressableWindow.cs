using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

public class AddressableWindow : EditorWindow
{
    [InitializeOnLoadMethod]
    static void EditorApplication_ProjectChanged()
    {
        AddressableUtil.AddCallBackForAssetChange(HandleForAssetChange);
    }

    [MenuItem("Custom/工具弹窗/资源Addressable")]
    static void CreateWindows()
    {
        var window = GetWindow<AddressableWindow>();
        window.titleContent = new GUIContent("Addressable资源管理");
        window.minSize = new Vector2(800, 400);
    }

    protected Vector2 scrollPosition;
    protected static List<AddressableAssetGroup> allGroup;
    protected static AddressableSaveBean addressableSaveData;

    protected static string pathSaveData = "Assets/Data/Addressable";
    protected static string saveDataFileName = "AddressableSaveData";

    // 记录各Group的折叠状态
    private readonly Dictionary<string, bool> foldoutStates = new();

    public void OnEnable()
    {
        InitData();
    }

    public void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();

        UIForBase();
        EditorGUILayout.Space(5);
        UIForListGroup();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitData()
    {
        allGroup = AddressableUtil.FindAllGrop();
        FileUtil.CreateDirectory(pathSaveData);

        string dataSave = FileUtil.LoadTextFile($"{pathSaveData}/{saveDataFileName}");
        if (dataSave.IsNull())
        {
            addressableSaveData = new AddressableSaveBean();
        }
        else
        {
            addressableSaveData = JsonUtil.FromJsonByNet<AddressableSaveBean>(dataSave);
        }

        // 容错：剔除已不存在的Group数据
        HashSet<string> existingGroupNames = new HashSet<string>(allGroup.Select(g => g.Name));
        List<string> listRemove = addressableSaveData.dicSaveData.Keys
            .Where(key => !existingGroupNames.Contains(key))
            .ToList();

        if (listRemove.Count > 0)
        {
            foreach (var key in listRemove)
            {
                addressableSaveData.dicSaveData.Remove(key);
            }
            SaveData();
        }
    }

    /// <summary>
    /// 保存数据到文件
    /// </summary>
    private static void SaveData()
    {
        string saveData = JsonUtil.ToJsonByNet(addressableSaveData);
        FileUtil.CreateTextFile(pathSaveData, saveDataFileName, saveData);
        EditorUtil.RefreshAsset();
    }

    private void SetAllFoldout(bool expand)
    {
        var keys = new List<string>(foldoutStates.Keys);
        foreach (var key in keys)
            foldoutStates[key] = expand;
    }

    /// <summary>
    /// 基础操作栏
    /// </summary>
    public void UIForBase()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (EditorUI.GUIButton("刷新所有资源", 120))
        {
            InitData();
            HandleForAllAssetChange();
        }
        if (EditorUI.GUIButton("保存所有数据", 120))
        {
            SaveData();
            EditorUtility.DisplayDialog("提示", "数据保存成功", "确定");
        }
        if (EditorUI.GUIButton("清除所有数据", 120))
        {
            if (EditorUI.GUIDialog("确认", "是否清除所有数据"))
            {
                FileUtil.DeleteFile($"{pathSaveData}/{saveDataFileName}");
                InitData();
                EditorUtil.RefreshAsset();
            }
        }

        GUILayout.FlexibleSpace();

        if (EditorUI.GUIButton("全部展开", 80))
        {
            SetAllFoldout(true);
        }
        if (EditorUI.GUIButton("全部折叠", 80))
        {
            SetAllFoldout(false);
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Group列表
    /// </summary>
    public void UIForListGroup()
    {
        if (allGroup.IsNull())
            return;

        for (int i = 0; i < allGroup.Count; i++)
        {
            AddressableAssetGroup itemGroup = allGroup[i];
            if (!addressableSaveData.dicSaveData.TryGetValue(itemGroup.name, out AddressableSaveItemBean value))
            {
                value = new AddressableSaveItemBean();
                addressableSaveData.dicSaveData.Add(itemGroup.name, value);
            }
            UIForItemGroup(itemGroup, value);
        }
    }

    /// <summary>
    /// 单个Group的UI
    /// </summary>
    public void UIForItemGroup(AddressableAssetGroup itemGroup, AddressableSaveItemBean value)
    {
        if (value == null)
            return;

        // 折叠标题
        if (!foldoutStates.ContainsKey(itemGroup.name))
            foldoutStates[itemGroup.name] = false;

        EditorGUILayout.BeginVertical("box");

        foldoutStates[itemGroup.name] = EditorGUILayout.Foldout(foldoutStates[itemGroup.name], itemGroup.name, true, EditorStyles.foldoutHeader);

        if (foldoutStates[itemGroup.name])
        {
            EditorGUI.indentLevel++;

            // 文件路径区域
            UIForPathList(value);

            EditorGUILayout.Space(3);

            // Label区域
            UIForLabelList(value);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 路径列表UI
    /// </summary>
    private void UIForPathList(AddressableSaveItemBean value)
    {
        GUILayout.BeginHorizontal();
        EditorUI.GUIText("文件路径地址：", 100);
        if (EditorUI.GUIButton("+", 25))
        {
            value.listPathSave.Add("");
        }
        GUILayout.EndHorizontal();

        for (int i = 0; i < value.listPathSave.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            // 支持拖拽文件夹设置路径
            Rect dropRect = EditorGUILayout.GetControlRect(GUILayout.Width(500), GUILayout.Height(20));
            value.listPathSave[i] = EditorGUI.TextField(dropRect, value.listPathSave[i]);
            HandleDragAndDrop(dropRect, (path) => { value.listPathSave[i] = path; });

            if (EditorUI.GUIButton("选择", 40))
            {
                string folder = EditorUI.GetFolderPanel("选择文件夹路径");
                if (!string.IsNullOrEmpty(folder))
                {
                    // 转换为相对于Assets的路径
                    int assetsIndex = folder.IndexOf("Assets");
                    if (assetsIndex >= 0)
                        value.listPathSave[i] = folder.Substring(assetsIndex);
                    else
                        value.listPathSave[i] = folder;
                }
            }
            if (EditorUI.GUIButton("-", 25))
            {
                value.listPathSave.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Label列表UI
    /// </summary>
    private void UIForLabelList(AddressableSaveItemBean value)
    {
        GUILayout.BeginHorizontal();
        EditorUI.GUIText("Label：", 100);
        if (EditorUI.GUIButton("+", 25))
        {
            value.listLabel.Add("");
        }
        GUILayout.EndHorizontal();

        for (int i = 0; i < value.listLabel.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            value.listLabel[i] = EditorUI.GUIEditorText(value.listLabel[i], 300);
            if (EditorUI.GUIButton("-", 25))
            {
                value.listLabel.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 处理拖拽文件夹到输入框
    /// </summary>
    private void HandleDragAndDrop(Rect dropRect, System.Action<string> onDrop)
    {
        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    evt.Use();
                }
                break;
            case EventType.DragPerform:
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.AcceptDrag();
                    string path = DragAndDrop.paths[0];
                    // 如果是文件，取其所在目录
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        int lastSlash = path.LastIndexOf("/");
                        if (lastSlash > 0)
                            path = path.Substring(0, lastSlash);
                    }
                    onDrop?.Invoke(path);
                    evt.Use();
                }
                break;
        }
    }

    /// <summary>
    /// 所有资源刷新
    /// </summary>
    public static void HandleForAllAssetChange()
    {
        List<AddressableAssetEntry> listAllData = AddressableUtil.FindAllAsset();
        List<AddressableAssetEntry> listData = listAllData
            .Where(entry => !entry.parentGroup.name.Equals("Built In Data"))
            .ToList();
        HandleForRefreshAssets(listData);
    }

    /// <summary>
    /// 资源修改监听处理
    /// </summary>
    public static void HandleForAssetChange(AddressableAssetSettings addressableAsset, ModificationEvent modificationEvent, object obj)
    {
        if (modificationEvent == ModificationEvent.EntryAdded && obj is List<AddressableAssetEntry> listData)
        {
            HandleForRefreshAssets(listData);
        }
    }

    /// <summary>
    /// 刷新Addressable资源
    /// </summary>
    public static void HandleForRefreshAssets(List<AddressableAssetEntry> listChangeAssetEntry)
    {
        InitData();
        if (addressableSaveData == null || listChangeAssetEntry == null || listChangeAssetEntry.Count == 0)
            return;

        try
        {
            for (int i = 0; i < listChangeAssetEntry.Count; i++)
            {
                AddressableAssetEntry itemAssetEntry = listChangeAssetEntry[i];
                float progress = (float)i / listChangeAssetEntry.Count;
                EditorUI.GUIShowProgressBar("刷新进度", $"({i + 1}/{listChangeAssetEntry.Count}) {itemAssetEntry.address}", progress);

                string assetPath = itemAssetEntry.AssetPath;
                int lastSlash = assetPath.LastIndexOf("/");
                if (lastSlash <= 0)
                    continue;

                string assetPathFile = assetPath.Substring(0, lastSlash);

                foreach (var itemSaveGroup in addressableSaveData.dicSaveData)
                {
                    string groupName = itemSaveGroup.Key;
                    List<string> listSavePath = itemSaveGroup.Value.listPathSave;

                    bool matched = false;
                    for (int f = 0; f < listSavePath.Count; f++)
                    {
                        if (!string.IsNullOrEmpty(listSavePath[f]) && assetPathFile.Contains(listSavePath[f]))
                        {
                            AddressableUtil.MoveAssetEntry(itemAssetEntry, groupName);
                            AddressableUtil.ClearAllLabel(itemAssetEntry);
                            AddressableUtil.SetLabel(itemAssetEntry, itemSaveGroup.Value.listLabel);
                            matched = true;
                            break;
                        }
                    }
                    if (matched) break;
                }
            }
        }
        finally
        {
            EditorUI.GUIHideProgressBar();
            EditorUtil.RefreshAsset();
        }
    }
}
