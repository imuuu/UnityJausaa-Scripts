using Game.ChunkSystem;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IMovement
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private float _speedSmoothTime = 10f; // smoothing factor
    [SerializeField] private float _rotationSpeed = 720f;   // degrees per second

    [Header("Clamping Settings")]
    [Tooltip("How many chunks from the grid edge to leave as a movement border.")]
    [SerializeField] private int _borderChunks = 1;

    private Vector3 _movementDirection;
    private Vector3 _lastPosition;

    private float _actualSpeed;   // raw speed from Rigidbody
    private float _smoothedSpeed; // smoothed speed for animations

    private bool _movementEnabled = true;
    public bool _isPerformingAction = false;

    public float CurrentSpeed => _smoothedSpeed; // expose smoothed speed

    #region Unity Callbacks

    private void Start()
    {
        _lastPosition = _rigidbody.position;
    }

    private void FixedUpdate()
    {
        // Always run our common movement logic using the fixed-timestep deltaTime
        PerformMovement(Time.fixedDeltaTime);
    }

    #endregion

    #region Core Movement Logic (called both from FixedUpdate and from UpdateMovement)

    private void PerformMovement(float deltaTime)
    {
        // If some action is playing, bail out
        if (_isPerformingAction) return;

        // If paused, zero out velocities/positions
        if (ManagerPause.IsPaused())
        {
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.position = _lastPosition;
            _actualSpeed = 0f;
            _smoothedSpeed = 0f;
            return;
        }

        // If movement is disabled (via interface), freeze in place
        if (!_movementEnabled)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _actualSpeed = 0f;
            _smoothedSpeed = 0f;
            return;
        }

        // Read input axes
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Always zero out any leftover linearVelocity
        _rigidbody.linearVelocity = Vector3.zero;

        _movementDirection = new Vector3(moveX, 0f, moveZ);
        if (_movementDirection.magnitude > 1f)
            _movementDirection.Normalize();

        Vector3 movement = _movementDirection * _moveSpeed * deltaTime;
        Vector3 targetPos = _rigidbody.position + movement;

        // Clamp inside the chunk-bounds inset by _borderChunks
        if (ManagerChunks.Instance != null)
        {
            ManagerChunks.Instance.GetMovementBounds(_borderChunks, out Vector3 min, out Vector3 max);
            targetPos.x = Mathf.Clamp(targetPos.x, min.x, max.x);
            targetPos.z = Mathf.Clamp(targetPos.z, min.z, max.z);
        }

        _rigidbody.MovePosition(targetPos);

        // If there's some movement input, rotate smoothly toward that direction
        if (_movementDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_movementDirection, Vector3.up);
            // Smooth rotation using RotateTowards (degrees/sec)
            Quaternion newRot = Quaternion.RotateTowards(
                _rigidbody.rotation,
                targetRotation,
                _rotationSpeed * deltaTime
            );
            _rigidbody.MoveRotation(newRot);
        }

        // Compute raw speed: distance covered divided by deltaTime
        _actualSpeed = (_rigidbody.position - _lastPosition).magnitude / deltaTime;

        // Smooth the speed for animation blending
        _smoothedSpeed = Mathf.Lerp(
            _smoothedSpeed,
            _actualSpeed,
            deltaTime * _speedSmoothTime
        );

        _lastPosition = _rigidbody.position;
    }

    #endregion

    #region IMovement Implementation

    public MonoBehaviour GetMonoBehaviour()
    {
        return this;
    }

    public bool IsMovementEnabled()
    {
        return _movementEnabled;
    }

    public void EnableMovement(bool enable)
    {
        _movementEnabled = enable;

        if (!enable)
        {
            // If disabling, also zero out velocity so the character instantly stops
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _actualSpeed = 0f;
            _smoothedSpeed = 0f;
        }
    }

    public void UpdateMovement(float deltaTime)
    {
        PerformMovement(deltaTime);
    }

    public float GetSpeed()
    {
        return _smoothedSpeed;
    }

    public void SetSpeed(float speed)
    {
        _moveSpeed = speed;
    }

    public void SetRotationSpeed(float rotationSpeed)
    {
        _rotationSpeed = rotationSpeed;
    }

    #endregion

    #region Public Helpers

    public Vector3 GetMovementDirection()
    {
        return _movementDirection;
    }

    public float GetCurrentSpeed()
    {
        return _smoothedSpeed;
    }

    public void FaceTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - _rigidbody.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            // Again, lerp this rotation rather than snapping
            Quaternion newRot = Quaternion.RotateTowards(
                _rigidbody.rotation,
                targetRotation,
                _rotationSpeed * Time.fixedDeltaTime
            );
            _rigidbody.MoveRotation(newRot);
        }
    }

    #endregion
}
