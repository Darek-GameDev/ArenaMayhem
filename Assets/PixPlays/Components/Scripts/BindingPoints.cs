using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class BindingPoints : MonoBehaviour
    {
        [System.Serializable]
        public class BindingPointData
        {
            public BindingPointType Type;
            public Transform Point;
        }

        [SerializeField] private List<BindingPointData> _BindingPoints;

        public Transform GetBindingPoint(BindingPointType type)
        {
            BindingPointData data = _BindingPoints.Find(x => x.Type == type);
            if (data != null)
            {
                return data.Point;
            }
            Debug.LogError($"Binding type {type.ToString()} not defined");
            return transform;
        }
    }
}
