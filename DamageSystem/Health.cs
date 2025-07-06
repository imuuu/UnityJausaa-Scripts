using System;
using Game.Mobs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IHealth
{
    [SerializeField, ReadOnly] private float _currentHealth;
    [SerializeField] private float _maxHealth = 100f;
     [SerializeField] private UnityEvent _onDamageTakenEvent;

    private bool _hasMobDataController = false;
    private bool _isDead = false;

    private IEnergyShield _energyShield;

    #region Events
    private Action _onHealthChanged;
    public Action OnHealthChanged
    {
        get => _onHealthChanged;
        set => _onHealthChanged = value;
    }

    private Action _onDamageTaken;
    public Action OnDamageTaken 
    { 
        get => _onDamageTaken; 
        set => _onDamageTaken = value; 
    }

    private Action _onDeath;
    public Action OnDeath
    {
        get => _onDeath;
        set => _onDeath = value;
    }
    #endregion Events

    private void Awake()
    {
        _hasMobDataController = GetComponent<MobDataController>() != null;
        _energyShield = GetComponent<IEnergyShield>();
    }

    private void OnEnable() 
    {
        if(_hasMobDataController) return;

        SetHealth(_maxHealth);
    }

    public void SetHealth(float amount)
    {
        _currentHealth = amount;
        _maxHealth = amount;
        _isDead = false;
        OnHealthChanged?.Invoke();
    }

    public void SetMaxHealth(float amount)
    {
        _maxHealth = amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void AddHealth(float amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        OnHealthChanged?.Invoke();
        if (_currentHealth > 0f && _isDead)
        {
            _isDead = false;
        }
    }

    public float GetHealth()
    {
        return _currentHealth;
    }

    public float GetMaxHealth()
    {
        return _maxHealth;
    }

    public void PlayerTakesDamage()
    {
        //Events.OnPlayerHit.Invoke();
    }

    public void TakeDamage(IDamageDealer dealer)
    {
        if (_isDead) return;

        float damage = dealer.GetDamage();

        if (_energyShield != null && _energyShield.GetShield() > 0f)
        {
            float shieldBefore = _energyShield.GetShield();

            _energyShield.TakeDamage(dealer);

            if (damage > shieldBefore)
            {
                float leftover = damage - shieldBefore;
                _currentHealth -= leftover;
                OnDamageTaken?.Invoke();
                _onDamageTakenEvent?.Invoke();
                OnHealthChanged?.Invoke();

                if (_currentHealth <= 0f)
                {
                    if (!Events.OnDeath.Invoke(this.transform, this))
                    {
                        _currentHealth = 1f;
                        return;
                    }
                    Die();
                }
            }
            return;
        }

        _currentHealth -= damage;
        OnDamageTaken?.Invoke();
        _onDamageTakenEvent?.Invoke();
        OnHealthChanged?.Invoke();

        if (_currentHealth <= 0f)
        {
            if (!Events.OnDeath.Invoke(this.transform, this))
            {
                _currentHealth = 1f;
                return;
            }
            Die();
        }
    }

    public void Die()
    {
        if (_isDead) return;
        //Debug.Log("Object has died");
        _currentHealth = 0;
        _isDead = true;
        OnDeath?.Invoke();
    }

    public Transform GetTransform()
    {
        return transform;
    }
}