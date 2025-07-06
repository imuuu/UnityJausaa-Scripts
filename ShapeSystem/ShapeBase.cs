using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Shapes
{
    public abstract class ShapeBase : IShape
    {
        [BoxGroup("Shape Settings"), PropertyOrder(-10)]
        [SerializeField, Tooltip("Local center position of the shape")]
        protected Vector3 _center;

        /// <summary>
        /// If true, the shape’s vertices (including rotation) will be recalculated
        /// every time <see cref="GetVertices"/> is called.
        /// If false, the shape will cache its vertices until something forces a refresh (e.g., MarkDirty).
        /// </summary>
        [BoxGroup("Shape Settings")]
        [SerializeField, Tooltip("Recalculate vertices every frame? (Performance warning)")]
        protected bool _dynamicVertices;

        [BoxGroup("Rotation"), InlineProperty]
        [SerializeField, LabelText("Euler Angles"), OnValueChanged("MarkDirty")]
        protected Vector3 _rotation;

        // Cached final vertices after rotation is applied.
        // Only recalculated if _dynamicVertices is true or if marked dirty.
        private Vector3[] _cachedVertices;
        private bool _isDirty = true;

        /// <summary>
        /// The center of the shape in world space.
        /// Setting this property marks the shape as dirty (to rebuild vertices).
        /// </summary>
        public Vector3 Center
        {
            get => _center;
            set
            {
                _center = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// The current rotation stored as Euler angles, applied around the shape’s center.
        /// </summary>
        public Vector3 GetRotation() => _rotation;

        /// <summary>
        /// Returns the shape’s vertices in world space, applying the stored rotation if needed.
        /// If _dynamicVertices is false, results are cached until MarkDirty() is called.
        /// If _dynamicVertices is true, shape is recalculated on each call.
        /// </summary>
        public virtual Vector3[] GetVertices()
        {
            if (_dynamicVertices || _isDirty)
            {
                Vector3[] baseVertices = GenerateUnrotatedVertices();

                Quaternion rotationQ = Quaternion.Euler(_rotation);
                _cachedVertices = ShapeRotator.RotateVertices(baseVertices, _center, rotationQ);

                _isDirty = false;
            }

            return _cachedVertices ?? System.Array.Empty<Vector3>();
        }

        /// <summary>
        /// Sets the shape’s vertices from external code.
        /// *But note that doing so can conflict with the shape’s own generation logic.*
        /// If you do this, you might want to disable shape generation entirely, or store them.
        /// </summary>
        public void SetVertices(Vector3[] vertices)
        {
            _cachedVertices = vertices;
            // If user sets them, presumably we do NOT want to regenerate automatically,
            // but you can decide if that means dynamic generation is off or not.
            _isDirty = false;
        }

        public void SetLikeParentTransform(Transform parent, Vector3 localOffset = default)
        {
            //float uniformScale = parent.lossyScale.x;
            //_center = parent.position + parent.rotation * (localOffset * uniformScale);
            _center = parent.position + parent.rotation * localOffset;

            SetRotation(parent.rotation);
            MarkDirty();
        }

        public void ApplyToTransform(Transform target)
        {
            target.position = _center;
            target.rotation = Quaternion.Euler(_rotation);
        }

        /// <summary>
        /// Overwrites the shape’s rotation with the given absolute world-space rotation.
        /// </summary>
        public void SetRotation(Quaternion absoluteRotation)
        {
            _rotation = absoluteRotation.eulerAngles; // if you store rotation as Euler
            MarkDirty();
        }

        /// <summary>
        /// Rotate the shape around the given pivot by the specified quaternion.
        /// Updates the shape’s stored Euler angles + marks shape as dirty.
        /// </summary>
        public void Rotate(Vector3 pivot, Quaternion rotation)
        {
            // Convert from the shape’s center-based rotation approach:
            // 1) The shape is currently oriented at _rotation.
            // 2) We apply an additional 'rotation'.

            // A simple approach: combine the existing rotation with the new one
             Quaternion currentQ = Quaternion.Euler(_rotation);
             Quaternion combinedQ = rotation * currentQ;
            _rotation = combinedQ.eulerAngles;

            _rotation = rotation.eulerAngles;

            MarkDirty();
        }

        /// <summary>
        /// Mark the shape as needing a refresh of cached vertices next time GetVertices() is called.
        /// </summary>
        protected void MarkDirty()
        {
            _isDirty = true;
        }

        protected bool IsDirty() => _isDirty;

        /// <summary>
        /// Subclasses must implement how they compute the shape's unrotated vertices
        /// (e.g., by calling ShapeBuilder).
        /// </summary>
        protected abstract Vector3[] GenerateUnrotatedVertices();

        /// <summary>
        /// Returns the closest point on the shape’s perimeter to a given point.
        /// Must be implemented in each subclass. Usually uses GetVertices() or a specialized method.
        /// </summary>
        public abstract Vector3 GetClosestPoint(Vector3 point);

        public void DrawGizmos(Color color)
        {
            ShapeBuilder.DrawShapeGizmos(GetVertices(), color);
        }
    }
}
