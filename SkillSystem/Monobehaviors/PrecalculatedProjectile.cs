using UnityEngine;
using System.Collections.Generic;

public class PrecalculatedProjectile : MonoBehaviour
{
    [Header("Trajectory Settings")]
    public float speed = 20f;
    public float launchAngle = 45f; // in degrees
    public int numberOfPoints = 30;
    public float gravity = 9.81f;
    public float impactY = 0f;  // target height (for ground impact)

    private List<Vector3> trajectoryPoints;
    private int currentPointIndex = 0;
    private bool isLaunched = false;
    private float flightDuration;

    // Called to initialize and launch the projectile.
    public void Launch(Vector3 startPosition, Vector3 direction)
    {
        transform.position = startPosition;
        trajectoryPoints = CalculateTrajectory(startPosition, direction);
        if (trajectoryPoints == null || trajectoryPoints.Count == 0)
        {
            Debug.LogError("Failed to calculate trajectory");
            return;
        }
        isLaunched = true;
        currentPointIndex = 0;
    }

    // Calculate the trajectory given a start, direction, and flight parameters.
    private List<Vector3> CalculateTrajectory(Vector3 startPos, Vector3 direction)
    {
        List<Vector3> points = new List<Vector3>();
        direction = direction.normalized;
        float radAngle = launchAngle * Mathf.Deg2Rad;

        // Decompose the initial velocity into horizontal and vertical components.
        float v0 = speed;
        float v0x = v0 * Mathf.Cos(radAngle);
        float v0y = v0 * Mathf.Sin(radAngle);

        // Calculate flight duration using the quadratic formula.
        // y(t) = startPos.y + v0y * t - 0.5 * gravity * t^2 = impactY
        // 0.5 * gravity * t^2 - v0y * t + (startPos.y - impactY) = 0
        float a = 0.5f * gravity;
        float b = -v0y;
        float c = impactY - startPos.y;

        Debug.Log($"v0y: {v0y}, startPos.y: {startPos.y}, impactY: {impactY}");
        Debug.Log($"a: {a}, b: {b}, c: {c}");
        float discriminant = b * b - 4 * a * c;
        Debug.Log($"Discriminant: {discriminant}");


        if (discriminant < 0)
        {
            Debug.LogError("No valid impact time found");
            return null;
        }
        // Use the positive root (t = (-b + sqrt(discriminant)) / (2*a))
        flightDuration = (-b + Mathf.Sqrt(discriminant)) / (2 * a);

        // Determine the horizontal velocity vector (ignoring vertical component).
        Vector3 horizontalVelocity = direction * v0x;

        // Sample the trajectory at equidistant time intervals.
        for (int i = 0; i <= numberOfPoints; i++)
        {
            float t = (i / (float)numberOfPoints) * flightDuration;
            // Horizontal displacement:
            Vector3 horizontalDisplacement = horizontalVelocity * t;
            // Vertical displacement:
            float verticalDisplacement = v0y * t - 0.5f * gravity * t * t;
            Vector3 point = startPos + horizontalDisplacement + Vector3.up * verticalDisplacement;
            points.Add(point);
        }
        return points;
    }

    void Update()
    {
        // If launched, move along the pre-calculated trajectory.
        if (!isLaunched || trajectoryPoints == null) return;

        // Move towards the next point
        if (currentPointIndex < trajectoryPoints.Count)
        {
            // Simple movement: you can use Lerp or move-to-point logic here.
            transform.position = Vector3.MoveTowards(transform.position, trajectoryPoints[currentPointIndex], speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, trajectoryPoints[currentPointIndex]) < 0.1f)
            {
                currentPointIndex++;
            }
        }
        else
        {
            // We've reached the final point (the impact). Trigger explosion.
            Explode();
            // Reset or disable further updates.
            isLaunched = false;
        }
    }

    // Draw the trajectory in the Unity editor for debugging purposes.
    private void OnDrawGizmos()
    {
        if (trajectoryPoints != null && trajectoryPoints.Count > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < trajectoryPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
            }
        }
    }

    void Explode()
    {
        // Trigger the explosion effect, area damage, etc.
        Debug.Log("Explosion triggered at: " + transform.position);
        // Optionally, return the projectile to your pool rather than destroying it:
        // ManagerPrefabPooler.Instance.ReturnToPool(gameObject);
    }
}
