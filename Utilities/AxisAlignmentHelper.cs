using UnityEngine;

public static class AxisAlignmentHelper
{
    /// <summary>
    /// Returns true if the given direction is almost perfectly aligned with the transform's right (X) axis.
    /// </summary>
    public static bool IsAlignedWithXAxis(Transform transform, Vector3 direction, float tolerance = 0.01f)
    {
        // The dot product between two normalized vectors is 1 (or -1) if they're parallel (or anti-parallel).
        float dot = Vector3.Dot(direction.normalized, transform.right);
        return Mathf.Abs(dot) >= 1.0f - tolerance;
    }

    /// <summary>
    /// Returns true if the given direction is almost perfectly aligned with the transform's forward (Z) axis.
    /// </summary>
    public static bool IsAlignedWithZAxis(Transform transform, Vector3 direction, float tolerance = 0.01f)
    {
        float dot = Vector3.Dot(direction.normalized, transform.forward);
        return Mathf.Abs(dot) >= 1.0f - tolerance;
    }

    /// <summary>
    /// Returns true if the direction is aligned with either the transform's X or Z axis.
    /// </summary>
    public static bool IsAlignedWithXorZAxis(Transform transform, Vector3 direction, float tolerance = 0.01f)
    {
        return IsAlignedWithXAxis(transform, direction, tolerance) ||
               IsAlignedWithZAxis(transform, direction, tolerance);
    }

}
