
using System.Collections.Generic;
using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "StatSystemIcons", menuName = "StatSystemIcons", order = 0)]
public class StatSystemIcons : ScriptableObject
{
    [System.Serializable]
    public class StatTypeIcon
    {
        public STAT_TYPE StatType;
        public Sprite Icon;
    }

    public Sprite PlayerBuffIcon;

    [InfoBox("Atm not used, but can be used in the future for stat type icons in the UI.")]
    [Space(15)] public List<StatTypeIcon> StatTypeIconsList;

    public Sprite GetIcon(STAT_TYPE statType)
    {
        foreach (var statTypeIcon in StatTypeIconsList)
        {
            if (statTypeIcon.StatType == statType)
            {
                return statTypeIcon.Icon;
            }
        }
        Debug.LogWarning($"No icon found for stat type: {statType}");
        return null;
    }
}