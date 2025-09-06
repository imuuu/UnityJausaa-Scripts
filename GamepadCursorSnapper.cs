using UnityEngine;

/// <summary>
/// Snaps the GamepadCursor's virtual cursor (screen space) to the nearest Rigidbody
/// on specified layers. Only runs while controlling with the gamepad.
/// Does nothing in mouse mode.
/// </summary>
public class GamepadCursorSnapper : MonoBehaviour
{
    [Header("Enable / When")]
    [SerializeField] private bool _enabledSnapping = true;
    [Tooltip("Disable snapping while RT / virtual Mouse0 is held.")]
    [SerializeField] private bool _disableWhileRT = true;

    [Header("Targets")]
    [Tooltip("Layers that are valid snap targets.")]
    [SerializeField] private LayerMask _snapLayers;
    [Tooltip("Physics sphere radius around the ray (meters).")]
    [SerializeField] private float _sphereRadius = 0.35f;
    [Tooltip("Max ray distance (meters).")]
    [SerializeField] private float _maxDistance = 25f;

    [Header("Screen-space gate (px)")]
    [Tooltip("Start snapping if target's screen position is within this radius (pixels).")]
    [SerializeField] private float _captureRadiusPx = 80f;
    [Tooltip("Stop snapping if it moves beyond this radius (hysteresis, pixels).")]
    [SerializeField] private float _exitRadiusPx = 120f;

    [Header("Smoothing")]
    [Tooltip("How quickly the cursor moves toward the snap point (higher = snappier).")]
    [SerializeField] private float _snapLerp = 20f;

    [Header("Debug")]
    [SerializeField] private bool _drawDebug = false;

    // Runtime state
    private Collider _currentTarget;
    private Vector2 _currentTargetScreen;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (!_enabledSnapping) return;
        if (!GamepadCursor.ControllingWithGamepad) return; // only in gamepad mode
        if (_disableWhileRT && GamepadCursor.GamepadMouse0) return; // skip while RT held

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector2 pos = GamepadCursor.CurrentScreenPosition;

        // Find candidates using a sphere cast along the cursor ray
        Ray ray = _cam.ScreenPointToRay(pos);
        var hits = Physics.SphereCastAll(
            ray,
            _sphereRadius,
            _maxDistance,
            _snapLayers,
            QueryTriggerInteraction.Ignore
        );

        // Select nearest in screen space (with hysteresis for the current target)
        Collider bestCol = null;
        Vector2 bestScreen = Vector2.zero;
        float bestDist2 = float.PositiveInfinity;

        float startGate2 = _captureRadiusPx * _captureRadiusPx;
        float keepGate2 = _exitRadiusPx * _exitRadiusPx;

        if (hits != null && hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col.attachedRigidbody == null) continue;

                Vector3 worldP = col.bounds.center;
                Vector3 sp = _cam.WorldToScreenPoint(worldP);
                if (sp.z <= 0f) continue; // behind camera

                Vector2 sp2 = new Vector2(sp.x, sp.y);
                float d2 = (sp2 - pos).sqrMagnitude;

                float gate = (_currentTarget != null && col == _currentTarget) ? keepGate2 : startGate2;
                if (d2 <= gate && d2 < bestDist2)
                {
                    bestDist2 = d2;
                    bestCol = col;
                    bestScreen = sp2;
                }
            }
        }

        // Update target
        if (bestCol != null)
        {
            _currentTarget = bestCol;
            _currentTargetScreen = bestScreen;
        }
        else
        {
            _currentTarget = null;
        }

        // Apply snap (exponential smoothing toward target)
        if (_currentTarget != null)
        {
            Vector2 snapped = Vector2.Lerp(
                pos,
                _currentTargetScreen,
                1f - Mathf.Exp(-_snapLerp * Time.unscaledDeltaTime)
            );

            GamepadCursor.InjectScreenPosition(snapped);
        }

        // Debug visualization
        if (_drawDebug)
        {
            if (_currentTarget != null)
                DebugDrawScreenCircle(_currentTargetScreen, 10f, Color.green);

            DebugDrawScreenCircle(pos, _captureRadiusPx, new Color(1f, 1f, 1f, 0.2f));
        }
    }

    // --- Helpers ---

    private void DebugDrawScreenCircle(Vector2 center, float radius, Color c)
    {
        const int steps = 36;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i <= steps; i++)
        {
            float a = i * Mathf.PI * 2f / steps;
            Vector3 p = new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y + Mathf.Sin(a) * radius,
                0f
            );

            if (i > 0)
            {
                Debug.DrawLine(
                    _cam.ScreenToWorldPoint(prev + Vector3.forward * 1f),
                    _cam.ScreenToWorldPoint(p + Vector3.forward * 1f),
                    c,
                    0f,
                    false
                );
            }
            prev = p;
        }
    }
}
