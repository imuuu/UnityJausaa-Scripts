using System.Collections.Generic;
using Game.UI;
using Sirenix.OdinInspector;
using UnityEngine;

public class ManagerPause : MonoBehaviour
{
    public static ManagerPause Instance { get; private set; }
    private static HashSet<PAUSE_REASON> _globalPauses = new ();
    private static HashSet<PAUSE_REASON> _otherPauses = new ();

#if UNITY_EDITOR
    [ShowInInspector, ReadOnly, BoxGroup("Pause Debug")]
    [PropertyOrder(-10)]
    private List<PAUSE_REASON> GlobalPauses => new(_globalPauses);

    [ShowInInspector, ReadOnly, BoxGroup("Pause Debug")]
    [PropertyOrder(-9)]
    private List<PAUSE_REASON> OtherPauses => new(_otherPauses);
#endif

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Registers a new pause with an optional reason.
    /// </summary>
    /// <param name="reason">Optional pause reason (can be null or a specific enum value).</param>
    /// <param name="affectsGlobalPause">If true, this pause contributes to the overall pause state.</param>
    /// <returns>The created PauseEntry (to be used for later removal).</returns>
    public static void AddPause(PAUSE_REASON reason, bool affectsGlobalPause = true)
    {
        if (affectsGlobalPause)
        {
            _globalPauses.Add(reason);
            Events.OnGamePause.Invoke(reason);
        }
        else
            _otherPauses.Add(reason);
            
        if( reason == PAUSE_REASON.PAUSE_MENU)
        {
            ManagerUI.Instance.OpenPage(PAGE_TYPE.PAUSE_MENU);
        }
    }

    public static bool IsPaused()
    {
        return _globalPauses.Count > 0;
    }

    public static bool IsPaused(PAUSE_REASON reason)
    {
        return _globalPauses.Contains(reason) || _otherPauses.Contains(reason);
    }

    /// <summary>
    /// Removes a previously added pause.
    /// </summary>
    /// <param name="reason">The pause entry to remove.</param>
    public static void RemovePause(PAUSE_REASON reason)
    {
        if (_globalPauses.Remove(reason))
        {
            Events.OnGameUnPause.Invoke(reason);
            if (reason == PAUSE_REASON.PAUSE_MENU)
            {
                ManagerUI.Instance.ClosePage(PAGE_TYPE.PAUSE_MENU);
            }
        }

        if (_otherPauses.Remove(reason))
        {
            Events.OnGameUnPause.Invoke(reason);
            return;
        }

        
    }

    /// <summary>
    /// Optional: Clears all active pauses.
    /// </summary>
    public void ClearPauses()
    {
        _globalPauses.Clear();
        _otherPauses.Clear();
    }

   
}
