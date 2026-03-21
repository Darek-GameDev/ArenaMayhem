using System;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    [Serializable]
    public class NameSnapshot
    {
        public string objectName;
        public string globalObjectId;

        public NameSnapshot(GameObject go)
        {
            objectName = go.name;
            globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
        }
    }
}