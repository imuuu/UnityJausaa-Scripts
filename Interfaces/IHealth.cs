using System;
using UnityEngine;
public interface IHealth
{
    public Transform GetTransform();
    public float GetHealth();
    public float GetMaxHealth();
    public void TakeDamage(IDamageDealer dealer);
    public void AddHealth(float amount);
    public void SetHealth(float amount);
    public void SetMaxHealth(float amount);
    public void Die();
    public Action OnHealthChanged { get; set; }
    public Action OnDeath { get; set; }

}