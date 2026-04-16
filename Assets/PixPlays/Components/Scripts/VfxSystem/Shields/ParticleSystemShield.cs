using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class ParticleSystemShield : Shield
    {
        [SerializeField] ParticleSystem _ShieldEffect;
        [SerializeField] ParticleSystem _HitEffectPrefab;
        protected override void HitImplementation(Vector3 point, Vector3 normal)
        {
            ParticleSystem effect = Instantiate(_HitEffectPrefab, point, Quaternion.identity);
            effect.transform.forward = normal;
            effect.Play();
        }

        protected override void PlayImplementation()
        {
            _ShieldEffect.Play();
        }

        protected override void StopImplemenation()
        {
            _ShieldEffect.Stop();
        }
    }
}
