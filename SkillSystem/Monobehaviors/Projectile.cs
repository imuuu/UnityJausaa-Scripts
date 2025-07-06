using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.SkillSystem
{
    /// <summary>
    /// Basic projectile with configurable speed behavior and optional target tracking.
    /// </summary>
    public class Projectile : MonoBehaviour, IProjectile, IEnabled
    {
        private bool _isEnabled = true;
        [SerializeField, ReadOnly] private float _speed;
        [SerializeField, ReadOnly] private float _maxSpeed;
        [SerializeField, ReadOnly] private SPEED_TYPE _speedType = SPEED_TYPE.FIXED;
        [SerializeField, Tooltip("Time (seconds) to fully accelerate/decelerate when no target is set.")]
        private float _speedDuration = 1f;

        private Vector3 _direction;
        [SerializeField,ReadOnly] private Transform _targetTransform;
        private Vector3 _targetPosition;
        private bool _hasTarget;

        private Vector3 _startPosition;
        private float _totalDistance;
        private float _elapsedTime;
        private Vector3 _lastKnownTargetPosition;

        private bool _targetReached = false;

        private System.Action _onTargetReached;
        public System.Action OnTargetReached
        {
            get => _onTargetReached;
            set => _onTargetReached = value;
        }

        [SerializeField] private bool _debugRoute = false;

        /// <summary>Sets the forward direction of the projectile.</summary>
        public void SetDirection(Vector3 direction)
        {
            _direction = direction.normalized;
        }

        /// <summary>Sets the base speed used by most speed types.</summary>
        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        /// <summary>Sets the maximum speed for accelerating and random modes.</summary>
        public void SetMaxSpeed(float speed)
        {
            _maxSpeed = speed;
        }

        /// <summary>Sets a target transform. Projectile will compute progress based on this target.</summary>
        public void SetTarget(Transform target)
        {
            _targetTransform = target;
            _targetPosition = target.position;
            _lastKnownTargetPosition = target.position;
            InitializeTargetTracking();
        }

        /// <summary>Sets a target position. Projectile will compute progress based on this point.</summary>
        public void SetTarget(Vector3 targetPosition)
        {
            _targetTransform = null;
            _targetPosition = targetPosition;
            InitializeTargetTracking();
        }

        /// <summary>Sets how speed changes over the lifetime or distance.</summary>
        public void SetSpeedType(SPEED_TYPE speedType, float accelerateDuration = 1f)
        {
            _speedDuration = accelerateDuration;
            _speedType = speedType;
        }

        private void InitializeTargetTracking()
        {
            _hasTarget = true;
            _startPosition = transform.position;
            _totalDistance = Vector3.Distance(_startPosition, _targetPosition);
            _elapsedTime = 0f;
        }

        private void OnEnable()
        {
            // Reset progress on every activation
            _elapsedTime = 0f;
            _targetReached = false;

            if (_hasTarget)
            {
                _startPosition = transform.position;
                _totalDistance = Vector3.Distance(_startPosition, _targetPosition);
            }
        }

        private void Update()
        {
            if (_debugRoute)
            {
                // Debug.DrawLine(_startPosition, _targetPosition, Color.cyan);
                // Debug.DrawLine(transform.position, transform.position + direction * 0.5f, Color.green);

                MarkHelper.DrawSphereTimed(transform.position, 0.1f, 2f, Color.red);
            }

            if (!_isEnabled) return;

            if (ManagerPause.IsPaused()) return;

            _elapsedTime += Time.deltaTime;

            Vector3 direction = _direction;

            if (_hasTarget && _targetTransform != null)
            {
                _targetPosition = _targetTransform.position;
                direction = (_targetPosition - transform.position).normalized;
            }

            float t = (_speedDuration > Mathf.Epsilon)
                ? Mathf.Clamp01(_elapsedTime / _speedDuration)
                : 1f;

            float currentSpeed;
            switch (_speedType)
            {
                case SPEED_TYPE.FIXED:
                    currentSpeed = _speed;
                    break;
                case SPEED_TYPE.ACCELERATE:
                    currentSpeed = Mathf.Lerp(_speed, GetMaxSpeed(), t);
                    break;
                case SPEED_TYPE.DECELERATE:
                    currentSpeed = Mathf.Lerp(_speed, 0f, t);
                    break;
                case SPEED_TYPE.ACCELERATE_DECELERATE:
                    if (t < 0.5f)
                        currentSpeed = Mathf.Lerp(_speed, GetMaxSpeed(), t * 2f);
                    else
                        currentSpeed = Mathf.Lerp(GetMaxSpeed(), 0f, (t - 0.5f) * 2f);
                    break;
                case SPEED_TYPE.DECELERATE_ACCELERATE:
                    if (t < 0.5f)
                        currentSpeed = Mathf.Lerp(_speed, 0f, t * 2f);
                    else
                        currentSpeed = Mathf.Lerp(0f, GetMaxSpeed(), (t - 0.5f) * 2f);
                    break;
                case SPEED_TYPE.RANDOM:
                    currentSpeed = Random.Range(_speed, GetMaxSpeed());
                    break;
                default:
                    currentSpeed = _speed;
                    break;
            }

            // Move and orient
            transform.position += direction * currentSpeed * Time.deltaTime;

            if (_hasTarget)
            {
                transform.LookAt(_targetPosition);
            }
            else
            {
                RotateTowardsDirection();
            }

            if (!_targetReached && _hasTarget && Vector3.Distance(transform.position, _targetPosition) <= 1f)
            {
                _onTargetReached?.Invoke();
                //_onTargetReached = null;
                _targetReached = true;
            }
        }

        private float GetMaxSpeed()
        {
            return _maxSpeed > 0f ? _maxSpeed : _speed;
        }

        private void RotateTowardsDirection()
        {
            if (_direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(_direction);
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        /// <summary>
        /// Sets the projectile to be enabled or disabled. Effects only the Update method => moving.
        /// </summary>
        public void SetEnable(bool enable)
        {
            _isEnabled = enable;
        }

        public Transform GetTransform()
        {
            return transform;
        }
    }
}
