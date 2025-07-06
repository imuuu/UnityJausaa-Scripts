using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(IHealth))]
public class DamageReceiver : MonoBehaviour, IDamageReceiver
{
    private IHealth _healthComponent;
    [SerializeField] private DAMAGE_SOURCE[] _acceptedDamageSource;

    private void Awake() 
    {
        _healthComponent = GetComponent<IHealth>();
    }

    public void TakeDamage(IDamageDealer dealer)
    {
        DamageCalculator.CalculateDamage(dealer, this);
    }

    public DAMAGE_SOURCE[] GetAcceptedDamageSource()
    {
        return _acceptedDamageSource;
    }

    public Transform GetTransform()
    {
        return transform;
    }

#region Odin

    [PropertySpace(SpaceBefore = 20)]
    [HorizontalGroup("Buttons")]
    [Button("Take Damage(10)")]
    public void TakeDamage()
    {
        if(_acceptedDamageSource.Length == 0)
        {
            Debug.LogWarning("No accepted damage source");
            return;
        }

        SimpleDamage damage = new SimpleDamage(10, DAMAGE_TYPE.PHYSICAL, _acceptedDamageSource[0]);
        TakeDamage(damage);
    }

    [PropertySpace(SpaceBefore = 20)]
    [HorizontalGroup("Buttons")]
    [Button("Heal Full")]
    public void Heal()
    {
        _healthComponent.AddHealth(_healthComponent.GetMaxHealth());
    }

    public IHealth GetHealth()
    {
        return _healthComponent;
    }

    #endregion
}