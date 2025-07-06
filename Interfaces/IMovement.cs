using UnityEngine;

public interface IMovement
{
    public MonoBehaviour GetMonoBehaviour();
    public bool IsMovementEnabled();
    public void EnableMovement(bool enable);
    public void UpdateMovement(float deltaTime);
    public float GetSpeed();
    public void SetSpeed(float speed);
    public void SetRotationSpeed(float rotationSpeed);
}