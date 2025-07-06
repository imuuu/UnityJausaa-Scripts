using Sirenix.OdinInspector;
using UnityEngine;

// <summary>
// Applies fall damage based on the height fallen when colliding with ground layers.
// activated normally in side mob state machine.
// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FallDamageOnCollision : MonoBehaviour
{
    [Header("Fall Damage Settings")]
    [Tooltip("Minimum fall distance (units) before any damage is applied.")]
    [SerializeField] private float _safeFallHeight = 0.5f;
    [Tooltip("Damage to apply per unit above the safe height.")]
    [SerializeField, HideIf(nameof(_enableDamagePercentHP))] private float _damagePerUnit = 5f;
    [SerializeField] private bool _enableDamagePercentHP = false;
    [Tooltip("If enabled, damage will be a percentage of the receiver's max HP instead of a flat value.")]
    [ShowIf(nameof(_enableDamagePercentHP))]
    [SerializeField, Range(0f, 100f)] private float _damagePercentHP = 1f;
    [Tooltip("Which layers count as 'ground' for landing.")]
    [SerializeField] private LayerMask groundLayers;

    private float _fallStartY;
    private float _maxHeightY;
    private IDamageReceiver _damageReceiver;
    private IHealth _health;

    private const float MIN_DAMAGE_THRESHOLD = 5f;
    private SimpleDamage _simpleDamage;

    private void Awake()
    {
        _simpleDamage = new SimpleDamage(0f, DAMAGE_TYPE.PHYSICAL, DAMAGE_SOURCE.FALL);
        _damageReceiver = GetComponent<IDamageReceiver>();
        if (_damageReceiver == null)
            Debug.LogWarning($"[{nameof(FallDamageOnCollision)}] No IDamageReceiver found; damage will be logged but not applied.");

        if (!_enableDamagePercentHP) return;

        _health = GetComponent<IHealth>();
        if (_health == null)
            Debug.LogWarning($"[{nameof(FallDamageOnCollision)}] No IHealth found; damage percent HP will not be applied.");
    }

    private void OnEnable()
    {
        _fallStartY = 0f;
        _maxHeightY = _fallStartY;
    }

    private void Update()
    {
        float currentY = transform.position.y;
        if (currentY > _maxHeightY)
            _maxHeightY = currentY;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((groundLayers.value & (1 << collision.gameObject.layer)) == 0)
            return;

        float landedY = transform.position.y;
        float fallDistance = _maxHeightY - landedY;

        if (fallDistance > _safeFallHeight)
        {
            float excess = fallDistance - _safeFallHeight;
            float dmgValue = excess * (_enableDamagePercentHP ? 
                (_health != null ? _health.GetMaxHealth() * (_damagePercentHP / 100f) : 0f) : 
                _damagePerUnit);

            if (dmgValue < MIN_DAMAGE_THRESHOLD) return;

            _simpleDamage.SetDamage(dmgValue);

            if (_damageReceiver != null)
            {
                DamageCalculator.CalculateDamage(_simpleDamage, _damageReceiver);
            }

            enabled = false;
        }

    }
}
