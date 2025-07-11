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
            Debug.Log($"ShieldOfLife: Blocked damage from {dealer.GetTransform().name} to {receiver.GetTransform().name}");
        }
    }
}