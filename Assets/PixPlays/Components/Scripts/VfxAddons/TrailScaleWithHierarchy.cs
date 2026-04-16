using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    [RequireComponent(typeof(TrailRenderer))]
    public class TrailScaleWithHierarchy : MonoBehaviour
    {
        private TrailRenderer _trailRenderer;
        private float _initialWidth;
        private float _initialTrailTime;
        private void Start()
        {
            _trailRenderer = GetComponent<TrailRenderer>();
            _initialWidth = _trailRenderer.widthMultiplier;
            _initialTrailTime = _trailRenderer.time;
        }
        private void Update()
        {
            _trailRenderer.widthMultiplier = GetWorldScale(transform, _initialWidth);
            _trailRenderer.time = _initialTrailTime * _trailRenderer.widthMultiplier;
        }

        private float GetWorldScale(Transform obj,float scale=1)
        {
            float newScale = obj.localScale.x * scale;
            if (obj.parent != null)
            {
                return GetWorldScale(obj.parent, newScale);
            }
            return newScale;
        }
    }
}
