using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    [Serializable]
    public class CompSnapshot
    {
        public string componentType;
        public string globalObjectId;
        public List<string> materialGuids = new();
        public Dictionary<string, object> Properties = new();

        public CompSnapshot()
        {
        }

        public CompSnapshot(Component comp)
        {
            componentType = comp.GetType().AssemblyQualifiedName;
            globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(comp.gameObject).ToString();
        }
    }
}