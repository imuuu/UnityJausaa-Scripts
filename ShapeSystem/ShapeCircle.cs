using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Shapes
{
    [System.Serializable]
    public class ShapeCircle : ShapeBase
    {
        [BoxGroup("Circle Settings", CenterLabel = true)]
        [SerializeField, MinValue(0.0f), LabelText("Radius"), Tooltip("Size of the circle")]
        private float _radius = 1f;

        [BoxGroup("Circle Settings")]
        [SerializeField, Range(0, 50), LabelText("Edge Count"), Tooltip("Higher = smoother circle")]
        private int _edgeCount = 20;

        public ShapeCircle(float radius, int edgeCount, Vector3 center)
        {
            _radius = radius;
            _edgeCount = edgeCount;
            _center = center;
        }

        public float GetRadius()
        {
            return _radius;
        }

        public int GetEdgeCount()
        {
            return _edgeCount;
        }

        public override Vector3 GetClosestPoint(Vector3 point)
        {
            // For an axis-aligned circle in the XZ plane, get the closest perimeter point.
            // If you were rotating this circle, you’d have to handle that differently.
            //return ShapeHelper.GetClosestPointOnCircle(_center, _radius, point);
            Quaternion rotationQ = Quaternion.Euler(_rotation);
            return ShapeHelper.GetClosestPointOnRotatedCircle(_center, _radius, rotationQ, point);
        }

        protected override Vector3[] GenerateUnrotatedVertices()
        {
            return ShapeBuilder.CreateCircle(_center, _radius, _edgeCount);
        }
    }
}
