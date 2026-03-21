using PlayModeChangesSaver.Editor.ChangesTracker;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    [InitializeOnLoad]
    public static class ChangesInspector
    {
        static ChangesInspector()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        private static void OnPostHeaderGUI(UnityEditor.Editor editor)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!TryGetTargetGameObject(editor, out var go))
            {
                return;
            }

            var changedComponents = ChangesTrackerCore.GetChangedComponents(go);
            bool hasNameDelta = ChangesTrackerCore.HasNameDelta(go);
            bool hasChanges = changedComponents.Count > 0 || hasNameDelta;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            using (new EditorGUI.DisabledScope(!hasChanges))
            {
                GUIContent buttonContent = new("Play Mode Overrides");
                Rect buttonRect =
                    GUILayoutUtility.GetRect(buttonContent, EditorStyles.miniButton, GUILayout.Width(140f));
                if (GUI.Button(buttonRect, buttonContent, EditorStyles.miniButton))
                {
                    PopupWindow.Show(buttonRect, new OverridesPopUp(go));
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        private static bool TryGetTargetGameObject(UnityEditor.Editor editor, out GameObject go)
        {
            go = null;
            if (!editor)
            {
                return false;
            }

            if (!editor.target)
            {
                return false;
            }

            go = editor.target as GameObject;
            if (go != null)
            {
                return true;
            }

            var comp = editor.target as Component;
            if (!comp)
            {
                return false;
            }

            go = comp.gameObject;
            return go != null;
        }
    }
}