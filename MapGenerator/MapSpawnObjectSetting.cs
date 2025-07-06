using UnityEngine;
using Sirenix.OdinInspector;
namespace Game.MapGenerator
{
    public class MapSpawnObjectSetting : MapSpawnItemBase
    {
        [FoldoutGroup("Random Rotation Settings")]
        [Tooltip("Enable random rotation for this object when spawned?")]
        public bool enableRandomRotation = false;

        [FoldoutGroup("Random Rotation Settings")]
        [Tooltip("Minimum rotation (in degrees).")]
        public float minRotation = 0f;

        [FoldoutGroup("Random Rotation Settings")]
        [Tooltip("Maximum rotation (in degrees).")]
        public float maxRotation = 360f;

        /// <summary>
        /// Spawns the object (applies random rotation, etc.).
        /// </summary>
        [Button(ButtonSizes.Medium)]
        public void SpawnObject()
        {
            float rotationAngle = enableRandomRotation ? Random.Range(minRotation, maxRotation) : 0f;
            transform.rotation = Quaternion.Euler(0f, rotationAngle, 0f);
            Debug.Log($"{name} spawned with rotation {rotationAngle}°.");
        }

        public override void TriggerSpawn()
        {
            if (isEnabled)
            {
                SpawnObject();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
