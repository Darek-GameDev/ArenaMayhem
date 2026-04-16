using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemStartStopLifetime : MonoBehaviour
    {
        [SerializeField] private float _StartLifetime;
        [SerializeField] private float _StopLifetime;
        private ParticleSystem _particleSystem;
        private bool _playedFlag;
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
        private void Update()
        {
            if (_particleSystem.isEmitting && !_playedFlag)
            {
                _playedFlag = true;
            }
            if (!_particleSystem.isEmitting && _playedFlag)
            {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[_particleSystem.particleCount];
                _particleSystem.GetParticles(particles);
                for (int i = 0; i < particles.Length; i++)
                {
                    float percentage = particles[i].remainingLifetime / particles[i].startLifetime;
                    particles[i].startLifetime = _StopLifetime;
                    particles[i].remainingLifetime = _StopLifetime * percentage;
                }
                _particleSystem.SetParticles(particles);
                _playedFlag = false;
            }
        }
    }
}
