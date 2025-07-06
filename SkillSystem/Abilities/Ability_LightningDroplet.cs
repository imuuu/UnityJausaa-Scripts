using Game.Extensions;
using Game.HitDetectorSystem;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.SkillSystem
{
    public class Ability_LightningDroplet : Ability, IRecastSkill
    {   
        [SerializeField][BoxGroup("Refs")] private GameObject _lightningDropletPrefab;
        [SerializeField][BoxGroup("Refs")] private GameObject _lightningDropletShardPrefab;

        [SerializeField] private float _dropletYOffset;
        private GameObject _spawnedDroplet;

        private int _shardCount;
        private float _durationToShoot = 2f;
        private float _distanceMultiplier = 1f;
        private float _dropletDistanceScaleMult = 1f;
        private Vector3 _defaultScale = Vector3.one;
        private GameObject[] _shards;
        private bool _launched = false;
        private bool _dropletScaledDown = false;
        public override void StartSkill()
        {
            _spawnedDroplet = ManagerPrefabPooler.Instance.GetFromPool(_lightningDropletPrefab);
            //_spawnedDroplet = GameObject.Instantiate(_lightningDropletPrefab, GetUserTransform().position, Quaternion.identity);
            _spawnedDroplet.transform.position = new Vector3(GetUserTransform().position.x,  _dropletYOffset, GetUserTransform().position.z);
            
            _defaultScale = _spawnedDroplet.transform.localScale;

            _durationToShoot = 2f;
            _distanceMultiplier = 1f;
            _dropletDistanceScaleMult = 0.15f;
            _launched = false;
            _dropletScaledDown = false;
            _shardCount = GetProjectileAmount();

            _shards = new GameObject[_shardCount];
            for(int i = 0; i < _shardCount; i++)
            {
                Vector3 offset = GetCircleOffset(i, _shardCount, 2f);

                _shards[i] = ManagerPrefabPooler.Instance.GetFromPool(_lightningDropletShardPrefab);
                _shards[i].transform.position = _spawnedDroplet.transform.position + offset;
                //_shards[i] = GameObject.Instantiate(_lightningDropletShardPrefab, _spawnedDroplet.transform.position + offset,Quaternion.identity);
                _shards[i].transform.LookAtInvert(_spawnedDroplet.transform);

                IProjectile projectile = _shards[i].GetComponent<IProjectile>();
                if(projectile is IEnabled enabledProjectile)
                {
                    enabledProjectile.SetEnable(false);
                }

                _shards[i].SetActive(true);
            }

            ActionScheduler.RunAfterDelay(_durationToShoot, () =>
            {
                Shoot();
            });
        }

        public override void EndSkill()
        {
            if (_spawnedDroplet != null)
            {
                _spawnedDroplet.transform.localScale = _defaultScale;
                if (_spawnedDroplet.activeSelf)
                {
                    ManagerPrefabPooler.Instance.ReturnToPool(_spawnedDroplet);
                }
                _spawnedDroplet = null;
            }

            if (_shards != null)
            {
                foreach (GameObject shard in _shards)
                {
                    if(shard != null && shard.activeSelf)
                    {
                        ManagerPrefabPooler.Instance.ReturnToPool(shard);
                    }
                  
                    //GameObject.Destroy(shard);
                }
                _shards = null;
            }
        }

        private void Shoot()
        {
            _launched = true;
            Transform[] targets = ManagerMob.Instance.GetClosestEnemies();
            if (targets.Length == 0)
            {
                Debug.Log("No targets found for Lightning Droplet.");
                return;
            }

            for (int i = 0; i < _shardCount; i++)
            {
                Transform target = targets[i % targets.Length];
                //MarkHelper.DrawSphereTimed(target.position+Vector3.up * 3, 0.5f, 3, Color.red);
                GameObject shard = _shards[i];
                SetupProjectile(shard, target);
            }
        }

        private void SetupProjectile(GameObject gameObject, Transform target)
        {
            IProjectile projectile = gameObject.GetComponent<IProjectile>();
            if (projectile != null)
            {
                projectile.SetTarget(target);
                projectile.SetSpeed(1f);
                projectile.SetMaxSpeed(40f);
                projectile.SetSpeedType(SPEED_TYPE.ACCELERATE, 0.5f);
            }

            if(projectile is IEnabled enabledProjectile)
            {
                enabledProjectile.SetEnable(true);
            }

            if(gameObject.TryGetComponent<HitDetector_SingleTarget>(out HitDetector_SingleTarget hitDetector))
            {
                hitDetector.SetTargetObject(target.gameObject);
            }

            //Damage etc
            IDamageDealer damageDealer = ApplyStats(gameObject);

            Vector3 playerPos = GetUserTransform().position;
            Vector3 dropletPos = _spawnedDroplet.transform.position;
            dropletPos.y = playerPos.y;

            float baseDamage = damageDealer.GetDamage();

            float distance = Vector3.Distance(dropletPos, playerPos);
            int wholeMeters = Mathf.FloorToInt(distance);

            float multiplier = 1f + (float) wholeMeters * _distanceMultiplier;

            damageDealer.SetDamage(baseDamage * multiplier);
        }

        
        public override void UpdateSkill()
        {
            if (_spawnedDroplet == null) return;

            if(_launched) 
            {
                if(_dropletScaledDown) return;
                
                Vector3 scale = _spawnedDroplet.transform.localScale;
                scale.x -= Time.deltaTime * 0.5f;
                scale.y -= Time.deltaTime * 0.5f;
                scale.z -= Time.deltaTime * 0.5f;
                _spawnedDroplet.transform.localScale = scale;
                if (scale.x <= 0f)
                {
                    _spawnedDroplet.transform.localScale = Vector3.zero;
                    _launched = false;
                    _dropletScaledDown = true;

                    if (_spawnedDroplet.activeSelf)
                    {
                        _spawnedDroplet.transform.localScale = _defaultScale;
                        ManagerPrefabPooler.Instance.ReturnToPool(_spawnedDroplet);
                    }
                    _spawnedDroplet = null;
                }
                return;
            }

            Vector3 playerPos = GetUserTransform().position;
            Vector3 dropPos = _spawnedDroplet.transform.position;
            dropPos.y = playerPos.y;

            float distance = Vector3.Distance(dropPos, playerPos);
            if (distance > 1f)
            {
                float extra = (distance - 1f) * _dropletDistanceScaleMult;
                float scale = 1f + extra;
                _spawnedDroplet.transform.localScale = _defaultScale * scale;
            }
            else
            {
                _spawnedDroplet.transform.localScale = _defaultScale;
            }
        }

        // public override void UpdateSkill()
        // {
        //     if (_spawnedDroplet == null) return;

        //     // horizontal‐plane distance from player
        //     Vector3 playerPos = GetUserTransform().position;
        //     Vector3 dropPos = _spawnedDroplet.transform.position;
        //     dropPos.y = playerPos.y;

        //     float rawDistance = Vector3.Distance(dropPos, playerPos);
        //     int wholeMeters = Mathf.FloorToInt(rawDistance);

        //     // if we’ve crossed one or more new whole meters
        //     if (wholeMeters > _lastMeterReported)
        //     {
        //         // for each new meter, fire a log
        //         Debug.Log($"▶️ Lightning Droplet is now {wholeMeters}m from player");

        //         GameObject chargeEffect = GameObject.Instantiate(_chargeEffectPrefab, dropPos, Quaternion.identity);
        //         chargeEffect.transform.position = GetUserTransform().position;

        //         IProjectile projectile = chargeEffect.GetComponent<IProjectile>();

        //         projectile.SetTarget(_spawnedDroplet.transform);
        //         projectile.SetSpeed(1f);
        //         projectile.SetMaxSpeed(8f);
        //         projectile.SetSpeedType(SPEED_TYPE.ACCELERATE, 0.1f);

        //         projectile.OnTargetReached += () =>
        //         {
        //             GameObject.Destroy(chargeEffect);
        //         };
        //         _lastMeterReported = wholeMeters;
        //     }
        // }
    }
}