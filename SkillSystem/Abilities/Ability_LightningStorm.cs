using System;
using Game.PoolSystem;
using UnityEngine;
namespace Game.SkillSystem
{
    public class Ability_LightningStorm : Ability, IRecastSkill
    {
        [SerializeField] private GameObject _lightningStormPrefab;

        [SerializeField] private float _radius;
        [SerializeField] private LayerMask _enemyLayerMask;
        private float _areaEffect;
        private float _count;
        private float _cooldown;
        private Collider[] _hitsOnce;
        private SimpleDamage _damageDealer;
        private GameObject[] _lightningStormInstances;
        public override void AwakeSkill()
        {
            base.AwakeSkill();
            _hitsOnce = new Collider[15];
        }

        public override void StartSkill()
        {
            _areaEffect = _baseStats.GetStat(StatSystem.STAT_TYPE.AREA_EFFECT).GetValue();
            _count = _baseStats.GetStat(StatSystem.STAT_TYPE.PROJECTILE_COUNT).GetValue();

            // TODO need to check the source, if added to mob
            _damageDealer = new SimpleDamage(_baseStats.GetStat(StatSystem.STAT_TYPE.DAMAGE).GetValue(),
           DAMAGE_TYPE.PHYSICAL, DAMAGE_SOURCE.PLAYER);

            float randomX = UnityEngine.Random.Range(-_radius, _radius);
            float randomZ = UnityEngine.Random.Range(minInclusive: -_radius, _radius);

            Vector3 randomPosition = new Vector3(randomX, 0, randomZ);

            ReturnToPool();
            _lightningStormInstances = new GameObject[(int)_count];
            for (int i = 0; i < _count; i++)
            {
                randomX = UnityEngine.Random.Range(-_radius, _radius);
                randomZ = UnityEngine.Random.Range(minInclusive: -_radius, _radius);
                randomPosition = new Vector3(randomX, 0, randomZ);
                randomPosition += GetUserTransform().position;
                randomPosition.y = 0;
                //_lightningStormInstances[i] = GameObject.Instantiate(_lightningStormPrefab, randomPosition, Quaternion.identity);
                _lightningStormInstances[i] = ManagerPrefabPooler.Instance.GetFromPool(_lightningStormPrefab);
                _lightningStormInstances[i].transform.position = randomPosition;
                ApplyStatsToReceivers(_lightningStormInstances[i], searchChildren:true);
            }
            

            //DoAreaDamage(randomPosition, _areaEffect);
            DoAreaDamage(_damageDealer, randomPosition, _hitsOnce, _enemyLayerMask, _areaEffect);
        }
        public override void EndSkill() 
        {
            ReturnToPool();
        }

        public override void UpdateSkill() { }


        private void ReturnToPool()
        {
            if (_lightningStormInstances != null)
            {
                foreach (GameObject lightningStorm in _lightningStormInstances)
                {
                    if (lightningStorm != null)
                    {
                        ManagerPrefabPooler.Instance.ReturnToPool(lightningStorm);
                    }
                }
                _lightningStormInstances = null;
            }
        }

        // private void DoAreaDamage(Vector3 position, float radius)
        // {
        //     //MarkHelper.DrawSphereTimed(GetUserTransform().position, radius, 1, Color.white);
        //     int count = Physics.OverlapSphereNonAlloc(position, radius, _hitsOnce, _enemyLayerMask);
        //     for (int i = 0; i < count; i++)
        //     {
        //         IOwner owner = _hitsOnce[i].GetComponent<IOwner>();

        //         owner = owner.GetRootOwner();
        //         IDamageReceiver damageReceiver = owner.GetGameObject().GetComponent<IDamageReceiver>();
        //         if (damageReceiver != null)
        //         {
        //             DamageCalculator.CalculateDamage(_damageDealer, damageReceiver);
        //         }
        //     }
        // }
    }
}