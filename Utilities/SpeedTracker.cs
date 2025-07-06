using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Utility component to track the average speed of a Transform or GameObject over time.
/// Records position samples at a configurable interval, keeps a sliding window of samples,
/// and computes average speed.
/// </summary>
public class SpeedTracker : MonoBehaviour
{
    [Title("SPEED")][PropertySpace(10,10)][BoxGroup("Speed Settings")]
    [SerializeField, ReadOnly] private float speed = 0f;
    [Header("Target Settings")]
    [Tooltip("Transform to track. If null, uses this GameObject's transform.")]
    public Transform target;

    [Header("Sampling Settings")]
    [Tooltip("Time in seconds between each position sample.")]
    public float sampleInterval = 1f;
    [Tooltip("Number of samples to keep for speed calculation. Older samples are discarded.")]
    public int sampleCount = 5;

    // Internal list of past samples (time and position)
    private readonly Queue<Sample> samples = new Queue<Sample>();
    private float timer;

    private void Awake()
    {
        if (target == null)
            target = transform;
    }

    private void Update()
    {
        // advance timer
        timer += Time.deltaTime;
        if (timer >= sampleInterval)
        {
            RecordSample();
            timer = 0f;
        }

        speed = GetAverageSpeed();
    }

    /// <summary>
    /// Records a new sample of the target's position and time.
    /// Maintains the sample window by discarding old samples.
    /// </summary>
    private void RecordSample()
    {
        samples.Enqueue(new Sample(Time.time, target.position));

        // Remove oldest if beyond sampleCount
        while (samples.Count > sampleCount)
        {
            samples.Dequeue();
        }
    }

    /// <summary>
    /// Calculates the average speed (units per second) of the target over the recorded samples.
    /// </summary>
    /// <returns>Average speed in world units per second.</returns>
    public float GetAverageSpeed()
    {
        if (samples.Count < 2)
            return 0f;

        Sample[] arr = samples.ToArray();
        float totalDist = 0f;
        float totalTime = arr[arr.Length - 1].time - arr[0].time;

        if (totalTime <= 0f)
            return 0f;

        for (int i = 1; i < arr.Length; i++)
        {
            totalDist += Vector3.Distance(arr[i - 1].position, arr[i].position);
        }

        return totalDist / totalTime;
    }

    /// <summary>
    /// Convenience static method: tracks speed by adding a SpeedTracker to the given GameObject if not present.
    /// </summary>
    /// <param name="go">GameObject to track.</param>
    /// <param name="interval">Sampling interval in seconds.</param>
    /// <param name="count">Number of samples to average.</param>
    /// <returns>The SpeedTracker instance.
    /// You can call GetAverageSpeed() on this to retrieve the speed.</returns>
    public static SpeedTracker Track(GameObject go, float interval = 1f, int count = 5)
    {
        var st = go.GetComponent<SpeedTracker>();
        if (st == null)
            st = go.AddComponent<SpeedTracker>();
        st.sampleInterval = interval;
        st.sampleCount = count;
        st.target = go.transform;
        return st;
    }

    // Internal sample structure
    private struct Sample
    {
        public float time;
        public Vector3 position;

        public Sample(float t, Vector3 pos)
        {
            time = t;
            position = pos;
        }
    }
}
