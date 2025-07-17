using UnityEngine;
using Game.StatSystem;
using System;

namespace Game.HitDetectorSystem
{
    /// <summary>
    /// A simple line-based hit detector implementing IHitDetector (non-MonoBehaviour).
    /// Can optionally always report a hit or perform a single Physics.Raycast.
    /// </summary>
    public class HitDetector_Simple : IHitDetector
    {
        private readonly GameObject _gameObject;
        private readonly IOwner _owner;
        private readonly Vector3 _origin;
        private readonly Vector3 _direction;
        private readonly float _range;
        private readonly bool _alwaysHit;
        private readonly float _hitHistoryTime;
        private float _hitHistoryTimer;

        // pierced tracking
        private readonly int _maxPiercing;
        private readonly float _piercingChance;
        private int _remainingPiercing;
        private int _totalPierces;

        // state flags
        private bool _isEnabled = true;
        private bool _manualDestroy = false;
        private bool _beginDestroyed = false;
        private bool _finalHit = false;

        public HitDetector_Simple(
            GameObject gameObject,
            IOwner owner,
            Vector3 origin,
            Vector3 direction,
            float range,
            bool alwaysHit = false,
            float hitHistoryTime = 0f,
            int maxPiercing = 0,
            float piercingChance = 0f)
        {
            _gameObject = gameObject;
            _owner = owner;
            _origin = origin;
            _direction = direction.normalized;
            _range = range;

            _alwaysHit = alwaysHit;
            _hitHistoryTime = hitHistoryTime;
            _hitHistoryTimer = hitHistoryTime;

            _maxPiercing = maxPiercing;
            _piercingChance = piercingChance;
            _remainingPiercing = maxPiercing < 0 ? int.MaxValue : maxPiercing;
        }

        public bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            // respect history timer
            if (!ShouldPerformHitCheck(Time.deltaTime))
            {
                hitInfo = default;
                return false;
            }

            if (_alwaysHit)
            {
                hitInfo = default;
                return true;
            }

            // raycast check
            if (Physics.Raycast(_origin, _direction, out var hit, _range, ManagerHitDectors.GetHitLayerMask()))
            {
                var target = hit.collider.gameObject;
                hitInfo = ManagerHitDectors.Instance.GetNewHitCollisionInfo(target);
                hitInfo.SetCollisionPoint(hit.point);
                hitInfo.SetDirection(_direction);
                return true;
            }

            hitInfo = default;
            return false;
        }

        public GameObject GetGameObject() => _gameObject;
        public IOwner GetOwner() => _owner;
        public int GetID() => this.GetHashCode();

        public void OnHit(HitCollisionInfo hitInfo) { /* no-op */ }
        public void OnFinalHit(HitCollisionInfo hitInfo) { /* no-op */ }
        public void OnPierceHit(HitCollisionInfo hitInfo) { /* no-op */ }

        public bool IsBeginDestroyed() => _beginDestroyed;
        public void SetBeginDestroyed(bool value) => _beginDestroyed = value;

        public bool IsFinalHit() => _finalHit;
        public void SetFinalHit(bool value) => _finalHit = value;

        public bool IsManual => false;
        public bool IsManualDestroy() => _manualDestroy;
        public void SetManualDestroy(bool value) => _manualDestroy = value;
        public void TriggerManualHitCheck()
        {
            if (PerformHitCheck(out var info))
                OnHit(info);
        }

        public int MaxPiercing => _maxPiercing;
        public float PiercingChance => _piercingChance;
        public int RemainingPiercing => _remainingPiercing;
        public int TotalPierces { get => _totalPierces; set => _totalPierces = value; }

        public bool DecrementPiercing()
        {
            if (_maxPiercing < 0)
                return false;
            if (_remainingPiercing > 0)
            {
                _remainingPiercing--;
                _totalPierces++;
                return true;
            }
            return false;
        }

        public float GetHitHistoryTimer() => _hitHistoryTime;

        public bool ShouldPerformHitCheck(float deltaTime)
        {
            if (_hitHistoryTime <= 0f)
                return true;
            _hitHistoryTimer -= deltaTime;
            if (_hitHistoryTimer <= 0f)
            {
                _hitHistoryTimer = _hitHistoryTime;
                return true;
            }
            return false;
        }

        public bool IsEnabled() => _isEnabled;
        public void SetEnable(bool enable) => _isEnabled = enable;

#if RAYFIRE
        public void SetRayFireTriggerEnable(bool enable) { /* no-op */ }
#endif
    }
}
