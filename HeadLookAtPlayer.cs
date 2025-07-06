using UnityEngine;

public class HeadLookAtPlayer : MonoBehaviour
{
    [SerializeField] private float _maxDistance = 10f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _maxYaw = 45f;

    private Transform _player;
    private Quaternion _initialLocalRotation;

    private void Start()
    {
        _initialLocalRotation = transform.localRotation;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("HeadLookAtPlayer:Couldn't find the Player with tag 'Player'.");
        }
    }

    private void LateUpdate()
    {
        if (_player == null) return;

        Vector3 targetPos = _player.position;
        Vector3 lookFrom = transform.position;

        float distance = Vector3.Distance(targetPos, lookFrom);
        if (distance > _maxDistance)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _initialLocalRotation, Time.deltaTime * _rotationSpeed);
            return;
        }

        Vector3 flatTargetDirWorld = targetPos - lookFrom;
        flatTargetDirWorld.y = 0;

        if (flatTargetDirWorld.sqrMagnitude < 0.001f) return;
       
        Vector3 localDirection = transform.parent.InverseTransformDirection(flatTargetDirWorld.normalized);
        
        Quaternion targetRotation = Quaternion.LookRotation(localDirection, Vector3.up);
       
        float targetYaw = Mathf.DeltaAngle(0, targetRotation.eulerAngles.y);
        float clampedYaw = Mathf.Clamp(targetYaw, -_maxYaw, _maxYaw);

        Quaternion clampedRotation = Quaternion.Euler(0, clampedYaw, 0);
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, clampedRotation, Time.deltaTime * _rotationSpeed);
    }
}
