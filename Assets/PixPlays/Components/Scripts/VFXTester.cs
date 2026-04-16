using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PixPlays.ElementalVFX
{

    public class VFXTester : MonoBehaviour
    {
        [System.Serializable]
        public class TestingData
        {
            public string Name;
            public AnimationClip clip;
            public float VfxSpawnDelay;
            public BindingPointType Source;
            public float _Duration;
            public float _Radius;
            public BaseVfx VFX;
        }

        [SerializeField] List<TestingData> _Data;
        [SerializeField] Character _Character;
        [SerializeField] string _CurrentData;
        [SerializeField] private Button _ActivateButton;

        private int index = 0;

        private void Start()
        {
            _ActivateButton.onClick.AddListener(OnActivateButtonClicked);
        }

        private void OnActivateButtonClicked()
        {
            StartCoroutine(Coroutine_Spawn());
        }

        IEnumerator Coroutine_Spawn()
        {
            _Character.PlayAnimation("New Animation", _Data[index].clip);
            yield return new WaitForSeconds(_Data[index].VfxSpawnDelay);
            BaseVfx go = Instantiate(_Data[index].VFX);
            Transform sourcePoint = _Character.BindingPoints.GetBindingPoint(_Data[index].Source);
            var vfxData = new VfxData(sourcePoint, _Character.GetTarget(), _Data[index]._Duration, _Data[index]._Radius);
            vfxData.SetGround(_Character.BindingPoints.GetBindingPoint(BindingPointType.Ground));
            go.Play(vfxData);
        }
    }
}