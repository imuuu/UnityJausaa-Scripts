using UnityEngine;
[System.Serializable]
public struct CurrencyDefinition
{
    public CURRENCY CurrencyType;
    public string DisplayName;
    public Sprite Icon;
    public GameObject Prefab;
    [Tooltip("If true, icons shows when the currency is added to player's balance.")]
    public bool EnableWorldIndicator;
    public bool ResetOnSceneChance;
    public bool SaveAble;
}