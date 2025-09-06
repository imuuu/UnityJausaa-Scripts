using Game.StatSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.SkillSystem
{
    public class Skill_MultiAbility : Skill
    {
        [Space(10)]
        [Title("Choose Abilities(NOT YET SUPPORTED, WILL BE IMPLEMENTED)")]
        [OdinSerialize, SerializeReference]
        public IAbility[] _abilities;

        public override void StartSkill()
        {
            foreach (IAbility ability in _abilities)
            {
                ability.SetRootSkill(this);
                ability.SetUser(GetUser());

                ManagerSkills.Instance.ExecuteSkill(ability);
            }
        }
        public override void EndSkill()
        {

        }

        public override void UpdateSkill()
        {

        }

        public override void AddModifier(Modifier modifier)
        {
            throw new System.NotImplementedException();
        }

        public override void ClearModifiers()
        {
            throw new System.NotImplementedException();
        }

        public override void OnAbilityAnimationStart()
        {
            throw new System.NotImplementedException();
        }
    }
}
