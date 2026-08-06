using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>像素图编辑器 · 步骤① 设置：选择网格尺寸与源图片。</summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
        #region UI - 步骤① 设置

        /// <summary>步骤①：选择网格尺寸与源图片。</summary>
        private void DrawStep1()
        {
            BeginCard("① 网格尺寸");
            _gridWidth = GridOptionPopup("网格宽（列）", _gridWidth);
            _gridHeight = GridOptionPopup("网格高（行）", _gridHeight);
            EditorGUILayout.LabelField($"输出像素图：{_gridWidth} × {_gridHeight}", _hintStyle);
            EndCard();

            BeginCard("② 源图片");
            DrawSourceDropArea();
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField("图片", _sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && _sourceTexture != null)
                _sourceExternalPath = "";

            if (_srcDisplayTex != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect thumb = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                    DrawChecker(thumb);
                    GUI.DrawTexture(thumb, _srcDisplayTex, ScaleMode.ScaleToFit, true);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("已载入尺寸", $"{_srcW} × {_srcH}");
                        EditorGUILayout.LabelField("（超过 1024 已等比缩小）", _hintStyle);
                    }
                }
            }
            EndCard();

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_sourceTexture == null))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button("载入并进入步骤②", GUILayout.Height(34)))
                {
                    try
                    {
                        LoadSource();
                        EnterStep2();
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog("载入失败", ex.Message, "确定");
                        Debug.LogException(ex);
                    }
                }
                GUI.backgroundColor = prev;
            }
            if (_sourceTexture == null)
                EditorGUILayout.LabelField("请先选择一张源图片。", _hintStyle);
        }

        /// <summary>网格档位下拉。</summary>
        private int GridOptionPopup(string label, int value)
        {
            int idx = Array.IndexOf(kGridOptions, value);
            if (idx < 0) idx = 0;
            string[] labels = new string[kGridOptions.Length];
            for (int i = 0; i < kGridOptions.Length; i++) labels[i] = kGridOptions[i].ToString();
            idx = EditorGUILayout.Popup(label, idx, labels);
            return kGridOptions[idx];
        }

        #endregion
    }
}
