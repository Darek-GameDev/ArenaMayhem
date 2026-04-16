using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class WindAoeVfx : LocationVfx
    {
        [SerializeField] Transform _GroundEffect;

        public override void Play(VfxData data)
        {
            base.Play(data);
            _GroundEffect.transform.position = _data.Ground;
        }
    }
}
