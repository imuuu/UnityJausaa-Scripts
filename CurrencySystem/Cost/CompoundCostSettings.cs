using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class CompoundCostSettings
{
    public enum ROUND_TO_TYPE
    {
        N_0 = 0,
        N_10 = 10,
        N_50 = 50,
        N_100 = 100,
        N_1000 = 1000
    }

    [SerializeField, EnumToggleButtons]
    private ROUND_TO_TYPE ROUND_TYPE = ROUND_TO_TYPE.N_0;

    [Tooltip("Base price in each currency, at rank = 0")]
    [SerializeField]
    private ListCurrencyAmount _basePrice = new();

    [Tooltip("Multiplier per rank (e.g. 1.2 = +20%)")]
    [SerializeField]
    private float _multiplier = 1.2f;

    /// <summary>
    /// Iteratively multiplies & rounds each step:
    /// rank0 = base, rank1 = Round(base * m), rank2 = Round(rank1 * m), …
    /// </summary>
    public List<CurrencyAmount> CalculateCostForRank(int rank)
    {
        // Start at base
        var current = new List<CurrencyAmount>();
        foreach (var ca in _basePrice.Amounts)
            current.Add(new CurrencyAmount { CurrencyType = ca.CurrencyType, Amount = ca.Amount });

        // For each subsequent rank, multiply & round
        for (int step = 1; step <= rank; step++)
        {
            var next = new List<CurrencyAmount>();
            foreach (var prev in current)
            {
                float raw = prev.Amount * _multiplier;
                int rounded = Mathf.RoundToInt(raw);

                int divisor = (int)ROUND_TYPE;
                if (divisor > 0)
                    rounded = Mathf.RoundToInt((float)rounded / divisor) * divisor;

                next.Add(new CurrencyAmount
                {
                    CurrencyType = prev.CurrencyType,
                    Amount = rounded
                });
            }
            current = next;
        }

        return current;
    }

    public void SetBasePrice(List<CurrencyAmount> basePrice)
    {
        for (int i = 0; i < basePrice.Count; i++)
        {
            if (i < _basePrice.Amounts.Count)
            {
                _basePrice.Amounts[i] = basePrice[i];
            }
            else
            {
                _basePrice.Amounts.Add(basePrice[i]);
            }
        }
    }


#if UNITY_EDITOR
        [Title("Debug Preview")]
    [PropertyRange(1, 100)]
    [PropertySpace(20)]
    [Tooltip("How many ranks to preview when you press the button")]
    [SerializeField]
    private int _debugCount = 10;

    [Button(ButtonSizes.Large)]
    [GUIColor(0.6f, 0.8f, 1f)]
    private void PrintDebugPrices()
    {
        _debugPrices.Clear();
        for (int i = 0; i < _debugCount; i++)
        {
            var costs = CalculateCostForRank(i);
            _debugPrices.Add(new DebugRow
            {
                Rank = i,
                Cost = new ListCurrencyAmount { Amounts = new List<CurrencyAmount>(costs) }
            });
        }
    }

    [ShowInInspector, TableList(AlwaysExpanded = true)]
    [PropertyOrder(100)]
    [ReadOnly]
    private List<DebugRow> _debugPrices = new List<DebugRow>();

    [Serializable]
    private struct DebugRow
    {
        public int Rank;
        public ListCurrencyAmount Cost;
    }
#endif

}

