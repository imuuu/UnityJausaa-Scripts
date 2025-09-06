using Game.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.SkillSystem
{
    public class AbilityBoss_SpitPoison : AbilityBoss, IRecastSkill
    {
        [SerializeField] private GameObject _poisonPrefab;
        [SerializeField] private GameObject _vfxSpit;

        [Header("Launch Settings")]
        [SerializeField] private float _launchForce = 10f;
        [SerializeField] private float _spreadAngleDegrees = 30f;
        [SerializeField] private float _verticalAngleDegrees = 20f;


        public override void StartSkill()
        {
            SpawnObject(_poisonPrefab, (gameObject, count, position, direction) =>
            {
                gameObject.GetComponent<IOwner>().SetOwner(GetOwnerType());
                ApplyStats(gameObject);
                ApplyStatsToReceivers(gameObject);
                Launch(gameObject);
            });

            SpawnObject(_vfxSpit, (gameObject, count, position, direction) =>
            {

            });

        }

        public override void EndSkill()
        {
            base.EndSkill();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
        }

        private void TriggerAnimation()
        {
            Transform rootUser = GetLaunchUser().transform;

            AnimationDirector animDirector = rootUser.FindInParents<AnimationDirector>(maxDepth: 10);

            //animDirector.PlayAttackClip(GetAbilityAnimation().AnimationClip, -1);
            animDirector.TriggerAttack(GetAbilityAnimation().AnimationClip, queueIfBusy: true);
        }

        public override void OnAbilityAnimationStart()
        {
            base.OnAbilityAnimationStart();
            TriggerAnimation();
        }


        private void Launch(GameObject target)
        {
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                float weight = _baseStats.GetStat(StatSystem.STAT_TYPE.WEIGHT).GetValue();

                rb.mass = weight;

                //Vector3 backwardDir = -footTransform.forward;

                float randomYaw = Random.Range(-_spreadAngleDegrees, _spreadAngleDegrees);
                Vector3 yawedDir = Quaternion.AngleAxis(randomYaw, Vector3.up) * target.transform.forward;

                Vector3 rightAxis = Vector3.Cross(yawedDir, Vector3.up).normalized;

                Vector3 launchDir = Quaternion.AngleAxis(_verticalAngleDegrees, rightAxis) * yawedDir;
                launchDir.Normalize();

                rb.AddForce(launchDir * _launchForce, ForceMode.Impulse);
            }
        }
    }
}