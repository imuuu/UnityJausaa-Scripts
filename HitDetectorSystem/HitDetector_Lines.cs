using UnityEngine;
using System.Collections.Generic;

namespace Game.HitDetectorSystem
{
    public class HitDetector_Lines : HitDetector
    {
        [Header("Line Detector Settings")]
        [Tooltip("Array of points defining the line segments to check for collisions.")]
        [SerializeField] private Vector3[] _points;

        [Header("Clipping Settings")]
        [Tooltip("If true, clip the line points at the collision with a no-owner object (e.g. a wall).")]
        [SerializeField] private bool _clipPointsOnCollision = true;

        private Vector3[] _finalPoints;

        protected override void OnEnable()
        {
            base.OnEnable();
            _finalPoints = _points;
        }

        /// <summary>
        /// Performs the hit check along the defined line segments.
        /// - In multiple-collision mode, all collisions along the line are gathered and processed internally.
        ///   For each collision, if the hit object lacks an owner and clipping is enabled, the line is clipped.
        ///   For each collision, OnHit is called and the piercing count is decremented.
        ///   In this mode the detector returns false so the manager does not process it again.
        /// - In single-collision mode, the detector returns the first qualifying hit (and possibly clipped points)
        ///   so that the manager can handle it.
        /// </summary>
        /// <param name="primaryHitObject">
        /// In single-collision mode, this is set to the hit object. In multiple mode, it remains null.
        /// </param>
        /// <returns>True if a collision is found (in single-collision mode); otherwise false.</returns>
        public override bool PerformHitCheck(out HitCollisionInfo hitInfo)
        {
            hitInfo = null;
            if (_points == null || _points.Length < 2)
                return false;

            ManagerHitDectors.Instance.ClearHitHistory(this);

            List<HitCollisionInfo> collisions =
                    ManagerHitDectors.Instance.GetAllCollisionsOnLine(_points);

            int indexClip = 0;
            for(;indexClip < collisions.Count; indexClip++)
            {
                HitCollisionInfo collision = collisions[indexClip];
#if UNITY_EDITOR
                if (_managerHitDectors.IsDebug())
                {
                    string parentName = collision.HitObject.transform.parent ? " Parent: " + collision.HitObject.transform.parent.name : "";
                    Debug.Log("||||||||||||||||| Line Collision: " + collision.HitObject.name + parentName + " id: " + collision.HitObject.GetInstanceID());
                }
#endif
                if (collision.HitObject.GetComponent<IOwner>() != null && ManagerHitDectors.Instance.HandleHit(this, collision))
                {
                    if (this.RemainingPiercing <= 0)
                    {
                        break;
                    }
                }

                if(collision.HitObject.GetComponent<IOwner>() == null)
                {
                    break;
                }

            }

            if(!_clipPointsOnCollision)
            {
                return false;
            }

            for(int i = 0; i < collisions.Count; i++)
            {
                if(i == indexClip) 
                {
                    _finalPoints = ManagerHitDectors.Instance.ClipPointsToCollision(_points, out List<GameObject> ownerHitObjects);
                    break;
                }

                if(collisions[i].HitObject.GetComponent<IOwner>() == null)
                {
                    _finalPoints = ManagerHitDectors.Instance.ClipPointsToCollision(_points, out List<GameObject> ownerHitObjects);
                    break;
                }
            }

            return false;

            // if (_detectMultipleCollisions)
            // {
            //     List<ManagerHitDectors.CollisionInfo> collisions =
            //         ManagerHitDectors.Instance.GetAllCollisionsOnLine(_points);

            //     foreach (ManagerHitDectors.CollisionInfo collision in collisions)
            //     {
            //         string parentName = collision.hitObject.transform.parent ? " Parent: " + collision.hitObject.transform.parent.name : "";
            //         Debug.Log("Collision: " + collision.hitObject.name + parentName + " id: " + collision.hitObject.GetInstanceID());


            //         if (collision.hitObject.GetComponent<IOwner>() != null && ManagerHitDectors.Instance.HandleHit(this, collision.hitObject))
            //         {
            //             if (this.RemainingPiercing <= 0)
            //             {
            //                 _finalPoints = ManagerHitDectors.Instance.ClipPointsToCollision(_points, out List<GameObject> ownerHitObjects);
            //                 return false; // we need to return true to avoid duplicate processing by the manager and infinite loop
            //             }
            //         }
            //     }

            //     foreach (ManagerHitDectors.CollisionInfo collision in collisions)
            //     {
            //         if(collision.hitObject.GetComponent<IOwner>() == null)
            //         {
            //             _finalPoints = ManagerHitDectors.Instance.ClipPointsToCollision(_points, out List<GameObject> ownerHitObjects);
            //             return false;
            //         }
            //     }
            //     return false;
            // }
            // return false;
            // else
            // {
            //     // SINGLE COLLISION MODE:
            //     if (_clipPointsOnCollision)
            //     {
            //         Vector3[] clippedPoints = ManagerHitDectors.Instance.ClipPointsToCollision(_points, _stopAtNoOwner, out List<GameObject> ownerHitObjects);
            //         if (clippedPoints.Length < _points.Length)
            //         {
            //             _finalPoints = clippedPoints;
            //             // Use the manager’s helper to get the primary collision.
            //             Vector3 collisionPoint;
            //             bool collided = ManagerHitDectors.Instance.CheckPointsCollision(_points, _stopAtNoOwner, out collisionPoint, out primaryHitObject);
            //             if (collided)
            //             {
            //                 // The manager will handle OnHit and piercing in its HandleHit.
            //                 return true;
            //             }
            //         }
            //         else
            //         {
            //             _finalPoints = _points;
            //             Vector3 collisionPoint;
            //             bool collided = ManagerHitDectors.Instance.CheckPointsCollision(_points, _stopAtNoOwner, out collisionPoint, out primaryHitObject);
            //             if (collided)
            //             {
            //                 return true;
            //             }
            //         }
            //     }
            //     else
            //     {
            //         Vector3 collisionPoint;
            //         bool collided = ManagerHitDectors.Instance.CheckPointsCollision(_points, _stopAtNoOwner, out collisionPoint, out primaryHitObject);
            //         if (collided)
            //         {
            //             return true;
            //         }
            //     }
            //     return false;
            // }
        }

        // private void TriggerPiercing(List<HitCollisionInfo> piercings)
        // {
        //     foreach (HitCollisionInfo collision in piercings)
        //     {
        //         OnPierceHit(collision.HitObject, collision.CollisionPoint, collision.Direction);
        //     }
        // }

        /// <summary>
        /// Returns the final (possibly clipped) array of points.
        /// </summary>
        public Vector3[] GetPoints()
        {
            return _finalPoints != null ? _finalPoints : _points;
        }

        /// <summary>
        /// Allows external scripts to set the line points at runtime.
        /// </summary>
        public void SetLinePoints(Vector3[] points)
        {
            _points = points;
            _finalPoints = points;
        }
    }
}
