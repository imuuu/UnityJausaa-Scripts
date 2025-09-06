using System;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using Game.Utility;

namespace Game.BossSystem
{
    public enum BEHAVIOR_TYPE : byte
    {
        HP_SEGMENTS_BY_PERCENT,
        HP_SEGMENT_DROPS_STATIC_CHILDREN,
        HP_SEGMENT_DROPS_DYNAMIC_CHILDREN
    }

    public enum CROSS_DIRECTION : byte
    {
        DOWN_ONLY,
        UP_ONLY,
        BOTH
    }

    [Serializable]
    public sealed class PhaseBehavior
    {
        // ───────────────── Inspector helpers ─────────────────
        private bool IsByPercent => Type == BEHAVIOR_TYPE.HP_SEGMENTS_BY_PERCENT;
        private bool IsByChildCount => Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN || Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_DYNAMIC_CHILDREN;
        private bool IsDynamicChildren => Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_DYNAMIC_CHILDREN;
        private bool IsHpScope => PercentRef == PercentReference.HP_SCOPE;

        // ───────────────── What ─────────────────
        [FoldoutGroup("What"), LabelText("Name")] public string Name;
        [FoldoutGroup("What")] public BEHAVIOR_TYPE Type = BEHAVIOR_TYPE.HP_SEGMENTS_BY_PERCENT;

        // ───────────────── Timing ─────────────────
        [FoldoutGroup("Timing"), Min(0.05f)] public float IntervalSeconds = 1f;
        [FoldoutGroup("Timing")] public bool FireImmediatelyOnEnter = false;

        // ───────────────── Segments: Percent Mode ─────────────────
        [FoldoutGroup("Segments"), ShowIf(nameof(IsByPercent))]
        [LabelText("Step (%)"), SuffixLabel("%", true), Range(1f, 100f)]
        public float StepPercent = 10f;

