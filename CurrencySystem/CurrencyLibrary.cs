using UnityEngine;
using System.Collections.Generic;
using Game.PoolSystem;

[CreateAssetMenu(menuName = "Currency/CurrencyLibrary")]
public partial class CurrencyLibrary : ScriptableObject
{
    public List<CurrencyDefinition> Definitions;

    public GameObject GetCurrencyPrefab(CURRENCY type)
    {
        foreach (var def in Definitions)
        {
            if (def.CurrencyType == type)
            {
                return def.Prefab;
            }
        }
        Debug.LogWarning($"Currency prefab not found for type: {type}");
        return null;
    }
    
    public GameObject GetPooledCurrencyPrefab(CURRENCY type)
    {
        GameObject prefab = GetCurrencyPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab found for currency type: {type}");
            return null;
        }

        return ManagerPrefabPooler.Instance.GetFromPool(prefab);
    }

    public void CreatePrefabPools()
    {
        if (ManagerPrefabPooler.Instance == null)
        {
            Debug.LogError("ManagerPrefabPooler instance is not initialized.");
            return;
        }

        foreach (var def in Definitions)
        {
            if (def.Prefab != null)
            {
                ManagerPrefabPooler.Instance.CreatePrefabPool(def.Prefab, GetPoolOptions());
            }
        }
    }

    public CurrencyDefinition GetCurrencyDefinition(CURRENCY type)
    {
        foreach (var def in Definitions)
        {
            if (def.CurrencyType == type)
            {
                return def;
            }
        }
        Debug.LogWarning($"Currency definition not found for type: {type}");
        return default;
    }

    private PoolOptions GetPoolOptions()
    {
        PoolOptions poolOptions = new PoolOptions()
        {
            Min = 10,
            Max = 0,
            PoolType = POOL_TYPE.DYNAMIC,
            ReturnType = POOL_RETURN_TYPE.MANUAL,
            EventTriggerType = POOL_EVENT_TRIGGER_TYPE.NONE,
            IsLifeTimeDeathEffect = false,
            LifeTimeDuration = 0f
        };

        return poolOptions;
    }
}
