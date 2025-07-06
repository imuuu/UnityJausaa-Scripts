using UnityEngine;

namespace Game.Extensions
{
    /// <summary>
    /// Extension methods for UnityEngine.Transform.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Rotates this transform so its forward vector points directly away from the specified world position.
        /// </summary>
        /// <param name="transform">The transform to rotate.</param>
        /// <param name="targetPosition">World position of the target.</param>
        public static void LookAtInvert(this Transform transform, Vector3 targetPosition)
        {
            // Vector3 direction = (targetPosition - transform.position).normalized * -10f;
            // direction = transform.position + direction;
            // transform.LookAt(direction);

            Vector3 invertedPoint = transform.position * 2f - targetPosition;
            transform.LookAt(invertedPoint);
        }

        /// <summary>
        /// Rotates this transform so its forward vector points directly away from the specified target transform.
        /// </summary>
        /// <param name="transform">The transform to rotate.</param>
        /// <param name="target">The target transform.</param>
        public static void LookAtInvert(this Transform transform, Transform target)
        {
            transform.LookAtInvert(target.position);
        }
    }
}