        [FoldoutGroup("Segments"), ShowIf(nameof(IsByPercent)), ShowInInspector, ReadOnly]
        [LabelText("Segments (preview)")]
        private int _segmentsPreview => Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Clamp(StepPercent / 100f, 0.01f, 1f)));

        // ───────────────── Segments: Child Modes ─────────────────
        [FoldoutGroup("Segments"), ShowIf(nameof(IsByChildCount))]
        [Tooltip("Where to count direct children. If null, falls back to ctx.Boss.")]
        public Transform ChildCountRoot;

        [FoldoutGroup("Segments"), ShowIf(nameof(IsByChildCount))]
        [Tooltip("Static: cache initial child count on phase enter.")]
        public bool CaptureAtPhaseEnter = true;

        [FoldoutGroup("Segments"), ShowIf("@Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN")]
        [LabelText("Extra segments (+)"), Min(0)]
        public int ExtraSegmentsStatic = 0;

        // ───────────────── Domain ─────────────────
        [FoldoutGroup("Domain")] public PercentReference PercentRef = PercentReference.FROM_FULL_HP;
        [FoldoutGroup("Domain")] public CROSS_DIRECTION CrossDirection = CROSS_DIRECTION.DOWN_ONLY;

        [FoldoutGroup("Domain"), Min(1)]
        [Tooltip("Max triggers to emit in one evaluation tick.")]
        public int MaxTriggersPerTick = 3;

        // Scope UI (only affects where thresholds are placed; evaluation is always against full HP %)
        [FoldoutGroup("Domain"), ShowIf(nameof(IsHpScope))]
        [LabelText("Min HP (%)"), SuffixLabel("%", true), Range(0f, 100f)]
        [SerializeField] private float _minScopeHP = 0f;

        [FoldoutGroup("Domain"), ShowIf(nameof(IsHpScope))]
        [LabelText("Max HP (%)"), SuffixLabel("%", true), Range(0f, 100f)]
        [SerializeField] private float _maxScopeHP = 100f;

        [FoldoutGroup("Domain"), ShowIf(nameof(IsHpScope))]
        [LabelText("Exclusive Bounds (>min and <max)")]
        [SerializeField] private bool _exclusiveScopeBounds = false;

        // ───────────────── Action ─────────────────
        [FoldoutGroup("Action")] public MechanicTrigger MechanicToActivate;
        [FoldoutGroup("Action")] public bool IgnoreGlobalCooldown;
        [FoldoutGroup("Action")] public bool IgnorePerHolderCooldown;
        [FoldoutGroup("Action")] public UnityEvent OnFired;

        // ───────────────── Runtime ─────────────────
        [NonSerialized] private float _nextEvalAt;
        [NonSerialized] private float[] _thresholds01; // full-scale 0..1 (1=full), inner boundaries only
        [NonSerialized] private bool[] _fired;        // same length as thresholds
        [NonSerialized] private Transform _cachedRoot;

        // ───────────────── Lifecycle ─────────────────
        public void OnPhaseEnter(BossContext ctx)
        {
            _cachedRoot = ChildCountRoot != null ? ChildCountRoot : (ctx != null ? ctx.Boss : null);

            // Capture static children at enter if needed.
            if (Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN && CaptureAtPhaseEnter && _cachedRoot != null)
                ChildCountCache.CaptureInitialChildCount(_cachedRoot);

            RebuildThresholds(ctx, markCrossedAsFired: false); // simple: we want first discovery to fire

            float now = Time.time;
            _nextEvalAt = FireImmediatelyOnEnter ? now : now + IntervalSeconds;

            if (FireImmediatelyOnEnter)
                Evaluate(ctx, now);
        }

        public void Tick(BossContext ctx, float dt)
        {
            float now = Time.time;
            if (now < _nextEvalAt) return;
            _nextEvalAt = now + IntervalSeconds;

            // Dynamic mode: rebuild each evaluation and mark already-crossed as fired,
            // so we only emit new crossings.
            if (Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_DYNAMIC_CHILDREN)
                RebuildThresholds(ctx, markCrossedAsFired: true);

            Evaluate(ctx, now);
        }

        // ───────────────── Core ─────────────────
        private void Evaluate(BossContext ctx, float now)
        {
            if (ctx == null || _thresholds01 == null || _fired == null) return;

            // FULL HP percent (1=full, 0=empty). We always evaluate on the full scale.
            float hp01Full = Mathf.Clamp01(ctx.Hp01);

            int firedThisTick = 0;

            // DOWN pass (from full → empty): trigger when hp <= threshold
            if (CrossDirection == CROSS_DIRECTION.DOWN_ONLY || CrossDirection == CROSS_DIRECTION.BOTH)
            {
                // thresholds are sorted descending (high→low); this keeps order intuitive
                for (int i = 0; i < _thresholds01.Length; i++)
                {
                    if (_fired[i]) continue;
                    if (hp01Full <= _thresholds01[i])
                    {
                        Fire(ctx);
                        _fired[i] = true;
                        if (++firedThisTick >= MaxTriggersPerTick) return;
                    }
                }
            }

            // UP pass (heals): trigger when hp >= threshold
            if (CrossDirection == CROSS_DIRECTION.UP_ONLY || CrossDirection == CROSS_DIRECTION.BOTH)
            {
                // scan from low→high so the closest upcoming boundary fires first
                for (int i = _thresholds01.Length - 1; i >= 0; i--)
                {
                    if (_fired[i]) continue;
                    if (hp01Full >= _thresholds01[i])
                    {
                        Fire(ctx);
                        _fired[i] = true;
                        if (++firedThisTick >= MaxTriggersPerTick) return;
                    }
                }
            }
        }

        private void Fire(BossContext ctx)
        {
            if (MechanicToActivate != null)
                MechanicToActivate.ActivateNow(IgnoreGlobalCooldown, IgnorePerHolderCooldown);

            OnFired?.Invoke();
        }

        // Build inner boundaries and (optionally) pre-mark those already crossed
        private void RebuildThresholds(BossContext ctx, bool markCrossedAsFired)
        {
            int segs = GetSegmentsNow(ctx);
            if (segs < 1) segs = 1;

            // Inner boundaries count
            int count = Mathf.Max(0, segs - 1);

            _thresholds01 = (count == 0) ? Array.Empty<float>() : new float[count];
            _fired = (count == 0) ? Array.Empty<bool>() : new bool[count];

            // Domain on full scale
            GetFullScaleDomain(out float min01, out float max01);
            if (_exclusiveScopeBounds && PercentRef == PercentReference.HP_SCOPE)
            {
                const float eps = 1e-6f;
                min01 = Mathf.Min(max01, min01 + eps);
                max01 = Mathf.Max(min01, max01 - eps);
            }

            float span = max01 - min01;

            // Place thresholds descending from full → empty (high to low).
            // k = 1..(segs-1): t = max - k * span / segs
            for (int i = 0, k = 1; k <= segs - 1; k++, i++)
                _thresholds01[i] = max01 - (k * span / segs);

            // Optionally pre-mark thresholds already crossed (prevents refiring after dynamic rebuilds)
            if (markCrossedAsFired && ctx != null)
            {
                float hp01Full = Mathf.Clamp01(ctx.Hp01);
                for (int i = 0; i < _thresholds01.Length; i++)
                {
                    bool crossed =
                        (CrossDirection == CROSS_DIRECTION.DOWN_ONLY && hp01Full <= _thresholds01[i]) ||
                        (CrossDirection == CROSS_DIRECTION.UP_ONLY && hp01Full >= _thresholds01[i]) ||
                        (CrossDirection == CROSS_DIRECTION.BOTH && (hp01Full <= _thresholds01[i] || hp01Full >= _thresholds01[i]));

                    _fired[i] = crossed;
                }
            }
        }

        private int GetSegmentsNow(BossContext ctx)
        {
            switch (Type)
            {
                case BEHAVIOR_TYPE.HP_SEGMENTS_BY_PERCENT:
                    {
                        float step01 = Mathf.Clamp(StepPercent / 100f, 0.01f, 1f);
                        return Mathf.Max(1, Mathf.RoundToInt(1f / step01));
                    }

                case BEHAVIOR_TYPE.HP_SEGMENT_DROPS_DYNAMIC_CHILDREN:
                    {
                        Transform root = _cachedRoot != null ? _cachedRoot : (ctx != null ? ctx.Boss : null);
                        return Mathf.Max(1, root != null ? root.childCount : 1);
                    }

                case BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN:
                default:
                    {
                        Transform root = _cachedRoot != null ? _cachedRoot : (ctx != null ? ctx.Boss : null);
                        int baseCount = (root == null) ? 1 : ChildCountCache.GetOrCaptureInitialChildCount(root);
                        return Mathf.Max(1, baseCount + Mathf.Max(0, ExtraSegmentsStatic));
                    }
            }
        }

        private void GetFullScaleDomain(out float min01, out float max01)
        {
            if (PercentRef == PercentReference.HP_SCOPE)
            {
                min01 = Mathf.Clamp01(_minScopeHP * 0.01f);
                max01 = Mathf.Clamp01(_maxScopeHP * 0.01f);
                if (max01 < min01) max01 = min01;
            }
            else
            {
                min01 = 0f; max01 = 1f;
            }
        }

        // ───────────────── Debug / Preview ─────────────────
