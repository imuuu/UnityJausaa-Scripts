using UnityEngine;

namespace Game.SkillSystem
{
    public class Ability_LightningBeam : AbilityChargeable, IRecastSkill
    {
        [SerializeField] private GameObject _beamPrefab;

        public override void StartSkill()
        {
            SpawnObject(_beamPrefab, (gameObject, count, position, direction) =>
            {
                ApplyStats(gameObject);
                ApplyStatsToReceivers(gameObject);
            });
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
        }
    }
}

