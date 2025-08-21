using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.SkillSystem
{
    public class OnAbilityCharging : MonoBehaviour, IChargeable
    {
        [SerializeField] private bool _enableChargingEvents;
        [SerializeField, ShowIf(nameof(_enableChargingEvents))] private UnityEvent _onChargingStart;
        [SerializeField, ShowIf(nameof(_enableChargingEvents))] private UnityEvent _onChargingEnd;

        [BoxGroup("Scaling")]
        [SerializeField] private bool _enableScaleDuringCharging;
        [BoxGroup("Scaling")]
        [SerializeField, ShowIf(nameof(_enableScaleDuringCharging))] private Vector3 _endScale;
        private Vector3 _startScale;

        private void Awake()
        {
            _startScale = transform.localScale;
        }

        public void OnChargingStart()
        {
            if (_enableScaleDuringCharging)
            {
                transform.localScale = _startScale;
            }
            _onChargingStart?.Invoke();
        }
        public void OnChargingEnd()
        {
            if (_enableScaleDuringCharging)
            {
                transform.localScale = _endScale;
            }
            _onChargingEnd?.Invoke();
        }

        public void OnChargingUpdate(float chargeProgress)
        {
            if (_enableScaleDuringCharging)
            {
                transform.localScale = Vector3.Lerp(_startScale, _endScale, chargeProgress);
            }
        }

        // not used
        public float GetChargeTime()
        {
            return 0f;
        }
    }
}