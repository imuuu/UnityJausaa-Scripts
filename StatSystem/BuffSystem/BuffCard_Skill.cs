using Game.SkillSystem;
using UnityEngine;

namespace Game.BuffSystem
{
    public class BuffCard_Skill : BuffCard
    {
        override public BUFF_CARD_TYPE BuffType { get; protected set; } = BUFF_CARD_TYPE.SKILL;
        private RarityDefinition _rarityDefinition;
        override public RarityDefinition RarityDefinition
        {
            get => ManagerBuffs.Instance.GetRarityListHolder().GetRarityByName(MODIFIER_RARITY.MYTHIC);
            protected set => _rarityDefinition = value;
        }

        public SkillDefinition SkillDefinition;

        override public void ApplyBuffToVisual(int index, BuffCardVisual buffVisual)
        {
            ChooseBuffCardVisual visual = buffVisual as ChooseBuffCardVisual;

            if (visual == null) return;

            visual.Icon.gameObject.SetActive(true);
            visual.Icon.SetImage(SkillDefinition.Icon);
            visual.NewIcon.gameObject.SetActive(true);

            visual.TextSkillName.text = SkillDefinition.Name;

            visual.TextSkillDescription.gameObject.SetActive(true);
            visual.TextSkillDescription.text = SkillDefinition.Description;

        }

        override public bool Roll()
        {
            SkillDefinition = ManagerSkills.Instance.GetRandomNotActivePlayerSkill();

            if (SkillDefinition == null) return false;

            return true;
        }

        override public void OnSelectBuff(int index)
        {
            int slot = ManagerSkills.Instance.GetNextValidSlot();
            ManagerSkills.Instance.AddPlayerSkill(slot, SkillDefinition);
            Debug.Log($"$$$$$$$ New skill added: {SkillDefinition.name}");

            ManagerBuffs.Instance.ReduceChanceToGetSkill();

            ManagerBuffs.Instance.CheckChoosesLeft();
        }
    }
}