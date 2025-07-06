using Game.Shapes;
using UnityEngine;

public class PlayerViewPort : MonoBehaviour 
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

    private ShapePolygon _shapePolygon;

    private void OnDrawGizmos()
    {
        if(ManagerPlayerViewPort.Instance != null && !ManagerPlayerViewPort.Instance.DebugViewPort) return;

        Gizmos.color = Color.green;

        Vector3 pos = GetPosition();
        Vector3[] vertices = new Vector3[] { Vertex0+pos, Vertex1 + pos, Vertex2 + pos, Vertex3 + pos };

        foreach (var vertex in vertices)
        {
            Gizmos.DrawSphere(vertex, 0.2f);
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            int nextIndex = (i + 1) % vertices.Length;
            Gizmos.DrawLine(vertices[i], vertices[nextIndex]);
        }
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public ShapePolygon GetShape()
    {
        if(_shapePolygon != null) return _shapePolygon;

        Vector3 pos = GetPosition();
        Vector3[] vertices = new Vector3[] { Vertex0, Vertex1, Vertex2, Vertex3};
        ShapePolygon shape = new ShapePolygon
        {
            LocalVertices = vertices,
            Center = pos,
        };
        return shape;
    }

}