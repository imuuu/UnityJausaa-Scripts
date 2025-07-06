using UnityEngine;
using Game.StatSystem;
using System.Collections.Generic;

namespace Game.HitDetectorSystem
{
    public class HitDetector_AreaDamage : IMultiHitDetector
    {
        // --- configuration fields ---
        private readonly GameObject _attachedObject;
        private readonly IOwner _owner;
        private readonly float _radius;
        private readonly float _hitHistoryTime;
        private readonly StatList _statList = new StatList();

        // --- runtime state ---
        private float _hitHistoryTimer;
        private bool _isEnabled = true;
        private bool _manualDestroy = false;
        private bool _beginDestroyed = false;
        private bool _manualTrigger = false;  // always automatic
        private int _totalPierces = 0;
        private IDamageDealer _customDamageDealer = null;

        // reuse this buffer every frame to avoid GC spikes
        private readonly Collider[] _hitsBuffer = new Collider[15];
        private GameObject _ignoredObject = null;

        public HitDetector_AreaDamage(
            GameObject attachedObject,
            IOwner owner,
            float radius,
            float hitHistoryTime = 0f,
            IDamageDealer customDamageDealer = null,
            GameObject ignoredObject = null)
        {
            _attachedObject = attachedObject;
            _owner = owner;
            _radius = radius;
            _hitHistoryTime = hitHistoryTime;
            _hitHistoryTimer = hitHistoryTime;
            _customDamageDealer = customDamageDealer;
            _ignoredObject = ignoredObject;
        }

        // Single-hit API just grabs the first entry from our multi-hit list
        public bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            var all = PerformHitChecks();
            if (all.Count > 0)
            {
                hitInfo = all[0];
                return true;
            }

            hitInfo = default;
            return false;
        }

        public List<HitCollisionInfo> PerformHitChecks()
        {
            List<HitCollisionInfo> results = new ();
            Vector3 center = _attachedObject.transform.position;
            LayerMask mask = ManagerHitDectors.GetHitLayerMask();

            int count = Physics.OverlapSphereNonAlloc(center, _radius, _hitsBuffer, mask);
            for (int i = 0; i < count; i++)
            {
                Collider col = _hitsBuffer[i];

                var root = col.transform.gameObject;

                if (root == _attachedObject) continue;

                if (_ignoredObject != null && root == _ignoredObject) continue;

                var info = ManagerHitDectors.Instance.GetNewHitCollisionInfo(col.gameObject);

                info.SetCollisionPoint(center);
                info.SetDirection((col.transform.position - center).normalized);
                info.CustomDamageDealer = _customDamageDealer;

                results.Add(info);
            }

            return results;
        }

        // --- IHitDetector members (unchanged / no piercing) ---

        public GameObject GetGameObject() => _attachedObject;
        public IOwner GetOwner() => _owner;
        public int GetID() => this.GetHashCode();
        public bool IsManual => _manualTrigger;
        public bool IsEnabled() => _isEnabled;
        public void SetEnable(bool e) => _isEnabled = e;
        public bool IsManualDestroy() => _manualDestroy;
        public void SetManualDestroy(bool v) => _manualDestroy = v;
        public bool IsBeginDestroyed() => _beginDestroyed;
        public void SetBeginDestroyed(bool v) => _beginDestroyed = v;
        public void TriggerManualHitCheck() { if (PerformHitCheck(out var info)) OnHit(info); }

        public int MaxPiercing => 0;
        public float PiercingChance => 0f;
        public int RemainingPiercing => 0;
        public int TotalPierces { get => _totalPierces; set => _totalPierces = value; }
        public bool DecrementPiercing() => false;

        public float GetHitHistoryTimer() => _hitHistoryTime;
        public bool ShouldPerformHitCheck(float dt)
        {
            if (_hitHistoryTime <= 0f) return true;
            _hitHistoryTimer -= dt;
            if (_hitHistoryTimer <= 0f)
            {
                _hitHistoryTimer = _hitHistoryTime;
                return true;
            }
            return false;
        }

        public StatList GetStatList() => _statList;
        public void SetStatToList(Stat stat) => _statList.SetStat(stat);

        public void OnHit(HitCollisionInfo hitInfo)
        {
            // no-op: actual Damage is handled by ManagerHitDectors.HandleHit()
        }

        public void OnPierceHit(HitCollisionInfo hitInfo) { /* never called */ }

#if RAYFIRE
        public void SetRayFireTriggerEnable(bool e) { }


        /// <summary>
        /// always false due to this isnt ever registered to manager, also it might break logic in some cases if true
        /// </summary>
        public bool IsFinalHit()
        {
            return false;
        }

        public void SetFinalHit(bool value)
        {
            
        }

        public void OnFinalHit(HitCollisionInfo hitInfo)
        {
        }



#endif
    }
}
