using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Shapes
{
    [System.Serializable]
    public class ShapePolygon : ShapeBase
    {
        /// <summary>
        /// Local-space vertices of the polygon.
        /// Each vertex should be in the XZ plane (y=0) if you treat this as a 2D shape in 3D.
        /// Or use XY plane if that’s your design (but then adapt the closest-point logic).
        /// </summary>
        [TitleGroup("Polygon Settings", "Define vertices in local space")]
        [SerializeField, ListDrawerSettings(ShowFoldout = true, ShowIndexLabels = true)]
        [Tooltip("Local-space vertices (relative to center)")]
        private Vector3[] _localVertices;

        /// <summary>
        /// Returns the local vertices array. You can expose this in the Inspector or manipulate it in code.
        /// </summary>
        public Vector3[] LocalVertices
        {
            get => _localVertices;
            set
            {
                _localVertices = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// The polygon’s unrotated vertices in WORLD space, as required by ShapeBase.
        /// 
        /// In this example, we interpret "GenerateUnrotatedVertices"
        /// to mean "take local vertices and place them in world space without any additional rotation from ShapeBase."
        /// That is, we only apply the shape’s _center offset here, ignoring _rotation. 
        /// (The final rotation in the base class is added later to produce the truly final vertices if needed.)
        /// 
        /// Alternatively, you could return purely local-space vertices if you skip adding _center here.
        /// But it’s more consistent with the rest of ShapeBase to at least factor in the shape’s center.
        /// </summary>
        protected override Vector3[] GenerateUnrotatedVertices()
        {
            if (_localVertices == null || _localVertices.Length == 0)
            {
                return System.Array.Empty<Vector3>();
            }

            Vector3[] worldVerts = new Vector3[_localVertices.Length];
            for (int i = 0; i < _localVertices.Length; i++)
            {
                // We’ll place them around _center, but NOT apply the shape’s rotation yet.
                // That rotation is applied by ShapeRotator in GetVertices().
                worldVerts[i] = _center + _localVertices[i];
            }
            return worldVerts;
        }

        /// <summary>
        /// Returns the closest point on the polygon’s perimeter (edges) to a given world-space point.
        /// This method uses the local-based approach in ShapeHelper
        /// to account for rotation properly (if _localVertices are truly local).
        /// 
        /// If your local polygon is in the XZ plane, then 
        /// GetClosestPointOnRotatedPolygon(...) will discard localPoint.y and clamp in 2D.
        /// </summary>
        public override Vector3 GetClosestPoint(Vector3 point)
        {
            if (_localVertices == null || _localVertices.Length < 2)
            {
                // If invalid polygon data, just return the point
                return point;
            }

            Quaternion rotationQ = Quaternion.Euler(_rotation);

            // Use a helper that transforms the point into local space,
            // finds the nearest point on the polygon edges in local space,
            // then transforms back to world space.
            return ShapeHelper.GetClosestPointOnRotatedPolygon(
                _center,
                rotationQ,
                _localVertices,
                point
            );
        }
    }
}
