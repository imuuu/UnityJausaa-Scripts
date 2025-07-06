using Game.StatSystem;
using UnityEngine;

namespace Game.HitDetectorSystem
{
    public interface IHitDetector : IEnabled
    {
        public int GetID();
        /// <summary>
        /// Called each frame (or manually) to perform hit detection logic.
        /// </summary>
        public bool PerformHitCheck(out HitCollisionInfo hitInfo);

        /// <summary>
        /// Returns the underlying GameObject.
        /// </summary>
        public GameObject GetGameObject();

        /// <summary>
        /// Returns the owner component.
        /// </summary>
        public IOwner GetOwner();

        /// <summary>
        /// Called when a hit is detected.
        /// </summary>
        public void OnHit(HitCollisionInfo hitInfo);

        /// <summary>
        /// Called when a final hit is detected. Normally returned to pool after this.
        /// </summary>
        public void OnFinalHit(HitCollisionInfo hitInfo);

        public void OnPierceHit(HitCollisionInfo hitInfo);

        public bool IsBeginDestroyed();
        public void SetBeginDestroyed(bool value);

        /// <summary>
        /// Return ture when detector is Final, normaly used in multi-hit detectors
        /// </summary>
        public bool IsFinalHit();

        /// <summary>
        /// Set this to true when the hit detector is the final hit in a sequence.
        /// This is typically used in multi-hit detectors to indicate that no further hits should be processed.
        /// </summary>
        public void SetFinalHit(bool value);

#if RAYFIRE
        public void SetRayFireTriggerEnable(bool enable);
#endif

        /// <summary>
        /// If true, this detector is triggered manually instead of automatically.
        /// </summary>
        public bool IsManual { get; }

        public bool IsManualDestroy();
        public void SetManualDestroy(bool value);

        /// <summary>
        /// Call this to manually trigger a hit check.
        /// </summary>
        public void TriggerManualHitCheck();

        // --- New piercing properties ---

        /// <summary>
        /// The maximum number of collisions that will cause this detector to stop.
        /// Use -1 for infinite.
        /// </summary>
        public int MaxPiercing { get; }

        public float PiercingChance { get; }

        /// <summary>
        /// The remaining number of collisions (piercings) allowed.
        /// </summary>
        public int RemainingPiercing { get; }

        public int TotalPierces { get; set; }

        /// <summary>
        /// Decrements the piercing count. The parameter hitHasOwner indicates whether the hit object has an owner.
        /// This lets the detector decide whether to count this hit toward the piercing limit.
        /// </summary>
        /// <param name="hitHasOwner">True if the hit object has an IOwner component; false otherwise.</param>
        public bool DecrementPiercing();

        /// <summary>
        /// The timer when the IHitDetector is able to hit again,
        /// if its <= 0 it can hit every frame if able
        /// </summary>
        public float GetHitHistoryTimer();

        public bool ShouldPerformHitCheck(float deltaTime);

    }
}
