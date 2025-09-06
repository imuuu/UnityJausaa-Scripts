using System.Threading.Tasks;
using System;
using Game.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PoisonPoolMorph : MonoBehaviour, IWidth
{
    // -------------------- Group IDs (BoxGroup only) --------------------
    private const string G_PREVIEW = "Debug Preview";
    private const string G_COLORS = "Debug Colors";

    [Header("Shape Settings")]
    [Range(3, 128)][SerializeField] private int _segments = 32;

    [Tooltip("Base radius of the pool before noise")]
    [SerializeField] private float _radius = 1f;

    [Tooltip("Amount of Perlin noise distortion")]
    [Range(0f, 1f)][SerializeField] private float _randomness = 0.3f;

    [Tooltip("Scale of the Perlin noise (bigger = larger blobs)")]
    [SerializeField] private float _noiseScale = 1f;

    [Header("Fill Control")]
    [Tooltip("0 = gone, 1 = full size")]
    [Range(0f, 1f)][SerializeField] private float _fill = 1f;

    [Tooltip("If true, scale the transform instead of morphing the mesh")]
    [SerializeField] private bool _scaleWithFill = false;

    [Header("Randomization Settings")]
    [Tooltip("Enable randomizing shape parameters when enabled")]
    [SerializeField] private bool _randomize = false;
    [SerializeField] private bool _generateOnce = true;

    [ShowIf(nameof(_randomize))]
    [MinMaxSlider(1f, 20f)]
    [LabelText("Radius Range")]
    [SerializeField] private Vector2 _radiusRange = new Vector2(1f, 2f);

    [ShowIf(nameof(_randomize))]
    [MinMaxSlider(0f, 1f)]
    [LabelText("Randomness Range")]
    [SerializeField] private Vector2 _randomnessRange = new Vector2(0f, 0.3f);

    [ShowIf(nameof(_randomize))]
    [MinMaxSlider(0f, 10f)]
    [LabelText("NoiseScale Range")]
    [SerializeField] private Vector2 _noiseScaleRange = new Vector2(0.5f, 1f);

    // ---------------------------- DEBUG / PREVIEW ----------------------------
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _drawDebug = true;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _drawInEditMode = true;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _drawVertices = false;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _drawTriangles = false;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _drawRangeRings = true;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _showFillOutline = true;
    [BoxGroup(G_PREVIEW)][SerializeField] private bool _previewRandomizedShape = true;
    [BoxGroup(G_PREVIEW), ShowIf(nameof(_previewRandomizedShape))]
    [SerializeField] private int _previewSeed = 1337;

    [BoxGroup(G_COLORS)][SerializeField] private Color _colBase = new Color(1f, 1f, 1f, 0.25f);
    [BoxGroup(G_COLORS)][SerializeField] private Color _colCurrent = new Color(0.2f, 1f, 0.5f, 0.9f);
    [BoxGroup(G_COLORS)][SerializeField] private Color _colFill = new Color(1f, 0.85f, 0.2f, 0.9f);
    [BoxGroup(G_COLORS)][SerializeField] private Color _colRandom = new Color(0.2f, 0.7f, 1f, 0.9f);
    [BoxGroup(G_COLORS)][SerializeField] private Color _colRange = new Color(1f, 0.3f, 0.2f, 0.35f);

    // ------------------------------------------------------------------------
    private Mesh _mesh;
    private Vector3[] _baseVerts;
    private float _lastFill = -1f;
    private bool _isUpdating = false;

    private SimpleTimer _timer = new SimpleTimer(0.3f);
    private bool _isRadiusSet = false;

    public float Width => GetRadius() * 2f;

    private bool _initialized = false;

    void OnEnable()
    {
        if (_initialized && _generateOnce)
            return;

        if (_randomize)
        {
            GetRadius();
            _randomness = UnityEngine.Random.Range(_randomnessRange.x, _randomnessRange.y);
            _noiseScale = UnityEngine.Random.Range(_noiseScaleRange.x, _noiseScaleRange.y);
        }

        BuildMesh();
        _mesh = GetComponent<MeshFilter>().mesh;
        _mesh.MarkDynamic();
        _baseVerts = _mesh.vertices;
        _lastFill = -1f;
        _timer = new SimpleTimer(0.1f);
        _initialized = true;
    }

    public void ReduceFill(float amount)
    {
        _fill = Mathf.Clamp01(_fill - amount);
        _lastFill = -1f;
    }

    public float GetRadius()
    {
        if (_isRadiusSet) return _radius;
        _isRadiusSet = true;
        if (_randomize) _radius = UnityEngine.Random.Range(_radiusRange.x, _radiusRange.y);
        return _radius;
    }

    public float GetFill() => _fill;

    void Update()
    {
        _timer.UpdateTimer();
        if (!_timer.IsRoundCompleted) return;

        if (!_scaleWithFill && Mathf.Approximately(_fill, _lastFill))
            return;

        if (_scaleWithFill)
        {
            transform.localScale = new Vector3(_fill, 1f, _fill);
            _lastFill = _fill;
            return;
        }

        if (_isUpdating) return;
        _ = UpdateMeshAsync(_fill);
    }

    private async Task UpdateMeshAsync(float targetFill)
    {
        _isUpdating = true;

        var newVerts = await Task.Run(() =>
        {
            var arr = new Vector3[_baseVerts.Length];
            arr[0] = Vector3.zero;
            for (int i = 1; i < arr.Length; i++)
                arr[i] = Vector3.Lerp(Vector3.zero, _baseVerts[i], targetFill);
            return arr;
        });

        _mesh.vertices = newVerts;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _lastFill = targetFill;
        _isUpdating = false;
    }

    [Button]
    void BuildMesh()
    {
        var verts = new Vector3[_segments + 1];
        var uvs = new Vector2[verts.Length];
        var tris = new int[_segments * 3];

        verts[0] = Vector3.zero;
        uvs[0] = Vector2.one * 0.5f;

        for (int i = 0; i < _segments; i++)
        {
            float t = (float)i / _segments;
            float angle = t * Mathf.PI * 2f;

            float nx = Mathf.Cos(angle) * _noiseScale + 10f;
            float ny = Mathf.Sin(angle) * _noiseScale + 10f;
            float n = Mathf.PerlinNoise(nx, ny);

            float r = _radius * (1f + (n - 0.5f) * 2f * _randomness);
            var p = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;

            verts[i + 1] = p;
            var p2 = new Vector2(p.x, p.z);
            uvs[i + 1] = p2 * 0.5f / _radius + Vector2.one * 0.5f;
        }

        for (int i = 0; i < _segments; i++)
        {
            int o = i * 3;
            int current = i + 1;
            int next = (i + 2 > _segments) ? 1 : i + 2;

            tris[o + 0] = 0;
            tris[o + 1] = next;
            tris[o + 2] = current;
        }

        _mesh = new Mesh();
        _mesh.vertices = verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = _mesh;

        transform.localScale = Vector3.one;
    }

    // ---------------------------- EDITOR GIZMOS ----------------------------
    private Vector3[] GeneratePerimeter(float radius, float randomness, float noiseScale, float fill01 = 1f)
    {
        var arr = new Vector3[_segments + 1];
        arr[0] = Vector3.zero;

        for (int i = 0; i < _segments; i++)
        {
            float t = (float)i / _segments;
            float angle = t * Mathf.PI * 2f;

            float nx = Mathf.Cos(angle) * noiseScale + 10f;
            float ny = Mathf.Sin(angle) * noiseScale + 10f;
            float n = Mathf.PerlinNoise(nx, ny);

            float r = radius * (1f + (n - 0.5f) * 2f * randomness) * Mathf.Max(fill01, 0f);
            var p = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;
            arr[i + 1] = p;
        }
        return arr;
    }

    private static void DrawPolyline(Vector3 origin, Quaternion rot, Vector3[] fan, Color c, bool drawVertices, bool drawTriangles)
    {
        if (fan == null || fan.Length <= 2) return;
        var m = Matrix4x4.TRS(origin, rot, Vector3.one);
        var prev = m.MultiplyPoint3x4(fan[1]);
        var center = m.MultiplyPoint3x4(fan[0]);

        Gizmos.color = c;
        for (int i = 2; i <= fan.Length - 1; i++)
        {
            var cur = m.MultiplyPoint3x4(fan[i]);
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
        Gizmos.DrawLine(prev, m.MultiplyPoint3x4(fan[1]));

        if (drawTriangles)
        {
            Gizmos.color = new Color(c.r, c.g, c.b, c.a * 0.35f);
            for (int i = 1; i <= fan.Length - 2; i++)
            {
                var a = m.MultiplyPoint3x4(fan[i]);
                var b = m.MultiplyPoint3x4(fan[i + 1]);
                Gizmos.DrawLine(center, a);
                Gizmos.DrawLine(center, b);
            }
        }

        if (drawVertices)
        {
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            for (int i = 1; i <= fan.Length - 1; i++)
            {
                var p = m.MultiplyPoint3x4(fan[i]);
                Gizmos.DrawSphere(p, HandleDotSize(center, p));
            }
        }
    }

    private static float HandleDotSize(Vector3 center, Vector3 p)
    {
        float d = (p - center).magnitude;
        return Mathf.Clamp(d * 0.015f, 0.01f, 0.08f);
    }

    private static void DrawCircleXZ(Vector3 origin, float radius, Color c, int segments = 64)
    {
        if (radius <= 0f) return;
        Gizmos.color = c;
        Vector3 prev = origin + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = t * Mathf.PI * 2f;
            Vector3 cur = origin + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }

    private static float LerpDeterministic(int seed, float a, float b, int hops = 1)
    {
        var rng = new System.Random(seed);
        double v = 0.0;
        for (int i = 0; i < hops; i++) v = rng.NextDouble();
        return Mathf.Lerp(a, b, (float)v);
    }

    private void OnDrawGizmos()
    {
        if (!_drawDebug) return;
        if (!Application.isPlaying && !_drawInEditMode) return;

        var full = GeneratePerimeter(_radius, _randomness, _noiseScale, 1f);
        DrawPolyline(transform.position, transform.rotation, full, _colCurrent, _drawVertices, _drawTriangles);

        if (_showFillOutline)
        {
            if (_scaleWithFill)
            {
                var scaled = GeneratePerimeter(_radius * Mathf.Max(_fill, 0f), _randomness, _noiseScale, 1f);
                DrawPolyline(transform.position, transform.rotation, scaled, _colFill, false, false);
            }
            else
            {
                var filled = GeneratePerimeter(_radius, _randomness, _noiseScale, Mathf.Clamp01(_fill));
                DrawPolyline(transform.position, transform.rotation, filled, _colFill, false, false);
            }
        }

        DrawCircleXZ(transform.position, Mathf.Max(_radius, 0.0001f), _colBase, Mathf.Max(_segments, 16));

        if (_randomize && _drawRangeRings)
        {
            DrawCircleXZ(transform.position, Mathf.Max(_radiusRange.x, 0.0001f), new Color(_colRange.r, _colRange.g, _colRange.b, 0.35f), Mathf.Max(_segments, 32));
            DrawCircleXZ(transform.position, Mathf.Max(_radiusRange.y, 0.0001f), new Color(_colRange.r, _colRange.g, _colRange.b, 0.55f), Mathf.Max(_segments, 32));
        }

        if (_randomize && _previewRandomizedShape)
        {
            float prRadius = LerpDeterministic(_previewSeed + 11, _radiusRange.x, _radiusRange.y, 1);
            float prRandomness = LerpDeterministic(_previewSeed + 23, _randomnessRange.x, _randomnessRange.y, 2);
            float prNoiseScale = LerpDeterministic(_previewSeed + 47, _noiseScaleRange.x, _noiseScaleRange.y, 3);

            var rnd = GeneratePerimeter(prRadius, prRandomness, prNoiseScale, 1f);
            DrawPolyline(transform.position, transform.rotation, rnd, _colRandom, false, false);
        }
    }
}
