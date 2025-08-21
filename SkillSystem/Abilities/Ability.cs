using System.Collections.Generic;
using Game.HitDetectorSystem;
using Game.PoolSystem;
using Game.StatSystem;
using Sirenix.OdinInspector;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.SkillSystem
{
    //todo why this couldnt be just SKILL? WHat I have been thinking?
    public abstract class Ability : IAbility, IDuration, ICooldown, IOwner
    {
        private int _instanceID = -1;
        private GameObject _user;
        private OWNER_TYPE _ownerType;
        private SKILL_NAME _skillName = SKILL_NAME.NONE;

        private ISkill _rootSkill;

        [Title("Base Statistics")]
        [InfoBox("Remember assign at least cooldown and duration!")]
        [SerializeField] protected StatList _baseStats;
        //[SerializeField] private float _cooldown;
        //[SerializeField] private float _duration;
        private float _currentCooldown;
        private float _currentDuration;

        private bool _isAwaken = false;
        private int _skillSlot; //player only

        [Space(10)]
        [SerializeField] protected DIRECTION _direction;

        private static Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        public enum DIRECTION
        {
            NONE,
            FORWARD,
            BACKWARD,
            LEFT,
            RIGHT,
            MOUSE_POSITION,
            CLOSEST_ENEMY,
            RANDOM_AROUND_PLAYER,
            FARTHEST_ENEMY,
            CUSTOM
        }

        public enum SHOOTING_PATTERN
        {
            CONE,
            CIRCLE,
            BURST
        }

        public virtual void AwakeSkill()
        {
            _isAwaken = true;

            _dirScratch = new List<Vector3>();
            _spawnedTracked = new List<GameObject>();
            _spawnedIndices = new List<int>();
            _followTotalCount = 0;

            // if (_baseStats == null)
            // {
            //     Debug.LogWarning("Base stats is null, cannot initialize ability." + GetSkillName());
            //     return;
            // }

            // if(_baseStats.GetStat(STAT_TYPE.DURATION) == null)
            // {
            //     Debug.LogWarning("Duration stat is null, cannot initialize ability." + GetSkillName());
            //     return;
            // }

            // if(_baseStats.GetStat(STAT_TYPE.COOLDOWN) == null)
            // {
            //     Debug.LogWarning("Cooldown stat is null, cannot initialize ability." + GetSkillName());
            //     return;
            // }

        }

        public abstract void StartSkill();
        public virtual void EndSkill()
        {
            ActionScheduler.CancelActions(ID_BURST_DELAY + GetInstanceID());
            ReturnAllSpawned();
        }

        public virtual void UpdateSkill()
        {
            UpdateSpawnPatternFollow();
        }
        public virtual bool IsSkillUsable()
        {
            return true;
        }
        public bool HasAwaken()
        {
            return _isAwaken;
        }

        public void PrintData()
        {
            Debug.Log("Ability: " + this.GetType().Name);

            if (_user != null) Debug.Log("==> User: " + _user.name);
            else Debug.Log("==> User: null");

            Debug.Log("==> MobType: " + _ownerType);
            Debug.Log("==> Cooldown: " + GetCooldown());
            Debug.Log("==> Duration: " + GetDuration());
            Debug.Log("==> CurrentCooldown: " + _currentCooldown);
            Debug.Log("==> CurrentDuration: " + _currentDuration);
            //Debug.Log("==> Damage: " + Damage);
            //Debug.Log("==> Speed: " + Speed);
        }

        protected IDamageDealer ApplyStats(GameObject target)
        {
            IDamageDealer damageDealer = target.GetComponent<IDamageDealer>();
            if (damageDealer == null)
            {
                Debug.LogWarning($"Target does not have IDamageDealer component on it: {target.name}");
                return null;
            }

            if (_baseStats.GetStat(STAT_TYPE.DAMAGE) == null)
            {
                return null;
            }

            damageDealer.SetDamage(_baseStats.GetStat(STAT_TYPE.DAMAGE).GetValue());

            IHitDetector hitDetector = target.GetComponent<IHitDetector>();
            if (hitDetector == null) return damageDealer;

            if (hitDetector is IStatReceiver statReceiver)
            {
                statReceiver.SetStats(_baseStats);
            }

            // if (_baseStats.TryGetStat(STAT_TYPE.PIERCE_CHANCE, out Stat pierceChanceStat))
            // {
            //     if (hitDetector is IStatReceiver statReceiver)
            //     {
            //         statReceiver.SetStat(pierceChanceStat);
            //     }
            // }

            // if (_baseStats.TryGetStat(STAT_TYPE.DAMAGE_PERCENT_EACH_PIERCE, out Stat damageByPierceStat))
            // {
            //     if(hitDetector is IStatReceiver statReceiver)
            //     {
            //         statReceiver.SetStat(damageByPierceStat);
            //     }
            // }

            //TODO CRIT HERE

            return damageDealer;
        }

        protected Vector3 GetDirection(DIRECTION direction)
        {
            switch (direction)
            {
                case DIRECTION.CLOSEST_ENEMY:
                    Transform[] enemies = ManagerMob.Instance.GetClosestEnemies();
                    if (enemies.Length == 0 || enemies[0] == null)
                    {
                        return GetDirection(DIRECTION.MOUSE_POSITION);
                    }
                    Vector3 directionToEnemy = enemies[0].position - GetUserTransform().position;
                    return directionToEnemy.normalized;
                case DIRECTION.FARTHEST_ENEMY:
                    Transform[] farthestEnemies = ManagerMob.Instance.GetFarthestEnemies();
                    if (farthestEnemies.Length == 0 || farthestEnemies[0] == null)
                    {
                        return GetDirection(DIRECTION.MOUSE_POSITION);
                    }
                    Vector3 directionToFarthestEnemy = farthestEnemies[0].position - GetUserTransform().position;
                    return directionToFarthestEnemy.normalized;
                case DIRECTION.FORWARD:
                    return GetUserTransform().forward;
                case DIRECTION.BACKWARD:
                    return -GetUserTransform().forward;
                case DIRECTION.LEFT:
                    return -GetUserTransform().right;
                case DIRECTION.RIGHT:
                    return GetUserTransform().right;
                case DIRECTION.CUSTOM:
                    return GetUserTransform().forward;
                case DIRECTION.MOUSE_POSITION:
                    Vector3 mousePosition = GetMousePosition();
                    Vector3 directionToMouse = mousePosition - GetUserTransform().position;

                    return directionToMouse.normalized;
                case DIRECTION.RANDOM_AROUND_PLAYER:
                    Vector3 randomDirection = Random.insideUnitSphere * 5f; // Random point in a sphere with radius 5
                    randomDirection.y = 0; // Keep it on the same plane
                    return randomDirection.normalized;
                default:
                    return Vector3.zero;
            }
        }

        private Vector3 GetMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (_groundPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return GetUserTransform().position + GetUserTransform().forward * 10f; // Default direction
        }

        protected Transform GetUserTransform()
        {
            return GetUser().transform;
        }

        private List<Stat> _tempStatsToReceivers;
        protected void ApplyStatsToReceivers(GameObject target, bool searchChildren = false)
        {
            IStatReceiver[] statReceivers;
            if (searchChildren)
                statReceivers = target.GetComponentsInChildren<IStatReceiver>(searchChildren);
            else
                statReceivers = target.GetComponents<IStatReceiver>();

            if (_tempStatsToReceivers == null)
                _tempStatsToReceivers = new List<Stat>();
            else
                _tempStatsToReceivers.Clear();


            for (int l = 0; l < statReceivers.Length; l++)
            {
                if (statReceivers[l] == null) continue;

                IStatReceiver statReceiver = statReceivers[l];

                if (statReceiver == null) continue;

                // if(statReceiver is MonoBehaviour mono)
                // {
                //    Debug.Log($"StatReceiver: {mono.GetType().Name} on {mono.gameObject.name}");
                // }

                // Skip hit detectors, due to ApplyStats does it
                if (statReceiver is IHitDetector) continue;

                if (statReceiver.GetStats() == null || statReceiver.GetStats()._stats == null) continue;

                for (int i = 0; i < statReceiver.GetStats()._stats.Count; i++)
                {
                    Stat stat = statReceiver.GetStats()._stats[i];

                    // Due to stat receiver there can be null stats.
                    if (stat == null) continue;

                    STAT_TYPE statType = stat.GetTags()[0];
                    if (_baseStats.TryGetStat(statType, out Stat baseStat))
                    {
                        _tempStatsToReceivers.Add(baseStat);
                    }
                }

                for (int i = 0; i < _tempStatsToReceivers.Count; i++)
                {
                    Stat stat = _tempStatsToReceivers[i];

                    statReceiver.SetStat(stat);
                }

                _tempStatsToReceivers.Clear();
            }
        }

        protected void DoAreaDamageWithoutHitDetection(IDamageDealer dealer, Vector3 position, Collider[] hitsOnceAlloc, LayerMask layer, float radius)
        {
            //MarkHelper.DrawSphereTimed(GetUserTransform().position, radius, 1, Color.white);
            int count = Physics.OverlapSphereNonAlloc(position, radius, hitsOnceAlloc, layer);
            for (int i = 0; i < count; i++)
            {
                IOwner owner = hitsOnceAlloc[i].GetComponent<IOwner>();

                owner = owner.GetRootOwner();
                IDamageReceiver damageReceiver = owner.GetGameObject().GetComponent<IDamageReceiver>();
                if (damageReceiver != null)
                {
                    DamageCalculator.CalculateDamage(dealer, damageReceiver);
                }
            }
        }

        public Vector3 GetCircleOffset(int index, int pointAmount, float radius)
        {
            float θ = (2 * Mathf.PI / pointAmount) * index;
            return new Vector3(Mathf.Cos(θ), 0, Mathf.Sin(θ)) * radius;
        }
        // public Vector3 GetShootPattern(int index, int pointAmount, SHOOTING_PATTERN pattern)
        // {
        //     Vector3 projectileDirection = GetDirection(_direction);
        //     Vector3 baseDirection = GetDirection(_direction);
        //     const float _coneAngle = 45f; // Example cone angle
        //     switch (pattern)
        //     {
        //         case SHOOTING_PATTERN.CONE:
        //             float angleStep = _coneAngle / (pointAmount - 1);
        //             float offsetAngle = -_coneAngle / 2 + angleStep * index;
        //             projectileDirection = Quaternion.Euler(0, offsetAngle, 0) * baseDirection;
        //             break;
        //         case SHOOTING_PATTERN.CIRCLE:
        //             float circleAngle = 360f / pointAmount;
        //             float circleOffset = circleAngle * index;
        //             projectileDirection = Quaternion.Euler(0, circleOffset, 0) * baseDirection;
        //             break;
        //     }

        //     return projectileDirection;
        // }

        public int GetProjectileAmount()
        {
            if (_baseStats == null) return 1;
            if (_baseStats.GetStat(STAT_TYPE.PROJECTILE_COUNT) == null) return 1;
            return _baseStats.GetStat(STAT_TYPE.PROJECTILE_COUNT).GetValueInt();
        }
        public void EndTheSkill()
        {
            ManagerSkills.Instance.EndTheSkill(this);
        }

        public bool IsRecastable()
        {
            return this is IRecastSkill; // && _ownerType == OWNER_TYPE.PLAYER;
        }

        public SimpleDamage CreateSimpleDamage(float damage = 0f)
        {
            SimpleDamage simpleDamage = new SimpleDamage(
                damage,
                DAMAGE_TYPE.PHYSICAL,
                DamageSourceHelper.GetSourceFromOwner(_ownerType));
            return simpleDamage;
        }
        #region PatternSystem
        [SerializeField]
        protected class SpawnPatternParams
        {
            public Vector3 SpawnOffset;   // z = forward distance, y = height, x ignored by default
            public SHOOTING_PATTERN Pattern;
            public float ConeAngle;
            [ShowIf(nameof(Pattern), SHOOTING_PATTERN.BURST)] public float BurstDelay;
            [ToggleLeft] public bool FollowUser;
        }

        [SerializeField] private bool _enableSpawnPattern;
        [SerializeField, ShowIf(nameof(_enableSpawnPattern)), BoxGroup("Spawn Pattern")]
        protected SpawnPatternParams SpawnPattern;

        // Abilities override this to feed their own fields (keeps old names/locations intact)
        protected virtual SpawnPatternParams GetSpawnPatternParams()
        {
            return SpawnPattern;

        }

        private List<Vector3> _dirScratch;
        private List<GameObject> _spawnedTracked;

        private List<int> _spawnedIndices; // pattern-indeksi / slotti kohden
        private int _followTotalCount;     // viimeisin kokonaismäärä (cone-asteikkoa varten)

        private const string ID_BURST_DELAY = "BurstDelay";

        // Calls spawnAtIndex for each projectile with (index, direction, spawnPosition)
        protected void ForEachSpawnPoint(int count, Vector3 baseDirection, System.Action<int, Vector3, Vector3> spawnAtIndex)
        {
            SpawnPatternParams p = GetSpawnPatternParams();

            _dirScratch.Clear();
            GeneratePatternDirections(count, baseDirection, p.Pattern, p.ConeAngle, _dirScratch);

            if (p.Pattern == SHOOTING_PATTERN.BURST && p.BurstDelay > 0f)
            {
                for (int i = 0; i < _dirScratch.Count; i++)
                {
                    int idx = i; // avoid closure capture bug
                    Vector3 dir = _dirScratch[idx];
                    ActionScheduler.RunAfterDelay(p.BurstDelay * idx, () =>
                    {
                        Vector3 pos = ComputeSpawnPosition(GetUserTransform().position, dir, p.SpawnOffset);
                        spawnAtIndex(idx, dir, pos);
                    },ID_BURST_DELAY + GetInstanceID());
                }
            }
            else
            {
                for (int i = 0; i < _dirScratch.Count; i++)
                {
                    Vector3 dir = _dirScratch[i];
                    Vector3 pos = ComputeSpawnPosition(GetUserTransform().position, dir, p.SpawnOffset);
                    spawnAtIndex(i, dir, pos);
                }
            }
        }

        protected void SpawnObject(GameObject prefab)
        {
            SpawnObject(prefab, null);
        }

        /// <summary>
        /// Spawns a game object from the prefab.
        /// Init(GameObject, Count, Position, Direction)
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="init"></param>
        protected void SpawnObject(
        GameObject prefab,
        System.Action<GameObject, int, Vector3, Vector3> init)
        {
            if (prefab == null)
            {
                Debug.LogError("SpawnObject prefab is null in " + GetType().Name);
                return;
            }
            if (_baseStats == null)
            {
                Debug.LogError("Base stats is null in ability " + GetSkillName());
                return;
            }

            int count = GetProjectileAmount();
            Vector3 baseDir = GetDirection(_direction);

            ForEachSpawnPoint(count, baseDir, (index, direction, pos) =>
            {
                int slot = _spawnedTracked.Count;
                _spawnedTracked.Add(null);
                _spawnedIndices.Add(index);
                _followTotalCount = Mathf.Max(_followTotalCount, count);

                GameObject go = ManagerPrefabPooler.Instance.GetFromPool(prefab, (returned) =>
                {
                    if (_spawnedTracked != null && slot < _spawnedTracked.Count && _spawnedTracked[slot] == returned)
                        _spawnedTracked[slot] = null;
                });

                if (go == null)
                {
                    Debug.LogError("Pool returned null for " + prefab.name);
                    return;
                }

                _spawnedTracked[slot] = go;

                IOwner ownerComp = go.GetComponent<IOwner>();
                if (ownerComp != null) ownerComp.SetOwner(GetOwnerType());

                go.transform.position = pos;
                if (direction.sqrMagnitude > 0.0001f) go.transform.forward = direction;

                init?.Invoke(go, index, pos, direction);
            });
        }

        protected void ReturnAllSpawned()
        {
            //Debug.Log($"@@@@@Returning all spawned objects for {GetType().Name}, count: {_spawnedTracked.Count}");
            if (_spawnedTracked == null || _spawnedTracked.Count == 0) return;

            for (int i = 0; i < _spawnedTracked.Count; i++)
            {
                GameObject go = _spawnedTracked[i];
                if (go == null) continue;
                if (go.activeSelf) ManagerPrefabPooler.Instance.ReturnToPool(go);
                _spawnedTracked[i] = null;
            }
            _spawnedTracked.Clear();
            _spawnedIndices?.Clear();
            _followTotalCount = 0;
        }

        protected static void GeneratePatternDirections(int count, Vector3 baseDirection,
            SHOOTING_PATTERN pattern, float coneAngle, List<Vector3> outDirs)
        {
            outDirs.Clear();

            if (count <= 1)
            {
                outDirs.Add(baseDirection.normalized);
                return;
            }

            switch (pattern)
            {
                case SHOOTING_PATTERN.CONE:
                    {
                        float step = coneAngle / (count - 1);
                        float start = -coneAngle * 0.5f;
                        for (int i = 0; i < count; i++)
                        {
                            float yaw = start + step * i;
                            outDirs.Add(Quaternion.Euler(0f, yaw, 0f) * baseDirection);
                        }
                        break;
                    }
                case SHOOTING_PATTERN.CIRCLE:
                    {
                        float step = 360f / count;
                        for (int i = 0; i < count; i++)
                        {
                            float yaw = step * i;
                            outDirs.Add(Quaternion.Euler(0f, yaw, 0f) * baseDirection);
                        }
                        break;
                    }
                case SHOOTING_PATTERN.BURST:
                default:
                    {
                        for (int i = 0; i < count; i++)
                            outDirs.Add(baseDirection.normalized);
                        break;
                    }
            }
        }

        protected static Vector3 GetPatternDirectionAt(
    int index, int count, Vector3 baseDirection, SHOOTING_PATTERN pattern, float coneAngle)
        {
            baseDirection = baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : Vector3.forward;

            if (count <= 1 || pattern == SHOOTING_PATTERN.BURST)
                return baseDirection;

            switch (pattern)
            {
                case SHOOTING_PATTERN.CONE:
                    {
                        float step = coneAngle / (count - 1);
                        float yaw = -coneAngle * 0.5f + step * index;
                        return Quaternion.Euler(0f, yaw, 0f) * baseDirection;
                    }
                case SHOOTING_PATTERN.CIRCLE:
                    {
                        float step = 360f / count;
                        return Quaternion.Euler(0f, step * index, 0f) * baseDirection;
                    }
                default:
                    return baseDirection;
            }
        }

        protected void UpdateSpawnPatternFollow()
        {
            if (!_enableSpawnPattern || SpawnPattern == null || !SpawnPattern.FollowUser)
                return;

            if (_spawnedTracked == null || _spawnedTracked.Count == 0)
                return;

            if (_followTotalCount <= 0)
                _followTotalCount = Mathf.Max(_followTotalCount, _spawnedTracked.Count);

            Vector3 userPos = GetUserTransform().position;
            Vector3 baseDir = GetDirection(_direction);
            SHOOTING_PATTERN pattern = SpawnPattern.Pattern;
            float cone = SpawnPattern.ConeAngle;
            Vector3 offset = SpawnPattern.SpawnOffset;

            for (int slot = 0; slot < _spawnedTracked.Count; slot++)
            {
                GameObject go = _spawnedTracked[slot];
                if (go == null) continue;

                int idx = (slot < _spawnedIndices.Count) ? _spawnedIndices[slot] : slot;

                Vector3 dir = GetPatternDirectionAt(idx, _followTotalCount, baseDir, pattern, cone);
                Vector3 pos = ComputeSpawnPosition(userPos, dir, offset);

                go.transform.position = pos;
                if (dir.sqrMagnitude > 0.0001f) go.transform.forward = dir;
            }
        }

        protected static Vector3 ComputeSpawnPosition(Vector3 userPos, Vector3 direction, Vector3 spawnOffset)
        {
            // Keeps your current semantics: z => forward distance, y => height
            Vector3 pos = userPos + direction.normalized * spawnOffset.z;
            pos.y = userPos.y + spawnOffset.y;
            // If you ever want offset.x as "right" offset, you can add:
            // pos += Vector3.Cross(Vector3.up, direction.normalized).normalized * spawnOffset.x;
            return pos;
        }

        #endregion PatternSystem

        #region Getters and Setters
        public GameObject GetUser()
        {
            return _user;
        }

        public void SetUser(GameObject user)
        {
            _user = user;
        }

        public int GetSlot()
        {
            return _skillSlot;
        }

        public void SetSlot(int slot)
        {
            _skillSlot = slot;
        }

        public GameObject GetGameObject()
        {
            return _user;
        }

        public IOwner GetRootOwner()
        {
            return this;
        }

        public void SetOwner(OWNER_TYPE ownerType)
        {
            _ownerType = ownerType;
        }
        public OWNER_TYPE GetOwnerType()
        {
            return _ownerType;
        }

        public void SetDuration(float duration)
        {
            _baseStats.GetStat(STAT_TYPE.DURATION).SetBaseValue(duration);
        }

        public float GetDuration()
        {
            return _baseStats.GetStat(STAT_TYPE.DURATION).GetValue();
        }

        /// <summary>
        /// Returns the charge time of the ability. Normally used if skill has IChargeable interface.
        /// </summary>
        public float GetChargeTime()
        {
            return _baseStats.GetValueOfStat(STAT_TYPE.CHARGE_TIME);
        }

        public void SetCooldown(float cooldown)
        {
            _baseStats.GetStat(STAT_TYPE.COOLDOWN).SetBaseValue(cooldown);
        }

        public float GetCooldown()
        {
            float cooldown = _baseStats.GetStat(STAT_TYPE.COOLDOWN).GetValue();
            float cooldown_reduction_percent = _baseStats.GetValueOfStat(STAT_TYPE.COOLDOWN_REDUCTION_PERCENT);
            if (_ownerType == OWNER_TYPE.PLAYER)
            {
                float playerCastSpeed = Player.Instance.GetStatValue(STAT_TYPE.CAST_SPEED);

                if (playerCastSpeed > 0) cooldown /= (1 + playerCastSpeed / 100f);

            }

            if (cooldown_reduction_percent > 0)
            {
                cooldown *= (1 - cooldown_reduction_percent / 100f);
            }

            if (cooldown < CONSTANTS.SKILL_LOWEST_COOLDOWN)
            {
                cooldown = CONSTANTS.SKILL_LOWEST_COOLDOWN;
            }

            return cooldown;
        }

        public void SetCurrentDuration(float currentDuration)
        {
            _currentDuration = currentDuration;
        }

        public float GetCurrentDuration()
        {
            return _currentDuration;
        }

        public void SetCurrentCooldown(float currentCooldown)
        {
            _currentCooldown = currentCooldown;
        }

        public float GetCurrentCooldown()
        {
            return _currentCooldown;
        }

        public void AddModifier(Modifier modifier)
        {
            if (_baseStats == null)
            {
                Debug.LogWarning("Base stats is null, cannot add modifier.");
                return;
            }

            //Debug.Log("ABILITY: Adding modifier: " + modifier.GetTarget() + " to ability: " + this.GetType().Name);
            _baseStats.AddModifier(modifier);
        }

        public void ClearModifiers()
        {
            if (_baseStats == null)
            {
                Debug.LogWarning("Base stats is null, cannot clear modifiers.");
                return;
            }

            _baseStats.ClearModifiers();
        }

        public SKILL_NAME GetSkillName()
        {
            return _skillName;
        }

        public void SetSkillName(SKILL_NAME skillName)
        {
            _skillName = skillName;
        }

        public int GetInstanceID()
        {
            return _instanceID;
        }

        public void SetInstanceID(int instanceID)
        {
            _instanceID = instanceID;
        }

        public ISkill GetRootSkill()
        {
            return _rootSkill;
        }

        public void SetRootSkill(ISkill rootSkill)
        {
            _rootSkill = rootSkill;
        }

        public bool IsManipulated()
        {
            return false;
        }

        public IOwner GetManipulatedOwner()
        {
            throw new System.NotImplementedException();
        }

        public void SetManipulatedOwner(IOwner owner)
        {
            throw new System.NotImplementedException();
        }

        #endregion Getters and Setters

        #region Editor Methods
        [ShowIf("@_baseStats == null")]
        [Button("Add Default Stats")]
        [PropertyOrder(-1)]
        [ColorPalette("Green")]
        public void AddStats()
        {
            if (_baseStats == null)
            {
                _baseStats = new StatList();
                _baseStats.Initialize(new List<STAT_TYPE>
                {
                    STAT_TYPE.COOLDOWN,
                    STAT_TYPE.DURATION,
                    STAT_TYPE.DAMAGE,
                });
            }
        }

        #endregion Editor Methods   
    }


}