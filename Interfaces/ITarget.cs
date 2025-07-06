using UnityEngine;

public interface ITarget
{
    public Transform GetTarget();
    public void SetTarget(Transform newTarget);
}