using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeBaseView : GraphView
{
    public NodeBaseView()
    {
        //添加背景网格
        styleSheets.Add(Resources.Load<StyleSheet>("NodeBackground"));

        //设置缩放
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        //设置背景
        var gird = new GridBackground();
        Insert(0,gird);
        gird.StretchToParentSize();

        ClearSelection();
    }

    /// <summary>
    /// 通用端口配置
    /// </summary>
    /// <param name="node"></param>
    /// <param name="protDirection"></param>
    /// <param name="capacity"></param>
    /// <returns></returns>
    private Port GeneratePort(NodeBase node, Direction protDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, protDirection, capacity, typeof(float));
    }

    /// <summary>
    /// 节点端口链接配置 
    /// </summary>
    /// <param name="startPort"></param>
    /// <param name="nodeAdapter"></param>
    /// <returns></returns>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach((port) =>
        {
            //不能链接自己端口
            if (startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }


    /// <summary>
    /// 创建一个节点
    /// </summary>
    /// <returns></returns>
    public virtual NodeBase CreateEntryPointNode()
    {
        var node = new NodeBase
        {
            title = "Start",
            id = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N),
            textContent = "Test",
            entryPoint = true
        };

        //配置节点的端口
        var generatePort = GeneratePort(node, Direction.Output);
        generatePort.portName = "Next";
        node.outputContainer.Add(generatePort);

        //刷新一下
        node.RefreshExpandedState();
        node.RefreshPorts();

        //设置节点位置
        node.SetPosition(new Rect(100, 200, 100, 150));

        return node;
    }
}