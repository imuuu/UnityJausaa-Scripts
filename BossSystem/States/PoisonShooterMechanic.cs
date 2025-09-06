using UnityEngine;

// ------------------------------------------------------------
// Boss Phase System (MonoBehaviours + Plain C# classes)
// - No ScriptableObjects needed.
// - Add BossPhaseController to your Boss prefab.
// - Add BossMechanic-derived components for individual abilities.
// - Configure Phases array in the inspector: which mechanics are
//   enabled/disabled per phase and what transitions trigger the next phase.
// - All runtime code avoids per-frame GC allocations (no LINQ, lists reused).
// ------------------------------------------------------------

namespace Game.BossSystem
{
    /// <summary>
    /// Example: shoots a projectile toward the player at fixed intervals if CanAct.
    /// Replace SpawnProjectile with your pooling/ability system.
    /// </summary>
    public sealed class PoisonShooterMechanic : Mechanic
    {
        [Header("Poison Shooter")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _interval = 1.5f;
        [SerializeField, Min(0f)] private float _projectileSpeed = 15f;

        private float _nextTime;

        public override void OnMechanicInit()
        {
            if (_muzzle == null) _muzzle = transform; // fallback
        }

        public override void OnPhaseEnter()
        {
            float t = Time.time;
            if (t > _nextTime) _nextTime = t + _interval; // simple desync protection
        }

        public override void Tick(float dt)
        {
            // AlwaysTick mechanics may run even when disabled; only act if CanAct
            if (!CanAct) return;
            float t = Time.time;
            if (t < _nextTime) return;
            _nextTime = t + _interval;

            if (Ctx == null || Ctx.Player == null) return;

            Vector3 origin = _muzzle.position;
            Vector3 dir = (Ctx.Player.position - origin);
            dir.y = 0f;
            float mag = dir.magnitude;
            if (mag < 0.001f) return;
            dir /= mag;

            SpawnProjectile(origin, dir * _projectileSpeed);
        }

        private void SpawnProjectile(Vector3 position, Vector3 velocity)
        {
            if (_projectilePrefab == null) return;
            // Pooling-friendly: avoid per-frame allocations
            GameObject go = Instantiate(_projectilePrefab, position, Quaternion.LookRotation(velocity.normalized, Vector3.up));
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = velocity;
        }
    }
}
