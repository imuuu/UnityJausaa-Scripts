using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[DefaultExecutionOrder(-10)]
public sealed class SnakeIntroSequence : MonoBehaviour
{
    private enum Phase { Idle, Diving, ImpactTailFlop, Burrowing, PullThrough, Finished }
    private enum AutoOrder { Hierarchy, DistanceFromHead }

    // ----------------------------- REFERENCES -----------------------------
#if ODIN_INSPECTOR
    [TitleGroup("References")]
#endif
    [SerializeField, Tooltip("Main movement/controller to enable after the intro.")]
    private MonoBehaviour _snakeMovement; // implements IMovement

    [SerializeField, Tooltip("Body controller (has SetLockY/GetLockY/SetSuspended/AdoptExternalPathOldestFirst).")]
    private MonoBehaviour _snakeBodyController;

    [SerializeField] private Transform _head;
    [SerializeField, Tooltip("All segments, Head (0) -> Tail (last). If empty/one, rest auto-collected.")]
    private List<Transform> _segmentsHeadToTail = new();

    // ----------------------------- REPLAY / START -----------------------------
#if ODIN_INSPECTOR
    [TitleGroup("Start Options")]
#endif
    [SerializeField] private bool _autoStartOnPlay = true;
    [SerializeField, Tooltip("Allow calling StartIntro again (not concurrently).")] private bool _allowReplay = true;

#if ODIN_INSPECTOR
    [Button("Start Intro (Now)"), DisableInEditorMode]
#endif
    public void StartIntro() => StartIntroInternal(useManualTarget: _useManualTargetXZ, manualXZ: _manualTargetXZ);

#if ODIN_INSPECTOR
    [Button("Restart Intro (Cancel if Playing)"), DisableInEditorMode]
#endif
    public void RestartIntro() { StopIntroImmediate(); StartIntro(); }

    public void StartIntroAt(Vector3 worldTargetXZ) { StartIntroInternal(true, worldTargetXZ); }

    // ----------------------------- AUTO COLLECT -----------------------------
#if ODIN_INSPECTOR
    [TitleGroup("Auto Collect"), FoldoutGroup("Auto Collect/Settings")]
#endif
    [SerializeField] private bool _autoCollectSegments = true;
    [SerializeField] private Transform _segmentsContainer;
    [SerializeField] private AutoOrder _autoOrder = AutoOrder.Hierarchy;
    [SerializeField] private string _segmentNameFilter = "";
    [SerializeField, Min(0)] private int _autoCollectMinChildren = 2;
    [SerializeField, Min(0f)] private float _autoCollectWaitTimeout = 1.5f;

#if ODIN_INSPECTOR
    [FoldoutGroup("Auto Collect/Utilities"), Button("Recollect Now (Play Mode)"), DisableInEditorMode]
#endif
    private void RecollectNow() { if (CollectSegmentsImmediate()) RefreshPhysicsCaches(); }

    // ----------------------------- WORLD / TARGET -----------------------------
#if ODIN_INSPECTOR
    [TitleGroup("World")]
#endif
    [SerializeField] private bool _useFlatGroundYZero = true;
    [SerializeField] private Transform _impactTarget;

    [SerializeField, Tooltip("Manual XZ target for impact/emerge (y ignored).")]
    private bool _useManualTargetXZ = false;
    [SerializeField] private Vector3 _manualTargetXZ = new Vector3(5, 0, 12);

    [SerializeField] private bool _enableAutoStartTestPosition = false;
    [SerializeField] private Vector3 _autoStartTestPosition = new Vector3(0, 40, 0);

    [SerializeField] private LayerMask _groundMask = ~0;

    // ----------------------------- DIVE -----------------------------
#if ODIN_INSPECTOR
    [TitleGroup("Cinematic"), FoldoutGroup("Cinematic/Dive")]
#endif
    [SerializeField, Range(45f, 89.9f), Tooltip("Steepness of entry. 90 = straight down.")]
    private float _entryAngleDeg = 80f;

    [SerializeField, Range(0f, 360f), Tooltip("Yaw if no manual target/impactTarget. 0 = +Z.")]
    private float _entryYaw = 0f;

    [SerializeField, Min(0.1f), Tooltip("Legacy constant dive speed if acceleration is disabled.")]
    private float _diveSpeed = 25f;

    [SerializeField, Min(0f), Tooltip("How deep head penetrates below ground at impact.")]
    private float _penetrationDepth = 0.5f;

    [SerializeField, Tooltip("Easing for legacy dive [0..1] (used only if acceleration is OFF).")]
    private AnimationCurve _diveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Dive Acceleration")]
    [SerializeField, Tooltip("If true, use physics-like acceleration toward the ground.")]
    private bool _useAcceleratedDive = true;

    [SerializeField, Min(0f), Tooltip("Initial speed at dive start (m/s).")]
    private float _diveStartSpeed = 0f;

    [SerializeField, Min(0f), Tooltip("Acceleration along the dive path (m/s²).")]
    private float _diveAcceleration = 90f;

    [SerializeField, Min(0f), Tooltip("Cap terminal speed (m/s). 0 = uncapped.")]
    private float _diveMaxSpeed = 0f;

