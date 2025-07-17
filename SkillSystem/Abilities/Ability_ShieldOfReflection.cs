using UnityEngine;
namespace Game.SkillSystem
{
    public class Ability_ShieldOfReflection : Ability_MagicShield
    {
        public override void UpdateSkill()
        {
        }

        protected override void OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver)
        {
            Debug.Log($"ShieldOfReflection: Blocked damage from {dealer.GetTransform().name} to {receiver.GetTransform().name}");

            float reflectPercent = _baseStats.GetStat(StatSystem.STAT_TYPE.REFLECT_DAMAGE_PERCENT).GetValue() * 0.01f;
            float reflectedDamage = dealer.GetDamage() * reflectPercent;

            if( reflectedDamage < 1f)
            {
                reflectedDamage = 1f;
            }

            

            Debug.Log($"===========> ShieldOfReflection: Reflecting {reflectedDamage} damage back to {dealer.GetTransform().name}");

            DAMAGE_SOURCE damageSource = receiver.GetTransform().GetComponent<IDamageDealer>()?.GetDamageSource() ?? DAMAGE_SOURCE.ENVIRONMENT;

            IDamageDealer reflectDealer = dealer.AsSimpleDamage();
            reflectDealer.SetDamageSource(damageSource);
            reflectDealer.SetDamage(reflectedDamage);

            IDamageReceiver reflectReceiver = dealer.GetTransform().GetComponent<IDamageReceiver>();

            if (reflectDealer == null || reflectReceiver == null)
            {
                return;
            }

            DamageCalculator.CalculateDamage(reflectDealer, reflectReceiver);
        }
    }
}