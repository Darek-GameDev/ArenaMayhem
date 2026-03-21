using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.App;
using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Recording
{
    public static class ComponentChangeRecorder
    {
        public static void RecordComponentChangeToStore(Component comp, CompSnapshot originalSnapshot)
        {
            var store = CompChangesStore.LoadOrCreate();
            string scenePath = ChangesTrackerCore.GetNormalizedScenePath(comp.gameObject);
            string objectPath = SceneAndPathUtilities.GetGameObjectPath(comp.transform);
            int index = Array.IndexOf(comp.gameObject.GetComponents(comp.GetType()), comp);
            string globalObjectId = originalSnapshot?.globalObjectId ??
                                    GlobalObjectId.GetGlobalObjectIdSlow(comp.gameObject).ToString();

            var (paths, values, typeNames) = ExtractComponentProps(comp);
            bool includeMaterialChanges = comp is Renderer;
            var materialGuids = includeMaterialChanges
                ? MaterialChangeHandler.GetRendererMaterialGuids((Renderer)comp)
                : new List<string>();

            if (originalSnapshot != null)
            {
                var storeCtx = new ComponentStoreContext
                {
                    ScenePath = scenePath,
                    ObjectPath = objectPath,
                    ComponentIndex = index,
                    Component = comp,
                    OriginalSnapshot = originalSnapshot,
                    PropertyPaths = paths,
                    IncludeMaterialChanges = includeMaterialChanges
                };
                SaveOriginalComponentData(storeCtx);
            }

            var changeCtx = new ComponentChangeContext
            {
                ScenePath = scenePath,
                ObjectPath = objectPath,
                GlobalObjectId = globalObjectId,
                ComponentIndex = index,
                Component = comp,
                PropertyPaths = paths,
                SerializedValues = values,
                ValueTypes = typeNames,
                IncludeMaterialChanges = includeMaterialChanges,
                MaterialGuids = materialGuids
            };

            SaveComponentChange(store, changeCtx);
        }

        private static (List<string> paths, List<string> values, List<string> typeNames) ExtractComponentProps(
            Component comp)
        {
            var paths = new List<string>();
            var values = new List<string>();
            var typeNames = new List<string>();

            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Skip ignored properties (auto-calculated values)
                if (SnapShotHelper.Component_SH.ShouldIgnorePropertyPath(prop.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }

                if (ComponentPropertySerializer.IsTypeSupported(prop.propertyType))
                {
                    // Leaf property - serialize it
                    enterChildren = false;
                    paths.Add(prop.propertyPath);
                    ComponentPropertySerializer.SerializeProperty(prop, out string typeName, out string serializedValue);
                    typeNames.Add(typeName);
                    values.Add(serializedValue);
                }
                else
                {
                    // Unsupported / Container property - recurse into children
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                    {
                        // Explicitly capture array size
                        string sizePath = prop.propertyPath + ".Array.size";
                        paths.Add(sizePath);
                        values.Add(prop.arraySize.ToString());
                        typeNames.Add("Integer");
                    }
                    
                    enterChildren = true;
                }
            }

            return (paths, values, typeNames);
        }

        private static void SaveOriginalComponentData(ComponentStoreContext ctx)
        {
            var originalStore = CompOriginalStore.LoadOrCreate();
            if (OriginalComponentAlreadyStored(originalStore, ctx))
            {
                return;
            }

            var (values, types) = ExtractOriginalValues(ctx.OriginalSnapshot, ctx.PropertyPaths);
            var entry = new CompOriginalStore.ComponentOriginal
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.OriginalSnapshot.globalObjectId,
                componentType = ctx.Component.GetType().AssemblyQualifiedName,
                componentIndex = ctx.ComponentIndex,
                propertyPaths = new List<string>(ctx.PropertyPaths),
                serializedValues = values,
                valueTypes = types,
                materialGuids = ctx.IncludeMaterialChanges
                    ? new List<string>(ctx.OriginalSnapshot.materialGuids ?? new List<string>())
                    : new List<string>()
            };

            originalStore.entries.Add(entry);
            EditorUtility.SetDirty(originalStore);
        }

        private static bool OriginalComponentAlreadyStored(CompOriginalStore store, ComponentStoreContext ctx)
        {
            return store.entries.Exists(e => IsMatchingComponentOriginal(e, ctx));
        }

        private static bool IsMatchingComponentOriginal(CompOriginalStore.ComponentOriginal e,
            ComponentStoreContext ctx)
        {
            return e.scenePath == ctx.ScenePath &&
                   e.objectPath == ctx.ObjectPath &&
                   e.componentType == ctx.Component.GetType().AssemblyQualifiedName &&
                   e.componentIndex == ctx.ComponentIndex;
        }

        private static (List<string>, List<string>) ExtractOriginalValues(CompSnapshot snapshot, List<string> paths)
        {
            var values = new List<string>();
            var types = new List<string>();
            var collector = new PropertyValueCollector(values, types);

            foreach (var path in paths)
            {
                ExtractValueForPath(snapshot, path, collector);
            }

            return (values, types);
        }

        private static void ExtractValueForPath(CompSnapshot snapshot, string path, PropertyValueCollector collector)
        {
            if (PropertyValueExists(snapshot, path))
            {
                SerializePropertyValue(snapshot, path, collector);
            }
            else
            {
                collector.Types.Add(string.Empty);
                collector.Values.Add(string.Empty);
            }
        }

        private static void SerializePropertyValue(CompSnapshot snapshot, string path, PropertyValueCollector collector)
        {
            var val = snapshot.Properties[path];
            var result = SnapshotSerializer.SerializeValue(val);
            collector.Types.Add(result.TypeName);
            collector.Values.Add(result.SerializedValue);
        }

        private static bool PropertyValueExists(CompSnapshot snapshot, string path)
        {
            return snapshot.Properties != null && snapshot.Properties.TryGetValue(path, out var val) && val != null;
        }

        private static void SaveComponentChange(CompChangesStore store, ComponentChangeContext ctx)
        {
            var change = BuildComponentChange(ctx);
            UpsertComponentChange(store, ctx, change);
            EditorUtility.SetDirty(store);
            AssetDatabase.SaveAssets();
        }

        private static void UpsertComponentChange(CompChangesStore store, ComponentChangeContext ctx,
            CompChangesStore.ComponentChange change)
        {
            int existing = FindComponentChangeIndex(store, ctx);
            if (existing >= 0)
            {
                store.changes[existing] = change;
            }
            else
            {
                store.changes.Add(change);
            }
        }

        private static CompChangesStore.ComponentChange BuildComponentChange(ComponentChangeContext ctx)
        {
            return new CompChangesStore.ComponentChange
            {
                scenePath = ctx.ScenePath,
                objectPath = ctx.ObjectPath,
                globalObjectId = ctx.GlobalObjectId,
                componentType = ctx.Component.GetType().AssemblyQualifiedName,
                componentIndex = ctx.ComponentIndex,
                propertyPaths = ctx.PropertyPaths,
                serializedValues = ctx.SerializedValues,
                valueTypes = ctx.ValueTypes,
                includeMaterialChanges = ctx.IncludeMaterialChanges,
                materialGuids = ctx.MaterialGuids
            };
        }

        private static bool IsMatchingComponentChange(CompChangesStore.ComponentChange c, ComponentChangeContext ctx)
        {
            if (c.scenePath != ctx.ScenePath)
            {
                return false;
            }

            if (c.objectPath != ctx.ObjectPath)
            {
                return false;
            }

            if (c.componentType != ctx.Component.GetType().AssemblyQualifiedName)
            {
                return false;
            }

            return c.componentIndex == ctx.ComponentIndex;
        }

        private static int FindComponentChangeIndex(CompChangesStore store, ComponentChangeContext ctx)
        {
            return store.changes.FindIndex(c => IsMatchingComponentChange(c, ctx));
        }

        private sealed class PropertyValueCollector
        {
            public PropertyValueCollector(List<string> values, List<string> types)
            {
                Values = values;
                Types = types;
            }

            public List<string> Values { get; }
            public List<string> Types { get; }
        }
    }
}