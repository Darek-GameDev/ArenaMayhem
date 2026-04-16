using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class BaseVfx : MonoBehaviour
    {
        [SerializeField] float _SafetyDestroy; //Destroy the object after a certan time in case user error keeps it active.
        [SerializeField] float _DestoyDelay; //Wait for effect to finish stopping before destroying the GameObject
        protected VfxData _data;
        public virtual void Play(VfxData data)
        {
            _data = data;
            if (_data.Duration > _SafetyDestroy)
            {
                _SafetyDestroy += _data.Duration;//Offset the safety destroy by the duration if bigger;
            }
            Destroy(gameObject, _SafetyDestroy);
            Invoke(nameof(Stop), _data.Duration);
            StopAllCoroutines();
        }

        public virtual void Stop()
        {
            StopAllCoroutines();
            Destroy(gameObject, _DestoyDelay);
        }
    }
}