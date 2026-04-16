using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public interface IHittable
    {
        public void OnHit(Vector3 hitPoint,Vector3 normal);
    }
}
