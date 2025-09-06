using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// Drives "Hidden/EdgeFogOverlay_WorldCircle" shader (circle or square) with a small intro animation:
/// start at BaseSize + StartOffset -> animate to BaseSize over Duration (with easing).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class EdgeFogOverlayController : MonoBehaviour
{
    public enum Shape { Circle = 0, Square = 1 }

    private static readonly int _ID_Color = Shader.PropertyToID("_Color");
    private static readonly int _ID_Center = Shader.PropertyToID("_Center");
    private static readonly int _ID_HalfSize = Shader.PropertyToID("_HalfSize");
    private static readonly int _ID_Soft = Shader.PropertyToID("_Soft");
    private static readonly int _ID_HeightY = Shader.PropertyToID("_HeightY");
    private static readonly int _ID_Shape = Shader.PropertyToID("_Shape");
    private static readonly int _ID_RotateWithObject = Shader.PropertyToID("_RotateWithObject");

    // ───────────────────────────────── TARGET ─────────────────────────────────
#if ODIN_INSPECTOR
    [TitleGroup("Edge Fog Overlay", "World-space edge fog overlay controller")]
    [BoxGroup("Edge Fog Overlay/Target", showLabel: true)]
    [LabelText("Renderer"), Required, Tooltip("Renderer that has the fog overlay material.")]
#endif
    [SerializeField] private Renderer _targetRenderer;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Target")]
    [LabelText("Material Slot"), MinValue(0), Tooltip("Which material slot in the renderer to set.")]
#endif
    [SerializeField] private int _materialIndex = 0;

    // ───────────────────────────────── VISUAL ─────────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Visual", showLabel: true)]
    [LabelText("Fog Color"), ColorPalette, Tooltip("RGB = color, A = overall opacity multiplier.")]
#endif
    [SerializeField] private Color _color = new Color(0f, 0f, 0f, 0.75f);

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Visual")]
    [LabelText("Soft Edge Width"), SuffixLabel("m", overlay: true)]
    [MinValue(0), Tooltip("Meters over which alpha fades outward from the boundary.")]
#endif
    [SerializeField] private float _softEdgeWidth = 20f;

    // ──────────────────────────────── PLACEMENT ───────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Placement", showLabel: true)]
    [LabelText("Center Offset (World)"), Tooltip("World-space offset from this object's pivot.")]
#endif
    [SerializeField] private Vector3 _centerOffsetWorld = Vector3.zero;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Placement")]
    [LabelText("Effective Height (Y)"), SuffixLabel("m", overlay: true)]
    [Tooltip("Optional vertical gating (disabled in shader by default).")]
#endif
    [SerializeField] private float _effectiveHeightY = 0f;

    // ───────────────────────────────── SHAPE ──────────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Shape", showLabel: true)]
    [EnumToggleButtons, LabelText("Shape")]
#endif
    [SerializeField] private Shape _shape = Shape.Circle;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Shape")]
    [ShowIf("@_shape == Shape.Circle")]
    [LabelText("Radius"), SuffixLabel("m", overlay: true)]
    [MinValue(0), Tooltip("Circle radius in meters (final/base size).")]
#endif
    [SerializeField] private float _circleRadius = 10f;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Shape")]
    [ShowIf("@_shape == Shape.Square")]
    [LabelText("Half Extents (X,Z)"), Tooltip("Square/rect half extents in meters (final/base size). Full size = 2x this.")]
#endif
    [SerializeField] private Vector2 _squareHalfExtents = new Vector2(10f, 10f);

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Shape")]
    [ShowIf("@_shape == Shape.Square")]
    [LabelText("Follow Object Rotation"), ToggleLeft, Tooltip("Aligns the square with this object's Y rotation.")]
#endif
    [SerializeField] private bool _squareFollowsObjectRotation = true;

    // ───────────────────────────── Intro Animation ────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Intro Animation", showLabel: true)]
    [LabelText("Enable Intro"), ToggleLeft, Tooltip("Animate from (BaseSize + Start Offset) to BaseSize.")]
#endif
    [SerializeField] private bool _introEnabled = true;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Intro Animation")]
    [ShowIf(nameof(_introEnabled))]
    [LabelText("Start Offset (+)"), SuffixLabel("m", overlay: true)]
    [Tooltip("Added to base size at the start. Circle: +meters to radius. Square: +meters to both half extents.")]
#endif
    [SerializeField] private float _introStartOffset = 10f;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Intro Animation")]
    [ShowIf(nameof(_introEnabled))]
    [LabelText("Duration"), SuffixLabel("s", overlay: true)]
    [MinValue(0)]
#endif
    [SerializeField] private float _introDuration = 2f;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Intro Animation")]
    [ShowIf(nameof(_introEnabled))]
    [LabelText("Easing")]
#endif
    [SerializeField] private AnimationCurve _introEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Intro Animation")]
    [ShowIf(nameof(_introEnabled))]
    [LabelText("Play On Enable"), ToggleLeft]
#endif
    [SerializeField] private bool _introPlayOnEnable = true;

    // runtime state for the intro animation
    private bool _isAnimatingIntro;
    private float _introElapsed;

    // ───────────────────────────────── UPDATE ─────────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Update", showLabel: true)]
    [LabelText("Live Update"), ToggleLeft, Tooltip("Re-apply every frame (editor & play).")]
#endif
    [SerializeField] private bool _liveUpdate = true;

    // ───────────────────────────────── DEBUG ──────────────────────────────────
#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Debug", showLabel: true)]
    [LabelText("Draw Gizmos"), ToggleLeft]
#endif
    [SerializeField] private bool _drawGizmos = true;

#if ODIN_INSPECTOR
    [BoxGroup("Edge Fog Overlay/Debug")]
    [LabelText("Gizmo Color")]
    [GUIColor(0.8f, 0.9f, 1f)]
#endif
    [SerializeField] private Color _gizmoColor = new Color(0f, 0f, 0f, 0.25f);

    private MaterialPropertyBlock _mpb;

    // ───────────────────────────────── ACTIONS ────────────────────────────────
#if ODIN_INSPECTOR
    [TitleGroup("Edge Fog Overlay")]
    [HorizontalGroup("Edge Fog Overlay/Actions", width: 0.5f)]
    [Button(ButtonSizes.Medium), GUIColor(0.2f, 0.8f, 0.4f)]
#endif
    public void Apply()
    {
        if (_targetRenderer == null)
            _targetRenderer = GetComponent<Renderer>();
        if (_targetRenderer == null)
            return;

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Calculate current size (animated or base)
        float currentRadius;
        Vector2 currentHalfExtents;

        if (_introEnabled && _isAnimatingIntro && _introDuration > 0f)
        {
            float t = Mathf.Clamp01(_introElapsed / _introDuration);
            float e = Mathf.Clamp01(_introEase.Evaluate(t));

            if (_shape == Shape.Circle)
            {
                float start = _circleRadius + _introStartOffset;
                float end = _circleRadius;
                currentRadius = Mathf.Lerp(start, end, e);
                currentHalfExtents = Vector2.zero; // unused for circle
            }
            else
            {
                Vector2 start = _squareHalfExtents + Vector2.one * _introStartOffset;
                Vector2 end = _squareHalfExtents;
                currentHalfExtents = Vector2.Lerp(start, end, e);
                currentRadius = 0f;
            }
        }
        else
        {
            currentRadius = _circleRadius;
            currentHalfExtents = _squareHalfExtents;
        }

        _targetRenderer.GetPropertyBlock(_mpb, _materialIndex);

        _mpb.SetColor(_ID_Color, _color);
        _mpb.SetVector(_ID_Center, _centerOffsetWorld);

        // Pack HalfSize for the shader:
        // - Circle uses x = radius
        // - Square uses xz = half extents
        Vector4 halfSize = Vector4.zero;
        if (_shape == Shape.Circle)
        {
            halfSize.x = Mathf.Max(0f, currentRadius);
            halfSize.z = 0f;
        }
        else
        {
            halfSize.x = Mathf.Max(0f, currentHalfExtents.x);
            halfSize.z = Mathf.Max(0f, currentHalfExtents.y);
        }
        _mpb.SetVector(_ID_HalfSize, halfSize);

        _mpb.SetFloat(_ID_Soft, Mathf.Max(0f, _softEdgeWidth));
        _mpb.SetFloat(_ID_HeightY, _effectiveHeightY);
        _mpb.SetFloat(_ID_Shape, (float)_shape);
        _mpb.SetFloat(_ID_RotateWithObject, _squareFollowsObjectRotation ? 1f : 0f);

        _targetRenderer.SetPropertyBlock(_mpb, _materialIndex);
    }

#if ODIN_INSPECTOR
    [HorizontalGroup("Edge Fog Overlay/Actions")]
    [Button("Snap Center to This", ButtonSizes.Medium)]
#endif
    public void SnapCenterToThis()
    {
        _centerOffsetWorld = Vector3.zero;
        Apply();
    }

#if ODIN_INSPECTOR
    [HorizontalGroup("Edge Fog Overlay/Actions")]
    [Button("Play Intro", ButtonSizes.Medium), EnableIf(nameof(_introEnabled))]
#endif
    public void PlayIntro()
    {
        if (!_introEnabled) return;
        _introElapsed = 0f;
        _isAnimatingIntro = _introDuration > 0f;
        // If duration is 0, just jump to end
        if (!_isAnimatingIntro) Apply();
    }

#if ODIN_INSPECTOR
    [HorizontalGroup("Edge Fog Overlay/Actions")]
    [Button("Restart Intro", ButtonSizes.Medium), EnableIf(nameof(_introEnabled))]
#endif
    public void RestartIntro()
    {
        _introElapsed = 0f;
        _isAnimatingIntro = _introEnabled && _introDuration > 0f;
        Apply();
    }

#if ODIN_INSPECTOR
    [HorizontalGroup("Edge Fog Overlay/Actions")]
    [Button("Stop Intro", ButtonSizes.Medium), EnableIf(nameof(_introEnabled))]
#endif
    public void StopIntro(bool snapToEnd = true)
    {
        if (snapToEnd && _introEnabled)
        {
            _introElapsed = _introDuration;
        }
        _isAnimatingIntro = false;
        Apply();
    }

    /// <summary>
    /// Programmatic trigger: begin an intro with custom offset/duration.
    /// </summary>
    public void BeginIntro(float startOffsetMeters, float durationSeconds, AnimationCurve easing = null)
    {
        _introStartOffset = startOffsetMeters;
        _introDuration = Mathf.Max(0f, durationSeconds);
        if (easing != null) _introEase = easing;
        _introEnabled = true;
        PlayIntro();
    }

    private void Reset()
    {
        _targetRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (_introEnabled && _introPlayOnEnable)
            PlayIntro();
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _circleRadius = Mathf.Max(0f, _circleRadius);
        _squareHalfExtents = new Vector2(Mathf.Max(0f, _squareHalfExtents.x),
                                         Mathf.Max(0f, _squareHalfExtents.y));
        _softEdgeWidth = Mathf.Max(0f, _softEdgeWidth);
        // If anim is active in editor, keep applying for preview
        Apply();
    }
#endif

    private void Update()
    {
        bool changed = false;

        // progress intro animation (runs in editor due to ExecuteAlways)
        if (_introEnabled && _isAnimatingIntro)
        {
            _introElapsed += Application.isPlaying ? Time.deltaTime : 1f / 60f;
            if (_introElapsed >= _introDuration)
            {
                _introElapsed = _introDuration;
                _isAnimatingIntro = false;
            }
            changed = true;
        }

        if (_liveUpdate || changed)
            Apply();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Vector3 center = transform.position + _centerOffsetWorld;
        Color c = _gizmoColor; c.a = Mathf.Clamp01(c.a);
        Gizmos.color = c;

        // Show the CURRENT size (animated if running)
        float currentRadius = _circleRadius;
        Vector2 currentHalfExtents = _squareHalfExtents;

        if (_introEnabled && _isAnimatingIntro && _introDuration > 0f)
        {
            float t = Mathf.Clamp01(_introElapsed / _introDuration);
            float e = Mathf.Clamp01(_introEase.Evaluate(t));
            if (_shape == Shape.Circle)
            {
                float start = _circleRadius + _introStartOffset;
                currentRadius = Mathf.Lerp(start, _circleRadius, e);
            }
            else
            {
                Vector2 start = _squareHalfExtents + Vector2.one * _introStartOffset;
                currentHalfExtents = Vector2.Lerp(start, _squareHalfExtents, e);
            }
        }

        if (_shape == Shape.Circle)
        {
            DrawGizmoCircleXZ(center, currentRadius);
        }
        else
        {
            Vector3 hxz = new Vector3(currentHalfExtents.x, 0f, currentHalfExtents.y);
            DrawGizmoSquareXZ(center, hxz, _squareFollowsObjectRotation ? transform.rotation : Quaternion.identity);
        }
    }

    // ───────────────────────────── Gizmo helpers ─────────────────────────────
    private static void DrawGizmoCircleXZ(Vector3 center, float radius, int segments = 64)
    {
        if (radius <= 0f) return;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        float step = Mathf.PI * 2f / Mathf.Max(12, segments);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    private static void DrawGizmoSquareXZ(Vector3 center, Vector3 halfXZ, Quaternion rot)
    {
        Vector3[] corners = new Vector3[4];
        corners[0] = center + rot * new Vector3(-halfXZ.x, 0f, -halfXZ.z);
        corners[1] = center + rot * new Vector3(halfXZ.x, 0f, -halfXZ.z);
        corners[2] = center + rot * new Vector3(halfXZ.x, 0f, halfXZ.z);
        corners[3] = center + rot * new Vector3(-halfXZ.x, 0f, halfXZ.z);

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    // ───────────────────────── Public API (optional) ─────────────────────────
    public void SetCircle(float radius) { _shape = Shape.Circle; _circleRadius = Mathf.Max(0f, radius); Apply(); }
    public void SetSquare(Vector2 halfExtents, bool followRotation)
    {
        _shape = Shape.Square;
        _squareHalfExtents = new Vector2(Mathf.Max(0f, halfExtents.x), Mathf.Max(0f, halfExtents.y));
        _squareFollowsObjectRotation = followRotation;
        Apply();
    }
    public void SetColor(Color c) { _color = c; Apply(); }
    public void SetSoft(float soft) { _softEdgeWidth = Mathf.Max(0f, soft); Apply(); }
    public void SetCenterOffsetWorld(Vector3 offset) { _centerOffsetWorld = offset; Apply(); }
}
