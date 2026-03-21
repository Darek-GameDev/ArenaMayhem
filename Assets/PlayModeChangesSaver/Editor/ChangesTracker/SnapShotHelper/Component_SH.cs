using System;
using System.Collections.Generic;
using System.Linq;
using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEngine;
using static PlayModeChangesSaver.Editor.ChangesTracker.SnapshotManager;

namespace PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper
{
    public static class Component_SH //Component SnapShot Helper
    {
        // Properties that should be ignored when checking for changes
        // These are typically auto-calculated or internal Unity properties
        // ONLY add properties that are truly auto-calculated and never user-editable
        private static readonly HashSet<string> IgnoredPropertyPaths = new()
        {
            // Renderer properties (auto-calculated bounds/batching)
            "m_Bounds",
            "m_AABB",
            "m_StaticBatchInfo",
            "m_SubsetIndices",
            
            // Collider properties (auto-calculated)
            "m_BoundingVolume",
            
            // Common internal Unity properties (prefab system)
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset"
        };

        private static bool ShouldIgnoreProperty(string propertyPath)
        {
            // Check exact match
            if (IgnoredPropertyPaths.Contains(propertyPath))
            {
                return true;
            }

            // Check if any ignored path is a prefix (for nested properties)
            foreach (var ignoredPath in IgnoredPropertyPaths)
            {
                if (propertyPath.StartsWith(ignoredPath + "."))
                {
                    return true;
                }
            }

            return false;
        }

        // Public wrapper for external use
        public static bool ShouldIgnorePropertyPath(string propertyPath)
        {
            return ShouldIgnoreProperty(propertyPath);
        }

        public static void ResetComponentBaseline(Component comp)
        {
            if (!comp)
            {
                return;
            }

            GameObject go = comp.gameObject;
            string goKey = GetGoKey(go);

            if (!ComponentSnapshots.TryGetValue(goKey, out var dict))
            {
                dict = new Dictionary<string, CompSnapshot>();
                ComponentSnapshots[goKey] = dict;
            }

            string compKey = SceneAndPathUtilities.GetComponentKey(comp);
            dict[compKey] = CaptureComponentSnapshot(comp);
        }

        public static List<Component> GetChangedComponents(GameObject go)
        {
            if (!go)
            {
                return new List<Component>();
            }

            string key = GetGoKey(go);

            if (!ComponentSnapshots.ContainsKey(key))
            {
                return new List<Component>();
            }

            var changed = new List<Component>();
            var compSnapshots = ComponentSnapshots[key];

            Transform_SH.CheckTransformChanges(go, key, changed);
            CheckComponentChanges(go, compSnapshots, changed);

            return changed;
        }


        private static void CheckComponentChanges(GameObject go, Dictionary<string, CompSnapshot> compSnapshots,
            List<Component> changed)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp is null or Transform)
                {
                    continue;
                }

                string compKey = SceneAndPathUtilities.GetComponentKey(comp);

                if (!compSnapshots.TryGetValue(compKey, out var snapshot))
                {
                    continue;
                }

                bool hasChanged = HasComponentChanged(comp, snapshot);

