using System;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    [Serializable]
    public class Snapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string globalObjectId;

        public bool isRectTransform;
        public Vector2 anchoredPosition;
        public Vector3 anchoredPosition3D;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector2 offsetMin;
        public Vector2 offsetMax;

        public Snapshot()
        {
        }

        public Snapshot(GameObject go)
        {
            Transform t = go.transform;
            position = t.localPosition;
            rotation = t.localRotation;
            scale = t.localScale;

            // Capture GlobalObjectId for robust GUID-based lookup
            globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            RectTransform rt = t as RectTransform;
            isRectTransform = rt;

            if (isRectTransform)
            {
                anchoredPosition = rt.anchoredPosition;
                anchoredPosition3D = rt.anchoredPosition3D;
                anchorMin = rt.anchorMin;
                anchorMax = rt.anchorMax;
                pivot = rt.pivot;
                sizeDelta = rt.sizeDelta;
                offsetMin = rt.offsetMin;
                offsetMax = rt.offsetMax;
            }
        }
    }
}