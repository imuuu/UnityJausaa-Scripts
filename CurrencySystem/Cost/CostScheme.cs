using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class CostScheme
{
    public enum PRICE_SCHEME_TYPE
    {
        FIXED,
        SCALED_BASE,
        SCALED_COMPOUND
    }

    [SerializeField, EnumToggleButtons]
    private PRICE_SCHEME_TYPE Type = PRICE_SCHEME_TYPE.FIXED;

    [SerializeField, ToggleLeft][InfoBox("Only works for SCALED_BASE and SCALED_COMPOUND types")]
    private bool _continueWhereLastTierLeftOff = false;

    [SerializeField, ShowIf(nameof(_continueWhereLastTierLeftOff))]
    private BlessingUpgrade _lastCostScheme = null;

    [ShowIf(nameof(Type), PRICE_SCHEME_TYPE.FIXED)]
    [Tooltip("Exact cost for each rank if using fixed pricing")]
    public List<ListCurrencyAmount> FixedCostsPerRank = new();

    [ShowIf(nameof(Type), PRICE_SCHEME_TYPE.SCALED_BASE)]
    [Tooltip("Settings for base-powered scaling (base * multiplier^rank)")]
    public ScaledCostSettings ScaledCost = new();

    [ShowIf(nameof(Type), PRICE_SCHEME_TYPE.SCALED_COMPOUND)]
    [Tooltip("Settings for compound scaling (iterative multiply+round each step)")]
    public CompoundCostSettings CompoundCost = new();

    /// <summary>
    /// Returns the cost for the given rank index (0-based).
    /// </summary>
    public List<CurrencyAmount> GetCostForRank(int rank)
    {
        switch (Type)
        {
            case PRICE_SCHEME_TYPE.FIXED:
                int idx = Mathf.Clamp(rank, 0, FixedCostsPerRank.Count - 1);
                var fixedList = FixedCostsPerRank[idx].Amounts;
                if (fixedList.Count == 0)
                    Debug.LogWarning($"[CostScheme] No fixed cost defined for rank {idx}");
                return fixedList;

            case PRICE_SCHEME_TYPE.SCALED_BASE:
                return ScaledCost.CalculateCostForRank(rank);

            case PRICE_SCHEME_TYPE.SCALED_COMPOUND:
                return CompoundCost.CalculateCostForRank(rank);

            default:
                return new List<CurrencyAmount>();
        }
    }

    public void ValidateScheme()
    {
        if (_continueWhereLastTierLeftOff && _lastCostScheme != null)
        {
            if (Type == PRICE_SCHEME_TYPE.FIXED) return;

            switch (Type)
            {
                case PRICE_SCHEME_TYPE.SCALED_BASE:
                    ScaledCost.SetBasePrice(_lastCostScheme.GetFinalLastCost());
                    break;

                case PRICE_SCHEME_TYPE.SCALED_COMPOUND:
                    CompoundCost.SetBasePrice(_lastCostScheme.GetFinalLastCost());
                    break;

                default:
                    Debug.LogWarning($"[CostScheme] Unsupported type for continuation: {Type}");
                    break;
            }
        }
    }
}

