using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Recording
{
    public static class NameChangeRecorder
    {
        public static void RecordNameChangeToStore(GameObject go)
        {
            var original = Name_SH.GetNameSnapshot(go);
            if (original==null)
            {
                return;
            }

            var nameStore = NameChangesStore.LoadOrCreate();
            var nameOriginalStore = NameOriginalStore.LoadOrCreate();

            var nameContext = new NameChangeContext
            {
                ScenePath = ChangesTrackerCore.GetNormalizedScenePath(go),
                ObjectPath = SceneAndPathUtilities.GetGameObjectPath(go.transform),
                GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString(),
                OriginalName = original.objectName,
                NewName = go.name
            };

            StoreOriginalNameIfNeeded(nameOriginalStore, nameContext);
            StoreNameChange(nameStore, nameContext);
            Name_SH.SetNameSnapshot(go, new NameSnapshot(go));
        }

        private static void StoreOriginalNameIfNeeded(NameOriginalStore store, NameChangeContext ctx)
        {
            if (NameOriginalAlreadyExists(store, ctx))
            {
                return;
            }

            var entry = new NameOriginalStore.NameOriginal
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.GlobalObjectId,
                originalName = ctx.OriginalName
            };
            store.entries.Add(entry);
            EditorUtility.SetDirty(store);
        }

        private static bool NameOriginalAlreadyExists(NameOriginalStore store, NameChangeContext ctx)
        {
            return store.entries.Exists(e => IsMatchingNameOriginal(e, ctx));
        }

        private static bool IsMatchingNameOriginal(NameOriginalStore.NameOriginal e, NameChangeContext ctx)
        {
            if (!string.IsNullOrEmpty(e.globalObjectId))
            {
                return e.globalObjectId == ctx.OriginalName;
            }

            return e.scenePath == ctx.ScenePath && e.objectPath == ctx.ObjectPath;
        }

        private static void StoreNameChange(NameChangesStore store, NameChangeContext ctx)
        {
            var change = new NameChangesStore.NameChange
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.GlobalObjectId,
                newName = ctx.NewName
            };

            int existingIndex = store.changes.FindIndex(c => IsMatchingNameChange(c, ctx));
            if (existingIndex >= 0)
            {
                store.changes[existingIndex] = change;
            }
            else
            {
                store.changes.Add(change);
            }

            EditorUtility.SetDirty(store);
            AssetDatabase.SaveAssets();
        }

        private static bool IsMatchingNameChange(NameChangesStore.NameChange c, NameChangeContext ctx)
        {
            if (!string.IsNullOrEmpty(c.globalObjectId))
            {
                return c.globalObjectId == ctx.GlobalObjectId;
            }

            return c.scenePath == ctx.ScenePath && c.objectPath == ctx.ObjectPath;
        }
    }
}