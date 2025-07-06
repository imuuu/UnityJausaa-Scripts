using UnityEngine;

namespace Game.HitDetectorSystem
{
    public interface IOnPiercing
    {
        public void OnPiercing(HitCollisionInfo hitInfo);
    }
}