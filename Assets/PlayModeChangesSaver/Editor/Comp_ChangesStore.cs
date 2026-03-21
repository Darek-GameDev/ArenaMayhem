using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class CompChangesStore : ScriptableObject
    {
        public List<ComponentChange> changes = new();

        public static CompChangesStore LoadExisting()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(CompChangesStore)}");
            if (guids is { Length: > 0 })
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CompChangesStore>(path);
            }

            return null;
        }

        public static CompChangesStore LoadOrCreate()
        {
            var store = LoadExisting();
            if (store == null)
            {
                string assetPath = GetDefaultAssetPath();
                store = CreateInstance<CompChangesStore>();
                AssetDatabase.CreateAsset(store, assetPath);
                AssetDatabase.SaveAssets();
            }

            return store;
        }

        private static string GetDefaultAssetPath()
        {
            string soFolder = PlayModeChangesSaverPaths.GetScriptableObjectsFolder();
            return $"{soFolder}/Comp_Changes_Store.asset";
        }

        public void Clear()
        {
            changes.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class ComponentChange
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string componentType;
            public int componentIndex;

            public List<string> propertyPaths = new();
            public List<string> serializedValues = new();
            public List<string> valueTypes = new();

            public bool includeMaterialChanges;
            public List<string> materialGuids = new();
        }
    }
}