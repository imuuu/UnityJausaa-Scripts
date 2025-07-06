#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Game.Shapes
{
    public class TEST_ShapeComponent : MonoBehaviour
    {
        // With Odin, you can do [SerializeReference, Polymorphic]
        // so you can pick from different shape classes in the inspector:
#if ODIN_INSPECTOR
        [SerializeReference, InlineProperty, LabelText("Shape")]
        [HideReferenceObjectPicker] // optional, purely for nicer display
#else
        [SerializeField]
#endif
        private IShape _shape;

        [Header("Rotation Settings")]
        [SerializeField] private bool _enableRotation = true;
        [SerializeField] private float _rotationSpeed = 30f;   // Degrees per second
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        [Header("Test Point Offset (from shape center)")]
        [SerializeField]
        private Vector3 _pointOffset = new Vector3(1, 0, 1);

        private void OnDrawGizmos()
        {
            if (_shape == null) return;

            // 1) Get the shape’s center and vertices
            Vector3 center = _shape.Center;
            Vector3[] vertices = _shape.GetVertices();

            // 2) (Optional) apply rotation to shape vertices purely for visualization
            //    This won't affect the shape's internal "Center" or how it calculates distance,
            //    but you can see it spin in the Scene.
            if (_enableRotation && vertices != null && vertices.Length > 1)
            {
                float angle = _rotationSpeed * (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
                vertices = ShapeRotator.RotateVertices(vertices, center, angle, _rotationAxis);
            }

            // 3) Draw the shape perimeter
            ShapeBuilder.DrawShapeGizmos(vertices, Color.white);

            // 4) Define a test point
            Vector3 testPoint = center + _pointOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(testPoint, 0.05f);

            // 5) Ask the shape for the closest point
            Vector3 closest = _shape.GetClosestPoint(testPoint);

            // 6) Draw the closest point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(closest, 0.05f);

            // 7) Draw line from testPoint to closest point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(testPoint, closest);
        }
    }
}
