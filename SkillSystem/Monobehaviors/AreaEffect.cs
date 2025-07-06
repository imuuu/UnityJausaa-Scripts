using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.SkillSystem
{
    public class AreaEffect : StatReceiver
    {
        [BoxGroup("Area Effect Settings")]
        [Tooltip("if true, the area effect will be disabled after the duration")]
        [SerializeField, ToggleLeft] private bool _enableDurationDisable = false;
        [BoxGroup("Area Effect Settings")]
        [SerializeField] private float _durationDisable = 0f;
        [SerializeField] private bool _disableAtStart = false;

        [BoxGroup("Events", ShowLabel = false)]
        [SerializeField] private UnityEvent _onAreaEffectHit;

        private void Start()
        {
            if(_disableAtStart) gameObject.SetActive(false);
        }

        public void OnAreaEffectHit()
        {
            gameObject.SetActive(true);

            _onAreaEffectHit?.Invoke();
            if (_enableDurationDisable)
            {
                ActionScheduler.RunAfterDelay(_durationDisable, () =>
            {
                gameObject.SetActive(false);
            });
            }

        }

        public void SetDuration(float duration)
        {
            _durationDisable = duration;

            if (_durationDisable > 0) _enableDurationDisable = true;
            else _enableDurationDisable = false;
        }
    }
}