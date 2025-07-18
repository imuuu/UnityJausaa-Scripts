using Game.HitDetectorSystem;
using UnityEngine;

namespace Game.SkillSystem
{
    public class Ability_ShieldOfVitalEcho : Ability_MagicShield
    {
        private Collider[] _hitsOnceAlloc;
        private const int MAX_HITS_ONCE_ALLOC = 15;
        public override void UpdateSkill()
        {
        }

        protected override void OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver)
        {
            float reflectPercent = _baseStats.GetValueOfStat(StatSystem.STAT_TYPE.REFLECT_DAMAGE_PERCENT) * 0.01f;
            float reflectedDamage = dealer.GetDamage() * reflectPercent;

            if (reflectedDamage < 1f)
            {
                reflectedDamage = 1f;
            }

            if (_hitsOnceAlloc == null)
            {
                _hitsOnceAlloc = new Collider[MAX_HITS_ONCE_ALLOC];
            }

            float radius = _baseStats.GetValueOfStat(StatSystem.STAT_TYPE.AREA_EFFECT);
            SimpleDamage reflectDamage = CreateSimpleDamage(reflectedDamage);
            LayerMask layer = ManagerHitDectors.GetHitLayerMask();

            DoAreaDamageWithoutHitDetection(
                reflectDamage,
                GetGameObject().transform.position,
                _hitsOnceAlloc,
                layer,
                radius);

        }
    }
}