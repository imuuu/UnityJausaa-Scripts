using UnityEngine;

public interface IViewPortTrigger
{
    public void OnEnter();
    public void OnExit();
    public float GetOffset();
    public Vector3 GetPosition();

    public bool IsInside();

    public void SetInside(bool value);
}