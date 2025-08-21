using Game.PoolSystem;
using Game.StatSystem;
using UnityEngine;

namespace Game.SkillSystem
{
    public class Ability_SpikeyBalls : Ability, IRecastSkill
    {
        [SerializeField] private GameObject _spikeyBallsPrefab;

        // <-- Local‐space offset from the foot/user transform (e.g. (0,1,0) spawns 1 unit above).
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1f, 0f);

        [Header("Launch Settings")]
        // How strongly to push the ball (Impulse)
        [SerializeField] private float _launchForce = 10f;
        // Maximum horizontal spread (± degrees) around the exact backward direction
        [SerializeField] private float _spreadAngleDegrees = 30f;
        // Vertical tilt: how many degrees above horizontal to shoot
        [SerializeField] private float _verticalAngleDegrees = 20f;

        public override void AwakeSkill()
        {
            base.AwakeSkill();

        }

        public override void StartSkill()
        {
            SpawnSpikeyBall(GetUserTransform());
        }

        public override void EndSkill()
        {
            base.EndSkill();
        }

        public override void UpdateSkill()
        {
        }

        private void SpawnSpikeyBall(Transform footTransform)
        {
            Vector3 position = footTransform.TransformPoint(_spawnOffset);
            Quaternion rotation = footTransform.rotation;

            GameObject step = ManagerPrefabPooler.Instance.GetFromPool(_spikeyBallsPrefab);
            step.transform.position = position;
            step.transform.rotation = rotation;

            step.GetComponent<IOwner>().SetOwner(GetOwnerType());
            ApplyStats(step);
            ApplyStatsToReceivers(step);
            step.GetOrAdd<ReturnPoolOnDelay>()
                .SetDelay(_baseStats.GetStat(StatSystem.STAT_TYPE.DURATION).GetValue());

            Rigidbody rb = step.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                float weight = _baseStats.GetStat(StatSystem.STAT_TYPE.WEIGHT).GetValue();

                rb.mass = weight;

                Vector3 backwardDir = -footTransform.forward;

                float randomYaw = Random.Range(-_spreadAngleDegrees, _spreadAngleDegrees);
                Vector3 yawedDir = Quaternion.AngleAxis(randomYaw, Vector3.up) * backwardDir;

                Vector3 rightAxis = Vector3.Cross(yawedDir, Vector3.up).normalized;

                Vector3 launchDir = Quaternion.AngleAxis(_verticalAngleDegrees, rightAxis) * yawedDir;
                launchDir.Normalize();

                rb.AddForce(launchDir * _launchForce, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("Ability_SpikeyBalls: no Rigidbody found on the spikey‐ball prefab!");
            }
        }
    }
}
