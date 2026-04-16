using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class ParticleSystemVfx : VfxReference
    {
        [SerializeField] ParticleSystem _Vfx;

        public override void Play()
        {
            _Vfx.Play();
        }

        public override void Stop()
        {
            _Vfx.Stop();
        }
    }
}
