using System;
using UnityEngine;

namespace Game.SkillSystem
{
    public interface IProjectile
    {
        public Transform GetTransform();
        public void SetMaxSpeed(float speed);
        public void SetSpeed(float speed);
        public void SetDirection(Vector3 direction);

        public void SetTarget(Transform target);
        public void SetTarget(Vector3 targetPosition);

        public void SetSpeedType(SPEED_TYPE speedType, float accelerateDuration = 1f);

        public Action OnTargetReached { get; set; }
    }
}