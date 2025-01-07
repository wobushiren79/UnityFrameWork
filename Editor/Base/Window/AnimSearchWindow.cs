using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(AnimSearchWindow))]
public class AnimSearchWindow : EditorWindow
{
    //搜索key
    public string searchKeyword = "";
    //目标动画
    public AnimatorController targetAnimatorController;
    //搜索结果
    public List<ChildAnimatorState> listSearchAnim = new List<ChildAnimatorState>();
    public List<ChildAnimatorState> listSearchClip = new List<ChildAnimatorState>();
    //滚动位置
    public Vector2 scrollPosition = Vector2.zero;
    public string animWindowPath = "Window/Animation/Animator";

    [MenuItem("Custom/工具弹窗/Animator搜索动画")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        AnimSearchWindow window = (AnimSearchWindow)EditorWindow.GetWindow(typeof(AnimSearchWindow));
        window.Show();
    }

    void OnGUI()
    {
        UIForSearch();
        GUILayout.Space(20);
        if (listSearchAnim.Count > 0 || listSearchClip.Count > 0)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < listSearchAnim.Count; i++)
            {
                var itemData = listSearchAnim[i];
                UIForSearchItemState(itemData);
            }
            GUILayout.Space(10);
            for (int i = 0; i < listSearchClip.Count; i++)
            {
                var itemData = listSearchClip[i];
                UIForSearchItemClip(itemData);
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 搜索按钮
    /// </summary>
    public void UIForSearch()
    {
        targetAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("目标animtor", (Object)targetAnimatorController, typeof(AnimatorController), true);
        if (targetAnimatorController == null)
        {
            if (listSearchAnim.Count > 0)
            {
                listSearchAnim.Clear();
            }
            if (listSearchClip.Count > 0)
            {
                listSearchClip.Clear();
            }
        }
        searchKeyword = EditorGUILayout.TextField("输入搜索名字:", searchKeyword);
        if (GUILayout.Button("开始搜索"))
        {
            if (targetAnimatorController == null)
            {
                LogUtil.LogError("没有目标animator");
                return;
            }
            scrollPosition = Vector2.zero;
            listSearchAnim.Clear();
            listSearchClip.Clear();
            LogUtil.Log("开始搜索");
            if (targetAnimatorController != null)
            {
                EditorApplication.ExecuteMenuItem(animWindowPath);
                Selection.activeObject = targetAnimatorController;

                AnimatorStateMachine rootStateMachine = targetAnimatorController.layers[0].stateMachine;
                ChildAnimatorState[] states = rootStateMachine.states;
                LogUtil.Log($"搜索到states_{states.Length}");
                foreach (ChildAnimatorState state in states)
                {
                    if (state.state.name.ToLower().Contains(searchKeyword.ToLower()))
                    {
                        LogUtil.Log($"item states_{state.state.name} position_{state.position}");
                        listSearchAnim.Add(state);
                    }
                    // 获取AnimationClip
                    AnimationClip itemClip = state.state.motion as AnimationClip;
                    if (itemClip != null && itemClip.name.ToLower().Contains(searchKeyword.ToLower()))
                    {
                        LogUtil.Log($"item itemclip_{itemClip.name} position_{state.position}");
                        listSearchClip.Add(state);
                    }
                }
            }
            else
            {
                LogUtil.Log("未搜索到数据");
            }
        }
    }

    /// <summary>
    /// 单个搜索结果UI
    /// </summary>
    public void UIForSearchItemState(ChildAnimatorState itemData)
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("state:", GUILayout.Width(60));
        if (GUILayout.Button($"{itemData.state.name}"))
        {
            Selection.activeObject = targetAnimatorController;
            EditorApplication.ExecuteMenuItem(animWindowPath);

            Selection.activeObject = itemData.state;
            // 将Scene视图焦点移动到状态
            SceneView.FrameLastActiveSceneView();
            SceneView.lastActiveSceneView.FrameSelected();
            //EditorGUIUtility.PingObject(state.state);
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 单个搜索结果UI
    /// </summary>
    public void UIForSearchItemClip(ChildAnimatorState itemData)
    {
        // 获取AnimationClip
        AnimationClip itemClip = itemData.state.motion as AnimationClip;
        if (itemClip != null)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("clip:", GUILayout.Width(60));
            if (GUILayout.Button($"{itemClip.name}"))
            {
                Selection.activeObject = targetAnimatorController;
                EditorApplication.ExecuteMenuItem(animWindowPath);


                Selection.activeObject = itemData.state;
                // 将Scene视图焦点移动到状态
                SceneView.FrameLastActiveSceneView();
                SceneView.lastActiveSceneView.FrameSelected();
                //EditorGUIUtility.PingObject(state.state);
            }
            GUILayout.EndHorizontal();
        }
    }
}