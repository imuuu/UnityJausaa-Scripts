using System.Collections.Generic;
using UnityEngine;
using Game.StatSystem;
using Sirenix.OdinInspector;
/// <summary>
/// Defines one of your “Gods Blessings” / upgrade cards:
/// – Name, icon, description
/// – Which STAT_TYPE it affects, and a list of Modifier(s)
/// – How many ranks you can buy
/// – A (multi-currency) cost per rank
/// – A (multi-currency) refund per rank
/// – An optional prize per rank
/// </summary>
[CreateAssetMenu(fileName = "NewBlessingUpgrade", menuName = "Game/Upgrade/Blessing")]
public class BlessingUpgrade : ScriptableObject
{
    public Modifier Modifier;

    [Header("Rank Settings")]
    [Tooltip("How many times the player can buy this upgrade")]
    [MinValue(1f)] public int MaxRanks = 5;

    [MinValue(0f)] public int CurrentRank = 0;

    [Header("Economy")]
    [Tooltip("What it costs to buy one rank (can be multiple currencies)")]
    public CostScheme CostScheme = new CostScheme();

    /// <summary>
    /// Get the cost for the *next* purchase (i.e. at CurrentRank).
    /// </summary>
    public List<CurrencyAmount> GetCostForCurrentRank()
    {
        //Debug.Log($"Getting cost for rank {CurrentRank} in BlessingUpgrade: {name}");
        return CostScheme.GetCostForRank(CurrentRank);
    }

    public List<CurrencyAmount> GetFinalLastCost()
    {
        return CostScheme.GetCostForRank(MaxRanks - 1);
    }

    public Modifier GetTotalModifier()
    {
        if (Modifier == null || CurrentRank <= 0)
        {
            return null;
        }

        List<Modifier> modifiers = new();
        for (int i = 0; i < CurrentRank; i++)
        {
            modifiers.Add(Modifier);
        }

        return Modifier.CombineModifiers(modifiers)[0];
    }

    private void OnValidate()
    {
        CostScheme?.ValidateScheme();
    }
}
