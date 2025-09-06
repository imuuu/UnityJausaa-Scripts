using System.Collections.Generic;
using System.Linq;
using Game.StatSystem;
using UnityEngine;

namespace Game.BuffSystem
{
    public class BuffCard_PlayerBuff : BuffCard
    {
        public override BUFF_CARD_TYPE BuffType { get; protected set; } = BUFF_CARD_TYPE.PLAYER_BUFF;
        private RarityDefinition _rarityDefinition;
        override public RarityDefinition RarityDefinition
        {
            get => ManagerBuffs.Instance.GetRarityListHolder().GetRarityByThreshold(Probability);
            protected set => _rarityDefinition = value;
        }

        public BuffDefinition PlayerBuffDefinition;
        public List<Modifier> Modifiers = new();
        public float Probability;

        override public void ApplyBuffToVisual(int index, BuffCardVisual buffVisual)
        {
            ChooseBuffCardVisual visual = buffVisual as ChooseBuffCardVisual;

            if (visual != null)
            {
                ApplyChooseBuffVisual(index, visual);
                return;
            }

            ChestBuffCardVisual chestVisual = buffVisual as ChestBuffCardVisual;
            if (chestVisual != null)
            {
                ApplyChestBuffVisual(index, chestVisual);
                return;
            }

        }

        private void ApplyChooseBuffVisual(int index, ChooseBuffCardVisual visual)
        {
            ManagerBuffs manager = ManagerBuffs.Instance;
            manager.SetRollModifierForBuff(index, Modifiers);
            manager.SetRollProbability(index, Probability);

            Sprite icon = manager.GetStatSystemIcons().PlayerBuffIcon;

            if (icon != null)
            {
                visual.Icon.gameObject.SetActive(true);
                visual.Icon.SetImage(icon);
            }

            visual.TextSkillName.text = PlayerBuffDefinition.PlayerBuffName;

            List<Modifier> modifiers = Modifier.CombineModifiers(Modifiers);


            visual.TextModifier1.text = PlayerBuffDefinition.GetModifierString(modifiers[0]);
            visual.TextModifier1.gameObject.SetActive(true);

            if (modifiers.Count > 1)
            {
                visual.TextModifier2.text = PlayerBuffDefinition.GetModifierString(modifiers[1]);
                visual.TextModifier2.gameObject.SetActive(true);
            }
            else
                visual.TextModifier2.gameObject.SetActive(false);

            if (modifiers.Count > 2)
            {
                visual.TextModifier3.text = PlayerBuffDefinition.GetModifierString(modifiers[2]);
                visual.TextModifier3.gameObject.SetActive(true);
            }
            else
                visual.TextModifier3.gameObject.SetActive(false);
        }

        private void ApplyChestBuffVisual(int index, ChestBuffCardVisual visual)
        {
            Sprite icon = ManagerBuffs.Instance.GetStatSystemIcons().PlayerBuffIcon;
            ManagerBuffs manager = ManagerBuffs.Instance;
            manager.SetRollModifierForBuff(index, Modifiers);
            manager.SetRollProbability(index, Probability);

            if (icon != null)
            {
                visual.Icon.gameObject.SetActive(true);
                visual.Icon.SetImage(icon);
            }

            visual.TextModifier.gameObject.SetActive(true);

            Modifier modifier = Modifiers[0];

            visual.TextModifier.text = PlayerBuffDefinition.GetModifierString(modifier);
        }

        override public void OnSelectBuff(int index)
        {
            Debug.Log($"$$$$$$$ New player buff added: {PlayerBuffDefinition.name}");

            Player player = Player.Instance;

            player.ApplyModifiers(Modifiers);

            ManagerBuffs.Instance.ReduceChanceToGetPlayerBuff();
            ManagerBuffs.Instance.CheckChoosesLeft();
        }

        // override public bool Roll()
        // {
        //     PlayerBuffDefinition = ManagerBuffs.Instance.GetPlayerBuffDefinition();

        //     if (PlayerBuffDefinition == null) return false;

        //     LootTable<BuffModifier> lootTable = PlayerBuffDefinition.LootTable;

        //     Probability = 0.0f;
        //     float p2 = ManagerBuffs.CHANCE_TO_GET_SECOND_BUFF / 100f;
        //     float p3 = ManagerBuffs.CHANCE_TO_GET_THIRD_BUFF / 100f;


        //     float totalP2P3 = p2 + p3;

        //     float forItem2 = p2 / totalP2P3;
        //     float forItem3 = p3 / totalP2P3;

        //     float[] slotChances = { 1f, p2, p3 };

        //     List<Modifier> mods = new ();

        //     Modifier first = PlayerBuffDefinition.GetRandomBuffModifier(Probability, out Probability);

        //     mods.Add(first);

        //     if (Random.value < p2) { mods.Add(PlayerBuffDefinition.GetRandomBuffModifier(Probability, out Probability)); }
        //     if (Random.value < p3) { mods.Add(PlayerBuffDefinition.GetRandomBuffModifier(Probability, out Probability)); }

        //     int count = 0;
        //     for (int i = 0; i < 3; i++)
        //     {
        //         if (i < mods.Count)
        //         {
        //             float pickProb = lootTable.GetProbabilityOf(mods[i] as BuffModifier);
        //             //Debug.Log($"BuffCard_PlayerBuff: Probability for is {pickProb}");
        //             //Probability += pickProb + slotChances[i];
        //             //Probability += pickProb;
        //             count += 1;
        //         }
        //     }


        //     float expectedSlotCount = (1f + p2 + p3) / 3f;
        //     float rarityScore = Probability / count;

        //     Probability = rarityScore;

        //     Debug.Log($"BuffCard_PlayerBuff: Probability updated to {Probability}");

        //     mods.ForEach(m => m.GenerateValue());

        //     Modifiers = mods;

        //     return true;

        // }

        override public bool Roll()
        {
            PlayerBuffDefinition = ManagerBuffs.Instance.GetPlayerBuffDefinition();
            if (PlayerBuffDefinition == null)
                return false;

            float p2 = ManagerBuffs.Instance.GetChanceToGetSecondBuffPercent() / 100f;
            float p3 = ManagerBuffs.Instance.GetChanceToGetThirdBuffPercent() / 100f;

            float slotProb1;
            Modifier buff1 = PlayerBuffDefinition.GetRandomBuffModifier(out slotProb1);
            List<Modifier> chosenBuffs = new() { buff1 };

            float total = slotProb1;
            //List<float> probabilities = new() { slotProb1 };

            float slotProb2 = 0f;
            if (Random.value < p2)
            {
                Modifier buff2 = PlayerBuffDefinition.GetRandomBuffModifier(out slotProb2);
                chosenBuffs.Add(buff2);
                total *= slotProb2;
                //probabilities.Add(slotProb2);

                float slotProb3 = 0f;
                if (Random.value < p3)
                {
                    Modifier buff3 = PlayerBuffDefinition.GetRandomBuffModifier(out slotProb3);
                    chosenBuffs.Add(buff3);
                    total *= slotProb3;

                    //probabilities.Add(slotProb3);
                }
            }

            //float lowestProbability = probabilities.Min();
            Probability = total;
            //Debug.Log($"BuffCard_PlayerBuff: Probability updated to {Probability})");

            foreach (var m in chosenBuffs)
                m.GenerateValue();

            Modifiers = chosenBuffs;
            return true;
        }



    }
}