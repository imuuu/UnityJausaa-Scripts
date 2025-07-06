using UnityEngine;

namespace Game.Shapes
{
    public class TEST_ShapeClosestPoint : MonoBehaviour
    {
        public enum TestShape
        {
            Circle,
            Square,
            Triangle
        }

        [Header("Shape Type")]
        [SerializeField] private TestShape _shapeType = TestShape.Circle;

        [Header("Circle or Polygon Settings")]
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private int _polygonEdges = 5;

        [Header("Square or Triangle Settings")]
        [SerializeField] private float _sideLength = 2f;

        [Header("Test Point Offset (from this object's position)")]
        [SerializeField] private Vector3 _pointOffset = new Vector3(1.25f, 0f, 0.5f);

        [Header("Rotation Settings")]
        [SerializeField] private bool _enableRotation = true;
        [SerializeField] private float _rotationSpeed = 30f;   // Degrees per second
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            Vector3[] shapeVertices = null;

            // 1) Generate shape vertices
            switch (_shapeType)
            {
                case TestShape.Circle:
                    // Approximates a circle using _polygonEdges vertices
                    shapeVertices = ShapeBuilder.CreateCircle(center, _radius, _polygonEdges);
                    break;

                case TestShape.Square:
                    shapeVertices = ShapeBuilder.CreateSquare(center, _sideLength);
                    break;

                case TestShape.Triangle:
                    shapeVertices = ShapeBuilder.CreateTriangle(center, _sideLength);
                    break;
            }

            // 2) Optionally rotate the shape (for visualization)
            //    - This rotation is applied to the shape's vertices *after* creation.
            //    - Note that for circle or square, the shape is still considered axis-aligned
            //      by the specialized ShapeHelper methods. If you want the "true" closest
            //      point on the *rotated* shape, you'd need to treat everything as a polygon
            //      (like the triangle case) or incorporate this rotation in your distance logic.
            if (_enableRotation && shapeVertices != null && shapeVertices.Length > 0)
            {
                // Time-based angle so it spins in the Scene view even when not in Play mode
                float angle = _rotationSpeed * (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);

                shapeVertices = ShapeRotator.RotateVertices(shapeVertices, center, angle, _rotationAxis);
            }

            // 3) Draw the shape
            if (shapeVertices != null && shapeVertices.Length > 0)
            {
                ShapeBuilder.DrawShapeGizmos(shapeVertices, Color.white);
            }

            // 4) Place a test point in the scene
            Vector3 testPoint = center + _pointOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(testPoint, 0.05f);

            // 5) Compute the closest point on the shape
            Vector3 closestPoint = testPoint; // Fallback if anything fails
            switch (_shapeType)
            {
                case TestShape.Circle:
                    // If you want the *rotated circle*, you’d need a different approach or treat it as a polygon.
                    closestPoint = ShapeHelper.GetClosestPointOnCircle(center, _radius, testPoint);
                    break;

                case TestShape.Square:
                    // By default, this is an axis-aligned square in XZ plane.
                    closestPoint = ShapeHelper.GetClosestPointOnSquareInterior(center, _sideLength, testPoint);
                    break;

                case TestShape.Triangle:
                    // We don’t have a specialized triangle closest-point method,
                    // so treat it as a polygon:
                    if (shapeVertices != null && shapeVertices.Length > 2)
                    {
                        closestPoint = ShapeHelper.GetClosestPointOnPolygon(shapeVertices, testPoint);
                    }
                    break;
            }

            // 6) Draw the closest point in green
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(closestPoint, 0.05f);

            // 7) Draw a line from testPoint to the closest point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(testPoint, closestPoint);
        }
    }
}
