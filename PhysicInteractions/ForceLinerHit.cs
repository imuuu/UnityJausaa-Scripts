using System.Collections.Generic;
using Game.MobStateSystem;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.PhysicInteractions
{
    public class ForceLinerHit : ForceInteraction, IEnabled
    {   

        [BoxGroup("General")]
        [SerializeField]
        [InfoBox("Normally false at start, set to true via PhysicHand")]
        private bool _enable = false;



        [BoxGroup("General")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [BoxGroup("Force Settings")]
        [InfoBox("The force applied to the object is calculated based on the object's velocity and mass. Also target mass is considered.")]
        [SerializeField]
        private float _forceMultiplier = 0.1f;

        [BoxGroup("Force Settings")]
        [Tooltip("Cooldown time before the object can be hit again.")]
        [SerializeField]
        private float _hitObjectsDelay = 1.5f;

        [BoxGroup("Force Settings")]
        [Tooltip("The angle of the force applied to the object related to ground.")]
        [SerializeField]
        private float _pitchAngle = 68f;

        [BoxGroup("Force Settings")]
        [Tooltip("If true, only objects with IOwner component will be affected.")]
        [SerializeField]
        private bool _onlyOwners = true;

        [BoxGroup("Around Force Settings")]
        [SerializeField]
        private bool _enableAroundForce = true;

        [BoxGroup("Around Force Settings")]
        [ShowIf("_enableAroundForce")]
        [SerializeField]
        private float _aroundForceScale = 2f;

        [BoxGroup("Around Force Settings")]
        [ShowIf("_enableAroundForce")]
        [SerializeField]
        private float _aroundForceMultiplier = 1f;

        [BoxGroup("Around Force Settings")]
        [Tooltip("If true, force objects away from this object; otherwise, use the base direction.")]
        [ShowIf("_enableAroundForce")]
        [SerializeField]
        private bool _forceAwayFromOrigin = false;

        [BoxGroup("Around Force Settings")]
        [Tooltip("Total number of hits to check around the object.")]
        [ShowIf("_enableAroundForce")]
        [SerializeField]
        private int _totalAroundHits = 10;

        [BoxGroup("Debug Settings")]
        [SerializeField]
        private bool _debugForceDirection = false;
        [BoxGroup("Debug Settings")]
        [SerializeField]
        private bool _debugDrawAroundForce = false;

        private HashSet<int> _hitObjects = new();

        [BoxGroup("Debug Settings")]
        [HideInInspector]
        private Collider[] _colliderResults;

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null) _rigidbody = GetComponentInChildren<Rigidbody>();

        }

        private void Start()
        {
            _colliderResults = new Collider[_totalAroundHits];
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(!_enable) return;

            Rigidbody targetRigidbody;
            if (!IsValidCollider(collision.collider, out targetRigidbody))
                return;

            //Debug.Log("Collision with " + collision.gameObject.name);

            Vector3 velocity = _rigidbody.linearVelocity;
            float magnitude = velocity.magnitude;
            //ebug.DrawLine(transform.position, transform.position + velocity, Color.yellow, 5f);

            Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);

            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.forward;
            else horizontal.Normalize();

            Vector3 direction = horizontal * Mathf.Cos(_pitchAngle * Mathf.Deg2Rad)
                                + Vector3.up * Mathf.Sin(_pitchAngle * Mathf.Deg2Rad);

            magnitude = magnitude * _rigidbody.mass / targetRigidbody.mass * _forceMultiplier;

            //Debug.Log("Force: " + magnitude + " Direction: " + direction + "id: " + collision.gameObject.GetInstanceID());
            //Debug.DrawLine(transform.position, transform.position + direction * magnitude, Color.red, 5f);

            MobStateMachine mobStateMachine = targetRigidbody.gameObject.GetComponentInChildren<MobStateMachine>();
            HandleStateMachine(mobStateMachine);
            HandleObjectHitReturn(targetRigidbody.gameObject, mobStateMachine);

            AddForce(targetRigidbody, direction, magnitude);

            if (_enableAroundForce)
            {
                ApplyAroundForce(direction, magnitude);
            }
        }

        private void AddForce(Rigidbody target, Vector3 direction, float magnitude)
        {
            if(_debugForceDirection) Debug.DrawLine(target.position, target.position + direction * magnitude, Color.yellow, 5f);

            Vector3 force = direction * magnitude;
            target.AddForce(force, ForceMode.Impulse);
        }

        private void HandleStateMachine(MobStateMachine mobStateMachine)
        {
            if (mobStateMachine == null || mobStateMachine.GetState() == MOB_STATE.READY_TO_FLY) return;

            mobStateMachine.SetForceState(MOB_STATE.READY_TO_FLY);
        }

        private void HandleObjectHitReturn(GameObject gameObject, MobStateMachine mobStateMachine = null)
        {
            int id = gameObject.GetInstanceID();

            if (mobStateMachine == null) mobStateMachine = gameObject.GetComponentInChildren<MobStateMachine>();

            if (mobStateMachine == null) return;

            _hitObjects.Add(id);

            ActionScheduler.RunAfterDelay(_hitObjectsDelay, () =>
            {
                if (_hitObjects == null) return;

                _hitObjects.Remove(id);
                mobStateMachine.SetForceState(MOB_STATE.NONE);
                //Debug.Log("Removed object id: " + id);
            });
        }

        private bool IsValidCollider(Collider collider, out Rigidbody rigidbody)
        {
            rigidbody = null;
            if (((1 << collider.gameObject.layer) & GetMask().value) == 0) return false;

            //TODO if multiple colldiers might trigger the same object 
            if (_hitObjects.Contains(collider.gameObject.GetInstanceID()))
            {
                //Debug.Log("Already hit this object id: " + collision.gameObject.GetInstanceID());
                return false;
            }

            if (!_onlyOwners && !collider.gameObject.TryGetComponent(out rigidbody))
            {
                return false;
            }

            if (collider.gameObject.TryGetComponent(out IOwner owner))
            {
                rigidbody = owner.GetRootOwner().GetGameObject().GetComponent<Rigidbody>();
            }
            else
                return false;

            return true;
        }

       
        /// <summary>
        /// Applies additional force to all nearby objects.
        /// It creates an area based on the object's own collider bounds scaled by _aroundForceScale.
        /// </summary>
        /// <param name="baseDirection">The original hit direction used if _forceAwayFromOrigin is false.</param>
        /// <param name="baseMagnitude">The original magnitude used as a baseline for the force.</param>
        private void ApplyAroundForce(Vector3 baseDirection, float baseMagnitude)
        {
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider == null)
                return;

            int count = 0;
            if (ownCollider is BoxCollider boxCollider)
            {
                Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
                Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, boxCollider.transform.lossyScale) * _aroundForceScale;
                Quaternion orientation = boxCollider.transform.rotation;
                count = Physics.OverlapBoxNonAlloc(center, halfExtents, _colliderResults, orientation, GetMask());

                if (_debugDrawAroundForce) DebugDrawBox(center, halfExtents, orientation, Color.red, 5f);
            }
            else if (ownCollider is CapsuleCollider capsuleCollider)
            {
                Vector3 localPoint0 = capsuleCollider.center;
                Vector3 localPoint1 = capsuleCollider.center;
                switch (capsuleCollider.direction)
                {
                    case 0:
                        localPoint0.x = capsuleCollider.center.x - (capsuleCollider.height / 2 - capsuleCollider.radius);
                        localPoint1.x = capsuleCollider.center.x + (capsuleCollider.height / 2 - capsuleCollider.radius);
                        break;
                    case 1:
                        localPoint0.y = capsuleCollider.center.y - (capsuleCollider.height / 2 - capsuleCollider.radius);
                        localPoint1.y = capsuleCollider.center.y + (capsuleCollider.height / 2 - capsuleCollider.radius);
                        break;
                    case 2:
                        localPoint0.z = capsuleCollider.center.z - (capsuleCollider.height / 2 - capsuleCollider.radius);
                        localPoint1.z = capsuleCollider.center.z + (capsuleCollider.height / 2 - capsuleCollider.radius);
                        break;
                }
                Vector3 point0 = capsuleCollider.transform.TransformPoint(localPoint0);
                Vector3 point1 = capsuleCollider.transform.TransformPoint(localPoint1);
                float radius = capsuleCollider.radius * _aroundForceScale;
                count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, _colliderResults, GetMask());

                if (_debugDrawAroundForce) DebugDrawCapsule(point0, point1, radius, Color.blue, 5f);
            }
            else if (ownCollider is SphereCollider sphereCollider)
            {
                Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
                float scale = Mathf.Max(sphereCollider.transform.lossyScale.x, sphereCollider.transform.lossyScale.y, sphereCollider.transform.lossyScale.z);
                float radius = sphereCollider.radius * scale * _aroundForceScale;
                count = Physics.OverlapSphereNonAlloc(center, radius, _colliderResults, GetMask());

                if (_debugDrawAroundForce) DebugDrawSphere(center, radius, Color.green, 5f);
            }
            else
            {
                Vector3 center = ownCollider.bounds.center;
                float radius = ownCollider.bounds.extents.magnitude * _aroundForceScale;
                count = Physics.OverlapSphereNonAlloc(center, radius, _colliderResults, GetMask());

                if(_debugDrawAroundForce) DebugDrawSphere(center, radius, Color.green, 5f);
            }

            for (int i = 0; i < count; i++)
            {
                Collider col = _colliderResults[i];
                if (col.gameObject == gameObject)
                {
                    _colliderResults[i] = null;
                    continue;
                }
                    

                if (col.attachedRigidbody == null)
                {
                    _colliderResults[i] = null;
                    continue;
                }

                Rigidbody rigidbody;

                if (!IsValidCollider(col, out rigidbody))
                {
                    _colliderResults[i] = null;
                    continue;
                }

                HandleObjectHitReturn(col.gameObject);

                Vector3 forceDirection = _forceAwayFromOrigin
                                         ? (rigidbody.position - transform.position).normalized
                                         : baseDirection;

                float forceMagnitude = baseMagnitude * _aroundForceMultiplier;

                AddForce(rigidbody, forceDirection, forceMagnitude);
                //rigidbody.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
                _colliderResults[i] = null;
            }
        }

        public bool IsEnabled()
        {
            return _enable;
        }

        public void SetEnable(bool enable)
        {
            _enable = enable;
        }

        #region Debug Drawing Helpers

        private static void DebugDrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration)
        {
            Debug.Log("Drawing box");
            Vector3[] corners = new Vector3[8];
            corners[0] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);
            corners[1] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            corners[2] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
            corners[3] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
            corners[4] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
            corners[5] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            corners[6] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
            corners[7] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);

            Debug.DrawLine(corners[0], corners[1], color, duration);
            Debug.DrawLine(corners[0], corners[2], color, duration);
            Debug.DrawLine(corners[0], corners[4], color, duration);

            Debug.DrawLine(corners[1], corners[3], color, duration);
            Debug.DrawLine(corners[1], corners[5], color, duration);

            Debug.DrawLine(corners[2], corners[3], color, duration);
            Debug.DrawLine(corners[2], corners[6], color, duration);

            Debug.DrawLine(corners[3], corners[7], color, duration);

            Debug.DrawLine(corners[4], corners[5], color, duration);
            Debug.DrawLine(corners[4], corners[6], color, duration);

            Debug.DrawLine(corners[5], corners[7], color, duration);
            Debug.DrawLine(corners[6], corners[7], color, duration);
        }

        private static void DebugDrawCapsule(Vector3 point0, Vector3 point1, float radius, Color color, float duration)
        {
            Debug.Log("Drawing capsule");
            int segments = 20;
            float angleStep = 360f / segments;

            Vector3 axis = (point1 - point0).normalized;
            Vector3 up = Vector3.up;
            if (Vector3.Dot(axis, up) > 0.99f)
                up = Vector3.forward;
            Vector3 perp1 = Vector3.Cross(axis, up).normalized;
            Vector3 perp2 = Vector3.Cross(axis, perp1);

            Vector3 prevCirclePoint0 = point0 + perp1 * radius;
            Vector3 prevCirclePoint1 = point1 + perp1 * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = perp1 * Mathf.Cos(rad) * radius + perp2 * Mathf.Sin(rad) * radius;
                Vector3 circlePoint0 = point0 + offset;
                Vector3 circlePoint1 = point1 + offset;
                Debug.DrawLine(prevCirclePoint0, circlePoint0, color, duration);
                Debug.DrawLine(prevCirclePoint1, circlePoint1, color, duration);
                prevCirclePoint0 = circlePoint0;
                prevCirclePoint1 = circlePoint1;
            }
            Debug.DrawLine(point0 + perp1 * radius, point1 + perp1 * radius, color, duration);
            Debug.DrawLine(point0 - perp1 * radius, point1 - perp1 * radius, color, duration);
        }

        private static void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
        {
            Debug.Log("Drawing sphere");
            int segments = 20;
            float angleStep = 360f / segments;

            Vector3 prevPointXY = center + new Vector3(radius, 0, 0);
            Vector3 prevPointXZ = center + new Vector3(radius, 0, 0);
            Vector3 prevPointYZ = center + new Vector3(0, radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 pointXY = center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
                Vector3 pointXZ = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
                Vector3 pointYZ = center + new Vector3(0, Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

                Debug.DrawLine(prevPointXY, pointXY, color, duration);
                Debug.DrawLine(prevPointXZ, pointXZ, color, duration);
                Debug.DrawLine(prevPointYZ, pointYZ, color, duration);

                prevPointXY = pointXY;
                prevPointXZ = pointXZ;
                prevPointYZ = pointYZ;
            }
        }
        #endregion
    }
}