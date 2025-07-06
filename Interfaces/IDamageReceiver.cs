using System;
using UnityEngine;

public interface IDamageReceiver
{
    public IHealth GetHealth();
    public void TakeDamage(IDamageDealer dealer);
    public DAMAGE_SOURCE[] GetAcceptedDamageSource();
    public Transform GetTransform();

}