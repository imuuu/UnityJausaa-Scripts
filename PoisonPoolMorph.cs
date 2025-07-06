using System.Threading.Tasks;
using Game.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PoisonPoolMorph : MonoBehaviour, IWidth
{
    [Header("Shape Settings")]
    [Range(3, 128)] [SerializeField] private int _segments = 32;

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
            _randomness = Random.Range(_randomnessRange.x, _randomnessRange.y);
            _noiseScale = Random.Range(_noiseScaleRange.x, _noiseScaleRange.y);
        }

        // Build and cache mesh
        BuildMesh();
        _mesh = GetComponent<MeshFilter>().mesh;
        _mesh.MarkDynamic();
        _baseVerts = _mesh.vertices;
        _lastFill = -1f; // force update on enable
        _timer = new SimpleTimer(0.1f);
        
        _initialized = true;
    }

    public void ReduceFill(float amount)
    {
        _fill = Mathf.Clamp01(_fill - amount);
        _lastFill = -1f; // force update on next frame
    }

    public float GetRadius()
    {
       if(_isRadiusSet) return _radius;

        _isRadiusSet = true;
        if(_randomize)
        {
            _radius = Random.Range(_radiusRange.x, _radiusRange.y);
        }
        return _radius;
    }

    public float GetFill()
    {
        return _fill;
    }

    void Update()
    {
        _timer.UpdateTimer();
        if (!_timer.IsRoundCompleted) return;

        // Skip update if fill hasn't changed in morph mode
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
}
