using System;
using System.Collections.Generic;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBlessingGrouper", menuName = "Game/Upgrade/BlessingGrouper")]
public partial class BlessingUpgradeGrouper : ScriptableObject
{
    [ShowInInspector] public static string TierPrefix = "Tier {0}";
    [Header("Blessing Groups")]
    public BlessingGroup[] Groups;

    private List<Modifier> _cachedModifiers;

    [SerializeField] private bool isDirty = true;

    [Button("Reset All Blessings")]
    [GUIColor(0.8f, 0.2f, 0.2f)]
    [PropertyOrder(100)]
    [PropertySpace(10)]
    public void ResetAllBlessings()
    {
        foreach (var group in Groups)
        {
            group.CurrentTier = 1;
            foreach (var blessing in group.Blessings)
            {
                blessing.CurrentRank = 0;
            }
        }
        MarkDirty();
    }
    
    public void MarkDirty()
    {
        isDirty = true;
        _cachedModifiers = null;
    }
    
    public List<Modifier> GetAllModifiers()
    {
        if (_cachedModifiers != null && !isDirty)
        {
            return _cachedModifiers;
        }

        List<Modifier> modifiers = new();
        foreach (var group in Groups)
        {
            Modifier combinedModifier = group.GetAllProgressedModifiers();
            if (combinedModifier != null)
            {
                modifiers.Add(combinedModifier);
            }
        }

        _cachedModifiers = modifiers;
        return _cachedModifiers;
    }
}