    [SerializeField, Min(0.01f), Tooltip("Minimum time the dive should visually take, even if very short.")]
    private float _diveMinDuration = 0.15f;

    // ----------------------------- IMPACT -----------------------------
#if ODIN_INSPECTOR
    [FoldoutGroup("Cinematic/Impact")]
#endif
    [SerializeField, Min(0), Tooltip("How many tail segments go dynamic for the flop.")]
    private int _flopSegmentsFromTail = 6;
    [SerializeField, Min(0f)] private float _flopDuration = 0.6f;
    [SerializeField, Min(0f)] private float _flopExtraDownImpulse = 2.5f;
    [SerializeField, Min(0f)] private float _impactPause = 0.15f;

    // --- Stiff, colliding flop rig ---
    [Header("Cinematic/Impact Physics")]
    [SerializeField, Tooltip("Add ConfigurableJoints (stiff) instead of springs.")]
    private bool _useConfigurableJoint = true;

    [SerializeField, Tooltip("Also add collider+kinematic RB to a few neighbors toward the head.")]
    private int _flopNeighborKinematicColliders = 2;

    [SerializeField, Tooltip("Allow a jointed pair to collide (can jitter).")]
    private bool _jointEnableCollision = false;

    [SerializeField, Min(0f), Tooltip("Joint position spring (very stiff).")]
    private float _jointPosSpring = 12000f;

    [SerializeField, Min(0f), Tooltip("Joint position damper.")]
    private float _jointPosDamper = 900f;

    [SerializeField, Min(0f), Tooltip("Joint rotation spring.")]
    private float _jointRotSpring = 800f;

    [SerializeField, Min(0f), Tooltip("Joint rotation damper.")]
    private float _jointRotDamper = 60f;

    [SerializeField, Tooltip("Capsule collider radius (if 0 => auto from spacing).")]
    private float _flopColliderRadius = 0.0f;

    [SerializeField, Tooltip("Capsule length as fraction of segmentSpacing.")]
    private float _flopColliderLengthFactor = 0.9f;

    // ----------------------------- BURROW -----------------------------
#if ODIN_INSPECTOR
    [FoldoutGroup("Cinematic/Burrow")]
#endif
    [SerializeField, Min(0.5f)] private float _emergeForwardDistance = 12f;
    [SerializeField, Min(0.1f)] private float _burrowDepth = 6f;
    [SerializeField] private AnimationCurve _burrowEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField, Min(0.1f)] private float _burrowDuration = 1.4f;
    [SerializeField] private bool _disableCollidersWhileBurrowing = true;

    // ----------------------------- PULL-THROUGH -----------------------------
#if ODIN_INSPECTOR
    [FoldoutGroup("Cinematic/Post-Emerge")]
#endif
    [SerializeField] private bool _pullThroughFullyOut = true;
    [SerializeField, Min(0.1f)] private float _pullThroughSpeed = 10f;
    [SerializeField, Min(0f)] private float _fullyOutExtraDistance = 1f;
    [SerializeField] private bool _failSafeAdvanceIfTailUnderGround = true;
    [SerializeField, Min(0.01f)] private float _failSafeAdvanceTimeout = 1.0f;
    [SerializeField, Min(0f)] private float _groundEpsilon = 0.01f;

    // ----------------------------- FOLLOW -----------------------------
#if ODIN_INSPECTOR
    [FoldoutGroup("Cinematic/Follow")]
#endif
    [SerializeField, Min(0.05f)] private float _segmentSpacing = 0.6f;
    [SerializeField, Min(0.005f)] private float _breadcrumbMinStep = 0.025f;
    [SerializeField, Range(0f, 1f)] private float _lookSlerp = 0.5f;

    // ----------------------------- EVENTS / PREVIEW -----------------------------
#if ODIN_INSPECTOR
    [FoldoutGroup("Events")]
#endif
    public UnityEvent OnDiveStarted, OnImpact, OnBurrowStarted, OnEmerge, OnIntroFinished;

#if ODIN_INSPECTOR
    [TitleGroup("Preview (Edit Mode)")]
