using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.App
{
    public static class TransformChangeApplicator
    {
        public static void ApplyTransformChange(GameObject go, ChangesStore.TransformChange change)
        {
            Transform t = go.transform;
            RectTransform rt = t as RectTransform;

            if (change.modifiedProperties is { Count: > 0 })
            {
                foreach (var prop in change.modifiedProperties)
                {
                    ApplyPropertyToTransform(t, rt, change, prop);
                }
            }
            else
            {
                ApplyAllTransformProperties(t, rt, change);
            }
        }

        private static void ApplyPropertyToTransform(Transform t, RectTransform rt, ChangesStore.TransformChange change,
            string prop)
        {
            bool appliedCore = ApplyTransformCoreProperty(t, change, prop);
            if (!appliedCore)
            {
                ApplyRectTransformProperty(rt, change, prop);
            }
        }

        private static bool ApplyTransformCoreProperty(Transform t, ChangesStore.TransformChange change, string prop)
        {
            switch (prop)
            {
                case "position":
                    t.localPosition = change.position;
                    return true;
                case "rotation":
                    t.localRotation = change.rotation;
                    return true;
                case "scale":
                    t.localScale = change.scale;
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyRectTransformProperty(RectTransform rt, ChangesStore.TransformChange change,
            string prop)
        {
            if (!rt)
            {
                return;
            }

            switch (prop)
            {
                case "anchoredPosition":
                    rt.anchoredPosition = change.anchoredPosition;
                    break;
                case "anchoredPosition3D":
                    rt.anchoredPosition3D = change.anchoredPosition3D;
                    break;
                case "anchorMin":
                    rt.anchorMin = change.anchorMin;
                    break;
                case "anchorMax":
                    rt.anchorMax = change.anchorMax;
                    break;
                case "pivot":
                    rt.pivot = change.pivot;
                    break;
                case "sizeDelta":
                    rt.sizeDelta = change.sizeDelta;
                    break;
                case "offsetMin":
                    rt.offsetMin = change.offsetMin;
                    break;
                case "offsetMax":
                    rt.offsetMax = change.offsetMax;
                    break;
            }
        }

        private static void ApplyAllTransformProperties(Transform t, RectTransform rt,
            ChangesStore.TransformChange change)
        {
            t.SetLocalPositionAndRotation(change.position, change.rotation);
            t.localScale = change.scale;

            if (!ShouldApplyRectTransform(change, rt))
            {
                return;
            }

            ApplyAllRectTransformProperties(rt, change);
        }

        private static bool ShouldApplyRectTransform(ChangesStore.TransformChange change, RectTransform rt)
        {
            if (!rt)
            {
                return false;
            }

            return change.isRectTransform;
        }

        private static void ApplyAllRectTransformProperties(RectTransform rt, ChangesStore.TransformChange change)
        {
            rt.anchoredPosition = change.anchoredPosition;
            rt.anchoredPosition3D = change.anchoredPosition3D;
            rt.anchorMin = change.anchorMin;
            rt.anchorMax = change.anchorMax;
            rt.pivot = change.pivot;
            rt.sizeDelta = change.sizeDelta;
            rt.offsetMin = change.offsetMin;
            rt.offsetMax = change.offsetMax;
        }
    }
}