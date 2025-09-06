// using System;
// using Game.BuffSystem;
// using Game.SkillSystem;
// using Game.StatSystem;
// using Sirenix.OdinInspector;
// using UnityEngine;

// [Serializable]
// public sealed class ChestModifierBlueprint : IWeightedLoot
// {
//     [BoxGroup("ID"), LabelText("Id")]
//     public string Id;

//     [BoxGroup("Target")]
//     public bool IsPlayerTarget = true;

//     [BoxGroup("Target"), ShowIf(nameof(IsPlayerTarget) == false)]
//     public SKILL_NAME SkillTarget;

//     [BoxGroup("Modifier")]
//     public STAT_TYPE Stat;

//     [BoxGroup("Modifier"), LabelText("Operation")]
//     public MODIFIER_OPERATION Operation; // esim. FLAT/INCREASE/MORE (pidä synkassa omaan Modifier-järjestelmääsi)

//     [BoxGroup("Values")]
//     [LabelText("Base Min/Max")] public Vector2 BaseRange = new(1, 3);

//     [BoxGroup("Values")]
//     [LabelText("Per-Rarity Mult")] public Vector2 RarityMultiplier = new(1f, 1f);

//     [BoxGroup("Rarity Weights")]
//     [TableList] public WeightedItem<RarityDefinition>[] RarityWeights = Array.Empty<WeightedItem<RarityDefinition>>();

//     [BoxGroup("Weight"), MinValue(0f)]
//     public float Weight = 1f;

//     public float GetWeight() => Weight;

//     public Modifier RollModifier(System.Random sysRand, RarityDefinition pickedRarity)
//     {
//         // Luo Modifier oman järjestelmäsi mukaisesti
//         // Esim:
//         float t = (float)sysRand.NextDouble();
//         float min = BaseRange.x * Mathf.Lerp(1f, RarityMultiplier.x, t);
//         float max = BaseRange.y * Mathf.Lerp(1f, RarityMultiplier.y, t);
//         float val = UnityEngine.Random.Range(min, max); // voit käyttää myös sysRand jos haluat deterministisen

//         var m = new Modifier
//         {
//             TargetStat = Stat,
//             Operation = Operation,
//             Value = val,
//             // täydennä jos teillä on muita kenttiä
//         };

//         // Kohdistus: lisätään m:lle tarvittaessa tieto skill-kohteesta (riippuu teidän Modifier-rakenteesta)
//         return m;
//     }

//     public RarityDefinition RollRarity()
//     {
//         if (RarityWeights == null || RarityWeights.Length == 0) return null;
//         var lt = new LootTable<WeightedItem<RarityDefinition>>(RarityWeights);
//         return lt.GetRandomItem().Item;
//     }
// }
