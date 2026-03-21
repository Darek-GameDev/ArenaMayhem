using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Handles snapshot creation and editor initialization for comparison.
    /// </summary>
    internal class OcpSnapshot
    {
        private readonly Component liveComponent;

        public OcpSnapshot(Component component)
        {
            liveComponent = component;
            CreateSnapshot();
        }

        public GameObject SnapshotGo { get; private set; }
        public Component SnapshotComponent { get; private set; }

        private void CreateSnapshot()
        {
            var go = liveComponent.gameObject;
            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            SnapshotGo = new GameObject("SnapshotTransform")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            if (liveComponent is Transform)
            {
                CreateTransformSnapshot(go, goid);
            }
            else
            {
                CreateComponentSnapshot(go, goid);
            }
        }

        private void CreateTransformSnapshot(GameObject go, string goid)
        {
            string scenePath = GetScenePath(go);
            string objectPath = OcpUtilities.GetGameObjectPath(go.transform);

            var originalMatch = FindTransformOriginal(scenePath, objectPath, goid);
            if (originalMatch != null)
            {
                CreateTransformSnapshotFromOriginal(originalMatch);
                return;
            }

            var changeMatch = FindTransformChange(scenePath, objectPath, goid);
            if (changeMatch != null)
            {
                CreateTransformSnapshotFromChange(changeMatch);
                return;
            }

            if (Application.isPlaying)
            {
                CreateTransformSnapshotFromLiveSnapshot(go);
            }
        }

        private static string GetScenePath(GameObject go)
        {
            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = go.scene.name;
            }

            return scenePath;
        }

        private static OriginalStore.TransformOriginal FindTransformOriginal(string scenePath, string objectPath, string goid)
        {
            var originalStore = OriginalStore.LoadExisting();
            return originalStore?.entries.Find(e =>
                (!string.IsNullOrEmpty(e.globalObjectId) && e.globalObjectId == goid) ||
                (string.IsNullOrEmpty(e.globalObjectId) && e.scenePath == scenePath && e.objectPath == objectPath));
        }

        private static ChangesStore.TransformChange FindTransformChange(string scenePath, string objectPath, string goid)
        {
            var changeStore = ChangesStore.LoadExisting();
            return changeStore?.changes.Find(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));
        }

        private void CreateTransformSnapshotFromOriginal(OriginalStore.TransformOriginal original)
        {
            CreateTransformSnapshotFromData(new TransformOriginalAdapter(original));
        }

        private void CreateTransformSnapshotFromChange(ChangesStore.TransformChange change)
        {
            CreateTransformSnapshotFromData(new TransformChangeAdapter(change));
        }

        private void CreateTransformSnapshotFromLiveSnapshot(GameObject go)
        {
            var originalSnapshot = ChangesTrackerCore.GetSnapshot(go);
            if (originalSnapshot==null)
            {
                return;
            }

            CreateTransformSnapshotFromData(new TransformSnapshotAdapter(originalSnapshot));
        }

        private void CreateTransformSnapshotFromData(ITransformData data)
        {
            SetupTransformComponent(data.IsRectTransform);
            ApplyTransformData(data.Position, data.Rotation, data.Scale);

            if (SnapshotComponent is RectTransform snapshotRT)
            {
                var rtData = new RectTransformData(data.AnchoredPosition, data.AnchoredPosition3D,
                    data.AnchorMin, data.AnchorMax, data.Pivot, data.SizeDelta,
                    data.OffsetMin, data.OffsetMax);
                rtData.ApplyTo(snapshotRT);
            }

            new SerializedObject(SnapshotComponent).Update();
        }

        private void SetupTransformComponent(bool isRectTransform)
        {
            if (isRectTransform && liveComponent is RectTransform)
            {
                SnapshotComponent = SnapshotGo.AddComponent<RectTransform>();
            }
            else
            {
                SnapshotComponent = SnapshotGo.transform;
            }
        }

        private void ApplyTransformData(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SnapshotComponent.transform.SetLocalPositionAndRotation(position, rotation);
            SnapshotComponent.transform.localScale = scale;
        }

        private void CreateComponentSnapshot(GameObject go, string goid)
        {
            var type = liveComponent.GetType();
            SnapshotComponent = SnapshotGo.AddComponent(type);

            bool appliedFromStore = TryApplyComponentSnapshotFromStores(go, type, goid);
            if (!appliedFromStore)
            {
                CreateComponentSnapshotFromLiveSnapshot(go);
            }
        }

        private void CreateComponentSnapshotFromLiveSnapshot(GameObject go)
        {
            string compKey = ChangesTrackerCore.GetComponentKey(liveComponent);
            var snapshot = ChangesTrackerCore.GetComponentSnapshot(go, compKey);

            if (snapshot != null)
            {
                SerializedObject so = new(SnapshotComponent);

                foreach (var kvp in snapshot.Properties)
                {
                    SerializedProperty prop = so.FindProperty(kvp.Key);
                    if (prop != null)
                    {
                        try
                        {
                            OcpSerialization.SetPropertyValue(prop, kvp.Value);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                if (SnapshotComponent is Renderer renderer && snapshot.materialGuids is { Count: > 0 })
                {
                    ApplyMaterials(renderer, snapshot.materialGuids);
                }
            }
        }

        private bool TryApplyComponentSnapshotFromStores(GameObject go, Type type, string goid)
        {
            var (scenePath, objectPath, componentType, index) = GetComponentIdentifiers(go, type);

            if (TryApplyFromOriginalStore(scenePath, objectPath, componentType, index, goid))
            {
                return true;
            }

            return TryApplyFromChangeStore(scenePath, objectPath, componentType, index, goid);
        }

        private (string scenePath, string objectPath, string componentType, int index) GetComponentIdentifiers(
            GameObject go, Type type)
        {
            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = go.scene.name;
            }

            string objectPath = OcpUtilities.GetGameObjectPath(go.transform);
            string componentType = type.AssemblyQualifiedName;
            var allOfType = go.GetComponents(type);
            int index = Array.IndexOf(allOfType, liveComponent);

            return (scenePath, objectPath, componentType, index);
        }

        private bool TryApplyFromOriginalStore(string scenePath, string objectPath, string componentType, int index, string goid)
        {
            var store = CompOriginalStore.LoadExisting();
            var match = store?.entries.Find(e =>
                MatchesComponentIdentity(e, new ComponentIdentity(scenePath, objectPath, componentType, index, goid)));

            if (match != null)
            {
                ApplyComponentOriginalToSnapshot(match);
                return true;
            }

            return false;
        }

        private bool TryApplyFromChangeStore(string scenePath, string objectPath, string componentType, int index, string goid)
        {
            var store = CompChangesStore.LoadExisting();
            var match = store?.changes.Find(c =>
                MatchesComponentIdentity(c, new ComponentIdentity(scenePath, objectPath, componentType, index, goid)));

            if (match != null)
            {
                ApplyComponentChangeToSnapshot(match);
                return true;
            }

            return false;
        }

        private static bool MatchesComponentIdentity(dynamic entry, ComponentIdentity identity)
        {
            if (!string.IsNullOrEmpty(entry.globalObjectId) && !string.IsNullOrEmpty(identity.GlobalObjectId))
            {
                if (entry.globalObjectId != identity.GlobalObjectId)
                {
                    return false;
                }
            }
            else
            {
                if (entry.scenePath != identity.ScenePath || entry.objectPath != identity.ObjectPath)
                {
                    return false;
                }
            }

            return entry.componentType == identity.ComponentType &&
                   entry.componentIndex == identity.Index;
        }

        private void ApplyComponentChangeToSnapshot(CompChangesStore.ComponentChange match)
        {
            ApplyPropertiesToSnapshot(match.propertyPaths, match.valueTypes, match.serializedValues);
            ApplyMaterialsIfNeeded(match.includeMaterialChanges, match.materialGuids);
        }

        private void ApplyComponentOriginalToSnapshot(CompOriginalStore.ComponentOriginal match)
        {
            ApplyPropertiesToSnapshot(match.propertyPaths, match.valueTypes, match.serializedValues);
            ApplyMaterialsIfNeeded(true, match.materialGuids);
        }

        private void ApplyPropertiesToSnapshot(List<string> propertyPaths,
            List<string> valueTypes, List<string> serializedValues)
        {
            SerializedObject so = new(SnapshotComponent);

            for (int i = 0; i < propertyPaths.Count; i++)
            {
                SerializedProperty prop = so.FindProperty(propertyPaths[i]);
                if (prop==null)
                {
                    continue;
                }

                string typeName = GetValueAtIndex(valueTypes, i);
                string value = GetValueAtIndex(serializedValues, i);

                TryApplyPropertyValue(prop, typeName, value);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetValueAtIndex(List<string> list, int index)
        {
            return index < list.Count ? list[index] : string.Empty;
        }

        private static void TryApplyPropertyValue(SerializedProperty prop, string typeName, string value)
        {
            try
            {
                OcpSerialization.ApplySerializedComponentValue(prop, typeName, value);
            }
            catch
            {
                // ignored
            }
        }

        private void ApplyMaterialsIfNeeded(bool includeMaterials, List<string> materialGuids)
        {
            if (ShouldApplyMaterials(includeMaterials, materialGuids))
            {
                ApplyMaterials((Renderer)SnapshotComponent, materialGuids);
            }
        }

        private bool ShouldApplyMaterials(bool includeMaterials, List<string> materialGuids)
        {
            return includeMaterials && SnapshotComponent is Renderer && materialGuids is { Count: > 0 };
        }

        public void Cleanup()
        {
            if (SnapshotGo)
            {
                Object.DestroyImmediate(SnapshotGo);
            }
        }

        private static void ApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!IsValidMaterialApplication(renderer, materialGuids))
            {
                return;
            }

            var current = renderer.sharedMaterials;
            var applied = new Material[materialGuids.Count];

            for (int i = 0; i < materialGuids.Count; i++)
            {
                applied[i] = ResolveMaterial(materialGuids[i], current, i, renderer.name);
            }

            renderer.sharedMaterials = applied;
        }

        private static bool IsValidMaterialApplication(Renderer renderer, List<string> materialGuids)
        {
            return renderer != null && materialGuids != null && materialGuids.Count > 0;
        }

        private static Material ResolveMaterial(string guid, Material[] currentMaterials, int index,
            string rendererName)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (!mat)
            {
                mat = GetFallbackMaterial(currentMaterials, index);
                LogMaterialResolutionWarning(guid, rendererName);
            }

            return mat;
        }

        private static Material GetFallbackMaterial(Material[] currentMaterials, int index)
        {
            return index < currentMaterials.Length ? currentMaterials[index] : null;
        }

        private static void LogMaterialResolutionWarning(string guid, string rendererName)
        {
        }

        private readonly struct RectTransformData
        {
            public readonly Vector2 AnchoredPosition;
            public readonly Vector3 AnchoredPosition3D;
            public readonly Vector2 AnchorMin;
            public readonly Vector2 AnchorMax;
            public readonly Vector2 Pivot;
            public readonly Vector2 SizeDelta;
            public readonly Vector2 OffsetMin;
            public readonly Vector2 OffsetMax;

            public RectTransformData(Vector2 anchoredPosition, Vector3 anchoredPosition3D,
                Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta,
                Vector2 offsetMin, Vector2 offsetMax)
            {
                AnchoredPosition = anchoredPosition;
                AnchoredPosition3D = anchoredPosition3D;
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                Pivot = pivot;
                SizeDelta = sizeDelta;
                OffsetMin = offsetMin;
                OffsetMax = offsetMax;
            }

            public void ApplyTo(RectTransform rt)
            {
                rt.anchoredPosition = AnchoredPosition;
                rt.anchoredPosition3D = AnchoredPosition3D;
                rt.anchorMin = AnchorMin;
                rt.anchorMax = AnchorMax;
                rt.pivot = Pivot;
                rt.sizeDelta = SizeDelta;
                rt.offsetMin = OffsetMin;
                rt.offsetMax = OffsetMax;
            }
        }

        private interface ITransformData
        {
            bool IsRectTransform { get; }
            Vector3 Position { get; }
            Quaternion Rotation { get; }
            Vector3 Scale { get; }
            Vector2 AnchoredPosition { get; }
            Vector3 AnchoredPosition3D { get; }
            Vector2 AnchorMin { get; }
            Vector2 AnchorMax { get; }
            Vector2 Pivot { get; }
            Vector2 SizeDelta { get; }
            Vector2 OffsetMin { get; }
            Vector2 OffsetMax { get; }
        }

        private class TransformOriginalAdapter : ITransformData
        {
            private readonly OriginalStore.TransformOriginal data;

            public TransformOriginalAdapter(OriginalStore.TransformOriginal data)
            {
                this.data = data;
            }

            public bool IsRectTransform => data.isRectTransform;
            public Vector3 Position => data.position;
            public Quaternion Rotation => data.rotation;
            public Vector3 Scale => data.scale;
            public Vector2 AnchoredPosition => data.anchoredPosition;
            public Vector3 AnchoredPosition3D => data.anchoredPosition3D;
            public Vector2 AnchorMin => data.anchorMin;
            public Vector2 AnchorMax => data.anchorMax;
            public Vector2 Pivot => data.pivot;
            public Vector2 SizeDelta => data.sizeDelta;
            public Vector2 OffsetMin => data.offsetMin;
            public Vector2 OffsetMax => data.offsetMax;
        }

        private class TransformChangeAdapter : ITransformData
        {
            private readonly ChangesStore.TransformChange data;

            public TransformChangeAdapter(ChangesStore.TransformChange data)
            {
                this.data = data;
            }

            public bool IsRectTransform => data.isRectTransform;
            public Vector3 Position => data.position;
            public Quaternion Rotation => data.rotation;
            public Vector3 Scale => data.scale;
            public Vector2 AnchoredPosition => data.anchoredPosition;
            public Vector3 AnchoredPosition3D => data.anchoredPosition3D;
            public Vector2 AnchorMin => data.anchorMin;
            public Vector2 AnchorMax => data.anchorMax;
            public Vector2 Pivot => data.pivot;
            public Vector2 SizeDelta => data.sizeDelta;
            public Vector2 OffsetMin => data.offsetMin;
            public Vector2 OffsetMax => data.offsetMax;
        }

        private class TransformSnapshotAdapter : ITransformData
        {
            private readonly Snapshot data;

            public TransformSnapshotAdapter(Snapshot data)
            {
                this.data = data;
            }

            public bool IsRectTransform => data.isRectTransform;
            public Vector3 Position => data.position;
            public Quaternion Rotation => data.rotation;
            public Vector3 Scale => data.scale;
            public Vector2 AnchoredPosition => data.anchoredPosition;
            public Vector3 AnchoredPosition3D => data.anchoredPosition3D;
            public Vector2 AnchorMin => data.anchorMin;
            public Vector2 AnchorMax => data.anchorMax;
            public Vector2 Pivot => data.pivot;
            public Vector2 SizeDelta => data.sizeDelta;
            public Vector2 OffsetMin => data.offsetMin;
            public Vector2 OffsetMax => data.offsetMax;
        }

        private readonly struct ComponentIdentity
        {
            public string ScenePath { get; }
            public string ObjectPath { get; }
            public string ComponentType { get; }
            public int Index { get; }
            public string GlobalObjectId { get; }

            public ComponentIdentity(string scenePath, string objectPath, string componentType, int index, string goid)
            {
                ScenePath = scenePath;
                ObjectPath = objectPath;
                ComponentType = componentType;
                Index = index;
                GlobalObjectId = goid;
            }
        }
    }
}