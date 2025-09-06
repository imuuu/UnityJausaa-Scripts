using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Animancer;
using Sirenix.OdinInspector;

/// <summary>
/// Boss animation driver using AnimancerComponent + AnimationClip lists (no NamedAnimancer).
/// - Random idle loop from _idleClips with optional delay and no immediate repeat.
/// - Trigger one-shot attacks from _attackClips (random or specific).
/// - Looping attacks are treated as one cycle, then end.
/// - Optional attack queue: requests are queued if an attack is already playing.
/// - No coroutines or Invoke; tiny Update() timer for idle delay.
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
[DefaultExecutionOrder(2)]
public sealed class AnimationDirector : MonoBehaviour
{
    private enum Mode { IdleLoop, AttackPlaying, Stopped }

    [Header("References")]
    [SerializeField] private AnimancerComponent _animancer; // auto-filled in Awake if null

    [Header("Idle Settings")]
    [SerializeField] private List<AnimationClip> _idleClips = new();
    [Tooltip("x = min, y = max seconds between idle changes")]
    [SerializeField] private Vector2 _idleDelayRange = Vector2.zero;
    [SerializeField, Min(0f)] private float _idleFade = 0.15f;
    [SerializeField] private bool _avoidImmediateRepeatIdle = true;

    [Header("Attack Settings")]
    [SerializeField] private List<AnimationClip> _attackClips = new();
    [SerializeField, Min(0f)] private float _attackFade = 0.10f;
    [SerializeField] private bool _avoidImmediateRepeatAttack = true;
    [SerializeField] private bool _returnToIdleAfterAttack = true;

    [Header("Attack Queue")]
    [SerializeField, Min(0)] private int _maxQueuedAttacks = 4; // 0 = unbounded
    [SerializeField] private bool _allowDuplicateQueuedAttacks = true;

    private readonly Queue<AttackRequest> _attackQueue = new();

    private readonly struct AttackRequest
    {
        public readonly AnimationClip Clip;
        public readonly int Index; // -1 if the clip is not in _attackClips
        public readonly Action EndCallback; // fired when THIS attack finishes
        public AttackRequest(AnimationClip clip, int index, Action endCallback)
        {
            Clip = clip; Index = index; EndCallback = endCallback;
        }
    }

    [ShowInInspector, ReadOnly] public int QueuedAttackCount => _attackQueue.Count;

    [Header("Lifecycle")]
    [SerializeField] private bool _autoStartIdleOnEnable = true;

    [Header("Events")]
    [SerializeField] private ClipEvent _onIdlePlayed;    // payload: idle clip
    [SerializeField] private ClipEvent _onAttackStarted; // payload: attack clip
    [SerializeField] private ClipEvent _onAttackEnded;   // payload: attack clip
    [Serializable] public sealed class ClipEvent : UnityEvent<AnimationClip> { }

    // --- runtime ---
    private Mode _mode = Mode.Stopped;
    private AnimancerState _state;       // current state
    private AnimationClip _stateClip;    // current clip

    private float _idleDelayTimer;
    private bool _waitingIdleDelay;

    private int _lastIdleIndex = -1;
    private int _lastAttackIndex = -1;

    // Per-attack one-shot callback supplied via TriggerAttack(..., endOfAnimationCallback)
    private Action _pendingEndCallback;

    // Prevent stacking duplicate loop events on reused states
    private AnimancerState _lastConfiguredLoopState;

    private void Awake()
    {
        if (_animancer == null) _animancer = GetComponent<AnimancerComponent>();
    }

    private void OnEnable()
    {
        if (_autoStartIdleOnEnable) StartIdleLoop();
    }

    private void OnDisable()
    {
        if (_state != null && _state.Events(this, out var ev)) ev.OnEnd = null;
        _mode = Mode.Stopped;
        _waitingIdleDelay = false;
        _pendingEndCallback = null;
    }

    private void Update()
    {
        if (_mode == Mode.IdleLoop && _waitingIdleDelay)
        {
            _idleDelayTimer -= Time.deltaTime;
            if (_idleDelayTimer <= 0f)
            {
                _waitingIdleDelay = false;
                PlayIdleNow();
            }
        }
    }

    // ----------------------------- Public API -----------------------------

    public void StartIdleLoop()
    {
        _mode = Mode.IdleLoop;
        _waitingIdleDelay = false;
        if (_state != null && _state.Events(this, out var ev)) ev.OnEnd = null;
        PlayIdleNow();
    }

    public void NextIdleNow()
    {
        if (_mode != Mode.IdleLoop) _mode = Mode.IdleLoop;
        _waitingIdleDelay = false;
        PlayIdleNow();
    }

    [Button("Clear Attack Queue")]
    public void ClearAttackQueue() => _attackQueue.Clear();

    public void StopAll()
    {
        _mode = Mode.Stopped;
        _waitingIdleDelay = false;
        if (_state != null && _state.Events(this, out var ev)) ev.OnEnd = null;
        _state = null;
        _stateClip = null;
        _animancer.Stop();
        _attackQueue.Clear();
        _pendingEndCallback = null;
    }

