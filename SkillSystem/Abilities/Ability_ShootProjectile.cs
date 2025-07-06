using Sirenix.OdinInspector;
using UnityEngine;
using Game.PoolSystem;
using Game.HitDetectorSystem;
using Game.StatSystem;

#if RAYFIRE
using RayFire;
#endif

namespace Game.SkillSystem
{
    public class Ability_ShootProjectile : Ability, IRecastSkill
    {
        [Title("Projectile Settings")]
        public GameObject _projectilePrefab;

        public Vector3 _spawnOffset = new Vector3(0, 0.5f, 1.5f);
        //[MinValue(0)] public int _protectileCount;
        //public float _projectileSpeed = 0;
        //public float _projectileDamage = 0;

        private GameObject[] _spawnedProjectiles;
        private GameObject[] _spawnedPathSkills;

        [Title("Shooting Pattern Settings")]
        [EnumToggleButtons] public SHOOTING_PATTERN shootingPattern = SHOOTING_PATTERN.CONE;
        [ShowIf("shootingPattern", SHOOTING_PATTERN.CONE)]
        public float _coneAngle = 30f;
        [ShowIf("shootingPattern", SHOOTING_PATTERN.BURST)]
        public float _burstDelay = 0.1f;

        [Title("Path Skill Option")]
        [SerializeField, ToggleLeft] private bool _enablePathFollower = false;
        [ShowIf("_enablePathFollower")]
        [SerializeField] private PathSkill _pathSkillPrefab;
        [ShowIf("_enablePathFollower")][InfoBox("if enabled, duration will be set to max value.")]
        [SerializeField] private bool _endAfterPathComplete = true;

        [Tooltip("If enabled, the path will follow the user.")]
        [ShowIf("_enablePathFollower")]
        [SerializeField] private bool _isPathFollowsUser = false;


#if RAYFIRE
        private enum RAYFIRE_TYPE
        {
            GUN,
            BOMB
        }

        [Title("RayFire Settings"), Space(10)]
        [SerializeField, ToggleLeft] private bool _enableRayFire = false;

        [Space(5)]
        [SerializeField, ToggleLeft, BoxGroup("Behavior", showLabel: false), ShowIf("_enableRayFire")]
        private bool _applyProjectileDmg = true;

        [Title("Add RayFire Component"), ShowIf("_enableRayFire"), Space(5)]
        [SerializeField, BoxGroup("Behavior", showLabel: false), EnumToggleButtons] private RAYFIRE_TYPE _behaviorType;

        //Bomb settings
        [SerializeField, BoxGroup("Behavior", showLabel: false), ShowIf("_behaviorType", RAYFIRE_TYPE.BOMB), Min(0)]
        private float _bombRadius = 0;
        [SerializeField, BoxGroup("ApplyStatsReceiver")] private bool _applyStatsToReceivers;
        [SerializeField, BoxGroup("ApplyStatsReceiver")] private bool _applyStatsToChildren;
#endif

        public override void AwakeSkill()
        {
            base.AwakeSkill();

            if(_enablePathFollower && _endAfterPathComplete)
            {
                const float maxDuration = 9999f;
                Stat stat = _baseStats.GetStat(STAT_TYPE.DURATION);
                if(stat == null)
                {
                    Stat newStat = new Stat(maxDuration);
                    newStat.AddEffectByTag(STAT_TYPE.DURATION);
                    stat = _baseStats.AddStat(newStat);
                    return;
                }

                stat.SetBaseValue(maxDuration);
            }
        }

        public override void StartSkill()
        {
            //Debug.Log($"Starting skill SHOOT PROJECTILE");
            ShootProjectile();

            if(_enablePathFollower && _pathSkillPrefab != null)
            {

            }
        }   
        public override void EndSkill()
        {
            if (_spawnedProjectiles == null)
            {
                Debug.LogWarning("Spawned projectiles is null");
                return;
            }

            int projectileCount = GetProjectileAmount();
            for (int i = 0; i < projectileCount; i++)
            {
                if (_spawnedProjectiles[i] != null)
                {
                    _spawnedProjectiles[i].SetActive(false);
                }

                if(_spawnedPathSkills != null && _spawnedPathSkills[i] != null)
                {
                    //_spawnedPathSkills[i].SetActive(false);
                    ManagerPrefabPooler.Instance.ReturnToPool(_spawnedPathSkills[i]);
                }
            }

            _spawnedProjectiles = null;
            _spawnedPathSkills = null;
        }


