using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages scheduling actions to run after a specified delay, on the next frame, or when a condition is true.
/// Runs on Unity's Update.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ActionScheduler : MonoBehaviour
{
    public static ActionScheduler Instance { get; private set; }
    private static LinkedList<IScheduledAction> _scheduledActions = new();

    //TODO POOLING: Add pooling for IScheduledAction objects

    private static bool _debug = false;

    #region Interfaces and Classes
    private interface IScheduledAction
    {
        Action Action { get; }
        object Identifier { get; }
        bool ShouldRun(float deltaTime);
        void Run();
    }

    private class DelayedAction : IScheduledAction
    {
        private float _delay;
        public DelayedAction(float delay, Action action, object identifier)
        {
            _delay = delay;
            Action = action;
            Identifier = identifier;
        }

        public Action Action { get; }
        public object Identifier { get; }

        public bool ShouldRun(float deltaTime)
        {
            _delay -= deltaTime;
            return _delay <= 0f;
        }

        public void Run()
        {
            Action.Invoke();
        }

        public void ResetDelay(float newDelay)
        {
            _delay = newDelay;
        }

        public float GetDelay()
        {
            return _delay;
        }
    }

    private class NextFrameAction : IScheduledAction
    {
        public Action Action { get; }
        public object Identifier { get; }
        public NextFrameAction(Action action, object identifier)
        {
            Action = action;
            Identifier = identifier;
        }

        public bool ShouldRun(float deltaTime) => true;

        public void Run()
        {
            Action.Invoke();
        }
    }

    private class ConditionalAction : IScheduledAction
    {
        private readonly Func<bool> _condition;
        public Action Action { get; }
        public object Identifier { get; }
        public ConditionalAction(Func<bool> condition, Action action, object identifier)
        {
            _condition = condition;
            Action = action;
            Identifier = identifier;
        }

        public bool ShouldRun(float deltaTime) => _condition.Invoke();

        public void Run()
        {
            Action.Invoke();
        }
    }

    private class RepeatedConditionalAction : IScheduledAction
    {
        private readonly Func<bool> _condition;
        private readonly int _maxUnchangedChecks;
        private int _unchangedCount;
        private Action _action;
        private float _checkInterval;
        private float _timeSinceLastCheck;

        public Action Action => _action;
        public object Identifier { get; }

        public RepeatedConditionalAction(Func<bool> condition, int maxUnchangedChecks, Action action, float checkInterval, object identifier)
        {
            _condition = condition;
            _maxUnchangedChecks = maxUnchangedChecks;
            _action = action;
            _checkInterval = checkInterval;
            _timeSinceLastCheck = 0f;
            Identifier = identifier;
        }

        public bool ShouldRun(float deltaTime)
        {
            _timeSinceLastCheck += deltaTime;
            if (_timeSinceLastCheck < _checkInterval)
                return false;

            _timeSinceLastCheck = 0f;

            if (_condition.Invoke())
            {
                _unchangedCount++;
                if (_unchangedCount >= _maxUnchangedChecks)
                {
                    return true;
                }
            }
            else
            {
                _unchangedCount = 0;
            }

            return false;
        }

        public void Run()
        {
            _action.Invoke();
        }
    }
    #endregion Interfaces and Classes

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_debugActions.Count > 0) _debugActions.Clear();
#endif

        if (_scheduledActions.Count == 0) return;

        float deltaTime = Time.deltaTime;
        LinkedListNode<IScheduledAction> node = _scheduledActions.First;

        while (node != null)
        {
            LinkedListNode<IScheduledAction> nextNode = node.Next;
            IScheduledAction action = node.Value;
            if (action.ShouldRun(deltaTime))
            {
                _scheduledActions.Remove(node);
                action.Run();
            }
            else
            {
#if UNITY_EDITOR
                _debugActions.Add(new DebugAction(action));
#endif
            }
            node = nextNode;
        }
    }

    /// <summary>
    /// Schedules an action to run on the next frame.
    /// </summary>
    public static void RunNextFrame(Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }
        _scheduledActions.AddFirst(new NextFrameAction(action, identifier));
    }

    /// <summary>
    /// Schedules an action to run after a specified delay.
    /// </summary>
    public static void RunAfterDelay(float delay, Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }
        _scheduledActions.AddLast(new DelayedAction(delay, action, identifier));
    }

    public static void RunAfterDelay(TimeSpan delay, Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }

        RunAfterDelay((float)delay.TotalSeconds, action, identifier);
    }

    /// <summary>
    /// Runs the action when the specified condition is true.
    /// </summary>
    public static void RunWhenTrue(Func<bool> condition, Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }

        if (condition.Invoke())
        {
            action.Invoke();
            return;
        }
        _scheduledActions.AddLast(new ConditionalAction(condition, action, identifier));
    }

    /// <summary>
    /// Runs the action after the specified delay. If an action with the same identifier is already scheduled, it will be refreshed with the new delay.
    /// </summary>
    public static void RunOrRefreshAfterDelay(TimeSpan delay, Action action, object identifier = null)
    {
        RunOrRefreshAfterDelay((float)delay.TotalSeconds, action, identifier);
    }

    /// <summary>
    /// Runs the action after the specified delay. If an action with the same identifier is already scheduled, it will be refreshed with the new delay.
    /// </summary>
    public static void RunOrRefreshAfterDelay(float delaySeconds, Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }

        LinkedListNode<IScheduledAction> node = _scheduledActions.First;
        while (node != null)
        {
            if (node.Value is DelayedAction delayedAction
            && node.Value.Action != null
            && node.Value.Action.Method == action.Method 
            && node.Value.Identifier.Equals(identifier))
            {
                delayedAction.ResetDelay(delaySeconds);
                return;
            }
            node = node.Next;
        }

        if (delaySeconds <= 0)
        {
            RunNextFrame(action, identifier);
            return;
        }

        _scheduledActions.AddLast(new DelayedAction(delaySeconds, action, identifier));
    }

    /// <summary>
    /// Runs the action when the condition is true for a specified number of consecutive checks, with a delay between each check.
    /// </summary>
    public static void RunWhenConditionUnchanged(Func<bool> condition, int maxUnchangedChecks, float checkInterval, Action action, object identifier = null)
    {
        if (Instance == null)
        {
            DebugLog();
        }

        _scheduledActions.AddLast(new RepeatedConditionalAction(condition, maxUnchangedChecks, action, checkInterval, identifier));
    }

    /// <summary>
    /// Removes scheduled actions with the specified identifier.
    /// </summary>
    public static void CancelActions(object identifier)
    {
        if (Instance == null)
        {
            return;
        }

        LinkedListNode<IScheduledAction> node = _scheduledActions.First;
        while (node != null)
        {
            LinkedListNode<IScheduledAction> nextNode = node.Next;
            IScheduledAction scheduledAction = node.Value;

            if (scheduledAction.Identifier != null && scheduledAction.Identifier.Equals(identifier))
            {
                _scheduledActions.Remove(node);
            }

            node = nextNode;
        }
    }

    private static void DebugLog()
    {
        if(!_debug) return;

        Debug.LogError("ActionScheduler is not initialized. Ensure it's attached to a GameObject in the scene.");
    }

#if UNITY_EDITOR
    [Header("=== Debug ===")]
    [SerializeField]
    private List<DebugAction> _debugActions = new();
    [Serializable]
    private class DebugAction
    {
        public string ClassName;
        public string ActionName;
        public float Delay;
        public object Identifier;

        public DebugAction(IScheduledAction scheduledAction)
        {
            ClassName = scheduledAction.GetType().Name;
            ActionName = scheduledAction.Action.Method.Name;
            Delay = scheduledAction is DelayedAction delayedAction ? delayedAction.GetDelay() : -1;
            Identifier = scheduledAction.Identifier;
        }
    }
#endif
}
