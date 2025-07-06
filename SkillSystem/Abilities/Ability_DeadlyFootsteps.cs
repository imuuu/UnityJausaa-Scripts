using Game.PoolSystem;
using Game.Utility;
using UnityEngine;
namespace Game.SkillSystem
{
    public class Ability_DeadlyFootsteps : Ability, IManualEnd
    {
        [SerializeField] private GameObject _footStepsPrefab;
        private Transform _rightFoot, _leftFoot;

        private float _prevRightLocalZ = 0f;
        private float _prevLeftLocalZ = 0f;

        private bool _wasRightMovingForward = false;
        private bool _wasLeftMovingForward = false;

        private float _r_maxZPosition = float.MinValue;
        private float _r_minZPosition = float.MaxValue;
        private float _l_maxZPosition = float.MinValue;
        private float _l_minZPosition = float.MaxValue;

        private SimpleTimer _timer;
        public override void StartSkill()
        {
            _timer = new SimpleTimer(0.1f, true);
            if (GetOwnerType() == OWNER_TYPE.PLAYER)
            {
                Player player = GetUser().GetComponent<Player>();
                if (player != null)
                {
                    _rightFoot = player.GetRightFoot();
                    _leftFoot = player.GetLeftFoot();
                }
            }

            if (_rightFoot != null)
            {
                float rightZ = GetUserTransform().InverseTransformPoint(_rightFoot.position).z;
                _prevRightLocalZ = rightZ;
                _wasRightMovingForward = false;
                _r_maxZPosition = rightZ;
                _r_minZPosition = rightZ;
            }

            if (_leftFoot != null)
            {
                float leftZ = GetUserTransform().InverseTransformPoint(_leftFoot.position).z;
                _prevLeftLocalZ = leftZ;
                _wasLeftMovingForward = false;
                _l_maxZPosition = leftZ;
                _l_minZPosition = leftZ;
            }

        }
        public override void EndSkill()
        {
        }

        public override void UpdateSkill()
        {
            _timer.UpdateTimer();

            if(!_timer.IsRoundCompleted)
                return;

            if (_rightFoot != null)
            {
                float currRightLocalZ = GetUserTransform().InverseTransformPoint(_rightFoot.position).z;

                // 2) Determine if it's currently moving “forward” (Z increasing) vs “backward” (Z decreasing).
                bool isRightMovingForward = currRightLocalZ > _prevRightLocalZ;

                // 3a) If it WAS moving forward last frame and NOW is moving backward → we just hit a forward peak.
                if (_wasRightMovingForward && !isRightMovingForward)
                {
                    // At this instant (last frame) was the true max. However, currRightLocalZ is already below it,
                    // so we can simply take the previous frame’s Z as the peak. That’s _prevRightLocalZ.
                    float forwardPeakZ = _prevRightLocalZ;
                    _r_maxZPosition = Mathf.Max(_r_maxZPosition, forwardPeakZ);

                    // Trigger your “right‐foot forward footstep” here:
                    OnRightFootForwardStep(_rightFoot.position);
                }
                // 3b) If it WAS moving backward last frame and NOW is moving forward → we just hit a backward trough.
                else if (!_wasRightMovingForward && isRightMovingForward)
                {
                    float backwardTroughZ = _prevRightLocalZ;
                    _r_minZPosition = Mathf.Min(_r_minZPosition, backwardTroughZ);

                    // (Optionally) trigger a special step for the backward trough:
                    OnRightFootBackwardStep(_rightFoot.position);
                }

                _r_maxZPosition = Mathf.Max(_r_maxZPosition, currRightLocalZ);
                _r_minZPosition = Mathf.Min(_r_minZPosition, currRightLocalZ);

                _wasRightMovingForward = isRightMovingForward;
                _prevRightLocalZ = currRightLocalZ;
            }

            if (_leftFoot != null)
            {
                float currLeftLocalZ = GetUserTransform().InverseTransformPoint(_leftFoot.position).z;
                bool isLeftMovingForward = currLeftLocalZ > _prevLeftLocalZ;

                if (_wasLeftMovingForward && !isLeftMovingForward)
                {
                    float forwardPeakZ = _prevLeftLocalZ;
                    _l_maxZPosition = Mathf.Max(_l_maxZPosition, forwardPeakZ);
                    OnLeftFootForwardStep(_leftFoot.position);
                }
                else if (!_wasLeftMovingForward && isLeftMovingForward)
                {
                    float backwardTroughZ = _prevLeftLocalZ;
                    _l_minZPosition = Mathf.Min(_l_minZPosition, backwardTroughZ);
                    OnLeftFootBackwardStep(_leftFoot.position);
                }

                _l_maxZPosition = Mathf.Max(_l_maxZPosition, currLeftLocalZ);
                _l_minZPosition = Mathf.Min(_l_minZPosition, currLeftLocalZ);

                _wasLeftMovingForward = isLeftMovingForward;
                _prevLeftLocalZ = currLeftLocalZ;
            }
        }

        private void SpawningFootprint(Transform footTransform)
        {
            Vector3 position = footTransform.position;
            Quaternion rotation = GetUserTransform().rotation;
            GameObject step = ManagerPrefabPooler.Instance.GetFromPool(_footStepsPrefab);
            step.transform.position = position;
            step.transform.rotation = rotation;

            step.GetComponent<IOwner>().SetOwner(GetOwnerType());

            ApplyStats(step);
            ApplyStatsToReceivers(step);

            step.GetOrAdd<ReturnPoolOnDelay>().SetDelay(_baseStats.GetStat(StatSystem.STAT_TYPE.DURATION).GetValue());

        }

        private void OnRightFootForwardStep(Vector3 position)
        {
            SpawningFootprint(_rightFoot);
        }

        private void OnRightFootBackwardStep(Vector3 position)
        {
        }

        private void OnLeftFootForwardStep(Vector3 position)
        {

            SpawningFootprint(_leftFoot);
        }

        private void OnLeftFootBackwardStep(Vector3 position)
        {
        }
    }
}