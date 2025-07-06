using UnityEngine;
using System.Collections.Generic;

public class FullBouncePathWithTime : MonoBehaviour
{
    [Header("Trajectory Settings")]
    public float speed = 20f;            // Initial speed for the first bounce.
    public float launchAngle = 45f;      // Initial launch angle in degrees.
    public float gravity = 9.81f;
    public float impactY = 0f;           // Impact height (for example, the ground level).

    [Header("Bounce Settings")]
    public int bounceCount = 3;          // Total number of bounces (0 means a single arc).
    [Range(0f, 1f)]
    public float dampingFactor = 0.8f;   // Reduction factor for speed after each bounce.
    [Range(0f, 1f)]
    public float angleDampingFactor = 1f; // Optionally, dampen the angle as well.

    [Header("Sampling Settings")]
    public int samplesPerBounce = 30;    // How many samples for each bounce arc.

    // Lists to hold the precomputed positions and their corresponding timestamps.
    private List<Vector3> samplePositions = new List<Vector3>();
    private List<float> sampleTimes = new List<float>();  // In seconds.

    private float totalFlightTime = 0f; // Total simulated time for all bounce segments.
    private float currentTime = 0f;     // Global time counter.

    private bool isLaunched = false;

    // Launch the projectile along its natural, physics-timed trajectory.
    // horizontalDirection should be a normalized horizontal vector.
    public void Launch(Vector3 startPosition, Vector3 horizontalDirection)
    {
        transform.position = startPosition;
        ComputeFullPath(startPosition, horizontalDirection.normalized);
        currentTime = 0f;
        isLaunched = true;
    }

    // Precompute the full path over all bounce segments with their physics time.
    private void ComputeFullPath(Vector3 startPosition, Vector3 horizontalDirection)
    {
        samplePositions.Clear();
        sampleTimes.Clear();
        totalFlightTime = 0f;

        // Set up the initial state.
        Vector3 currentStart = startPosition;
        float currentSpeed = speed;
        float currentAngle = launchAngle;

        // Always include the start position at time 0.
        samplePositions.Add(currentStart);
        sampleTimes.Add(totalFlightTime);

        // For each bounce segment (including the initial arc and subsequent bounces).
        for (int bounce = 0; bounce <= bounceCount; bounce++)
        {
            // Convert angle to radians.
            float radAngle = currentAngle * Mathf.Deg2Rad;
            // Decompose the initial velocity.
            float v0x = currentSpeed * Mathf.Cos(radAngle);
            float v0y = currentSpeed * Mathf.Sin(radAngle);

            // Solve for the flight time of this segment (when the projectile goes from currentStart.y to impactY).
            // The equation is: currentStart.y + v0y*t - 0.5*g*t^2 = impactY.
            // Rearranged using standard form (using impactY - currentStart.y for proper sign): 
            // 0.5*g*t^2 - v0y*t + (currentStart.y - impactY) = 0.
            // Here we compute t using c = impactY - currentStart.y.
            float a = 0.5f * gravity;
            float b = -v0y;
            float c = impactY - currentStart.y;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                Debug.LogError("No valid impact time found on bounce " + bounce);
                break;
            }
            // Use the positive solution.
            float flightTime = (-b + Mathf.Sqrt(discriminant)) / (2 * a);

            // Sample points along this bounce segment using the actual physics time.
            // We'll sample from t = 0 to t = flightTime.
            for (int i = 1; i <= samplesPerBounce; i++)
            {
                float t = (i / (float)samplesPerBounce) * flightTime;
                // Calculate horizontal displacement (which is constant in speed).
                Vector3 horizontalDisp = horizontalDirection * v0x * t;
                // Calculate vertical displacement.
                float verticalDisp = v0y * t - 0.5f * gravity * t * t;
                Vector3 samplePoint = currentStart + horizontalDisp + Vector3.up * verticalDisp;

                // Increase total time by t (note: t here is relative to the start of the segment).
                float sampleGlobalTime = totalFlightTime + t;
                samplePositions.Add(samplePoint);
                sampleTimes.Add(sampleGlobalTime);
            }

            // Update total flight time with the flight time of this bounce.
            totalFlightTime += flightTime;
            // The end of the current segment becomes the start for the next bounce.
            currentStart = samplePositions[samplePositions.Count - 1];

            // Apply damping to speed (and optionally to angle) for the next bounce.
            currentSpeed *= dampingFactor;
            currentAngle *= angleDampingFactor;
        }
    }

    void Update()
    {
        if (!isLaunched || samplePositions.Count < 2)
            return;

        // Increment the global time by the real elapsed time.
        currentTime += Time.deltaTime;

        // If we've reached (or exceeded) the full flight time, place the object at the last sample.
        if (currentTime >= totalFlightTime)
        {
            transform.position = samplePositions[samplePositions.Count - 1];
            isLaunched = false;
            Debug.Log("Final impact at: " + transform.position);
            return;
        }

        // Find the current segment by checking which two sample times bracket the currentTime.
        int index = 0;
        for (int i = 0; i < sampleTimes.Count - 1; i++)
        {
            if (sampleTimes[i] <= currentTime && currentTime <= sampleTimes[i + 1])
            {
                index = i;
                break;
            }
        }
        // Calculate an interpolation factor (between 0 and 1) between the two sample points.
        float tSegment = Mathf.InverseLerp(sampleTimes[index], sampleTimes[index + 1], currentTime);
        // Smoothly interpolate between these two positions.
        transform.position = Vector3.Lerp(samplePositions[index], samplePositions[index + 1], tSegment);
    }

    // For debugging: draw the full precomputed path with Gizmos.
    private void OnDrawGizmos()
    {
        if (samplePositions == null || samplePositions.Count < 2)
            return;

        Gizmos.color = Color.red;
        for (int i = 0; i < samplePositions.Count - 1; i++)
        {
            Gizmos.DrawLine(samplePositions[i], samplePositions[i + 1]);
        }
    }
}
