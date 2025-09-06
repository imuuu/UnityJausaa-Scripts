using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ParabolicMoverOnTrigger : MonoBehaviour
{

    [Header("Events")]
    [SerializeField] private UnityEvent _onPlayerTriggered; // Invoked immediately when Player hits the trigger
    [Header("Target (can be left empty; will be resolved at runtime)")]
    [SerializeField] private Transform _targetB;
    [SerializeField] private bool _autoResolveTargetFromPlayer = true;
    [SerializeField] private bool _resolveFromMainCamera = true;

    [Header("Resolve Paths (fallbacks if no Anchor component is found)")]
    [SerializeField] private string _targetPathUnderMainCamera = "ChestTransformPoint";
    [SerializeField] private string _targetPathFromPlayer = "Camera/ChestTransformPoint";

    [Header("Motion")]
    [SerializeField] private float _flightDuration = 0.12f;     // Very fast hop
    [SerializeField] private float _arcHeight = 1.0f;           // Parabola peak height
    [SerializeField] private AnimationCurve _easing = null;     // Easing over [0..1]
    [SerializeField] private bool _followTargetDuringFlight = true;  // If true, home towards the live target each frame
    [SerializeField] private bool _useUnscaledTime = false;          // Use unscaled time (works during Time.timeScale = 0)

    private enum ArrivalRotationMode
    {
        MatchTarget,            // exactly target.rotation
        MatchTargetPlusOffset,  // target.rotation * offsetEuler
        KeepStartRotation,      // keep initial rotation
        CustomWorldEuler        // use a fixed world rotation from _customArrivalEuler
    }

    [Header("Arrival Rotation")]
    [SerializeField] private ArrivalRotationMode _arrivalRotationMode = ArrivalRotationMode.MatchTarget;
    [SerializeField] private Vector3 _arrivalRotationOffsetEuler = Vector3.zero; // used when MatchTargetPlusOffset
    [SerializeField] private Vector3 _customArrivalEuler = Vector3.zero;         // used when CustomWorldEuler

    [Header("Arrival Animation & Parenting")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _animTrigger = "OnArrived";
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private bool _zeroLocalAfterParent = false;

    private bool _isFlying = false;
    private bool _hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        _easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void Awake()
    {
        if (_easing == null)
            _easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (_animator == null)
            _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_triggerOnce && _hasTriggered) return;
        if (_isFlying) return;

        // 🔔 Invoke the inspector event as soon as the Player enters
        _onPlayerTriggered?.Invoke();

        // Resolve target if not assigned or if auto-resolve is requested
        if (_targetB == null || _autoResolveTargetFromPlayer)
        {
            bool resolved = false;

            if (_resolveFromMainCamera)
                resolved = TryResolveTargetFromMainCamera(out _targetB);

            if (!resolved)
            {
                if (!TryResolveTargetFromPlayerRoot(other, out _targetB))
                {
                    var root = GetPlayerRootTransform(other);
                    _targetB = FindByPath(root, _targetPathFromPlayer);
                }
            }
        }

        if (_targetB == null) return;

        _hasTriggered = true;
        StartCoroutine(FlyToTarget());
    }

    private IEnumerator FlyToTarget()
    {
        _isFlying = true;

        // Cache start pose
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Snapshot end pose (used if _followTargetDuringFlight == false)
        Vector3 snapEndPos = _targetB.position;
        Quaternion snapEndRot = ComputeDesiredArrivalRotation(_targetB);

        float duration = Mathf.Max(0.0001f, _flightDuration);
        float elapsed = 0f;

        // Tight loop; uses scaled or unscaled deltaTime based on setting
        while (elapsed < duration)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);

            // Eased t
            float te = _easing.Evaluate(t);

            // Live target pose if following, otherwise frozen snapshot
            Vector3 endPos = _followTargetDuringFlight ? _targetB.position : snapEndPos;
            Quaternion endRot = _followTargetDuringFlight ? ComputeDesiredArrivalRotation(_targetB) : snapEndRot;

            // Linear move + parabolic world-up offset
            Vector3 pos = Vector3.LerpUnclamped(startPos, endPos, te);
            float parabola = 4f * _arcHeight * te * (1f - te); // peak at t=0.5
            pos += Vector3.up * parabola;

            // Smoothly orient toward the chosen arrival rotation
            Quaternion rot = Quaternion.Slerp(startRot, endRot, te);

            transform.SetPositionAndRotation(pos, rot);

            // Use null yield; this works even during pause if you use unscaled time
            yield return null;
        }

        // Final snap to the exact target pose (respecting rotation mode)
        Vector3 finalPos = _followTargetDuringFlight ? _targetB.position : snapEndPos;
        Quaternion finalRot = _followTargetDuringFlight ? ComputeDesiredArrivalRotation(_targetB) : snapEndRot;
        transform.SetPositionAndRotation(finalPos, finalRot);

        // Parent under target so it follows from now on
        transform.SetParent(_targetB);
        if (_zeroLocalAfterParent)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // Animator trigger
        if (_animator != null && !string.IsNullOrEmpty(_animTrigger))
            _animator.SetTrigger(_animTrigger);

        _isFlying = false;
    }

    // --- Rotation policy ---
    // Computes the desired arrival rotation based on the chosen mode.
    private Quaternion ComputeDesiredArrivalRotation(Transform target)
    {
        switch (_arrivalRotationMode)
        {
            case ArrivalRotationMode.MatchTarget:
                return target.rotation;

            case ArrivalRotationMode.MatchTargetPlusOffset:
                return target.rotation * Quaternion.Euler(_arrivalRotationOffsetEuler);

            case ArrivalRotationMode.KeepStartRotation:
                return transform.rotation; // Note: this is called during flight; caller snapshots this at start too

            case ArrivalRotationMode.CustomWorldEuler:
                return Quaternion.Euler(_customArrivalEuler);

            default:
                return target.rotation;
        }
    }

    // --- Resolution helpers (samat kuin ennen) ---
    private bool TryResolveTargetFromMainCamera(out Transform target)
    {
        target = null;
        var cam = Camera.main;
        if (cam == null) return false;

        var anchor = cam.transform.GetComponentInChildren<MoveTargetAnchor>(true);
        if (anchor != null && anchor.Point != null)
        {
            target = anchor.Point;
            return true;
        }

        target = FindByPath(cam.transform, _targetPathUnderMainCamera);
        return target != null;
    }

    private Transform GetPlayerRootTransform(Collider other)
    {
        var t = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        var root = t.root;
        if (!root.CompareTag("Player"))
        {
            var p = t;
            while (p != null && !p.CompareTag("Player"))
                p = p.parent;
            if (p != null) root = p;
        }
        return root;
    }

    private bool TryResolveTargetFromPlayerRoot(Collider other, out Transform target)
    {
        target = null;
        var playerRoot = GetPlayerRootTransform(other);
        if (playerRoot == null) return false;

        var anchor = playerRoot.GetComponentInChildren<MoveTargetAnchor>(true);
        if (anchor != null && anchor.Point != null)
        {
            target = anchor.Point;
            return true;
        }
        return false;
    }

    private Transform FindByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;
        var current = root;
        var segments = path.Split('/');
        foreach (var seg in segments)
        {
            current = current.Find(seg);
            if (current == null) return null;
        }
        return current;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger)
            col.isTrigger = true;

        if (_flightDuration < 0f) _flightDuration = 0.01f;
        if (_easing == null)
            _easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (_targetB == null) return;
        Gizmos.DrawLine(transform.position, _targetB.position);
        Gizmos.DrawWireSphere(_targetB.position, 0.05f);
    }
#endif
}
