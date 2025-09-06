using System.Collections.Generic;
using UnityEngine;
// If you use your pooler, keep this using. Otherwise remove it.
// using Game.PoolSystem;

[DefaultExecutionOrder(2)]
public sealed class HydraHeadPlacer : MonoBehaviour
{
    [Header("Prefabs & Parenting")]
    [SerializeField] private GameObject _headPrefab;
    [SerializeField] private Transform _headsParent;   // optional parent for spawned heads
    [SerializeField] private bool _usePool = false;    // set true if you want to use your pooler

    [Header("Ring Layout")]
    [SerializeField] private float _mainRadius = 6f;   // big circle (body)
    [SerializeField] private float _headRadius = 0.9f; // used to keep heads tangent to body
    [SerializeField, Min(1)] private int _count = 5;
    [SerializeField] private bool _useXZPlane = true;  // top-down = true
    [SerializeField] private Vector3 _firstDirection = Vector3.forward; // where head #0 starts
    [SerializeField, Min(0f)] private float _edgeInset = 0.001f;        // tiny safety margin

    [Header("Facing / Aim")]
    [Range(0f, 1f)]
    [SerializeField] private float _forwardBias = 0.2f; // 0 = pure radial out, 1 = face boss forward
    [SerializeField] private bool _updateEveryFrame = false; // re-orient every Update (e.g., if boss rotates)

    [Header("Debug")]
    [SerializeField] private bool _drawDebug = true;
    [SerializeField] private float _debugDuration = 0f; // 0 = one frame

    private readonly List<Transform> _spawned = new();

    public void Awake()
    {
        PlaceHeads();
    }

    // --- Public API ---
    [ContextMenu("Place Heads Now")]
    public void PlaceHeads()
    {
        if (_headPrefab == null)
        {
            Debug.LogWarning("[HydraHeadPlacer] No head prefab assigned.");
            return;
        }

        // Compute positions
        if (!CircleRingPlacer.TryPlaceOnInnerTangentRing(
                transform.position, _mainRadius, _headRadius, _count,
                _firstDirection, _useXZPlane, out var centers, _edgeInset))
        {
            int max = CircleRingPlacer.MaxCountThatFits(_mainRadius, _headRadius);
            Debug.LogWarning($"[HydraHeadPlacer] Cannot fit count={_count}. Max without overlap ≈ {max}.");
            return;
        }

        // Ensure we have exactly _count heads (reuse existing when possible)
        EnsureSpawnedCount(_count);

        // Place + rotate
        for (int i = 0; i < centers.Length; i++)
        {
            Transform t = _spawned[i];
            t.SetPositionAndRotation(centers[i], ComputeHeadRotation(centers[i]));
        }

        // Optional debug
        // if (_drawDebug)
        // {
        //     CircleRingPlacer.DebugDrawLayout(transform.position, _mainRadius, _headRadius,
        //                                      _count, _firstDirection, _useXZPlane,
        //                                      _edgeInset, _debugDuration);
        //     DrawAimArrows(centers);
        // }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_drawDebug)
        {
            CircleRingPlacerGizmos.DrawLayoutGizmos(
                transform.position, _mainRadius, _headRadius,
                _count, _firstDirection, _useXZPlane, _edgeInset);
        }
    }
#endif

