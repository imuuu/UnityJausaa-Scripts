using System;
using System.Collections.Generic;
using Game.StatSystem;
using RayFire;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.HitDetectorSystem
{
    [DefaultExecutionOrder(100)]
    public abstract class HitDetector : MonoBehaviour, IHitDetector, IStatReceiver
    {
        protected ManagerHitDectors _managerHitDectors => ManagerHitDectors.Instance;

        private bool _isEnabled = true;
        private IOwner _owner;

        [SerializeField] protected bool _manualTrigger = false;
        public bool IsManual => _manualTrigger;
        [Tooltip("If true, after hitDetector is return to pool or destroyed, will be canceled.")]
        [SerializeField] protected bool _manualDestroy = false;
        [InfoBox("Time when hit history is removed. Set to 0 to disable(can hit every frame).")]
        [SerializeField, MinValue(0)] private float _hitHistoryTime = 0.5f;
        [Tooltip("If >= 0, damage will be checked every this seconds. For example, if set to 0.5, it will deal hit every 0.5 seconds")]
        [SerializeField, MinValue(-1f)] private float hitCheckInterval = -1f;
        private float _hitCheckTimer;

        [Space(8)]
        [SerializeField, ToggleLeft, PropertyOrder(7), BoxGroup("Rayfire"), PropertySpace(5, 5)]
        private bool _triggerRayFire = false;
        private MonoBehaviour _rayFireComponent;

        [Space(8)]
        [SerializeField, ToggleLeft, PropertyOrder(10)]
        private bool _enableEvents;
        [SerializeField, ShowIf("_enableEvents"), PropertyOrder(10)]
        private UnityEvent _onHitEvent;

        // --- Piercing Settings ---
        protected bool _enablePiercing = true;
        [Header("Piercing Settings")]
        [Tooltip("Set to -1 for infinite piercing. For example, an arrow might have 1, while lightning could be -1.")]
        [SerializeField, ShowIf("_enablePiercing")] protected int _maxPiercing = 1;
        [Tooltip("Chance to pierce through an object. Set to -1 for not enabled. Pierce chance will be applied if false, the pierce count is reduced by 1.")]
        [SerializeField, ShowIf("_enablePiercing")][Range(-1, 100)] protected float _piercingChance = -1f;
        // [Tooltip("If true, every hit (even on objects with an owner) decrements the piercing count. Otherwise, only hits with no owner count.")]
        // [SerializeField] protected bool _decrementPiercingOnHitWithOwner = true;
        protected int _remainingPiercing;

        // --- IHitDetector Piercing properties ---
        public int MaxPiercing => _maxPiercing;
        public float PiercingChance => _piercingChance;
        public int RemainingPiercing => _remainingPiercing;

        private int _totalPiercing = 0;
        public int TotalPierces { get { return _totalPiercing; } set { _totalPiercing = value; } }
        private IOnPiercing[] _onPiercingComponents;

        private List<IOnHit> _onHitComponents = new();

        private StatList _statList;

        private bool _isBeginDestroyed = false;

        public event Action<HitCollisionInfo> OnHitEvent;
        public event Action<HitCollisionInfo> OnFinalHitEvent;

        protected virtual void Awake()
        {
            _owner = GetComponent<IOwner>();
            _onPiercingComponents = GetComponents<IOnPiercing>();
        }

        protected virtual void OnEnable()
        {
            _totalPiercing = 0; // Reset total piercing count on enable
            _remainingPiercing = _maxPiercing <= 0 ? 99999 : _maxPiercing;
            _hitCheckTimer = hitCheckInterval;
            if (!_manualTrigger)
            {
                if (_managerHitDectors == null)
                {
                    ActionScheduler.RunNextFrame(() =>
                    {
                        _managerHitDectors.RegisterDetector(this);
                    });
                    return;
                }
            }
            _managerHitDectors.RegisterDetector(this);
        }

        protected virtual void OnDisable()
        {
            if (ManagerHitDectors.Instance != null)
            {
                _managerHitDectors.UnregisterDetector(this);
            }
        }

        public void RegisterOnHit(IOnHit onHitComponent)
        {
            if (!_onHitComponents.Contains(onHitComponent))
            {
                _onHitComponents.Add(onHitComponent);
            }
        }

        public void UnregisterOnHit(IOnHit onHitComponent)
        {
            if (_onHitComponents.Contains(onHitComponent))
            {
                _onHitComponents.Remove(onHitComponent);
            }
        }

        public void DestroyThisHitDetectorScript()
        {
            ManagerHitDectors.Instance.DestroyDetectorScript(this);
        }

        public abstract bool PerformHitCheck(out HitCollisionInfo hitCollisionInfo);

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public IOwner GetOwner()
        {
            return _owner;
        }

        public bool IsManualDestroy()
        {
            return _manualDestroy;
        }

        public void SetManualDestroy(bool value)
        {
            _manualDestroy = value;
        }

        public virtual void OnHit(HitCollisionInfo hitInfo)
        {
            foreach (IOnHit onHitComponent in _onHitComponents)
            {
                onHitComponent.OnHit(hitInfo);
            }

            if (_enableEvents)
                _onHitEvent?.Invoke();

            if (_triggerRayFire)
                TriggerRayFireComponent();

            if (OnHitEvent != null)
            {
                OnHitEvent.Invoke(hitInfo);
            }
        }

        public void SetRayFireTriggerEnable(bool enable)
        {
            _triggerRayFire = enable;
        }

        private void TriggerRayFireComponent()
        {
#if RAYFIRE
            if (_rayFireComponent == null)
            {
                _rayFireComponent = GetComponent<RayfireGun>();
                if (_rayFireComponent == null)
                    _rayFireComponent = GetComponent<RayfireBomb>();
            }

            if (_rayFireComponent == null)
                return;

            if (_rayFireComponent is RayfireGun gun)
            {
                gun.Shoot();
            }
            else if (_rayFireComponent is RayfireBomb bomb)
            {
                bomb.Explode(0);
            }
#endif
        }

        /// <summary>
        /// Call this to manually trigger the hit check (for detectors set to manual).
        /// </summary>
        public void TriggerManualHitCheck()
        {
            if (PerformHitCheck(out HitCollisionInfo hitCollisionInfo))
            {
                OnHit(hitCollisionInfo);
            }
        }

        /// <summary>
        /// Decrements the piercing count. For infinite piercing (_maxPiercing == -1) nothing is done.
        /// If the hit object has an owner and _decrementPiercingOnHitWithOwner is false, the count is not reduced.
        /// return true if the piercing count was decremented.
        /// </summary>
        public bool DecrementPiercing()
        {
            if (_maxPiercing == -1)
                return false; // infinite piercing

            _remainingPiercing--;
            return true;
        }

        public void OnPierceHit(HitCollisionInfo hitInfo)
        {
            //Debug.Log("OnPierceHit");
            foreach (var onPiercingComponent in _onPiercingComponents)
            {
                if (hitInfo.IsEnemy()) onPiercingComponent.OnPiercing(hitInfo);
            }
        }

        public bool HasPiercingComponents()
        {
            return _onPiercingComponents.Length > 0;
        }

        public float GetHitHistoryTimer()
        {
            return _hitHistoryTime;
        }

        /// <summary>
        /// Returns true exactly when the interval has elapsed (or if interval<0).
        /// Resets the timer *only* at the moment you get true.
        /// </summary>
        public bool ShouldPerformHitCheck(float deltaTime)
        {
            if (hitCheckInterval < 0f)
                return true;

            _hitCheckTimer -= deltaTime;
            if (_hitCheckTimer <= 0f)
            {
                _hitCheckTimer = hitCheckInterval;
                return true;
            }
            return false;
        }

        // public STAT_TYPE GetTarget()
        // {
        //     return STAT_TYPE.PIERCE_CHANCE;
        // }

        public void SetStat(Stat stat)
        {
            if (_statList == null)
            {
                _statList = new StatList();
            }

            _statList.SetStat(stat);

            if (stat.GetTags().Contains(STAT_TYPE.PIERCE_CHANCE))
            {
                _piercingChance = stat.GetValue();
            }
        }

        public void SetStats(StatList statList)
        {
            _statList = statList;

            if(_statList.TryGetStat(STAT_TYPE.PIERCE_CHANCE, out Stat stat))
            {
                _piercingChance = stat.GetValue();
            }

        }

        public StatList GetStats()
        {
            return _statList;
        }

        public bool HasStat(STAT_TYPE type)
        {
            if (_statList == null)
                return false;

            return _statList.HasStat(type);
        }


        public bool IsBeginDestroyed()
        {
            return _isBeginDestroyed;
        }

        public void SetBeginDestroyed(bool value)
        {
            _isBeginDestroyed = value;
        }

        public int GetID()
        {
            return this.GetInstanceID();
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        public void SetEnable(bool enable)
        {
            _isEnabled = enable;
        }

        public bool IsFinalHit()
        {
            return true;
        }

        public void SetFinalHit(bool value)
        {
            throw new System.NotImplementedException();
        }

        public virtual void OnFinalHit(HitCollisionInfo hitInfo)
        {
            if (OnFinalHitEvent != null)
            {
                OnFinalHitEvent.Invoke(hitInfo);
            }
        }

       

    }
}
