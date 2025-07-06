
namespace Game.HitDetectorSystem
{
    public class HitDetector_PhysicHand : HitDetector_UnityCollider
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            _manualDestroy = true;
            _hitInterval = 0.3f;
            _maxPiercing = -1;
            _enableVelocityCheck = true;
            _velocityThreshold = 12f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
