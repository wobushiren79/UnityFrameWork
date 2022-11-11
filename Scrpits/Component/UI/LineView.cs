using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LineView : MaskableGraphic
{
    [Header("线粗细")]
    public float lineThickness;
    [Header("线上点 颜色")]
    public List<Color> listPositionColor = new List<Color>();
    [Header("线上点 位置")]
    public List<Vector3> listPosition = new List<Vector3>();
    [Header("线上点 渲染方向 0：自动 1：左右 2：上下 ")]
    public int linePositionDirection = 0;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        UIVertex vertex = UIVertex.simpleVert;

        for (int i = 0; i < listPosition.Count; i++)
        {
            //设置顶点颜色
            if (listPositionColor.IsNull())
                vertex.color = Color.white;
            else if (listPositionColor.Count > i)
                vertex.color = listPositionColor[i];
            else
                vertex.color = listPositionColor[listPositionColor.Count - 1];

            Vector3 startPosition = listPosition[i];
            if (linePositionDirection == 1)
            {
                //左右顶点渲染
                vertex.position = startPosition + new Vector3(-lineThickness / 2f, 0);
                vh.AddVert(vertex);
                vertex.position = startPosition + new Vector3(lineThickness / 2f, 0);
                vh.AddVert(vertex);
            }
            else if (linePositionDirection == 2)
            {
                //上线顶点渲染
                vertex.position = startPosition + new Vector3(0, -lineThickness / 2f);
                vh.AddVert(vertex);
                vertex.position = startPosition + new Vector3(0, lineThickness / 2f);
                vh.AddVert(vertex);
            }
            else
            {
                Vector3 endPosition;
                if (i + 1 >= listPosition.Count)
                {
                    endPosition = listPosition[i - 1];
                }
                else
                {
                    endPosition = listPosition[i + 1];
                }
                Vector3 positionNormal = (endPosition - startPosition).normalized;
                Vector3 offsetPosition = VectorUtil.GetVerticalDir2D(positionNormal) * lineThickness / 2f;
                vertex.position = startPosition - offsetPosition;
                vh.AddVert(vertex);
                vertex.position = startPosition + offsetPosition;
                vh.AddVert(vertex);
            }
        }
        if (linePositionDirection == 0)
        {
            for (int i = 0; i < listPosition.Count - 1; i++)
            {
                int index = i * 2;
                vh.AddTriangle(index + 0, index + 1, index + 3);
                vh.AddTriangle(index + 3, index + 2, index + 1);
            }
        }
        else
        {
            for (int i = 0; i < listPosition.Count - 1; i++)
            {
                int index = i * 2;
                vh.AddTriangle(index + 0, index + 1, index + 3);
                vh.AddTriangle(index + 3, index + 2, index + 0);
            }
        }

    }
}