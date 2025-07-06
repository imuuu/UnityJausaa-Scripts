using System.Collections.Generic;
using Game;
using Game.Shapes;
using Game.Utility;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ManagerPlayerViewPort : MonoBehaviour, IEnabled
{
    public static ManagerPlayerViewPort Instance { get; private set; }
    [SerializeField] private bool _isEnabled = true;
    [SerializeField] private float _updateInterval = 0.1f;
    private List<IViewPortTrigger> _viewPortTriggers = new ();

    private PlayerViewPort _playerViewPort;

    private const int BATCH_SIZE = 20;
    private SimpleTimer _timer;

    public bool DebugViewPort = false;
    [SerializeField] private bool _debugShape = false;
 
    public void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);

        Events.OnPlayerSet.AddListenerOnce(OnPlayerSet);
        _timer = new SimpleTimer(_updateInterval);
    }

    private bool OnPlayerSet(Player param)
    {
        _playerViewPort = param.GetComponentInChildren<PlayerViewPort>();
        return true;
    }

    public void RegisterViewPortTrigger(IViewPortTrigger viewPortTrigger)
    {
        if(!_isEnabled) return;

        _viewPortTriggers.Add(viewPortTrigger);
        viewPortTrigger.SetInside(false);
        viewPortTrigger.OnExit();
    }

    public void UnregisterViewPortTrigger(IViewPortTrigger viewPortTrigger)
    {
        _viewPortTriggers.Remove(viewPortTrigger);
    }

    public ShapePolygon GetShape()
    {
        if(_playerViewPort == null) return null;

        ShapePolygon shape = _playerViewPort.GetShape();
        shape.ApplyToTransform(_playerViewPort.transform);
        return shape;
    }
    
    // OLD CODE
    // public void Update()
    // {
    //     if(_playerViewPort == null) return;

    //     Vector3 pos = _playerViewPort.GetPosition();
    //     float2 v0 = new float2(_playerViewPort.Vertex0.x + pos.x, _playerViewPort.Vertex0.z + pos.z);
    //     float2 v1 = new float2(_playerViewPort.Vertex1.x + pos.x, _playerViewPort.Vertex1.z + pos.z);
    //     float2 v2 = new float2(_playerViewPort.Vertex2.x + pos.x, _playerViewPort.Vertex2.z + pos.z);
    //     float2 v3 = new float2(_playerViewPort.Vertex3.x + pos.x, _playerViewPort.Vertex3.z + pos.z);

    //     float2 n0 = math.normalize(new float2((v1 - v0).y, -(v1 - v0).x));
    //     float2 n1 = math.normalize(new float2((v2 - v1).y, -(v2 - v1).x));
    //     float2 n2 = math.normalize(new float2((v3 - v2).y, -(v3 - v2).x));
    //     float2 n3 = math.normalize(new float2((v0 - v3).y, -(v0 - v3).x));

    //     foreach(var trigger in _viewPortTriggers)
    //     {
    //         Debug.Log("Update LOOKING, count = " + _viewPortTriggers.Count);
    //         Vector3 effectivePos = trigger.GetPosition();

    //         if (trigger.GetOffset() != 0)
    //         {
    //             Vector3 direction = _playerViewPort.GetPosition() - trigger.GetPosition();
    //             effectivePos = trigger.GetPosition() + direction.normalized * trigger.GetOffset();
    //         }

    //         float2 point = new float2(effectivePos.x, effectivePos.z);

    //         float d0 = math.dot(point - v0, n0);
    //         float d1 = math.dot(point - v1, n1);
    //         float d2 = math.dot(point - v2, n2);
    //         float d3 = math.dot(point - v3, n3);

    //         float minDistance = math.min(math.min(d0, d1), math.min(d2, d3));

    //         if (minDistance >= 0)
    //         {
    //             if (!trigger.IsInside())
    //             {
    //                 trigger.SetInside(true);
    //                 trigger.OnEnter();
    //             }
    //         }
    //         else if (minDistance < 0)
    //         {
    //             if (trigger.IsInside())
    //             {
    //                 trigger.SetInside(false);
    //                 trigger.OnExit();
    //             }
    //         }
    //     }

    // }

    public void Update()
    {
        if(_isEnabled == false) return;

        if (_playerViewPort == null) return;

        _timer.UpdateTimer();

        if(!_timer.IsRoundCompleted) return;

        Vector3 pos = _playerViewPort.GetPosition();
        float2 v0 = new float2(_playerViewPort.Vertex0.x + pos.x, _playerViewPort.Vertex0.z + pos.z);
        float2 v1 = new float2(_playerViewPort.Vertex1.x + pos.x, _playerViewPort.Vertex1.z + pos.z);
        float2 v2 = new float2(_playerViewPort.Vertex2.x + pos.x, _playerViewPort.Vertex2.z + pos.z);
        float2 v3 = new float2(_playerViewPort.Vertex3.x + pos.x, _playerViewPort.Vertex3.z + pos.z);

        float2 n0 = math.normalize(new float2((v1 - v0).y, -(v1 - v0).x));
        float2 n1 = math.normalize(new float2((v2 - v1).y, -(v2 - v1).x));
        float2 n2 = math.normalize(new float2((v3 - v2).y, -(v3 - v2).x));
        float2 n3 = math.normalize(new float2((v0 - v3).y, -(v0 - v3).x));

        int count = _viewPortTriggers.Count;
        if (count == 0) return;

        NativeArray<float2> triggerPositions = new NativeArray<float2>(count, Allocator.TempJob);
        NativeArray<float> triggerOffsets = new NativeArray<float>(count, Allocator.TempJob);
        NativeArray<bool> results = new NativeArray<bool>(count, Allocator.TempJob);

        for (int i = 0; i < count; i++)
        {
            IViewPortTrigger trigger = _viewPortTriggers[i];
            Vector3 effectivePos = trigger.GetPosition();
            if (trigger.GetOffset() != 0)
            {
                Vector3 direction = _playerViewPort.GetPosition() - trigger.GetPosition();
                effectivePos = trigger.GetPosition() + direction.normalized * trigger.GetOffset();
            }
            triggerPositions[i] = new float2(effectivePos.x, effectivePos.z);
            triggerOffsets[i] = trigger.GetOffset();
        }

        ViewPortJob job = new ViewPortJob
        {
            v0 = v0,
            v1 = v1,
            v2 = v2,
            v3 = v3,
            n0 = n0,
            n1 = n1,
            n2 = n2,
            n3 = n3,
            triggerPositions = triggerPositions,
            triggerOffsets = triggerOffsets,
            results = results
        };

        JobHandle handle = job.Schedule(count, BATCH_SIZE);
        handle.Complete();

        for (int i = 0; i < count; i++)
        {
            IViewPortTrigger trigger = _viewPortTriggers[i];
            bool isInside = results[i];
            if (isInside && !trigger.IsInside())
            {
                trigger.SetInside(true);
                trigger.OnEnter();
            }
            else if (!isInside && trigger.IsInside())
            {
                trigger.SetInside(false);
                trigger.OnExit();
            }
        }

        triggerPositions.Dispose();
        triggerOffsets.Dispose();
        results.Dispose();
    }

    public void OnDrawGizmos()
    {
        if(!_isEnabled) return;

        if(_playerViewPort == null) return;

        if(DebugViewPort)
        {
            foreach (IViewPortTrigger trigger in _viewPortTriggers)
            {
                Vector3 direction = _playerViewPort.GetPosition() - trigger.GetPosition();

                Gizmos.DrawLine(trigger.GetPosition(), trigger.GetPosition() + direction.normalized * trigger.GetOffset());
            }
        }
        
        if(_debugShape)
        {
            GetShape().DrawGizmos(Color.cyan);
        }
       
    }

    public bool IsEnabled()
    {
        return _isEnabled;
    }

    public void SetEnable(bool enable)
    {
        _isEnabled = enable;
    }
}