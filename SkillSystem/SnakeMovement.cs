using System;
using UnityEngine;
using Sirenix.OdinInspector;

public sealed class SnakeMovement : MobMovement
{
    private enum STATE { Slither, AttackWindup, AttackLunge, Cooldown }

    // ─────────────────────────────────────────────────────────────────────────────
    // GROUP IDS
    // ─────────────────────────────────────────────────────────────────────────────
    private const string G_REF = "References";
    private const string G_SLITHER = "Slither";
    private const string G_ORBIT = "Orbiting";
    private const string G_AVOID = "Avoidance";
    private const string G_ATTACK = "Attack";
    private const string G_DEBUG = "Debug";

    // ─────────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_REF)]
    [SerializeField, LabelText("Rigidbody (Kinematic)"),
     Tooltip("Optional. If assigned, prefer Kinematic + MovePosition/MoveRotation.")]
    private Rigidbody _rigidbody;

    [TitleGroup(G_REF)]
    [SerializeField, LabelText("Lock to XZ"),
     Tooltip("Force Y to 0 for all movement and checks (flat world).")]
    private bool _lockY = true;

    // ─────────────────────────────────────────────────────────────────────────────
    // SLITHER / LOCOMOTION
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_SLITHER)]
    [SerializeField, LabelText("Amplitude (m)"), MinValue(0f),
     Tooltip("Side-to-side sway amplitude (meters).")]
    private float _slitherAmplitude = 0.5f;

    [TitleGroup(G_SLITHER)]
    [SerializeField, LabelText("Frequency (Hz)"), MinValue(0f),
     Tooltip("How fast the snake oscillates side-to-side.")]
    private float _slitherFrequency = 2.0f;

    [TitleGroup(G_SLITHER)]
    [SerializeField, LabelText("Turn Speed (°/s)"), MinValue(0f),
     Tooltip("Max yaw speed when slithering/orbiting.")]
    private float _turnSpeed = 180f;

    [TitleGroup(G_SLITHER)]
    [SerializeField, LabelText("Acceleration (m/s²)"), MinValue(0f),
     Tooltip("Forward acceleration toward cruise speed.")]
    private float _acceleration = 12f;

    [TitleGroup(G_SLITHER)]
    [SerializeField, LabelText("Deceleration (m/s²)"), MinValue(0f),
     Tooltip("Braking force when slowing down.")]
    private float _deceleration = 14f;

    // ─────────────────────────────────────────────────────────────────────────────
    // ORBITING
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_ORBIT)]
    [SerializeField, LabelText("Orbit Radius"), MinValue(0f),
     Tooltip("Start circling the target inside this distance.")]
    private float _orbitRadius = 6f;

    [TitleGroup(G_ORBIT)]
    [SerializeField, LabelText("Tangent Bias"), Range(0f, 1f),
     Tooltip("0 = move radial in/out, 1 = circle around the target.")]
    private float _orbitTangentBias = 0.85f;

    [TitleGroup(G_ORBIT)]
    [SerializeField, LabelText("Direction (+1/-1)"),
     Tooltip("+1 = counter-clockwise, -1 = clockwise orbit.")]
    private int _orbitDirection = 1;

    [TitleGroup(G_ORBIT)]
    [Button(Icon = SdfIconType.ArrowRepeat)]
    private void FlipOrbitDirection() => _orbitDirection = -_orbitDirection;

    // ─────────────────────────────────────────────────────────────────────────────
    // AVOIDANCE (with smoothing, wall-slide, stuck escape)
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Probe Height (Y)"), MinValue(0f),
     Tooltip("Height above ground for avoidance ray origins.")]
    private float _avoidProbeHeight = 0.30f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Probe Distance"), MinValue(0f),
     Tooltip("Max length of center/side avoidance rays.")]
    private float _avoidProbeDistance = 4.5f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Side Angle (°)"), Range(0f, 80f),
     Tooltip("Angle left/right from forward for side rays.")]
    private float _avoidSideAngle = 25f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Turn Influence"), Range(0f, 1f),
     Tooltip("How strongly avoidance bends the desired direction.")]
    private float _avoidTurnInfluence = 0.9f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Enable Wall Slide"),
     Tooltip("When the center ray hits, project the desired direction along the wall normal instead of turning sharply.")]
    private bool _enableWallSlide = true;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Hold Time After Clear (s)"), MinValue(0f),
     Tooltip("Keep an avoidance bias for this long after rays clear to avoid snapping back into corners.")]
    private float _avoidHoldTime = 0.35f;

    [TitleGroup(G_AVOID)]
    [LabelText("Obstacle Mask"),
     InfoBox("Layers treated as obstacles for avoidance rays.\nExample: Walls / Environment / Obstacles. Exclude Player/Snake.", InfoMessageType.Info)]
    [SerializeField, Tooltip("Layers that the avoidance rays consider as blocking.")]
    private LayerMask _avoidMask = ~0;

    // Stuck + Escape
    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Stuck Speed ε (m/frame)"), MinValue(0f),
     Tooltip("If movement per frame is below this while avoidance hits, we consider the snake stuck.")]
    private float _stuckEpsilon = 0.02f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Stuck Time (s)"), MinValue(0f),
     Tooltip("Continuous time under ε before triggering an escape turn.")]
    private float _stuckTime = 0.5f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Escape Angle (°)"), Range(30f, 180f),
     Tooltip("Angle to rotate away when stuck. 90–180° works well.")]
    private float _escapeAngle = 120f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Escape Duration (s)"), MinValue(0.05f),
     Tooltip("How long to bias the escape direction once triggered.")]
    private float _escapeDuration = 0.6f;

    [TitleGroup(G_AVOID)]
    [SerializeField, LabelText("Escape Cooldown (s)"), MinValue(0f),
     Tooltip("Minimum time between escape triggers.")]
    private float _escapeCooldown = 1.0f;

    // ─────────────────────────────────────────────────────────────────────────────
    // ATTACK / LUNGE (with LOS height)
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_ATTACK)]
    [MinMaxSlider(0f, 50f, ShowFields = true)]
    [LabelText("Range (Min..Max)"),
     Tooltip("Only attempt an attack when target distance is within this range.")]
    [SerializeField] private Vector2 _attackRange = new(2.0f, 9.0f);

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Windup (s)"), MinValue(0f),
     Tooltip("Time spent coiling before the lunge.")]
    private float _attackWindupTime = 0.35f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Duration (s)"), MinValue(0f),
     Tooltip("How long the lunge lasts.")]
    private float _attackDuration = 0.60f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Lunge Speed (m/s)"), MinValue(0f),
     Tooltip("Forward speed during the lunge.")]
    private float _attackLungeSpeed = 12f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Turn Speed (°/s)"), MinValue(0f),
     Tooltip("Yaw speed while windup/lunge is happening.")]
    private float _attackTurnSpeed = 360f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Cooldown Min (s)"), MinValue(0f),
     Tooltip("Minimum time before another attack can start.")]
    private float _attackCooldownMin = 1.2f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("Cooldown Max (s)"), MinValue(0f),
     Tooltip("Maximum time before another attack can start.")]
    private float _attackCooldownMax = 2.0f;

    [TitleGroup(G_ATTACK)]
    [SerializeField, LabelText("LOS Ray Height (Y)"), MinValue(0f),
     Tooltip("Height above ground for the line-of-sight ray used to decide attacks.")]
    private float _losRayHeight = 0.30f;

    [TitleGroup(G_ATTACK)]
    [LabelText("Line of Sight (LOS) Mask"),
     InfoBox("Layers that BLOCK vision when deciding to lunge (e.g., walls/geometry).\nExclude Player/Snake layers.", InfoMessageType.Info)]
    [SerializeField, Tooltip("Layers that block the LOS ray when deciding to attack.")]
    private LayerMask _lineOfSightMask = ~0;

    // ─────────────────────────────────────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────────────────────────────────────
    [TitleGroup(G_DEBUG)]
    [SerializeField, LabelText("Draw Debug"),
     Tooltip("Draw debug rays and helpers in Scene view.")]
    private bool _debugDraw;

    [TitleGroup(G_DEBUG)][ShowInInspector, ReadOnly, LabelText("State")] private STATE _state = STATE.Slither;
    [TitleGroup(G_DEBUG)][ShowInInspector, ReadOnly, LabelText("Current Speed (m/s)")] private float _currentSpeed;
    [TitleGroup(G_DEBUG)][ShowInInspector, ReadOnly, LabelText("State Timer (s)")] private float _stateTimer;
    [TitleGroup(G_DEBUG)][ShowInInspector, ReadOnly, LabelText("Distance to Target (m)")] private float _debugDist;

    // ─────────────────────────────────────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────────────────────────────────────
    private float _slitherPhase; // radians
    private Transform _tr;

    // Avoidance memory & hit info
    private Vector3 _avoidMemory;     // last avoidance bias dir (XZ)
    private float _avoidMemoryT;      // time remaining for memory bias
    private bool _avoidHitThisFrame;  // any avoidance ray hit this frame
    private float _lastLeftClear, _lastRightClear;

    // Stuck detection & escape
    private Vector3 _prevPos;
    private float _stuckTimer;
    private float _escapeTimer;
    private float _escapeCooldownT;
    private int _escapeDir = 1; // +1 = turn right, -1 = turn left

    // Events (optional)
    public event Action OnAttackStart;
    public event Action OnAttackEnd;

    // ─────────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        _tr = transform;

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null) _rigidbody = GetComponentInChildren<Rigidbody>();
        }

        _state = STATE.Slither;
        _stateTimer = 0f;
        _currentSpeed = 0f;
        _prevPos = _tr.position;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _currentSpeed = 0f;
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MAIN UPDATE (DRIVEN BY ManagerMobMovement)
    // ─────────────────────────────────────────────────────────────────────────────
    public override void UpdateMovement(float dt)
    {
        if (!_isEnabled || !IsMovementEnabled()) return;

        var target = GetTarget();
        Vector3 toTarget = Vector3.zero;
        float distToTarget = 0f;

        if (target != null)
        {
            toTarget = target.position - _tr.position;
            if (_lockY) toTarget.y = 0f;
            distToTarget = toTarget.magnitude;
        }

        _debugDist = distToTarget;

        switch (_state)
        {
            case STATE.Slither:
                HandleSlither(dt, target, toTarget, distToTarget);
                TryStartAttack(target, distToTarget);
                break;

            case STATE.AttackWindup:
                HandleAttackWindup(dt, target, toTarget);
                break;

            case STATE.AttackLunge:
                HandleAttackLunge(dt, target, toTarget);
                break;

            case STATE.Cooldown:
                HandleCooldown(dt);
                break;
        }

        // Stuck detection & escape bias
        UpdateStuck(dt);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // SLITHER / ORBIT / AVOID
    // ─────────────────────────────────────────────────────────────────────────────
    private void HandleSlither(float dt, Transform target, Vector3 toTarget, float dist)
    {
        Vector3 desiredDir;
        if (target == null || dist > _orbitRadius * 1.25f)
        {
            desiredDir = (target != null && dist > 0.001f) ? (toTarget / dist) : _tr.forward;
        }
        else
        {
            Vector3 n = (dist > 0.001f) ? (toTarget / dist) : _tr.forward;
            Vector3 tangent = new Vector3(-n.z, 0f, n.x) * _orbitDirection;
            float radialBias = 1f - _orbitTangentBias;
            float sign = (dist < _orbitRadius) ? -1f : +1f;
            Vector3 radial = n * sign;
            desiredDir = (tangent * _orbitTangentBias + radial * radialBias);
            if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();
            else desiredDir = _tr.forward;
        }

        // Slither sway
        _slitherPhase += (Mathf.PI * 2f) * _slitherFrequency * dt;
        float sway = Mathf.Sin(_slitherPhase) * _slitherAmplitude;
        Vector3 right = Vector3.Cross(Vector3.up, _tr.forward);
        Vector3 steerDir = desiredDir + (right.normalized * (sway * 0.15f));
        steerDir.y = 0f;
        if (steerDir.sqrMagnitude > 0.0001f) steerDir.Normalize();

        // Avoidance + smoothing + escape
        steerDir = ComputeSteerDir(steerDir, dt);
        YawToward(steerDir, _turnSpeed, dt);

        // Accelerate to cruise
        ApplyAcceleration(_speed, dt);

        // Move forward
        MoveForward(_currentSpeed * dt);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ATTACK STATE MACHINE
    // ─────────────────────────────────────────────────────────────────────────────
    private void TryStartAttack(Transform target, float dist)
    {
        if (target == null) return;
        if (dist < _attackRange.x || dist > _attackRange.y) return;

        // LOS origin at configurable height
        Vector3 origin = _tr.position;
        if (_lockY) origin.y = 0f;
        origin.y += _losRayHeight;

        Vector3 dir = target.position - _tr.position;
        if (_lockY) dir.y = 0f;
        float d = dir.magnitude;
        if (d < 0.001f) return;
        dir /= d;

        if (Physics.Raycast(origin, dir, d, _lineOfSightMask, QueryTriggerInteraction.Ignore))
            return; // blocked

        _state = STATE.AttackWindup;
        _stateTimer = _attackWindupTime;
        _currentSpeed = 0f;
        OnAttackStart?.Invoke();
    }

    private void HandleAttackWindup(float dt, Transform target, Vector3 toTarget)
    {
        _stateTimer -= dt;

        Vector3 dir = (target != null) ? toTarget : _tr.forward;
        if (_lockY) dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        Vector3 steerDir = ComputeSteerDir(dir, dt);  // avoidance while attacking
        YawToward(steerDir, _attackTurnSpeed, dt);

        // Brief coil (no forward accel)
        ApplyAcceleration(0f, dt);
        MoveForward(_currentSpeed * dt);

        if (_stateTimer <= 0f)
        {
            _state = STATE.AttackLunge;
            _stateTimer = _attackDuration;
            _currentSpeed = _attackLungeSpeed;
        }
    }

    private void HandleAttackLunge(float dt, Transform target, Vector3 toTarget)
    {
        _stateTimer -= dt;

        Vector3 dir = (target != null) ? toTarget : _tr.forward;
        if (_lockY) dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        Vector3 steerDir = ComputeSteerDir(dir, dt);  // avoidance while attacking
        YawToward(steerDir, _attackTurnSpeed, dt);

        MoveForward(_currentSpeed * dt);

        if (_stateTimer <= 0f)
        {
            _state = STATE.Cooldown;
            _stateTimer = UnityEngine.Random.Range(_attackCooldownMin, _attackCooldownMax);
            OnAttackEnd?.Invoke();
        }
    }

    private void HandleCooldown(float dt)
    {
        _stateTimer -= dt;
        ApplyAcceleration(_speed, dt);
        MoveForward(_currentSpeed * dt);

        if (_stateTimer <= 0f)
            _state = STATE.Slither;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // AVOIDANCE + MEMORY + ESCAPE WRAPPER
    // ─────────────────────────────────────────────────────────────────────────────
    private Vector3 ComputeSteerDir(Vector3 desiredDir, float dt)
    {
        // Decay previous avoidance memory
        if (_avoidMemoryT > 0f) _avoidMemoryT = Mathf.Max(0f, _avoidMemoryT - dt);

        // Fresh avoidance (sets _avoidHitThisFrame and memory if steering)
        Vector3 steered = ApplyAvoidance(desiredDir);

        // If we didn't hit this frame, apply a fading memory bias to avoid re-clipping the corner
        if (!_avoidHitThisFrame && _avoidMemoryT > 0f)
        {
            float k = Mathf.Clamp01(_avoidMemoryT / Mathf.Max(0.0001f, _avoidHoldTime));
            Vector3 memDir = desiredDir + _avoidMemory * k;
            memDir.y = 0f;
            if (memDir.sqrMagnitude > 0.0001f) memDir.Normalize();
            steered = Vector3.Slerp(steered, memDir, 0.85f * k);
        }

        // Escape turn if active (bias strongly to escape direction)
        if (_escapeTimer > 0f)
        {
            Vector3 escDir = Quaternion.AngleAxis(_escapeAngle * _escapeDir, Vector3.up) * _tr.forward;
            steered = Vector3.Slerp(steered, escDir, 0.85f);
            _escapeTimer -= dt;
            if (_escapeTimer <= 0f) _escapeCooldownT = _escapeCooldown;
        }
        else if (_escapeCooldownT > 0f)
        {
            _escapeCooldownT = Mathf.Max(0f, _escapeCooldownT - dt);
        }

        return steered;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // APPLY AVOIDANCE (wall-slide + memory + hit flags)
    // ─────────────────────────────────────────────────────────────────────────────
    private Vector3 ApplyAvoidance(Vector3 desiredDir)
    {
        _avoidHitThisFrame = false;

        // Ray origin at configurable height
        Vector3 origin = _tr.position;
        if (_lockY) origin.y = 0f;
        origin.y += _avoidProbeHeight;

        Vector3 fwd = _tr.forward;

        bool centerHit = Physics.Raycast(origin, fwd, out var hitC,
            _avoidProbeDistance, _avoidMask, QueryTriggerInteraction.Ignore);

        Quaternion leftRot = Quaternion.AngleAxis(-_avoidSideAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(+_avoidSideAngle, Vector3.up);
        Vector3 leftDir = leftRot * fwd;
        Vector3 rightDir = rightRot * fwd;

        bool leftHit = Physics.Raycast(origin, leftDir, out var hitL,
            _avoidProbeDistance, _avoidMask, QueryTriggerInteraction.Ignore);

        bool rightHit = Physics.Raycast(origin, rightDir, out var hitR,
            _avoidProbeDistance, _avoidMask, QueryTriggerInteraction.Ignore);

        _lastLeftClear = leftHit ? hitL.distance : _avoidProbeDistance;
        _lastRightClear = rightHit ? hitR.distance : _avoidProbeDistance;

        Vector3 steered = desiredDir;
        bool steeredThisFrame = false;

        if (centerHit)
        {
            _avoidHitThisFrame = true;

            if (_enableWallSlide)
            {
                // Slide along wall: project desired onto plane orthogonal to wall normal
                Vector3 n = hitC.normal; if (_lockY) n.y = 0f;
                if (n.sqrMagnitude > 1e-6f)
                {
                    Vector3 slide = Vector3.ProjectOnPlane(desiredDir, n);
                    if (slide.sqrMagnitude > 1e-6f)
                    {
                        slide.Normalize();
                        steered = Vector3.Slerp(desiredDir, slide, _avoidTurnInfluence);
                        steeredThisFrame = true;
                    }
                }
            }

            if (!steeredThisFrame)
            {
                // Fallback: lateral away from the more blocked side
                float steerSign = (_lastLeftClear > _lastRightClear) ? -1f : +1f;
                Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir) * steerSign;
                Vector3 s = Vector3.Slerp(desiredDir, lateral.normalized, _avoidTurnInfluence);
                s.y = 0f; if (s.sqrMagnitude > 1e-6f) s.Normalize();
                steered = s; steeredThisFrame = true;
            }
        }
        else if (leftHit != rightHit)
        {
            _avoidHitThisFrame = true;
            float steerSign = leftHit ? +1f : -1f; // steer away from the hit side
            Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir) * steerSign;
            Vector3 s = Vector3.Slerp(desiredDir, lateral.normalized, _avoidTurnInfluence);
            s.y = 0f; if (s.sqrMagnitude > 1e-6f) s.Normalize();
            steered = s; steeredThisFrame = true;
        }

        // Record memory bias when we actually steered
        if (steeredThisFrame)
        {
            Vector3 bias = steered - desiredDir;
            bias.y = 0f;
            if (bias.sqrMagnitude > 1e-6f) _avoidMemory = bias.normalized;
            _avoidMemoryT = _avoidHoldTime;
        }

        if (_debugDraw)
        {
            Debug.DrawRay(origin, fwd * _avoidProbeDistance, centerHit ? Color.red : Color.green);
            Debug.DrawRay(origin, leftDir * _avoidProbeDistance, leftHit ? Color.red : Color.green);
            Debug.DrawRay(origin, rightDir * _avoidProbeDistance, rightHit ? Color.red : Color.green);
            Debug.DrawRay(origin, steered * 3f, Color.cyan);
        }

        return steered;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // STUCK DETECTION & ESCAPE
    // ─────────────────────────────────────────────────────────────────────────────
    private void UpdateStuck(float dt)
    {
        // Measure progress since last frame
        Vector3 now = _tr.position; if (_lockY) now.y = 0f;
        Vector3 prev = _prevPos; if (_lockY) prev.y = 0f;
        float moved = (now - prev).magnitude;
        _prevPos = now;

        // Build stuck timer only when avoidance is involved
        if (_avoidHitThisFrame && moved < _stuckEpsilon)
        {
            _stuckTimer += dt;
        }
        else
        {
            // small decay to avoid flicker
            _stuckTimer = Mathf.Max(0f, _stuckTimer - dt * 0.5f);
        }

        // Trigger escape if needed
        if (_stuckTimer >= _stuckTime && _escapeTimer <= 0f && _escapeCooldownT <= 0f)
        {
            // Pick the freer side if we have it; otherwise random
            _escapeDir = (_lastLeftClear > _lastRightClear) ? -1 : +1;
            if (Mathf.Abs(_lastLeftClear - _lastRightClear) < 0.01f)
                _escapeDir = (UnityEngine.Random.value < 0.5f) ? -1 : +1;

            _escapeTimer = _escapeDuration;
            _stuckTimer = 0f;
        }

        // Cooldown handled in ComputeSteerDir()
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MOVEMENT HELPERS
    // ─────────────────────────────────────────────────────────────────────────────
    private void YawToward(Vector3 dir, float degPerSec, float dt)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 fwd = _tr.forward;
        fwd.y = 0f; dir.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = dir;
        fwd.Normalize(); dir.Normalize();

        float angle = Vector3.SignedAngle(fwd, dir, Vector3.up);
        float step = Mathf.Clamp(angle, -degPerSec * dt, degPerSec * dt);
        if (Mathf.Abs(step) > 0.0001f)
        {
            Quaternion q = Quaternion.AngleAxis(step, Vector3.up);
            if (_rigidbody != null) _rigidbody.MoveRotation(q * _rigidbody.rotation);
            else _tr.rotation = q * _tr.rotation;
        }
    }

    private void ApplyAcceleration(float targetSpeed, float dt)
    {
        float accel = (targetSpeed > _currentSpeed) ? _acceleration : _deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accel * dt);
    }

    private void MoveForward(float distance)
    {
        if (Mathf.Abs(distance) < 0.00001f) return;
        Vector3 delta = _tr.forward * distance;
        if (_lockY) delta.y = 0f;

        if (_rigidbody != null) _rigidbody.MovePosition(_rigidbody.position + delta);
        else _tr.position += delta;

        if (_lockY)
        {
            Vector3 p = (_rigidbody != null) ? _rigidbody.position : _tr.position;
            p.y = 0f;
            if (_rigidbody != null) _rigidbody.position = p; else _tr.position = p;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_debugDraw) return;
        var t = transform;

        // Use the same avoidance height for debug rays
        Vector3 o = t.position;
        if (_lockY) o.y = 0f;
        o.y += _avoidProbeHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(o, o + t.forward * 2f);

        // Avoid probes
        Vector3 fwd = t.forward;
        Vector3 left = Quaternion.AngleAxis(-_avoidSideAngle, Vector3.up) * fwd;
        Vector3 right = Quaternion.AngleAxis(+_avoidSideAngle, Vector3.up) * fwd;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, o + fwd * _avoidProbeDistance);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(o, o + left * _avoidProbeDistance);
        Gizmos.DrawLine(o, o + right * _avoidProbeDistance);

        // Orbit radius in ground plane
        Vector3 center = t.position; center.y = 0f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, _orbitRadius);
    }
#endif

    // Sanity clamps (optional)
    private void OnValidate()
    {
        if (_attackRange.y < _attackRange.x) _attackRange.y = _attackRange.x;
        if (_orbitDirection == 0) _orbitDirection = 1;
    }
}
