using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using static BossArenaDefinition;

[DisallowMultipleComponent]
public sealed class BossArenaController : MonoBehaviour
{
    [SerializeField, Required] private BossArenaDefinition _def;
    // [SerializeField, Tooltip("Register this arena instance to ManagerBosses on Awake.")]
    // private bool _registerOnAwake = true;

    private Vector3 _center;
    private readonly List<GameObject> _vfx = new();
    private bool _active;

    private EdgeFogOverlayController _fog;
    private bool _fogInitialized = false;

    // Always reflects the current Transform position.
    // In play mode you can keep using _center for gameplay if desired,
    // but for debug draws we rely on this live value.
    private Vector3 CenterNow => Application.isPlaying ? GetCenter() : transform.position;
  
    private void OnEnable()
    {
        // Keep your center cache if you use it
        // _center = transform.position;

        ActionScheduler.RunNextFrame( () =>
        {
            ManagerBosses.Instance?.RegisterPreplacedArena(this);
        });

    }

    private void OnDisable()
    {
        ManagerBosses.Instance?.UnregisterPreplacedArena(this);
    }

    // ---------------- Activation API ----------------

    public void Activate()
    {
        if (_active) return;
        _active = true;
        ShowVFX();
        InitializeFog();

        if (_def.TeleportPlayerToArenaEdge && Player.Instance != null && !IsInside(Player.Instance.transform.position))
        {
            TeleportPlayerToArena();
        }
    }

    public void Deactivate()
    {
        if (!_active) return;
        _active = false;
        ClearVFX();
        RemoveFog();
    }

    public void Despawn()
    {
        Deactivate();
        Destroy(gameObject);
    }

    // ---------------- Queries ----------------
    private Vector3 Center => transform.position;

    // 2) Change these methods to use Center:
    //
    // GetCenter:
    public Vector3 GetCenter() => Center;
    public BossArenaDefinition GetDefinition() => _def;

    public void TeleportPlayerToArena()
    {
        if (this == null || Player.Instance == null) return;

        Vector3 playerPos = Player.Instance.transform.position;
        Vector3 closestPoint = ClosestPointOnBoundary(playerPos);
        Vector3 direction = (playerPos - GetCenter()).normalized;
        closestPoint += direction * 1f;
        closestPoint.y = 0;

        Player.Instance.transform.position = closestPoint;
        //Log($"Teleported player to arena boundary: {arena.name} @ {closestPoint}");
    }

    public bool IsInside(Vector3 pos)
    {
        switch (_def.Type)
        {
            case ARENA_TYPE.CIRCLE:
                {
                    Vector3 d = pos - GetCenter(); d.y = 0f;
                    return d.sqrMagnitude <= _def.CircleRadius * _def.CircleRadius;
                }
            case ARENA_TYPE.SQUARE:
                {
                    float half = _def.SquareSize * 0.5f;
                    Vector3 lp = pos - GetCenter(); // local (XZ)
                    return Mathf.Abs(lp.x) <= half && Mathf.Abs(lp.z) <= half;
                }
            case ARENA_TYPE.POLYGON:
            default:
                return IsInsidePolygonXZ(pos);
        }
    }

    public Vector3 GetBossSpawnWorldPosition()
    {
        if (_def == null) return transform.position;
        if (_def.BossSpawnPosition == BOSS_SPAWN_POS.ARENA_CENTER)
            return Center;
        Vector2 off = _def.BossSpawnLocalOffsetXZ;
        return transform.TransformPoint(new Vector3(off.x, 0f, off.y));
    }


    // ---------------- Enforcement (call each frame by manager) ----------------

    public void EnforceFor(Transform target, float dt)
    {
        if (!_active || target == null || _def == null) return;

        Vector3 p = target.position;
        if (IsInside(p))
        {
            if (_fog != null && !_fog.gameObject.activeInHierarchy && !_fogInitialized)
            {
                _fog.gameObject.SetActive(true);
            }
            return;
        }

        switch (_def.Rule)
            {
                case OUT_OF_BOUNDS_RULE.Pushback:
                    {
                        Vector3 corrected = ClosestPointOnBoundary(p);
                        Vector3 dir = (corrected - p);
                        float len = dir.magnitude;
                        if (len > 0.001f)
                            target.position += dir.normalized * Mathf.Min(len, 12f * dt);
                        break;
                    }
                case OUT_OF_BOUNDS_RULE.DamageOverTime:
                    {
                        // Hook your damage system here:
                        // var hp = target.GetComponent<IHealth>();
                        // if (hp != null) hp.TakeDamage(_def.BoundsDamagePerSecond * dt, DamageType.Environment);
                        Vector3 corrected = ClosestPointOnBoundary(p);
                        target.position = Vector3.Lerp(p, corrected, 0.5f * dt);
                        break;
                    }
            }
    }

