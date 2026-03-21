using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyProcessor;


namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyHelper
{
    public class ApplyTransformChangeCommand : SceneApplyProcessor.IApplyCommand
    {
        private readonly ChangesStore.TransformChange change;
        private Scene scene;

        public ApplyTransformChangeCommand(Scene scene, ChangesStore.TransformChange change)
        {
            this.scene = scene;
            this.change = change;
        }

        public void Execute()
        {
            GameObject go =
                SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
            if (!go)
            {
                return;
            }

            Transform t = go.transform;
            RectTransform rt = t as RectTransform;

            Undo.RecordObject(t, "Apply Play Mode Transform Changes");

            if (HasModifiedProperties())
            {
                ApplyModifiedProperties(t, rt);
            }
            else
            {
                ApplyFullSnapshot(t, rt);
            }

            MarkDirty(go);
        }

        private bool HasModifiedProperties()
        {
            return change.modifiedProperties is { Count: > 0 };
        }

        private void ApplyModifiedProperties(Transform t, RectTransform rt)
        {
            foreach (var prop in change.modifiedProperties)
            {
                ApplyPropertyToTransform(t, rt, change, prop);
            }
        }

        private void ApplyFullSnapshot(Transform t, RectTransform rt)
        {
            t.SetLocalPositionAndRotation(change.position, change.rotation);
            t.localScale = change.scale;

            ApplyRectTransformSnapshot(rt);
        }

        private void ApplyRectTransformSnapshot(RectTransform rt)
        {
            if (!rt || !change.isRectTransform)
            {
                return;
            }

            rt.anchoredPosition = change.anchoredPosition;
            rt.anchoredPosition3D = change.anchoredPosition3D;
            rt.anchorMin = change.anchorMin;
            rt.anchorMax = change.anchorMax;
            rt.pivot = change.pivot;
            rt.sizeDelta = change.sizeDelta;
            rt.offsetMin = change.offsetMin;
            rt.offsetMax = change.offsetMax;
        }

        private void MarkDirty(Object target)
        {
            EditorUtility.SetDirty(target);
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}