using UnityEngine;

namespace Game.Shapes
{
    public class TEST_ShapeRotatorBehavior : MonoBehaviour
    {
        [Header("Shape Settings")]
        [SerializeField] private float _sideLength = 1f;
        [SerializeField] private int _circleEdgeCount = 20;

        [Header("Rotation Settings")]
        [SerializeField] private bool _rotateInEditor = true; // toggles rotation in OnDrawGizmos
        [SerializeField] private float _rotationSpeed = 45f;   // degrees per second
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        [Header("Gizmo Settings")]
        [SerializeField] private Color _shapeColor = Color.yellow;
        [SerializeField] private SHAPE_TYPE _shapeType = SHAPE_TYPE.SQUARE;

        private Vector3[] vertices;

        private void Update()
        {
            transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            // If you want the shape itself to appear rotated in the editor, you can:
            // 1) Build shape in local space
            // 2) Use Gizmos.matrix to apply transform's position/rotation
            //    or manually rotate the vertices each frame.

            // Build the shape around transform.position
            switch (_shapeType)
            {
                case SHAPE_TYPE.TRIANGLE:
                    vertices = ShapeBuilder.CreateTriangle(transform.position, _sideLength);
                    break;
                case SHAPE_TYPE.SQUARE:
                    vertices = ShapeBuilder.CreateSquare(transform.position, _sideLength);
                    break;
                case SHAPE_TYPE.CIRCLE:
                    vertices = ShapeBuilder.CreateCircle(transform.position, _sideLength, _circleEdgeCount);
                    break;
            }

            if (_rotateInEditor)
            {
                // Rotate shape’s vertices (in editor) by time to see it spin
                float angle = _rotationSpeed * (Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
                vertices = ShapeRotator.RotateVertices(vertices, transform.position, angle, _rotationAxis);
            }

            ShapeBuilder.DrawShapeGizmos(vertices, _shapeColor);
        }
    }
}
