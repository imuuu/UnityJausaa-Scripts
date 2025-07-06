using System;
using Game.DropSystem;
using UnityEngine;

public class DropOrb : MonoBehaviour, IDropOrb
{
    public float Value { get; protected set; }
    public Transform Transform => transform;
    public float Amount => Value;
    public event Action<IDropOrb> Collected;

    public virtual void Init(float amount)
    {
        Value = amount;
    }

    public void OnCollected()
    {
        Collected?.Invoke(this);
    }

    public void Clear()
    {
        Collected = null;
        Value = 0f;
    }

}