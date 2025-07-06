using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Shapes
{
    [System.Serializable]
    public class ShapeTriangle : ShapeBase
    {
        [BoxGroup("Triangle Settings")]
        [SerializeField, MinValue(0.0f), LabelText("Side Length"), Tooltip("Length of each side")]
        private float _sideLength = 1f;

        private Vector3[] _localVertices;

        public ShapeTriangle(float sideLength, Vector3 center)
        {
            _sideLength = sideLength;
            _center = center;
        }

        public float GetSideLength()
        {
            return _sideLength;
        }

        // Good for local geometry (no center added)
        public Vector3[] CreateTriangleLocal(float sideLength)
        {
            if(_localVertices != null && _localVertices.Length > 0 && !IsDirty())
                return _localVertices;

            float height = Mathf.Sqrt(3f) * 0.5f * sideLength;
            Vector3 p1 = new Vector3(-sideLength * 0.5f, 0f, -height / 3f);
            Vector3 p2 = new Vector3(sideLength * 0.5f, 0f, -height / 3f);
            Vector3 p3 = new Vector3(0f, 0f, 2f * height / 3f);
            _localVertices = new[] { p1, p2, p3 };
            return _localVertices;
        }


        public override Vector3 GetClosestPoint(Vector3 point)
        {
            Quaternion rotationQ = Quaternion.Euler(_rotation);
            return ShapeHelper.GetClosestPointOnRotatedTriangle(_center, rotationQ, CreateTriangleLocal(_sideLength), point);
        }

        protected override Vector3[] GenerateUnrotatedVertices()
        {
            return ShapeBuilder.CreateTriangle(_center, _sideLength);
        }
    }
}
