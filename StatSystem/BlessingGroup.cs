using System.Collections.Generic;
using Game.BuffSystem;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class BlessingGroup
{
    public string GroupName;
    public Sprite GroupIcon;
    [SerializeField] private List<BlessingPrefixes> _blessingPrefixes = new();

    [MinValue(1f)] public int CurrentTier = 1;
    [Title("Upgrades with lowest tier first")]
    public BlessingUpgrade[] Blessings;

    [System.Serializable]
    public class BlessingPrefixes
    {
        public MODIFIER_TYPE ModifierType;
        [TextArea] public string Description;
        public string ValuePrefix = "+{0}%";
    }

    public BlessingUpgrade GetCurrentUpgrade()
    {
        int index = CurrentTier - 1;
        if (index < Blessings.Length)
        {
            return Blessings[index];
        }
        return null; // or throw an exception if preferred
    }

    public bool Buy()
    {
        BlessingUpgrade currentUpgrade = GetCurrentUpgrade();
        var currentCost = currentUpgrade.GetCostForCurrentRank();

        bool ableToBuy = ManagerBuffs.Instance.IsAbleToBuyBlessing(currentUpgrade);
        if (!ableToBuy)
        {
            Debug.LogWarning($"Unable to buy upgrade for {GroupName} at tier {CurrentTier}");
            return false;
        }

        bool maxedOut = false;

        int currentRank = currentUpgrade.CurrentRank;
        if (currentRank < currentUpgrade.MaxRanks - 1)
        {
            currentUpgrade.CurrentRank++;
        }
        else
        {
            currentUpgrade.CurrentRank = currentUpgrade.MaxRanks;
            CurrentTier++;
            if (CurrentTier > Blessings.Length)
            {
                CurrentTier = Blessings.Length;
                var nextUpgrade = GetCurrentUpgrade();
                nextUpgrade.CurrentRank = nextUpgrade.MaxRanks;
                maxedOut = true;
            }
            else
            {
                var nextUpgrade = GetCurrentUpgrade();
                if (nextUpgrade != null)
                {
                    nextUpgrade.CurrentRank = 0;
                }
            }

        }

        if (!maxedOut)
        {
            ManagerBuffs.Instance.BuyBlessing(currentCost);
        }
        return true;
    }

    // public bool Refund()
    // {
    //     Debug.Log($"Refunding upgrade for {GroupName} at tier {CurrentTier}");
    //     BlessingUpgrade currentUpgrade = GetCurrentUpgrade();
    //     int currentRank = currentUpgrade.CurrentRank;
    //     if (currentRank >= 0)
    //     {
    //         currentUpgrade.CurrentRank--;
    //         if (currentUpgrade.CurrentRank < 0)
    //         {
    //             currentUpgrade.CurrentRank = 0;
    //             CurrentTier--;
    //             if (CurrentTier < 1)
    //             {
    //                 CurrentTier = 1;
    //             }
    //             else
    //             {
    //                 var nextUpgrade = GetCurrentUpgrade();
    //                 nextUpgrade.CurrentRank = nextUpgrade.MaxRanks - 1;
    //                 ManagerBuffs.Instance.RefundBlessing(currentUpgrade);
    //             }

    //         }
    //     }
    //     return true;
    // }

    /// <summary>
    /// Refunds the most recent blessing purchase: reduces rank or tier and refunds via ManagerBuffs.
    /// </summary>
    public bool Refund()
    {
        Debug.Log($"Refunding upgrade for {GroupName} at tier {CurrentTier}");
        var currentUpgrade = GetCurrentUpgrade();
        if (currentUpgrade == null)
            return false;

        if (currentUpgrade.CurrentRank > 0)
        {
            currentUpgrade.CurrentRank--;
            ManagerBuffs.Instance.RefundBlessing(currentUpgrade);
        }
        else
        {
            if (CurrentTier > 1)
            {
                CurrentTier--;
                var prevUpgrade = GetCurrentUpgrade();
                if (prevUpgrade != null)
                {
                    prevUpgrade.CurrentRank = prevUpgrade.MaxRanks - 1;
                    ManagerBuffs.Instance.RefundBlessing(prevUpgrade);
                }
            }
            else
            {
                Debug.LogWarning($"Cannot refund {GroupName}, already at minimum tier and rank.");
                return false;
            }
        }

        return true;
    }

    public string GetCurrentValuePrefix()
    {
        BlessingUpgrade currentUpgrade = GetCurrentUpgrade();
        foreach (var desc in _blessingPrefixes)
        {
            if (desc.ModifierType == currentUpgrade.Modifier.GetTYPE())
            {
                return desc.ValuePrefix;
            }
        }
        return "{0}";
    }
    public string GetCurrentDescription()
    {
        BlessingUpgrade currentUpgrade = GetCurrentUpgrade();

        Modifier modifier = currentUpgrade.Modifier;
        if (modifier == null)
        {
            return "No modifier available.";
        }

        foreach (var desc in _blessingPrefixes)
        {
            if (desc.ModifierType == modifier.GetTYPE())
            {
                float value = currentUpgrade.Modifier.GetValue();
                return string.Format(desc.Description, value);
            }
        }

        return "No description " + modifier.GetTYPE() + " available.";
    }

    public Modifier GetAllProgressedModifiers()
    {
        List<Modifier> modifiers = new();
        for (int i = 0; i < CurrentTier; i++)
        {
            BlessingUpgrade upgrade = Blessings[i];
            if (upgrade != null && upgrade.CurrentRank > 0)
            {
                Modifier mod = upgrade.GetTotalModifier();
                if (mod != null)
                {
                    modifiers.Add(mod);
                }
            }
        }
        if (modifiers.Count == 0)
        {
            return null;
        }

        return Modifier.CombineModifiers(modifiers)[0];
    }

}