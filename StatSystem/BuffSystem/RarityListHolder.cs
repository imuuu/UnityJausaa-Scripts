using Game.BuffSystem;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "RarityListHolder", menuName = "RarityListHolder", order = 1)]
public class RarityListHolder : ScriptableObject
{

    public RarityDefinition[] Rarities;

    public RarityDefinition GetRarityByThreshold(float threshold)
    {
        // Iterate from the smallest threshold (rarest) down to the largest
        for (int i = Rarities.Length - 1; i >= 0; i--)
        {
            var modRarity = Rarities[i];
            if (threshold < modRarity.Threshold)
                return modRarity;
        }

        // If threshold ≥ 1.0 (or never matched), return the “worst” (highest Threshold)
        return Rarities[0];
    }

    public RarityDefinition GetRarityByName(MODIFIER_RARITY rarity)
    {
        foreach (var modRarity in Rarities)
        {
            if (modRarity.Rarity == rarity)
            {
                return modRarity;
            }
        }

        return null;
    }

    [Button("Open Buff Cards TEST")][PropertySpace(10, 10)][PropertyOrder(1)]
    public void OpenBuffCards()
    {
        ManagerBuffs.Instance.TestGETBuff();
    }
}