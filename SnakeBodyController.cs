using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// Perf-first snake body: segments follow a distance-sampled path behind the head.
/// Supports fixed prefix/suffix parts and random middle parts.
/// - Flat XZ plane (optional Y lock)
/// - No per-frame allocations; ring-buffer path
/// - Constant segment spacing
/// </summary>
[DefaultExecutionOrder(10)] // after movement
public sealed class SnakeBodyController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────────
    [Title("References")]
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _segmentParent;

    [Title("Length & Spacing")]
    [Tooltip("Desired TOTAL body segment count (not including the final tail). If less than fixed prefix+suffix, the fixed parts win.")]
    [SerializeField, Min(0)] private int _bodySegmentCount = 10;
    [SerializeField, Min(0.05f)] private float _segmentSpacing = 0.6f;

    [Title("Segment Library")]
    [Tooltip("Unique segments placed immediately AFTER the head, in order.")]
    [SerializeField] private GameObject[] _fixedPrefixPrefabs;
    [Tooltip("Pool for filling the middle. A random prefab is chosen for each middle slot.")]
    [SerializeField] private GameObject[] _randomBodyPrefabs;
    [Tooltip("Unique segments placed just BEFORE the final tail, in order.")]
    [SerializeField] private GameObject[] _fixedSuffixPrefabs;
    [Tooltip("Used if random pool is empty; optional fallback.")]
    [SerializeField] private GameObject _defaultBodyPrefab;
    [Tooltip("The very last piece.")]
    [SerializeField] private GameObject _tailPrefab;

    [Title("Randomization")]
    [SerializeField] private bool _deterministic = true;
    [SerializeField, EnableIf(nameof(_deterministic))] private int _randomSeed = 1337;

    [Title("Path Sampling")]
    [InfoBox("Samples are inserted by distance. Smaller spacing = smoother path but more samples.", InfoMessageType.None)]
    [SerializeField, Min(0.05f)] private float _sampleSpacing = 0.3f; // recommend ~0.5x segment spacing
    [SerializeField, Min(0)] private int _extraTailMeters = 2;

    [Title("Smoothing")]
    [SerializeField, Range(0f, 1f)] private float _rotationLerp = 0.25f;
    [SerializeField] private bool _lockY = true;

    public void SetLockY(bool value)
    {
        _lockY = value;
    }

    public bool GetLockY()
    {
        return _lockY;
    }

    [Title("Debug")]
    [SerializeField] private bool _drawPath;
    [SerializeField] private Color _pathColor = new(0, 1, 1, 0.8f);

    // ─────────────────────────────────────────────────────────────────────────────
    // RUNTIME
    // ─────────────────────────────────────────────────────────────────────────────
    private Transform[] _segments;        // all body segments + last tail
    private Vector3[] _segmentTargets;    // per-frame targets (no GC after init)

    private struct Sample { public Vector3 pos; }
    private Sample[] _samples;
    private int _cap, _count, _headIdx;
    private Vector3 _lastHeadPos;

    private Transform _tr;

    // ── Add near other fields ──
    private bool _suspended = false;

    // Optional: expose sample spacing if you want the intro to mirror it
    public float SampleSpacing => _sampleSpacing;

    // ─────────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────────
    public int TotalBodySegments =>
        Mathf.Max(_bodySegmentCount, (_fixedPrefixPrefabs?.Length ?? 0) + (_fixedSuffixPrefabs?.Length ?? 0));

    public float SegmentSpacing => _segmentSpacing;

    [Button("Rebuild Segments")]
    public void Rebuild()
    {
        DestroySegments();
        CreateSegments();
        AllocatePathBuffer();
        PrimePathWithHead();
    }

    public void SetBodySegmentCount(int count)
    {
        count = Mathf.Max(0, count);
        if (count == _bodySegmentCount) return;
        _bodySegmentCount = count;
        Rebuild();
    }

    public void SetSegmentSpacing(float spacing)
    {
        spacing = Mathf.Max(0.05f, spacing);
        if (Mathf.Approximately(spacing, _segmentSpacing)) return;
        _segmentSpacing = spacing;
        _sampleSpacing = Mathf.Min(_sampleSpacing, _segmentSpacing * 0.75f);
        Rebuild();
    }

    // ── Add to PUBLIC API section ──
    public void SetSuspended(bool value)
    {
        _suspended = value;
    }

    /// <summary>
    /// Seed the internal path ring buffer from an external polyline (oldest→newest).
    /// Call while suspended, then unsuspend to continue from that trail.
    /// </summary>
    public void AdoptExternalPathOldestFirst(IList<Vector3> oldestToNewest, float newSampleSpacing, bool snapSegmentsNow = true)
    {
        if (oldestToNewest == null || oldestToNewest.Count == 0) return;

        _sampleSpacing = Mathf.Max(0.01f, newSampleSpacing);

        // Allocate buffer big enough for our usual body length or the incoming samples.
        AllocatePathBufferWithMinCapacity(oldestToNewest.Count + 8);

        _count = 0;
        _headIdx = -1;

        // Feed samples (flattened if _lockY)
        for (int i = 0; i < oldestToNewest.Count; i++)
            PushSample(GetFlat(oldestToNewest[i]));

        // Latest head pos:
        _lastHeadPos = GetFlat(oldestToNewest[oldestToNewest.Count - 1]);

        if (snapSegmentsNow)
        {
            PlaceSegmentsAlongPath();
            ApplySegmentTransforms();
        }
    }

    // ── Put next to AllocatePathBuffer() ──
    private void AllocatePathBufferWithMinCapacity(int minSamples)
    {
        float meters = TotalBodySegments * _segmentSpacing + Mathf.Max(0, _extraTailMeters);
        int baseNeeded = Mathf.CeilToInt(meters / Mathf.Max(0.01f, _sampleSpacing)) + 8;
        int needed = Mathf.Max(16, baseNeeded, minSamples);

        if (_samples == null || _samples.Length != needed)
            _samples = new Sample[needed];

        _cap = needed;
        _count = 0;
        _headIdx = -1;
        _lastHeadPos = GetFlat(_head != null ? _head.position : Vector3.zero);
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _tr = transform;
        if (_head == null) _head = _tr;
        if (_segmentParent == null) _segmentParent = _tr;

        Rebuild();
    }

    private void LateUpdate()
    {
        if (_suspended)
        {                     // ← NEW
#if UNITY_EDITOR
            if (_drawPath) DrawPathGizmos();
#endif
            return;
        }

        if (_head == null || _segments == null || _segments.Length == 0) return;

        SampleHeadPath();
        PlaceSegmentsAlongPath();
        ApplySegmentTransforms();

#if UNITY_EDITOR
        if (_drawPath) DrawPathGizmos();
#endif
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // BUILD / DESTROY
    // ─────────────────────────────────────────────────────────────────────────────
    private int RequiredObjects() => TotalBodySegments + 1; // + final tail

    private void CreateSegments()
    {
        int totalBody = TotalBodySegments;
        int totalObjs = RequiredObjects();

        _segments = new Transform[totalObjs];
        _segmentTargets = new Vector3[totalObjs];

        // Build the ordered prefab list for body
        var bodyList = BuildBodyPrefabOrder(totalBody);

        // Instantiate body segments
        for (int i = 0; i < totalBody; i++)
        {
            GameObject prefab = bodyList[i];
            if (prefab == null) prefab = _defaultBodyPrefab;
            if (prefab == null) prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            var t = Instantiate(prefab, _segmentParent).transform;
            t.name = $"SnakeSegment_{i}";
            _segments[i] = t;
        }

        // Instantiate final tail
        {
            GameObject prefab = _tailPrefab != null ? _tailPrefab : (_defaultBodyPrefab != null ? _defaultBodyPrefab : GameObject.CreatePrimitive(PrimitiveType.Capsule));
            var t = Instantiate(prefab, _segmentParent).transform;
            t.name = "SnakeTail";
            _segments[totalBody] = t;
        }

        PrimeSegmentsAtHead();
    }

    private void DestroySegments()
    {
        if (_segments == null) return;
        for (int i = 0; i < _segments.Length; i++)
            if (_segments[i] != null) DestroyImmediate(_segments[i].gameObject);
        _segments = null;
        _segmentTargets = null;
    }

    /// <summary>
    /// Returns an ordered list of body prefabs = [prefix..., middle(random)..., suffix...].
    /// Length = totalBodySegments.
    /// </summary>
    private GameObject[] BuildBodyPrefabOrder(int totalBody)
    {
        int prefixN = _fixedPrefixPrefabs?.Length ?? 0;
        int suffixN = _fixedSuffixPrefabs?.Length ?? 0;
        int middleN = Mathf.Max(0, totalBody - prefixN - suffixN);

        var result = new GameObject[totalBody];

        // prefix
        for (int i = 0; i < Mathf.Min(prefixN, totalBody); i++)
            result[i] = _fixedPrefixPrefabs[i];

        // middle (random)
        if (middleN > 0)
        {
            var rnd = _deterministic ? new System.Random(_randomSeed) : new System.Random();
            for (int i = 0; i < middleN; i++)
            {
                int idx = prefixN + i;
                if (_randomBodyPrefabs != null && _randomBodyPrefabs.Length > 0)
                {
                    int pick = rnd.Next(0, _randomBodyPrefabs.Length);
                    result[idx] = _randomBodyPrefabs[pick];
                }
                else
                {
                    // fallback to default body prefab (or null—handled later)
                    result[idx] = _defaultBodyPrefab;
                }
            }
        }

        // suffix (anchored to the end)
        for (int i = 0; i < Mathf.Min(suffixN, totalBody); i++)
        {
            int idx = totalBody - suffixN + i;
            if (idx >= 0 && idx < totalBody)
                result[idx] = _fixedSuffixPrefabs[i];
        }

        // Any nulls (e.g., totalBody < prefix+suffix) get filled with defaults
        for (int i = 0; i < totalBody; i++)
            if (result[i] == null) result[i] = _defaultBodyPrefab;

        return result;
    }

    private void PrimeSegmentsAtHead()
    {
        Vector3 hp = GetFlat(_head.position);
        Vector3 look = GetFlat(_head.forward);
        if (look.sqrMagnitude < 1e-6f) look = Vector3.forward;

        var rot = Quaternion.LookRotation(look, Vector3.up);
        for (int i = 0; i < _segments.Length; i++)
        {
            float back = (i + 1) * _segmentSpacing;
            Vector3 p = hp - look * back;
            _segments[i].SetPositionAndRotation(p, rot);
        }
    }

    private void AllocatePathBuffer()
    {
        float meters = TotalBodySegments * _segmentSpacing + Mathf.Max(0, _extraTailMeters);
        int needed = Mathf.CeilToInt(meters / Mathf.Max(0.01f, _sampleSpacing)) + 8;
        needed = Mathf.Max(needed, 16);

        if (_samples == null || _samples.Length != needed)
            _samples = new Sample[needed];

        _cap = needed;
        _count = 0;
        _headIdx = -1;
        _lastHeadPos = GetFlat(_head.position);
    }

    private void PrimePathWithHead()
    {
        Vector3 hp = GetFlat(_head.position);
        int needed = Mathf.Min(_cap, Mathf.CeilToInt(TotalBodySegments * _segmentSpacing / _sampleSpacing) + 1);
        for (int i = 0; i < needed; i++) PushSample(hp);
        _lastHeadPos = hp;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // PATH SAMPLING
    // ─────────────────────────────────────────────────────────────────────────────
    private void SampleHeadPath()
    {
        Vector3 hp = GetFlat(_head.position);
        float dist = Vector3.Distance(hp, _lastHeadPos);

        while (_count == 0 || dist >= _sampleSpacing)
        {
            if (_count > 0)
            {
                Vector3 dir = hp - _lastHeadPos;
                float len = Mathf.Max(1e-6f, dir.magnitude);
                _lastHeadPos += (dir / len) * _sampleSpacing;
                dist -= _sampleSpacing;
                PushSample(_lastHeadPos);
            }
            else
            {
                PushSample(hp);
                _lastHeadPos = hp;
                dist = 0f;
            }
        }
    }

    private void PushSample(Vector3 pos)
    {
        _headIdx = (_headIdx + 1) % _cap;
        _samples[_headIdx].pos = pos;
        if (_count < _cap) _count++;
    }

    private int Wrap(int idx) => (idx < 0) ? (idx + _cap) : (idx >= _cap ? idx - _cap : idx);

    // ─────────────────────────────────────────────────────────────────────────────
    // SEGMENT PLACEMENT
    // ─────────────────────────────────────────────────────────────────────────────
    private void PlaceSegmentsAlongPath()
    {
        if (_count == 0)
        {
            PrimeSegmentsAtHead();
            for (int i = 0; i < _segments.Length; i++) _segmentTargets[i] = _segments[i].position;
            return;
        }

        float nextDist = _segmentSpacing;
        int segIdx = 0;
        int targetCount = _segments.Length;

        int a = _headIdx;
        int b = Wrap(a - 1);
        Vector3 A = _samples[a].pos;
        Vector3 B = _samples[b].pos;
        float accumulated = 0f;

        Vector3 headPos = GetFlat(_head.position);
        float headToA = Vector3.Distance(headPos, A);
        if (headToA > 1e-6f)
        {
            B = A;
            A = headPos;
        }

        while (segIdx < targetCount)
        {
            float edgeLen = Vector3.Distance(A, B);
            if (edgeLen < 1e-5f)
            {
                a = b; b = Wrap(b - 1);
                if (a == b) break;
                A = _samples[a].pos; B = _samples[b].pos;
                continue;
            }

            while (segIdx < targetCount && nextDist <= accumulated + edgeLen)
            {
                float t = Mathf.Clamp01((nextDist - accumulated) / edgeLen);
                _segmentTargets[segIdx] = Vector3.Lerp(A, B, t);
                nextDist += _segmentSpacing;
                segIdx++;
            }

            accumulated += edgeLen;
            a = b; b = Wrap(b - 1);
            if (a == b) break;
            A = _samples[a].pos; B = _samples[b].pos;
        }

        if (segIdx < targetCount)
        {
            Vector3 clampPos = (segIdx > 0) ? _segmentTargets[segIdx - 1] : A;
            for (; segIdx < targetCount; segIdx++) _segmentTargets[segIdx] = clampPos;
        }
    }

    private void ApplySegmentTransforms()
    {
        Vector3 headPos = GetFlat(_head.position);

        for (int i = 0; i < _segments.Length; i++)
        {
            Vector3 p = _segmentTargets[i];
            if (_lockY) p.y = 0f;

            Vector3 ahead = (i == 0) ? headPos : _segmentTargets[i - 1];
            Vector3 dir = ahead - p;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = _segments[i].forward; else dir.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            _segments[i].rotation = (_rotationLerp > 0f)
                ? Quaternion.Slerp(_segments[i].rotation, targetRot, _rotationLerp)
                : targetRot;

            _segments[i].position = p;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // UTILS & DEBUG
    // ─────────────────────────────────────────────────────────────────────────────
    private Vector3 GetFlat(Vector3 v) { if (_lockY) v.y = 0f; return v; }

#if UNITY_EDITOR
    private void DrawPathGizmos()
    {
        if (_count <= 1) return;
        Gizmos.color = _pathColor;

        Vector3 prev = GetFlat(_head.position);
        int a = _headIdx, b = Wrap(a - 1);
        Vector3 A = _samples[a].pos;
        Gizmos.DrawLine(prev, A);

        for (int i = 0; i < _count - 1; i++)
        {
            Vector3 P = _samples[a].pos;
            Vector3 Q = _samples[b].pos;
            Gizmos.DrawLine(P, Q);
            a = b; b = Wrap(b - 1);
        }
    }
#endif
}
