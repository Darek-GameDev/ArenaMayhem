using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class VfxData
    {
        public Vector3 Source => _sourceTransform != null ? _sourceTransform.position : _sourcePos;
        public Vector3 Target => _targetTransform != null ? _targetTransform.position : _targetPos;
        public Vector3 Ground => _groundTransform != null ? _groundTransform.position : _groundPos;

        public float Duration => _duration;
        public float Radius => _radius;


        private float _duration;
        private float _radius;
        private Transform _sourceTransform;
        private Transform _targetTransform;
        private Transform _groundTransform;
        private Vector3 _sourcePos;
        private Vector3 _targetPos;
        private Vector3 _groundPos;

        public VfxData(Vector3 source,Vector3 target,float duration,float radius)
        {
            _radius = radius;
            _sourcePos = source;
            _targetPos = target;
            _duration = duration;
        }
        public VfxData(Transform source, Vector3 target, float duration, float radius)
        {
            _radius = radius;
            _sourceTransform = source;
            _sourcePos = source.position;
            _targetPos = target;
            _duration = duration;
        }

        public VfxData(Transform source,Transform target,float duration,float radius)
        {
            _radius = radius;
            _sourceTransform = source;
            _targetTransform = target;
            _sourcePos = source.position;
            _targetPos = target.position;
            _duration = duration;
        }

        public void SetGround(Transform groundPoint)
        {
            _groundTransform = groundPoint;
            _groundPos = _groundTransform.position;
        }

        public void SetGround(Vector3 groundPos)
        {
            _groundPos = groundPos;
        }
    }
}
