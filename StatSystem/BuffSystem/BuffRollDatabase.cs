using System;
using System.Collections.Generic;
using Game.BuffSystem;
using Game.SkillSystem;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Buffs/Buff Roll Database", fileName = "BuffRollDatabase")]
public sealed class BuffRollDatabase : SerializedScriptableObject
{
    [Serializable]
    public sealed class ExtraPickSettings
    {
        [LabelText("Second % (base)")] public float SecondBase = 15f;
        [LabelText("Third % (base)")] public float ThirdBase = 0f;
        [LabelText("Luck -> +% / pt"), MinValue(0f)] public float LuckBonusPerPoint = 1f;
        [LabelText("Clamp Min/Max %")] public Vector2 Clamp = new(0f, 100f);

        public float ComputeChance(int pickIndex, float luck)
        {
            float baseVal = pickIndex == 2 ? SecondBase : ThirdBase;
            float v = baseVal + luck * LuckBonusPerPoint;
            return Mathf.Clamp(v, Clamp.x, Clamp.y);
        }
    }

    [Serializable]
    public sealed class DiminishingSettings
    {
        [LabelText("Reduce Skill Chance / pick")] public float ReduceSkill = 3f;
        [LabelText("Reduce PlayerBuff Chance / pick")] public float ReducePlayerBuff = 5f;
        [LabelText("Min Skill %")] public float SkillMin = 3f;
        [LabelText("Min PlayerBuff %")] public float PlayerMin = 3f;
    }

    [Serializable]
    public sealed class CardTypeWeights : IWeightedLoot
    {
        public BUFF_CARD_CATEGORY Category;
        [MinValue(0f)] public float Weight = 1f;
        public float GetWeight() => Weight;
    }

    [Serializable]
    public sealed class OpenTypeSettings
    {
        [BoxGroup("Open Type"),ReadOnly] public BUFF_OPEN_TYPE OpenType;
        [BoxGroup("Open Type"), LabelText("Cards / page"), MinValue(1)]
        public int CardsPerPage = 3;

        [BoxGroup("Category Weights")]
        public List<CardTypeWeights> CategoryWeights = new();

        [BoxGroup("Pools Skill")]
        public List<WeightedItem<SkillDefinition>> SkillPool = new();

        [BoxGroup("Pools Player")]
        public List<WeightedItem<BuffDefinition>> PlayerBuffPool = new(); // IsTargetPlayer = true

        [BoxGroup("Pools Chest Modifiers")]
        public List<WeightedItem<BuffDefinition>> ChestModifierPool = new();

        [BoxGroup("Rules")] public bool AvoidDuplicateSkillsInPage = true;
        [BoxGroup("Rules")] public bool ChestEnsureUniqueByStat = true;

        [BoxGroup("Extra Picks")] public ExtraPickSettings ExtraPicks = new();
        [BoxGroup("Diminishing")] public DiminishingSettings Diminishing = new();
    }

    [BoxGroup("Per OpenType")]
    [SerializeField] private List<OpenTypeSettings> _perOpenType = new();

    public OpenTypeSettings Get(BUFF_OPEN_TYPE type)
    {
        for (int i = 0; i < _perOpenType.Count; i++)
            if (_perOpenType[i].OpenType == type) return _perOpenType[i];
        return null;
    }

#if UNITY_EDITOR
    [Button, PropertyOrder(-1)]
    private void EnsureEntries()
    {
        Array values = Enum.GetValues(typeof(BUFF_OPEN_TYPE));
        var set = new HashSet<BUFF_OPEN_TYPE>();
        foreach (var s in _perOpenType) set.Add(s.OpenType);
        foreach (BUFF_OPEN_TYPE v in values)
            if (!set.Contains(v)) _perOpenType.Add(new OpenTypeSettings { OpenType = v });
    }
#endif
}
