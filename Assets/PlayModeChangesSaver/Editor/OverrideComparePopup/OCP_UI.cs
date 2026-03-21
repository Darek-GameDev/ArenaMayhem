using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Handles UI rendering and layout for the compare popup.
    /// </summary>
    internal static class OcpUI
    {
        /// <summary>
        ///     Draws the column header with component icon and name.
        /// </summary>
        public static void DrawColumnHeader(Rect rect, Object target, string title)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, false, false, false, false);
            }

            var content = EditorGUIUtility.ObjectContent(target, target.GetType());

            Rect iconRect = new(rect.x + 4, rect.y + 4, 16, 16);
            if (content.image != null)
            {
                GUI.DrawTexture(iconRect, content.image);
            }

            Rect labelRect = new(rect.x + 24, rect.y + 4, rect.width - 28, 16);
            EditorGUI.LabelField(labelRect, $"{content.text} ({title})", EditorStyles.boldLabel);
        }

        /// <summary>
        ///     Draws a vertical separator line between columns.
        /// </summary>
        public static void DrawSeparator(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color separatorColor = EditorGUIUtility.isProSkin
                    ? new Color(0.15f, 0.15f, 0.15f)
                    : new Color(0.6f, 0.6f, 0.6f);
                EditorGUI.DrawRect(rect, separatorColor);
            }
        }

        /// <summary>
        ///     Draws a Transform inspector manually with Position, Rotation, Scale fields.
        /// </summary>
        public static void DrawTransformInspector(Transform t, bool editable)
        {
            if (!t)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(!editable);

            EditorGUI.BeginChangeCheck();
            Vector3 pos = EditorGUILayout.Vector3Field("Position", t.localPosition);
            Vector3 rot = EditorGUILayout.Vector3Field("Rotation", t.localEulerAngles);
            Vector3 scale = EditorGUILayout.Vector3Field("Scale", t.localScale);

            if (EditorGUI.EndChangeCheck() && editable)
            {
                Undo.RecordObject(t, "Edit Transform");
                t.localPosition = pos;
                t.localEulerAngles = rot;
                t.localScale = scale;
                EditorUtility.SetDirty(t);
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        ///     Draws a synchronized scrollable column with inspector content.
        /// </summary>
        public static void DrawSynchronizedColumn(UnityEditor.Editor editor, ref ColumnRenderContext context, bool editable)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(context.Width), GUILayout.Height(context.Height));

            float currentOffset = context.ScrollValue * context.MaxScroll;
            Vector2 scrollPos = new(0, currentOffset);

            GUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUIStyle.none, GUILayout.Height(context.Height));

            bool prevEnabled = GUI.enabled;
            GUI.enabled = editable;

            if (editor.target is Transform t)
            {
                DrawTransformInspector(t, editable);
            }
            else
            {
                editor.OnInspectorGUI();
            }

            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                context.MaxScroll = Mathf.Max(0, lastRect.yMax - context.Height);
            }

            GUI.enabled = prevEnabled;
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws a footer with info and action buttons.
        /// </summary>
        public static void DrawFooter(Rect rect, bool hasUnsavedChanges)
        {
            if (hasUnsavedChanges)
            {
                EditorGUILayout.HelpBox(
                    "Current Play Mode changes differ from the stored overrides and have not been applied yet.",
                    MessageType.Info);
            }
        }

        /// <summary>
        ///     Encapsulates parameters for synchronized column rendering.
        /// </summary>
        internal struct ColumnRenderContext
        {
            public float Width { get; set; }
            public float Height { get; set; }
            public float ScrollValue { get; set; }
            public float MaxScroll { get; set; }

            public ColumnRenderContext(float width, float height, float scrollValue, float maxScroll)
            {
                Width = width;
                Height = height;
                ScrollValue = scrollValue;
                MaxScroll = maxScroll;
            }
        }
    }
}