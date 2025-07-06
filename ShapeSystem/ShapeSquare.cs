using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Shapes
{
    [System.Serializable]
    public class ShapeSquare : ShapeBase
    {
        [BoxGroup("Square Settings")]
        [SerializeField, MinValue(0.0f), LabelText("Side Length"), Tooltip("Length of each side")]
        private float _sideLength = 1f;

        public ShapeSquare(float sideLength, Vector3 center)
        {
            _sideLength = sideLength;
            _center = center;
        }

        public float GetSideLength()
        {
            return _sideLength;
        }

        /// <summary>
        /// Returns the closest point on the square’s perimeter or interior (axis-aligned approach).
        /// </summary>
        public override Vector3 GetClosestPoint(Vector3 point)
        {
            // If you want the perimeter only, you’ll need a more specialized approach.
            // This method clamps the point inside the square boundaries (XZ plane).
            //return ShapeHelper.GetClosestPointOnSquareEdges(_center, _sideLength, point);
            Quaternion rotationQ = Quaternion.Euler(_rotation);
            return ShapeHelper.GetClosestPointOnRotatedSquareEdges(_center, _sideLength, rotationQ, point);
        }

        protected override Vector3[] GenerateUnrotatedVertices()
        {
            return ShapeBuilder.CreateSquare(_center, _sideLength);
        }
    }
}
