using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Recording
{
    public static class TransformChangeRecorder
    {
        public static void RecordTransformChangeToStore(GameObject go, Snapshot original, Snapshot current)
        {
            var store = ChangesStore.LoadOrCreate();
            string scenePath = ChangesTrackerCore.GetNormalizedScenePath(go);
            string objectPath = SceneAndPathUtilities.GetGameObjectPath(go.transform);
            string globalObjectId = current.globalObjectId;

            List<string> modifiedProps = original != null
                ? SnapshotManager.GetChangedProperties(original, current)
                : new List<string> { "position", "rotation", "scale" };

            if (original != null)
            {
                var ctx = new TransformStoreContext
                {
                    ScenePath = scenePath,
                    ObjectPath = objectPath,
                    GlobalObjectId = globalObjectId,
                    Original = original
                };
                StoreOriginalTransformData(ctx);
            }

            var changeCtx = new TransformChangeContext
            {
                ScenePath = scenePath,
                ObjectPath = objectPath,
                GlobalObjectId = globalObjectId,
                Current = current,
                ModifiedProperties = modifiedProps
            };
            UpdateTransformStore(store, changeCtx);
        }

        private static void StoreOriginalTransformData(TransformStoreContext ctx)
        {
            var originalStore = OriginalStore.LoadOrCreate();
            bool entryExists = originalStore.entries.Exists(e =>
                IsMatchingTransformOriginal(e, ctx));

            if (!entryExists)
            {
                var entry = BuildTransformOriginalEntry(ctx);
                originalStore.entries.Add(entry);
                EditorUtility.SetDirty(originalStore);
            }
        }

        private static bool IsMatchingTransformOriginal(OriginalStore.TransformOriginal e, TransformStoreContext ctx)
        {
            if (!string.IsNullOrEmpty(e.globalObjectId))
            {
                return e.globalObjectId == ctx.GlobalObjectId;
            }

            return e.scenePath == ctx.ScenePath && e.objectPath == ctx.ObjectPath;
        }

        private static OriginalStore.TransformOriginal BuildTransformOriginalEntry(TransformStoreContext ctx)
        {
            var entry = new OriginalStore.TransformOriginal
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.Original.globalObjectId,
                isRectTransform = ctx.Original.isRectTransform,
                position = ctx.Original.position,
                rotation = ctx.Original.rotation,
                scale = ctx.Original.scale
            };

            SetRectTransformProperties(entry, ctx.Original);
            return entry;
        }

        private static void SetRectTransformProperties(OriginalStore.TransformOriginal entry, Snapshot original)
        {
            if (original.isRectTransform)
            {
                entry.anchoredPosition = original.anchoredPosition;
                entry.anchoredPosition3D = original.anchoredPosition3D;
                entry.anchorMin = original.anchorMin;
                entry.anchorMax = original.anchorMax;
                entry.pivot = original.pivot;
                entry.sizeDelta = original.sizeDelta;
                entry.offsetMin = original.offsetMin;
                entry.offsetMax = original.offsetMax;
            }
            else
            {
                entry.anchoredPosition = Vector2.zero;
                entry.anchoredPosition3D = Vector3.zero;
                entry.anchorMin = Vector2.zero;
                entry.anchorMax = Vector2.one;
                entry.pivot = new Vector2(0.5f, 0.5f);
                entry.sizeDelta = Vector2.zero;
                entry.offsetMin = Vector2.zero;
                entry.offsetMax = Vector2.zero;
            }
        }

        private static void UpdateTransformStore(ChangesStore store, TransformChangeContext ctx)
        {
            var change = new ChangesStore.TransformChange
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.GlobalObjectId,
                isRectTransform = ctx.Current.isRectTransform,
                position = ctx.Current.position,
                rotation = ctx.Current.rotation,
                scale = ctx.Current.scale,
                anchoredPosition = ctx.Current.anchoredPosition,
                anchoredPosition3D = ctx.Current.anchoredPosition3D,
                anchorMin = ctx.Current.anchorMin,
                anchorMax = ctx.Current.anchorMax,
                pivot = ctx.Current.pivot,
                sizeDelta = ctx.Current.sizeDelta,
                offsetMin = ctx.Current.offsetMin,
                offsetMax = ctx.Current.offsetMax,
                modifiedProperties = ctx.ModifiedProperties
            };

            int existingIndex = FindTransformChangeIndex(store, ctx);
            if (ctx.ModifiedProperties.Count > 0)
            {
                if (existingIndex >= 0)
                {
                    store.changes[existingIndex] = change;
                }
                else
                {
                    store.changes.Add(change);
                }
            }
            else if (existingIndex >= 0)
            {
                store.changes.RemoveAt(existingIndex);
            }

            EditorUtility.SetDirty(store);
            AssetDatabase.SaveAssets();
        }

        private static int FindTransformChangeIndex(ChangesStore store, TransformChangeContext ctx)
        {
            return store.changes.FindIndex(c => IsMatchingTransformChange(c, ctx));
        }

        private static bool IsMatchingTransformChange(ChangesStore.TransformChange c, TransformChangeContext ctx)
        {
            if (!string.IsNullOrEmpty(c.globalObjectId))
            {
                return c.globalObjectId == ctx.GlobalObjectId;
            }

            return c.scenePath == ctx.ScenePath && c.objectPath == ctx.ObjectPath;
        }
    }
}