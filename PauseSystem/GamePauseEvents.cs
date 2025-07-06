using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
public class GamePauseEvents : MonoBehaviour
{
    [SerializeField] private UnityEvent _onPauseEvent;
    [SerializeField] private UnityEvent _onUnpauseEvent;
    private void Awake()
    {
        Events.OnGamePause.AddListener(OnPause);
        Events.OnGameUnPause.AddListener(OnUnpause);
    }

    private bool OnPause(PAUSE_REASON reason)
    {
        _onPauseEvent?.Invoke();
        return true;
    }

    private bool OnUnpause(PAUSE_REASON reason)
    {
        _onUnpauseEvent?.Invoke();
        return true;
    }
}