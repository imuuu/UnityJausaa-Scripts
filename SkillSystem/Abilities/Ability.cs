using System.Collections.Generic;
using Game.HitDetectorSystem;
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
        public abstract void EndSkill();
        public abstract void UpdateSkill();
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

                if (statReceiver.GetStats() == null || statReceiver.GetStats()._stats == null) continue;

                for (int i = 0; i < statReceiver.GetStats()._stats.Count; i++)
                {
                    Stat stat = statReceiver.GetStats()._stats[i];
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

        protected void DoAreaDamage(IDamageDealer dealer, Vector3 position, Collider[] hitsOnceAlloc, LayerMask layer, float radius)
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
        public Vector3 GetShootPattern(int index, int pointAmount, SHOOTING_PATTERN pattern)
        {
            Vector3 projectileDirection = GetDirection(_direction);
            Vector3 baseDirection = GetDirection(_direction);
            const float _coneAngle = 45f; // Example cone angle
            switch (pattern)
            {
                case SHOOTING_PATTERN.CONE:
                    float angleStep = _coneAngle / (pointAmount - 1);
                    float offsetAngle = -_coneAngle / 2 + angleStep * index;
                    projectileDirection = Quaternion.Euler(0, offsetAngle, 0) * baseDirection;
                    break;
                case SHOOTING_PATTERN.CIRCLE:
                    float circleAngle = 360f / pointAmount;
                    float circleOffset = circleAngle * index;
                    projectileDirection = Quaternion.Euler(0, circleOffset, 0) * baseDirection;
                    break;
            }

            return projectileDirection;
        }

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
            return this is IRecastSkill && _ownerType == OWNER_TYPE.PLAYER;
        }

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

        public void SetCooldown(float cooldown)
        {
            _baseStats.GetStat(STAT_TYPE.COOLDOWN).SetBaseValue(cooldown);
        }

        public float GetCooldown()
        {
            float cooldown = _baseStats.GetStat(STAT_TYPE.COOLDOWN).GetValue();
            if (_ownerType == OWNER_TYPE.PLAYER)
            {
                float playerCastSpeed = Player.Instance.GetStatValue(STAT_TYPE.CAST_SPEED);

                if (playerCastSpeed > 0) cooldown /= (1 + playerCastSpeed / 100f);

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