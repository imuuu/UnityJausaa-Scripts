using UnityEngine;
using DG.Tweening; // DOTween
using Sirenix.OdinInspector; // For [Button], etc.

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShapeAnchor : MonoBehaviour
{
    public enum ShapeType
    {
        Circle,
        Square,
        Triangle,
        Polygon
    }

    [Header("Shape Settings")]
    public ShapeType shapeType = ShapeType.Circle;

    [SerializeField] private float circleRadius = 2f;
    [SerializeField] private float squareSize = 2f;
    [SerializeField] private float triangleSize = 2f;

    // Polygon points, stored as local positions (relative to this transform).
    [SerializeField]
    private Vector3[] polygonPoints = new Vector3[]
    {
        new Vector3(-1f, 0f, -1f),
        new Vector3(-1f, 0f,  1f),
        new Vector3( 1f, 0f,  1f),
        new Vector3( 1f, 0f, -1f)
    };

    [Header("Circle Discrete Sampling")]
    [Tooltip("If true, the circle's closest point is found by sampling discrete points along the circle.")]
    public bool useDiscreteCircle = false;
    [Tooltip("Number of sample points if discrete approach is used.")]
    public int circleSamples = 100;

    [Header("Tween Settings")]
    [SerializeField] private float rotationDuration = 1f;
    [SerializeField] private Ease easeType = Ease.Linear;

    // -------------------------------------------------------------------------
    // 1) "MoveAndRotate": demonstrates your custom parenting trick + DOTween
    // -------------------------------------------------------------------------
    [Button("Move and Rotate")]
    public void MoveAndRotate()
    {
        // 1) We'll use 'target' as our own transform (like your snippet).
        Transform target = this.transform;

        // 2) Reset local X/Z to 0, preserving local Y
        target.localPosition = new Vector3(0f, target.localPosition.y, 0f);

        // 3) Remember old local position so we can restore it after the tween
        Vector3 oldPos = target.localPosition;

        // 4) Get a "closest point" on the shape
        Vector3 closestPoint = GetClosestPointOnShape(new Vector3(target.position.x, -10f, target.position.z));

        // 5) Move to that closest point
        target.position = closestPoint;

        // 6) Save old parent, unparent 'target', then make old parent a child of 'target'
        Transform oldParent = target.parent;
        target.SetParent(null);
        oldParent.SetParent(target);

        // 7) Tween rotation of this transform to (0,0,0)
        Tween tween = target.DORotateQuaternion(Quaternion.Euler(0, 0, 0), rotationDuration);
        tween.SetEase(easeType);

        tween.OnComplete(() =>
        {
            Debug.Log("Rotation Completed");

            // 8) Revert the parent-child relationships
            oldParent.SetParent(null);
            target.SetParent(oldParent);

            // 9) Restore local position/rotation
            target.localPosition = oldPos;
            target.localRotation = Quaternion.Euler(0, 0, 0);
        });

        tween.Play();
    }

    // -------------------------------------------------------------------------
    // 2) ShowClosestPoint: quick debug method to see which point it picks
    // -------------------------------------------------------------------------
    [Button("Show Closest Point")]
    public void ShowClosestPoint()
    {
        Transform target = this.transform;
        // Just pick the transform's position
        Vector3 closestPoint = GetClosestPointOnShape(target.position);

        // We'll create a small sphere to visualize the chosen point
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = closestPoint;
        sphere.transform.localScale = Vector3.one * 0.2f;
        sphere.GetComponent<Renderer>().material.color = Color.cyan;

        // Destroy after 2 seconds
        ActionScheduler.RunAfterDelay(2f, () =>
        {
            Destroy(sphere);
        });
    }

    // -------------------------------------------------------------------------
    // 3) GetClosestPointOnShape: central method that picks the correct shape
    // -------------------------------------------------------------------------
    public Vector3 GetClosestPointOnShape(Vector3 worldPoint)
    {
        switch (shapeType)
        {
            case ShapeType.Circle:
                if (useDiscreteCircle)
                {
                    // Discrete approach
                    return GetClosestOnCircleDiscrete(worldPoint, circleRadius, circleSamples);
                }
                else
                {
                    // Analytic approach
                    return GetClosestOnCircle(worldPoint, circleRadius);
                }

            case ShapeType.Square:
                return GetClosestOnSquare(worldPoint, squareSize);

            case ShapeType.Triangle:
                return GetClosestOnTriangle(worldPoint, triangleSize);

            case ShapeType.Polygon:
                return GetClosestOnPolygon(worldPoint, polygonPoints);
        }

        return worldPoint;
    }

    // -------------------------------------------------------------------------
    // 4) Discrete approach for a circle - optional, if 'useDiscreteCircle' is true
    // -------------------------------------------------------------------------
    private Vector3 GetClosestOnCircleDiscrete(Vector3 worldPoint, float radius, int sampleCount)
    {
        // Convert target point to local
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        float bestDistSqr = float.MaxValue;
        Vector3 bestLocal = localPoint; // fallback

        // We'll sample sampleCount points along the circle's circumference
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float angle = t * 2f * Mathf.PI; // 0..2π
            // For a circle in local XZ plane:
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            Vector3 candidateLocal = new Vector3(x, localPoint.y, z);
            // Convert candidate to world space
            Vector3 candidateWorld = transform.TransformPoint(candidateLocal);

            // Compare squared distances
            float distSqr = (candidateWorld - worldPoint).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestLocal = candidateLocal;
            }
        }

        // Convert the best local point back to world space
        return transform.TransformPoint(bestLocal);
    }

    // -------------------------------------------------------------------------
    // 5) Analytic approach for the circle
    // -------------------------------------------------------------------------
    private Vector3 GetClosestOnCircle(Vector3 worldPoint, float radius)
    {
        // Convert to local space
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        Vector2 planePoint = new Vector2(localPoint.x, localPoint.z);
        float dist = planePoint.magnitude;

        if (dist > radius)
            planePoint = planePoint.normalized * radius;

        Vector3 closestLocal = new Vector3(planePoint.x, localPoint.y, planePoint.y);

        // Back to world space
        return transform.TransformPoint(closestLocal);
    }

    private Vector3 GetClosestOnSquare(Vector3 worldPoint, float size)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        float half = size * 0.5f;

        // Clamps XZ in local space
        float clampedX = Mathf.Clamp(localPoint.x, -half, half);
        float clampedZ = Mathf.Clamp(localPoint.z, -half, half);

        Vector3 closestLocal = new Vector3(clampedX, localPoint.y, clampedZ);
        return transform.TransformPoint(closestLocal);
    }

    private Vector3 GetClosestOnTriangle(Vector3 worldPoint, float size)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // We'll define an equilateral triangle in local XZ plane:
        Vector3 p0 = new Vector3(-size / 2f, 0f, -size * Mathf.Sqrt(3f) / 6f);
        Vector3 p1 = new Vector3(size / 2f, 0f, -size * Mathf.Sqrt(3f) / 6f);
        Vector3 p2 = new Vector3(0f, 0f, size * Mathf.Sqrt(3f) / 3f);

        // 1) Closest point on each edge
        Vector3 on01 = ClosestPointOnLineSegment(p0, p1, new Vector3(localPoint.x, 0f, localPoint.z));
        Vector3 on12 = ClosestPointOnLineSegment(p1, p2, new Vector3(localPoint.x, 0f, localPoint.z));
        Vector3 on20 = ClosestPointOnLineSegment(p2, p0, new Vector3(localPoint.x, 0f, localPoint.z));

        float d01 = Vector3.Distance(on01, localPoint);
        float d12 = Vector3.Distance(on12, localPoint);
        float d20 = Vector3.Distance(on20, localPoint);

        Vector3 closest = on01;
        float minDist = d01;
        if (d12 < minDist) { minDist = d12; closest = on12; }
        if (d20 < minDist) { minDist = d20; closest = on20; }

        // 2) If localPoint is inside the triangle, just use localPoint
        if (PointInTriangle2D(
                new Vector2(localPoint.x, localPoint.z),
                new Vector2(p0.x, p0.z),
                new Vector2(p1.x, p1.z),
                new Vector2(p2.x, p2.z)))
        {
            closest = new Vector3(localPoint.x, 0f, localPoint.z);
        }

        // Keep local y
        Vector3 closestLocal = new Vector3(closest.x, localPoint.y, closest.z);
        return transform.TransformPoint(closestLocal);
    }

    private Vector3 GetClosestOnPolygon(Vector3 worldPoint, Vector3[] points)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        if (points == null || points.Length < 3)
            return transform.TransformPoint(localPoint); // Not valid

        Vector3 best = localPoint;
        float bestDist = float.MaxValue;

        // For each edge:
        for (int i = 0; i < points.Length; i++)
        {
            int j = (i + 1) % points.Length;
            Vector3 edgeClosest = ClosestPointOnLineSegment(
                points[i], points[j],
                new Vector3(localPoint.x, 0f, localPoint.z));

            float dist = Vector3.Distance(edgeClosest, localPoint);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = edgeClosest;
            }
        }

        // Preserve local y
        Vector3 closestLocal = new Vector3(best.x, localPoint.y, best.z);
        return transform.TransformPoint(closestLocal);
    }

    // -------------------------------------------------------------------------
    // 6) Geometry Helpers
    // -------------------------------------------------------------------------
    private Vector3 ClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    private bool PointInTriangle2D(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float area = TriangleArea2D(p0, p1, p2);
        float area1 = TriangleArea2D(p, p1, p2);
        float area2 = TriangleArea2D(p0, p, p2);
        float area3 = TriangleArea2D(p0, p1, p);

        // If the sum of sub-areas == total, the point is inside
        return Mathf.Approximately(area, area1 + area2 + area3);
    }

    private float TriangleArea2D(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Abs(a.x * (b.y - c.y)
                       + b.x * (c.y - a.y)
                       + c.x * (a.y - b.y)) * 0.5f;
    }

