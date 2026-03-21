using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class NameOriginalStore : ScriptableObject
    {
        public List<NameOriginal> entries = new();

        public static NameOriginalStore LoadExisting()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(NameOriginalStore)}");
            if (guids is { Length: > 0 })
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<NameOriginalStore>(path);
            }

            return null;
        }

        public static NameOriginalStore LoadOrCreate()
        {
            var store = LoadExisting();
            if (store == null)
            {
                string assetPath = GetDefaultAssetPath();
                store = CreateInstance<NameOriginalStore>();
                AssetDatabase.CreateAsset(store, assetPath);
                AssetDatabase.SaveAssets();
            }

            return store;
        }

        private static string GetDefaultAssetPath()
        {
            string soDir = PlayModeChangesSaverPaths.GetScriptableObjectsFolder();
            return soDir + "/GO_Name_Original_Store.asset";
        }

        public void Clear()
        {
            entries.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class NameOriginal
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string originalName;
        }
    }
}