using System.Collections.Generic;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class UI_ManagerWorldIndicators : MonoBehaviour
{
    public static UI_ManagerWorldIndicators Instance { get; private set; }

    [SerializeField] private bool _enable = true;
    [SerializeField] private List<IndicatorConfig> _configs;

    private Dictionary<INDICATOR_TYPE, IndicatorConfig> _configMap;

    private UI_WorldIndicatorPanel _worldIndicatorPanel;

    private bool _initialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

    }

    public void Initialize(UI_WorldIndicatorPanel panel)
    {
        if (_initialized)
        {
            Debug.LogWarning("UI_ManagerWorldIndicators is already initialized.");
            return;
        }
        _initialized = true;

        _worldIndicatorPanel = panel;

        _configMap = new Dictionary<INDICATOR_TYPE, IndicatorConfig>();

        foreach (IndicatorConfig cfg in _configs)
        {
            if (cfg.Prefab == null) continue;
            _configMap[cfg.Type] = cfg;

            PoolOptions poolOption = ManagerPrefabPooler.Instance.CreateDefaultPoolOption();

            poolOption.ReturnType = POOL_RETURN_TYPE.TIMED;
            poolOption.ReturnDelay = cfg.Duration;

            // GameObject customPoolHolder = new GameObject(cfg.Type.ToString());
            // customPoolHolder.transform.SetParent(panel.transform);
            // customPoolHolder.transform.localPosition = Vector3.zero;
            // customPoolHolder.transform.localRotation = Quaternion.identity;
            // customPoolHolder.transform.localScale = Vector3.one;

            GameObject prefab = cfg.Prefab;
            ManagerPrefabPooler.Instance.CreatePrefabPool(prefab, poolOption);
        }
    }

    /// <summary>
    /// Spawn or recycle a prefab for this indicator type, configure it, and let it float.
    /// </summary>
    public void ShowIndicator(Vector3 worldPosition, INDICATOR_TYPE type, float amount, Sprite iconSprite = null)
    {
        if(!_initialized)
        {
            return;
        }

        if (!_enable || !_configMap.ContainsKey(type))
            return;

        IndicatorConfig cfg = _configMap[type];
        GameObject go = ManagerPrefabPooler.Instance.GetFromPool(cfg.Prefab);
        if (go == null) return;

        UI_WorldIndicator worldIndicator = go.GetComponent<UI_WorldIndicator>();

        if (cfg.RandomizeOffset)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(cfg.MinRandomOffsetRange.x, cfg.MaxRandomOffsetRange.x),
                Random.Range(cfg.MinRandomOffsetRange.y, cfg.MaxRandomOffsetRange.y),
                Random.Range(cfg.MinRandomOffsetRange.z, cfg.MaxRandomOffsetRange.z)
            );
            worldPosition += randomOffset;
        }

        Vector3 targetWorldPos = worldPosition + cfg.WorldOffset;

        go.transform.position = targetWorldPos;

        Sprite icon = iconSprite ?? cfg.IconSprite;

        worldIndicator.Initialize(amount, icon, cfg.Duration);

        if (cfg.EnableTextColor && !cfg.EnableTextColorGradient) worldIndicator.SetTextColor(cfg.TextColor);

        if (cfg.EnableTextColorGradient) worldIndicator.SetColorGradient(cfg.TextColorGradient);
        

    }

    [Button]
    public void TEST_INDICATOR()
    {
        Transform player = Player.Instance?.transform;

        if (player == null || !_initialized)
        {
            Debug.LogWarning("Player transform is null or UI_ManagerWorldIndicators is not initialized.");
            return;
        }

        ShowIndicator(player.position, INDICATOR_TYPE.DAMAGE, 100f);
    }

    public void CreateFloatingDamage(Transform targetTransform, INDICATOR_TYPE indicatorType, float damage)
    {
        if (!_enable) return;

        // IndicatorConfig cfg = _configMap[indicatorType];
        // GameObject floatingDamage = ManagerPrefabPooler.Instance.GetFromPool(cfg.Prefab);

        Vector3 worldPosition = targetTransform.position;
        if (targetTransform.TryGetComponent<IStatistics>(out IStatistics statistics))
        {
            worldPosition = statistics.HeadPosition;
        }

        ShowIndicator(worldPosition, indicatorType, damage);
    }
}
