using UnityEngine;

public class FollowerController : MonoBehaviour
{
    private bool _findPlayer = false;
    [Header("References")]
    public Transform _player;
    public Transform _target;

    [Header("Settings")]
    public float _distanceFromTarget = 5f;

    public void LateUpdate()
    {
        if(!_findPlayer)
        {
            _player = ManagerGame.Instance.GetPlayer().transform;
        }

        if (_player == null || _target == null)
            return;

        Vector3 direction = (_target.position - _player.position).normalized;

        Vector3 followerPosition = _target.position - direction * _distanceFromTarget;
        transform.position = followerPosition;

        transform.LookAt(_target);
    }
}
