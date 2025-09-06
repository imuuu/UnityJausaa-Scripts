using UnityEngine;
namespace Game.HitDetectorSystem
{
    /// <summary>
    /// Helper class to forward trigger events to the HitDetector_UnityCollider.
    /// </summary>
    public class HitDetectorHelper_UnityCollider : MonoBehaviour
    {
        [SerializeField] private HitDetector_UnityCollider _detector;

        public void OnTriggerEnter(Collider other)
        {
            _detector.OnTriggerEnter(other);
        }

        public void OnTriggerExit(Collider other)
        {
            _detector.OnTriggerExit(other);
        }

    }
}