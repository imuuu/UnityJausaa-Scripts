using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Game.SkillSystem;
using System;

[CreateAssetMenu(menuName = "Bosses/Ability Definition")]
public class BossAbilityDefinition : SerializedScriptableObject
{
    [Title("Choose Ability")]
    [OdinSerialize, SerializeReference]
    [NonSerialized] private SkillDefinition _ability;

    // public ABILITY_ID Id => ABILITY_ID.NONE;
    // public float Cooldown => (_ability as ICooldown)?.GetCooldown() ?? 0f;
    // public float Weight => 0;//_ability.GetWeight();

    // public AbilityBoss Ability => null;// _ability;

}