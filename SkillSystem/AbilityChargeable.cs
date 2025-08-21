using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.SkillSystem
{
    public abstract class AbilityChargeable : Ability, IChargeable
    {
        [BoxGroup("Charging Options")]
        [SerializeField] private GameObject _chargingEffectPrefab;
        [BoxGroup("Charging Options")]
        [SerializeField] private Vector3 _spawnOffset;
        [BoxGroup("Charging Options")]
        [Tooltip("If true, the charging effect will be removed when the charge ends. If false, it will removed after the ability ends")]
        [SerializeField] private bool _removeOnChargeEnd;
        [BoxGroup("Charging Options")]
        [SerializeField] private bool _followUser;

        private GameObject _chargeEffectInstance;
        private OnAbilityCharging _monoOnAbilityCharging;

        public void OnChargingStart()
        {
            if (_chargingEffectPrefab != null)
            {
                Vector3 position = GetUserTransform().position;
                //_chargeEffectInstance = GameObject.Instantiate(_chargingEffectPrefab, position, Quaternion.identity);
                _chargeEffectInstance = ManagerPrefabPooler.Instance.GetFromPool(_chargingEffectPrefab);
                _chargeEffectInstance.transform.position = position;
                _monoOnAbilityCharging = _chargeEffectInstance.GetComponentInChildren<OnAbilityCharging>();
                _monoOnAbilityCharging?.OnChargingStart();
            }
        }

        public void OnChargingEnd()
        {
            _monoOnAbilityCharging?.OnChargingEnd();
            if (!_removeOnChargeEnd) return;

            RemoveChargeInstance();
        }

        public void OnChargingUpdate(float chargeProgress)
        {
            _monoOnAbilityCharging?.OnChargingUpdate(chargeProgress);
            FollowUser();
        }

        public override void EndSkill()
        {
            base.EndSkill();

            if (_removeOnChargeEnd) return;

            RemoveChargeInstance();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            FollowUser();
        }

        private void FollowUser()
        {
            if (!_followUser || _chargeEffectInstance == null) return;

            Vector3 userPos = GetUserTransform().position;
            Vector3 baseDir = GetDirection(_direction);
            //SHOOTING_PATTERN pattern = SpawnPattern.Pattern;
            float cone = SpawnPattern.ConeAngle;
            Vector3 offset = _spawnOffset;

            Vector3 pos = ComputeSpawnPosition(userPos, baseDir, offset);

            _chargeEffectInstance.transform.position = pos;
        }

        private void RemoveChargeInstance()
        {
            if (_chargeEffectInstance != null)
            {
                //GameObject.Destroy(_chargeEffectInstance);
                ManagerPrefabPooler.Instance.ReturnToPool(_chargeEffectInstance);
                _chargeEffectInstance = null;
            }
        }
    }
}