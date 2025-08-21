using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum TURRET_FOLLOW_MODE { FollowHull, FollowTarget }
public enum TURN_STATE { None, Left, Right }

/// <summary>
/// Differential-drive tank: chassis (tracks) + independent turret.
/// - Uses IMovement via MobMovement (ManagerMobMovement drives FixedUpdate).
/// - No per-frame allocations; no LINQ; all refs cached.
/// - While hull is rotating beyond align threshold, translation is locked (optional).
/// - Turret can track a target or the hull forward independently.
/// - Exposes left/right turn state + track blend values for animations.
/// </summary>
[DefaultExecutionOrder(2)]
public sealed class TankMovement : MobMovement
{
    [Header("References")]
    [SerializeField] private Transform _hull;          // If null, uses this.transform
    [SerializeField] private Transform _turret;        // Optional; turret independent yaw
    [SerializeField] private Rigidbody _rigidbody;     // Optional; if set, uses MovePosition/MoveRotation

    [Header("Drive")]
    [Tooltip("If true, tank won't translate while the hull is still turning toward the move direction.")]
    [SerializeField] private bool _lockTranslationWhileTurning = true;

    [Tooltip("Degrees within which we consider hull 'aligned' and allow translation.")]
    [SerializeField] private float _alignAngleDegrees = 3f;

    [Tooltip("Max angular speed for hull in degrees/second (uses _rotationSpeed from base if > 0).")]
    [SerializeField] private float _hullTurnSpeed = 180f;

    [Tooltip("Clamp movement to XZ plane.")]
    [SerializeField] private bool _lockY = true;

    [Header("Turret")]
    [SerializeField] private TURRET_FOLLOW_MODE _turretFollowMode = TURRET_FOLLOW_MODE.FollowTarget;
    [SerializeField] private float _turretTurnSpeed = 240f;
    [SerializeField,HideIf(nameof(_findPlayer))] private Transform _turretTarget;

    [Header("Input (AI/Controller sets these)")]
    [Tooltip("If set, tank will steer toward this position. Otherwise uses explicit move direction.")]
    [SerializeField, HideIf(nameof(_findPlayer))] private Transform _moveTarget;
    private Vector3 _explicitMoveDir; // world-space dir if no _moveTarget
    private bool _hasExplicitMoveDir;

    [Header("Animation Hooks (read-only)")]
    [SerializeField, Tooltip("Left track blend [-1..+1]. +1 forward, -1 backward.")]
    [ReadOnly]
    private float _leftTrackBlend;
    [SerializeField, Tooltip("Right track blend [-1..+1]. +1 forward, -1 backward.")]
    [ReadOnly]
    private float _rightTrackBlend;
    
    [SerializeField][ReadOnly] private TURN_STATE _turnState = TURN_STATE.None;

    // Events (optional) for animation/state machines. Subscribe from elsewhere.
    public event Action<TURN_STATE> OnTurnStateChanged;

    // Public read-only properties for external systems (animator, SFX, etc.)
    public TURN_STATE GetTurnState() => _turnState;
    public float GetLeftTrackBlend() => _leftTrackBlend;
    public float GetRightTrackBlend() => _rightTrackBlend;

    [Title("Movement Unlock")]
    [Tooltip("Allow translation when |angle to target| ≤ this (used when lockTranslationWhileTurning = true).")]
    [SerializeField] private float _moveUnlockAngle = 25f; // was effectively 3f via _alignAngleDegrees

    [Tooltip("If true, move slowly even when angle is larger than unlock angle.")]
    [SerializeField] private bool _creepWhileTurning = false;

    [Tooltip("Speed factor while creeping during large turn (0..1).")]
    [SerializeField, Range(0f, 1f)] private float _creepSpeedFraction = 0.3f;

    [Title("Acceleration")]
    [SerializeField, Min(0f)] private float _acceleration = 10f;   // m/s^2 when speeding up
    [SerializeField, Min(0f)] private float _deceleration = 14f;   // m/s^2 when slowing down
    [SerializeField, Min(0f)] private float _minMoveSpeed = 0f;    // optional floor for tiny creeping

    private float _currentSpeed; // m/s along hull.forward



    // Cached
    private Transform _tr;
    private float _lastYaw; // degrees

    protected override void Start()
    {
        _tr = transform;
        if (_hull == null) _hull = _tr;
        _lastYaw = _hull.eulerAngles.y;

        base.Start();
    }

    public override void SetTarget(Transform newTarget)
    {
        base.SetTarget(newTarget);
        SetMoveTarget(newTarget);
        SetTurretTarget(newTarget);
    }

    #region External Control API
    public void SetMoveTarget(Transform target)
    {
        _moveTarget = target;
        _hasExplicitMoveDir = false;
    }

    /// <summary>Set a world-space movement direction (normalized or not). Clears _moveTarget.</summary>
    public void SetMoveDirection(Vector3 worldDirection)
    {
        _moveTarget = null;
        if (_lockY) worldDirection.y = 0f;
        _explicitMoveDir = worldDirection;
        _hasExplicitMoveDir = worldDirection.sqrMagnitude > 0.0001f;
    }

    public void SetTurretTarget(Transform t) => _turretTarget = t;
    public void SetTurretFollowMode(TURRET_FOLLOW_MODE mode) => _turretFollowMode = mode;
    #endregion

