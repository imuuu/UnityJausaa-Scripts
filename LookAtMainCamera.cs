using UnityEngine;

public class LookAtMainCamera : MonoBehaviour
{
    [SerializeField] private bool _oppositeDirection = true;
    [SerializeField] private bool _lockAxesX = false;
    [SerializeField] private bool _lockAxesY = false;
    [SerializeField] private bool _lockAxesZ = false;
    [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

    private void OnEnable()
    {
        // Register this object with the manager
        ManagerLookMainCamera.Register(this);
    }

    private void OnDisable()
    {
        // Unregister from the manager
        ManagerLookMainCamera.Unregister(this);
    }

    /// <summary>
    /// Called by the manager once per its Update cycle.
    /// </summary>
    public void TriggerLookAt(Transform target)
    {
        if (target == null) return;

        Vector3 lookDirection = target.position - transform.position;

        if (_oppositeDirection)
            lookDirection = -lookDirection;

        if (_lockAxesX) lookDirection.x = 0;
        if (_lockAxesY) lookDirection.y = 0;
        if (_lockAxesZ) lookDirection.z = 0;

        Quaternion rotation = Quaternion.LookRotation(lookDirection);

        // Apply the rotation offset
        transform.rotation = rotation * Quaternion.Euler(_rotationOffset);
    }
}