    // ---------------- Internals ----------------

    private bool IsInsidePolygonXZ(Vector3 pos)
    {
        Vector3[] pts = _def.PolygonPoints;
        if (pts == null || pts.Length < 3) return true;

        bool inside = false; float x = pos.x, z = pos.z; int j = pts.Length - 1;
        for (int i = 0; i < pts.Length; i++)
        {
            float xi = GetCenter().x + pts[i].x, zi = GetCenter().z + pts[i].z;
            float xj = GetCenter().x + pts[j].x, zj = GetCenter().z + pts[j].z;
            bool intersect = ((zi > z) != (zj > z)) && (x < (xj - xi) * (z - zi) / ((zj - zi) + 1e-6f) + xi);
            if (intersect) inside = !inside; j = i;
        }
        return inside;
    }

    public Vector3 ClosestPointOnBoundary(Vector3 pos)
    {
        switch (_def.Type)
        {
            case ARENA_TYPE.CIRCLE:
                {
                    Vector3 d = pos - GetCenter(); d.y = 0f;
                    float r = _def.CircleRadius;
                    if (d.sqrMagnitude < 1e-6f) return GetCenter() + Vector3.forward * r;
                    return GetCenter() + d.normalized * r;
                }
            case ARENA_TYPE.SQUARE:
                {
                    float half = _def.SquareSize * 0.5f;
                    Vector3 lp = pos - GetCenter(); lp.y = 0f;
                    float cx = Mathf.Clamp(lp.x, -half, half);
                    float cz = Mathf.Clamp(lp.z, -half, half);
                    float dx = Mathf.Min(Mathf.Abs(cx + half), Mathf.Abs(cx - half));
                    float dz = Mathf.Min(Mathf.Abs(cz + half), Mathf.Abs(cz - half));
                    if (dx < dz) cx = (cx > 0f) ? half : -half;
                    else cz = (cz > 0f) ? half : -half;
                    return GetCenter() + new Vector3(cx, 0f, cz);
                }
            case ARENA_TYPE.POLYGON:
            default:
                return ClosestPointOnPolygonPerimeter(pos);
        }
    }

