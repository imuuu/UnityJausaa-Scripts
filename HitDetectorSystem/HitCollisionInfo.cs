using UnityEngine;

namespace Game.HitDetectorSystem
{
    public class HitCollisionInfo
    {
        public bool HasCollisionPoint;
        public Vector3 CollisionPoint;

        public bool HasDirection;
        public Vector3 Direction;
        public GameObject HitObject;

        //layer where hit was detected
        public LayerMask HitLayer;

        public IDamageDealer CustomDamageDealer;

        public void SetCollisionPoint(Vector3 point)
        {
            CollisionPoint = point;
            HasCollisionPoint = true;
        }

        public void SetDirection(Vector3 direction)
        {
            Direction = direction;
            HasDirection = true;
        }

        public bool IsEnemy() => HitObject != null && HitObject.layer == ManagerHitDectors.ENEMY_LAYER;
        public bool IsWall() => HitObject != null && HitObject.layer == ManagerHitDectors.WALL_LAYER;
        public bool IsObstacle() => HitObject != null && HitObject.layer == ManagerHitDectors.OBSTACLE_LAYER;
        public bool IsPlayer() => HitObject != null && HitObject.layer == ManagerHitDectors.PLAYER_LAYER;

        public bool IsWallOrObstacle()
        {
            return HitObject != null && (HitObject.layer == ManagerHitDectors.WALL_LAYER || HitObject.layer == ManagerHitDectors.OBSTACLE_LAYER);
        }
    }
}
