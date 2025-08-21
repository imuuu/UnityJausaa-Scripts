using Game.MobStateSystem;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public class ChaseMovement : MobMovement
{
    [Title("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private MobStateMachine _mobStateMachine;
    [Title("Movement")]
    [SerializeField] private bool _overrideSpeedToPathFinding = true;
    [Tooltip("Lock the Y to 0")]
    [SerializeField] private bool _lockY = true;

   

    [Title("Patrol / Wandering")]
    [SerializeField] private bool _enablePatrol = false;
    [ShowIf("_enablePatrol")]
    [SerializeField] private Transform _patrolTarget;
    [ShowIf("_enablePatrol")]
    [SerializeField, Min(0.1f)] private float _patrolRadius = 5f;
    [ShowIf("_enablePatrol")]
    [SerializeField, Min(0.1f)] private float _pointReachedDistance = 1f;
    [SerializeField, Range(0.0f, 1.0f)] private float _patrolSpeedReduced = 0.4f;

    private Vector3 _wanderPoint;
    private Vector3 _lookAtPosition;
    private Vector3 _position;
    private IPathfindSwitch _pathFindSwitch;
    private IHealth _health;

    protected void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_rigidbody == null)
            _rigidbody = GetComponentInChildren<Rigidbody>();

        if (_overrideSpeedToPathFinding)
            GetComponent<IAstarAI>().maxSpeed = _speed;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        InitMobStateMachine(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        InitMobStateMachine(false);
        _rigidbody.linearVelocity = Vector3.zero;
    }

    private void InitMobStateMachine(bool enable)
    {
        if (_mobStateMachine == null) return;

        if (enable)
        {
            _mobStateMachine.AddEnterListener(MOB_STATE.STAND, () => EnableMovement(true));
            _mobStateMachine.AddExitListener(MOB_STATE.MOVING, () => EnableMovement(false));
        }
    }

    protected override void Start()
    {
        base.Start();

        if (!_findPlayer) return;

        ActionScheduler.RunWhenTrue(IsPlayerPresent, () =>
        {
            SetTarget(ManagerGame.Instance.GetPlayer().transform);
        });

        if (_enablePatrol)
        {
            PickNewWanderPoint();
        }
    }


    public override void UpdateMovement(float deltaTime)
    {
        if (_target == null) return;

        if (_mobStateMachine != null && _mobStateMachine.GetState() == MOB_STATE.ON_AIR)
            return;

        if (_enablePatrol)
        {
            Wander(deltaTime);
            return;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        Vector3 direction = (_target.position - transform.position).normalized;
        Vector3 newPosition = transform.position + direction * _speed * deltaTime;

        _rigidbody.MovePosition(newPosition);

        if (_lockY)
        {
            _position = _rigidbody.position;
            _position.y = 0;

            _rigidbody.transform.position = _position;
        }

        _lookAtPosition.x = _target.position.x; _lookAtPosition.y = transform.position.y; _lookAtPosition.z = _target.position.z;

        Quaternion targetRotation = Quaternion.LookRotation(_lookAtPosition - transform.position);

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * deltaTime);
    }

    private void Wander(float deltaTime)
    {
        if (_patrolTarget == null) return;

        Vector3 direction = (_wanderPoint - transform.position).normalized;
        Vector3 newPos = transform.position + direction * _speed * _patrolSpeedReduced * deltaTime;
        _rigidbody.MovePosition(newPos);

        if (_lockY)
            LockYPosition();

        Quaternion targetRot = Quaternion.LookRotation(_wanderPoint - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, _rotationSpeed * deltaTime);

        if (Vector3.Distance(transform.position, _wanderPoint) <= _pointReachedDistance)
            PickNewWanderPoint();
    }

    private void LockYPosition()
    {
        Vector3 pos = _rigidbody.position;
        pos.y = 0f;
        _rigidbody.transform.position = pos;
    }

    private void PickNewWanderPoint()
    {
        Vector2 rand2D = Random.insideUnitCircle * _patrolRadius;
        _wanderPoint = _patrolTarget.position + new Vector3(rand2D.x, 0f, rand2D.y);
    }

    private bool IsPlayerPresent()
    {
        return Player.Instance != null;
    }

    public void SetPatrol(Transform patrolTarget, float patrolRadius = 5f)
    {
        _enablePatrol = true;
        _patrolTarget = patrolTarget;
        _patrolRadius = patrolRadius;

        if (PathFindSwitch != null && PathFindSwitch is MonoBehaviour monoBehaviour)
        {
            monoBehaviour.enabled = false;
        }

        GetHealth().OnHealthChanged += DisablePatrol;

        PickNewWanderPoint();

        //Debug.Log($"Patrol set to target: {_patrolTarget.name} with radius: {_patrolRadius}");
    }

    public void DisablePatrol()
    {
        _enablePatrol = false;
        _patrolTarget = null;

        GetHealth().OnHealthChanged -= DisablePatrol;

        if (PathFindSwitch != null && PathFindSwitch is MonoBehaviour monoBehaviour)
        {
            monoBehaviour.enabled = true;
        }
        Debug.Log("Patrol disabled.");
    }

    private IPathfindSwitch PathFindSwitch
    {
        get
        {
            if (_pathFindSwitch == null)
            {
                _pathFindSwitch = GetComponentInChildren<IPathfindSwitch>();
            }
            return _pathFindSwitch;
        }
    }
    
    public IHealth GetHealth()
    {
        if (_health == null)
        {
            _health = GetComponent<IHealth>();
        }
        return _health;
    }
}