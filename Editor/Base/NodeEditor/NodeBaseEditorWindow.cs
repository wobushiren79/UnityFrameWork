using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

public class NodeBaseEditorWindow : EditorWindow
{
    private NodeBaseView nodeView;

    [MenuItem("Custom/Node/NodeBaseEditor")]
    public static void OpenWindow()
    {
        var window = GetWindow<NodeBaseEditorWindow>();
        window.titleContent = new GUIContent("NodeBaseEditor");
    }

    public void OnEnable()
    {
        CreateGraphView();
    }

    public void OnDestroy()
    {
        rootVisualElement.Remove(nodeView);
    }

    /// <summary>
    /// 构造节点面板
    /// </summary>
    public virtual void CreateGraphView()
    {
        nodeView = new NodeBaseView
        {
            name = "NodeView"
        };

        nodeView.StretchToParentSize();
        rootVisualElement.Add(nodeView);

        //添加目录
        var toolbar = new Toolbar();

        var addButton = new Button(OnClickForToolBarAddNode);
        addButton.text = "添加节点";
        toolbar.Add(addButton);

        var saveButton = new Button(OnClickForToolBarSave);
        saveButton.text = "保存";
        toolbar.Add(saveButton);

        var loadButton = new Button(OnClickForToolBarLoad);
        loadButton.text = "读取";
        toolbar.Add(loadButton);

        rootVisualElement.Add(toolbar);
    }

    /// <summary>
    /// 点击-增加节点
    /// </summary>
    public virtual void OnClickForToolBarAddNode()
    {
        NodeBase node = nodeView.CreateEntryPointNode();
        nodeView.AddElement(node);
    }

    /// <summary>
    /// 点击-保存数据
    /// </summary>
    public virtual void OnClickForToolBarSave()
    {

    }
    
    /// <summary>
    /// 点击-读取数据
    /// </summary>
    public virtual void OnClickForToolBarLoad()
    { 

    }
}