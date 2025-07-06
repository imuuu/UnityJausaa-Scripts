using Game.HitDetectorSystem;
using Game.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

public class DealDamageOnDistance : HitDetector
{
    [SerializeField] private bool _usePlayerTarget = true;
    [SerializeField, HideIf(nameof(_usePlayerTarget))] private Transform _target;

    [SerializeField] private bool _useStatisticsAsDistance = true;
    [SerializeField, ShowIf(nameof(_useStatisticsAsDistance))] private float _distanceMultiplier = 1.1f;

    [SerializeField, HideIf(nameof(_useStatisticsAsDistance))] private float _distance = 5f;

    //[SerializeField] private float _damageInterval = 0.5f;

    //private SimpleTimer _timer;
    private IStatistics _statistics;
    private IStatistics _playerStatistics;
    // private IDamageDealer _damageDealer;
    // private IOwner _owner;

    // private float _hitHistoryTimer = 0f;

    private float _sumDistance;

    // public bool IsManual => false;

    // public int MaxPiercing => 99999;

    // public int RemainingPiercing => 9999;


    protected override void Awake()
    {
        base.Awake();
        _statistics = GetComponent<IStatistics>();
        //_damageDealer = GetComponent<IDamageDealer>();
        _sumDistance = _useStatisticsAsDistance ?
            _statistics.Width * 0.5f * _distanceMultiplier :
            _distance;
        //_owner = GetComponent<IOwner>().GetRootOwner();
    }

    private void Start()
    {
        //_timer = new SimpleTimer(_damageInterval);
        if (!_usePlayerTarget) return;

        Player.AssignTransformWhenAvailable((player) =>
       {
           _target = player.transform;
           _playerStatistics = player.GetComponent<IStatistics>();
           _sumDistance = (_playerStatistics.Width * 0.5f + _playerStatistics.Width * 0.5f) * _distanceMultiplier;
       });
    }

    

    // private void Update()
    // {
    //     if (_target == null) return;

    //     _timer.UpdateTimer(Time.deltaTime);

    //     if (!_timer.IsRoundCompleted)
    //         return;

    //     float currentDistance = Vector3.Distance(_target.transform.position, this.transform.position);

    //     bool distanceIsValid = _useStatisticsAsDistance ?
    //         (currentDistance < _sumDistance) :
    //         (currentDistance < _sumDistance);


    //     if (distanceIsValid)
    //     {
    //         IDamageReceiver damageReceiver = _target.GetComponent<IDamageReceiver>();

    //         if (_damageDealer != null && damageReceiver != null)
    //         {
    //             DamageCalculator.CalculateDamage(_damageDealer, damageReceiver);
    //         }
    //     }
    // }

    // public bool DecrementPiercing()
    // {
    //     return false;
    // }

    // public GameObject GetGameObject()
    // {
    //     return gameObject;
    // }

    // public float GetHitHistoryTimer()
    // {
    //     return _hitHistoryTimer;
    // }

    // public IOwner GetOwner()
    // {
    //     return _owner;
    // }

    // public bool IsManualDestroy()
    // {
    //     return true;
    // }

    // public void OnHit(HitCollisionInfo hitInfo)
    // {
        
    // }

    // public void OnPierceHit(HitCollisionInfo hitInfo)
    // {
        
    // }

    public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
    {
        hitInfo = null;
        float currentDistance = Vector3.Distance(_target.transform.position, this.transform.position);

        bool distanceIsValid = _useStatisticsAsDistance ?
            (currentDistance < _sumDistance) :
            (currentDistance < _sumDistance);


        if (distanceIsValid)
        {
            // IDamageReceiver damageReceiver = _target.GetComponent<IDamageReceiver>();

            // if (_damageDealer != null && damageReceiver != null)
            // {
            //     DamageCalculator.CalculateDamage(_damageDealer, damageReceiver);
            // }

            hitInfo = new HitCollisionInfo
            {
                HasCollisionPoint = true,
                CollisionPoint = _target.position,
                HitObject = _target.gameObject,
                HitLayer = LayerMask.GetMask("Default") // Assuming Default layer, change as needed
            };
        }

        return distanceIsValid;
    }

    // public void SetManualDestroy(bool value)
    // {
    //     throw new System.NotImplementedException();
    // }

    // public void SetRayFireTriggerEnable(bool enable)
    // {
    //     throw new System.NotImplementedException();
    // }

    // public void TriggerManualHitCheck()
    // {
    //     throw new System.NotImplementedException();
    // }


}