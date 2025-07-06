using System;
using UnityEngine;
namespace Game.DropSystem
{
public interface IDropOrb
{
    public Transform Transform { get; }
    public float Amount { get; }
    public event Action<IDropOrb> Collected;
    public void Init(float amount);
}

}
