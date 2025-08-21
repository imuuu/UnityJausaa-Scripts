

using System.Collections.Generic;
using UnityEngine;
using Game.Mobs;
using Sirenix.OdinInspector;
using System.Linq;
using Game.DropSystem;

public class ManagerCurrency : MonoBehaviour
{
    public static ManagerCurrency Instance { get; private set; }

    [Header("Currency Definitions")]
    [SerializeField] private CurrencyLibrary _currencyLibrary;

    [Header("Default Mob Drops")]
    [Tooltip("If a MobData has no DropEntries, this table will be used instead")]
    [SerializeField] private List<CurrencyDropEntry> _defaultMobDropEntries = new();

    // [Header("Chest Drop Configurations")]
    // [SerializeField] private List<CurrencyInfo> _chestDropConfigs = new();

    [ShowInInspector, ReadOnly] private Dictionary<CURRENCY, int> _balances; // todo chance it to a List<CurrencyAmount>?
    private Dictionary<string, LootTable<CurrencyDropEntry>> _chestDropTables;

    private LootTable<CurrencyDropEntry> _lootTableDefaultMobDrops;
    private Dictionary<MOB_TYPE, LootTable<CurrencyDropEntry>> _mobDropTables;

    // [System.Serializable]
    // public class CurrencyInfo : IWeightedLoot
    // {
    //     public CURRENCY _type;
    //     public float _weight;

    //     public float GetWeight()
    //     {
    //         return _weight;
    //     }
    // }

    private const string ES3KeyPrefix = "Currency_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();

