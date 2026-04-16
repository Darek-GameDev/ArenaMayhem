using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemScaleLifetime : MonoBehaviour
    {
        [SerializeField] private float _LifetimeFactor;
        [SerializeField] Transform _ScaleObject;
        private ParticleSystem _particleSystem;
        private bool _playedFlag;
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
        private void Update()
        {
            if (_particleSystem.isPlaying)
            {
                ParticleSystem.MainModule main = _particleSystem.main;
                main.startLifetime = _LifetimeFactor * _ScaleObject.localScale.z;
            }
        }
    }
}
