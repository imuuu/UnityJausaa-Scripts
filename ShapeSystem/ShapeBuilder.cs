using UnityEngine;

namespace Game.Shapes
{
    public static class ShapeBuilder
    {
        /// <summary>
        /// Generates the vertices of an equilateral triangle centered at a given position.
        /// </summary>
        public static Vector3[] CreateTriangle(Vector3 center, float sideLength)
        {
            float height = Mathf.Sqrt(3) * 0.5f * sideLength;

            Vector3 p1 = center + new Vector3(-sideLength * 0.5f, 0, -height / 3);
            Vector3 p2 = center + new Vector3(sideLength * 0.5f, 0, -height / 3);
            Vector3 p3 = center + new Vector3(0, 0, 2 * height / 3);

            return new Vector3[] { p1, p2, p3 };
        }

        /// <summary>
        /// Generates the vertices of a square centered at a given position.
        /// </summary>
        public static Vector3[] CreateSquare(Vector3 center, float sideLength)
        {
            float halfLength = sideLength * 0.5f;

            Vector3 p1 = center + new Vector3(-halfLength, 0, -halfLength);
            Vector3 p2 = center + new Vector3(halfLength, 0, -halfLength);
            Vector3 p3 = center + new Vector3(halfLength, 0, halfLength);
            Vector3 p4 = center + new Vector3(-halfLength, 0, halfLength);

            return new Vector3[] { p1, p2, p3, p4 };
        }

        /// <summary>
        /// Generates the vertices of a circle (approximated with a polygon) centered at a given position.
        /// </summary>
        public static Vector3[] CreateCircle(Vector3 center, float radius, int edgeCount)
        {
            if (edgeCount < 3)
                edgeCount = 3;

            Vector3[] vertices = new Vector3[edgeCount];
            float angleStep = 360f / edgeCount;

            for (int i = 0; i < edgeCount; i++)
            {
                float angle = Mathf.Deg2Rad * (angleStep * i);
                float x = center.x + radius * Mathf.Cos(angle);
                float z = center.z + radius * Mathf.Sin(angle);

                vertices[i] = new Vector3(x, center.y, z);
            }

            return vertices;
        }

        public static void DrawShapeGizmos(IShape shape, Color color)
        {
            if(shape == null) return;

            DrawShapeGizmos(shape.GetVertices(), color);
        }

        /// <summary>
        /// Draws a shape using Gizmos given its vertices.
        /// </summary>
        public static void DrawShapeGizmos(Vector3[] vertices, Color color)
        {
            if (vertices == null || vertices.Length < 2)
            {
                Debug.LogError("Invalid vertices array. It must contain at least 2 vertices.");
                return;
            }

            Gizmos.color = color;

            int vertexCount = vertices.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                Gizmos.DrawLine(vertices[i], vertices[(i + 1) % vertexCount]);
            }
        }
    }

}