#if UNITY_EDITOR
    // -------------------------------------------------------------------------
    // 7) OnDrawGizmos: debug the shape in the Scene view
    // -------------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Handles.color = Color.yellow;

        switch (shapeType)
        {
            case ShapeType.Circle:
                // We'll just draw a wire disc in the shape’s local plane,
                // which should be transform.up as normal if shape isn't tilted
                Handles.DrawWireDisc(transform.position, transform.up, circleRadius);
                break;

            case ShapeType.Square:
                // We'll just draw a wire cube in the local XZ plane
                Vector3 size = new Vector3(squareSize, 0.0001f, squareSize);
                Gizmos.DrawWireCube(transform.position, size);
                break;

            case ShapeType.Triangle:
                {
                    // Define the same corners we used in GetClosestOnTriangle
                    Vector3 p0 = new Vector3(-triangleSize / 2f, 0f, -triangleSize * Mathf.Sqrt(3f) / 6f);
                    Vector3 p1 = new Vector3(triangleSize / 2f, 0f, -triangleSize * Mathf.Sqrt(3f) / 6f);
                    Vector3 p2 = new Vector3(0f, 0f, triangleSize * Mathf.Sqrt(3f) / 3f);

                    // Convert to world space
                    p0 = transform.TransformPoint(p0);
                    p1 = transform.TransformPoint(p1);
                    p2 = transform.TransformPoint(p2);

                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p0);
                }
                break;

            case ShapeType.Polygon:
                {
                    if (polygonPoints != null && polygonPoints.Length > 1)
                    {
                        for (int i = 0; i < polygonPoints.Length; i++)
                        {
                            int next = (i + 1) % polygonPoints.Length;
                            Vector3 wp1 = transform.TransformPoint(polygonPoints[i]);
                            Vector3 wp2 = transform.TransformPoint(polygonPoints[next]);
                            Gizmos.DrawLine(wp1, wp2);
                        }
                    }
                }
                break;
        }
    }
#endif
}
