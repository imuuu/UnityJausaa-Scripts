using UnityEngine;
namespace Game.PoolSystem
{
    public class PoolHealthDetection : MonoBehaviour
    {
        private IHealth _healthComponent;
        private HealthTriggerThreshold[] _healthTriggerThresholds;

        private void Awake() 
        {
            _healthComponent = GetComponent<IHealth>();
        }
        private void OnEnable() 
        {
            if(_healthComponent == null)
            {
                Debug.LogError("Health component not found on " + gameObject.name);
                return;
            }

            _healthComponent.OnHealthChanged += OnHealthChanged;
        }

        private void OnDisable() 
        {
            if(_healthComponent == null)
                return;

            _healthComponent.OnHealthChanged -= OnHealthChanged;
        }

        public void Init(HealthTriggerThreshold[] healthTriggerThresholds)
        {
            _healthTriggerThresholds = healthTriggerThresholds;
        }

        private void OnHealthChanged()
        {
            for (int i = 0; i < _healthTriggerThresholds.Length; i++)
            {
                if(_healthTriggerThresholds[i].IsTriggered)
                    continue;

                if(_healthTriggerThresholds[i].HealthTriggerPercent <= 0)
                {
                    if(_healthComponent.GetHealth() > 0) continue;

                    HealthTriggerThreshold(_healthTriggerThresholds[i]);
                    continue;
                }

                float healthPercent = _healthComponent.GetHealth() / _healthComponent.GetMaxHealth();
                if(healthPercent <= _healthTriggerThresholds[i].HealthTriggerPercent)
                {
                    HealthTriggerThreshold(_healthTriggerThresholds[i]);
                }
            }
        }

        private void HealthTriggerThreshold(HealthTriggerThreshold healthTriggerThreshold)
        {
            if(healthTriggerThreshold.IsReturnPool)
            {
                ManagerPrefabPooler.Instance.ReturnToPool(gameObject);
            }
        }
    }
}