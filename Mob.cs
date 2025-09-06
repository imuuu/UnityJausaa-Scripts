using UnityEngine;

public class Mob : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        ActionScheduler.RunWhenTrue(() => ManagerMob.Instance != null, () =>
        {
            ManagerMob.Instance.RegisterEnemy(transform);
        });
    }

    protected virtual void OnDisable()
    {
        if(ManagerMob.Instance == null) return;

        ManagerMob.Instance.UnregisterEnemy(transform);
    }
}

