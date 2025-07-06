using System.Collections.Generic;
using UnityEngine;

public class PointsUtilities
{
    /// <summary>
    /// The axis enum is used to specify which plane (or rotation axis) to use when calculating a new point.
    /// For example, if you choose Axis.Y the “straight” direction is defined as Vector3.forward (movement on the XZ plane)
    /// and deviations are applied by rotating around the Y axis.
    /// </summary>
    public enum Axis { X, Y, Z }

    #region Draw Lines

    /// <summary>
    /// Draws line segments connecting each consecutive pair of points in the array.
    /// Uses Debug.DrawLine so the lines are visible in the Scene view (with Gizmos enabled) during Play mode.
    /// </summary>
    /// <param name="points">Array of points to connect.</param>
    /// <param name="color">Color of the lines.</param>
    /// <param name="duration">How long the lines should be visible (in seconds; 0 means one frame).</param>
    public static void DrawLines(Vector3[] points, Color color, float duration = 0f)
    {
        if (points == null || points.Length < 2)
            return;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Debug.DrawLine(points[i], points[i + 1], color, duration);
        }
    }

    #endregion

    #region Median and Direction

    /// <summary>
    /// Computes the centroid (average position) of an array of points and then computes an overall direction
    /// as the normalized vector from the first to the last point in the array.
    /// It draws a debug ray (of length rayLength) from the centroid in that direction.
    /// </summary>
    /// <param name="points">Array of points.</param>
    /// <param name="medianColor">Color of the debug ray.</param>
    /// <param name="rayLength">Length of the debug ray drawn from the median point.</param>
    /// <param name="duration">Duration the debug ray is visible (in seconds).</param>
    /// <returns>The centroid (median) of the points.</returns>
    public static Vector3 DrawMedianDirection(Vector3[] points, Color medianColor, float rayLength = 1f, float duration = 0f)
    {
        if (points == null || points.Length == 0)
            return Vector3.zero;

        // Calculate the centroid (average position)
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pt in points)
        {
            sum += pt;
        }
        Vector3 centroid = sum / points.Length;

        // Determine overall direction.
        // Here we simply take the direction from the first to the last point.
        Vector3 direction = Vector3.forward; // fallback
        if (points.Length >= 2)
        {
            direction = (points[points.Length - 1] - points[0]).normalized;
        }

        // Draw a debug ray from the centroid
        Debug.DrawRay(centroid, direction * rayLength, medianColor, duration);

        return centroid;
    }

    #endregion

    #region Calculate Offset Point

    /// <summary>
    /// Returns a new point computed from a starting point plus an offset of a given length.
    /// The offset is computed by starting from a “base” direction (which is chosen based on the provided axis)
    /// and then rotating that direction by a deviation angle.
    /// 
    /// If angle is 0 the new point lies exactly in the base direction.
    /// If a nonzero angle is provided then (if randomize is false) the deviation is set to angle/2,
    /// otherwise (if randomize is true) the deviation is chosen randomly between -angle/2 and +angle/2.
    /// 
    /// The enum <c>Axis</c> tells the method which axis to use as the rotation axis.
    /// The following convention is used:
    ///   - Axis.Y: Base direction is Vector3.forward (movement on the XZ plane), rotation axis is Vector3.up.
    ///   - Axis.X: Base direction is Vector3.up (movement on the YZ plane), rotation axis is Vector3.right.
    ///   - Axis.Z: Base direction is Vector3.up (movement on the XY plane), rotation axis is Vector3.forward.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="axis">The axis that defines the plane of movement and the rotation axis.</param>
    /// <param name="length">Length of the segment.</param>
    /// <param name="angle">Total deviation angle in degrees. For example, 60 means the actual deviation will be ±30°.</param>
    /// <param name="randomize">If true, a random deviation (in the interval [-angle/2, +angle/2]) is applied; otherwise a fixed deviation of angle/2 is used.</param>
    /// <returns>The computed end point.</returns>
    public static Vector3 GetPointFromDirection(Vector3 start, Axis axis, float length, float angle, bool randomize = false)
    {
        // Calculate deviation angle.
        // If angle == 0 the deviation remains 0 (i.e. the line goes “straight”).
        float deviation = 0f;
        if (angle != 0f)
        {
            // For a total spread of 'angle' degrees, use ±angle/2.
            deviation = randomize ? Random.Range(-angle / 2f, angle / 2f) : angle / 2f;
        }

        // Choose base direction and rotation axis based on the selected axis.
        Vector3 baseDirection = Vector3.zero;
        Vector3 rotationAxis = Vector3.zero;
        switch (axis)
        {
            case Axis.X:
                // Movement is in the YZ plane.
                baseDirection = Vector3.up;
                rotationAxis = Vector3.right;
                break;
            case Axis.Y:
                // Movement is in the XZ plane.
                baseDirection = Vector3.forward;
                rotationAxis = Vector3.up;
                break;
            case Axis.Z:
                // Movement is in the XY plane.
                baseDirection = Vector3.up;
                rotationAxis = Vector3.forward;
                break;
        }

        // Rotate the base direction by the calculated deviation.
        Vector3 newDirection = Quaternion.AngleAxis(deviation, rotationAxis) * baseDirection;
        return start + newDirection.normalized * length;
    }

    public static Vector3[] GetPointsInLine(Vector3 startPoint, Vector3 endPoint, int numPoints)
    {
        Vector3[] points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            float t = i / (numPoints - 1f);
            points[i] = Vector3.Lerp(startPoint, endPoint, t);
        }
        return points;
    }

    public static List<Vector3> GetCirclePointsXZ(Transform transform, int numberOfPoints, float radius)
    {
        List<Vector3> points = new();

        float angleStep = 360f / numberOfPoints;

        for (int i = 0; i < numberOfPoints; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            points.Add(point);
        }

        return points;
    }

    #endregion
}
