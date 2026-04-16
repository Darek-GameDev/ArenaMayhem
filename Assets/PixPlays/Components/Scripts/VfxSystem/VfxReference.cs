using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public abstract class VfxReference : MonoBehaviour
    {
        public abstract void Play();
        public abstract void Stop();
    }
}
