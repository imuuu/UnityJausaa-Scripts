public delegate bool EventPointHandler();

public class EventPoint
{
    private event EventPointHandler _action = delegate { return true; };
    private readonly object _lock = new object();

    public bool Invoke()
    {
        EventPointHandler handlers;
        lock (_lock)
        {
            handlers = _action;
        }

        // Make things more efficient by not using foreach, maybe priorities
        foreach (EventPointHandler handler in handlers.GetInvocationList())
        {
            bool shouldContinue = handler();
            if (!shouldContinue)
                return false;
        }

        return true; 
    }

    public void AddListener(EventPointHandler listener)
    {
        lock (_lock)
        {
            _action += listener;
        }
    }

    public void RemoveListener(EventPointHandler listener)
    {
        lock (_lock)
        {
            _action -= listener;
        }
    }

    public void AddListenerOnce(EventPointHandler listener)
    {
        EventPointHandler wrapper = null;
        wrapper = () =>
        {
            bool result;
            RemoveListener(wrapper);
            result = listener();
            return result;
        };
        AddListener(wrapper);
    }
}

public delegate bool EventPointHandler<T>(T param);

public class EventPoint<T>
{
    private event EventPointHandler<T> _action = delegate { return true; };
    private readonly object _lock = new object();

    public bool Invoke(T param)
    {
        EventPointHandler<T> handlers;
        lock (_lock)
        {
            handlers = _action;
        }

        //Make things more efficient by not using foreach, maybe priorities
        foreach (EventPointHandler<T> handler in handlers.GetInvocationList())
        {
            bool shouldContinue = handler(param);
            if (!shouldContinue)
                return false;
        }

        return true;
    }

    public void AddListener(EventPointHandler<T> listener)
    {
        lock (_lock)
        {
            _action += listener;
        }
    }

    public void RemoveListener(EventPointHandler<T> listener)
    {
        lock (_lock)
        {
            _action -= listener;
        }
    }

    public void AddListenerOnce(EventPointHandler<T> listener)
    {
        EventPointHandler<T> wrapper = null;
        wrapper = (param) =>
        {
            bool result;
            RemoveListener(wrapper);
            result = listener(param);
            return result;
        };
        AddListener(wrapper);
    }
}

public delegate bool EventPointHandler<T1, T2>(T1 param1, T2 param2);

public class EventPoint<T1, T2>
{
    private event EventPointHandler<T1, T2> _action = delegate { return true; };
    private readonly object _lock = new object();

    /// <summary>
    /// Invokes the event with the given parameters.
    /// Returns false if the event was canceled by a listener, true otherwise.
    /// </summary>
    public bool Invoke(T1 param1, T2 param2)
    {
        EventPointHandler<T1, T2> handlers;
        lock (_lock)
        {
            handlers = _action;
        }

        //Make things more efficient by not using foreach, maybe priorities
        foreach (EventPointHandler<T1, T2> handler in handlers.GetInvocationList())
        {
            bool shouldContinue = handler(param1, param2);
            if (!shouldContinue)
                return false;
        }

        return true;
    }

    public void AddListener(EventPointHandler<T1, T2> listener)
    {
        lock (_lock)
        {
            _action += listener;
        }
    }

    public void RemoveListener(EventPointHandler<T1, T2> listener)
    {
        lock (_lock)
        {
            _action -= listener;
        }
    }

    public void AddListenerOnce(EventPointHandler<T1, T2> listener)
    {
        EventPointHandler<T1, T2> wrapper = null;
        wrapper = (param1, param2) =>
        {
            bool result;
            RemoveListener(wrapper);
            result = listener(param1, param2);
            return result;
        };
        AddListener(wrapper);
    }
}


