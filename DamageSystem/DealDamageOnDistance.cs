using Game.HitDetectorSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class DealDamageOnDistance : HitDetector
{
    [SerializeField] private bool _usePlayerTarget = true;
    [SerializeField, HideIf(nameof(_usePlayerTarget))] private Transform _target;

    [SerializeField] private bool _useStatisticsAsDistance = true;
    [SerializeField, ShowIf(nameof(_useStatisticsAsDistance))] private float _distanceMultiplier = 1.1f;

    [SerializeField, HideIf(nameof(_useStatisticsAsDistance))] private float _distance = 5f;

    private IStatistics _statistics;
    private IStatistics _playerStatistics;

    private float _sumDistance;

    protected override void Awake()
    {
        base.Awake();
        _statistics = GetComponent<IStatistics>();
        //_damageDealer = GetComponent<IDamageDealer>();
        _sumDistance = _useStatisticsAsDistance ?
            _statistics.Width * 0.5f * _distanceMultiplier :
            _distance;
    }

    private void Start()
    {
        if (!_usePlayerTarget) return;

        Player.AssignTransformWhenAvailable((player) =>
       {
           _target = player.transform;
           _playerStatistics = player.GetComponent<IStatistics>();
           _sumDistance = (_playerStatistics.Width * 0.5f + _statistics.Width * 0.5f) * _distanceMultiplier;
       });
    }


    public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
    {
        hitInfo = null;
        float currentDistance = Vector3.Distance(_target.transform.position, this.transform.position);

        bool distanceIsValid = _useStatisticsAsDistance ?
            (currentDistance < _sumDistance) :
            (currentDistance < _sumDistance);


        if (distanceIsValid)
        {
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
}