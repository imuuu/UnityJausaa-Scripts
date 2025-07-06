using System.Collections.Generic;
using Game.SkillSystem;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BuffSystem
{
    [CreateAssetMenu(fileName = "BuffDefinition", menuName = "Skill/BuffDefinition", order = 1)]
    public class BuffDefinition : ScriptableObject
    {
        [Title("Target")]
        public bool IsTargetPlayer = false;
        [HideIf(nameof(IsTargetPlayer))]public SKILL_NAME TargetSkill;
        [ShowIf(nameof(IsTargetPlayer))] public string PlayerBuffName = "Player Buff";

        [Title("Display Settings")]
        [SerializeField] private ModifierStringTemplates _stringTemplates;
        [Title("Modifier Pool")]
        public List<BuffModifier> Modifiers;
        private LootTable<BuffModifier> _lootTable;

        public LootTable<BuffModifier> LootTable
        {
            get
            {
                if (_lootTable == null)
                {
                    _lootTable = new LootTable<BuffModifier>(Modifiers);
                }
                return _lootTable;
            }
        }

        public Modifier GetRandomBuffModifier()
        {
            Modifier mod = LootTable.GetRandomItem();
            //Debug.Log("=== Probability for buff" + _lootTable.GetProbabilityOf(mod as BuffModifier));
            //_probability += _lootTable.GetProbabilityOf(mod as BuffModifier);
            Debug.Log($"BuffDefinition: {name} - Probability updated to {_lootTable.GetProbabilityOf(mod as BuffModifier)}");
            return mod.Clone();
        }

        public Modifier GetRandomBuffModifier(float currentProbability, out float probability)
        {
            Modifier mod = LootTable.GetRandomItem();
            //Debug.Log("=== Probability for buff" + _lootTable.GetProbabilityOf(mod as BuffModifier));
            probability = _lootTable.GetProbabilityOf(mod as BuffModifier) + currentProbability;
            return mod.Clone();
        }

        public Modifier GetRandomBuffModifier(out float probability)
        {
            Modifier mod = LootTable.GetRandomItem();
            //Debug.Log("=== Probability for buff" + _lootTable.GetProbabilityOf(mod as BuffModifier));
            probability = _lootTable.GetProbabilityOf(mod as BuffModifier);
            return mod.Clone();
        }

        public Modifier GetRandomBuffModifier2(out float weight)
        {
            Modifier mod = LootTable.GetRandomItem();
            //Debug.Log("=== Probability for buff" + _lootTable.GetProbabilityOf(mod as BuffModifier));
            weight = _lootTable.GetWeightOf(mod as BuffModifier);
            return mod.Clone();
        }


        // private float _probability = 0f;
       
        // public float Probability => _probability;
        // public List<Modifier> GetRandomModifiers()
        // {
        //     _probability = 0f;
        //     float p2 = ManagerBuffs._chance_to_get_second_buff / 100f;
        //     float p3 = ManagerBuffs.CHANCE_TO_GET_THIRD_BUFF / 100f;

        //     float[] slotChances = { 1f, p2, p3 };
        //     List<Modifier> mods = new List<Modifier>();

        //     // draw up to 3
        //     var first = GetRandomBuffModifier(); mods.Add(first);
        //     if (Random.value < p2) { mods.Add(GetRandomBuffModifier()); }
        //     if (Random.value < p3) { mods.Add(GetRandomBuffModifier()); }

        //     int count = 0;
        //     for (int i = 0; i < 3; i++)
        //     {
        //         if (i < mods.Count)
        //         {
        //             // probability of picking that specific buff in the table
        //             float pickProb = _lootTable.GetProbabilityOf(mods[i] as BuffModifier);
        //             //_probability += slotChances[i] * pickProb;
        //             _probability += pickProb + slotChances[i];
        //             count += 2; 
        //         }
        //         // else: you didn’t get that slot, so contribute 0
        //     }

        //     // normalize by total “expected slots”
        //     float expectedSlotCount = (1f + p2 + p3) / 3f;
        //     //float rarityScore = _probability / expectedSlotCount;
        //     float rarityScore = _probability / count;// - (mods.Count * 0.07f);


        //     // Debug.Log(
        //     //   $">>>>> Rolled {mods.Count} buffs; " +
        //     //   $"raw sum-weight = {_probability:F4}, " +
        //     //   $"normalized rarity = {rarityScore:F4}" +
        //     //   $"expected slots = {expectedSlotCount:F4}"
        //     // );

        //     _probability = rarityScore;

        //     mods.ForEach(m => m.GenerateValue());

        //     return mods;
        // }

        public string GetModifierString(Modifier modifier)
        {
            return _stringTemplates.GetTemplate(modifier);
        }

        // [ValueDropdown("GetAbilityNames")]
        // public string SelectedAbility;

        // private static IEnumerable<string> GetAbilityNames()
        // {
        //     var abilityType = typeof(Ability);
        //     return Assembly.GetAssembly(abilityType)
        //         .GetTypes()
        //         .Where(t => t.IsClass && !t.IsAbstract && abilityType.IsAssignableFrom(t))
        //         .Select(t => t.Name);
        // }

    }
}