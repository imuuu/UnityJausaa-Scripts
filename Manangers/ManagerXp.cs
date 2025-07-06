using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Game.DropSystem;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class ManagerXp : MonoBehaviour, IEnabled
{
    private bool _isEnabled = true;
    public static ManagerXp Instance { get; private set; }

    [Header("XP Settings")]
    [Tooltip("XP required for level 1")]
    [SerializeField] private float _baseXp = 100f;
    [Tooltip("Exponent to control XP curve. Increase for slower leveling later.")]
    [SerializeField] private float _exponent = 1.2f;
    [SerializeField] private XpOrbSettings[] _xpOrbSettings;

    [SerializeField, ReadOnly] private float _currentXp = 0;
    private int _currentLevel = 1;

    #region ExtraClasses

    [Serializable]
    public class XpOrbSettings
    {
        public float XpThreshold;
        public GameObject Prefab;
    }

    #endregion ExtraClasses

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        Events.OnDeath.AddListener(OnDeath);
        Events.OnPlayableSceneChange.AddListener(OnPlayableSceneChange);
    }

    private bool OnPlayableSceneChange(SCENE_NAME param)
    {
        Reset();
        return true;
    }

    private void OnDisable()
    {
        _isEnabled = false;
    }

    private void Reset()
    {
        _currentXp = 0;
        _currentLevel = 1;
    }

    private bool OnDeath(Transform transform, IHealth health)
    {
        IOwner owner = transform.GetComponent<IOwner>();

        if (owner == null || owner.GetRootOwner().GetOwnerType() == OWNER_TYPE.PLAYER) return true;

        if (transform.GetComponent<Mob>() == null) return true;

        GameObject mob = owner.GetRootOwner().GetGameObject();
        float xp = CalculateXp(mob);
        GameObject orb = GetXpOrb(xp);

        if (orb != null)
        {
            GameObject xpOrb = ManagerPrefabPooler.Instance.GetFromPool(orb);
            xpOrb.transform.position = new Vector3(
                mob.transform.position.x,
                0, 
                mob.transform.position.z);

            IDropOrb dropOrb = xpOrb.GetComponent<IDropOrb>();
            dropOrb.Init(xp);
            dropOrb.Collected += AddXp;
            ManagerDrops.Instance.Register(dropOrb);
        }

        return true;
    }
    
    public void AddXp(IDropOrb xpOrb)
    {
        AddXp(xpOrb.Amount);
    }

    public void AddXp(float xp)
    {
        _currentXp += xp;
        Events.OnPlayerXpChange.Invoke(GetXpPercent());
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (_currentXp >= GetXpForLevel(_currentLevel))
        {
            _currentXp -= GetXpForLevel(_currentLevel);
            _currentLevel++;
            OnLevelUp();
            Events.OnPlayerLevelChange.Invoke(_currentLevel);
            Events.OnPlayerXpChange.Invoke(0);
        }
    }

    // XP threshold function: XP required for current level is baseXp * level^(exponent)
    private float GetXpForLevel(int level)
    {
        return _baseXp * Mathf.Pow(level, _exponent);
    }

    private void OnLevelUp()
    {
        Debug.Log("Level Up! New Level: " + _currentLevel);
    }

    public float GetXpPercent()
    {
        return _currentXp / GetXpForLevel(_currentLevel);
    }

    public int GetCurrentLevel()
    {
        return _currentLevel;
    }

    private float CalculateXp(GameObject mob)
    {
        float xp = 0;
        float damage = mob.GetComponent<DamageDealer>().GetDamage();
        float health = mob.GetComponent<Health>().GetMaxHealth();

        xp = (damage + health) * 0.5f;
        return xp;
    }

    private GameObject GetXpOrb(float xp)
    {
        foreach (XpOrbSettings settings in _xpOrbSettings)
        {
            if (xp >= settings.XpThreshold)
            {
                return settings.Prefab;
            }
        }

        return null;
    }

    public bool IsEnabled()
    {
        return _isEnabled;
    }

    public void SetEnable(bool enable)
    {
        _isEnabled = enable;
    }
}
