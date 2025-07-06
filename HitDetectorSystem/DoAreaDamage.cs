using UnityEngine;
using Game.HitDetectorSystem;
using Game.StatSystem;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[RequireComponent(typeof(IOwner))]
public class DoAreaDamage : MonoBehaviour, IStatReceiver
{
    [SerializeField] private bool _overrideAreaRadius = false;
    [SerializeField, ShowIf(nameof(_overrideAreaRadius))] private float _areaRadius = 5f;
    private float _hitHistoryTime = 0f;
    [SerializeField, Required] private DamageDealer _customDamageDealer = null;

    [SerializeField, Required] private HitDetector _mainHitDetector = null;
    [SerializeField] private bool _enableAreaDamageOnHit = false;
    [SerializeField] private bool _enableAreaDamageOnFinalHit = false;

    private StatList _statList;
    [SerializeField][Space(10)] private bool _debug;

    private IOwner _owner;

    private void Awake()
    {
        _owner = GetComponent<IOwner>();
#if UNITY_EDITOR
        if (_owner == null)
        {
            Debug.LogError("DoAreaDamage: IOwner component is missing on the GameObject. Please add it.");
        }
#endif
        _statList = new StatList();
        _statList.Initialize(new List<STAT_TYPE> { STAT_TYPE.AREA_EFFECT, STAT_TYPE.AREA_DAMAGE_MULTIPLIER });
    }

    private void OnEnable()
    {
        if (_enableAreaDamageOnHit)
        {
            _mainHitDetector.OnHitEvent += ExecuteAreaDamage;
        }

        if (_enableAreaDamageOnFinalHit)
        {
            _mainHitDetector.OnFinalHitEvent += ExecuteAreaDamage;
        }
    }

    private void OnDisable()
    {
        if (_enableAreaDamageOnHit)
        {
            _mainHitDetector.OnHitEvent -= ExecuteAreaDamage;
        }

        if (_enableAreaDamageOnFinalHit)
        {
            _mainHitDetector.OnFinalHitEvent -= ExecuteAreaDamage;
        }
    }

    /// <summary>
    /// Call this from anywhere to blast everyone in _areaRadius
    /// around <paramref name="position"/>.  Damage application
    /// is entirely driven by ManagerHitDectors.HandleHit().
    /// </summary>
    public void ExecuteAreaDamage(HitCollisionInfo hitInfo = null)
    {
        float radius = _overrideAreaRadius ? _areaRadius : _statList.GetValueOfStat(STAT_TYPE.AREA_EFFECT);
        float damageMultiplier = _statList.GetValueOfStat(STAT_TYPE.AREA_DAMAGE_MULTIPLIER, 100);

        damageMultiplier *= 0.01f;

        SimpleDamage simpleDamage = _customDamageDealer.AsSimpleDamage();
        simpleDamage.SetDamage(simpleDamage.GetDamage() * damageMultiplier);

        var detector = new HitDetector_AreaDamage(
            attachedObject: this.gameObject,
            owner: _owner,
            radius: radius,
            hitHistoryTime: _hitHistoryTime,
            customDamageDealer: simpleDamage,
            ignoredObject: hitInfo?.HitObject
        );

        detector.SetManualDestroy(true);
        ManagerHitDectors.Instance.CallPerformHitCheck(detector);
    }

    public bool HasStat(STAT_TYPE type)
    {
        return _statList.HasStat(type);
    }

    public void SetStat(Stat stat)
    {
        _statList.SetStat(stat);
    }

    public StatList GetStats()
    {
        return _statList;
    }
    
    private void OnDrawGizmosSelected()
    {
        if(!_debug)
            return;

        if (_overrideAreaRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _areaRadius);
        }
    }

    public void SetStats(StatList statList)
    {
        _statList = statList;
    }
}
