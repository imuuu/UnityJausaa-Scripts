
using Game.StatSystem;
using Sirenix.Serialization;
using UnityEngine;
namespace Game.SkillSystem
{
    public abstract class AbilityBoss : Ability, IAbilityBoss, IWeightedLoot, IStatList
    {
        [Tooltip("Weight for loot selection. Higher means more likely to be chosen.")]
        [SerializeField] private float _weightTrigger;

        public StatList GetStatList()
        {
            return _baseStats;
        }

        public float GetStatValue(STAT_TYPE statType)
        {
            return _baseStats.GetValueOfStat(statType);
        }

        public float GetWeight()
        {
            return _weightTrigger;
        }

        public AbilityBoss Clone()
        {
            var clone = (AbilityBoss)SerializationUtility.CreateCopy(this);
            return clone;
        }


    }
}