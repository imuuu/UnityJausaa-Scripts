using UnityEngine;

public class UI_WorldIndicatorPanel : MonoBehaviour
{
    private void Start()
    {
        ActionScheduler.RunWhenTrue(() => UI_ManagerWorldIndicators.Instance != null, () =>
        {
            UI_ManagerWorldIndicators.Instance.Initialize(this);
        });
    }
}