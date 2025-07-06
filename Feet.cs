using UnityEngine;

public class Feet : MonoBehaviour, IFeet
{
    [Header("References")]
    [SerializeField] private Transform[] _footTransforms;
    [SerializeField] private Collider _primaryCollider;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Ground Check")]
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private float _groundedVelocityThreshold = 0.1f;

    private float _lastGroundedTime;
    private Bounds _colliderBounds;

    public float GroundCheckRadius { get; private set; }
    public bool _isGrounded = false;
    public Collider PrimaryCollider => _primaryCollider;

    private void Awake()
    {
        InitializeCollider();
    }

    private void InitializeCollider()
    {
        if (_primaryCollider == null)
            _primaryCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        if (_primaryCollider != null)
        {
            UpdateColliderProperties();
        }
    }

    private void UpdateColliderProperties()
    {
        _colliderBounds = _primaryCollider.bounds;
        GroundCheckRadius = CalculateGroundCheckRadius();
    }

    private float CalculateGroundCheckRadius()
    {
        Vector3 scaledExtents = _colliderBounds.extents;
        return Mathf.Max(scaledExtents.x, scaledExtents.z);
    }

    public Vector3 GetFeetPosition()
    {
        if (_footTransforms.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (Transform foot in _footTransforms)
                sum += foot.position;
            return sum / _footTransforms.Length;
        }

        return GetColliderBottomPosition();
    }

    private Vector3 GetColliderBottomPosition()
    {
        UpdateColliderProperties();
        return new Vector3(_colliderBounds.center.x,
                          _colliderBounds.min.y,
                          _colliderBounds.center.z);
    }

    // private void FixedUpdate()
    // {
    //     UpdateColliderProperties();
    //     UpdateGroundStatus();
    // }

    private void UpdateGroundStatus()
    {
        Vector3 origin = GetFeetPosition();
        bool wasGrounded = IsGrounded();

        _isGrounded = CheckGround(origin) &&
                    Mathf.Abs(_rigidbody.linearVelocity.y) <= _groundedVelocityThreshold;

        if (_isGrounded) _lastGroundedTime = Time.time;
    }

    private bool CheckGround(Vector3 origin)
    {
        return Physics.CheckSphere(origin, GroundCheckRadius, _groundLayers);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || _primaryCollider == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 feetPos = GetFeetPosition();
        Gizmos.DrawWireSphere(feetPos, GroundCheckRadius);
        Gizmos.DrawLine(feetPos, feetPos + Vector3.down * _groundCheckDistance);
    }

    public bool IsGrounded()
    {
        return _isGrounded;
    }
}