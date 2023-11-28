using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public static class ToolbarExtension
    {
        static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");

        static ScriptableObject m_currentToolbar;

        public static Action<VisualElement> ToolbarZoneLeftAlign;
        public static Action<VisualElement> ToolbarZoneRightAlign;

        static ToolbarExtension()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            // Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
            if (m_currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (m_currentToolbar != null)
                {
                    var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(m_currentToolbar);
                    var mRoot = rawRoot as VisualElement;
                    RegisterVisualElementCallback("ToolbarZoneLeftAlign", ToolbarZoneLeftAlign);
                    RegisterVisualElementCallback("ToolbarZoneRightAlign", ToolbarZoneRightAlign);

                    void RegisterVisualElementCallback(string root, Action<VisualElement> cb)
                    {
                        var toolbarZone = mRoot.Q(root);

                        var parent = new VisualElement()
                        {
                            style = {
                                flexGrow = 1,
                                marginLeft = 2,
                                flexDirection = FlexDirection.Row,
                            }
                        };
                        cb?.Invoke(parent);
                        toolbarZone.Add(parent);
                    }
                }
            }
        }
    }
}
