using Sirenix.OdinInspector;
using UnityEngine;

public class UI_HealthBar : UI_ProgressBar
{
    [Title("Health Bar Settings")]
    [Tooltip("Find IHealh component self or parents")]
    [SerializeField] private bool _findHealthComponent = true;
    [SerializeField] private bool _displayIfDamaged = false;
    private IHealth _health;

    protected override void Awake()
    {
        if (_findHealthComponent)
        {
            _health = FindHealthComponent(gameObject);

            if (_health != null)
            {
                _health.OnHealthChanged += OnHealthChanged;
                OnHealthChanged();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        UpdateTextFormat();
    }

    private void UpdateIHealth(IHealth health)
    {
        if (_displayIfDamaged && health.GetHealth() >= health.GetMaxHealth())
        {
            gameObject.SetActive(false);
            return;
        }
        // gameObject.SetActive(true);

        Percent = health.GetHealth() / health.GetMaxHealth();
        UpdateProgressVisuals();
    }

    private void OnHealthChanged()
    {
        UpdateIHealth(_health);
    }

}