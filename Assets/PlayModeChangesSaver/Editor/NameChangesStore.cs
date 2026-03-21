using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class NameChangesStore : ScriptableObject
    {
        public List<NameChange> changes = new();

        public static NameChangesStore LoadExisting()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(NameChangesStore)}");
            if (guids is { Length: > 0 })
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var store = AssetDatabase.LoadAssetAtPath<NameChangesStore>(path);
                return store;
            }

            return null;
        }

        public static NameChangesStore LoadOrCreate()
        {
            var store = LoadExisting();
            if (store == null)
            {
                string assetPath = GetDefaultAssetPath();
                store = CreateInstance<NameChangesStore>();
                AssetDatabase.CreateAsset(store, assetPath);
                AssetDatabase.SaveAssets();
            }

            return store;
        }

        private static string GetDefaultAssetPath()
        {
            string soDir = PlayModeChangesSaverPaths.GetScriptableObjectsFolder();
            return soDir + "/GO_Name_Changes_Store.asset";
        }

        public void Clear()
        {
            changes.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class NameChange
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string newName;
        }
    }
}