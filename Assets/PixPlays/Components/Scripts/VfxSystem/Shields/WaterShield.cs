using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class WaterShield : Shield
    {
        [SerializeField] ParticleSystem _HitEffectPrefab;
        [SerializeField] Animation _Anim;
        [SerializeField] AnimationClip _SpawnAnimation;
        [SerializeField] AnimationClip _DespawnAnimation;
        [SerializeField] ParticleSystem _WaterAdditionalParticles;
        protected override void HitImplementation(Vector3 point, Vector3 normal)
        {
            ParticleSystem effect = Instantiate(_HitEffectPrefab, point, Quaternion.identity);
            effect.transform.forward = normal;
            effect.Play();
        }

        protected override void PlayImplementation()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            _WaterAdditionalParticles.Play();
            _Anim.clip = _SpawnAnimation;
            _Anim.Play();
        }

        protected override void StopImplemenation()
        {
            _WaterAdditionalParticles.Stop();
            _Anim.clip = _DespawnAnimation;
            _Anim.Play();
            StopAllCoroutines();
        }

    }
}