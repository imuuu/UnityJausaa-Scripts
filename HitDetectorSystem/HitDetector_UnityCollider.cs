using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    public class HitDetector_UnityCollider : HitDetector
    {
        [Header("Hit Mode Settings")]
        [Tooltip("Time (in seconds) between repeated hits while an object stays in this trigger. 0 = only‐once.")]
        [SerializeField] protected float _hitInterval = 1f;

        [Tooltip("If true, each overlapped object only gets hit once (ignores _hitInterval).")]
        [SerializeField] protected bool _singleHit = false;

        [SerializeField] protected bool _enableVelocityCheck = false;

        [SerializeField, ShowIf(nameof(_enableVelocityCheck))] protected float _velocityThreshold = 3f;

        private readonly HashSet<GameObject> _insideObjects = new();
        private readonly Dictionary<GameObject, float> _overlapTimers = new();

        private readonly List<GameObject> _toRemove = new();

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _overlapTimers.Clear();
            _insideObjects.Clear();
        }

        private Rigidbody GetRigidbody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
            return _rigidbody;
        }

        public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            _toRemove.Clear();
            foreach (var kv in _overlapTimers)
            {
                var go = kv.Key;
                if (go == null || !go.activeInHierarchy)
                    _toRemove.Add(go);
            }
            foreach (var dead in _toRemove)
            {
                _insideObjects.Remove(dead);
                _overlapTimers.Remove(dead);
            }

            if (!_singleHit)
            {
                foreach (var go in _insideObjects)
                {
                    if (_overlapTimers.ContainsKey(go))
                        _overlapTimers[go] -= Time.deltaTime;
                }
            }

            foreach (var go in _insideObjects)
            {
                if (!_overlapTimers.TryGetValue(go, out float timeLeft))
                    timeLeft = _overlapTimers[go] = 0f;

                if (_singleHit || timeLeft <= 0f)
                {
                    if (_enableVelocityCheck && GetRigidbody().linearVelocity.magnitude < _velocityThreshold)
                    {
                        continue;
                    }

                    hitInfo = _managerHitDectors.GetNewHitCollisionInfo(go);
                    hitInfo.SetCollisionPoint(go.transform.position);
                    hitInfo.SetDirection((go.transform.position - transform.position).normalized);

                    if (_singleHit)
                    {
                        _insideObjects.Remove(go);
                        _overlapTimers.Remove(go);
                    }
                    else
                    {
                        _overlapTimers[go] = _hitInterval;
                    }

                    //Debug.Log($"HitDetector_UnityCollider: Hit on {go.name}, next in {_hitInterval}s velocity={_rigidbody?.linearVelocity.magnitude}");
                    return true;
                }
            }

            hitInfo = null;
            return false;
        }

        public void OnTriggerEnter(Collider other)
        {
            //Debug.Log($"HitDetector_UnityCollider: {other.gameObject.name} entered trigger");
            if (!other.gameObject.TryGetComponent<IOwner>(out var owner)) return;
            if (owner.GetRootOwner().GetOwnerType() == GetOwner().GetRootOwner().GetOwnerType()) return;

            var go = other.gameObject;
            // Add to inside set
            if (_insideObjects.Add(go))
            {
                // If no existing timer, schedule immediate hit
                if (!_overlapTimers.ContainsKey(go))
                    _overlapTimers[go] = 0f;

                //Debug.Log($"HitDetector_UnityCollider: {go.name} entered; timer={_overlapTimers[go]}");

                ManagerHitDectors.Instance.CallPerformHitCheck(this);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            var go = other.gameObject;
            _insideObjects.Remove(go);
        }

        // Mirror trigger logic for collisions if you need
        private void OnCollisionEnter(Collision collision) => OnTriggerEnter(collision.collider);
        private void OnCollisionExit(Collision collision) => OnTriggerExit(collision.collider);
    }
}
