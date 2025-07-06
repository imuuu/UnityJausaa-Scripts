namespace Game.SkillSystem
{
    using Game.HitDetectorSystem;
    using UnityEngine;
    using Game.Utility;

#if UNITY_EDITOR
    using UnityEditor;  
#endif

    [RequireComponent(typeof(SkillController))]
    public class SkillTrigger_LineOfSight : SkillTriggerBehavior
    {
        [Header("Detection")]
        [SerializeField] private float _distanceThreshold = 5f;
        [SerializeField, Range(0f, 180f)] private float _fieldOfView = 20f;

        [Header("Timing")]
        [SerializeField] private float _checkInterval = 0.5f;

        private SimpleTimer _timer;
        private Transform _playerTransform;
        private float _halfFOV;

        protected override void Awake()
        {
            base.Awake();
            _halfFOV = _fieldOfView * 0.5f;
            _timer = new SimpleTimer(_checkInterval);
        }

        private void OnEnable()
        {
            Player.AssignTransformWhenAvailable(t => _playerTransform = t);
        }

        private void Update()
        {
            _timer.UpdateTimer();

            if (!_timer.IsRoundCompleted) return;

            if (_playerTransform == null)
                return;

            if (IsPlayerInSight())
                UseSkill();
        }

        private bool IsPlayerInSight()
        {
            Vector3 origin = transform.position;
            Vector3 direction = _playerTransform.position - origin;
            float distance = direction.magnitude;

            if (distance > _distanceThreshold)
                return false;

            if (Vector3.Angle(transform.forward, direction.normalized) > _halfFOV)
                return false;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, ManagerHitDectors.GetHitLayerMask()))
            {
                if (hit.transform != _playerTransform)
                    return false;
            }

            return true;
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float halfFOV = _fieldOfView * 0.5f;
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            Vector3 leftBoundary = Quaternion.Euler(0, -halfFOV, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, halfFOV, 0) * forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, leftBoundary * _distanceThreshold);
            Gizmos.DrawRay(origin, rightBoundary * _distanceThreshold);

            Handles.color = new Color(1f, 1f, 0f, 0.1f);
            Handles.DrawSolidArc(
                origin,
                Vector3.up,
                leftBoundary,
                _fieldOfView,
                _distanceThreshold
            );
        }
#endif
    }
}