        LoadAllBalances();
        Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
    }

    private void OnApplicationQuit()
    {
        SaveAllBalances();
    }

    private bool OnPlayableSceneChange(SCENE_NAME param)
    {
        if (_balances != null)
        {
            List<CURRENCY> chanceToReset = _currencyLibrary.Definitions
                .Where(def => def.ResetOnSceneChance)
                .Select(def => def.CurrencyType)
                .ToList();

            foreach (CURRENCY type in chanceToReset)
            {
                if (_balances.ContainsKey(type))
                {
                    _balances[type] = 0;
                    SetBalance(type, 0);
                }
            }
            // foreach (var balance in _balances)
            // {
            //     CURRENCY type = balance.Key;
            //     bool isResetSceneChange = _currencyLibrary.GetCurrencyDefinition(type).ResetOnSceneChance;
            //     if (isResetSceneChange)
            //     {
            //         balance.Value = 0;
            //     }
            // }
        }

        return true;
    }

    private void Start()
    {
        _currencyLibrary.CreatePrefabPools();
    }

    private void OnEnable()
    {
        Events.OnDeath.AddListener(OnMobKilled);
    }

    private void OnDisable()
    {
        Events.OnDeath.RemoveListener(OnMobKilled);
    }

    private void Initialize()
    {
        _balances = new Dictionary<CURRENCY, int>();
        foreach (CurrencyDefinition def in _currencyLibrary.Definitions)
            _balances[def.CurrencyType] = 0;


        _lootTableDefaultMobDrops = new(_defaultMobDropEntries);
    }


    /// <summary>
    /// Saves all "SaveAble" currency balances via Easy Save.
    /// </summary>
    public void SaveAllBalances()
    {
        foreach (CurrencyDefinition def in _currencyLibrary.Definitions)
        {
            SaveBalance(def.CurrencyType);
        }
    }

    public void SaveBalance(CURRENCY type)
    {
        if (_currencyLibrary.GetCurrencyDefinition(type).SaveAble)
        {
            string key = ES3KeyPrefix + type;
            ES3.Save<int>(key, GetBalance(type));
        }
    }

    /// <summary>
    /// Loads all "SaveAble" currency balances via Easy Save.
    /// </summary>
    public void LoadAllBalances()
    {
        foreach (CurrencyDefinition def in _currencyLibrary.Definitions)
        {
            if (def.SaveAble)
            {
                string key = ES3KeyPrefix + def.CurrencyType;
                int loaded = ES3.KeyExists(key)
                    ? ES3.Load<int>(key)
                    : 0;
                _balances[def.CurrencyType] = loaded;
                Events.OnCurrencyBalanceChange.Invoke(def.CurrencyType, loaded);
            }
        }
    }

    /// <summary>
    /// Call when a mob is killed.
    /// Rolls CurrencyDropChance; if it passes, picks from either the mob’s own table or the default.
    /// </summary>
    public bool OnMobKilled(Transform trans, IHealth health)
    {
        MobDataController mob = trans.GetComponent<MobDataController>();
        if (mob == null) return true;

        AddCurrency(CURRENCY.KILLS, 1);

        MobData mobData = ManagerMob.Instance.GetMobLibrary().GetMobDataByType(mob.GetMobData().MobType);

        if (mobData == null) return true;

        CurrencyDropEntry info = _lootTableDefaultMobDrops.GetRandomItem();

        if (mobData.CurrencyDrops.Count > 0)
        {
            info = GetMobDropTable(mobData.MobType).GetRandomItem();
        }

        if (info.currencyType == CURRENCY.NONE) return true;

        GameObject orbGO = _currencyLibrary.GetPooledCurrencyPrefab(info.currencyType);

        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 1;

        float yPosition = trans.position.y;
        const int maxGroundHeight = 1;
        if (yPosition > maxGroundHeight)
        {
            RaycastHit hit;
            LayerMask layerMask = LayerMask.GetMask("Ground");
            if (Physics.Raycast(trans.position, Vector3.down, out hit, 50, layerMask))
            {
                yPosition = hit.point.y;
            }
        }

        if(yPosition < 0) yPosition = 0;

        orbGO.transform.position = new Vector3(trans.position.x + randomOffset.x, trans.position.y, trans.position.z + randomOffset.z);

        CurrencyOrb orb = orbGO.GetOrAdd<CurrencyOrb>();
        orb.Init(1, info.currencyType);

        orb.Collected += (collectedOrb) =>
        {
            if (collectedOrb is CurrencyOrb currencyOrb)
            {
                AddCurrency(currencyOrb.CurrencyType, (int)currencyOrb.Amount);
            }
        };

        ManagerDrops.Instance.Register(orb);

        return true;
    }

    /// <summary>
    /// Call when a chest is opened.
    /// </summary>
    public void OnChestOpened(string chestName)
    {
        if (!_chestDropTables.TryGetValue(chestName, out LootTable<CurrencyDropEntry> table))
            return;

        CurrencyDropEntry entry = table.GetRandomItem();
        int amount = UnityEngine.Random.Range(entry.amountRange.x, entry.amountRange.y + 1);
        AddCurrency(entry.currencyType, amount);
    }

    private void ShowWorldIndicator(CURRENCY type, int amount)
    {
        if (Player.Instance == null) return;

        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

        CurrencyDefinition definition = _currencyLibrary.GetCurrencyDefinition(type);

        if (!definition.EnableWorldIndicator) return;

        Sprite sprite = definition.Icon;
        Vector3 worldPosition = Player.Instance.transform.position;
        UI_ManagerWorldIndicators.Instance.ShowIndicator(worldPosition, INDICATOR_TYPE.CURRENCY_ALL, amount, sprite);
    }

    // ─── Balance Management ────────────────────────────────────────────────

    public void AddCurrency(CURRENCY type, int amount)
    {
        if (!_balances.ContainsKey(type)) return;
        _balances[type] += amount;
        //Debug.Log($"[Currency] Added {amount} {type}. New balance: {_balances[type]}");

        Events.OnCurrencyAdded.Invoke(type, amount);
        Events.OnCurrencyBalanceChange.Invoke(type, _balances[type]);

        ShowWorldIndicator(type, amount);
        SaveBalance(type);
    }

    public void AddCurrency(List<CurrencyAmount> amounts)
    {
        foreach (CurrencyAmount amount in amounts)
        {
            AddCurrency(amount.CurrencyType, amount.Amount);
        }
    }

    public bool TrySpendCurrency(CURRENCY type, int amount)
    {
        if (_balances.TryGetValue(type, out int bal) && bal >= amount)
        {
            _balances[type] = bal - amount;
            Debug.Log($"[Currency] Spent {amount} {type}. New balance: {_balances[type]}");
            Events.OnCurrencyBalanceChange.Invoke(type, _balances[type]);
            SaveBalance(type);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to spend multiple currency amounts.
    /// If any amount cannot be spent, rolls back all changes.
    /// </summary>
    public bool TrySpendCurrency(List<CurrencyAmount> amounts)
    {
        foreach (CurrencyAmount amount in amounts)
        {
            if (!TrySpendCurrency(amount.CurrencyType, amount.Amount))
            {
                foreach (CurrencyAmount rollbackAmount in amounts)
                {
                    AddCurrency(rollbackAmount.CurrencyType, rollbackAmount.Amount);
                }
                return false;
            }
        }
        return true;
    }


    [Button("Set Currency", ButtonSizes.Large)]
    [GUIColor(0.6f, 0.8f, 1f)]
    [PropertySpace(20)]
    public void SetBalance(CURRENCY type, int amount)
    {
        if (!_balances.ContainsKey(type))
        {
            _balances.Add(type, 0);
        }

        _balances[type] = amount;
        Events.OnCurrencyBalanceChange.Invoke(type, amount);
        SaveBalance(type);
    }

    public List<CurrencyAmount> GetAllBalances()
    {
        List<CurrencyAmount> balances = new List<CurrencyAmount>();
        foreach (KeyValuePair<CURRENCY, int> kvp in _balances)
        {
            if (kvp.Value <= 0) continue; // Skip zero balances
            CurrencyAmount currencyAmount = new CurrencyAmount
            {
                CurrencyType = kvp.Key,
                Amount = kvp.Value
            };
            balances.Add(currencyAmount);
        }
        return balances;
    }
    public int GetBalance(CURRENCY type)
    {
        return _balances.TryGetValue(type, out int bal) ? bal : 0;
    }

    public Sprite GetIcon(CURRENCY currencyType)
    {
        return _currencyLibrary.GetCurrencyDefinition(currencyType).Icon;
    }

    public CurrencyDefinition GetCurrencyDefinition(CURRENCY type)
    {
        return _currencyLibrary.GetCurrencyDefinition(type);
    }

    public LootTable<CurrencyDropEntry> GetMobDropTable(MOB_TYPE mobType)
    {
        if (_mobDropTables == null)
        {
            _mobDropTables = new Dictionary<MOB_TYPE, LootTable<CurrencyDropEntry>>();
        }

        if (!_mobDropTables.TryGetValue(mobType, out LootTable<CurrencyDropEntry> table))
        {
            MobData mobData = ManagerMob.Instance.GetMobLibrary().GetMobDataByType(mobType);
            if (mobData != null)
            {
                table = new LootTable<CurrencyDropEntry>(mobData.CurrencyDrops);
                _mobDropTables[mobType] = table;
            }
        }

        return table;
    }


}
