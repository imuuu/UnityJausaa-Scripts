using UnityEngine;

namespace Game.Shapes
{
    public static class ShapeRotator
    {
        /// <summary>
        /// Rotates a shape around a given pivot by a specified rotation.
        /// </summary> 
        public static Vector3[] Rotate(IShape shape, Vector3 pivot, Quaternion rotation)
        {
            Vector3[] rotatedVertices = RotateVertices(shape.GetVertices(), pivot, rotation);
            shape.SetVertices(rotatedVertices);
            return rotatedVertices;
        }

        /// <summary>
        /// Rotates an array of vertices around a given pivot by a specified rotation.
        /// </summary>
        /// <param name="vertices">The original array of vertices to rotate.</param>
        /// <param name="pivot">The point around which to rotate the vertices.</param>
        /// <param name="rotation">The rotation to apply (e.g., Quaternion.Euler(0, angle, 0)).</param>
        /// <returns>A new array containing the rotated vertices.</returns>
        public static Vector3[] RotateVertices(Vector3[] vertices, Vector3 pivot, Quaternion rotation)
        {
            if (vertices == null || vertices.Length == 0)
                return vertices;

            Vector3[] rotatedVertices = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 direction = vertices[i] - pivot;
                direction = rotation * direction;
                rotatedVertices[i] = pivot + direction;
            }

            return rotatedVertices;
        }

        /// <summary>
        /// Rotates an array of vertices around a given pivot by a certain angle (in degrees) around an axis.
        /// </summary>
        /// <param name="vertices">The original array of vertices.</param>
        /// <param name="pivot">The pivot point around which to rotate.</param>
        /// <param name="angleDegrees">Rotation angle in degrees.</param>
        /// <param name="axis">Axis of rotation (e.g., Vector3.up).</param>
        /// <returns>Rotated array of vertices.</returns>
        public static Vector3[] RotateVertices(Vector3[] vertices, Vector3 pivot, float angleDegrees, Vector3 axis)
        {
            Quaternion rotation = Quaternion.AngleAxis(angleDegrees, axis);
            return RotateVertices(vertices, pivot, rotation);
        }
    }
}
