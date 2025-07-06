using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Currency/MobDropConfig")]
public class MobDropConfig : ScriptableObject
{
    public MOB_TYPE mobType;
    public List<CurrencyDropEntry> dropEntries;
}