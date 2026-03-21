using PlayModeChangesSaver.Editor.ChangesTracker;
using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Lightweight compare popup for GameObject name changes.
    ///     Shows original/saved/current names and allows revert/apply actions.
    /// </summary>
    internal class NameOcpContent : PopupWindowContent
    {
        private readonly GameObject go;
        private string currentName;

        private bool hasOriginal;
        private bool hasSaved;

        private string originalName;
        private string savedName;

        public NameOcpContent(GameObject gameObject)
        {
            go = gameObject;
            LoadState();
        }

        private void LoadState()
        {
            currentName = go.name;

            string scenePath = ChangesTrackerCore.GetNormalizedScenePath(go);
            string objectPath = SceneAndPathUtilities.GetGameObjectPath(go.transform);
            string guid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            var nameOriginalStore = NameOriginalStore.LoadExisting();
            var originalEntry = nameOriginalStore?.entries.Find(e =>
                (!string.IsNullOrEmpty(e.globalObjectId) && e.globalObjectId == guid) ||
                (string.IsNullOrEmpty(e.globalObjectId) && e.scenePath == scenePath && e.objectPath == objectPath));

            originalName = originalEntry?.originalName ?? Name_SH.GetNameSnapshot(go)?.objectName;
            hasOriginal = !string.IsNullOrEmpty(originalName);

            var nameStore = NameChangesStore.LoadExisting();
            var savedEntry = nameStore?.changes.Find(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == guid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));

            savedName = savedEntry?.newName;
            hasSaved = !string.IsNullOrEmpty(savedName);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(340f, 170f);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Name Comparison", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawRow("Original", hasOriginal ? originalName : "(unbekannt)");
            DrawRow("Saved", hasSaved ? savedName : "(keine Speicherung)");
            DrawRow("Play Mode", currentName);

            GUILayout.FlexibleSpace();
            DrawButtons();
        }

        private static void DrawRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(80f));
            EditorGUILayout.LabelField(value ?? string.Empty);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!hasOriginal))
            {
                if (GUILayout.Button("Revert to Original"))
                {
                    RevertToOriginal();
                }
            }

            using (new EditorGUI.DisabledScope(!hasSaved))
            {
                if (GUILayout.Button("Revert to Saved"))
                {
                    RevertToSaved();
                }
            }

            if (GUILayout.Button("Apply Current"))
            {
                ApplyCurrent();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RevertToOriginal()
        {
            if (!hasOriginal)
            {
                return;
            }

            Undo.RecordObject(go, "Revert Name to Original");
            go.name = originalName;
            RemoveNameChangeEntry(go);
            Name_SH.SetNameSnapshot(go, new NameSnapshot(go));
            RefreshBrowserIfOpen();
            editorWindow.Close();
        }

        private void RevertToSaved()
        {
            if (!hasSaved)
            {
                return;
            }

            Undo.RecordObject(go, "Revert Name to Saved");
            go.name = savedName;
            Name_SH.SetNameSnapshot(go, new NameSnapshot(go));
            RefreshBrowserIfOpen();
            editorWindow.Close();
        }

        private void ApplyCurrent()
        {
            ChangesTrackerCore.AcceptNameChanges(go);
            RefreshBrowserIfOpen();
            editorWindow.Close();
        }

        private static void RefreshBrowserIfOpen()
        {
            if (EditorWindow.HasOpenInstances<OverridesBrowserWindow>())
            {
                OverridesBrowserWindow.Open();
            }
        }

        private static void RemoveNameChangeEntry(GameObject go)
        {
            var nameStore = NameChangesStore.LoadExisting();
            if (!nameStore)
            {
                return;
            }

            string scenePath = ChangesTrackerCore.GetNormalizedScenePath(go);
            string objectPath = SceneAndPathUtilities.GetGameObjectPath(go.transform);
            string guid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            for (int i = nameStore.changes.Count - 1; i >= 0; i--)
            {
                var c = nameStore.changes[i];
                if ((!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == guid) ||
                    (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath))
                {
                    nameStore.changes.RemoveAt(i);
                }
            }

            EditorUtility.SetDirty(nameStore);
            AssetDatabase.SaveAssets();
        }
    }
}