    [ContextMenu("Clear Spawned Heads")]
    public void ClearSpawned()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] == null) continue;

            // Return to pool or destroy
            if (_usePool /* && ManagerPrefabPooler.Instance != null */)
            {
                // ManagerPrefabPooler.Instance.ReturnToPool(_spawned[i].gameObject);
                Destroy(_spawned[i].gameObject); // fallback if pooler not shown here
            }
            else
            {
                Destroy(_spawned[i].gameObject);
            }
        }
        _spawned.Clear();
    }

    public void ClearLastHead()
    {
        if (_spawned.Count > 0)
        {
            Transform lastHead = _spawned[_spawned.Count - 1];
            if (lastHead != null)
            {
                if (_usePool /* && ManagerPrefabPooler.Instance != null */)
                {
                    // ManagerPrefabPooler.Instance.ReturnToPool(lastHead.gameObject);
                    Destroy(lastHead.gameObject); // fallback if pooler not shown here
                }
                else
                {
                    Destroy(lastHead.gameObject);
                }
            }
            _spawned.RemoveAt(_spawned.Count - 1);
        }
    }

    private void Update()
    {
        if (!_updateEveryFrame || _spawned.Count == 0) return;

        // Re-position and/or re-orient continuously (e.g., boss moves/rotates)
        if (CircleRingPlacer.TryPlaceOnInnerTangentRing(
                transform.position, _mainRadius, _headRadius, _count,
                _firstDirection, _useXZPlane, out var centers, _edgeInset))
        {
            for (int i = 0; i < centers.Length && i < _spawned.Count; i++)
            {
                Transform t = _spawned[i];
                t.SetPositionAndRotation(centers[i], ComputeHeadRotation(centers[i]));
            }

            if (_drawDebug)
            {
                CircleRingPlacer.DebugDrawLayout(transform.position, _mainRadius, _headRadius,
                                                 _count, _firstDirection, _useXZPlane,
                                                 _edgeInset, _debugDuration);
                DrawAimArrows(centers);
            }
        }
    }

    // --- Helpers ---

    private void EnsureSpawnedCount(int target)
    {
        // Spawn missing
        while (_spawned.Count < target)
        {
            GameObject go;
            if (_usePool /* && ManagerPrefabPooler.Instance != null */)
            {
                // go = ManagerPrefabPooler.Instance.GetFromPool(_headPrefab);
                go = Instantiate(_headPrefab);
                go.transform.SetParent(_headsParent, true);
            }
            else
            {
                if (_headsParent != null) go = Instantiate(_headPrefab, _headsParent);
                else go = Instantiate(_headPrefab);
            }

            // if (_headsParent != null)
            // {
            //     go.transform.SetParent(_headsParent, true);
            // }
            _spawned.Add(go.transform);
        }

        // Remove extras
        for (int i = _spawned.Count - 1; i >= target; i--)
        {
            Transform t = _spawned[i];
            if (t != null)
            {
                if (_usePool /* && ManagerPrefabPooler.Instance != null */)
                {
                    // ManagerPrefabPooler.Instance.ReturnToPool(t.gameObject);
                    Destroy(t.gameObject); // fallback
                }
                else
                {
                    Destroy(t.gameObject);
                }
            }
            _spawned.RemoveAt(i);
        }
    }

    private Quaternion ComputeHeadRotation(Vector3 headPos)
    {
        Vector3 center = transform.position;
        Vector3 planeNormal = _useXZPlane ? Vector3.up : Vector3.forward;

        // Radial-out direction on the chosen plane
        Vector3 radial = (headPos - center);
        radial = Vector3.ProjectOnPlane(radial, planeNormal).normalized;

        // Boss forward projected to plane
        Vector3 fwdRef = Vector3.ProjectOnPlane(transform.forward, planeNormal).normalized;
        if (fwdRef.sqrMagnitude < 1e-6f) fwdRef = radial;

        // Blend: 0 = pure radial; 1 = boss forward
        Vector3 aim = Vector3.Slerp(radial, fwdRef, Mathf.Clamp01(_forwardBias)).normalized;

        // Build rotation
        if (_useXZPlane)
            return Quaternion.LookRotation(aim, Vector3.up);                   // 3D top-down
        else
            return Quaternion.LookRotation(aim, Vector3.forward);              // 2D (XY)
    }

    private void DrawAimArrows(Vector3[] centers)
    {
        Vector3 planeNormal = _useXZPlane ? Vector3.up : Vector3.forward;
        for (int i = 0; i < centers.Length; i++)
        {
            Vector3 headPos = centers[i];
            Vector3 radial = Vector3.ProjectOnPlane(headPos - transform.position, planeNormal).normalized;
            Vector3 fwdRef = Vector3.ProjectOnPlane(transform.forward, planeNormal).normalized;
            if (fwdRef.sqrMagnitude < 1e-6f) fwdRef = radial;
            Vector3 aim = Vector3.Slerp(radial, fwdRef, Mathf.Clamp01(_forwardBias)).normalized;

            Debug.DrawLine(headPos, headPos + aim * (_headRadius * 1.5f), Color.green, _debugDuration);
        }
    }
}
