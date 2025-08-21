using System;
using Game.PoolSystem;
using UnityEngine;
[Obsolete]
public class ManagerFloatingDamages : MonoBehaviour
{
    public static ManagerFloatingDamages Instance { get; private set; }

    [SerializeField] private bool _enable = true;

    [SerializeField] private GameObject _floatingDamagePrefab;
    //[SerializeField] private float _offsetY = 1f;

    private void Awake()
    {
        if (Instance == null)Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PoolOptions poolOptions = ManagerPrefabPooler.Instance.GetPoolOptions(_floatingDamagePrefab);

        if(poolOptions == null)
        {
            Debug.LogError("Floating Damage Prefab is not in the pool system");
            return;
        }

        UI_FloatingDamageNumber floatingDamageNumber = _floatingDamagePrefab.GetComponent<UI_FloatingDamageNumber>();
        poolOptions.ReturnType = POOL_RETURN_TYPE.TIMED;
        poolOptions.ReturnDelay = floatingDamageNumber.GetDuration();

    }

    public void CreateFloatingDamage(Transform targetTransform, float damage)
    {
        if (!_enable) return;

        // GameObject floatingDamage = ManagerPrefabPooler.Instance.GetFromPool(_floatingDamagePrefab);

        // if(targetTransform.TryGetComponent<IStatistics>(out IStatistics statistics))
        // {
        //     floatingDamage.transform.position = statistics.HeadPosition +Vector3.up*_offsetY;
        // }else
        //     floatingDamage.transform.position = targetTransform.position + Vector3.up * _offsetY;

        // UI_FloatingDamageNumber floatingDamageNumber = floatingDamage.GetComponent<UI_FloatingDamageNumber>();
        // //Debug.Log($"FFFFFFFloating Damage Number: {damage}");
        // floatingDamageNumber.ShowDamage(damage);
        
        UI_ManagerWorldIndicators.Instance.CreateFloatingDamage(targetTransform, INDICATOR_TYPE.DAMAGE, damage);

    }
}