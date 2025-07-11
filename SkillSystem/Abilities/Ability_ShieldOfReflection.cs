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
        }
    }
}