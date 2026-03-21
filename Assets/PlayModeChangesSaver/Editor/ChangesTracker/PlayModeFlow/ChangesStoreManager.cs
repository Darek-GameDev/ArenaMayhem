using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow
{
    public static class ChangesStoreManager
    {
        public static void RemoveChangesForSceneFromStore(string targetScenePath, ChangesStore tStore,
            CompChangesStore cStore, NameChangesStore nameStore)
        {
            string normalizedScenePath = SceneAndPathUtilities.NormalizeScenePath(targetScenePath);

            bool transformChanged = RemoveTransformChanges(normalizedScenePath, tStore);
            bool componentChanged = RemoveComponentChanges(normalizedScenePath, cStore);
            bool nameChanged = RemoveNameChanges(normalizedScenePath, nameStore);

            MarkDirtyIfChanged(transformChanged, tStore);
            MarkDirtyIfChanged(componentChanged, cStore);
            MarkDirtyIfChanged(nameChanged, nameStore);
            AssetDatabase.SaveAssets();
        }

        private static bool RemoveTransformChanges(string scenePath, ChangesStore store)
        {
            if (!store || store.changes.Count == 0)
            {
                return false;
            }

            bool changed = false;
            for (int i = store.changes.Count - 1; i >= 0; i--)
            {
                var change = store.changes[i];
                if (!IsSameScene(change.scenePath, scenePath))
                {
                    continue;
                }

                store.changes.RemoveAt(i);
                changed = true;
            }

            return changed;
        }

        private static bool RemoveComponentChanges(string scenePath, CompChangesStore store)
        {
            if (!store || store.changes.Count == 0)
            {
                return false;
            }

            bool changed = false;
            for (int i = store.changes.Count - 1; i >= 0; i--)
            {
                var change = store.changes[i];
                if (!IsSameScene(change.scenePath, scenePath))
                {
                    continue;
                }

                store.changes.RemoveAt(i);
                changed = true;
            }

            return changed;
        }

        private static bool RemoveNameChanges(string scenePath, NameChangesStore store)
        {
            if (!store || store.changes.Count == 0)
            {
                return false;
            }

            bool changed = false;
            for (int i = store.changes.Count - 1; i >= 0; i--)
            {
                var change = store.changes[i];
                if (!IsSameScene(change.scenePath, scenePath))
                {
                    continue;
                }

                store.changes.RemoveAt(i);
                changed = true;
            }

            return changed;
        }

        private static void MarkDirtyIfChanged(bool changed, Object store)
        {
            if (changed && store != null)
            {
                EditorUtility.SetDirty(store);
            }
        }

        private static bool IsSameScene(string changeScenePath, string targetScenePath)
        {
            return string.Equals(SceneAndPathUtilities.NormalizeScenePath(changeScenePath), targetScenePath,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}