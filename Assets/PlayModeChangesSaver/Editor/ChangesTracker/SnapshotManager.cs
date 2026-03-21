using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    public static class SnapshotManager
    {
        public static readonly Dictionary<string, Snapshot> Snapshots = new();
        public static readonly Dictionary<string, Dictionary<string, CompSnapshot>> ComponentSnapshots = new();
        public static readonly Dictionary<string, NameSnapshot> NameSnapshots = new();

        public static void CaptureSnapshots()
        {
            Snapshots.Clear();
            ComponentSnapshots.Clear();
            NameSnapshots.Clear();

            int sceneCount = SceneManager.sceneCount;
            if (sceneCount == 0)
            {
                return;
            }

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject rootGo in roots)
                {
                    CaptureGameObjectRecursive(rootGo);
                }
            }
        }

        public static Snapshot GetSnapshot(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            string key = GetGoKey(go);
            bool found = Snapshots.TryGetValue(key, out var snap);
            return found ? snap : null;
        }

        public static void SetSnapshot(GameObject go, Snapshot snapshot)
        {
            if (snapshot== null)
            {
                return;
            }

            string key = GetGoKey(go);
            Snapshots[key] = snapshot;
        }

        private static int CaptureGameObjectRecursive(GameObject go)
        {
            int count = 1;

            string key = GetGoKey(go);
            Snapshots[key] = new Snapshot(go);
            NameSnapshots[key] = new NameSnapshot(go);

            var compDict = new Dictionary<string, CompSnapshot>();
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is null or Transform)
                {
                    continue;
                }

                string compKey = SceneAndPathUtilities.GetComponentKey(comp);
                CompSnapshot compSnapshot = Component_SH.CaptureComponentSnapshot(comp);
                compDict[compKey] = compSnapshot;
            }

            ComponentSnapshots[key] = compDict;

            foreach (Transform child in go.transform)
            {
                count += CaptureGameObjectRecursive(child.gameObject);
            }

            return count;
        }


        public static List<string> GetChangedProperties(Snapshot original, Snapshot current)
        {
            List<string> changed = new();

            if (original.position != current.position)
            {
                changed.Add("position");
            }

            if (original.rotation != current.rotation)
            {
                changed.Add("rotation");
            }

            if (original.scale != current.scale)
            {
                changed.Add("scale");
            }

            if (original.isRectTransform)
            {
                Transform_SH.CheckRectTransformChanges(original, current, changed);
            }

            return changed;
        }


        public static string GetGoKey(GameObject go)
        {
            if (!go)
            {
                return string.Empty;
            }

            string guid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            return SceneAndPathUtilities.GetGameObjectKey(go);
        }
    }
}