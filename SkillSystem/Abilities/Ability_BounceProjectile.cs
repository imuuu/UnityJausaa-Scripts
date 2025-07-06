using System;
using System.Collections.Generic;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.SkillSystem
{

    // I Think this ability went too complex. It would be much better the projectile use mono with logic.
    public class Ability_BounceProjectile : Ability_ShootProjectile, IManualEnd
    {
        #region Inspector Fields

        [BoxGroup("Area Effect Prefab")]
        [SerializeField][AssetsOnly] private GameObject _areaEffectPrefab; // Prefab for the area effect.
        [BoxGroup("Area Effect Prefab")]
        [SerializeField] private float _delayReturnToPoolAreaEffect = 0.5f;
        [BoxGroup("Area Effect Prefab")]
        [SerializeField] private float _areaEffectDamagePercent = 0.5f;

        [BoxGroup("Trajectory Settings")]
        [SerializeField] private float _speed = 8f;            // Initial speed for the first bounce.
        [BoxGroup("Trajectory Settings")]
        [SerializeField] private float _launchAngle = 45f;       // Initial launch angle in degrees.
        [BoxGroup("Trajectory Settings")]
        [SerializeField] private float _gravity = 9.81f;
        [BoxGroup("Trajectory Settings")]
        [SerializeField] private float _impactY = 0f;            // Impact height (e.g. ground level).

        [BoxGroup("Bounce Settings")]
        [SerializeField] private int _bounceCount = 3;           // Total number of bounces.
        [BoxGroup("Bounce Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _dampingFactor = 0.8f;    // Speed reduction per bounce.
        [BoxGroup("Bounce Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _angleDampingFactor = 1f; // Optional angle damping.

        [BoxGroup("Sampling Settings")]
        [SerializeField] private int _samplesPerBounce = 30;     // Samples for each bounce arc.

        [Title("Damage layer")]
        [SerializeField] private LayerMask _enemyLayerMask;
        private Collider[] _hitColliders;

        private SimpleDamage _damageDealer;                  // Damage dealer for the area effect.
        private float _radiusAreaEffect;
        private int _lastBounceHitCount = 0;


        #endregion

        #region Internal Bounce State and Caching

        // For each spawned projectile we store its bouncing state.
        private class ProjectileBounceState
        {
            public Transform Target;                   // The spawned projectile's transform.
            public List<Vector3> SamplePositions;        // Precomputed positions along the bounce path.
            public List<float> SampleTimes;              // The corresponding timestamps (in seconds).
            public float TotalFlightTime;                // Total time from launch to final impact.
            public float CurrentTime;                    // Time elapsed for this projectile.
            public int LastBounceIndex;                  // For triggering bounce effects.
            public bool IsLaunched;                      // True while the projectile is active.
        }

        // List to hold each projectile's bounce state.
        private List<ProjectileBounceState> _activeBounceStates = new List<ProjectileBounceState>();

        // Structure to define the bounce parameters.
        private struct BounceParameters : IEquatable<BounceParameters>
        {
            public float Speed;
            public float LaunchAngle;
            public float Gravity;
            public float ImpactY;
            public int BounceCount;
            public float DampingFactor;
            public float AngleDampingFactor;
            public int SamplesPerBounce;
            public float StartY;

            public bool Equals(BounceParameters other)
            {
                return Mathf.Approximately(Speed, other.Speed) &&
                       Mathf.Approximately(LaunchAngle, other.LaunchAngle) &&
                       Mathf.Approximately(Gravity, other.Gravity) &&
                       Mathf.Approximately(ImpactY, other.ImpactY) &&
                       BounceCount == other.BounceCount &&
                       Mathf.Approximately(DampingFactor, other.DampingFactor) &&
                       Mathf.Approximately(AngleDampingFactor, other.AngleDampingFactor) &&
                       SamplesPerBounce == other.SamplesPerBounce &&
                       Mathf.Approximately(StartY, other.StartY);
            }
            public override bool Equals(object obj) => obj is BounceParameters bp && Equals(bp);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Speed.GetHashCode();
                    hashCode = (hashCode * 397) ^ LaunchAngle.GetHashCode();
                    hashCode = (hashCode * 397) ^ Gravity.GetHashCode();
                    hashCode = (hashCode * 397) ^ ImpactY.GetHashCode();
                    hashCode = (hashCode * 397) ^ BounceCount;
                    hashCode = (hashCode * 397) ^ DampingFactor.GetHashCode();
                    hashCode = (hashCode * 397) ^ AngleDampingFactor.GetHashCode();
                    hashCode = (hashCode * 397) ^ SamplesPerBounce;
                    hashCode = (hashCode * 397) ^ StartY.GetHashCode();
                    return hashCode;
                }
            }
        }

        // Cached bounce path (computed once for given parameters) to save computation.
        private static Dictionary<BounceParameters, (List<Vector3> positions, List<float> times, float totalTime)> _bouncePathCache;

        #endregion

        #region Ability Lifecycle

        public override void AwakeSkill()
        {
            base.AwakeSkill();
            _hitColliders = new Collider[10];
        }

        public override void StartSkill()
        {
            if(_activeBounceStates == null)
                _activeBounceStates = new List<ProjectileBounceState>();
            else
                _activeBounceStates.Clear();

            if(_bouncePathCache == null)
                _bouncePathCache = new Dictionary<BounceParameters, (List<Vector3>, List<float>, float)>();

            if (_hitColliders == null || _hitColliders.Length == 0)
                _hitColliders = new Collider[10];

            if (_damageDealer == null)
            {
                _damageDealer = new SimpleDamage(_baseStats.GetStat(StatSystem.STAT_TYPE.DAMAGE).GetValue(),
                    DAMAGE_TYPE.PHYSICAL, DAMAGE_SOURCE.PLAYER);
            }
            float damage = _baseStats.GetStat(StatSystem.STAT_TYPE.DAMAGE).GetValue();
            _damageDealer.SetDamage(damage);
            _radiusAreaEffect = _baseStats.GetStat(StatSystem.STAT_TYPE.AREA_EFFECT).GetValue();

            _lastBounceHitCount = 0;

            base.StartSkill();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            UpdateBounceStates();
        }

        protected override void OnProjectileSpawn(GameObject spawned, Vector3 spawnPosition, Vector3 direction, PathSkill pathSkill)
        {
            base.OnProjectileSpawn(spawned, spawnPosition, direction, pathSkill);

            ProjectileBounceState state = CreateBounceState(spawnPosition, direction);
            state.Target = spawned.transform;
            _activeBounceStates.Add(state);
        }

        #endregion

        #region Bounce Path Computation and Caching

        private ProjectileBounceState CreateBounceState(Vector3 startPosition, Vector3 horizontalDirection)
        {
            ProjectileBounceState state = new ProjectileBounceState();
            state.CurrentTime = 0f;
            state.IsLaunched = true;
            state.LastBounceIndex = 0;

            BounceParameters bp = new BounceParameters
            {
                Speed = _speed,
                LaunchAngle = _launchAngle,
                Gravity = _gravity,
                ImpactY = _impactY,
                BounceCount = _bounceCount,
                DampingFactor = _dampingFactor,
                AngleDampingFactor = _angleDampingFactor,
                SamplesPerBounce = _samplesPerBounce,
                StartY = startPosition.y
            };

            Vector3 canonicalDir = Vector3.right;

            if (_bouncePathCache.TryGetValue(bp, out var cached))
            {
                state.SamplePositions = new List<Vector3>();
                // Compute the rotation required to convert from canonical to the actual horizontalDirection.
                // (Use FromToRotation to get a Quaternion that rotates from Vector3.right to horizontalDirection)
                Quaternion rot = Quaternion.FromToRotation(canonicalDir, horizontalDirection);
                // The cached path's first point is treated as the origin.
                Vector3 cachedOrigin = cached.positions[0];
                foreach (Vector3 pos in cached.positions)
                {
                    // Compute the delta from the origin and rotate it.
                    Vector3 delta = pos - cachedOrigin;
                    Vector3 rotatedDelta = rot * delta;
                    state.SamplePositions.Add(startPosition + rotatedDelta);
                }
                state.SampleTimes = new List<float>(cached.times);
                state.TotalFlightTime = cached.totalTime;
            }
            else
            {
                // If no cached path exists, compute the full path using the canonical direction.
                Debug.Log("======== Computing new bounce path for parameters: " + bp.ToString());
                ComputeFullPathForState(startPosition, canonicalDir,
                    out List<Vector3> positions, out List<float> times, out float totalTime);
                state.SamplePositions = positions;
                state.SampleTimes = times;
                state.TotalFlightTime = totalTime;
                // Cache it for future projectiles.
                _bouncePathCache[bp] = (new List<Vector3>(positions), new List<float>(times), totalTime);
            }
            return state;
        }


        private void ComputeFullPathForState(Vector3 startPosition, Vector3 horizontalDirection,
     out List<Vector3> positions, out List<float> times, out float totalTime)
        {
            positions = new List<Vector3>();
            times = new List<float>();
            totalTime = 0f;
            Vector3 currentStart = startPosition;
            float currentSpeed = _speed;
            float currentAngle = _launchAngle;

            // Record the start point.
            positions.Add(currentStart);
            times.Add(totalTime);

            for (int bounce = 0; bounce < _bounceCount; bounce++)
            {
                float radAngle = currentAngle * Mathf.Deg2Rad;
                float v0x = currentSpeed * Mathf.Cos(radAngle);
                float v0y = currentSpeed * Mathf.Sin(radAngle);

                // Solve for flight time where: currentStart.y + v0y*t - 0.5*g*t^2 = impactY.
                float a = 0.5f * _gravity;
                float b = -v0y;
                float c = _impactY - currentStart.y;
                float discriminant = b * b - 4 * a * c;
                if (discriminant < 0)
                {
                    Debug.LogError("No valid impact time found on bounce " + bounce);
                    break;
                }
                float flightTime = (-b + Mathf.Sqrt(discriminant)) / (2 * a);

                // Sample this arc.
                for (int i = 1; i <= _samplesPerBounce; i++)
                {
                    float t = (i / (float)_samplesPerBounce) * flightTime;
                    // Use horizontalDirection provided (canonical: Vector3.right).
                    Vector3 horizontalDisp = horizontalDirection * v0x * t;
                    float verticalDisp = v0y * t - 0.5f * _gravity * t * t;
                    Vector3 samplePoint = currentStart + horizontalDisp + Vector3.up * verticalDisp;
                    float sampleGlobalTime = totalTime + t;
                    positions.Add(samplePoint);
                    times.Add(sampleGlobalTime);
                }

                totalTime += flightTime;
                currentStart = positions[positions.Count - 1];
                currentSpeed *= _dampingFactor;
                currentAngle *= _angleDampingFactor;
            }
        }


        #endregion

        #region Update and Impact Handling

        private void UpdateBounceStates()
        {
            if (_activeBounceStates.Count == 0)
                return;

            // Iterate over a copy so we can remove finished states.
            for (int s = _activeBounceStates.Count - 1; s >= 0; s--)
            {
                ProjectileBounceState state = _activeBounceStates[s];
                if (!state.IsLaunched)
                    continue;

                state.CurrentTime += Time.deltaTime;
                if (state.CurrentTime >= state.TotalFlightTime)
                {
                    state.Target.position = state.SamplePositions[state.SamplePositions.Count - 1];
                    state.IsLaunched = false;
                    OnBounce(_bounceCount, state.Target.position);
                    OnLastBounceHit(state.Target.position);
                    // Optionally, remove this state from the active list.
                    _activeBounceStates.RemoveAt(s);
                    continue;
                }

                // Find which two sample times bracket currentTime.
                int index = 0;
                for (int i = 0; i < state.SampleTimes.Count - 1; i++)
                {
                    if (state.SampleTimes[i] <= state.CurrentTime && state.CurrentTime <= state.SampleTimes[i + 1])
                    {
                        index = i;
                        break;
                    }
                }
                // Trigger bounce effect if we passed into a new bounce segment.
                int bounceIndex = index / _samplesPerBounce;
                if (bounceIndex != state.LastBounceIndex)
                {
                    state.LastBounceIndex = bounceIndex;
                    OnBounce(bounceIndex, state.SamplePositions[index]);
                }
                float tSegment = Mathf.InverseLerp(state.SampleTimes[index], state.SampleTimes[index + 1], state.CurrentTime);
                state.Target.position = Vector3.Lerp(state.SamplePositions[index], state.SamplePositions[index + 1], tSegment);
            }
        }

        private void OnBounce(int bounceNumber, Vector3 position)
        {
            GameObject pooledAreaEffect = ManagerPrefabPooler.Instance.GetFromPool(_areaEffectPrefab);
            pooledAreaEffect.transform.position = position;
            ApplyStatsToReceivers(pooledAreaEffect, false);

            AreaEffect areaEffects = pooledAreaEffect.GetComponent<AreaEffect>();
            ActionScheduler.RunAfterDelay(_delayReturnToPoolAreaEffect, () =>
            {
                if (pooledAreaEffect != null)
                {
                    ManagerPrefabPooler.Instance.ReturnToPool(pooledAreaEffect);
                }
            });
            if (areaEffects != null)
            {
                areaEffects.OnAreaEffectHit();
            }
            float multiplier = bounceNumber == 1 ? 1f : 1.5f * bounceNumber;
            float damage = _baseStats.GetStat(StatSystem.STAT_TYPE.DAMAGE).GetValue();
            float areaDamage = damage * _areaEffectDamagePercent * multiplier;
            _damageDealer.SetDamage(areaDamage);

            DoAreaDamage(_damageDealer, position, _hitColliders, _enemyLayerMask, _radiusAreaEffect);
        }
        
        private void OnLastBounceHit(Vector3 position)
        {
            if(++_lastBounceHitCount == GetProjectileAmount())
            {
                ManagerSkills.Instance.EndManualSkill(this);
            }
        }

        #endregion

        // (Optional) Uncomment if you need a Gizmo visualization in the editor.
        // private void OnDrawGizmos()
        // {
        //     if (_samplePositions == null || _samplePositions.Count < 2)
        //         return;
        //     Gizmos.color = Color.red;
        //     for (int i = 0; i < _samplePositions.Count - 1; i++)
        //         Gizmos.DrawLine(_samplePositions[i], _samplePositions[i + 1]);
        // }
    }
}
