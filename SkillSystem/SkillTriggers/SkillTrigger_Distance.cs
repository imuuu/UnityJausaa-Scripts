namespace Game.SkillSystem
{
    using Game.Utility;
    using UnityEngine;

    public class SkillTrigger_Distance : SkillTriggerBehavior
    {
        [SerializeField] private float _distanceThreshold = 5f;
        [SerializeField] private float _checkInterval = 0.5f;

        private Transform _playerTransform;
        private SimpleTimer _timer;

        private void Start()
        {
            _timer = new SimpleTimer(_checkInterval);
            Player.AssignTransformWhenAvailable((transform) => _playerTransform = transform);
        }

        private void Update()
        {
            if (SkillController == null)
            {
                return;
            }

            _timer.UpdateTimer();

            if (!_timer.IsRoundCompleted) return;

            if (IsPlayerWithinDistance())
            {
                UseSkill();
            }
        }

        private bool IsPlayerWithinDistance()
        {
            if (_playerTransform == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return distance <= _distanceThreshold;
        }
    }
}