    public override void UpdateMovement(float deltaTime)
    {
        if (!_isEnabled || !IsMovementEnabled()) return;

        Vector3 desiredDir = _hull.forward;
        if (_moveTarget != null)
            desiredDir = _moveTarget.position - _hull.position;
        else if (_hasExplicitMoveDir)
            desiredDir = _explicitMoveDir;

        if (_lockY) desiredDir.y = 0f;

        bool hasMoveDir = desiredDir.sqrMagnitude > 0.000001f;
        if (hasMoveDir) desiredDir.Normalize();

        float maxHullTurn = (_rotationSpeed > 0f ? _rotationSpeed : _hullTurnSpeed) * deltaTime;
        float signedAngleToDesired = 0f;
        bool turning = false;

        if (hasMoveDir)
        {
            Vector3 fwd = _hull.forward;
            if (_lockY) fwd.y = 0f;
            fwd.Normalize();

            signedAngleToDesired = Vector3.SignedAngle(fwd, desiredDir, Vector3.up);
            turning = Mathf.Abs(signedAngleToDesired) > _alignAngleDegrees;

            float step = Mathf.Clamp(signedAngleToDesired, -maxHullTurn, maxHullTurn);
            if (Mathf.Abs(step) > 0.0001f)
            {
                Quaternion q = Quaternion.AngleAxis(step, Vector3.up);
                if (_rigidbody != null)
                    _rigidbody.MoveRotation(q * _rigidbody.rotation);
                else
                    _hull.rotation = q * _hull.rotation;
            }
        }

        UpdateTurnStateAndTracks(deltaTime, signedAngleToDesired, turning);

        //Translation with unlock angle + optional creep + ACCELERATION
        float absAng = Mathf.Abs(signedAngleToDesired);
        bool canTranslate;
        float speedFactor = 1f;

        if (_lockTranslationWhileTurning)
        {
            if (absAng <= _moveUnlockAngle)
            {
                canTranslate = hasMoveDir;
                speedFactor = 1f;
            }
            else if (_creepWhileTurning)
            {
                canTranslate = hasMoveDir;
                speedFactor = _creepSpeedFraction;
            }
            else
            {
                canTranslate = false;
            }
        }
        else
        {
            canTranslate = hasMoveDir;
        }

        float targetSpeed = (canTranslate ? (_speed * speedFactor) : 0f);
        if (targetSpeed <= _minMoveSpeed && targetSpeed > 0f) targetSpeed = _minMoveSpeed;

        float accel = (targetSpeed > _currentSpeed) ? _acceleration : _deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accel * deltaTime);

        if (_currentSpeed > 0.0001f)
        {
            Vector3 delta = _hull.forward * (_currentSpeed * deltaTime);
            if (_lockY) delta.y = 0f;

            if (_rigidbody != null)
                _rigidbody.MovePosition(_rigidbody.position + delta);
            else
                _hull.position += delta;

            float norm = (_speed > 0f) ? Mathf.Clamp01(_currentSpeed / _speed) : 0f;
            _leftTrackBlend = norm;
            _rightTrackBlend = norm;
        }
        else
        {
            if (_turnState == TURN_STATE.None)
            {
                _leftTrackBlend = 0f;
                _rightTrackBlend = 0f;
            }
        }

        UpdateTurret(deltaTime);
    }

    private void UpdateTurnStateAndTracks(float dt, float signedAngleToDesired, bool isTurning)
    {
        float yaw = _hull.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(_lastYaw, yaw);
        _lastYaw = yaw;
        float yawRate = dt > 0f ? (yawDelta / dt) : 0f;

        TURN_STATE newState = TURN_STATE.None;
        if (isTurning)
        {
            newState = (signedAngleToDesired > 0f) ? TURN_STATE.Left : TURN_STATE.Right;
        }
        else if (Mathf.Abs(yawRate) > 0.01f)
        {
            newState = (yawRate > 0f) ? TURN_STATE.Left : TURN_STATE.Right;
        }

        if (newState != _turnState)
        {
            _turnState = newState;
            var cb = OnTurnStateChanged;
            if (cb != null) cb(_turnState);
        }

        if (_turnState == TURN_STATE.Left)
        {
            _leftTrackBlend = 1f;
            _rightTrackBlend = -1f;
        }
        else if (_turnState == TURN_STATE.Right)
        {
            _leftTrackBlend = -1f;
            _rightTrackBlend = 1f;
        }
        else
        {
            if (Mathf.Abs(yawRate) < 0.01f)
            {
                _leftTrackBlend = 0f;
                _rightTrackBlend = 0f;
            }
        }
    }

    private void UpdateTurret(float dt)
    {
        if (_turret == null) return;

        Vector3 targetDir;
        if (_turretFollowMode == TURRET_FOLLOW_MODE.FollowHull || _turretTarget == null)
        {
            targetDir = _hull.forward;
        }
        else
        {
            targetDir = _turretTarget.position - _turret.position;
            if (_lockY) targetDir.y = 0f;
        }

        if (targetDir.sqrMagnitude < 0.000001f) return;
        targetDir.Normalize();

        Vector3 turretFwd = _turret.forward;
        if (_lockY) turretFwd.y = 0f;
        if (turretFwd.sqrMagnitude < 0.000001f) turretFwd = _hull.forward;

        float angle = Vector3.SignedAngle(turretFwd.normalized, targetDir, Vector3.up);
        float step = Mathf.Clamp(angle, -_turretTurnSpeed * dt, _turretTurnSpeed * dt);

        if (Mathf.Abs(step) > 0.0001f)
        {
            Quaternion q = Quaternion.AngleAxis(step, Vector3.up);
            _turret.rotation = q * _turret.rotation;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform hull = _hull != null ? _hull : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(hull.position, hull.position + hull.forward * 2f);

        if (_moveTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(hull.position, _moveTarget.position);
        }

        if (_turret != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_turret.position, _turret.position + _turret.forward * 1.5f);
        }
    }
#endif
}
