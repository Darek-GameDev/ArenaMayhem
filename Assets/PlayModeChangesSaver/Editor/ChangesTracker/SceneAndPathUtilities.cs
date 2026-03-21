using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    public static class SceneAndPathUtilities
    {
        public static string NormalizeScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').Trim();
        }

        public static Scene GetSceneByPathOrName(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                scene = SceneManager.GetSceneByName(scenePath);
            }

            return scene;
        }

        /// <summary>
        ///     Hybrid lookup: attempts GUID-based lookup first, falls back to path-based lookup.
        ///     Logs warning if fallback is used.
        /// </summary>
        public static GameObject FindGameObjectByGuidOrPath(Scene scene, string globalObjectIdStr, string objectPath)
        {
            // Attempt GUID lookup first (primary method)
            if (!string.IsNullOrEmpty(globalObjectIdStr))
            {
                GameObject guidResult = FindGameObjectByGuid(globalObjectIdStr);
                if (guidResult != null)
                {
                    return guidResult;
                }
            }

            // Fallback to path-based lookup
            GameObject pathResult = FindInSceneByPath(scene, objectPath);
            if (pathResult != null && !string.IsNullOrEmpty(globalObjectIdStr))
            {
            }

            return pathResult;
        }

        /// <summary>
        ///     Attempts to find a GameObject by its GlobalObjectId string.
        ///     Returns null if GUID is invalid or object not found.
        /// </summary>
        public static GameObject FindGameObjectByGuid(string globalObjectIdStr)
        {
            if (string.IsNullOrEmpty(globalObjectIdStr))
            {
                return null;
            }

            if (!GlobalObjectId.TryParse(globalObjectIdStr, out GlobalObjectId globalObjectId))
            {
                return null;
            }

            Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            if (obj is GameObject go)
            {
                return go;
            }

            if (obj is Component comp)
            {
                return comp.gameObject;
            }

            return null;
        }

        public static GameObject FindInSceneByPath(Scene scene, string path)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            GameObject current = FindRootGameObjectByName(scene, parts[0]);
            if (!current)
            {
                return null;
            }

            current = TraverseChildPath(current, parts, 1);
            return current;
        }

        private static GameObject FindRootGameObjectByName(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject TraverseChildPath(GameObject root, string[] parts, int startIdx)
        {
            GameObject current = root;
            for (int i = startIdx; i < parts.Length; i++)
            {
                var childName = parts[i];
                Transform child = null;
                foreach (Transform t in current.transform)
                {
                    if (t.name == childName)
                    {
                        child = t;
                        break;
                    }
                }

                if (!child)
                {
                    return null;
                }

                current = child.gameObject;
            }

            return current;
        }

        public static string GetGameObjectPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        public static string GetGameObjectKey(GameObject go)
        {
            if (!go)
            {
                return "";
            }

            string scenePath = NormalizeScenePath(go.scene.path);
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = go.scene.name;
            }

            string goPath = GetGameObjectPath(go.transform);
            return $"{scenePath}|{goPath}";
        }

        public static string GetComponentKey(Component comp)
        {
            var allComps = comp.gameObject.GetComponents(comp.GetType());
            int index = Array.IndexOf(allComps, comp);
            return $"{comp.GetType().Name}_{index}";
        }
    }
}