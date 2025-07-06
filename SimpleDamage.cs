using UnityEngine;

public class SimpleDamage : IDamageDealer
{
    private float _damage = 0;
    private float _criticalMultiplier = -1f; // -1 means no critical multiplier set, 0 means no critical hit
    private DAMAGE_TYPE _damageType;
    private DAMAGE_SOURCE _damageSource;
    private float _temporaryDamage = -1f;

    public SimpleDamage(float damage, DAMAGE_TYPE damageType, DAMAGE_SOURCE damageSource)
    {
        _damage = damage;
        _damageType = damageType;
        _damageSource = damageSource;
    }

    /// <summary>
    /// Creates a new SimpleDamage instance from an existing IDamageDealer.
    /// </summary>
    public SimpleDamage AsSimpleDamage()
    {
        return new SimpleDamage(_damage, _damageType, _damageSource);
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
        Debug.LogWarning("SimpleDamage does not have a transform");
        return null;
    }

    public void SetCriticalMultiplier(float multiplier)
    {
        _criticalMultiplier = multiplier;
    }

    public void SetDamage(float damage)
    {
        _damage = damage;
    }

    public void SetDamageSource(DAMAGE_SOURCE damageSource)
    {
        _damageSource = damageSource;
    }

    public void SetTemporaryDamage(float damage)
    {
        _temporaryDamage = damage;
    }
}