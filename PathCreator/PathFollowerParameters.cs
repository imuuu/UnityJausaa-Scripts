using Game.SkillSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PathSystem
{
    [System.Serializable]
    public class PathFollowerParameters
    {
        [Header("Path Source")]
        public PathCreator Path;

        [Header("Speed Settings")]
        [HideInInspector] public bool _showBaseSpeedInspector = true;
        [ShowIf("_showBaseSpeedInspector")] public float BaseSpeed = 5f;

        public SPEED_TYPE SpeedType = SPEED_TYPE.FIXED;
        [HideIf("SpeedType", SPEED_TYPE.FIXED)]
        public float MaxSpeed = 5f;

        [Header("Movement Mode")]
        public PathFollower.FOLLOW_MODE FollowMode = PathFollower.FOLLOW_MODE.LOOP;
        public bool IsReverse = false;
        public bool IsLookForward = true;
        [HideInInspector] public bool IsClosed = false; // not working atm

        [Header("Sampling Settings")]
        [Range(2, 100)]
        public int SamplesPerSegment = 20;
    }
}
