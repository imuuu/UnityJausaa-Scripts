using System;
using UnityEngine;
namespace Game.SkillSystem
{
    public class Ability_Aura : Ability
    {
        [SerializeField] private GameObject _auraPrefab;
        [SerializeField] private LayerMask _enemyLayerMask;
        [SerializeField] private float _tickRate = 0.4f;
        [SerializeField] private int _maxEnemiesOnce = 15;
        private GameObject _auraInstance;

        private float _timer = 0f;

        private float _radius;
        private SimpleDamage _damageDealer;
        private string _uuid;
        public override void StartSkill()
        {
            _uuid = Guid.NewGuid().ToString();
            _radius = _baseStats.GetStat(StatSystem.STAT_TYPE.AREA_EFFECT).GetValue();

            _damageDealer = new SimpleDamage(_baseStats.GetStat(StatSystem.STAT_TYPE.DAMAGE).GetValue(),
            DAMAGE_TYPE.PHYSICAL, DAMAGE_SOURCE.PLAYER);

            if (_auraPrefab != null)
            {
                Transform target = GetUserTransform();
                _auraInstance = GameObject.Instantiate(_auraPrefab, target.position, Quaternion.identity);

                _auraInstance.transform.localScale = new Vector3(_radius * 2, _radius * 2, _radius * 2);
                //_auraInstance.transform.SetParent(target);
                //_auraInstance.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogError("Aura prefab is not assigned!");
            }
        }
        public override void EndSkill()
        {
            if (_auraInstance != null) GameObject.Destroy(_auraInstance);
        }

        public override void UpdateSkill()
        {
            _timer += Time.deltaTime;
            if (_timer >= _tickRate && ManagerMob.Instance.CanStartNewSearch())
            {
                //Debug.Log($"Aura tick: {_uuid}");
                _timer = 0f;
                ManagerMob.Instance.FindEnemiesInRangeAsync(_radius, _maxEnemiesOnce, OnEnemiesFound);
            }

            if (_auraInstance == null) return;

            _auraInstance.transform.position = GetUserTransform().position;
        }

        private void OnEnemiesFound(Transform[] obj)
        {
            if (obj == null || obj.Length == 0) return;

            foreach (Transform enemy in obj)
            {
                IOwner owner = enemy.GetComponent<IOwner>();
                if (owner == null) continue;

                owner = owner.GetRootOwner();
                IDamageReceiver damageReceiver = owner.GetGameObject().GetComponent<IDamageReceiver>();
                if (damageReceiver != null)
                {
                    DamageCalculator.CalculateDamage(_damageDealer, damageReceiver);
                }
            }
        }

        // private void TickAura()
        // {
        //     //MarkHelper.DrawSphereTimed(GetUserTransform().position, 1, 1, Color.white);
        //     int count = Physics.OverlapSphereNonAlloc(GetUserTransform().position, _radius, _results, _enemyLayerMask);
        //     for (int i = 0; i < count; i++)
        //     {
        //         IOwner owner = _results[i].GetComponent<IOwner>();

        //         owner = owner.GetRootOwner();
        //         IDamageReceiver damageReceiver = owner.GetGameObject().GetComponent<IDamageReceiver>();
        //         if (damageReceiver != null)
        //         {
        //             DamageCalculator.CalculateDamage(_damageDealer, damageReceiver);
        //         }
        //     }
        // }

        // void PrintLayerMaskNames(LayerMask mask)
        // {
        //     for (int i = 0; i < 32; i++)
        //     {
        //         if ((mask.value & (1 << i)) != 0)
        //         {
        //             string layerName = LayerMask.LayerToName(i);
        //             Debug.Log($"Layer {i}: {layerName}");
        //         }
        //     }
        // }
    }
}