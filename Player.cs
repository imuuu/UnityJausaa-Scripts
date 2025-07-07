using System;
using System.Collections.Generic;
using Game.BuffSystem;
using Game.StatSystem;
using Game.UI;
using UnityEngine;

public class Player : MonoBehaviour, IStatList
{
    public static Player Instance { get; private set; }
    private ManagerGame ManagerGame => ManagerGame.Instance;
    [SerializeField] private Transform _staffTip;
    [SerializeField] private Transform _rightFoot;
    [SerializeField] private Transform _leftFoot;
    [SerializeField] private StatList _stats;
    private IHealth _health;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("There should only be one Player in the scene. Destroying the duplicate.");
            Destroy(this.gameObject);
        }

        _health = GetComponent<IHealth>();
        _health.OnDeath += Die;
    }

    private void OnEnable()
    {
        Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
        Events.OnUIPageClose.AddListener(OnUIPageClose);
    }

    private void OnDisable()
    {
        Events.OnPlayableSceneChangeEnter.RemoveListener(OnPlayableSceneChange);
        Events.OnUIPageClose.RemoveListener(OnUIPageClose);
    }

    private bool OnPlayableSceneChange(SCENE_NAME param)
    {
        RefreshStats();
        transform.position = Vector3.zero;
        return true;
    }

    private bool OnUIPageClose(PAGE_TYPE page)
    {
        if (page != PAGE_TYPE.PLAYER_UPGRADES) return true;

        RefreshStats();
        return true;
    }

    public void RefreshStats()
    {
        _stats.ClearModifiers();

        if (ManagerBuffs.Instance != null)
        {
            List<Modifier> blessingModifiers = ManagerBuffs.Instance.GetBlessings();

            _stats.AddModifiers(blessingModifiers);
        }

        ApplyStats(true);
    }

    private void Start()
    {
        ManagerGame.SetPlayer(this);
    }

    private void ApplyStats(bool initialize = false)
    {
        if (initialize)
        {
            _health.SetHealth(_stats.GetStat(STAT_TYPE.HEALTH).GetValue());
        }

        if (TryGetComponent<IMovement>(out IMovement movement))
        {
            movement.SetSpeed(_stats.GetStat(STAT_TYPE.MOVE_SPEED).GetValue());
        }

        if (TryGetComponent<IEnergyShield>(out IEnergyShield energyShield))
        {
            energyShield.SetMaxShield(_stats.GetStat(STAT_TYPE.ENERGY_SHIELD).GetValue());
            energyShield.SetRechargeRate(_stats.GetStat(STAT_TYPE.ENERGY_SHIELD_RECHARGE_RATE).GetValue());
            energyShield.SetRechargeDelay(_stats.GetStat(STAT_TYPE.ENERGY_SHIELD_START_DELAY).GetValue());
        }

        Events.OnPlayerStatsUpdated.Invoke();
    }

    public void ApplyModifiers(List<Modifier> modifiers)
    {
        float hpFlat = 0f;
        float hpIncrease = 0f;
        foreach (Modifier mod in modifiers)
        {
            if (mod.GetTarget() == STAT_TYPE.HEALTH)
            {
                if (mod.GetTYPE() == MODIFIER_TYPE.FLAT)
                {
                    hpFlat += mod.GetValue();
                }
                else if (mod.GetTYPE() == MODIFIER_TYPE.INCREASE)
                {
                    hpIncrease += mod.GetValue();
                }
            }
            _stats.AddModifier(mod);
        }

        _health.SetMaxHealth(_stats.GetStat(STAT_TYPE.HEALTH).GetValue());

        if (hpIncrease > 0)
        {
            float hpIncreaseAdd = _health.GetMaxHealth() * (hpIncrease / 100f);

            _health.AddHealth(hpIncreaseAdd);
        }
        if (hpFlat > 0)
        {
            _health.AddHealth(hpFlat);
        }

        ApplyStats();
    }

    public void Die()
    {
        Debug.Log("Player died");
        Events.OnPlayerDeath.Invoke();

        ManagerUI.Instance.OpenPage(PAGE_TYPE.DIE_MENU);
    }

    public Transform GetStaffTip()
    {
        return _staffTip;
    }

    public Transform GetRightFoot()
    {
        return _rightFoot;
    }
    public Transform GetLeftFoot()
    {
        return _leftFoot;
    }

    /// <summary>
    /// Assigns the player's transform to the given action when it becomes available.
    /// Please use it START Method instead of Awake!
    /// </summary>
    public static void AssignTransformWhenAvailable(Action<Transform> onTransformAvailable)
    {
        ActionScheduler.RunWhenTrue(() => Player.Instance != null, () =>
        {
            onTransformAvailable?.Invoke(Player.Instance.transform);
        });
    }

    public StatList GetStatList()
    {
        return _stats;
    }

    public float GetStatValue(STAT_TYPE statType)
    {
        return _stats.GetStat(statType).GetValue();
    }

}
