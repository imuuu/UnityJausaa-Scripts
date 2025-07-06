using System;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class DamageDealer : MonoBehaviour, IDamageDealer
{
    [SerializeField] private float _damage = 0;
    private float _criticalMultiplier = -1f; // -1 means no critical multiplier set, 0 means no critical hit
    [SerializeField] private DAMAGE_TYPE _damageType;
    [Tooltip("The source of the damage, e.g. player, enemy, environment")]
    [SerializeField] private DAMAGE_SOURCE _damageSource;

    private float _temporaryDamage = -1f;

    private IOwner _owner;
    private bool _alwaysGetOwner = false;

    private void Awake()
    {
        if (_damageSource == DAMAGE_SOURCE.GET_OWNER)
        {
            _owner = GetComponent<IOwner>();
        }

        if(_damageSource == DAMAGE_SOURCE.GET_OWNER)
        {
            _alwaysGetOwner = true;
        }
    }

    private void OnEnable()
    {
        if (_alwaysGetOwner)
        {
            ActionScheduler.RunNextFrame(() =>
            {
                _damageSource =
                ConvertOwnerTypeToDamageSource(_owner.GetRootOwner().GetOwnerType());
            });
            // _damageSource =
            //     ConvertOwnerTypeToDamageSource(_owner.GetRootOwner().GetOwnerType());
        }
    }

    public float GetDamage()
    {
        if (_temporaryDamage >= 0f)
        {
            return _temporaryDamage * (_criticalMultiplier > 0f ? _criticalMultiplier * 0.01f : 1f);
        }

        return _damage * (_criticalMultiplier > 0f ? _criticalMultiplier * 0.01f : 1f);
    }

    public DAMAGE_SOURCE GetDamageSource()
    {
        return _damageSource;
    }

    public DAMAGE_TYPE GetDamageType()
    {
        return _damageType;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    private DAMAGE_SOURCE ConvertOwnerTypeToDamageSource(OWNER_TYPE ownerType)
    {
        switch (ownerType)
        {
            case OWNER_TYPE.PLAYER:
                return DAMAGE_SOURCE.PLAYER;
            case OWNER_TYPE.ENEMY:
                return DAMAGE_SOURCE.ENEMY;
            case OWNER_TYPE.PHYSIC_HAND:
                return DAMAGE_SOURCE.PHYSIC_HAND;
            default:
                return DAMAGE_SOURCE.ENVIRONMENT;
        }
    }

    public void SetDamage(float damage)
    {
        _damage = damage;
    }

    public void SetTemporaryDamage(float damage)
    {
        _temporaryDamage = damage;
    }

    public void SetDamageSource(DAMAGE_SOURCE damageSource)
    {
        _damageSource = damageSource;
    }

    public SimpleDamage AsSimpleDamage()
    {
        return new SimpleDamage(GetDamage(), GetDamageType(), GetDamageSource());
    }

    public void SetCriticalMultiplier(float multiplier)
    {
        _criticalMultiplier = multiplier;
    }
}