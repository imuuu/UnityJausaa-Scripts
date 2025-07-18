using UnityEngine;

namespace Game.SkillSystem
{
    public class Ability_ShieldOfLife : Ability_MagicShield
    {
        public override void UpdateSkill()
        {
        }

        protected override void OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver)
        {
            float heal = _baseStats.GetValueOfStat(StatSystem.STAT_TYPE.HEAL);

            if (heal <= 0f)
            {
                return;
            }

            IHealth health = GetUser().GetComponent<IHealth>();
            health.AddHealth(heal);
        }
    }
}