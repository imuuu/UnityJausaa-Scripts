using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TiltDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _updateInterval = 0.1f;
    [SerializeField] private bool _showDebug = true;

    [Header("Tilt Values")]
    [ReadOnly] public float totalTilt;
    [ReadOnly] public float xTilt;
    [ReadOnly] public float zTilt;
    [ReadOnly] public Vector3 tiltAxis;

    private Vector3 _worldUp = Vector3.up;
    private Quaternion _lastRotation;
    private float _lastUpdateTime;

    private void Start()
    {
        _lastRotation = transform.rotation;
        UpdateTiltValues(true);
    }

    private void Update()
    {
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            UpdateTiltValues();
            _lastUpdateTime = Time.time;
        }
    }

    private void UpdateTiltValues(bool forceUpdate = false)
    {
        if (!forceUpdate && _lastRotation == transform.rotation) return;

        // Calculate total tilt using vector angle
        totalTilt = Vector3.Angle(_worldUp, transform.up);

        // Calculate rotation difference
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // Project axis onto XZ plane for pure tilt detection
        tiltAxis = Vector3.ProjectOnPlane(axis, _worldUp).normalized;

        // Calculate individual tilt components
        Vector3 rotatedUp = transform.up;
        xTilt = Mathf.Asin(rotatedUp.z) * Mathf.Rad2Deg;
        zTilt = Mathf.Asin(rotatedUp.x) * Mathf.Rad2Deg;

        _lastRotation = transform.rotation;
    }

    private void OnDrawGizmos()
    {
        if (!_showDebug || !Application.isPlaying) return;

        // Draw world up vector
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + _worldUp * 2);

        // Draw current up vector
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2);

        // Draw tilt axis
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + tiltAxis * 1.5f);
    }
}