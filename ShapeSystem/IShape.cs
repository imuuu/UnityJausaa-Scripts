using UnityEngine;

namespace Game.Shapes
{
    /// <summary>
    /// A simple interface that all shapes must implement.
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// The center of the shape in world space.
        /// </summary>
        public Vector3 Center { get; set; }

        public Vector3 GetRotation();

        public void Rotate(Vector3 pivot, Quaternion rotation);

        /// <summary>
        /// Returns the shape’s vertices in world space.
        /// </summary>
        public Vector3[] GetVertices();

        public void SetVertices(Vector3[] vertices);

        /// <summary>
        /// Returns the closest point on the shape’s perimeter to a given world-space point.
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 point);
    }
}
