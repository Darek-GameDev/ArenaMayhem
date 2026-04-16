using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class EarthShield : Shield
{
        [SerializeField] List<Rigidbody> _ShardRigidbodies;
        [SerializeField] float _AnimSpeed;
        [SerializeField] AnimationCurve _AnimCurve;
        [SerializeField] ParticleSystem _SpawnExplosionEffectPrefab;
        [SerializeField] Transform _RotationObjectOuter;
        [SerializeField] Transform _RotationObjectInner;
        [SerializeField] float _RotationSpeedOuter;
        [SerializeField] float _RotationSpeedInner;
        [SerializeField] Vector2 _ShardRadiusSpawn;
        [SerializeField] ParticleSystem _HitEffectPrefab;
        [SerializeField] float _ShardScale;
        private List<MeshCollider> _meshColliders;
        private List<Vector3> _sourcePositions;
        private List<Quaternion> _sourceRotations;
        IEnumerator Coroutine_Animate()
        {
            StartCoroutine(Coroutine_Rotate());
            if (_sourcePositions == null)
            {
                _sourcePositions = new List<Vector3>();
                _sourceRotations = new List<Quaternion>();
                foreach (var i in _ShardRigidbodies)
                {
                    _sourcePositions.Add(i.transform.localPosition);
                    _sourceRotations.Add(i.transform.localRotation);
                }
            }
            if (_meshColliders == null)
            {
                _meshColliders=new List<MeshCollider>();
                foreach (var i in _ShardRigidbodies)
                {
                    if(i.TryGetComponent<MeshCollider>(out MeshCollider collider))
                    {
                        _meshColliders.Add(collider);
                        collider.enabled = false;
                    }
                }
            }
            foreach(var i in _meshColliders)
            {
                i.enabled = false;
            }
            List<Vector3> shardPosition = new List<Vector3>();
            List<Quaternion> shardRotation = new List<Quaternion>();
            for (int i = 0; i < _ShardRigidbodies.Count; i++)
            {
                _ShardRigidbodies[i].gameObject.SetActive(true);
                _ShardRigidbodies[i].isKinematic = true;
                _ShardRigidbodies[i].transform.localScale= Vector3.one* _ShardScale;
                Vector3 dir = _ShardRigidbodies[i].transform.localPosition;//(_ShardRigidbodies[i].transform.position - transform.position);
                dir.y = 0;
                float distance = Random.Range(_ShardRadiusSpawn.x, _ShardRadiusSpawn.y);
                Quaternion rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
                Vector3 pos = transform.position + dir.normalized * distance;
                Ray ray = new Ray(transform.position, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 10))
                {
                    pos.y = hit.point.y;
                }
                else
                {
                    pos.y = transform.position.y - 1;
                }
                shardPosition.Add(pos);
                shardRotation.Add(rotation);

                ParticleSystem effect = Instantiate(_SpawnExplosionEffectPrefab, pos, Quaternion.identity);
                effect.Play();
            }
            float lerp = 0;
            while (lerp < 1)
            {
                for (int i = 0; i < _ShardRigidbodies.Count; i++)
                {
                    _ShardRigidbodies[i].transform.localPosition = Vector3.Lerp(transform.InverseTransformPoint(shardPosition[i]),
                        _sourcePositions[i], _AnimCurve.Evaluate(lerp));
                    _ShardRigidbodies[i].transform.localRotation = Quaternion.Lerp(shardRotation[i], _sourceRotations[i], _AnimCurve.Evaluate(lerp));
                }
                lerp += Time.deltaTime * _AnimSpeed;
                yield return null;
            }
            for (int i = 0; i < _ShardRigidbodies.Count; i++)
            {
                _ShardRigidbodies[i].transform.localPosition = _sourcePositions[i];
                _ShardRigidbodies[i].transform.localRotation = _sourceRotations[i];
            }
        }
        IEnumerator Coroutine_Rotate()
        {
            while (true)
            {
                _RotationObjectInner.Rotate(0, _RotationSpeedInner * Time.deltaTime, 0);
                _RotationObjectOuter.Rotate(0, _RotationSpeedOuter * Time.deltaTime, 0);
                yield return null;
            }
        }
        IEnumerator Coroutine_StopAnimation()
        {
            foreach (var i in _ShardRigidbodies)
            {
                i.isKinematic = false;
            }
            foreach(var i in _meshColliders)
            {
                i.enabled = true;
            }
            float lerp = 0;
            Vector3 startScale = Vector3.one* _ShardScale;
            Vector3 endScale = Vector3.zero;
            while (lerp < 1)
            {
                for (int i = 0; i < _ShardRigidbodies.Count; i++)
                {
                    _ShardRigidbodies[i].transform.localScale = Vector3.Lerp(startScale, endScale,lerp);
                }
                lerp +=Time.deltaTime * _AnimSpeed;
                yield return null;
            }
            for(int i = 0; i < _ShardRigidbodies.Count; i++)
            {
                _ShardRigidbodies[i].gameObject.SetActive(false);
            }
        }

        protected override void PlayImplementation()
        {
            StopAllCoroutines();
            StartCoroutine(Coroutine_Animate());
        }

        protected override void StopImplemenation()
        {
            StopAllCoroutines();
            StartCoroutine(Coroutine_StopAnimation());
        }

        protected override void HitImplementation(Vector3 point, Vector3 normal)
        {
            ParticleSystem effect = Instantiate(_HitEffectPrefab, point, Quaternion.identity);
            effect.transform.forward = normal;
            effect.Play();
        }
    }
}
