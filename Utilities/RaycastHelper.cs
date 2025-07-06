using UnityEngine;
using System.Collections.Generic;

public static class RaycastHelper
{
    /// <summary>
    /// Eight local directions (in the collider’s local space) arranged in a circle.
    /// Index 0 is Vector3.back (the “back”), then the other directions follow.
    /// </summary>
    private static readonly List<Vector3> _localDirections = new List<Vector3>
    {
        Vector3.back,                      // 0: "back" (−Z)
        //Vector3.back + Vector3.right,      // 1: back-right
        Vector3.right,                     // 2
        //Vector3.forward + Vector3.right,   // 3: front-right
        Vector3.forward,                   // 4
        //Vector3.forward + Vector3.left,    // 5: front-left
        Vector3.left,                      // 6
        //Vector3.back + Vector3.left        // 7: back-left
    };

    private static System.Random _random = new System.Random();

    /// <summary>
    /// Tries all directions (starting with "back") and returns the first direction
    /// (converted to world space) in which two raycasts—shot from the top and bottom of the
    /// collider’s bounds—do NOT hit anything.
    /// 
    /// The raycast distance is provided as a parameter. If not provided (or ≤0), it defaults
    /// to the collider’s bounds height (size.y).
    /// </summary>
    public static bool TryGetFirstFreeDirectionTopBottomChecks(
        Collider col,
        out Vector3 freeDirection,
        bool randomAfterBack = true,
        float raycastDistance = -1f
    )
    {
        freeDirection = Vector3.zero;
        if (col == null) return false;

        if (raycastDistance <= 0f)
            raycastDistance = col.bounds.size.y;

        List<Vector3> directions = new List<Vector3>(_localDirections);
        for (int i = 0; i < directions.Count; i++)
            directions[i] = directions[i].normalized;

        if (randomAfterBack)
        {
            List<Vector3> subset = directions.GetRange(1, directions.Count - 1);
            Shuffle(subset);
            for (int i = 1; i < directions.Count; i++)
                directions[i] = subset[i - 1];
        }

        foreach (var localDir in directions)
        {
            Vector3 worldDir = col.transform.TransformDirection(localDir);
            if (IsDirectionFree(col, worldDir, raycastDistance))
            {
                freeDirection = worldDir;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the given direction is free by shooting two raycasts (from the top and bottom
    /// points of the collider’s bounds) in that world direction.
    /// </summary>
    private static bool IsDirectionFree(Collider col, Vector3 worldDir, float distance)
    {
        Bounds b = col.bounds;
        Vector3 center = b.center;
        Vector3 ext = b.extents;

        Vector3 top = center + Vector3.up * ext.y;
        Vector3 bottom = center - Vector3.up * ext.y;

        bool hitTop = Physics.Raycast(top, worldDir, out _, distance);
        bool hitBottom = Physics.Raycast(bottom, worldDir, out _, distance);

        //UnityEngine.Debug.DrawRay(top, worldDir * distance, Color.red, 2f);
        //UnityEngine.Debug.DrawRay(bottom, worldDir * distance, Color.red, 2f);

        // Direction is free if neither ray hits an obstacle.
        return !hitTop && !hitBottom;
    }

    /// <summary>
    /// Tries all directions (starting with "back") and returns the first direction
    /// (converted to world space) in which two raycasts—shot from the left and right sides of
    /// the collider’s bounds—do NOT hit anything.
    /// 
    /// The raycast distance is provided as a parameter. If not provided (or ≤0), it defaults
    /// to the collider’s bounds height (size.y).
    /// </summary>
    public static bool TryGetFirstFreeDirectionSidesChecks(
        Collider col,
        out Vector3 freeDirection,
        bool randomAfterBack = true,
        float raycastDistance = -1f
    )
    {
        freeDirection = Vector3.zero;
        if (col == null) return false;

        if (raycastDistance <= 0f)
            raycastDistance = col.bounds.size.y;

        List<Vector3> directions = new List<Vector3>(_localDirections);
        for (int i = 0; i < directions.Count; i++)
            directions[i] = directions[i].normalized;

        if (randomAfterBack)
        {
            List<Vector3> subset = directions.GetRange(1, directions.Count - 1);
            Shuffle(subset);
            for (int i = 1; i < directions.Count; i++)
                directions[i] = subset[i - 1];
        }

        foreach (var localDir in directions)
        {
            Vector3 worldDir = col.transform.TransformDirection(localDir);
            if (IsDirectionFreeSides(col, worldDir, raycastDistance))
            {
                freeDirection = worldDir;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the given direction is free by shooting two raycasts (from the left and right
    /// side points of the collider’s bounds) in that world direction.
    /// 
    /// It computes the left/right points by using a perpendicular (right) vector defined as the
    /// cross of world up and the raycast direction.
    /// </summary>
    private static bool IsDirectionFreeSides(Collider col, Vector3 worldDir, float rayLength)
    {
        Vector3 center = col.bounds.center;
        Vector3 up = Vector3.up;
        Vector3 rightVector = Vector3.Cross(up, worldDir).normalized;

        // Compute the effective half width along the rightVector.
        // For an AABB, the half-length along an arbitrary axis is the dot product of the extents with the absolute axis components.
        Vector3 ext = col.bounds.extents;
        float halfWidth = ext.x * Mathf.Abs(rightVector.x) +
                          ext.y * Mathf.Abs(rightVector.y) +
                          ext.z * Mathf.Abs(rightVector.z);

        Vector3 leftPoint = center - rightVector * halfWidth;
        Vector3 rightPoint = center + rightVector * halfWidth;

        bool hitLeft = Physics.Raycast(leftPoint, worldDir, out _, rayLength);
        bool hitRight = Physics.Raycast(rightPoint, worldDir, out _, rayLength);

        //UnityEngine.Debug.DrawRay(leftPoint, worldDir * rayLength, Color.blue, 2f);
        //UnityEngine.Debug.DrawRay(rightPoint, worldDir * rayLength, Color.blue, 2f);

        return !hitLeft && !hitRight;
    }

    /// <summary>
    /// Fisher–Yates shuffle used for randomizing the list.
    /// </summary>
    private static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
