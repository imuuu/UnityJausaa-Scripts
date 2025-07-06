using Sirenix.OdinInspector;
using UnityEngine;

public class UI_EnergyShieldBar : UI_ProgressBar
{
    [Title("Energy Shield Bar Settings")]
    [Tooltip("Find IEnergyShield component self or in parents")]
    [SerializeField] private bool _findShieldComponent = true;
    [SerializeField] private bool _displayIfFull = false;

    private IEnergyShield _shield;

    protected override void Awake()
    {
        if (_findShieldComponent)
        {
            _shield = FindShieldComponent(gameObject);

            if (_shield != null)
            {
                _shield.OnShieldChanged += OnShieldChanged;
                OnShieldChanged();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        UpdateTextFormat();
    }

    private IEnergyShield FindShieldComponent(GameObject go)
    {
        return go.GetComponentInParent<IEnergyShield>();
    }

    private void UpdateIShield(IEnergyShield shield)
    {
        if (_displayIfFull && shield.GetShield() >= shield.GetMaxShield())
        {
            gameObject.SetActive(false);
            return;
        }

        // gameObject.SetActive(true);

       
        Percent = shield.GetShield() <= 0 ? 0f : shield.GetShield() / shield.GetMaxShield();
        UpdateProgressVisuals();
    }

    private void OnShieldChanged()
    {
        UpdateIShield(_shield);
    }
}
