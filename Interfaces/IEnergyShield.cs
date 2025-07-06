using System;
using UnityEngine;

public interface IEnergyShield
{
    public Transform GetTransform();

    // Current and maximum shield values:
    public float GetShield();
    public float GetMaxShield();

    public void SetShield(float amount);
    public void SetMaxShield(float amount);

    public void SetRechargeDelay(float delay);
    public void SetRechargeRate(float rate);
    public void AddShield(float amount);

    public void TakeDamage(IDamageDealer dealer);

    // Force the shield to begin recharging immediately
    // (useful for testing or atypical scenarios):
    public void StartRecharge();

    // Events (mirroring Health’s pattern):
    // - OnShieldChanged: any time _currentShield changes (damage or recharge)
    // - OnShieldHit: when damage is applied to the shield
    // - OnRechargeStarted: when the delay elapses and actual recharge begins
    public Action OnShieldChanged { get; set; }
    public Action OnShieldHit { get; set; }
    public Action OnRechargeStarted { get; set; }
}