    /// <summary>
    /// Trigger a random attack now or enqueue if one is already playing.
    /// </summary>
    public bool TriggerRandomAttack(bool queueIfBusy = true)
    {
        int index = PickIndex(_attackClips, _lastAttackIndex, _avoidImmediateRepeatAttack);
        return TriggerAttackByIndex(index, queueIfBusy);
    }

    /// <summary>
    /// Trigger a specific attack clip now or enqueue if one is already playing.
    /// </summary>
    public bool TriggerAttack(AnimationClip clip, bool queueIfBusy = true)
    {
        if (clip == null) return false;

        if (_mode == Mode.AttackPlaying && queueIfBusy)
            return EnqueueAttack(clip, _attackClips.IndexOf(clip), endCallback: null);

        int idx = _attackClips.IndexOf(clip);
        if (idx < 0) return PlayAttackClip(clip, -1, endCallback: null); // allow external clips too
        return PlayAttackClip(_attackClips[idx], idx, endCallback: null);
    }

    /// <summary>
    /// Trigger a specific attack clip and register a one-shot callback that fires
    /// when THAT attack finishes. If an attack is already playing and queueIfBusy is true,
    /// this attack (with its callback) will be queued and the callback will fire when it ends.
    /// </summary>
    public bool TriggerAttack(AnimationClip clip, bool queueIfBusy = true, Action endOfAnimationCallBack = null)
    {
        if (clip == null) return false;

        if (_mode == Mode.AttackPlaying && queueIfBusy)
            return EnqueueAttack(clip, _attackClips.IndexOf(clip), endOfAnimationCallBack);

        int idx = _attackClips.IndexOf(clip);
        if (idx < 0) return PlayAttackClip(clip, -1, endOfAnimationCallBack); // external clip
        return PlayAttackClip(_attackClips[idx], idx, endOfAnimationCallBack);
    }

    /// <summary>
    /// Trigger a specific attack by index now or enqueue if one is already playing.
    /// </summary>
    public bool TriggerAttackByIndex(int index, bool queueIfBusy = true)
    {
        if (index < 0 || index >= _attackClips.Count) return false;

        if (_mode == Mode.AttackPlaying && queueIfBusy)
            return EnqueueAttack(_attackClips[index], index, endCallback: null);

        return PlayAttackClip(_attackClips[index], index, endCallback: null);
    }

    /// <summary>
    /// Always enqueue the given clip (even if not busy). Useful for pre-built combos.
    /// </summary>
    public bool QueueAttack(AnimationClip clip, Action endCallback = null)
    {
        if (clip == null) return false;
        return EnqueueAttack(clip, _attackClips.IndexOf(clip), endCallback);
    }

    /// <summary>
    /// Always enqueue by index (even if not busy). Useful for pre-built combos.
    /// </summary>
    public bool QueueAttackByIndex(int index, Action endCallback = null)
    {
        if (index < 0 || index >= _attackClips.Count) return false;
        return EnqueueAttack(_attackClips[index], index, endCallback);
    }

    // ---- Event subscription helpers (optional, still useful) ----

    /// <summary>Add a persistent listener that receives the finished attack clip.</summary>
    public void ListenEventAttackEnd(UnityAction<AnimationClip> callback)
    {
        _onAttackEnded.AddListener(callback);
    }

    /// <summary>Register a one-shot listener (no parameters) for the next attack end.</summary>
    public void ListenEventAttackEndOnce(Action callback)
    {
        void Handler(AnimationClip _)
        {
            _onAttackEnded.RemoveListener(Handler);
            callback?.Invoke();
        }
        _onAttackEnded.AddListener(Handler);
    }

    /// <summary>Register a one-shot listener (with clip) for the next attack end.</summary>
    public void ListenEventAttackEndOnce(UnityAction<AnimationClip> callback)
    {
        void Handler(AnimationClip clip)
        {
            _onAttackEnded.RemoveListener(Handler);
            callback?.Invoke(clip);
        }
        _onAttackEnded.AddListener(Handler);
    }

    // ----------------------------- Internals -----------------------------

    private bool EnqueueAttack(AnimationClip clip, int index, Action endCallback)
    {
        if (clip == null) return false;

        if (!_allowDuplicateQueuedAttacks)
        {
            foreach (var req in _attackQueue)
                if (ReferenceEquals(req.Clip, clip))
                    return false; // already queued
        }

        if (_maxQueuedAttacks > 0 && _attackQueue.Count >= _maxQueuedAttacks)
            _attackQueue.Dequeue(); // drop oldest (simple policy)

        _attackQueue.Enqueue(new AttackRequest(clip, index, endCallback));
        return true;
    }

    private bool TryDequeueNextAttack(out AttackRequest req)
    {
        if (_attackQueue.Count > 0)
        {
            req = _attackQueue.Dequeue();
            return true;
        }
        req = default;
        return false;
    }

