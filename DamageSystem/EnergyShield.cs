using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnergyShield : MonoBehaviour, IEnergyShield
{
    [SerializeField, ReadOnly]
    private float _currentShield;

    [SerializeField]
    private float _maxShield = 50f;

    [Tooltip("Shield points per second once recharge begins.")]
    [SerializeField]
    private float _rechargeRate = 10f;

    [Tooltip("Seconds to wait after last hit before beginning recharge.")]
    [SerializeField]
    private float _rechargeDelay = 3f;

    // Internal timer counting down until recharge starts.
    private float _rechargeTimer = 0f;
    private bool _isRecharging = false;

    #region Events
    private Action _onShieldChanged;
    public Action OnShieldChanged
    {
        get => _onShieldChanged;
        set => _onShieldChanged = value;
    }

    private Action _onShieldHit;
    public Action OnShieldHit
    {
        get => _onShieldHit;
        set => _onShieldHit = value;
    }

    private Action _onRechargeStarted;
    public Action OnRechargeStarted
    {
        get => _onRechargeStarted;
        set => _onRechargeStarted = value;
    }
    #endregion

    private void Awake()
    {
        // Nothing special here unless you need other component references.
    }

    private void OnEnable()
    {
        // Fill to max whenever enabled (spawn/respawn, re‐enable, etc.).
        ResetShieldToMax();
    }

    private void Update()
    {
        if(ManagerPause.IsPaused())
        {
            return;
        }

        //Debug.Log($"EnergyShield Update: Current Shield = {_currentShield}, Max Shield = {_maxShield}, Is Recharging = {_isRecharging}, Recharge Timer = {_rechargeTimer}");
        // If shield is already full, no need to do anything.
        if (_currentShield >= _maxShield)
        {
            _currentShield = _maxShield;
            _isRecharging = false;
            _rechargeTimer = 0f;
            return;
        }

        // If we're in the delay period, count it down.
        if (_rechargeTimer > 0f)
        {
            _rechargeTimer -= Time.deltaTime;
            if (_rechargeTimer <= 0f)
            {
                // Delay has elapsed; begin actual recharge.
                _isRecharging = true;
                OnRechargeStarted?.Invoke();
            }
        }
        else if (_isRecharging)
        {
            // Perform per‐second recharge.
            float deltaAmount = _rechargeRate * Time.deltaTime;
            _currentShield = Mathf.Clamp(_currentShield + deltaAmount, 0f, _maxShield);
            OnShieldChanged?.Invoke();

            // If we've reached max, stop recharging.
            if (_currentShield >= _maxShield)
            {
                _currentShield = _maxShield;
                _isRecharging = false;
                _rechargeTimer = 0f;
            }
        }
    }

    private void ResetShieldToMax()
    {
        _currentShield = _maxShield;
        _isRecharging = false;
        _rechargeTimer = 0f;
        OnShieldChanged?.Invoke();
    }

    public void SetShield(float amount)
    {
        _currentShield = Mathf.Clamp(amount, 0f, _maxShield);
        OnShieldChanged?.Invoke();
    }

    public void SetMaxShield(float amount)
    {
        if (_maxShield == amount) return;

        _maxShield = Mathf.Max(0f, amount);
        _currentShield = Mathf.Clamp(_currentShield, 0f, _maxShield);
        StartRecharge();
        OnShieldChanged?.Invoke();
    }

    public void AddShield(float amount)
    {
        _currentShield = Mathf.Clamp(_currentShield + amount, 0f, _maxShield);
        OnShieldChanged?.Invoke();
    }

    public float GetShield()
    {
        return _currentShield;
    }

    public float GetMaxShield()
    {
        return _maxShield;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void TakeDamage(IDamageDealer dealer)
    {
        float damage = dealer.GetDamage();
        if (_currentShield <= 0f)
        {
            // No shield left to absorb.
            return;
        }

        // Subtract damage from shield.
        _currentShield -= damage;
        OnShieldHit?.Invoke();
        OnShieldChanged?.Invoke();

        // Clamp if we went negative.
        if (_currentShield <= 0f)
        {
            _currentShield = 0f;
            OnShieldChanged?.Invoke();
        }

        // Reset recharge delay.
        _rechargeTimer = _rechargeDelay;
        _isRecharging = false;
    }

    public void StartRecharge()
    {
        // Force immediate recharge: zero out delay and begin.
        _rechargeTimer = 0f;
        if (!_isRecharging && _currentShield < _maxShield)
        {
            _isRecharging = true;
            OnRechargeStarted?.Invoke();
        }
    }

    public void SetRechargeDelay(float delay)
    {
        if(_rechargeDelay == delay)
        {
            return;
        }

        _rechargeDelay = delay;
    }

    public void SetRechargeRate(float rate)
    {
        if (_rechargeRate == rate)
        {
            return;
        }

        _rechargeRate = Mathf.Max(0f, rate);
    }
}
