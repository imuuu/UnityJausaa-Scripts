using UnityEngine;

public sealed class BoundaryEnforcer : MonoBehaviour
{
    [SerializeField] private Transform _player;

    private void Update()
    {
        var mgr = ManagerBosses.Instance; if (mgr == null || _player == null) return;
        bool inside = mgr.IsInsideArena(_player.position);
        if (!inside)
        {
            // Choose one rule; example Pushback toward center
            Vector3 center = mgr.transform.position; // or arena center expose
            Vector3 dir = (center - _player.position); dir.y = 0f; float len = dir.magnitude + 1e-6f; dir /= len;
            _player.position += dir * (10f * Time.deltaTime);
            // You can add DoT rule here via your Stat/Hit systems
        }
    }
}