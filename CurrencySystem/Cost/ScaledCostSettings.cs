using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ScaledCostSettings
{
    public enum ROUND_TO_TYPE
    {
        N_0 = 0,
        N_10 = 10,
        N_50 = 50,
        N_100 = 100,
        N_1000 = 1000
    }

    [SerializeField, EnumToggleButtons] private ROUND_TO_TYPE ROUND_TYPE = 0;
    [Tooltip("Base price in each currency, at rank = 0")]
    [SerializeField] private ListCurrencyAmount _basePrice = new();

    [Tooltip("Multiplier per rank (e.g. 1.2 = +20%)")]
    [SerializeField] private float _multiplier = 1.2f;

    public List<CurrencyAmount> CalculateCostForRank(int rank)
    {
        var result = new List<CurrencyAmount>();
        foreach (var ca in _basePrice.Amounts)
        {
            float raw = ca.Amount * Mathf.Pow(_multiplier, rank);
            int rounded = Mathf.RoundToInt(raw);
            if ((int)ROUND_TYPE > 0)
                rounded = Mathf.RoundToInt((float)rounded / (float)ROUND_TYPE) * (int)ROUND_TYPE;

            result.Add(new CurrencyAmount
            {
                CurrencyType = ca.CurrencyType,
                Amount = rounded
            });
        }
        return result;
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
    [PropertyRange(1, 100)][PropertySpace(20)]
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

    [ShowInInspector, TableList(AlwaysExpanded = true)][PropertyOrder(100)]
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

   



