using System.Collections.Generic;
using UnityEngine;
using static PlayModeChangesSaver.Editor.ChangesTracker.SnapshotManager;


namespace PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper
{
    public static class Transform_SH //Transform SnapShot Helper
    {
        public static void ResetTransformBaseline(GameObject go)
        {
            if (!go)
            {
                return;
            }

            string key = GetGoKey(go);
            Snapshots[key] = new Snapshot(go);
        }


        public static void CheckTransformChanges(GameObject go, string key, List<Component> changed)
        {
            if (Snapshots.TryGetValue(key, out var originalTransform))
            {
                Snapshot currentTransform = new(go);
                var transformChanges = GetChangedProperties(originalTransform, currentTransform);

                if (transformChanges.Count > 0)
                {
                    changed.Add(go.transform);
                }
            }
            else
            {
            }
        }


        public static void CheckRectTransformChanges(Snapshot original, Snapshot current, List<string> changed)
        {
            AddIfChanged(original.anchoredPosition, current.anchoredPosition, "anchoredPosition", changed);
            AddIfChanged(original.anchoredPosition3D, current.anchoredPosition3D, "anchoredPosition3D", changed);
            AddIfChanged(original.anchorMin, current.anchorMin, "anchorMin", changed);
            AddIfChanged(original.anchorMax, current.anchorMax, "anchorMax", changed);
            AddIfChanged(original.pivot, current.pivot, "pivot", changed);
            AddIfChanged(original.sizeDelta, current.sizeDelta, "sizeDelta", changed);
            AddIfChanged(original.offsetMin, current.offsetMin, "offsetMin", changed);
            AddIfChanged(original.offsetMax, current.offsetMax, "offsetMax", changed);
        }


        private static void AddIfChanged<T>(T originalValue, T currentValue, string propertyName, List<string> changed)
            where T : struct
        {
            if (!originalValue.Equals(currentValue))
            {
                changed.Add(propertyName);
            }
        }
    }
}