#if UNITY_EDITOR
        [OnValueChanged(nameof(_SanitizeScope))]
        private void _SanitizeScope()
        {
            _minScopeHP = Mathf.Clamp(_minScopeHP, 0f, 100f);
            _maxScopeHP = Mathf.Clamp(_maxScopeHP, 0f, 100f);
            if (_maxScopeHP < _minScopeHP) _maxScopeHP = _minScopeHP;
        }

        [UsedByInspector]
        [FoldoutGroup("Debug"), SerializeField] private bool _debugShowSegmentPreview = false;

        [FoldoutGroup("Debug"), ShowIf("@Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN")]
        [LabelText("Use debug child count")][SerializeField] private bool _useDebugStaticChildPreview = false;

        [FoldoutGroup("Debug"), ShowIf("@Type == BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN && _useDebugStaticChildPreview")]
        [LabelText("Preview children count"), Min(1)]
        [SerializeField] private int _debugStaticChildren = 6;

        [FoldoutGroup("Debug"), ShowIf(nameof(_debugShowSegmentPreview)), ReadOnly, ShowInInspector]
        [LabelText("Boundaries (%) – Full HP (descending)")]
        private string _DebugBoundariesPercent
        {
            get
            {
                int segs;
                switch (Type)
                {
                    case BEHAVIOR_TYPE.HP_SEGMENTS_BY_PERCENT:
                        segs = _segmentsPreview; break;

                    case BEHAVIOR_TYPE.HP_SEGMENT_DROPS_STATIC_CHILDREN:
                        {
                            int baseCount =
                                _useDebugStaticChildPreview ? Mathf.Max(1, _debugStaticChildren)
                                                            : (ChildCountRoot != null ? Mathf.Max(1, ChildCountRoot.childCount) : 1);
                            segs = Mathf.Max(1, baseCount + Mathf.Max(0, ExtraSegmentsStatic));
                            break;
                        }

                    case BEHAVIOR_TYPE.HP_SEGMENT_DROPS_DYNAMIC_CHILDREN:
                        segs = Mathf.Max(1, ChildCountRoot != null ? ChildCountRoot.childCount : 1);
                        break;

                    default: segs = 1; break;
                }

                GetFullScaleDomain(out float min01, out float max01);
                if (_exclusiveScopeBounds && PercentRef == PercentReference.HP_SCOPE)
                {
                    const float eps = 1e-6f;
                    min01 = Mathf.Min(max01, min01 + eps);
                    max01 = Mathf.Max(min01, max01 - eps);
                }

                int count = Mathf.Max(0, segs - 1);
                if (count == 0) return "(no inner boundaries)";

                float span = max01 - min01;
                var sb = new System.Text.StringBuilder(128);
                for (int k = 1; k <= segs - 1; k++)
                {
                    float p01 = max01 - (k * span / segs);
                    if (k > 1) sb.Append(", ");
                    sb.Append((p01 * 100f).ToString("0.#")).Append('%');
                }
                return sb.ToString();
            }
        }

        [FoldoutGroup("Debug"), ShowIf(nameof(_debugShowSegmentPreview))]
        [Button("Copy boundaries (CSV)")]
        private void _CopyBoundsCsv()
        {
            string txt = _DebugBoundariesPercent.Replace("%", "").Replace(" ", "");
            UnityEditor.EditorGUIUtility.systemCopyBuffer = txt;
            Debug.Log($"[PhaseBehavior] Copied boundaries: {txt}");
        }

        private void OnValidate()
        {
            if (_debugStaticChildren < 1) _debugStaticChildren = 1;
            _SanitizeScope();
        }
#endif
    }
}
