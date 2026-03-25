using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimSearchWindow : EditorWindow
{
    private string searchKeyword = "";
    private AnimatorController targetAnimatorController;
    private AnimatorController lastAnimatorController;

    private List<SearchResult> listSearchState = new List<SearchResult>();
    private List<SearchResult> listSearchClip = new List<SearchResult>();
    private Vector2 scrollPosition = Vector2.zero;
    private const string animWindowPath = "Window/Animation/Animator";
    private bool autoSearch = true;

    private struct SearchResult
    {
        public ChildAnimatorState state;
        public string layerName;
        public string subStateMachinePath;
    }

    [MenuItem("Custom/工具弹窗/Animator搜索动画")]
    static void Init()
    {
        AnimSearchWindow window = GetWindow<AnimSearchWindow>("Animator动画搜索");
        window.minSize = new Vector2(350, 300);
        window.Show();
    }

    void OnGUI()
    {
        DrawToolbar();
        GUILayout.Space(5);
        DrawSearchArea();
        GUILayout.Space(5);
        DrawResults();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        autoSearch = GUILayout.Toggle(autoSearch, "实时搜索", EditorStyles.toolbarButton, GUILayout.Width(70));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("清空结果", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            ClearResults();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchArea()
    {
        targetAnimatorController = (AnimatorController)EditorGUILayout.ObjectField(
            "目标Animator", targetAnimatorController, typeof(AnimatorController), false);

        if (targetAnimatorController == null && lastAnimatorController != null)
        {
            ClearResults();
            lastAnimatorController = null;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        searchKeyword = EditorGUILayout.TextField("搜索关键字", searchKeyword);
        bool keywordChanged = EditorGUI.EndChangeCheck();

        if (!autoSearch)
        {
            if (GUILayout.Button("搜索", GUILayout.Width(60)))
            {
                DoSearch();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (autoSearch && (keywordChanged || targetAnimatorController != lastAnimatorController))
        {
            DoSearch();
        }
    }

    private void DrawResults()
    {
        int totalCount = listSearchState.Count + listSearchClip.Count;
        if (targetAnimatorController != null && !string.IsNullOrEmpty(searchKeyword))
        {
            EditorGUILayout.LabelField(
                $"搜索结果: State({listSearchState.Count}) Clip({listSearchClip.Count}) 共{totalCount}条",
                EditorStyles.boldLabel);
        }

        if (totalCount == 0) return;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (listSearchState.Count > 0)
        {
            EditorGUILayout.LabelField("--- State 匹配 ---", EditorStyles.miniLabel);
            for (int i = 0; i < listSearchState.Count; i++)
            {
                DrawResultItem(listSearchState[i], true);
            }
        }

        if (listSearchClip.Count > 0)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("--- Clip 匹配 ---", EditorStyles.miniLabel);
            for (int i = 0; i < listSearchClip.Count; i++)
            {
                DrawResultItem(listSearchClip[i], false);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResultItem(SearchResult result, bool isState)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        string label = isState ? "State" : "Clip";
        string displayName = isState
            ? result.state.state.name
            : (result.state.state.motion as AnimationClip != null ? (result.state.state.motion as AnimationClip).name : "null");
        string path = string.IsNullOrEmpty(result.subStateMachinePath)
            ? result.layerName
            : $"{result.layerName}/{result.subStateMachinePath}";

        EditorGUILayout.LabelField($"[{label}]", GUILayout.Width(40));

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button(displayName, EditorStyles.linkLabel))
        {
            SelectState(result);
        }
        EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DoSearch()
    {
        lastAnimatorController = targetAnimatorController;
        scrollPosition = Vector2.zero;
        listSearchState.Clear();
        listSearchClip.Clear();

        if (targetAnimatorController == null || string.IsNullOrEmpty(searchKeyword))
            return;

        string keyword = searchKeyword.ToLower();

        for (int layerIndex = 0; layerIndex < targetAnimatorController.layers.Length; layerIndex++)
        {
            var layer = targetAnimatorController.layers[layerIndex];
            SearchStateMachineRecursive(layer.stateMachine, layer.name, "", keyword);
        }
    }

    private void SearchStateMachineRecursive(AnimatorStateMachine stateMachine, string layerName, string path, string keyword)
    {
        foreach (var childState in stateMachine.states)
        {
            string stateName = childState.state.name.ToLower();
            if (stateName.Contains(keyword))
            {
                listSearchState.Add(new SearchResult
                {
                    state = childState,
                    layerName = layerName,
                    subStateMachinePath = path
                });
            }

            AnimationClip clip = childState.state.motion as AnimationClip;
            if (clip != null && clip.name.ToLower().Contains(keyword))
            {
                listSearchClip.Add(new SearchResult
                {
                    state = childState,
                    layerName = layerName,
                    subStateMachinePath = path
                });
            }
        }

        foreach (var childSM in stateMachine.stateMachines)
        {
            string subPath = string.IsNullOrEmpty(path)
                ? childSM.stateMachine.name
                : $"{path}/{childSM.stateMachine.name}";
            SearchStateMachineRecursive(childSM.stateMachine, layerName, subPath, keyword);
        }
    }

    private void SelectState(SearchResult result)
    {
        if (targetAnimatorController == null) return;

        Selection.activeObject = targetAnimatorController;
        EditorApplication.ExecuteMenuItem(animWindowPath);
        EditorApplication.delayCall += () =>
        {
            Selection.activeObject = result.state.state;
        };
    }

    private void ClearResults()
    {
        listSearchState.Clear();
        listSearchClip.Clear();
        searchKeyword = "";
        scrollPosition = Vector2.zero;
        Repaint();
    }
}