#endif
    [SerializeField] private bool _previewInEditMode = true;
    [SerializeField, Range(8, 128)] private int _previewBezierSegments = 48;
    [SerializeField] private bool _previewCombinedPath = true;

    // ----------------------------- RUNTIME STATE -----------------------------
    private Phase _phase = Phase.Idle;
    private Coroutine _runner;
    private bool _isPlaying;

    private readonly List<Vector3> _trail = new(4096);
    private readonly List<float> _trailCumDist = new(4096);
    private Rigidbody[] _rbs;
    private Collider[] _cols;

    private float _groundY;
    private Vector3 _impactPointGround;
    private Vector3 _diveStartPos;

    private bool _lockYSnapshot = false;
    private bool _hasLockYSnapshot = false;

    // --- Flop rig tracking ---
    private sealed class FlopTemp
    {
        public Transform seg;
        public bool rbExisted, colExisted, prevRbExisted;
        public Rigidbody rb, prevRb;
        public Collider colAdded;   // only if we added one
        public Joint joint;         // only if we added one
        public bool dynamic;        // this segment is dynamic during flop
    }
    private readonly List<FlopTemp> _flopTemps = new();
    private readonly HashSet<Transform> _flopDynamic = new();

    private void Start()
    {
        if (_autoStartOnPlay)
        {
            ActionScheduler.RunAfterDelay(1f, () =>
            {
                EnableSnakeMovement(false);
                RecollectNow();
                if (!_enableAutoStartTestPosition)
                {
                    StartIntro();
                }
                else
                {
                    _head.position = _autoStartTestPosition;
                    StartIntroInternal(_useManualTargetXZ, _manualTargetXZ);
                }
            });
        }
    }

    // ----------------------------- PUBLIC CONTROL -----------------------------
    public void StopIntroImmediate()
    {
        if (_runner != null) StopCoroutine(_runner);
        _runner = null; _isPlaying = false; _phase = Phase.Idle;

        // Remove any temporary physics
        TeardownFlopRigs();

        // back to normal
        SetAllCollidersEnabled(true);
        var bc = _snakeBodyController as SnakeBodyController;
        if (bc != null)
        {
            if (_hasLockYSnapshot) bc.SetLockY(_lockYSnapshot);
            bc.SetSuspended(false);
        }
        if (_snakeBodyController) _snakeBodyController.enabled = true;
        EnableSnakeMovement(true);
    }

    private void StartIntroInternal(bool useManualTarget, Vector3 manualXZ)
    {
        if (_isPlaying)
        {
            if (!_allowReplay) return;
            StopIntroImmediate();
        }
        _runner = StartCoroutine(RunIntro(useManualTarget, manualXZ));
    }

    // ----------------------------- MAIN COROUTINE -----------------------------
    private IEnumerator RunIntro(bool useManualTarget, Vector3 manualXZ)
    {
        _isPlaying = true; _phase = Phase.Idle;

        // Suspend BodyController & unlock Y (snapshot)
        var bc = _snakeBodyController as SnakeBodyController;
        if (bc != null)
        {
            _lockYSnapshot = bc.GetLockY();
            _hasLockYSnapshot = true;
            if (_lockYSnapshot) bc.SetLockY(false);
            bc.SetSuspended(true);
        }
        EnableSnakeMovement(false);

        // Auto-collect
        if (_autoCollectSegments && (_segmentsHeadToTail == null || _segmentsHeadToTail.Count <= 1))
        {
            float start = Time.time;
            while (Time.time - start < _autoCollectWaitTimeout) { if (CollectSegmentsImmediate()) break; yield return null; }
            CollectSegmentsImmediate();
        }
        RefreshPhysicsCaches();
        SetAllRigidbodiesKinematic(true);

        // Path setup
        _groundY = _useFlatGroundYZero ? 0f : ResolveGroundYViaRaycastOrTarget();
        _diveStartPos = _head.position;

        Vector3 forwardHeading;
        if (useManualTarget)
        {
            Vector3 headXZ = new Vector3(_head.position.x, 0f, _head.position.z);
            Vector3 targetXZ = new Vector3(manualXZ.x, 0f, manualXZ.z);
            Vector3 dirXZ = (targetXZ - headXZ);
            forwardHeading = dirXZ.sqrMagnitude > 1e-6f ? dirXZ.normalized : ResolveForwardHeading();
            _impactPointGround = new Vector3(manualXZ.x, _groundY, manualXZ.z);
        }
        else
        {
            forwardHeading = ResolveForwardHeading();
            _impactPointGround = PredictImpactXZ(_diveStartPos, forwardHeading, _groundY, _entryAngleDeg);
        }

        Vector3 diveEnd = _impactPointGround + Vector3.down * _penetrationDepth;

        _trail.Clear(); _trailCumDist.Clear();
        AppendTrailPoint(_head.position, true);

        // ---------------- DIVE (accelerated or legacy) ----------------
        _phase = Phase.Diving; OnDiveStarted?.Invoke();

        float diveDist = Vector3.Distance(_diveStartPos, diveEnd);
        if (_useAcceleratedDive)
        {
            // Kinematics along a straight line (distance s)
            float v0 = Mathf.Max(0f, _diveStartSpeed);
            float a = Mathf.Max(0f, _diveAcceleration);
            float vmax = Mathf.Max(0f, _diveMaxSpeed);

            // Edge cases: if both v0 and a are ~0, fall back to legacy speed
            if (a <= 1e-6f && v0 <= 1e-6f) v0 = Mathf.Max(0.01f, _diveSpeed);

            float T;           // total time
            float s_accel = 0; // distance until hit vmax (if capped)
            float t_accel = 0; // time to reach vmax

            if (vmax > 0f && a > 0f && vmax > v0)
            {
                t_accel = (vmax - v0) / a;
                s_accel = v0 * t_accel + 0.5f * a * t_accel * t_accel;

                if (s_accel >= diveDist)
                {
                    // Never reaches vmax; solve quadratic: v0*T + 0.5*a*T^2 = diveDist
                    T = SolveTimeForDistance(v0, a, diveDist);
                }
                else
                {
                    float s_rem = diveDist - s_accel;
                    float t_cruise = s_rem / vmax;
                    T = t_accel + t_cruise;
                }
            }
            else
            {
                // Uncapped or no acceleration to cap
                if (a > 0f) T = SolveTimeForDistance(v0, a, diveDist);
                else T = diveDist / Mathf.Max(0.01f, v0);
            }

            // Enforce minimum duration for readability
            T = Mathf.Max(_diveMinDuration, T);

            Vector3 dir = (diveEnd - _diveStartPos).normalized;
            float t = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                float s;
                if (vmax > 0f && a > 0f && vmax > v0)
                {
                    if (t < t_accel)
                    {
                        s = v0 * t + 0.5f * a * t * t;
                    }
                    else
                    {
                        float s_at_vmax = s_accel;
                        float t_after = t - t_accel;
                        s = s_at_vmax + vmax * t_after;
                    }
                }
                else if (a > 0f)
                {
                    s = v0 * t + 0.5f * a * t * t;
                }
                else
                {
                    s = v0 * t;
                }

                float f = Mathf.Clamp01(s / diveDist);
                MoveHeadAndBody(Vector3.LerpUnclamped(_diveStartPos, diveEnd, f));
                yield return null;
            }
        }
        else
        {
            float diveDuration = Mathf.Max(_diveMinDuration, diveDist / Mathf.Max(0.01f, _diveSpeed));
            for (float t = 0f; t < 1f; t += Time.deltaTime / diveDuration)
            {
                float eased = _diveEase.Evaluate(Mathf.Clamp01(t));
                MoveHeadAndBody(Vector3.LerpUnclamped(_diveStartPos, diveEnd, eased));
                yield return null;
            }
        }
        MoveHeadAndBody(diveEnd);

        // ---------------- IMPACT + stiff flop ----------------
        OnImpact?.Invoke();
        if (_impactPause > 0f) yield return new WaitForSeconds(_impactPause);

        int flopCount = Mathf.Clamp(_flopSegmentsFromTail, 0, Mathf.Max(0, _segmentsHeadToTail.Count - 1));
        if (flopCount > 0)
        {
            SetupFlopNeighborhood(flopCount); // dynamic tail + kinematic neighbors

            _phase = Phase.ImpactTailFlop;
            if (_flopExtraDownImpulse > 0f)
            {
                foreach (var t in _flopTemps) if (t.dynamic && t.rb)
                        t.rb.AddForce(Vector3.down * _flopExtraDownImpulse, ForceMode.VelocityChange);
            }

            if (_flopDuration > 0f) yield return new WaitForSeconds(_flopDuration);

            // Tear down all temp physics before burrow
            TeardownFlopRigs();

            // IMPORTANT: re-prime our breadcrumb trail from current chain pose
            ReprimeTrailFromCurrentChain();
        }

        // ---------------- BURROW ----------------
        _phase = Phase.Burrowing; OnBurrowStarted?.Invoke();
        if (_disableCollidersWhileBurrowing) SetAllCollidersEnabled(false);

        Vector3 forwardFlat = new Vector3(forwardHeading.x, 0f, forwardHeading.z).normalized;
        Vector3 p0 = diveEnd;
        Vector3 p3 = _impactPointGround + forwardFlat * _emergeForwardDistance; p3.y = _groundY;
        Vector3 p1 = p0 + Vector3.down * (_burrowDepth * 0.7f) + forwardFlat * (_emergeForwardDistance * 0.25f);
        Vector3 p2 = p0 + Vector3.down * (_burrowDepth * 0.3f) + forwardFlat * (_emergeForwardDistance * 0.75f);

        float burrowDur = Mathf.Max(0.05f, _burrowDuration);
        for (float t = 0f; t < 1f; t += Time.deltaTime / burrowDur)
        {
            float e = _burrowEase.Evaluate(Mathf.Clamp01(t));
            MoveHeadAndBody(EvaluateCubicBezier(p0, p1, p2, p3, e));
            yield return null;
        }
        MoveHeadAndBody(p3);

        if (_disableCollidersWhileBurrowing) SetAllCollidersEnabled(true);

        // ---------------- PULL THROUGH ----------------
        OnEmerge?.Invoke();

        if (_pullThroughFullyOut)
        {
            _phase = Phase.PullThrough;

            float bodyLen = EstimateBodyLength();
            float travel = bodyLen + _fullyOutExtraDistance;

            Vector3 start = _head.position;
            Vector3 dir = forwardFlat.sqrMagnitude > 1e-6f ? forwardFlat : Vector3.forward;

            float speed = Mathf.Max(0.01f, _pullThroughSpeed);
            float duration = Mathf.Max(0.01f, travel / speed);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float d = Mathf.LerpUnclamped(0f, travel, Mathf.Clamp01(t));
                Vector3 pos = start + dir * d; pos.y = _groundY;
                MoveHeadAndBody(pos);
                yield return null;
            }
            Vector3 finalPos = start + dir * travel; finalPos.y = _groundY;
            MoveHeadAndBody(finalPos);

            if (_failSafeAdvanceIfTailUnderGround && _segmentsHeadToTail.Count > 0)
            {
                float tFail = 0f;
                while (TailBelowGround() && tFail < _failSafeAdvanceTimeout)
                {
                    Vector3 step = dir * speed * Time.deltaTime;
                    Vector3 pos2 = _head.position + step; pos2.y = _groundY;
                    MoveHeadAndBody(pos2);
                    tFail += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // ---------------- HANDOFF ----------------
        if (bc != null)
        {
            float adoptSpacing = Mathf.Max(0.05f, bc.SegmentSpacing * 0.5f);
            bc.AdoptExternalPathOldestFirst(_trail, adoptSpacing, snapSegmentsNow: true);
            if (_hasLockYSnapshot) bc.SetLockY(_lockYSnapshot);
            bc.SetSuspended(false);
        }

        EnableSnakeMovement(true);
        SetAllRigidbodiesKinematic(false);

        OnIntroFinished?.Invoke();

        _isPlaying = false; _phase = Phase.Idle; _runner = null;
    }

    private bool CollectSegmentsImmediate()
    {
        if (!_head) return false;
        Transform container = _segmentsContainer ? _segmentsContainer : _head.parent;
        if (!container) return false;

        if (_autoCollectMinChildren > 0 && container.childCount < _autoCollectMinChildren)
            return false;

        var children = container.GetComponentsInChildren<Transform>(true)
                                .Where(t => t && t != container)
                                .ToList();

        if (!string.IsNullOrEmpty(_segmentNameFilter))
        {
            string f = _segmentNameFilter.ToLowerInvariant();
            children = children.Where(t => t.name.ToLowerInvariant().Contains(f)).ToList();
        }

        children.RemoveAll(t => t == _head);

        if (_autoOrder == AutoOrder.DistanceFromHead)
        {
            Vector3 headPos = _head.position;
            children.Sort((a, b) =>
                Vector3.SqrMagnitude(a.position - headPos).CompareTo(
                Vector3.SqrMagnitude(b.position - headPos)));
        }

        if (_segmentsHeadToTail == null)
            _segmentsHeadToTail = new List<Transform>(children.Count + 1);
        _segmentsHeadToTail.Clear();
        _segmentsHeadToTail.Add(_head);
        _segmentsHeadToTail.AddRange(children);

        return _segmentsHeadToTail.Count >= 1;
    }

    private static float SolveTimeForDistance(float v0, float a, float s)
    {
        // v0*T + 0.5*a*T^2 = s  =>  0.5*a*T^2 + v0*T - s = 0
        if (a <= 1e-6f) return s / Mathf.Max(0.01f, v0);
        float disc = v0 * v0 + 2f * a * s;
        float T = (-v0 + Mathf.Sqrt(Mathf.Max(0f, disc))) / a;
        return Mathf.Max(0f, T);
    }

    private void EnableSnakeMovement(bool enable)
    {
        if (!_snakeMovement) return;
        IMovement m = _snakeMovement as IMovement;
        m?.EnableMovement(enable);
    }

    // ----------------------------- FLOP RIG (stiff & colliding) -----------------------------
    private static void ZeroVel(Rigidbody rb)
    {
        if (!rb) return;

        if (rb.isKinematic) return;

        try
        {
            var prop = typeof(Rigidbody).GetProperty("linearVelocity");
            if (prop != null && prop.CanWrite) prop.SetValue(rb, Vector3.zero, null);
            else rb.linearVelocity = Vector3.zero;
        }
        catch { rb.linearVelocity = Vector3.zero; }
        rb.angularVelocity = Vector3.zero;
    }

    private void SetupFlopNeighborhood(int flopCount)
    {
        _flopTemps.Clear(); _flopDynamic.Clear();

        int last = _segmentsHeadToTail.Count - 1;
        int firstDynamic = Mathf.Max(1, last - flopCount + 1);
        int firstNeighbor = Mathf.Max(1, firstDynamic - _flopNeighborKinematicColliders);

        // Ensure neighbors (kinematic collider only)
        for (int i = firstNeighbor; i < firstDynamic; i++)
            SetupSegmentPhysics(i, dynamicBody: false);

        // Dynamic chain with joints
        for (int i = firstDynamic; i <= last; i++)
            SetupSegmentPhysics(i, dynamicBody: true);
    }

    private void SetupSegmentPhysics(int i, bool dynamicBody)
    {
        if (i <= 0 || i >= _segmentsHeadToTail.Count) return;

        Transform seg = _segmentsHeadToTail[i];
        Transform prev = _segmentsHeadToTail[i - 1];
        if (!seg) return;

        var t = new FlopTemp { seg = seg, dynamic = dynamicBody };

        // Rigidbody
        var rb = seg.GetComponent<Rigidbody>();
        if (rb == null) { rb = seg.gameObject.AddComponent<Rigidbody>(); t.rbExisted = false; }
        else t.rbExisted = true;

        rb.collisionDetectionMode = dynamicBody ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.ContinuousSpeculative;
        rb.mass = dynamicBody ? 0.25f : 1f;
        rb.linearDamping = 0.05f; rb.linearDamping = 0.05f;
        rb.useGravity = dynamicBody;
        rb.isKinematic = !dynamicBody;
        ZeroVel(rb);
        t.rb = rb;

        // Collider (capsule)
        bool hadCol = seg.TryGetComponent<Collider>(out var existingCol);
        if (!hadCol)
        {
            var cap = seg.gameObject.AddComponent<CapsuleCollider>();
            cap.direction = 2; // Z axis
            float radius = (_flopColliderRadius > 0f) ? _flopColliderRadius : Mathf.Max(0.05f, _segmentSpacing * 0.2f);
            cap.radius = radius;
            cap.height = Mathf.Max(radius * 2f + 0.01f, _segmentSpacing * _flopColliderLengthFactor);
            cap.center = new Vector3(0, 0, -cap.height * 0.5f * 0.5f); // slight offset back
            t.colAdded = cap;
        }
        else t.colExisted = true;

        // Joint for dynamic ones
        if (dynamicBody && prev != null)
        {
            // Anchor body
            var prevRb = prev.GetComponent<Rigidbody>();
            if (prevRb == null) { prevRb = prev.gameObject.AddComponent<Rigidbody>(); prevRb.isKinematic = true; t.prevRbExisted = false; }
            else t.prevRbExisted = true;
            t.prevRb = prevRb;

            if (_useConfigurableJoint)
            {
                var cj = seg.gameObject.AddComponent<ConfigurableJoint>();
                cj.connectedBody = prevRb;
                cj.autoConfigureConnectedAnchor = true;
                cj.enableCollision = _jointEnableCollision;
                cj.xMotion = ConfigurableJointMotion.Locked;
                cj.yMotion = ConfigurableJointMotion.Locked;
                cj.zMotion = ConfigurableJointMotion.Locked;
                cj.angularXMotion = ConfigurableJointMotion.Limited;
                cj.angularYMotion = ConfigurableJointMotion.Limited;
                cj.angularZMotion = ConfigurableJointMotion.Limited;

                var l = new SoftJointLimit { limit = 5f }; // small flex
                cj.lowAngularXLimit = new SoftJointLimit { limit = -l.limit };
                cj.highAngularXLimit = l;
                cj.angularYLimit = l;
                cj.angularZLimit = l;

                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0.01f;
                cj.projectionAngle = 1f;

                var pd = new JointDrive { positionSpring = _jointPosSpring, positionDamper = _jointPosDamper, maximumForce = float.MaxValue };
                cj.xDrive = cj.yDrive = cj.zDrive = pd;

                var ad = new JointDrive { positionSpring = _jointRotSpring, positionDamper = _jointRotDamper, maximumForce = float.MaxValue };
                cj.angularXDrive = cj.angularYZDrive = ad;

                t.joint = cj;
            }
            else
            {
                var fj = seg.gameObject.AddComponent<FixedJoint>();
                fj.connectedBody = prevRb;
                fj.enableCollision = _jointEnableCollision;
                fj.breakForce = Mathf.Infinity; fj.breakTorque = Mathf.Infinity;
                t.joint = fj;
            }
        }

        _flopTemps.Add(t);
        if (dynamicBody) _flopDynamic.Add(seg);
    }

    private void TeardownFlopRigs()
    {
        for (int k = _flopTemps.Count - 1; k >= 0; k--)
        {
            var t = _flopTemps[k];
            if (!t.seg) continue;

            if (t.joint) Destroy(t.joint);
            if (t.colAdded) Destroy(t.colAdded);

            if (t.rb)
            {
                ZeroVel(t.rb);
                t.rb.useGravity = false;
                t.rb.isKinematic = true;
                if (!t.rbExisted) Destroy(t.rb);
            }
            if (t.prevRb && !t.prevRbExisted) Destroy(t.prevRb);

            _flopDynamic.Remove(t.seg);
        }
        _flopTemps.Clear();
    }

    // Rebuild the intro trail from the chain's current pose (tail->head polyline)
    private void ReprimeTrailFromCurrentChain()
    {
        if (_segmentsHeadToTail == null || _segmentsHeadToTail.Count == 0) return;

        var poly = new List<Vector3>(_segmentsHeadToTail.Count + 4);
        for (int i = _segmentsHeadToTail.Count - 1; i >= 0; i--)
        {
            var t = _segmentsHeadToTail[i];
            if (t) poly.Add(t.position);
        }
        poly.Add(_head.position);

        float step = Mathf.Max(0.01f, _breadcrumbMinStep);
        var resampled = new List<Vector3>(poly.Count * 2);
        Vector3 prev = poly[0]; resampled.Add(prev);
        float acc = 0f;
        for (int i = 1; i < poly.Count; i++)
        {
            Vector3 a = poly[i - 1];
            Vector3 b = poly[i];
            float len = Vector3.Distance(a, b);
            if (len < 1e-4f) continue;
            Vector3 dir = (b - a) / len;
            float d = 0f;
            while (d + acc < len)
            {
                float tt = d + acc;
                Vector3 p = a + dir * tt;
                resampled.Add(p);
                d += step;
            }
            acc = (acc + len) % step;
        }
        if (resampled.Count == 0 || (resampled[^1] - _head.position).sqrMagnitude > 1e-6f)
            resampled.Add(_head.position);

        _trail.Clear(); _trailCumDist.Clear();
        float cum = 0f;
        _trail.Add(resampled[0]); _trailCumDist.Add(0f);
        for (int i = 1; i < resampled.Count; i++)
        {
            float d = Vector3.Distance(resampled[i - 1], resampled[i]);
            cum += d;
            _trail.Add(resampled[i]);
            _trailCumDist.Add(cum);
        }
    }

    // ----------------------------- HELPERS / FOLLOW -----------------------------
    private void MoveHeadAndBody(Vector3 headPos)
    {
        Vector3 moveDir = (headPos - _head.position);
        _head.position = headPos;
        if (moveDir.sqrMagnitude > 1e-8f)
        {
            var targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            _head.rotation = Quaternion.Slerp(_head.rotation, targetRot, 0.9f);
        }
        AppendTrailPoint(headPos, false);
        PlaceSegmentsAlongTrail();
    }

    private void AppendTrailPoint(Vector3 p, bool force)
    {
        if (_trail.Count == 0) { _trail.Add(p); _trailCumDist.Add(0f); return; }

        float d = (p - _trail[_trail.Count - 1]).magnitude;
        if (force || d >= _breadcrumbMinStep)
        {
            _trail.Add(p);
            _trailCumDist.Add(_trailCumDist[_trailCumDist.Count - 1] + d);

            float needed = Mathf.Max((_segmentsHeadToTail.Count - 1) * _segmentSpacing + 2f, 5f);
            while (_trailCumDist.Count > 2 && (_trailCumDist[^1] - _trailCumDist[0]) > (needed + 5f))
            {
                _trail.RemoveAt(0);
                _trailCumDist.RemoveAt(0);
            }
        }
    }

    private void PlaceSegmentsAlongTrail()
    {
        if (_trail.Count < 2) return;

        float headCum = _trailCumDist[^1];

        for (int i = 0; i < _segmentsHeadToTail.Count; i++)
        {
            var seg = _segmentsHeadToTail[i];
            if (!seg) continue;

            float targetDistBack = i * _segmentSpacing;
            float targetCum = headCum - targetDistBack;

            int idx = _trailCumDist.Count - 1;
            while (idx > 0 && _trailCumDist[idx - 1] > targetCum) idx--;

            if (idx <= 0) { seg.position = _trail[0]; continue; }

            float d2 = _trailCumDist[idx] - _trailCumDist[idx - 1];
            float t = (d2 <= 1e-5f) ? 0f : Mathf.InverseLerp(_trailCumDist[idx - 1], _trailCumDist[idx], targetCum);
            Vector3 pos = Vector3.LerpUnclamped(_trail[idx - 1], _trail[idx], t);
            Vector3 fwd = (_trail[idx] - _trail[idx - 1]);
            if (fwd.sqrMagnitude < 1e-6f) fwd = seg.forward;

            var rb = seg.GetComponent<Rigidbody>();

            // If physics is driving this piece (during flop), let it be.
            if (_phase == Phase.ImpactTailFlop && _flopDynamic.Contains(seg) && rb != null && !rb.isKinematic)
                continue;

            if (rb != null && rb.isKinematic)
            {
                rb.MovePosition(pos);
                var lookRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(seg.rotation, lookRot, _lookSlerp));
            }
            else
            {
                seg.position = pos;
                var lookRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                seg.rotation = Quaternion.Slerp(seg.rotation, lookRot, _lookSlerp);
            }
        }
    }

    private void SetAllRigidbodiesKinematic(bool kinematic)
    {
        if (_rbs == null) return;
        for (int i = 0; i < _rbs.Length; i++)
        {
            var rb = _rbs[i];
            if (!rb) continue;
            ZeroVel(rb);
            rb.useGravity = false;
            rb.isKinematic = kinematic;
        }
    }

    private void SetAllCollidersEnabled(bool enabled)
    {
        if (_cols == null) return;
        for (int i = 0; i < _cols.Length; i++)
        {
            var c = _cols[i];
            if (!c) continue;
            c.enabled = enabled;
        }
    }

    private void RefreshPhysicsCaches()
    {
        var tmpRBs = new List<Rigidbody>(_segmentsHeadToTail.Count);
        var tmpCols = new List<Collider>(_segmentsHeadToTail.Count);
        for (int i = 0; i < _segmentsHeadToTail.Count; i++)
        {
            var t = _segmentsHeadToTail[i];
            if (!t) continue;
            var rb = t.GetComponent<Rigidbody>(); if (rb) tmpRBs.Add(rb);
            var col = t.GetComponent<Collider>(); if (col) tmpCols.Add(col);
        }
        _rbs = tmpRBs.ToArray(); _cols = tmpCols.ToArray();
    }

    private float ResolveGroundYViaRaycastOrTarget()
    {
        if (_useFlatGroundYZero) return 0f;
        if (_impactTarget) return _impactTarget.position.y;
        if (Physics.Raycast(_head.position, Vector3.down, out var hit, 5000f, _groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return 0f;
    }

    private Vector3 ResolveForwardHeading()
    {
        if (_impactTarget)
        {
            Vector3 f = _impactTarget.forward;
            if (f.sqrMagnitude > 1e-4f) return f;
        }
        return Quaternion.Euler(0f, _entryYaw, 0f) * Vector3.forward;
    }

    private static Vector3 PredictImpactXZ(Vector3 headPos, Vector3 forwardHeading, float groundY, float entryAngleDeg)
    {
        Vector3 forwardFlat = new Vector3(forwardHeading.x, 0f, forwardHeading.z).normalized;
        if (forwardFlat.sqrMagnitude < 1e-6f) forwardFlat = Vector3.forward;

        Vector3 axis = Vector3.Cross(Vector3.down, forwardFlat).normalized;
        Vector3 diveDir = Quaternion.AngleAxis(entryAngleDeg, axis) * Vector3.down;
        float denom = diveDir.y;
        float t = (groundY - headPos.y) / denom;
        if (t < 0f) t = 0f;
        Vector3 hit = headPos + diveDir * t;
        return new Vector3(hit.x, groundY, hit.z);
    }

    private static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t; float tt = t * t; float uu = u * u; float uuu = uu * u; float ttt = tt * t;
        return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
    }

    private float EstimateBodyLength()
    {
        float lenBySpacing = Mathf.Max(0, (_segmentsHeadToTail.Count - 1) * _segmentSpacing);
        float measured = 0f;
        for (int i = 1; i < _segmentsHeadToTail.Count; i++)
        {
            var a = _segmentsHeadToTail[i - 1]; var b = _segmentsHeadToTail[i];
            if (a && b) measured += Vector3.Distance(a.position, b.position);
        }
        return Mathf.Max(lenBySpacing, measured);
    }

    private bool TailBelowGround()
    {
        if (_segmentsHeadToTail == null || _segmentsHeadToTail.Count == 0) return false;
        var tail = _segmentsHeadToTail[^1];
        if (!tail) return false;
        return tail.position.y < _groundY - _groundEpsilon;
    }

    // ----------------------------- PREVIEW -----------------------------
    private void OnDrawGizmos()
    {
        if (!_previewInEditMode && !Application.isPlaying) return;
        if (_head == null) return;

        float groundY = _useFlatGroundYZero ? 0f : (_impactTarget ? _impactTarget.position.y : 0f);

        Vector3 forwardHeading;
        Vector3 impactGround;

        if (_useManualTargetXZ)
        {
            Vector3 headXZ = new Vector3(_head.position.x, 0f, _head.position.z);
            Vector3 targetXZ = new Vector3(_manualTargetXZ.x, 0f, _manualTargetXZ.z);
            Vector3 dirXZ = (targetXZ - headXZ);
            forwardHeading = dirXZ.sqrMagnitude > 1e-6f ? dirXZ.normalized : ResolveForwardHeading();
            impactGround = new Vector3(_manualTargetXZ.x, groundY, _manualTargetXZ.z);
        }
        else
        {
            forwardHeading = ResolveForwardHeading();
            impactGround = PredictImpactXZ(_head.position, forwardHeading, groundY, _entryAngleDeg);
        }

        Vector3 diveEnd = impactGround + Vector3.down * _penetrationDepth;

        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Gizmos.DrawLine(impactGround + Vector3.left * 3f, impactGround + Vector3.right * 3f);
        Gizmos.DrawLine(impactGround + Vector3.forward * 3f, impactGround + Vector3.back * 3f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_head.position, diveEnd);
        Gizmos.DrawSphere(impactGround, 0.15f);

        Vector3 forwardFlat = new Vector3(forwardHeading.x, 0f, forwardHeading.z).normalized;
        Vector3 p0 = diveEnd;
        Vector3 p3 = impactGround + forwardFlat * _emergeForwardDistance; p3.y = groundY;
        Vector3 p1 = p0 + Vector3.down * (_burrowDepth * 0.7f) + forwardFlat * (_emergeForwardDistance * 0.25f);
        Vector3 p2 = p0 + Vector3.down * (_burrowDepth * 0.3f) + forwardFlat * (_emergeForwardDistance * 0.75f);

        Gizmos.color = Color.magenta;
        Vector3 prev = p0;
        int segs = Mathf.Max(8, _previewBezierSegments);
        for (int i = 1; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector3 pt = EvaluateCubicBezier(p0, p1, p2, p3, t);
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(p3, 0.18f);

        if (_previewCombinedPath && _pullThroughFullyOut)
        {
            float estLen = Application.isPlaying ? EstimateBodyLength() : Mathf.Max(0, (_segmentsHeadToTail.Count - 1) * _segmentSpacing);
            float travel = estLen + _fullyOutExtraDistance;
            Gizmos.color = new Color(0.6f, 1f, 0.6f, 0.9f);
            Vector3 end = p3 + forwardFlat * travel; end.y = groundY;
            Gizmos.DrawLine(p3, end);
            Gizmos.DrawWireSphere(end, 0.13f);
        }
    }
}