        public override void UpdateSkill()
        {
            if (!_enablePathFollower) return;

            if (!_isPathFollowsUser) return;

            foreach (GameObject pathSkill in _spawnedPathSkills)
            {
                if (pathSkill == null) continue;

                pathSkill.transform.position = GetUserTransform().position + _spawnOffset;
            }
        }

        private void ShootProjectile()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab is null");
                return;
            }

            if(_baseStats == null)
            {
                Debug.LogError("Base stats is null in skill " + GetSkillName());
                return;
            }

            int projectileCount = GetProjectileAmount();

            //Debug.Log($"Shooting is there spawned protectiles in arrray: {(_spawnedProjectiles != null ? _spawnedProjectiles.Length : 0)}");
            _spawnedProjectiles = new GameObject[projectileCount];

            if(_enablePathFollower) _spawnedPathSkills = new GameObject[projectileCount];

            Vector3 baseDirection = GetDirection(_direction);

            if (projectileCount <= 1)
            {
                SpawnProjectileAtIndex(0, baseDirection);
                return;
            }

            if (shootingPattern == SHOOTING_PATTERN.BURST)
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    int index = i;
                    ActionScheduler.RunAfterDelay(_burstDelay * i, () =>
                    {
                        SpawnProjectileAtIndex(index, baseDirection);
                    });
                }
            }
            else
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    Vector3 projectileDirection = baseDirection;
                    switch (shootingPattern)
                    {
                        case SHOOTING_PATTERN.CONE:
                            float angleStep = _coneAngle / (projectileCount - 1);
                            float offsetAngle = -_coneAngle / 2 + angleStep * i;
                            projectileDirection = Quaternion.Euler(0, offsetAngle, 0) * baseDirection;
                            break;
                        case SHOOTING_PATTERN.CIRCLE:
                            float circleAngle = 360f / projectileCount;
                            float circleOffset = circleAngle * i;
                            projectileDirection = Quaternion.Euler(0, circleOffset, 0) * baseDirection;
                            break;
                    }
                    SpawnProjectileAtIndex(i, projectileDirection);
                }
            }
        }

        private void SpawnProjectileAtIndex(int index, Vector3 projectileDirection)
        {
            Vector3 spawnPosition;
            spawnPosition = GetUserTransform().position + projectileDirection.normalized * _spawnOffset.z;
            spawnPosition.y = GetUserTransform().position.y + _spawnOffset.y;


            _spawnedProjectiles[index] = ManagerPrefabPooler.Instance.GetFromPool(_projectilePrefab, (spawned) =>
            {
                // This was so if object has returned to pool from other resource, it will be set to null.
                if (_spawnedProjectiles == null) return;

                _spawnedProjectiles[index] = null;
            });

            if(_enablePathFollower)
            {
                _spawnedPathSkills[index] = ManagerPrefabPooler.Instance.GetFromPool(_pathSkillPrefab.gameObject, (spawned) =>
                {
                    if (_spawnedPathSkills == null) return;

                    _spawnedPathSkills[index] = null;
                });

            }

            if (_spawnedProjectiles[index] == null)
            {
                Debug.LogError("Failed to get projectile from pool");
                return;
            }

            if (_enablePathFollower && _spawnedPathSkills[index] == null)
            {
                Debug.LogError("Failed to get path skill from pool");
                return;
            }

#if UNITY_EDITOR
            if (_spawnedProjectiles[index].GetComponent<IOwner>() == null)
            {
                Debug.LogError("IOwner component not found on projectile prefab");
            }
#endif
            PathSkill pathSkill = _enablePathFollower ? _spawnedPathSkills[index].GetComponent<PathSkill>() : null;

            if(pathSkill != null)
            {
                pathSkill.SetOnCompleteAction(() =>
                {
                    EndTheSkill();
                });
            }

            OnProjectileSpawn(_spawnedProjectiles[index], spawnPosition, projectileDirection, pathSkill);
        }

        /// <summary>
        /// Spawns the projectile and sets its properties.
        /// 
        /// </summary>
        /// <param name="spawned"></param>
        /// <param name="spawnPosition"></param>
        /// <param name="direction">Base projectile direction</param>
        /// <param name="pathSkill"> Can be null, if these isnt path follower enabled</param>
        protected virtual void OnProjectileSpawn(GameObject spawned, Vector3 spawnPosition, Vector3 direction, PathSkill pathSkill)
        {
            spawned.GetComponent<IOwner>().SetOwner(GetOwnerType());

            IProjectile projectile = spawned.GetComponent<IProjectile>();
            float speed = _baseStats.GetStat(STAT_TYPE.SPEED) != null ? _baseStats.GetStat(STAT_TYPE.SPEED).GetValue() : 0;
            projectile.SetSpeed(speed);
            projectile.SetDirection(direction);

            ApplyStats(spawned);

            if(_applyStatsToReceivers)
            {
                ApplyStatsToReceivers(spawned, searchChildren: _applyStatsToChildren);
            }

            // POSITION
            if (pathSkill != null)
            {
                pathSkill.transform.position = spawnPosition;

                // if (_isPathFollowsUser)
                // {
                //     pathSkill.transform.SetParent(GetUserTransform());
                //     pathSkill.transform.SetParent(null);
                // }

                //pathSkill.transform.LookAt(spawnPosition + direction*10f);

                pathSkill.SetDirection(direction);

                pathSkill.AddObjectToPath(projectile, Vector3.zero, () => 
                {
                    if(spawned == null || !spawned.activeSelf) return;

                    //Debug.Log("Path skill complete, returning to pool projectile: " + spawned.name);
                    ManagerPrefabPooler.Instance.ReturnToPool(spawned);
                });

            }
            else
            {
                spawned.transform.position = spawnPosition;
            }

            // if(GetSoundData() != null)
            // {
            //     SoundBuilder soundBuilder = new SoundBuilder(ManagerSound.Instance)
            //         .SetSoundData(GetSoundData())
            //         .SetPosition(GetUserTransform().position)
            //         .SetRandomPitch(true);

            //     soundBuilder.Play();
            // }

            if (!_enableRayFire) return;

            ApplyRayFireBehavior(spawned);
        }

        private void ApplyRayFireBehavior(GameObject spawned)
        {
#if RAYFIRE
            IHitDetector hitDetector = spawned.GetComponent<IHitDetector>();

            if (hitDetector == null)
            {
                Debug.LogError("IHitDetector component not found on projectile prefab. Due to this RayFire component will not be added");
                return;
            }

            hitDetector.SetRayFireTriggerEnable(true);

            switch (_behaviorType)
            {
                case RAYFIRE_TYPE.GUN:
                    RayfireGun gun = spawned.GetComponent<RayfireGun>();
                    if (gun == null)
                        gun = spawned.AddComponent<RayfireGun>();

                    gun.axis = AxisType.ZBlue;

                    if (_applyProjectileDmg) gun.damage = _baseStats.GetStat(STAT_TYPE.DAMAGE).GetValue();

                    break;
                case RAYFIRE_TYPE.BOMB:
                    RayfireBomb bomb = spawned.GetComponent<RayfireBomb>();
                    if (bomb == null)
                        bomb = spawned.AddComponent<RayfireBomb>();

                    if (_applyProjectileDmg)
                    {
                        bomb.applyDamage = true;
                        bomb.damageValue = _baseStats.GetStat(STAT_TYPE.DAMAGE).GetValue();
                    }

                    if (_bombRadius > 0)
                        bomb.range = _bombRadius;

                    break;
            }
#endif
        }

    }
}