    private void PlayIdleNow()
    {
        if (_idleClips == null || _idleClips.Count == 0) return;

        int index = PickIndex(_idleClips, _lastIdleIndex, _avoidImmediateRepeatIdle);
        if (index < 0) return;

        _lastIdleIndex = index;
        var clip = _idleClips[index];

        _state = _animancer.Play(clip, _idleFade);
        _stateClip = clip;
        if (_state == null) return;

        _state.NormalizedTime = 0f;

        if (_state.Events(this, out var ev))
        {
            // Looping idles never call OnEnd; fire once per loop at normalized 1.0.
            if (_state.IsLooping)
            {
                if (!ReferenceEquals(_lastConfiguredLoopState, _state))
                {
                    ev.OnEnd = null; // ensure no dangling end handler
                    ev.Add(1f, OnIdleLoopIterationEnd);
                    _lastConfiguredLoopState = _state;
                }
            }
            else
            {
                // IMPORTANT: OnEnd fires every frame after it has passed.
                // Detach OnEnd inside the callback to avoid re-scheduling every frame.
                ev.OnEnd = OnIdleEndOnce;
            }
        }

        _onIdlePlayed?.Invoke(clip);
    }

    private void OnIdleLoopIterationEnd()
    {
        if (_mode != Mode.IdleLoop) return;

        // If already waiting for a delayed switch, ignore further loop events until switch happens.
        if (_waitingIdleDelay) return;

        ScheduleNextIdleAfterDelay();
    }

    private void OnIdleEndOnce()
    {
        // Detach OnEnd immediately so it doesn't fire every frame while the state is clamped at the end.
        if (_state != null && _state.Events(this, out var ev)) ev.OnEnd = null;

        if (_mode != Mode.IdleLoop) return;
        if (_waitingIdleDelay) return;

        ScheduleNextIdleAfterDelay();
    }

    private void ScheduleNextIdleAfterDelay()
    {
        float min = Mathf.Min(_idleDelayRange.x, _idleDelayRange.y);
        float max = Mathf.Max(_idleDelayRange.x, _idleDelayRange.y);
        float delay = (max > 0f) ? UnityEngine.Random.Range(min, max) : 0f;

        if (delay > 0f)
        {
            _waitingIdleDelay = true;
            _idleDelayTimer = delay;
        }
        else
        {
            PlayIdleNow();
        }
    }

    public bool PlayAttackClip(AnimationClip clip, int attackIndex = -1, Action endCallback = null)
    {
        _mode = Mode.AttackPlaying;
        _waitingIdleDelay = false;

        // Clean up any previous state's end handler.
        if (_state != null && _state.Events(this, out var oldEv)) oldEv.OnEnd = null;

        // Replace the pending callback (interrupting cancels previous one).
        _pendingEndCallback = null;

        _state = _animancer.Play(clip, _attackFade);
        _stateClip = clip;
        if (_state == null) return false;

        _state.NormalizedTime = 0f;

        if (_state.Events(this, out var ev))
        {
            if (_state.IsLooping)
            {
                // Treat looping attack as one cycle, then end.
                ev.OnEnd = null;
                ev.Add(1f, OnAttackEndOnce);
            }
            else
            {
                // OnEnd fires every frame after end -> we detach inside OnAttackEnd.
                ev.OnEnd = OnAttackEnd;
            }
        }

        // Store the one-shot callback for THIS attack only.
        _pendingEndCallback = endCallback;

        if (attackIndex >= 0) _lastAttackIndex = attackIndex;
        _onAttackStarted?.Invoke(clip);
        return true;
    }

    [Button("Play Attack Clip")]
    public void PlayClip(int index)
    {
        if (index < 0 || index >= _attackClips.Count) return;
        PlayAttackClip(_attackClips[index], index, endCallback: null);
    }

    private void OnAttackEndOnce() => OnAttackEnd();

    private void OnAttackEnd()
    {
        // Detach end callback to avoid repeated calls if clip is clamped at end.
        if (_state != null && _state.Events(this, out var ev)) ev.OnEnd = null;

        // Fire global event (with clip) first.
        _onAttackEnded?.Invoke(_stateClip);

        // Then fire per-attack one-shot callback (no params).
        _pendingEndCallback?.Invoke();
        _pendingEndCallback = null;

        // If a queued attack exists, play it immediately (preempts idle return).
        if (TryDequeueNextAttack(out var next))
        {
            PlayAttackClip(next.Clip, next.Index, next.EndCallback);
            return;
        }

        if (_returnToIdleAfterAttack)
            StartIdleLoop();
        else
            _mode = Mode.Stopped;
    }

    private static int PickIndex(List<AnimationClip> list, int lastIndex, bool avoidImmediateRepeat)
    {
        if (list == null || list.Count == 0) return -1;
        if (list.Count == 1) return 0;

        int index = UnityEngine.Random.Range(0, list.Count);
        if (avoidImmediateRepeat && list.Count > 1 && index == lastIndex)
            index = (index + 1) % list.Count;
        return index;
    }
}
