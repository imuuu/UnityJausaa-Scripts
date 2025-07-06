
using System.Collections.Generic;
using DigitalRuby.ThunderAndLightning;
using Game.HitDetectorSystem;
using Game.MobStateSystem;
using Game.PhysicInteractions;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.SkillSystem
{
    public class Ability_PhysicHand : Ability, IManualEnd, IStaticSkill, ISkillButtonUp
    {
        [Title("Reference")]
        [SerializeField] private GameObject _lightningSplineLine;
        [SerializeField] private GameObject _lightningSplineLasso;

        [Title("Damage Settings")]
        [SerializeField] private float _weightReduceMultiplier = 0.1f;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _throwMultiplier = 5f;

        [Header("Throw Angle Settings")]
        // Mouse speed (magnitude) corresponding to a horizontal (0°) throw.
        [SerializeField] private float _minMouseSpeed = 0f;
        // Mouse speed (magnitude) at which the throw will reach the maximum angle.
        [SerializeField] private float _maxMouseSpeed = 50f;
        // Maximum angle (in degrees) above horizontal for the throw.
        [SerializeField] private float _maxThrowAngle = 45f;

        [Header("Weight Influence Settings")]
        // Enable this option to have the object's mass affect drag and throw speeds.
        [SerializeField] private bool _useWeightInfluence = false;
        // Multiplier to adjust how object mass affects dragging speed.
        [SerializeField] private float _weightDragMultiplier = 1f;
        // Multiplier to adjust how object mass affects throwing speed.
        [SerializeField] private float _weightThrowMultiplier = 1f;

        [Header("Lift Settings")]
        // Base speed factor for smoothly lifting the object to the correct height.
        [SerializeField] private float _liftSpeed = 5f;
        // Enable this option to have the object's mass affect the lift speed.
        [SerializeField] private bool _useWeightInfluenceLift = true;
        // Multiplier to adjust how object mass affects lift speed.
        [SerializeField] private float _weightLiftMultiplier = 1f;
        // Maximum mass used for lift calculations (objects heavier than this lift at the same speed).
        [SerializeField] private float _maxLiftWeight = 50f;

        [Header("Auto Height Settings")]
        // If enabled, the object's proper vertical offset is determined automatically.
        [SerializeField] private bool _autoHeight = true;
        // The ground level (y position) where objects should "rest."
        [SerializeField] private float _groundLevel = 0f;
        [Header("Point Modifier")]
        [Tooltip("If enabled, the script will apply forces to the points of the lightning spline.")]
        [SerializeField] private bool _applyForceToPoints = true;

        private Rigidbody _pickedRigidbody;
        private Vector3 _lastMouseWorldPos;
        private Vector3 _mouseDelta;
        private float _dragY;

        private Plane _dragPlane;

        [Title("Hand Settings")]
        [SerializeField] private Transform _handObject;
        private Transform _handObjectTransform;

        private PHYSIC_HAND_STATE _state = PHYSIC_HAND_STATE.PICK_UP;

        private LightningSplineScript _lightningSplineScriptLine;
        private LightningSplineScript _lightningSplineScriptLasso;
        private Vector3[] _lassoLocalOffsets;

        private List<Rigidbody> _pointRigidbodys;
        private RaycastHit _hit;
        private ForceInteraction _forceInteraction;
        private bool _isForceInteractionGenerated = false;


        private const float DEFAULT_DRAG_Y = 1f;

        public enum PHYSIC_HAND_STATE
        {
            PICK_UP,
            DRAG,
        }

        override public void AwakeSkill()
        {
            base.AwakeSkill();

            _dragPlane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));

            // if (_lightningSplineLine != null || (_lightningSplineScriptLine == null && _lightningSplineLasso != null))
            // {
            //     _lightningSplineScriptLine = GameObject.Instantiate(_lightningSplineLine, Vector3.zero, Quaternion.identity).GetComponent<LightningSplineScript>();
            //     _lightningSplineScriptLine.gameObject.SetActive(false);
            // }

            // if (_lightningSplineLasso != null || (_lightningSplineScriptLasso == null && _lightningSplineLine != null))
            // {
            //     _lightningSplineScriptLasso = GameObject.Instantiate(_lightningSplineLasso, Vector3.zero, Quaternion.identity).GetComponent<LightningSplineScript>();
            //     _lightningSplineScriptLasso.gameObject.SetActive(false);
            // }

            // if (_handObject == null) return;

            // _handObjectTransform = GameObject.Instantiate(_handObject, Vector3.zero, Quaternion.identity);
            // if (_handObjectTransform.transform.GetComponent<FollowerController>() == null) _handObjectTransform.transform.AddComponent<FollowerController>();
            // _handObjectTransform.gameObject.SetActive(false);

            CheckHandObjects();
        }

        private void CheckHandObjects()
        {
            if (_lightningSplineScriptLine == null && _lightningSplineLasso != null)
            {
                _lightningSplineScriptLine = GameObject.Instantiate(_lightningSplineLine, Vector3.zero, Quaternion.identity).GetComponent<LightningSplineScript>();
                _lightningSplineScriptLine.gameObject.SetActive(false);
            }

            if (_lightningSplineScriptLasso == null && _lightningSplineLine != null)
            {
                _lightningSplineScriptLasso = GameObject.Instantiate(_lightningSplineLasso, Vector3.zero, Quaternion.identity).GetComponent<LightningSplineScript>();
                _lightningSplineScriptLasso.gameObject.SetActive(false);
            }

            if (_handObject == null) return;

            if (_handObjectTransform != null) return;

            _handObjectTransform = GameObject.Instantiate(_handObject, Vector3.zero, Quaternion.identity);
            if (_handObjectTransform.transform.GetComponent<FollowerController>() == null) _handObjectTransform.transform.AddComponent<FollowerController>();
            _handObjectTransform.gameObject.SetActive(false);
        }
        [SerializeField] private bool _enableMobGrabbing = false;
        private MobStateMachine _mobStateMachine;

        // private bool _isOwnerCreated = false;
        // private Owner _createdOwner;
        // private HitDetector _foundHitDetector;
        // private HitDetector_PhysicHand _hitDetectorPhysicHand;

        // private bool _isDamageDealerCreated = false;
        // private float _oldDamage;
        // private IDamageDealer _damageDealer;
        // private DAMAGE_SOURCE _oldDamageSource;

        public override void StartSkill()
        {
            _mobStateMachine = null;
            CheckHandObjects();

            _dragY = DEFAULT_DRAG_Y;
            _state = PHYSIC_HAND_STATE.PICK_UP;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out _hit))
            {
                if (_hit.rigidbody != null)
                {
                    if (!_enableMobGrabbing && _hit.transform.GetComponent<Mob>() != null)
                    {
                        Debug.Log("Mob grabbing is disabled, ending physic hand.");
                        EndPhysicHand();
                        return;
                    }

                    if (_enableMobGrabbing)
                    {
                        _mobStateMachine = _hit.transform.GetComponentInChildren<MobStateMachine>();
                        if (_mobStateMachine != null)
                        {
                            _mobStateMachine.JumpToState(MOB_STATE.MOB_GRABBING);
                        }
                    }

                    _pickedRigidbody = _hit.rigidbody;
                    float enter;
                    if (_dragPlane.Raycast(ray, out enter))
                    {
                        _lastMouseWorldPos = ray.GetPoint(enter);
                    }
                    _pickedRigidbody.useGravity = false;

                    if (_handObjectTransform != null)
                    {
                        _handObjectTransform.gameObject.SetActive(true);
                        if (_handObjectTransform.TryGetComponent(out FollowerController followerController))
                        {
                            followerController._target = _pickedRigidbody.transform;
                            followerController._distanceFromTarget = Vector3.Distance(_hit.point, _pickedRigidbody.transform.position);
                        }
                    }

                    PhysicHandTouched touchedScript = _pickedRigidbody.transform.gameObject.GetOrAdd<PhysicHandTouched>();
                    touchedScript.OwnerOfPhysicHand = this;
                    touchedScript.Initialize(_baseStats, _pickedRigidbody, _weightReduceMultiplier);

                    if (_autoHeight)
                    {
                        EntityStatistics entityStats = _pickedRigidbody.GetComponent<EntityStatistics>();
                        if (entityStats != null)
                        {
                            _dragY = _groundLevel;
                        }
                        else
                        {
                            Collider col = _pickedRigidbody.GetComponent<Collider>();
                            if (col != null)
                            {
                                _dragY = _groundLevel + col.bounds.extents.y;
                            }
                            else
                            {
                                _dragY = _groundLevel;
                            }
                        }
                    }
                    else
                    {
                        _dragY = DEFAULT_DRAG_Y;
                    }

                    _state = PHYSIC_HAND_STATE.DRAG;
                    GenerateLine();
                    GenerateLasso();
                    HandleForceInteraction();
                }
            }
            else
            {
                Debug.Log("No rigidbody found");
                EndPhysicHand();
            }
        }
        private List<GameObject> _lightningPoints;
        private void GenerateLine()
        {
            if (_pickedRigidbody == null) return;

            _lightningSplineScriptLine.Camera = Camera.main;
            _lightningSplineScriptLine.gameObject.SetActive(true);
            _pointRigidbodys = new List<Rigidbody>();

            Vector3 playerPos = Player.Instance.transform.position;

            _lightningPoints = _lightningSplineScriptLine.LightningPath;

            Vector3[] points = PointsUtilities.GetPointsInLine(playerPos, _hit.point, _lightningPoints.Count);

            for (int i = 0; i < _lightningPoints.Count; i++)
            {
                _lightningPoints[i].transform.position = points[i] + Vector3.up;
                if (_applyForceToPoints) _pointRigidbodys.Add(_lightningPoints[i].GetComponent<Rigidbody>());
            }
        }

        private void GenerateLasso()
        {
            if (_pickedRigidbody == null)
                return;

            _lightningSplineScriptLasso.Camera = Camera.main;
            _lightningSplineScriptLasso.gameObject.SetActive(true);

            List<GameObject> gameObjectPoints = _lightningSplineScriptLasso.LightningPath;

            List<Vector3> directions = PointsUtilities.GetCirclePointsXZ(_pickedRigidbody.transform, gameObjectPoints.Count, 1f);

            Vector3[] pointsArray = new Vector3[gameObjectPoints.Count];
            Vector3 worldCenterOfMass = _pickedRigidbody.transform.TransformPoint(_pickedRigidbody.centerOfMass);
            for (int i = 0; i < directions.Count; i++)
            {
                pointsArray[i] = worldCenterOfMass;
            }

            short iterations = 100;
            bool[] isLocked = new bool[gameObjectPoints.Count];

            Collider collider = _enableMobGrabbing ? _pickedRigidbody.GetComponentInChildren<Collider>()
            : _pickedRigidbody.GetComponent<Collider>();

            if (collider == null)
            {
                Debug.LogError("No collider found on the picked Rigidbody!");
                return;
            }

            while (iterations > 0)
            {
                bool allLocked = true;

                for (int i = 0; i < pointsArray.Length; i++)
                {
                    if (isLocked[i])
                        continue;

                    Vector3 direction = (directions[i] - _pickedRigidbody.transform.position).normalized;

                    if (collider.ClosestPoint(pointsArray[i]) != pointsArray[i])
                    {
                        isLocked[i] = true;
                        gameObjectPoints[i].transform.position = pointsArray[i] + direction * 0.1f;
                        //gameObjectPoints[i].transform.SetParent(_pickedRigidbody.transform);
                    }
                    else
                    {
                        pointsArray[i] += direction * 0.1f;
                        allLocked = false;
                    }
                }

                if (allLocked)
                    break;

                iterations--;
            }

            Vector3 rbPos = _pickedRigidbody.transform.position;
            _lassoLocalOffsets = new Vector3[gameObjectPoints.Count];
            for (int i = 0; i < gameObjectPoints.Count; i++)
            {
                _lassoLocalOffsets[i] = gameObjectPoints[i].transform.position - rbPos;
            }
        }
        private void HandleForceInteraction()
        {
            _forceInteraction = _pickedRigidbody.GetComponent<ForceInteraction>();

            if (_forceInteraction == null)
            {
                _forceInteraction = _pickedRigidbody.gameObject.AddComponent<ForceLinerHit>();
                _isForceInteractionGenerated = true;
            }

            if (_forceInteraction.GetMask() == 0)
            {
                _forceInteraction.SetMask(LayerMask.GetMask("Enemy"));
            }

            if (_forceInteraction is IEnabled iEnabled)
            {
                iEnabled.SetEnable(true);
            }
        }

        private void UpdateLightningPoints()
        {
            if (_pickedRigidbody == null) return;

            if (_lightningPoints.Count == 0) return;

            _lightningPoints[0].transform.position = Player.Instance.GetStaffTip().position;
            _lightningPoints[_lightningPoints.Count - 1].transform.position = _pickedRigidbody.position;
        }

        public override void UpdateSkill()
        {
            if (!IsObjectAvailable())
            {
                EndPhysicHand();
                return;
            }

            UpdateLightningPoints();
            if (!DragPickedObject())
            {
                EndPhysicHand();
                return;
            }

            MoveLassoCirclePoints();

            MovePointRigidbodys();

        }

        // public void FixedUpdate()
        // {
        //     if (!IsObjectAvailable())
        //     {
        //         EndPhysicHand();
        //         return;
        //     }

        //     UpdateLightningPoints();
        //     if (!DragPickedObject())
        //     {
        //         EndPhysicHand();
        //         return;
        //     }

        //     MoveLassoCirclePoints();

        //     MovePointRigidbodys();
        // }

        public void OnButtonUp()
        {
            if (_pickedRigidbody == null)
            {
                EndPhysicHand();
                return;
            }

            if (_handObjectTransform != null) _handObjectTransform.gameObject.SetActive(false);

            float effectiveThrowMultiplier = _throwMultiplier;
            if (_useWeightInfluence)
            {
                effectiveThrowMultiplier = _throwMultiplier * (_weightThrowMultiplier / _pickedRigidbody.mass);
            }

            Vector3 baseThrowVelocity = _mouseDelta * effectiveThrowMultiplier;
            float totalSpeed = baseThrowVelocity.magnitude;
            if (totalSpeed > 0.001f)
            {
                // Use mouse movement magnitude to determine the throw angle.
                float mouseSpeed = _mouseDelta.magnitude;
                float t = Mathf.InverseLerp(_minMouseSpeed, _maxMouseSpeed, mouseSpeed);
                float angleDeg = Mathf.Lerp(0, _maxThrowAngle, t);
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector3 horizontalDir = baseThrowVelocity.normalized;
                Vector3 finalVelocity = horizontalDir * (totalSpeed * Mathf.Cos(angleRad))
                                        + Vector3.up * (totalSpeed * Mathf.Sin(angleRad));

                _pickedRigidbody.linearVelocity = finalVelocity;
            }
            else
            {
                _pickedRigidbody.linearVelocity = Vector3.zero;
            }

            // Re-enable gravity and release the object.
            if (_pickedRigidbody.transform.TryGetComponent(out PhysicHandTouched physicHandTouched))
            {
                GameObject.Destroy(physicHandTouched);
            }

            _pickedRigidbody.useGravity = true;
            _pickedRigidbody = null;

            EndPhysicHand();
        }

        private void EndPhysicHand()
        {
            if(_mobStateMachine != null)
            {
                _mobStateMachine.JumpToState(MOB_STATE.NONE);
            }
            ManagerSkills.Instance.EndManualSkill(this);
        }

        private bool DragPickedObject()
        {
            if (_pickedRigidbody != null && _state == PHYSIC_HAND_STATE.DRAG)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                float enter;
                Vector3 currentMouseWorldPos = _lastMouseWorldPos;
                if (_dragPlane.Raycast(ray, out enter))
                {
                    currentMouseWorldPos = ray.GetPoint(enter);
                }
                // Compute mouse movement (used later for throwing).
                _mouseDelta = (currentMouseWorldPos - _lastMouseWorldPos) / Time.deltaTime;
                _lastMouseWorldPos = currentMouseWorldPos;

                Vector3 currentPos = _pickedRigidbody.position;

                // --- Horizontal Movement ---
                // For horizontal motion, use the mouse's x and z, but keep the object's current y.
                Vector3 horizontalTarget = new Vector3(currentMouseWorldPos.x, currentPos.y, currentMouseWorldPos.z);
                float effectiveMoveSpeed = _moveSpeed;
                if (_useWeightInfluence)
                {
                    effectiveMoveSpeed = _moveSpeed * (_weightDragMultiplier / _pickedRigidbody.mass);
                }
                Vector3 horizontalVelocity = (horizontalTarget - currentPos) * effectiveMoveSpeed;

                // --- Vertical Lift ---
                float effectiveLiftSpeed = _liftSpeed;
                if (_useWeightInfluenceLift)
                {
                    // Use the object's mass but cap it to maxLiftWeight so it doesn't slow down indefinitely.
                    float massForLift = Mathf.Min(_pickedRigidbody.mass, _maxLiftWeight);
                    effectiveLiftSpeed = _liftSpeed * (_weightLiftMultiplier / massForLift);
                }
                // Calculate the vertical difference from desired height.
                float verticalDiff = _dragY - currentPos.y;
                float verticalVelocity = verticalDiff * effectiveLiftSpeed;

                // Combine horizontal and vertical velocities.
                Vector3 desiredVelocity = horizontalVelocity + new Vector3(0, verticalVelocity, 0);
                _pickedRigidbody.linearVelocity = desiredVelocity;
                return true;
            }
            return false;
        }

        private void MoveLassoCirclePoints()
        {
            Vector3 rbPos = _pickedRigidbody.transform.position;
            for (int i = 0; i < _lightningSplineScriptLasso.LightningPath.Count; i++)
            {
                GameObject point = _lightningSplineScriptLasso.LightningPath[i];
                point.transform.position = rbPos + _lassoLocalOffsets[i];
            }
        }

        private void MovePointRigidbodys()
        {
            if (!_applyForceToPoints) return;

            if (_pointRigidbodys == null || _pointRigidbodys.Count < 2)
                return;

            const float forceMultiplier = 10f;
            const float deadZoneDistance = 0.5f;
            const float slowDownDistance = 2f;

            Vector3 lineStart = _pointRigidbodys[0].transform.position;
            Vector3 lineEnd = _pointRigidbodys[_pointRigidbodys.Count - 1].transform.position;
            Vector3 lineDirection = (lineEnd - lineStart).normalized;
            float lineLength = Vector3.Distance(lineStart, lineEnd);

            for (int i = 1; i < _pointRigidbodys.Count - 1; i++)
            {
                Rigidbody rb = _pointRigidbodys[i];

                Vector3 pointPosition = rb.transform.position;
                Vector3 toPoint = pointPosition - lineStart;

                // Project the point onto the infinite line
                float projection = Vector3.Dot(toPoint, lineDirection);
                // Clamp projection so the closest point lies within the line segment
                projection = Mathf.Clamp(projection, 0, lineLength);
                Vector3 closestPointOnLine = lineStart + lineDirection * projection;

                float distance = Vector3.Distance(pointPosition, closestPointOnLine);

                if (distance > deadZoneDistance)
                {
                    float forceMagnitude = forceMultiplier * (distance - deadZoneDistance);

                    if (distance < slowDownDistance)
                    {
                        forceMagnitude *= distance / slowDownDistance;
                    }

                    Vector3 forceDirection = (closestPointOnLine - pointPosition).normalized;
                    Vector3 force = forceDirection * forceMagnitude;
                    rb.AddForce(force, ForceMode.Acceleration);
                }
            }
        }

        public override void EndSkill()
        {
            foreach (GameObject point in _lightningSplineScriptLasso.LightningPath)
            {
                point.transform.SetParent(_lightningSplineScriptLasso.gameObject.transform);
            }

            _lightningSplineScriptLine.gameObject.SetActive(false);
            _lightningSplineScriptLasso.gameObject.SetActive(false);

            if (_pointRigidbodys != null) _pointRigidbodys.Clear();

            if (_forceInteraction != null && _forceInteraction is IEnabled iEnabled)
            {
                iEnabled.SetEnable(false);

                bool isOwner = _forceInteraction.GetComponent<IOwner>() != null;

                if (_isForceInteractionGenerated && isOwner)
                {
                    GameObject.Destroy(_forceInteraction);
                    _isForceInteractionGenerated = false;
                }

                _isForceInteractionGenerated = false;
            }

        }

        public bool IsObjectAvailable()
        {
            return _pickedRigidbody != null && _pickedRigidbody.gameObject.activeInHierarchy;
        }
    }


}