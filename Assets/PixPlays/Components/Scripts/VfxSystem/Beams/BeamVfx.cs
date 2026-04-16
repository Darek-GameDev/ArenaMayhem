using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class BeamVfx : BaseVfx
    {
        [SerializeField] private ParticleSystem _BeamBodyEffect;
        [SerializeField] private ParticleSystem _CastEffect;
        [SerializeField] private ParticleSystem _HitEffect;
        [SerializeField] private ParticleSystem _BodyTip;

        [SerializeField] private float _ScaleSpeed;
        private float _Length;

        public override void Play(VfxData _data)
        {
            base.Play(_data);
            _Length = (_data.Target - _data.Source).magnitude;
            StartCoroutine(Coroutine_Play());
        }

        public override void Stop()
        {
            base.Stop();
            _BeamBodyEffect.Stop();
            _CastEffect.Stop();
            _HitEffect.Stop();
            _BodyTip.Stop();
        }

        IEnumerator Coroutine_Play()
        {
            _HitEffect.gameObject.SetActive(false);
            _CastEffect.gameObject.SetActive(true);
            _BeamBodyEffect.gameObject.SetActive(true);
            _BodyTip.gameObject.SetActive(true);

            float length = (_data.Target - _data.Source).magnitude;
            Vector3 direction = (_data.Target - _data.Source);

            float lerp = 0;
            _CastEffect.transform.position = _data.Source;
            _CastEffect.transform.forward = direction;
            _CastEffect.Play();

            _BeamBodyEffect.transform.position = _data.Source;
            _BeamBodyEffect.transform.forward = direction;
            _BeamBodyEffect.Play();

            _BodyTip.Play();
            Vector3 startScale = _BeamBodyEffect.transform.localScale;
            startScale.z = 0;
            while (lerp < 1)
            {
                length = (_data.Target - _data.Source).magnitude;
                direction = (_data.Target - _data.Source);
                _CastEffect.transform.position = _data.Source;
                _CastEffect.transform.forward = direction;
                _BeamBodyEffect.transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x, startScale.y, length), lerp);
                _BeamBodyEffect.transform.position = _data.Source;
                _BeamBodyEffect.transform.forward = direction;
                _BodyTip.transform.position = _data.Source + direction.normalized * _BeamBodyEffect.transform.localScale.z;
                _BodyTip.transform.forward = direction;
                lerp += (Time.deltaTime*_ScaleSpeed)/_Length;
                yield return null;
            }
            _BodyTip.transform.position = _data.Source + direction.normalized * _BeamBodyEffect.transform.localScale.z;
            _HitEffect.gameObject.SetActive(true);
            _HitEffect.transform.position = _data.Target;
            _HitEffect.transform.forward = -direction;
            _HitEffect.Play();

            while (true)
            {
                length = (_data.Target - _data.Source).magnitude;
                direction = (_data.Target - _data.Source);
                _BodyTip.transform.position = _data.Source + direction.normalized * _BeamBodyEffect.transform.localScale.z;
                _CastEffect.transform.position = _data.Source;
                _CastEffect.transform.forward = direction;
                _BeamBodyEffect.transform.localScale = new Vector3(startScale.x, startScale.y, length);
                _BeamBodyEffect.transform.position = _data.Source;
                _BeamBodyEffect.transform.forward = direction;
                _HitEffect.transform.position = _data.Target;
                _HitEffect.transform.forward = -direction;
                yield return null;
            }
        }
    }
}
