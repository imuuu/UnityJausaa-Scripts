using Unity.Entities;
using UnityEngine;

public class PlayerViewPortAuthoring : MonoBehaviour
{
    [Header("Viewport Corners (world space)")]
    [Tooltip("Corner 0 of the viewport polygon.")]
    public Vector3 Vertex0;
    [Tooltip("Corner 1 of the viewport polygon.")]
    public Vector3 Vertex1;
    [Tooltip("Corner 2 of the viewport polygon.")]
    public Vector3 Vertex2;
    [Tooltip("Corner 3 of the viewport polygon.")]
    public Vector3 Vertex3;

    // Draw gizmos in the Scene view for debugging.
    private void OnDrawGizmos()
    {
        // Choose a color for the gizmos
        Gizmos.color = Color.green;

        // Create an array of the vertices
        Vector3[] vertices = new Vector3[] { Vertex0, Vertex1, Vertex2, Vertex3 };

        // Draw a small sphere at each vertex
        foreach (var vertex in vertices)
        {
            Gizmos.DrawSphere(vertex, 0.2f);
        }

        // Draw lines between consecutive vertices to form the polygon (wrap around at the end)
        for (int i = 0; i < vertices.Length; i++)
        {
            int nextIndex = (i + 1) % vertices.Length;
            Gizmos.DrawLine(vertices[i], vertices[nextIndex]);
        }
    }

    public class PlayerViewPortBaker : Baker<PlayerViewPortAuthoring>
    {
        public override void Bake(PlayerViewPortAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerViewPortData
            {
                Vertex0 = authoring.Vertex0,
                Vertex1 = authoring.Vertex1,
                Vertex2 = authoring.Vertex2,
                Vertex3 = authoring.Vertex3,
            });
        }
    }
}
