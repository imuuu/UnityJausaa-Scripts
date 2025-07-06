using Game.Utility;
using Pathfinding;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.Pathfind
{
    public class PathfindSwitchController : SerializedMonoBehaviour, IPathfindSwitch
    {
        [Title("References")]
        [OdinSerialize] private VersionedMonoBehaviour _pathFindScript;
        [OdinSerialize] private IMovement _movementScript;
        [SerializeField] private EntityStatistics _entityStatistics;

        private bool _pathfindON = false;

        // [Title("Settings")]
        // [SerializeField, Range(15, 100)] private float _switchPlayerDistance = 35f;
        // [SerializeField, Range(15, 100)] private float _switchObstacleDistance = 10f;
        // [SerializeField, Range(0, 10)] private float _switchTimeCheck = 1f;

        // private bool _isPathFindingPresent = false;
        // private SimpleTimer _timer;
        // private Transform _playerTransform;

        //[SerializeField] private LayerMask _layerRaycast;

        private void Awake()
        {
            _pathfindON = true;
            SwitchPathFindingOn(false);
        }
        // private void Start()
        // {
        //     if (AstarPath.active != null) _isPathFindingPresent = true;

        //     _timer = new SimpleTimer(_switchTimeCheck);


        //     ActionScheduler.RunWhenTrue(() => Player.Instance != null, () =>
        //     {
        //         _playerTransform = Player.Instance.transform;

        //         if(!_isPathFindingPresent)
        //             SwitchPathFindingOn(false);
        //     });
        // }

        private void OnEnable()
        {
            ActionScheduler.RunNextFrame( () => 
            {
                ManagerPathfindSwitch.Instance.RegisterSwitch(this);
            });
            
        }

        private void OnDisable()
        {
            ManagerPathfindSwitch.Instance.UnregisterSwitch(this);
        }

        // private void Update()
        // {
        //     if (!_isPathFindingPresent) return;

        //     if (_playerTransform == null) return;

        //     _timer.UpdateTimer();

        //     if (_timer.IsRoundCompleted)
        //     {
        //         SwitchPathFindingOn(IsPlayerWithInDistance() && IsSomethingBetweenPlayerAndTarget());
        //     }
        // }

        // private bool IsPlayerWithInDistance()
        // {
        //     return Vector3.Distance(_playerTransform.position, transform.position) < _switchPlayerDistance;
        // }

        // private bool IsSomethingBetweenPlayerAndTarget()
        // {
        //     //MIDDLE RAYCAST
        //     RaycastHit hit;
        //     //Debug.DrawRay(_playerTransform.position, GetDirectionToPlayer(transform.position), Color.red, 1);
        //     if (Physics.Raycast(_playerTransform.position, GetDirectionToPlayer(transform.position), out hit, _switchPlayerDistance, layerMask: _layerRaycast))
        //     {
        //         return IsObjectInDistance(hit.point);
        //     }
        //     //RIGHT RAYCAST

        //     //Debug.DrawRay(_playerTransform.position, GetDirectionToPlayer(transform.position) + (Vector3.right * _entityStatistics.Width * 0.6f), Color.red, 1);
        //     if(Physics.Raycast(_playerTransform.position, GetDirectionToPlayer(transform.position) + (Vector3.right * _entityStatistics.Width * 0.5f), out _, _switchPlayerDistance, layerMask:_layerRaycast))
        //     {
        //         return IsObjectInDistance(hit.point);
        //     }

        //     //LEFT RAYCAST
        //     //Debug.DrawRay(_playerTransform.position, GetDirectionToPlayer(transform.position) + (Vector3.left * _entityStatistics.Width * 0.5f), Color.red, 1);
        //     if (Physics.Raycast(_playerTransform.position, GetDirectionToPlayer(transform.position) + (Vector3.left * _entityStatistics.Width * 0.5f), out hit, _switchPlayerDistance, layerMask:_layerRaycast))
        //     {
        //         return IsObjectInDistance(hit.point);
        //     }
        //     return false;
        // }

        // private Vector3 GetDirectionToPlayer(Vector3 target)
        // {
        //     return target - _playerTransform.position + Vector3.up;
        // }

        public void SwitchPathFindingOn(bool isPathFinding)
        {
            if(_pathfindON == isPathFinding) return;

            _pathfindON = isPathFinding;
            if (isPathFinding && _pathFindScript != null)
            {
                _pathFindScript.enabled = true;
                _movementScript.GetMonoBehaviour().enabled = false;
            }
            else if (_movementScript != null)
            {
                _pathFindScript.enabled = false;
                _movementScript.GetMonoBehaviour().enabled = true;
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public float GetWidth()
        {
            return _entityStatistics.Width;
        }


        // private bool IsObjectInDistance(Vector3 target)
        // {
        //     return Vector3.Distance(transform.position, target) < _switchObstacleDistance;
        // }

    }
}