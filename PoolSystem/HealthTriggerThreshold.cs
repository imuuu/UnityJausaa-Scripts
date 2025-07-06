using System;
using Sirenix.OdinInspector;

namespace Game.PoolSystem
{
    [Serializable]
    public class HealthTriggerThreshold
    {
        [PropertySpace(SpaceBefore = 3, SpaceAfter = 5)]
        public bool IsReturnPool = true;
        [ProgressBar(0, 100, Height = 25)]
        [PropertySpace(SpaceBefore = 5, SpaceAfter = 5)]
        public float HealthTriggerPercent;
        // [PropertySpace(SpaceBefore = 5, SpaceAfter = 5)]
        // public GameObject SpawnedPrefab;

        public bool IsTriggered { get; set; } = false;
    }
}