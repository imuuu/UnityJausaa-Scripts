using Game;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(1)]
public abstract class MobMovement : MonoBehaviour, IMovement, IEnabled,ITarget
{
    protected bool _isEnabled = true;
    protected ManagerMobMovement ManagerMobMovement => ManagerMobMovement.Instance;
    private bool _movementEnabled = true;
    [SerializeField] protected float _rotationSpeed = 5f; // Default rotation speed
    [SerializeField] protected float _speed = 5f; // Default speed
    [Title("Target")]
    [SerializeField] protected bool _findPlayer = true;
    [SerializeField, HideIf("_findPlayer")] protected Transform _target;
    protected virtual void Start()
    {
        if (!_findPlayer) return;

        Player.AssignTransformWhenAvailable( (player) => SetTarget(player.transform));

        // ActionScheduler.RunWhenTrue(IsPlayerPresent, () =>
        // {
        //     SetTarget(ManagerGame.Instance.GetPlayer().transform);
        // });

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

    public Transform GetTarget()
    {
        return _target;
    }

    public virtual void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }

}