using Game;
using UnityEngine;

[DefaultExecutionOrder(1)]
public abstract class MobMovement : MonoBehaviour, IMovement, IEnabled
{
    protected bool _isEnabled = true;
    protected ManagerMobMovement ManagerMobMovement => ManagerMobMovement.Instance;
    private bool _movementEnabled = true;
    [SerializeField] protected float _rotationSpeed = 5f; // Default rotation speed
    [SerializeField] protected float _speed = 5f; // Default speed
    protected virtual void Start() 
    {
        Register();
    }

    protected virtual void OnEnable()
    {
        if (ManagerMobMovement != null)
        {
            ManagerMobMovement.RegisterMob(this);
        }
    }

    protected virtual void OnDisable()
    {
        if (ManagerMobMovement != null)
        {
            ManagerMobMovement.UnregisterMob(this);
        }
    }

    private void Register()
    {
        if (ManagerMobMovement != null)
        {
            ManagerMobMovement.RegisterMob(this);
        }
    }

    public abstract void UpdateMovement(float deltaTime);

    public bool IsMovementEnabled()
    {
        return _movementEnabled;
    }

    public void EnableMovement(bool enable)
    {
        _movementEnabled = enable;
    }

    public bool IsEnabled()
    {
        return _isEnabled;
    }

    public void SetEnable(bool enable)
    {
        _isEnabled = enable;
    }

    public MonoBehaviour GetMonoBehaviour()
    {
        return this;
    }

    public float GetSpeed()
    {
        return _speed;
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    public void SetRotationSpeed(float rotationSpeed)
    {
        _rotationSpeed = rotationSpeed;
    }
}