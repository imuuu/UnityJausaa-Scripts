using UnityEngine;

[System.Serializable]
public class CurrencyDropEntry : IWeightedLoot
{
    public CURRENCY currencyType;
    [SerializeField] private float _weight;
    public Vector2Int amountRange = new Vector2Int(1, 1);

    public float GetWeight() => _weight;

    public float GetAmount()
    {
        if(amountRange.x <= 0 || amountRange.y <= 0)
        {
            return 1f;
        }
        return Random.Range(amountRange.x, amountRange.y + 1);
    }
}