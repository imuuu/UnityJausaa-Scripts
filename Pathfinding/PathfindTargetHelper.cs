using Pathfinding;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.Pathfind
{
    public class PathfindTargetHelper : SerializedMonoBehaviour
    {
        [Title("References")]
        [OdinSerialize] private AIDestinationSetter _aiDestinationSetter;
        [Title("List of Target Scripts")]
        [OdinSerialize] private ITarget[] _targetScripts;

        [Title("Target")]
        [SerializeField] private bool _findPlayer = true;

        [HideIf("_findPlayer")]
        [SerializeField] private Transform _target;

        protected void Start()
        {
            if (!_findPlayer) return;

            // ActionScheduler.RunWhenTrue(IsPlayerPresent, () =>
            // {
            //     SetTarget(Player.Instance.transform);
            // });
        }

        protected void OnEnable()
        {
            if (_findPlayer && _target == null)
            {
                ActionScheduler.RunWhenTrue(IsPlayerPresent, () =>
                {
                    _target = Player.Instance.transform;
                    SetTarget(_target);
                });
            }
        }

        public void SetTarget(Transform newTarget)
        {
            for (int i = 0; i < _targetScripts.Length; i++)
            {
                _targetScripts[i].SetTarget(newTarget);
            }
            _aiDestinationSetter.target = newTarget;
        }

        private bool IsPlayerPresent()
        {
            return Player.Instance != null;
        }

    }
}
