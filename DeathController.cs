using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(IHealth))]
public class DeathController : MonoBehaviour
{
    [InfoBox("This script is related to HEALTH if it goes to 0")]
    [SerializeField] private bool _returnPoolIfPossible = true;

    [SerializeField, HideIf(nameof(_returnPoolIfPossible))]
    private bool _enableDeathEffectOption = false;
    [SerializeField, ShowIf(nameof(_enableDeathEffectOption))]
    private DeathEffectOptions _deathEffectOptions;

    [ToggleLeft]
    [SerializeField] private bool _enableEvents = false;

    [ShowIf("_enableEvents")]
    [SerializeField] private UnityEvent _onDeathEvent;
    [ShowIf("_enableEvents")]
    [SerializeField] private UnityEvent _onDisableEvent;
    private IHealth _health;

    private void Awake()
    {
        _health = GetComponent<IHealth>();
    }

    private void OnEnable()
    {
        _health.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= OnDeath;
        if(_enableEvents) _onDisableEvent?.Invoke();
    }


    private void OnDeath()
    {
        //Debug.Log("OnDeath");
        if(_enableEvents) _onDeathEvent?.Invoke();

        if(GetComponent<PoolHealthDetection>() != null) return;

        if(_returnPoolIfPossible && ManagerPrefabPooler.Instance.ReturnToPool(gameObject))
        {
            return;
        }

        if (_enableDeathEffectOption && _deathEffectOptions != null)
        {
            ManagerPrefabPooler.Instance.SpawnDeathEffect(this.gameObject, _deathEffectOptions);
        }

        gameObject.SetActive(false);

        ActionScheduler.RunAfterDelay(1f, () =>
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        });
    }

}