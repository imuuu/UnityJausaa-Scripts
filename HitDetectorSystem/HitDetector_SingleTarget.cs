using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    public class HitDetector_SingleTarget : HitDetector
    {
        [SerializeField][ReadOnly] private GameObject _targetObject;
        [SerializeField][ReadOnly] private float _triggerHitRadius = 1f;
        private Vector3 _targetPosition;
        private Vector3 _lastPosition;

        private void Reset()
        {
            _enablePiercing = false;
        }

        public override bool PerformHitCheck(out HitCollisionInfo hitCollisionInfo)
        {
            if(_targetObject != null)
            {
                _targetPosition = _targetObject.transform.position;
            }

            float distance = Vector3.Distance(this.transform.position, _targetPosition);
            if (distance > _triggerHitRadius)
            {
                hitCollisionInfo = null;
                _lastPosition = this.transform.position;
                return false;
            }

            HitCollisionInfo hitInfo = new HitCollisionInfo()
            {
                HasCollisionPoint = false,
                HitObject = _targetObject,
                HasDirection = true,
                Direction = (_targetPosition - _lastPosition).normalized, // this might be wrong if target is null
            };
            hitCollisionInfo = hitInfo;
            return true;
        }

        public void SetTargetObject(GameObject targetObject)
        {
            _targetObject = targetObject;
            _targetPosition = targetObject.transform.position;
            _lastPosition = this.transform.position;
        }

        public void SetTriggerHitRadius(float radius)
        {
            _triggerHitRadius = radius;
        }
    }
}