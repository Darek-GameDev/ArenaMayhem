using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Utility functions for OverrideComparePopup.
    /// </summary>
    internal static class OcpUtilities
    {
        /// <summary>
        ///     Gets the full hierarchy path of a GameObject.
        /// </summary>
        public static string GetGameObjectPath(Transform transform)
        {
            if (!transform)
            {
                return string.Empty;
            }

            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        /// <summary>
        ///     Estimates the height needed for the Inspector based on property count.
        /// </summary>
        public static float EstimateInspectorHeight(UnityEditor.Editor editor)
        {
            if (!editor || !editor.target)
            {
                return 300f;
            }

            SerializedObject so = new(editor.target);
            SerializedProperty prop = so.GetIterator();

            int lineCount = 0;
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script")
                {
                    continue;
                }

                lineCount++;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight + 4f;
            float estimated = lineCount * lineHeight + 24f + 20f; // HeaderHeight = 24f
            return Mathf.Clamp(estimated, 200f, 800f);
        }

        /// <summary>
        ///     Checks if two GameObjects represent the same location in the scene hierarchy.
        /// </summary>
        public static bool IsSameSceneObject(GameObject a, GameObject b)
        {
            if (!a || !b)
            {
                return false;
            }

            return a.scene.path == b.scene.path && GetGameObjectPath(a.transform) == GetGameObjectPath(b.transform);
        }
    }
}