using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.MapGenerator
{
    public abstract class MapSpawnItemBase : MonoBehaviour
    {
        [FoldoutGroup("Common Settings")]
        [Tooltip("Weight used for weighted selection (higher means more likely).")]
        [Range(1, 100)]
        public int weight = 1;

        [FoldoutGroup("Common Settings")]
        [Tooltip("If false, this spawn item will be skipped.")]
        public bool isEnabled = true;

        /// <summary>
        /// When called, this item will perform its spawn logic.
        /// </summary>
        public abstract void TriggerSpawn();

    }
}