    private Vector3 ClosestPointOnPolygonPerimeter(Vector3 pos)
    {
        Vector3[] pts = _def.PolygonPoints;
        if (pts == null || pts.Length == 0) return GetCenter();

        float bestSqr = float.PositiveInfinity;
        Vector3 best = GetCenter();

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 a = GetCenter() + new Vector3(pts[i].x, 0f, pts[i].z);
            Vector3 b = GetCenter() + new Vector3(pts[(i + 1) % pts.Length].x, 0f, pts[(i + 1) % pts.Length].z);
            Vector3 ap = pos - a;
            Vector3 ab = (b - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / (ab.sqrMagnitude + 1e-6f));
            Vector3 p = a + ab * t;
            float sqr = (pos - p).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = p; }
        }
        return best;
    }

    private void ShowVFX()
    {
        ClearVFX();
        if (_def.RingPrefab == null) return;

        if (_def.VfxShowType == BossArenaDefinition.VFX_TYPE.SINGLE_EFFECT)
        {
            _vfx.Add(Instantiate(_def.RingPrefab, GetCenter(), Quaternion.identity, transform));
            return;
        }

        int count = Mathf.Max(2, _def.VfxShowCount);
        foreach (Vector3 pos in EnumeratePerimeterPoints(count))
            _vfx.Add(Instantiate(_def.RingPrefab, pos, Quaternion.identity, transform));
    }

    private IEnumerable<Vector3> EnumeratePerimeterPoints(int count)
    {
        switch (_def.Type)
        {
            case ARENA_TYPE.CIRCLE:
                for (int i = 0; i < count; i++)
                {
                    float t = (i / (float)count) * Mathf.PI * 2f;
                    yield return GetCenter() + new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * _def.CircleRadius;
                }
                break;

            case ARENA_TYPE.SQUARE:
                {
                    float half = _def.SquareSize * 0.5f;
                    float perim = _def.SquareSize * 4f;
                    for (int i = 0; i < count; i++)
                    {
                        float d = (i / (float)count) * perim;
                        yield return GetCenter() + PointOnSquarePerimeter(half, d);
                    }
                }
                break;

            case ARENA_TYPE.POLYGON:
            default:
                {
                    Vector3[] pts = _def.PolygonPoints;
                    if (pts == null || pts.Length < 3)
                    {
                        yield return GetCenter();
                        yield break;
                    }

                    // build world points + cumulative lengths
                    List<Vector3> world = new List<Vector3>(pts.Length);
                    float total = 0f;
                    for (int i = 0; i < pts.Length; i++)
                        world.Add(GetCenter() + new Vector3(pts[i].x, 0f, pts[i].z));

                    float[] segLen = new float[pts.Length];
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float len = Vector3.Distance(world[i], world[(i + 1) % world.Count]);
                        segLen[i] = len; total += len;
                    }

                    for (int k = 0; k < count; k++)
                    {
                        float target = (k / (float)count) * total;
                        float acc = 0f;
                        for (int i = 0; i < world.Count; i++)
                        {
                            float nextAcc = acc + segLen[i];
                            if (target <= nextAcc)
                            {
                                float t = Mathf.InverseLerp(acc, nextAcc, target);
                                Vector3 a = world[i];
                                Vector3 b = world[(i + 1) % world.Count];
                                yield return Vector3.Lerp(a, b, t);
                                break;
                            }
                            acc = nextAcc;
                        }
                    }
                }
                break;
        }
    }

    private static Vector3 PointOnSquarePerimeter(float half, float distance)
    {
        float side = half * 2f;
        float perim = side * 4f;
        distance = Mathf.Repeat(distance, perim);

        if (distance < side) return new Vector3(-half + distance, 0f, -half);
        distance -= side;
        if (distance < side) return new Vector3(half, 0f, -half + distance);
        distance -= side;
        if (distance < side) return new Vector3(half - distance, 0f, half);
        distance -= side;
        return new Vector3(-half, 0f, half - distance);
    }

    private void ClearVFX()
    {
        for (int i = 0; i < _vfx.Count; i++)
            if (_vfx[i]) Destroy(_vfx[i]);
        _vfx.Clear();
    }

    private void InitializeFog()
    {
        GameObject go = this.gameObject;
        BossArenaDefinition def = _def;
        _fog = go.GetComponentInChildren<EdgeFogOverlayController>(includeInactive: true);

        if (_fog == null) return;

        if (def.Type == ARENA_TYPE.CIRCLE) _fog.SetCircle(def.CircleRadius);
        else if (def.Type == ARENA_TYPE.SQUARE)
        {
            float halfExtendRadius = def.SquareSize * 0.5f;
            _fog.SetSquare(new Vector2(halfExtendRadius, halfExtendRadius), followRotation: false);
        }

        _fog.gameObject.SetActive(false);
    }

    private void RemoveFog()
    {
        if (_fog != null)
        {
            _fog.gameObject.SetActive(false);
            _fog = null;
        }
    }

    // ---------------- Factory for dynamic arenas ----------------

    public static BossArenaController Spawn(BossArenaDefinition def, Vector3 center, Transform parent)
    {
        Debug.Log($"Spawning BossArena at {center}");
        GameObject go = new GameObject("BossArena");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = center;
        BossArenaController c = go.AddComponent<BossArenaController>();
        c._def = def;
        c._center = center;
        //c._registerOnAwake = false;
        c.Activate();
        return c;
    }

    // =====================================================================
    //                               DEBUG
    // =====================================================================

    [FoldoutGroup("Debug"), SerializeField] private bool _drawDebug = true;
    [FoldoutGroup("Debug"), SerializeField, Tooltip("Draw even when not selected. If false, draws only when selected.")]
    private bool _debugAlways = false;

    [FoldoutGroup("Debug"), SerializeField] private bool _dbgShowBossSpawnPoint = true;
    [FoldoutGroup("Debug"), SerializeField] private float _dbgBossSpawnMarkerSize = 0.3f;

    [FoldoutGroup("Debug/What"), SerializeField] private bool _dbgShowCenter = true;
    [FoldoutGroup("Debug/What"), SerializeField] private bool _dbgShowBoundary = true;
    [FoldoutGroup("Debug/What"), SerializeField] private bool _dbgShowPerimeterSamples = false;
    [FoldoutGroup("Debug/What"), SerializeField, Tooltip("Show where multi VFX would spawn on the perimeter")]
    private bool _dbgShowVfxPoints = false;
    [FoldoutGroup("Debug/What"), SerializeField, Tooltip("Preview lines from outside back to closest boundary point")]
    private bool _dbgShowClosestPointPreview = false;
    [FoldoutGroup("Debug/What"), SerializeField, Tooltip("Preview a proximity radius (for range-activated encounters)")]
    private bool _dbgShowProximityRadius = false;

    [FoldoutGroup("Debug/Style"), SerializeField] private Color _dbgColorBoundary = new Color(1f, 0.5f, 0f, 1f);     // orange
    [FoldoutGroup("Debug/Style"), SerializeField] private Color _dbgColorCenter = new Color(0.2f, 0.9f, 1f, 1f);      // cyan
    [FoldoutGroup("Debug/Style"), SerializeField] private Color _dbgColorSamples = new Color(1f, 0.8f, 0.2f, 1f);     // yellow
    [FoldoutGroup("Debug/Style"), SerializeField] private Color _dbgColorVfx = new Color(0.7f, 0.3f, 1f, 1f);         // purple
    [FoldoutGroup("Debug/Style"), SerializeField] private Color _dbgColorClosest = new Color(1f, 0.2f, 0.2f, 1f);     // red
    [FoldoutGroup("Debug/Style"), SerializeField] private float _dbgVerticalOffset = 0.02f;

    [FoldoutGroup("Debug/Params"), SerializeField, Min(3)] private int _dbgCircleSegments = 64;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(2)] private int _dbgPerimeterSamples = 24;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(0.01f)] private float _dbgVfxMarkerSize = 0.25f;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(0.01f)] private float _dbgCenterMarkerSize = 0.15f;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(0f)] private float _dbgProximityRadius = 12f;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(1)] private int _dbgClosestRays = 12;
    [FoldoutGroup("Debug/Params"), SerializeField, Min(0.1f)] private float _dbgClosestRayDistanceFactor = 1.3f; // how far outside to sample

    private void OnDrawGizmos()
    {
        if (_drawDebug && _debugAlways) DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (_drawDebug && !_debugAlways) DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        if (_def == null) return;

        Vector3 center = CenterNow;               // <= use live transform position
        Vector3 upOff = Vector3.up * _dbgVerticalOffset;

        if (_dbgShowCenter)
        {
            Gizmos.color = _dbgColorCenter;
            Gizmos.DrawSphere(center + upOff, _dbgCenterMarkerSize);
            Gizmos.DrawLine(center + upOff, center + upOff + Vector3.forward * 1.0f);
        }

        if (_dbgShowBossSpawnPoint)
        {
            Gizmos.color = Color.green;
            Vector3 sp = GetBossSpawnWorldPosition();
            Gizmos.DrawSphere(sp + Vector3.up * _dbgVerticalOffset, _dbgBossSpawnMarkerSize);
            Gizmos.DrawLine(sp + Vector3.up * _dbgVerticalOffset, sp + Vector3.up * (_dbgVerticalOffset + 0.75f));
        }


        if (_dbgShowBoundary)
        {
            Gizmos.color = _dbgColorBoundary;
            switch (_def.Type)
            {
                case ARENA_TYPE.CIRCLE:
                    DrawCircleXZ(center + upOff, _def.CircleRadius, _dbgCircleSegments);
                    break;
                case ARENA_TYPE.SQUARE:
                    Gizmos.DrawWireCube(center + upOff, new Vector3(_def.SquareSize, 0.001f, _def.SquareSize));
                    break;
                case ARENA_TYPE.POLYGON:
                default:
                    DrawPolygonXZ(center + upOff, _def.PolygonPoints);
                    break;
            }
        }

        if (_dbgShowPerimeterSamples)
        {
            Gizmos.color = _dbgColorSamples;
            int n = Mathf.Max(2, _dbgPerimeterSamples);
            foreach (Vector3 p in EnumeratePerimeterPoints(n, center)) // <= pass live center
                Gizmos.DrawSphere(p + upOff, _dbgCenterMarkerSize * 0.75f);
        }

        if (_dbgShowVfxPoints)
        {
            Gizmos.color = _dbgColorVfx;
            int count = (_def.VfxShowType == BossArenaDefinition.VFX_TYPE.SINGLE_EFFECT) ? 1 : Mathf.Max(2, _def.VfxShowCount);
            if (count == 1)
                Gizmos.DrawCube(center + upOff, Vector3.one * _dbgVfxMarkerSize);
            else
                foreach (Vector3 p in EnumeratePerimeterPoints(count, center)) // <= pass live center
                    Gizmos.DrawCube(p + upOff, Vector3.one * _dbgVfxMarkerSize);
        }

        if (_dbgShowProximityRadius && _dbgProximityRadius > 0f)
        {
            Gizmos.color = new Color(_dbgColorBoundary.r, _dbgColorBoundary.g, _dbgColorBoundary.b, 0.8f);
            DrawCircleXZ(center + upOff * 2f, _dbgProximityRadius, Mathf.Max(16, _dbgCircleSegments / 2));
        }

        if (_dbgShowClosestPointPreview)
        {
            Gizmos.color = _dbgColorClosest;
            int rays = Mathf.Max(1, _dbgClosestRays);
            float baseDist = GetApproximateRadiusForOutsideSample() * _dbgClosestRayDistanceFactor;

            for (int i = 0; i < rays; i++)
            {
                float t = (i / (float)rays) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t));
                Vector3 start = center + dir * baseDist;                // <= use live center
                Vector3 target = ClosestPointOnBoundary(start);
                Gizmos.DrawLine(start + upOff, target + upOff);
                Gizmos.DrawSphere(target + upOff, _dbgCenterMarkerSize * 0.6f);
            }
        }
    }

    private float GetApproximateRadiusForOutsideSample()
    {
        switch (_def.Type)
        {
            case ARENA_TYPE.CIRCLE: return Mathf.Max(1f, _def.CircleRadius);
            case ARENA_TYPE.SQUARE: return Mathf.Max(1f, _def.SquareSize * 0.75f);
            case ARENA_TYPE.POLYGON:
                {
                    float max = 1f;
                    Vector3[] pts = _def.PolygonPoints;
                    if (pts != null && pts.Length > 0)
                    {
                        for (int i = 0; i < pts.Length; i++)
                            max = Mathf.Max(max, pts[i].magnitude);
                    }
                    return max;
                }
        }
        // fallback
        return 5f;
    }

    private static void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        segments = Mathf.Max(3, segments);
        float step = Mathf.PI * 2f / segments;
        Vector3 prev = center + new Vector3(Mathf.Cos(0f), 0f, Mathf.Sin(0f)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = step * i;
            Vector3 curr = center + new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * radius;
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }

    private static void DrawPolygonXZ(Vector3 center, Vector3[] pts)
    {
        if (pts == null || pts.Length < 2) return;
        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 a = center + new Vector3(pts[i].x, 0f, pts[i].z);
            Vector3 b = center + new Vector3(pts[(i + 1) % pts.Length].x, 0f, pts[(i + 1) % pts.Length].z);
            Gizmos.DrawLine(a, b);
        }
    }

    // Keep your original EnumeratePerimeterPoints(int) for runtime if you like,
    // but add this overload and use it from the gizmos:

    private IEnumerable<Vector3> EnumeratePerimeterPoints(int count, Vector3 center)
    {
        switch (_def.Type)
        {
            case ARENA_TYPE.CIRCLE:
                for (int i = 0; i < count; i++)
                {
                    float t = (i / (float)count) * Mathf.PI * 2f;
                    yield return center + new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * _def.CircleRadius;
                }
                break;

            case ARENA_TYPE.SQUARE:
                {
                    float half = _def.SquareSize * 0.5f;
                    float perim = _def.SquareSize * 4f;
                    for (int i = 0; i < count; i++)
                    {
                        float d = (i / (float)count) * perim;
                        yield return center + PointOnSquarePerimeter(half, d);
                    }
                    break;
                }

            case ARENA_TYPE.POLYGON:
            default:
                {
                    Vector3[] pts = _def.PolygonPoints;
                    if (pts == null || pts.Length < 3) { yield return center; yield break; }

                    // Build world points + cumulative lengths using the provided center
                    List<Vector3> world = new List<Vector3>(pts.Length);
                    float total = 0f;
                    for (int i = 0; i < pts.Length; i++)
                        world.Add(center + new Vector3(pts[i].x, 0f, pts[i].y)); // Vector2.y => Z

                    float[] segLen = new float[pts.Length];
                    for (int i = 0; i < pts.Length; i++)
                    {
                        float len = Vector3.Distance(world[i], world[(i + 1) % world.Count]);
                        segLen[i] = len; total += len;
                    }

                    for (int k = 0; k < count; k++)
                    {
                        float target = (k / (float)count) * total;
                        float acc = 0f;
                        for (int i = 0; i < world.Count; i++)
                        {
                            float nextAcc = acc + segLen[i];
                            if (target <= nextAcc)
                            {
                                float t = Mathf.InverseLerp(acc, nextAcc, target);
                                Vector3 a = world[i];
                                Vector3 b = world[(i + 1) % world.Count];
                                yield return Vector3.Lerp(a, b, t);
                                break;
                            }
                            acc = nextAcc;
                        }
                    }
                    break;
                }
        }
    }

}
