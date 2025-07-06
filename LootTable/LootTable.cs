using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  LootTable class that allows for weighted random selection of items.
///  Higher weight means higher chance of being selected.
/// </summary>
/// <typeparam name="T"></typeparam>
public class LootTable<T> where T : IWeightedLoot
{
    private readonly List<T> items;
    private float totalWeight;

    public LootTable(IEnumerable<T> items)
    {
        this.items = new List<T>();
        totalWeight = 0f;
        foreach (T item in items)
        {
            this.items.Add(item);
            totalWeight += item.GetWeight();
        }
    }

    public T GetRandomItem()
    {
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        foreach (var item in items)
        {
            cumulativeWeight += item.GetWeight();
            if (randomValue < cumulativeWeight)
            {
                return item;
            }
        }
        return default;
    }

    public void AddItem(T item)
    {
        items.Add(item);
        totalWeight += item.GetWeight();
    }

    public void AddItems(IEnumerable<T> newItems)
    {
        foreach (T item in newItems)
        {
            AddItem(item);
        }
    }

    /// <summary>
    /// Returns the exact probability (from 0 to 1) of picking this item.
    /// </summary>
    public float GetProbabilityOf(T item)
    {
        if (!items.Contains(item))
        {
            //Debug.LogWarning($"Item {item} is not in the loot table.");
            return 0f;
        }

        return item.GetWeight() / totalWeight;
    }

    public float GetWeightOf(T item)
    {
        if (!items.Contains(item))
        {
            //Debug.LogWarning($"Item {item} is not in the loot table.");
            return 0f;
        }

        return item.GetWeight();
    }

    public List<T> GetItems()
    {
        return new List<T>(items);
    }
    
    public float GetTotalWeight()
    {
        return totalWeight;
    }
}
