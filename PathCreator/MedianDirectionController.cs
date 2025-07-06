using System.Linq;
using UnityEngine;
using Utils;

[RequireComponent(typeof(Transform))]
public class MedianDirectionController : MonoBehaviour
{
    [Header("Filter Settings")]
    [Tooltip("How many frames of history to keep when computing the median path.")]
    public int historySize = 5;

    [Tooltip("How quickly to rotate toward the filtered direction.")]
    public float turnSpeed = 2f;

    [Tooltip("Toggle visual debug lines in the Scene view.")]
    public bool debugDraw = true;

    private PositionMedianFilter _filter;

    void Awake()
    {
        _filter = new PositionMedianFilter(historySize);
    }

    void Update()
    {
        // 1) Record this frame’s local position
        _filter.AddPosition(transform.localPosition);

        // 2) Compute median-based direction
        Vector3 desiredDir = _filter.GetMedianDirection();
        if (desiredDir == Vector3.zero)
            return; // not enough data yet

        // 3) Rotate smoothly toward it
        Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );

        // 4) Debug visualization
        if (debugDraw)
        {
            var positions = _filter.Positions;
            Transform parent = transform.parent;
            // Draw the raw path in blue
            for (int i = 1; i < positions.Count; i++)
            {
                Vector3 worldA = parent != null
                    ? parent.TransformPoint(positions[i - 1])
                    : positions[i - 1];
                Vector3 worldB = parent != null
                    ? parent.TransformPoint(positions[i])
                    : positions[i];
                Debug.DrawLine(worldA, worldB, Color.blue);
            }

            // Draw the median point in green, connected to the start sample
            Vector3 medianLocal = _filter.GetMedianPoint();
            Vector3 startLocal = positions.First();
            Vector3 worldStart = parent != null
                ? parent.TransformPoint(startLocal)
                : startLocal;
            Vector3 worldMedian = parent != null
                ? parent.TransformPoint(medianLocal)
                : medianLocal;
            Debug.DrawLine(worldStart, worldMedian, Color.green);

            // Draw the chosen direction from the object in red
            Debug.DrawLine(transform.position,
                           transform.position + desiredDir,
                           Color.red);
        }
    }
}
