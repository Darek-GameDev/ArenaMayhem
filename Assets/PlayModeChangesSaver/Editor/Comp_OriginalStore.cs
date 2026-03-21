using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class CompOriginalStore : ScriptableObject
    {
        public List<ComponentOriginal> entries = new();

        public static CompOriginalStore LoadExisting()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(CompOriginalStore)}");
            if (guids is { Length: > 0 })
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CompOriginalStore>(path);
            }

            return null;
        }

        public static CompOriginalStore LoadOrCreate()
        {
            var store = LoadExisting();
            if (store == null)
            {
                string assetPath = GetDefaultAssetPath();
                store = CreateInstance<CompOriginalStore>();
                AssetDatabase.CreateAsset(store, assetPath);
                AssetDatabase.SaveAssets();
            }

            return store;
        }

        private static string GetDefaultAssetPath()
        {
            string soFolder = PlayModeChangesSaverPaths.GetScriptableObjectsFolder();
            return $"{soFolder}/Comp_OriginalStore.asset";
        }

        public void Clear()
        {
            entries.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class ComponentOriginal
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string componentType;
            public int componentIndex;

            public List<string> propertyPaths = new();
            public List<string> serializedValues = new();
            public List<string> valueTypes = new();
            public List<string> materialGuids = new();
        }
    }
}