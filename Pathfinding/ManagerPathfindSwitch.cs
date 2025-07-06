using System.Collections.Generic;
using Game.Utility;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Manager that centralizes all pathfinding switches.
/// Demonstrates scheduling a job in Update and completing it in LateUpdate,
/// with cleanup in OnDisable/OnDestroy to avoid temp job allocation warnings.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ManagerPathfindSwitch : MonoBehaviour
{
    [SerializeField] private bool _isEnabled = true;
    public static ManagerPathfindSwitch Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _switchTimeCheck = 0.5f;
    [SerializeField, Range(15, 100)] private float _switchPlayerDistance = 35f;
    [SerializeField, Range(5, 100)] private float _switchObstacleDistance = 10f;
    [SerializeField] private LayerMask _layerRaycast;
    // [Title("Options")]
    // [SerializeField] private bool _enableRaycasts = true;
    // [SerializeField,ShowIf("_enableRaycasts")] private bool _enableThreeRaycasts = true;

    private List<IPathfindSwitch> _switchControllers = new();
    private List<IPathfindSwitch> _toRemove = new();
    private SimpleTimer _timer;
    private Transform _playerTransform;

    // Job-related fields
    private bool _jobScheduled = false;
    private JobHandle _distanceHandle;
    private NativeArray<float3> _switchPositions;
    private NativeArray<bool> _distanceResults;
    private int _jobCount = 0;
    private const int BATCH_SIZE = 100;

    private float _yOffset = 0.2f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _timer = new SimpleTimer(_switchTimeCheck);

        if (Player.Instance != null)
            _playerTransform = Player.Instance.transform;
    }

    private void Start()
    {
        // In case the player was null in Awake, try again here
        if (_playerTransform == null && Player.Instance != null)
            _playerTransform = Player.Instance.transform;
    }

    public void RegisterSwitch(IPathfindSwitch switchController)
    {
        if (switchController == null || _switchControllers.Contains(switchController))
            return;

        if(_toRemove.Contains(switchController))
            _toRemove.Remove(switchController);

        _switchControllers.Add(switchController);
        switchController.SwitchPathFindingOn(false);
    }

    public void UnregisterSwitch(IPathfindSwitch switchController)
    {
        _toRemove.Add(switchController);
        //_switchControllers.Remove(switchController);
    }

    private void Update()
    {
        if (!_isEnabled || _playerTransform == null || _switchControllers.Count == 0)
            return;

        // If you only ever want one job per frame, you can optionally skip scheduling
        // if _jobScheduled is still true. (Not usually needed if you complete in LateUpdate.)
        if (_jobScheduled)
            return;

        _timer.UpdateTimer();
        if (!_timer.IsRoundCompleted)
            return;

        ScheduleDistanceJob();
    }
    

    /// <summary>
    /// In LateUpdate, we complete the job, read results, run any raycasts, and switch pathfinding on/off.
    /// </summary>
    private void LateUpdate()
    {
        if (!_jobScheduled)
            return;

        _distanceHandle.Complete();

        for (int i = 0; i < _jobCount; i++)
        {
            bool withinDistance = _distanceResults[i];
            if (_switchControllers[i] == null) continue;

            IPathfindSwitch switchController = _switchControllers[i];
            bool finalState = false;

            if (withinDistance)
            {
                finalState = IsSomethingBetweenPlayerAndSwitch(switchController);
            }

            switchController.SwitchPathFindingOn(finalState);
        }

        foreach (IPathfindSwitch switchController in _toRemove)
        {
            _switchControllers.Remove(switchController);
        }
        _toRemove.Clear();

        _switchPositions.Dispose();
        _distanceResults.Dispose();
        _jobScheduled = false;

    }
    

    /// <summary>
    /// Schedules a Burst–compiled job that checks which switches are within distance of the player.
    /// </summary>
    private void ScheduleDistanceJob()
    {
        _jobCount = _switchControllers.Count;

        _switchPositions = new NativeArray<float3>(_jobCount, Allocator.TempJob);
        _distanceResults = new NativeArray<bool>(_jobCount, Allocator.TempJob);

        for (int i = 0; i < _jobCount; i++)
        {
            //TODO if transform gone error
            if(_switchControllers[i] == null || _switchControllers[i].GetTransform() == null)
            {
                _distanceResults[i] = false;
                continue;
            }
            _switchPositions[i] = _switchControllers[i].GetTransform().position;
        }

        float3 playerPos = _playerTransform.position;
        float switchDistanceSq = _switchPlayerDistance * _switchPlayerDistance;

        PathfindSwitchJob job = new PathfindSwitchJob
        {
            playerPos = playerPos,
            switchDistanceSq = switchDistanceSq,
            switchPositions = _switchPositions,
            results = _distanceResults
        };

        _distanceHandle = job.Schedule(_jobCount, BATCH_SIZE);

        JobHandle.ScheduleBatchedJobs();

        _jobScheduled = true;
    }

    /// <summary>
    /// Performs three raycasts (center, right-offset, left-offset) from the player
    /// toward the switch. Returns true if an obstacle is detected close enough.
    /// </summary>
    // private bool IsSomethingBetweenPlayerAndSwitch(IPathfindSwitch switchController)
    // {
    //     // if(!_enableRaycasts)
    //     //     return true;

    //     Vector3 switchPos = switchController.GetTransform().position;
    //     Vector3 direction = GetDirectionToSwitch(switchPos);
    //     RaycastHit hit;

    //     // Middle raycast
    //     Debug.DrawRay(_playerTransform.position, direction, Color.red, 3);
    //     if (Physics.Raycast(_playerTransform.position, direction, out hit, _switchPlayerDistance, _layerRaycast))
    //     {
    //         if (IsObjectInDistance(switchPos, hit.point))
    //             return true;
    //     }

    //     // if (!_enableThreeRaycasts)
    //     //     return false;

    //     // Right raycast
    //     Vector3 rightDirection = direction + (Vector3.right * switchController.GetWidth() * 0.5f);
    //     Debug.DrawRay(_playerTransform.position, rightDirection, Color.red, 3);
    //     if (Physics.Raycast(_playerTransform.position, rightDirection, out hit, _switchPlayerDistance, _layerRaycast))
    //     {
    //         if (IsObjectInDistance(switchPos, hit.point))
    //             return true;
    //     }

    //     // Left raycast
    //     Vector3 leftDirection = direction + (Vector3.left * switchController.GetWidth() * 0.5f);
    //     Debug.DrawRay(_playerTransform.position, leftDirection, Color.red, 3);
    //     if (Physics.Raycast(_playerTransform.position, leftDirection, out hit, _switchPlayerDistance, _layerRaycast))
    //     {
    //         if (IsObjectInDistance(switchPos, hit.point))
    //             return true;
    //     }

    //     return false;
    // }

    private bool IsSomethingBetweenPlayerAndSwitch(IPathfindSwitch switchController)
    {
        // if(!_enableRaycasts)
        //     return true;

        Vector3 switchPos = switchController.GetTransform().position;

        // if(AstarPath.active.Linecast(_playerTransform.position, switchPos))
        // {
        //     Debug.DrawLine(_playerTransform.position, switchPos, Color.blue, 3);
        //     return true;
        // }

        // if (!_enableThreeRaycasts)
        //     return false;

        // Right raycast
        if(AstarPath.active.Linecast(_playerTransform.position, switchPos+ (Vector3.right * switchController.GetWidth() * 0.5f)))
        {
            //Debug.DrawLine(_playerTransform.position, switchPos + (Vector3.right * switchController.GetWidth() * 0.5f), Color.yellow, 3);
            return true;
        }

        // Left raycast
        if(AstarPath.active.Linecast(_playerTransform.position, switchPos+ (Vector3.left * switchController.GetWidth() * 0.5f)))
        {
            //Debug.DrawLine(_playerTransform.position, switchPos + (Vector3.left * switchController.GetWidth() * 0.5f), Color.green, 3);
            return true;
        }

        return false;
    }

    private Vector3 GetDirectionToSwitch(Vector3 switchPos)
    {
        return switchPos - _playerTransform.position + (Vector3.up * _yOffset).normalized;
    }

    private bool IsObjectInDistance(Vector3 switchPos, Vector3 hitPoint)
    {
        return Vector3.Distance(switchPos, hitPoint) < _switchObstacleDistance;
    }

    /// <summary>
    /// If this object is disabled, ensure we clean up any outstanding job.
    /// </summary>
    private void OnDisable()
    {
        CleanupIfNeeded();
    }

    /// <summary>
    /// If this object is destroyed, ensure we clean up any outstanding job.
    /// </summary>
    private void OnDestroy()
    {
        CleanupIfNeeded();
    }

    private void CleanupIfNeeded()
    {
        if (_jobScheduled)
        {
            _distanceHandle.Complete();
            if (_switchPositions.IsCreated) _switchPositions.Dispose();
            if (_distanceResults.IsCreated) _distanceResults.Dispose();
            _jobScheduled = false;
        }
    }

    

    public void SetEnable(bool enable)
    {
        _isEnabled = enable;
    }
}

