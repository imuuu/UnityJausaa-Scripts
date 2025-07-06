using System.Collections.Generic;

namespace Game.HitDetectorSystem
{
    /// <summary>
    /// A detector that can return more than one HitCollisionInfo in a single check.
    /// </summary>
    public interface IMultiHitDetector : IHitDetector
    {
        /// <summary>
        /// Perform your custom hit logic and return *all* collisions this frame.
        /// </summary>
        List<HitCollisionInfo> PerformHitChecks();
    }
}
