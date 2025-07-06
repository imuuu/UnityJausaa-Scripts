using System.Collections.Generic;
using Game.SkillSystem;
using Game.StatSystem;
using UnityEngine;

namespace Game.BuffSystem
{
    public class BuffCard_SkillBuff : BuffCard
    {
        public override BUFF_CARD_TYPE BuffType { get; protected set; } = BUFF_CARD_TYPE.SKILL_BUFF;
        private RarityDefinition _rarityDefinition;
        public override RarityDefinition RarityDefinition
        {
            get => ManagerBuffs.Instance.GetRarityListHolder().GetRarityByThreshold(Probability);
            protected set => _rarityDefinition = value;
        }

        public SkillDefinition SkillDefinition;
        public List<Modifier> Modifiers = new();
        public float Probability;
        override public void ApplyBuffToVisual(int index, ChooseBuffCardVisual visual)
        {
            ManagerBuffs manager = ManagerBuffs.Instance;
            manager.SetRollModifierForBuff(index, Modifiers);
            manager.SetRollProbability(index, Probability);

            visual.TextSkillName.text = SkillDefinition.Name;

            visual.Icon.gameObject.SetActive(true);
            visual.Icon.SetImage(SkillDefinition.Icon);

            //modRarity = ManagerBuffs.Instance.GetRarityListHolder().GetRarityByThreshold(Probability);

            List<Modifier> modifiers = Modifier.CombineModifiers(Modifiers);

            visual.TextSkillDescription.gameObject.SetActive(false);
            visual.TextSkillDescriptionSmall.gameObject.SetActive(true);
            visual.TextSkillDescriptionSmall.text = SkillDefinition.DescriptionSmall;

            visual.TextModifier1.text = manager.GetModifierString(SkillDefinition.SkillName, modifiers[0]);
            visual.TextModifier1.gameObject.SetActive(true);

            if (modifiers.Count > 1)
            {
                visual.TextModifier2.text = manager.GetModifierString(SkillDefinition.SkillName, modifiers[1]);
                visual.TextModifier2.gameObject.SetActive(true);
            }
            else
                visual.TextModifier2.gameObject.SetActive(false);

            if (modifiers.Count > 2)
            {
                visual.TextModifier3.text = manager.GetModifierString(SkillDefinition.SkillName, modifiers[2]);
                visual.TextModifier3.gameObject.SetActive(true);
            }
            else
                visual.TextModifier3.gameObject.SetActive(false);


            //visual.NewIcon.gameObject.SetActive(false);
        }

        override public bool Roll()
        {
            BuffDefinition buffDef = GetRandomBuffDefInActiveSkills();

            if( buffDef == null)
            {
                Debug.LogError("No valid BuffDefinition found for active skills.");
                return false;
            }
            SkillDefinition = ManagerSkills.Instance.GetSkillDefinition(buffDef.TargetSkill);
            LootTable<BuffModifier> lootTable = buffDef.LootTable;

            float p2 = ManagerBuffs.Instance.GetChanceToGetSecondBuffPercent() / 100f;
            float p3 = ManagerBuffs.Instance.GetChanceToGetThirdBuffPercent() / 100f;

            float slotProb1;
            Modifier buff1 = buffDef.GetRandomBuffModifier(out slotProb1);
            //Debug.Log($"1 buff probability: {slotProb1}");
            List<Modifier> chosenBuffs = new() { buff1 };
            float total = slotProb1;

            float slotProb2 = 0f;
            if (Random.value < p2)
            {
                Modifier buff2 = buffDef.GetRandomBuffModifier(out slotProb2);
                chosenBuffs.Add(buff2);
                total *= slotProb2;

                //Debug.Log($"2 buff probability: {slotProb2}");

                float slotProb3 = 0f;
                if (Random.value < p3)
                {
                    Modifier buff3 = buffDef.GetRandomBuffModifier(out slotProb3);
                    chosenBuffs.Add(buff3);
                    total *= slotProb3;

                    //Debug.Log($"3 buff probability: {slotProb3}");
                }
            }

            Probability = total;
            //Debug.Log($"BuffCard_SkillBuff: Probability updated to {Probability}");

            chosenBuffs.ForEach(m => m.GenerateValue());

            Modifiers = chosenBuffs;

            return true;
        }

        // public Modifier GetRandomBuffModifier(LootTable<BuffModifier> lootTable)
        // {
        //     Modifier mod = lootTable.GetRandomItem();
        //     Probability += lootTable.GetProbabilityOf(mod as BuffModifier);
        //     return mod.Clone();
        // }

        public BuffDefinition GetRandomBuffDefInActiveSkills()
        {
            List<BuffDefinition> buffDefinitions = ManagerBuffs.Instance.GetBuffDefinitions();

            if (buffDefinitions.Count == 0) return null;

            int randomIndex = -1;

            bool found = false;
            int maxAttempts = 1000;
            while (true)
            {
                SkillDefinition skillDefinition = ManagerSkills.Instance.GetRandomPlayerSkill();

                for (int i = 0; i < buffDefinitions.Count; i++)
                {
                    if (buffDefinitions[i].TargetSkill == skillDefinition.SkillName
                    && !buffDefinitions[i].IsTargetPlayer)
                    {
                        found = true;
                        randomIndex = i;
                        break;
                    }
                }

                maxAttempts--;
                if (maxAttempts <= 0)
                {
                    Debug.LogError("Max attempts reached while trying to find a valid buff definition.");
                    break;
                }

                if (found) break;
            }

            if (randomIndex < 0)
            {
                Debug.LogError("Invalid random index for buff definitions: " + randomIndex);
                return null;
            }

            return buffDefinitions[randomIndex];
        }

        public override void OnSelectBuff(int index)
        {
            SkillDefinition skillDefinition =
            ManagerSkills.Instance.GetActivePlayerSkillDef(SkillDefinition.SkillName);

            if (skillDefinition == null)
            {
                Debug.LogError($"Skill not found for buff: ---");
                return;
            }

            //int id = 0;
            foreach (Modifier mod in Modifiers)
            {
                // Debug.Log(" ");
                // Debug.Log(" ");
                // Debug.Log($"============{id}============");
                // Debug.Log($"===> Adding Buff: {buffModifier.GetTYPE()} to Skill: {skillDefinition.name}");
                // Debug.Log($"===> {buffModifier.ToString()}");
                // Debug.Log("===========================");
                // Debug.Log(" ");
                // Debug.Log(" ");
                skillDefinition.AddModifier(mod);
                //id++;
            }

            ManagerSkills.Instance.RemoveExecutedSkill(skillDefinition.GetSkill());
            ICooldown cooldown = skillDefinition.GetSkill() as ICooldown;
            if (cooldown != null)
            {
                cooldown.SetCurrentCooldown(0);
            }
            skillDefinition.UseSkill();

            ManagerBuffs.Instance.CheckChoosesLeft();
        }
    }


}