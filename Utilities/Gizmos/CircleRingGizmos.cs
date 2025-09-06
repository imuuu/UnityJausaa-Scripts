using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class CircleRingGizmos : MonoBehaviour
{
    [Header("Ring Setup")]
    [SerializeField] private float _mainRadius = 5f;
    [SerializeField] private float _smallRadius = 1f;
    [SerializeField, Min(1)] private int _count = 5;
    [SerializeField] private bool _useXZPlane = true;
    [SerializeField] private Vector3 _firstDirection = Vector3.forward;
    [SerializeField, Min(0f)] private float _edgeInset = 0.001f;

    [Header("Colors")]
    [SerializeField] private Color _outerColor = Color.black;
    [SerializeField] private Color _smallColor = Color.red;
    [SerializeField] private Color _lineColor = Color.white;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;

        // Try to place
        if (!CircleRingPlacer.TryPlaceOnInnerTangentRing(center, _mainRadius, _smallRadius,
                                                         _count, _firstDirection, _useXZPlane,
                                                         out var pts, _edgeInset))
        {
            Handles.color = Color.yellow;
            Handles.Label(center, "Doesn't fit (reduce rSmall or count)");
            return;
        }

        // Draw big circle
        Handles.color = _outerColor;
        Vector3 normal = _useXZPlane ? Vector3.up : Vector3.forward;
        Handles.DrawWireDisc(center, normal, _mainRadius);

        // Draw lines + small circles + labels
        Handles.color = _lineColor;
        for (int i = 0; i < pts.Length; i++)
            Handles.DrawLine(center, pts[i]);

        Handles.color = _smallColor;
        for (int i = 0; i < pts.Length; i++)
            Handles.DrawWireDisc(pts[i], normal, _smallRadius);

        // index labels
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = _smallColor;
        for (int i = 0; i < pts.Length; i++)
            Handles.Label(pts[i], $"#{i}", style);
    }
#endif
}
