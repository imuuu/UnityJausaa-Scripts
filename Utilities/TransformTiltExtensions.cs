using UnityEngine;

public enum TiltAxis { Total, X, Z }

public static class TransformTiltExtensions
{
    /// <summary>
    /// Gets tilt information relative to world Y-axis
    /// </summary>
    public static float GetTilt(this Transform transform, TiltAxis axis = TiltAxis.Total)
    {
        Vector3 up = transform.up;

        return axis switch
        {
            TiltAxis.X => Mathf.Asin(Mathf.Clamp(up.z, -1f, 1f)) * Mathf.Rad2Deg,
            TiltAxis.Z => Mathf.Asin(Mathf.Clamp(up.x, -1f, 1f)) * Mathf.Rad2Deg,
            _ => Vector3.Angle(up, Vector3.up)
        };
    }

    /// <summary>
    /// Gets both lateral tilt components (X and Z) as a Vector2
    /// </summary>
    public static Vector2 GetLateralTilt(this Transform transform)
    {
        Vector3 up = transform.up;
        return new Vector2(
            Mathf.Asin(Mathf.Clamp(up.z, -1f, 1f)) * Mathf.Rad2Deg,
            Mathf.Asin(Mathf.Clamp(up.x, -1f, 1f)) * Mathf.Rad2Deg
        );
    }

    /// <summary>
    /// Gets the tilt direction vector in XZ plane (normalized)
    /// </summary>
    public static Vector3 GetTiltDirection(this Transform transform)
    {
        Vector3 up = transform.up;
        return new Vector3(up.x, 0f, up.z).normalized;
    }
}