                if (hasChanged)
                {
                    changed.Add(comp);
                }
            }
        }

        private static bool HasComponentChanged(Component comp, CompSnapshot snapshot)
        {
            // Additional safety check - component might be destroyed between enumeration and check
            if (!comp)
            {
                return false;
            }

            try
            {
                var so = new SerializedObject(comp);
                if (HasComponentPropertiesChanged(so, snapshot))
                {
                    return true;
                }

                return comp is Renderer renderer && HaveRendererMaterialsChanged(renderer, snapshot.materialGuids);
            }
            catch (Exception)
            {
                // Component might have been destroyed or is in an invalid state
                return false;
            }
        }

        private static bool HasComponentPropertiesChanged(SerializedObject so, CompSnapshot snapshot)
        {
            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            
            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Skip ignored properties
                if (ShouldIgnoreProperty(prop.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }

                // If the property type is supported directly (e.g. primitives, vectors), check it.
                // If it is NOT supported (e.g. Generic, Array), we need to enter children.
                bool isSupported = ComponentPropertySerializer.IsTypeSupported(prop.propertyType);
                
                if (isSupported)
                {
                    enterChildren = false; // We handle this "leaf", don't go deeper
                    
                    if (!snapshot.Properties.TryGetValue(prop.propertyPath, out var originalValue))
                    {
                        // Property not in snapshot - likely added in newer Unity version or was filtered
                        // Skip it rather than marking as changed to avoid false positives
                        continue;
                    }

                    try
                    {
                        object currentValue = ComponentPropertySerializer.GetPropertyValue(prop);
                        if (HasPropertyValueChanged(currentValue, originalValue))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    // Not a supported leaf type, so it must be a container (e.g., Array, List, Struct)
                    // we want to dive into.
                    enterChildren = true;
                }
            }

            return false;
        }

        private static bool HasPropertyValueChanged(object currentValue, object originalValue)
        {
            if (AreValuesNullEqual(currentValue, originalValue))
            {
                return false;
            }

            if (IsEitherValueNull(currentValue, originalValue))
            {
                return true;
            }

            return ComparePropertyValues(currentValue, originalValue);
        }

        private static bool AreValuesNullEqual(object currentValue, object originalValue)
        {
            return currentValue==null && originalValue==null;
        }

        private static bool IsEitherValueNull(object currentValue, object originalValue)
        {
            return currentValue==null || originalValue==null;
        }

        private static bool ComparePropertyValues(object currentValue, object originalValue)
        {
            return currentValue switch
            {
                Vector2 v2Current when originalValue is Vector2 v2Original => HasVector2Changed(v2Current, v2Original),
                Vector3 v3Current when originalValue is Vector3 v3Original => HasVector3Changed(v3Current, v3Original),
                Quaternion qCurrent when originalValue is Quaternion qOriginal => HasQuaternionChanged(qCurrent, qOriginal),
                Rect rCurrent when originalValue is Rect rOriginal => rCurrent != rOriginal,
                RectInt riCurrent when originalValue is RectInt riOriginal => !riCurrent.Equals(riOriginal),
                Bounds bCurrent when originalValue is Bounds bOriginal => bCurrent != bOriginal,
                BoundsInt biCurrent when originalValue is BoundsInt biOriginal => biCurrent != biOriginal,
                Vector2Int v2iCurrent when originalValue is Vector2Int v2iOriginal => v2iCurrent != v2iOriginal,
                Vector3Int v3iCurrent when originalValue is Vector3Int v3iOriginal => v3iCurrent != v3iOriginal,
                float fCurrent when originalValue is float fOriginal => HasFloatChanged(fCurrent, fOriginal),
                UnityEngine.Object objCurrent when originalValue is UnityEngine.Object objOriginal => objCurrent != objOriginal,
                _ => !object.Equals(currentValue, originalValue)
            };
        }

        private static bool HasVector2Changed(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) > 0.0001f;
        }

        private static bool HasVector3Changed(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) > 0.0001f;
        }

        private static bool HasQuaternionChanged(Quaternion a, Quaternion b)
        {
            return Quaternion.Angle(a, b) > 0.0001f;
        }

        private static bool HasFloatChanged(float a, float b)
        {
            return Mathf.Abs(a - b) > 0.0001f;
        }

        private static bool HaveRendererMaterialsChanged(Renderer renderer, List<string> originalGuids)
        {
            var currentGuids = CaptureMaterialGuids(renderer);
            var origGuids = originalGuids ?? new List<string>();

            return AreGuidListsDifferent(currentGuids, origGuids);
        }

        private static bool AreGuidListsDifferent(List<string> currentGuids, List<string> originalGuids)
        {
            if (currentGuids.Count != originalGuids.Count)
            {
                return true;
            }

            return currentGuids.Where((guid, i) => !string.Equals(guid, originalGuids[i], StringComparison.Ordinal))
                .Any();
        }

        public static List<string> CaptureMaterialGuids(Renderer renderer)
        {
            var result = new List<string>();
            if (!renderer)
            {
                return result;
            }

            var materials = renderer.sharedMaterials;
            foreach (var mat in materials)
            {
                if (!mat)
                {
                    result.Add(string.Empty);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(mat);
                string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                result.Add(guid);
            }

            return result;
        }

        public static CompSnapshot CaptureComponentSnapshot(Component comp)
        {
            var snapshot = new CompSnapshot
            {
                componentType = comp.GetType().AssemblyQualifiedName,
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(comp.gameObject).ToString()
            };

            if (comp is Renderer renderer)
            {
                snapshot.materialGuids = CaptureMaterialGuids(renderer);
            }

            SerializedObject so = new(comp);
            SerializedProperty prop = so.GetIterator();

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Skip ignored properties
                if (ShouldIgnoreProperty(prop.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }

                bool isSupported = ComponentPropertySerializer.IsTypeSupported(prop.propertyType);
                if (isSupported)
                {
                    enterChildren = false;
                    try
                    {
                        object value = ComponentPropertySerializer.GetPropertyValue(prop);
                        if (value != null)
                        {
                            snapshot.Properties[prop.propertyPath] = value;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    // Recurse into complex types / arrays
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                    {
                        // Explicitly capture array size, because sometimes NextVisible skips it or order matters
                        string sizePath = prop.propertyPath + ".Array.size";
                        snapshot.Properties[sizePath] = prop.arraySize;
                    }
                    
                    enterChildren = true;
                }
            }

            return snapshot;
        }

        public static CompSnapshot GetComponentSnapshot(GameObject go, string componentKey)
        {
            string goKey = GetGoKey(go);
            if (!ComponentSnapshots.TryGetValue(goKey, out var componentSnapshot))
            {
                return null;
            }

            return componentSnapshot.GetValueOrDefault(componentKey);
        }
    }
}