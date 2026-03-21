using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.App;
using PlayModeChangesSaver.Editor.ChangesTracker.Recording;
using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    /// <summary>
    ///     Main entry point for tracking play mode changes.
    ///     Coordinates snapshot management, serialization, and apply/discard workflow.
    ///     Legacy facade during refactor: lifecycle and runtime apply orchestration moved to Lifecycle/.
    /// </summary>
    public static class ChangesTrackerCore
    {
        private static readonly Dictionary<string, HashSet<string>> SelectedProperties = new();

        public static void AcceptTransformChanges(GameObject go)
        {
            if (!go)
            {
                return;
            }

            Snapshot original = SnapshotManager.GetSnapshot(go);
            Snapshot current = new(go);

            SnapshotManager.SetSnapshot(go, current);
            TransformChangeRecorder.RecordTransformChangeToStore(go, original, current);
        }

        public static void AcceptNameChanges(GameObject go)
        {
            if (!go)
            {
                return;
            }

            NameChangeRecorder.RecordNameChangeToStore(go);
        }

        public static void AcceptComponentChanges(Component comp)
        {
            if (!comp)
            {
                return;
            }

            GameObject go = comp.gameObject;
            string compKey = SceneAndPathUtilities.GetComponentKey(comp);

            CompSnapshot originalSnapshot = Component_SH.GetComponentSnapshot(go, compKey);
            Component_SH.ResetComponentBaseline(comp);
            ComponentChangeRecorder.RecordComponentChangeToStore(comp, originalSnapshot);
        }

        public static List<Component> GetChangedComponents(GameObject go)
        {
            return Component_SH.GetChangedComponents(go);
        }

        public static Snapshot GetSnapshot(GameObject go)
        {
            return SnapshotManager.GetSnapshot(go);
        }

        public static string GetComponentKey(Component comp)
        {
            return SceneAndPathUtilities.GetComponentKey(comp);
        }

        public static CompSnapshot GetComponentSnapshot(GameObject go, string componentKey)
        {
            return Component_SH.GetComponentSnapshot(go, componentKey);
        }

        public static void ResetTransformBaseline(GameObject go)
        {
            Transform_SH.ResetTransformBaseline(go);
        }

        public static void ResetComponentBaseline(Component comp)
        {
            Component_SH.ResetComponentBaseline(comp);
        }

        public static bool HasNameDelta(GameObject go)
        {
            if (!go)
            {
                return false;
            }

            var original = Name_SH.GetNameSnapshot(go);
            if (original==null)
            {
                return false;
            }

            return !string.Equals(original.objectName, go.name, StringComparison.Ordinal);
        }

        public static bool HasMaterialDelta(Renderer renderer)
        {
            if (!renderer)
            {
                return false;
            }

            var go = renderer.gameObject;
            string compKey = GetComponentKey(renderer);
            var snapshot = Component_SH.GetComponentSnapshot(go, compKey);
            if (snapshot==null)
            {
                return false;
            }

            var current = MaterialChangeHandler.GetRendererMaterialGuids(renderer);
            var original = snapshot.materialGuids ?? new List<string>();

            if (current.Count != original.Count)
            {
                return true;
            }

            for (int i = 0; i < current.Count; i++)
            {
                if (!string.Equals(current[i], original[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static void RevertAll(GameObject go)
        {
            string key = SceneAndPathUtilities.GetGameObjectKey(go);
            if (SelectedProperties.TryGetValue(key, out var property))
            {
                property.Clear();
            }
        }

        public static void ApplyAll(GameObject go)
        {
            Snapshot original = SnapshotManager.GetSnapshot(go);
            if (original==null)
            {
                return;
            }

            Snapshot current = new(go);
            string key = SceneAndPathUtilities.GetGameObjectKey(go);

            if (!SelectedProperties.ContainsKey(key))
            {
                SelectedProperties[key] = new HashSet<string>();
            }

            SelectedProperties[key].Clear();

            foreach (var change in SnapshotManager.GetChangedProperties(original, current))
            {
                SelectedProperties[key].Add(change);
            }
        }

        public static void ToggleProperty(GameObject go, string property)
        {
            string key = SceneAndPathUtilities.GetGameObjectKey(go);
            if (!SelectedProperties.ContainsKey(key))
            {
                SelectedProperties[key] = new HashSet<string>();
            }

            if (SelectedProperties[key].Contains(property))
            {
                SelectedProperties[key].Remove(property);
            }
            else
            {
                SelectedProperties[key].Add(property);
            }
        }

        public static bool IsPropertySelected(GameObject go, string property)
        {
            string key = SceneAndPathUtilities.GetGameObjectKey(go);
            return SelectedProperties.ContainsKey(key) &&
                   SelectedProperties[key].Contains(property);
        }

        // ===== PRIVATE HELPERS =====

        internal static string GetNormalizedScenePath(GameObject go)
        {
            string path = SceneAndPathUtilities.NormalizeScenePath(go.scene.path);
            return string.IsNullOrEmpty(path) ? go.scene.name : path;
        }

        internal static void ClearStoresOnPlayEnter()
        {
            var transformStore = ChangesStore.LoadExisting();
            if (transformStore != null)
            {
                transformStore.Clear();
            }

            var componentStore = CompChangesStore.LoadExisting();
            if (componentStore != null)
            {
                componentStore.Clear();
            }

            var transformOriginalStore = OriginalStore.LoadExisting();
            if (transformOriginalStore != null)
            {
                transformOriginalStore.Clear();
            }

            var componentOriginalStore = CompOriginalStore.LoadExisting();
            if (componentOriginalStore != null)
            {
                componentOriginalStore.Clear();
            }

            var nameStore = NameChangesStore.LoadExisting();
            if (nameStore != null)
            {
                nameStore.Clear();
            }

            var nameOriginalStore = NameOriginalStore.LoadExisting();
            if (nameOriginalStore != null)
            {
                nameOriginalStore.Clear();
            }
        }

        internal static void ApplyTransformChange(GameObject go, ChangesStore.TransformChange change)
        {
            TransformChangeApplicator.ApplyTransformChange(go, change);
        }
    }
}