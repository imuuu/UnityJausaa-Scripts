using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.BossSystem
{
    #region Context

    /// <summary>Lightweight shared references for mechanics and conditions.</summary>
    public sealed class BossContext
    {
        public Transform Boss;      // Boss root
        public Transform Player;    // Player/target
        public IHealth Health;      // Health provider
        public Rigidbody BossRb;    // Optional rigidbody

        public float Hp01
        {
            get
            {
                if (Health == null) return 1f;
                float max = Health.GetMaxHealth();
                if (max <= 0f) return 1f;
                return Mathf.Clamp01(Health.GetHealth() / max);
            }
        }
    }

    #endregion

    #region Phases, Transitions, Toggles

    public enum TOGGLE_ACTION : byte { KEEP, ENABLE, DISABLE }

    [Serializable]
    public struct MechanicToggle
    {
        public Mechanic Mechanic;
        public TOGGLE_ACTION Action;
    }

    public enum TRANSITION_TYPE
    {
        ON_HP_AT_OR_BELOW_PERCENT,   // value = 0..100 (percent)
        ON_PHASE_TIME_REACHED,     // value = seconds
        ON_CONDITIONS_MET,        // evaluate ConditionGroup only
    }

    [Serializable]
    public struct PhaseTransition
    {
        public string Name;
        public TRANSITION_TYPE Type;
        [Tooltip("For HP %, 0..100. For Time, seconds.")] public float Value;
        public ConditionGroup Conditions; // optional extra gate
        public int NextPhaseIndex;        // target phase index (0..N-1)
        public bool Once;                 // if true, transition can only happen once

        [NonSerialized] public bool Consumed;

        public bool Evaluate(BossContext ctx)
        {
            if (Once && Consumed) return false;

            bool basic = false;
            switch (Type)
            {
                case TRANSITION_TYPE.ON_HP_AT_OR_BELOW_PERCENT:
                    basic = ctx != null && ctx.Hp01 <= (Value * 0.01f);
                    break;

                case TRANSITION_TYPE.ON_PHASE_TIME_REACHED:
                    basic = BossPhaseController.CurrentPhaseTime >= Value;
                    break;

                case TRANSITION_TYPE.ON_CONDITIONS_MET:
                    basic = true; // defer to conditions only
                    break;
            }

            if (!basic) return false;
            if (Conditions != null && !Conditions.Evaluate(ctx)) return false;
            return true;
        }
    }

   

    /// <summary>
    /// A phase that is evaluated independently of the main progression and can be active alongside any phase.
    /// It turns ON when its gate is satisfied, and OFF when not. While ON, its behaviors tick.
    /// </summary>
    [Serializable]
    public sealed class AlwaysActivePhase
    {
        [FoldoutGroup("Phase"), LabelText("Name")]
        public string Name;

        [FoldoutGroup("Phase")]
        public bool Enabled = true;

        // ---- Gate ----
        [FoldoutGroup("Gate"), LabelText("Use HP window")]
        public bool UseHpWindow = true;

        [FoldoutGroup("Gate"), ShowIf(nameof(UseHpWindow)), LabelText("Percent ref")]
        public PercentReference HpPercentRef = PercentReference.FROM_FULL_HP;

        [FoldoutGroup("Gate"), ShowIf(nameof(UseHpWindow)), LabelText("Min %"), Range(0, 100)]
        public float HpMinPercent = 15f;

        [FoldoutGroup("Gate"), ShowIf(nameof(UseHpWindow)), LabelText("Max %"), Range(0, 100)]
        public float HpMaxPercent = 100f;

        [FoldoutGroup("Gate"), Tooltip("Optional extra AND-gate.")]
        public ConditionGroup Conditions;

        // ---- Evaluation timing ----
        [FoldoutGroup("Timing"), Min(0.05f), Tooltip("How often to (re)evaluate the ON/OFF gate.")]
        public float EvalIntervalSeconds = 0.25f;

        [FoldoutGroup("Timing"), Tooltip("Check & apply gate immediately when entering any main phase.")]
        public bool ReevaluateOnPhaseEnter = true;

        // ---- Behaviors ----
        [FoldoutGroup("Behaviors"), Tooltip("Behaviors that tick while this always-phase is active.")]
        public PhaseBehavior[] Behaviors;

        // ---- Runtime ----
        [NonSerialized] public bool Active;
        [NonSerialized] public float NextEvalTime;

        public bool EvaluateGate(BossContext ctx)
        {
            if (!Enabled) return false;

            bool hpOk = true;
            if (UseHpWindow)
            {
                float p01 = Mathf.Clamp01(ctx != null ? ctx.Hp01 : 1f);
                // Measure from full or from zero (damage taken)
                float percent = (HpPercentRef == PercentReference.FROM_FULL_HP ? p01 : 1f - p01) * 100f;

                float min = Mathf.Min(HpMinPercent, HpMaxPercent);
                float max = Mathf.Max(HpMinPercent, HpMaxPercent);
                hpOk = percent >= min && percent <= max;
            }

            bool condOk = Conditions == null || Conditions.Evaluate(ctx);
            return hpOk && condOk;
        }
    }

    #endregion

    #region Controller

    /// <summary>Attach to the Boss root. Configure phases and reference mechanics.</summary>
    [DisallowMultipleComponent]
    public class BossPhaseController : MonoBehaviour
    {
        [FoldoutGroup("References"), SerializeField] private Transform _root;
        private Transform _player;
        private IHealth _health;
        [FoldoutGroup("References"), SerializeField] private Rigidbody _bossRb; // Optional

        // Phases - Progression (flat group, no parent required)
        [FoldoutGroup("Phases - Progression"), SerializeField] private int _startPhase = 0;
        [FoldoutGroup("Phases - Progression"), SerializeField] private BossPhase[] _phases = Array.Empty<BossPhase>();

        // Phases - Always Active (flat group, no parent required)
        [FoldoutGroup("Phases - Always Active")]
        [Tooltip("Always-on phases that run alongside the progression. Each has its own ON/OFF gate and behaviors.")]
        [SerializeField] private AlwaysActivePhase[] _alwaysPhases = Array.Empty<AlwaysActivePhase>();

        // Misc
        [FoldoutGroup("Misc"), SerializeField] private bool _autoCollectMechanicsInChildren = true;
        [FoldoutGroup("Misc"), SerializeField] private bool _logTransitions = false;

        // Runtime
        private BossContext _ctx;
        private Mechanic[] _allMechanics = Array.Empty<Mechanic>();
        private int _phaseIndex = -1;
        private float _phaseStartTime;

        // Static phase time for Condition.PhaseTimeAtLeast
        public static float CurrentPhaseTime { get; private set; }

        public int CurrentPhaseIndex => _phaseIndex;
        public BossPhase CurrentPhase =>
            (_phases != null && _phaseIndex >= 0 && _phaseIndex < _phases.Length)
            ? _phases[_phaseIndex]
            : null;

        // --- Debug (inspector) ---
        [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, PropertyOrder(-10)]
        private string CurrentPhaseLabel =>
    CurrentPhase != null && !string.IsNullOrEmpty(CurrentPhase.name)
        ? $"{_phaseIndex}: {CurrentPhase.name}"
        : (_phaseIndex >= 0 ? _phaseIndex.ToString() : "(none)");

        [FoldoutGroup("Debug"), ShowInInspector, ReadOnly]
        private string[] ActiveAlwaysPhaseNames
        {
            get
            {
                if (_alwaysPhases == null || _alwaysPhases.Length == 0) return Array.Empty<string>();
                var names = new System.Collections.Generic.List<string>();
                for (int i = 0; i < _alwaysPhases.Length; i++)
                {
                    var p = _alwaysPhases[i];
                    if (p != null && p.Active)
                        names.Add(string.IsNullOrEmpty(p.Name) ? $"#{i}" : p.Name);
                }
                return names.ToArray();
            }
        }

        private bool _initialized = false;

        private void Awake()
        {
            // Player ref comes from your own player-locator helper
            if (_player == null)
                Player.AssignTransformWhenAvailable(p => _player = p);

            // Health on root (fallback to self if _root is null)
            var host = _root != null ? _root : transform;
            _health = host.GetComponent<IHealth>();

            _ctx = new BossContext
            {
                Boss = transform,
                Player = _player,
                Health = _health,
                BossRb = _bossRb
            };

            _allMechanics = _autoCollectMechanicsInChildren
                ? GetComponentsInChildren<Mechanic>(includeInactive: true)
                : GetComponents<Mechanic>();

            // Initialize mechanics and pass context (delay to ensure other systems are up)
            _initialized = false;
            ActionScheduler.RunAfterDelay(1, () =>
            {
                for (int i = 0; i < _allMechanics.Length; i++)
                {
                    var m = _allMechanics[i];
                    if (m == null) continue;
                    m.__SetContext(_ctx);
                    m.OnMechanicInit();
                    m.__SetActive(false); // phase 0 will enable desired ones
                }
                _initialized = true;
            });

            // Prime always-phase eval timers
            if (_alwaysPhases != null)
            {
                float now = Time.time;
                for (int i = 0; i < _alwaysPhases.Length; i++)
                {
                    var ap = _alwaysPhases[i];
                    if (ap == null) continue;
                    ap.Active = false;
                    ap.NextEvalTime = now; // evaluate ASAP
                }
            }
        }

        private void OnEnable()
        {
            ActionScheduler.RunWhenTrue(() => _initialized, () =>
            {
                SetPhase(Mathf.Clamp(_startPhase, 0, (_phases != null ? _phases.Length - 1 : 0)));
            });
        }

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;
            CurrentPhaseTime = Time.time - _phaseStartTime;

            // Tick mechanics (active + AlwaysTick)
            for (int i = 0; i < _allMechanics.Length; i++)
            {
                var m = _allMechanics[i];
                if (m == null) continue;
                if (m.__AlwaysTick || m.enabled) m.Tick(dt);
            }

            // Tick always-active phases: gate + behaviors
            TickAlwaysActivePhases(dt);

            // Evaluate transitions (one per frame)
            var phase = CurrentPhase;
            if (phase != null && phase.transitions != null)
            {
                for (int i = 0; i < phase.transitions.Length; i++)
                {
                    var tr = phase.transitions[i];
                    if (tr.Evaluate(_ctx))
                    {
                        if (tr.Once) phase.transitions[i].Consumed = true;
                        SetPhase(tr.NextPhaseIndex);
                        break;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            for (int i = 0; i < _allMechanics.Length; i++)
            {
                var m = _allMechanics[i];
                if (m == null) continue;
                m.FixedTick(fdt);
            }
        }

        /// <summary>Force a phase by index (0..N-1).</summary>
        public void SetPhase(int index)
        {
            if (_phases == null || _phases.Length == 0) return;
            index = Mathf.Clamp(index, 0, _phases.Length - 1);
            if (index == _phaseIndex && _phaseIndex >= 0) return; // no-op

            // Exit current
            if (_phaseIndex >= 0)
            {
                for (int i = 0; i < _allMechanics.Length; i++)
                {
                    var m = _allMechanics[i];
                    if (m != null) m.OnPhaseExit();
                }
            }

            _phaseIndex = index;
            _phaseStartTime = Time.time;
            CurrentPhaseTime = 0f;

            var phase = _phases[_phaseIndex];

            // Apply toggles
            if (phase != null && phase.toggles != null)
            {
                for (int i = 0; i < phase.toggles.Length; i++)
                {
                    var t = phase.toggles[i];
                    if (t.Mechanic == null) continue;

                    switch (t.Action)
                    {
                        case TOGGLE_ACTION.ENABLE: t.Mechanic.__SetActive(true); break;
                        case TOGGLE_ACTION.DISABLE: t.Mechanic.__SetActive(false); break;
                        case TOGGLE_ACTION.KEEP: t.Mechanic.__SetActive(true); break;
                    }
                }
            }

            // Notify mechanics of phase entry
            for (int i = 0; i < _allMechanics.Length; i++)
            {
                var m = _allMechanics[i];
                if (m != null) m.OnPhaseEnter();
            }

            // Optionally force re-eval of always phases on phase enter
            if (_alwaysPhases != null)
            {
                float now = Time.time;
                for (int i = 0; i < _alwaysPhases.Length; i++)
                {
                    var ap = _alwaysPhases[i];
                    if (ap == null) continue;
                    if (ap.ReevaluateOnPhaseEnter) ap.NextEvalTime = now;
                }
            }

            if (_logTransitions)
            {
                string label = phase != null ? (string.IsNullOrEmpty(phase.name) ? $"#{_phaseIndex}" : phase.name) : $"#{_phaseIndex}";
                Debug.Log($"[BossPhase] Enter -> {label} (index {_phaseIndex})");
            }
        }

        private void TickAlwaysActivePhases(float dt)
        {
            if (_alwaysPhases == null || _alwaysPhases.Length == 0) return;

            float now = Time.time;

            for (int i = 0; i < _alwaysPhases.Length; i++)
            {
                var alwaysActivePhase = _alwaysPhases[i];
                if (alwaysActivePhase == null || !alwaysActivePhase.Enabled) continue;

                // Gate evaluation on its own interval
                if (now >= alwaysActivePhase.NextEvalTime)
                {
                    alwaysActivePhase.NextEvalTime = now + Mathf.Max(0.05f, alwaysActivePhase.EvalIntervalSeconds);
                    bool shouldBeActive = alwaysActivePhase.EvaluateGate(_ctx);

                    if (shouldBeActive && !alwaysActivePhase.Active)
                    {
                        // Activate
                        alwaysActivePhase.Active = true;
                        if (alwaysActivePhase.Behaviors != null)
                        {
                            for (int b = 0; b < alwaysActivePhase.Behaviors.Length; b++)
                                alwaysActivePhase.Behaviors[b]?.OnPhaseEnter(_ctx);
                        }
                        if (_logTransitions)
                            Debug.Log($"[AlwaysPhase] ON -> {(string.IsNullOrEmpty(alwaysActivePhase.Name) ? $"#{i}" : alwaysActivePhase.Name)}");
                    }
                    else if (!shouldBeActive && alwaysActivePhase.Active)
                    {
                        // Deactivate
                        alwaysActivePhase.Active = false;
                        // If you added PhaseBehavior.OnPhaseExit, you can call it here:
                        // for (int b = 0; b < ap.behaviors.Length; b++) ap.behaviors[b]?.OnPhaseExit(_ctx);
                        if (_logTransitions)
                            Debug.Log($"[AlwaysPhase] OFF -> {(string.IsNullOrEmpty(alwaysActivePhase.Name) ? $"#{i}" : alwaysActivePhase.Name)}");
                    }
                }

                // Tick behaviors while active (they self-throttle with their own interval)
                if (alwaysActivePhase.Active && alwaysActivePhase.Behaviors != null)
                {
                    for (int b = 0; b < alwaysActivePhase.Behaviors.Length; b++)
                        alwaysActivePhase.Behaviors[b]?.Tick(_ctx, dt);
                }

                // write back struct to array (since AlwaysActivePhase is a class, not needed; left for clarity)
                _alwaysPhases[i] = alwaysActivePhase;
            }
        }
    }

    #endregion

    // (Optional) Keep here if you didn’t move it to its own file.
    internal static class ChildCountCache
    {
        private static readonly Dictionary<int, int> _initialChildCounts = new(64);

        public static void CaptureInitialChildCount(Transform root)
        {
            if (root == null) return;
            _initialChildCounts[root.GetInstanceID()] = root.childCount;
        }

        public static int GetOrCaptureInitialChildCount(Transform root)
        {
            if (root == null) return 0;
            int id = root.GetInstanceID();
            if (_initialChildCounts.TryGetValue(id, out int c)) return c;
            c = root.childCount;
            _initialChildCounts[id] = c;
            return c;
        }
    }
}
