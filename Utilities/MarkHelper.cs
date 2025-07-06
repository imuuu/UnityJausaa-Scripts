using System.Collections;
using UnityEngine;

public static class MarkHelper
{
    public static GameObject DrawSphereTimed(Vector3 position, float radius, float duration, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * radius * 2;
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        GameObject.Destroy(sphere, duration);
        return sphere;
    }

    public static void DrawLineTimed(Vector3 start, Vector3 end, float duration, Color color)
    {
        GameObject lineObject = new GameObject("GizmoLine");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = color;
        GameObject.Destroy(lineObject, duration);
    }
}