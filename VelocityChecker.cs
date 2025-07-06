
using UnityEngine;

public class VelocityChecker : MonoBehaviour 
{
    [SerializeField] private Rigidbody _rigidbody;

    [SerializeField] private float _velocity = 0;
    [SerializeField] private Vector3 _direction = Vector3.zero;
    private static int _count = 0;
    private void Awake()
    {
        if(_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if(_rigidbody == null)
            _rigidbody = GetComponentInChildren<Rigidbody>();

        _count++;

        Debug.Log($"VelocityChecker count: {_count}");
    }

    private void Update()
    {
        _velocity = _rigidbody.linearVelocity.magnitude;
        _direction = _rigidbody.linearVelocity.normalized;

        _rigidbody.linearVelocity = Vector3.zero;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + _direction * _velocity * 3f);
    }

}