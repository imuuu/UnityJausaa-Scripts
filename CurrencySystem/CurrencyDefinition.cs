using UnityEngine;
[System.Serializable]
public struct CurrencyDefinition
{
    public CURRENCY CurrencyType;
    public string DisplayName;
    public Sprite Icon;
    public GameObject Prefab;
    public bool ResetOnSceneChance;
    public bool SaveAble;
}