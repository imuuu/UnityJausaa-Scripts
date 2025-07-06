using System;
using System.Collections.Generic;

[Serializable]
public struct CurrencyAmount
{
    public CURRENCY CurrencyType;
    public int Amount;
}

[System.Serializable]
public class ListCurrencyAmount
{
    public List<CurrencyAmount> Amounts = new();
}