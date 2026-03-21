using System;
using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.App
{
    public static class ComponentChangeApplicator
    {
        public static void ApplyComponentChange(GameObject go, CompChangesStore.ComponentChange change)
        {
            string componentTypeName = change.componentType;
            if (string.IsNullOrEmpty(componentTypeName))
            {
                return;
            }

            var type = Type.GetType(componentTypeName);
            if (type==null)
            {
                return;
            }

            var allComps = go.GetComponents(type);
            if (change.componentIndex < 0 || change.componentIndex >= allComps.Length)
            {
                return;
            }

            var comp = allComps[change.componentIndex];
            if (!comp)
            {
                return;
            }

            ApplyComponentProperties(comp, change);

            if (change.includeMaterialChanges)
            {
                var renderer = comp as Renderer;
                if (renderer != null)
                {
                    MaterialChangeHandler.ApplyMaterials(renderer, change.materialGuids);
                }
            }
        }

        public static void ApplyComponentProperties(Component comp, CompChangesStore.ComponentChange change)
        {
            var so = new SerializedObject(comp);

            for (int i = 0; i < change.propertyPaths.Count; i++)
            {
                string path = change.propertyPaths[i];
                string value = change.serializedValues[i];
                string typeName = change.valueTypes[i];

                SerializedProperty prop = so.FindProperty(path);
                if (prop==null)
                {
                    continue;
                }

                ComponentPropertySerializer.ApplyPropertyValue(prop, typeName, value);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}