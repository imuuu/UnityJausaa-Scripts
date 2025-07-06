using Game.SkillSystem;
using UnityEngine;
namespace Game.PoolSystem
{
    public class ReturnPoolOnDelay : MonoBehaviour
    {
        [SerializeField] private float _delay = 1f;
        private float _returnAtTime;
        private bool _returned = false;

        private PhysicHandTouched _physicHandTouched;
        private float _delayAfterTouch = 1f; 
        private void OnEnable()
        {
            _returnAtTime = _delay;
            _returned = false;
            _physicHandTouched = null;
        }

        private void Update()
        {
            if (ManagerPause.IsPaused()) return;

            if (_returned) return;

            _returnAtTime -= Time.deltaTime;
            if (_returnAtTime > 0f) return;

            if(_physicHandTouched == null)
            {
                _physicHandTouched = GetComponent<PhysicHandTouched>();
            }

            if (_physicHandTouched != null && _physicHandTouched.enabled)
            {
                _returnAtTime = _delayAfterTouch;
                return;
            }

            _returned = true;

            if (!gameObject.activeSelf) return;

            ManagerPrefabPooler.Instance.ReturnToPool(gameObject);
        }

        public void SetDelay(float delay)
        {
            _delay = delay;
            _returnAtTime = _delay; // reset the timer
            _returned = false; // reset the returned state
        }

        public float GetDelay()
        {
            return _delay;
        }
    }
}
