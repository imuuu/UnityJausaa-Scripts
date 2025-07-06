using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Game.HitDetectorSystem
{
    public class HitDetector_PointForward : HitDetector
    {
        [Title("Raycast Settings")]
        [Tooltip("How far ahead of the last position to start the raycast (in the travel direction).")]
        [SerializeField] private float _raycastStartOffset = 0f;

        [Tooltip("Extra distance beyond the traveled distance to cast the ray.")]
        [SerializeField] private float _raycastExtraDistance = 0f;

        [Title("Add More Rays")]
        [SerializeField, ToggleLeft] private bool _useCustomRays = false;
        [SerializeField, ShowIf("_useCustomRays")]
        private List<CustomRay> _customRays = new List<CustomRay>();

        #region Debug
        [Space(8)]
        [SerializeField, ToggleLeft, PropertyOrder(100)] private bool _EnableDebug = true;
        [SerializeField, ShowIf("_EnableDebug"), PropertyOrder(100)] private Color _debugLineColor = Color.red;
        #endregion

        private Vector3 _rayStart;
        private Vector3 _lastPosition = Vector3.zero;
        private bool _skipFirstFrame = true;

        [Serializable]
        private class CustomRay
        {
            [Tooltip("Start offset from the object's position.")]
            public Vector3 StartOffset = Vector3.zero;

            [Tooltip("Direction of the custom ray.")]
            public Vector3 Direction = Vector3.forward;

            [Tooltip("Extra distance beyond traveled distance for this ray.")]
            public float ExtraDistance = 0f;

            [Tooltip("Enable debug drawing for this custom ray.")]
            public bool Debug = false;

            [ShowIf("Debug"), Tooltip("Color for the debug ray.")]
            public Color DebugColor = Color.green;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _lastPosition = transform.position;
            _skipFirstFrame = true;
        }

        public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            if (_skipFirstFrame)
            {
                _lastPosition = transform.position;
                _skipFirstFrame = false;
                hitInfo = null;
                return false;
            }

            Vector3 currentPosition = transform.position;
            Vector3 movement = currentPosition - _lastPosition;
            float distance = movement.magnitude;

            if (distance > 0f)
            {
                Vector3 direction = movement.normalized;
                _rayStart = _lastPosition + direction * _raycastStartOffset;
                float totalRayDistance = distance + _raycastExtraDistance;

                if (Physics.Raycast(_rayStart, direction, out RaycastHit hit, totalRayDistance, ManagerHitDectors.GetHitLayerMask()))
                {

#if UNITY_EDITOR
                    if (_managerHitDectors.IsDebugHitRays())
                    {
                        var go = MarkHelper.DrawSphereTimed(hit.point, 0.1f, 5, Color.red);
                        go.transform.localScale = new Vector3(0.1f, 5, 0.1f);
                        Debug.DrawRay(_rayStart, direction * totalRayDistance, _debugLineColor, 10f);
                    }
#endif
                    
                    hitInfo = _managerHitDectors.GetNewHitCollisionInfo(hit.collider.gameObject);
                    hitInfo.SetCollisionPoint(hit.point);
                    hitInfo.SetDirection(direction);
                    _lastPosition = currentPosition;
                    return true;
                }

                if (_useCustomRays)
                {
                    foreach (CustomRay custom in _customRays)
                    {
                        Vector3 start = currentPosition + custom.StartOffset;
                        Vector3 dir = custom.Direction.normalized;
                        float rayDist = distance + custom.ExtraDistance;
                        if (Physics.Raycast(start, dir, out RaycastHit crHit, rayDist, ManagerHitDectors.GetHitLayerMask()))
                        {
#if UNITY_EDITOR
                            if (_managerHitDectors.IsDebugHitRays())
                                Debug.DrawRay(start, dir * rayDist, custom.DebugColor, 10f);
#endif
                            hitInfo = _managerHitDectors.GetNewHitCollisionInfo(crHit.collider.gameObject);
                            hitInfo.SetCollisionPoint(crHit.point);
                            hitInfo.SetDirection(dir);
                            _lastPosition = currentPosition;
                            return true;
                        }
// #if UNITY_EDITOR
//                         if (_managerHitDectors.IsDebugHitRays())
//                             Debug.DrawRay(start, dir * rayDist, custom.debugColor, 0f);
// #endif
                    }
                }
            }
            else
            {
                _rayStart = transform.position;
            }

            _lastPosition = currentPosition;
            hitInfo = null;
            return false;
        }

        private bool IsDebug()
        {
            return _EnableDebug;
        }

        private void OnDrawGizmosSelected()
        {
            if ((!_EnableDebug && !_useCustomRays) || !this.enabled) return;
            //Debug.Log("HitDetector_PointForward: OnDrawGizmosSelected()");
            if (IsDebug())
            {
                Vector3 debugRayStart = transform.position + transform.forward * _raycastStartOffset;
                Vector3 debugRayEnd = debugRayStart + transform.forward * _raycastExtraDistance;
                Gizmos.color = _debugLineColor;
                Gizmos.DrawLine(debugRayStart, debugRayEnd);
                Gizmos.DrawSphere(debugRayStart, 0.05f);
                Gizmos.DrawSphere(debugRayEnd, 0.05f);
            }

            if (_useCustomRays)
            {
                foreach (CustomRay custom in _customRays)
                {
                    Vector3 start = transform.position + custom.StartOffset;
                    Vector3 end = start + custom.Direction.normalized * (custom.ExtraDistance);
                    Gizmos.color = custom.Debug ? custom.DebugColor : Color.yellow;
                    Gizmos.DrawLine(start, end);
                    Gizmos.DrawSphere(start, 0.05f);
                    Gizmos.DrawSphere(end, 0.05f);
                }
            }
        }
    }
}
