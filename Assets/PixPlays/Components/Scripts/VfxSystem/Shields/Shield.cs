using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public abstract class Shield : BaseVfx, IHittable
    {
        [SerializeField] float _RadiusFactor;
        public override void Play(VfxData data)
        {
            base.Play(data);
            transform.position = data.Source;
            transform.localScale = Vector3.one * _RadiusFactor * _data.Radius;
            gameObject.SetActive(true);
            PlayImplementation();
        }

        public override void Stop()
        {
            base.Stop();
            StopImplemenation();
        }

        protected abstract void PlayImplementation();
        protected abstract void StopImplemenation();
        protected abstract void HitImplementation(Vector3 point, Vector3 normal);

        public void OnHit(Vector3 hitPoint, Vector3 normal)
        {
            HitImplementation(hitPoint, normal);
        }
    }
}