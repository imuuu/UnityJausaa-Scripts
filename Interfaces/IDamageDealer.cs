using UnityEngine;

public interface IDamageDealer
{
    public float GetDamage();
    public void SetDamage(float damage);

    /// <summary>
    /// Sets the critical hit multiplier. In format is a percentage, e.g. 200 for 200% damage.
    /// </summary>
    public void SetCriticalMultiplier(float multiplier);

    /// <summary>
    /// Sets a temporary damage value that will override the normal damage
    /// </summary>
    public void SetTemporaryDamage(float damage);

    public void RemoveTemporaryDamage()
    {
        SetTemporaryDamage(-1f);
    }
    public DAMAGE_TYPE GetDamageType();
    public DAMAGE_SOURCE GetDamageSource();
    public void SetDamageSource(DAMAGE_SOURCE damageSource);
    public Transform GetTransform();

    public SimpleDamage AsSimpleDamage();
}