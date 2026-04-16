using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PixPlays.ElementalVFX
{
    public class Character : MonoBehaviour
    {
        [SerializeField] Animator _Anim;
        [SerializeField] BindingPoints _BindingPoints;
        [SerializeField] Transform _Target;

        private AnimatorOverrideController _overrideController;
        public BindingPoints BindingPoints => _BindingPoints;

        private void Start()
        {
            if (_Anim.runtimeAnimatorController != null)
            {
                _overrideController = new AnimatorOverrideController(_Anim.runtimeAnimatorController);
                _Anim.runtimeAnimatorController = _overrideController;
            }
        }

        public void PlayAnimation(string clipId,AnimationClip clip)
        {
            if (_overrideController != null)
            {
                _overrideController[clipId] = clip;
                _Anim.SetTrigger("Play");
            }
        }

        public Vector3 GetTarget()
        {
            Vector3 direction = (_Target.position - transform.position).normalized;
            Ray ray = new Ray(transform.position, direction);
            RaycastHit hit;
            if(Physics.Raycast(ray,out hit,100))
            {
                return hit.point;
            }
            return _Target.position;
        }
    }
}