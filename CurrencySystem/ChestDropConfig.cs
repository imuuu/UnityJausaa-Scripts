using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Currency/ChestDropConfig")]
public class ChestDropConfig : ScriptableObject
{
    public string chestName;
    public List<CurrencyDropEntry> dropEntries;
}