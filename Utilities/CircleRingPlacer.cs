#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class CircleRingPlacer
{
    /// <summary>
    /// Compute N center points for small circles (radius rSmall) placed inside a larger
    /// circle (radius rMain). Each small circle is tangent to the large one (never outside).
    /// Points are equally spaced around the ring.
    /// 
    /// Returns false if the layout cannot fit without overlaps between small circles.
    /// </summary>
    /// <param name="center">Center of the large circle (world space).</param>
    /// <param name="rMain">Radius of the large (black) circle.</param>
    /// <param name="rSmall">Radius of each small (red) circle.</param>
    /// <param name="count">How many small circles to place.</param>
    /// <param name="firstDirection">
    /// Direction for the first point. Use world-space direction:
    ///  - 2D: e.g. Vector3.up (XY plane)
    ///  - 3D top-down: e.g. Vector3.forward (XZ plane)
    /// </param>
    /// <param name="useXZPlane">True = positions on XZ plane (y locked);
    /// False = positions on XY plane (z locked).</param>
    /// <param name="positions">Output array of centers.</param>
    /// <param name="edgeInset">
    /// Optional small inset so the small circles are safely inside (e.g. 0.001f).
    /// Set 0 to be exactly tangent.
    /// </param>
    /// <returns>True if placed without overlaps, otherwise false.</returns>
    public static bool TryPlaceOnInnerTangentRing(
        Vector3 center,
        float rMain,
        float rSmall,
        int count,
        Vector3 firstDirection,
        bool useXZPlane,
        out Vector3[] positions,
        float edgeInset = 0f)
    {
        positions = null;

        if (count <= 0 || rMain <= 0f || rSmall <= 0f) return false;

        // Radius of the ring where the centers sit (tangent from inside).
        float ringR = rMain - rSmall - Mathf.Max(0f, edgeInset);
        if (ringR <= 0f) return false; // small circle doesn't fit inside at all

        // If count >= 2, ensure neighbors won't overlap each other.
        if (count >= 2)
        {
            float chord = 2f * ringR * Mathf.Sin(Mathf.PI / count); // distance between adjacent centers
            if (chord + 1e-6f < 2f * rSmall) return false; // cannot fit without overlap
        }

        positions = new Vector3[count];

        // Normalize the starting direction on the chosen plane.
        Vector2 dir2;
        float yFixed = center.y;
        float zFixed = center.z;

        if (useXZPlane)
        {
            Vector2 d = new Vector2(firstDirection.x, firstDirection.z);
            dir2 = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up;
        }
        else
        {
            Vector2 d = new Vector2(firstDirection.x, firstDirection.y);
            dir2 = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up;
        }

        float baseAngle = Mathf.Atan2(dir2.y, dir2.x);
        float step = (2f * Mathf.PI) / count;

        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + step * i;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            if (useXZPlane)
            {
                positions[i] = new Vector3(
                    center.x + ringR * cos,
                    yFixed,
                    center.z + ringR * sin
                );
            }
            else
            {
                positions[i] = new Vector3(
                    center.x + ringR * cos,
                    center.y + ringR * sin,
                    zFixed
                );
            }
        }

        return true;
    }

    /// <summary>
    /// Maximum number of equal small circles that can sit tangent to the big circle
    /// (no overlaps) for given radii. Returns at least 1 if a single one fits.
    /// </summary>
    public static int MaxCountThatFits(float rMain, float rSmall)
    {
        float ringR = rMain - rSmall;
        if (ringR <= 0f) return 0;                // none fit
        if (rSmall > ringR) return 1;             // only 1 fits without touching neighbors

        // For N>=2: 2*ringR*sin(pi/N) >= 2*rSmall  ->  sin(pi/N) >= rSmall/ringR
        float x = Mathf.Clamp01(rSmall / ringR);
        int n = Mathf.FloorToInt(Mathf.PI / Mathf.Asin(x));
        return Mathf.Max(1, n);
    }

    /// <summary>
    /// Draws the layout using Debug.DrawLine (shows in Scene/Game during Play).
    /// </summary>
    public static void DebugDrawLayout(
        Vector3 center, float rMain, float rSmall, int count, Vector3 firstDirection,
        bool useXZPlane, float edgeInset = 0f, float duration = 0f)
    {
        if (!TryPlaceOnInnerTangentRing(center, rMain, rSmall, count, firstDirection, useXZPlane,
                                        out var points, edgeInset))
        {
            Debug.LogWarning("[CircleRingPlacer] Layout doesn't fit. Nothing drawn.");
            return;
        }

        // Plane basis
        Vector3 axisX = Vector3.right;
        Vector3 axisY = useXZPlane ? Vector3.forward : Vector3.up;

        // Big circle
        DrawCircleDebug(center, axisX, axisY, rMain, duration);

        // Lines from center + small circles
        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(center, points[i], Color.white, duration);
            DrawCircleDebug(points[i], axisX, axisY, rSmall, duration);
        }
    }

    // ---------- internal helpers ----------

    static void DrawCircleDebug(Vector3 center, Vector3 axisX, Vector3 axisY, float radius, float duration)
    {
        const int Segments = 64;
        Vector3 prev = center + axisX * radius;
        for (int i = 1; i <= Segments; i++)
        {
            float t = (i / (float)Segments) * Mathf.PI * 2f;
            Vector3 p = center + (Mathf.Cos(t) * axisX + Mathf.Sin(t) * axisY) * radius;
            Debug.DrawLine(prev, p, Color.cyan, duration);
            prev = p;
        }
    }


}


public static class CircleRingPlacerGizmos
{
    // Draws with Handles/Gizmos so it works in OnDrawGizmos/OnDrawGizmosSelected.
    public static void DrawLayoutGizmos(
        Vector3 center, float rMain, float rSmall, int count, Vector3 firstDirection,
        bool useXZPlane, float edgeInset = 0f,
        Color outer = default, Color small = default, Color lines = default)
    {
#if UNITY_EDITOR
        if (outer == default) outer = Color.black;
        if (small == default) small = Color.red;
        if (lines == default) lines = Color.white;

        if (!CircleRingPlacer.TryPlaceOnInnerTangentRing(
                center, rMain, rSmall, count, firstDirection, useXZPlane, out var pts, edgeInset))
        {
            Handles.color = Color.yellow;
            Handles.Label(center, "Doesn't fit");
            return;
        }

        Vector3 normal = useXZPlane ? Vector3.up : Vector3.forward;

        // Big circle
        Handles.color = outer;
        Handles.DrawWireDisc(center, normal, rMain);

        // Spokes
        Handles.color = lines;
        for (int i = 0; i < pts.Length; i++)
            Handles.DrawLine(center, pts[i]);

        // Small circles
        Handles.color = small;
        for (int i = 0; i < pts.Length; i++)
            Handles.DrawWireDisc(pts[i], normal, rSmall);
#endif
    }
}

