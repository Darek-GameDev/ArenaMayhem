using System;
using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyProcessor;


namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyHelper
{
    public class ApplyComponentChangeCommand : SceneApplyProcessor.IApplyCommand
    {
        private readonly CompChangesStore.ComponentChange change;
        private Scene scene;

        public ApplyComponentChangeCommand(Scene scene, CompChangesStore.ComponentChange change)
        {
            this.scene = scene;
            this.change = change;
        }

        public void Execute()
        {
            if (!TryGetTargetComponent(out var comp))
            {
                return;
            }

            var so = new SerializedObject(comp);
            Undo.RecordObject(comp, "Apply Play Mode Component Changes");

            ApplySerializedProperties(so);
            so.ApplyModifiedProperties();

            ApplyMaterialChangesIfNeeded(comp);

            EditorUtility.SetDirty(comp);
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private bool TryGetTargetComponent(out Component component)
        {
            component = null;

            var go = SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
            if (!go)
            {
                return false;
            }

            var type = Type.GetType(change.componentType);
            if (type==null)
            {
                return false;
            }

            var allComps = go.GetComponents(type);
            if (change.componentIndex < 0 || change.componentIndex >= allComps.Length)
            {
                return false;
            }

            component = allComps[change.componentIndex];
            if (!component)
            {
                return false;
            }

            return true;
        }

        private void ApplySerializedProperties(SerializedObject so)
        {
            for (int i = 0; i < change.propertyPaths.Count; i++)
            {
                string path = change.propertyPaths[i];
                string value = change.serializedValues[i];
                string typeName = change.valueTypes[i];

                var prop = so.FindProperty(path);
                if (prop==null)
                {
                    continue;
                }

                ComponentPropertySerializer.ApplyPropertyValue(prop, typeName, value);
            }
        }

        private void ApplyMaterialChangesIfNeeded(Component comp)
        {
            if (!change.includeMaterialChanges)
            {
                return;
            }

            if (comp is Renderer renderer)
            {
                ApplyMaterials(renderer, change.materialGuids);
            }
        }
    }
}