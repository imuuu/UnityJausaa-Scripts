using UnityEngine;

 public sealed class BossEncounterProximityTrigger : MonoBehaviour
{
    private ManagerBosses _manager;
    private BossEncounterDefinition _enc;
    private float _radius;

    public static BossEncounterProximityTrigger Create(ManagerBosses mgr, BossEncounterDefinition enc, Vector3 center, float radius)
    {
        GameObject go = new GameObject("BossProximityTrigger");
        go.transform.position = center;

        BossEncounterProximityTrigger t = go.AddComponent<BossEncounterProximityTrigger>();
        t._manager = mgr;
        t._enc = enc;
        t._radius = radius;
        return t;
    }


    private void Update()
    {
        if (ManagerPause.IsPaused()) return;

        if (IsPlayerCloseEnough())
        {
            _manager.OnProximityTriggered(_enc, transform.position);
            Destroy(gameObject);
        }
    }

    private bool IsPlayerCloseEnough()
    {
        if (Player.Instance == null) return false;
        float distance = Vector3.Distance(Player.Instance.transform.position, transform.position);
        return distance <= _radius;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
#endif
}
