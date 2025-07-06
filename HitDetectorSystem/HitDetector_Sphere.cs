using UnityEngine;
namespace Game.HitDetectorSystem
{
    public class HitDetector_Sphere : HitDetector
    {
        [Header("Sphere Settings")]
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        [Header("Debug")]
        [SerializeField] private bool _drawDebug = true;
        [SerializeField] private Color _debugColor = Color.red;

        private Vector3 lastPosition = Vector3.zero;

        /// <summary>
        /// Called each frame by ManagerRaycastHit to do our collision check.
        /// </summary>
        public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            if(lastPosition == Vector3.zero)
            {
                lastPosition = transform.position;
            }

            Vector3 currentOffsetPos = transform.position + _offset;
            Vector3 lastOffsetPos = lastPosition + _offset;

            Vector3 direction = currentOffsetPos - lastOffsetPos;
            float distance = direction.magnitude;

            if (distance > 0f)
            {
                if (Physics.SphereCast(lastOffsetPos, _radius, direction.normalized, out RaycastHit hit, distance))
                {
                    hitInfo = _managerHitDectors.GetNewHitCollisionInfo(hit.collider.gameObject);
                    return true;
                }

            }

            lastPosition = transform.position;
            hitInfo = null;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_drawDebug && this.enabled)
            {
                DrawDebugSphere(transform.position + _offset, _radius, _debugColor);
            }
        }
        private void DrawDebugSphere(Vector3 center, float r, Color color)
        {
#if UNITY_EDITOR
        // Draw a wire sphere in the Scene view (editor only)
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, r);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.right, r);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, r);
#endif
        }
    }
}
