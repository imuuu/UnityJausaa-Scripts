using UnityEngine;

namespace Game.PoolSystem
{
    /// <summary>
    /// If attached to a pooled object, calls ReturnObjectToPool on OnDisable.
    /// </summary>
    public class PoolReturnOnDisabled : MonoBehaviour
    {
        [Tooltip("If there's no pool manager or the object isn't recognized, destroy instead.")]
        public bool destroyIfNoPool = false;

        public bool IsReturnEnabled { get; set; } = true;

        private void OnDisable()
        {
            // We only do the logic if the game is still running (e.g. not exiting play mode)
            if (!Application.isPlaying) return;

            if (!IsReturnEnabled) return;

            if (ManagerPrefabPooler.Instance)
            {
                ManagerPrefabPooler.Instance.ReturnToPool(gameObject);
            }
            else
            {
                if (destroyIfNoPool)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
