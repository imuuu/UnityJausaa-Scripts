using UnityEngine;

public class Mob : MonoBehaviour
{
    private void OnEnable()
    {
        ActionScheduler.RunWhenTrue(() => ManagerMob.Instance != null, () =>
        {
            ManagerMob.Instance.RegisterEnemy(transform);
        });
    }

    private void OnDisable()
    {
        if(ManagerMob.Instance == null) return;

        ManagerMob.Instance.UnregisterEnemy(transform);
    }
}

