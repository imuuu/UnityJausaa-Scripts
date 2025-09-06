using System.Collections.Generic;
using System.Linq;
using Game.Extensions; // TraverseChildren (editor helper)
using Game.Utility;     // SimpleTimer
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BossSystem
{
    public enum CooldownGroupSource : byte
    {
        AutoOwnerRoot,
        CustomTransform,
        CustomInt,
        None,
    }

    public enum DistanceOriginSource : byte { Holder, MechanicSelf, OwnerRoot, CustomTransform }
    public enum AngleOriginSource : byte { Holder, MechanicSelf, OwnerRoot, CustomTransform }

    public enum RangeGizmoMode : byte { Sphere3D, CircleXZ, Both }

    /// <summary>
    /// Base for trigger-like boss mechanics.
    /// Handles: interval timing, holder selection (modes, chance), per-holder cooldown,
    /// min/max range + min/max angle/LOS gates, and optional global cooldown groups.
    ///
    /// Requires a BossMechanic base that provides Ctx (BossContext) and CanAct.
    /// </summary>
    public abstract class MechanicTrigger : Mechanic
    {
        #region Fields

        [FoldoutGroup("Trigger"), SerializeField]
        private bool _useTriggerLoop = true;

        // Timing
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Timing"), Min(0f)]
        [SerializeField] protected float _triggerInterval = 1.0f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Timing")]
        [SerializeField] protected bool _startDesynced = true;

        // Holders
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Holders")]
        [SerializeField] protected List<MechanicHolder> _holders = new();

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Holders"), LabelText("Auto-fill from children")]
        [SerializeField] protected bool _autoFillFromChildren = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Holders"), ShowIf(nameof(_autoFillFromChildren)), LabelText("Include inactive")]
        [SerializeField] protected bool _includeInactiveChildren = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Holders"), ShowIf(nameof(_autoFillFromChildren)), LabelText("Clear existing")]
        [SerializeField] protected bool _clearExistingChildren = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Holders"), ShowIf(nameof(_autoFillFromChildren)), LabelText("Children root (optional)")]
        [SerializeField] protected Transform _childrenRootOverride = null;

        // Selection
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Selection")]
        [SerializeField] protected HolderSelectMode _selectMode = HolderSelectMode.All;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Selection"), ShowIf("@_selectMode == Game.BossSystem.HolderSelectMode.RandomSome"), Min(1)]
        [SerializeField] protected int _randomSomeCount = 2;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Selection"), LabelText("Max per activation (0 = no cap)"), Min(0)]
        [SerializeField] protected int _maxPerActivation = 0;

        // Chance
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Chance"), LabelText("Chance per holder"), Range(0f, 1f)]
        [SerializeField] protected float _chancePerHolder = 1f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Chance"), LabelText("Guarantee ≥ 1")]
        [SerializeField] protected bool _guaranteeAtLeastOne = true;

        // Per-Holder Cooldown
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Per-Holder Cooldown"), LabelText("Min interval per holder"), Min(0f)]
        [SerializeField] protected float _minIntervalPerHolder = 0f;

        // Range
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range"), LabelText("Limit by range")]
        [SerializeField] protected bool _limitByRange = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range"), ShowIf(nameof(_limitByRange)), LabelText("Min range"), Min(0f)]
        [SerializeField] protected float _minRange = 0f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range"), ShowIf(nameof(_limitByRange)), LabelText("Max range"), Min(0f)]
        [SerializeField] protected float _maxRange = 25f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range"), LabelText("Distance origin")]
        [SerializeField] protected DistanceOriginSource _distanceOriginSource = DistanceOriginSource.Holder;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range"), ShowIf("@_distanceOriginSource == Game.BossSystem.DistanceOriginSource.CustomTransform"), LabelText("Custom origin")]
        [SerializeField] protected Transform _distanceOriginOverride;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range/Debug"), LabelText("Debug Show Ranges")]
        [SerializeField] private bool _debugShowRanges = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range/Debug"), ShowIf(nameof(_debugShowRanges)), LabelText("Range gizmo mode")]
        [SerializeField] private RangeGizmoMode _debugRangeMode = RangeGizmoMode.Sphere3D;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Range/Debug"), ShowIf(nameof(_debugShowRanges)), LabelText("Draw per holder")]
        [SerializeField] private bool _debugRangesPerHolder = true;

        // Angle
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle"), LabelText("Limit by angle to player")]
        [SerializeField] protected bool _limitByAngleToPlayer = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle"), ShowIf(nameof(_limitByAngleToPlayer)), LabelText("Min angle"), Min(0f)]
        [SerializeField] protected float _minAngle = 0f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle"), ShowIf(nameof(_limitByAngleToPlayer)), LabelText("Max angle"), Range(0f, 180f)]
        [SerializeField] protected float _maxAngle = 60f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle"), ShowIf(nameof(_limitByAngleToPlayer)), LabelText("Angle origin")]
        [SerializeField] protected AngleOriginSource _angleOriginSource = AngleOriginSource.Holder;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle"), ShowIf("@_angleOriginSource == Game.BossSystem.AngleOriginSource.CustomTransform && _limitByAngleToPlayer"), LabelText("Custom angle origin")]
        [SerializeField] protected Transform _angleOriginOverride;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle/Debug"), LabelText("Debug Show Angle")]
        [SerializeField] private bool _debugShowAngle = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle/Debug"), ShowIf(nameof(_debugShowAngle)), LabelText("Angle radius (for gizmo)"), Min(0.1f)]
        [SerializeField] private float _debugAngleRadius = 3f;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Angle/Debug"), ShowIf(nameof(_debugShowAngle)), LabelText("Draw per holder")]
        [SerializeField] private bool _debugAnglePerHolder = true;

        // Sight
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Sight"), LabelText("Require line of sight")]
        [SerializeField] protected bool _requireLineOfSight = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Sight"), ShowIf(nameof(_requireLineOfSight))]
        [SerializeField] protected LayerMask _losMask = ~0;

        // Global Cooldown
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Global Cooldown")]
        [SerializeField] protected bool _useGlobalCooldown = true;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Global Cooldown")]
        [SerializeField] protected bool _ignoreGlobalCooldown = false;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Global Cooldown")]
        [SerializeField] protected CooldownGroupSource _groupSource = CooldownGroupSource.AutoOwnerRoot;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Global Cooldown"), ShowIf("@_groupSource == Game.BossSystem.CooldownGroupSource.CustomTransform")]
        [SerializeField] protected Transform _customGroupTransform;

        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Global Cooldown"), ShowIf("@_groupSource == Game.BossSystem.CooldownGroupSource.CustomInt")]
        [SerializeField] protected int _customGroupId;

        // Debug
        [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Debug")]
        [SerializeField] private bool _debugLog = false;

        // [FoldoutGroup("Trigger"), ShowIf(nameof(_useTriggerLoop)), BoxGroup("Trigger/Debug")]
        // [SerializeField] private bool _debugGizmos = false;

        // Runtime (no ShowIf needed)
        protected SimpleTimer _timer;
        private int[] _indices;
        private float[] _holderNextReady;
        private int _rrIndex;
        private Transform _playerTf;
        private int _cachedGroupId;
        private bool _groupIdCached;


        #endregion


        // Public one-shot activation: evaluates selection & gates and executes immediately.
        // - ignoreGlobalCooldown: bypass group readiness & skip pushing CD
        // - ignorePerHolderCooldown: don't stamp per-holder cooldowns
        public virtual bool ActivateNow(bool ignoreGlobalCooldown = false, bool ignorePerHolderCooldown = false)
        {
            if (!CanAct) return false;

            // Global CD gate (optional bypass)
            if (_useGlobalCooldown && !_ignoreGlobalCooldown && !ignoreGlobalCooldown)
            {
                int gid = GetGroupId();
                if (gid != 0 && !ManagerMechanics.IsGroupReady(gid))
                    return false;
            }

            var targets = GetSelectedTargetsBuffer();
            targets.Clear();

            bool hadEligible = BuildTargetsDefault(targets);
            if (!hadEligible) return false;

            if (targets.Count == 0 && _guaranteeAtLeastOne)
            {
                int count = _holders != null ? _holders.Count : 0;
                int eligibleCount = 0;
                float now = Time.time;
                EnsureCaches();
                for (int i = 0; i < count; i++)
                    if (IsHolderEligibleBasic(i, now) && IsHolderEligibleExtra(i, now))
                        _indices[eligibleCount++] = i;

                if (eligibleCount > 0)
                {
                    int idx = _indices[UnityEngine.Random.Range(0, eligibleCount)];
                    targets.Add(_holders[idx]);
                }
            }

            if (targets.Count == 0) return false;

            ExecuteActivation(targets);

            // Per-holder cooldown (optional bypass)
            if (_minIntervalPerHolder > 0f && !ignorePerHolderCooldown)
            {
                float now = Time.time;
                for (int i = 0; i < targets.Count; i++)
                {
                    int idx = IndexOfHolder(targets[i]);
                    if (idx >= 0 && idx < _holderNextReady.Length)
                        _holderNextReady[idx] = now + _minIntervalPerHolder;
                }
            }

            // Push global cooldown (optional bypass)
            if (!ignoreGlobalCooldown)
            {
                float cd = GetActivationGlobalCooldownSeconds(targets);
                if (_useGlobalCooldown && !_ignoreGlobalCooldown && cd > 0f)
                {
                    int gid = GetGroupId();
                    if (gid != 0) ManagerMechanics.PushCooldown(gid, cd);
                }
            }

            return true;
        }

        private void InitHolders()
        {
            if (_autoFillFromChildren)
            {
                var arrayHolders = _childrenRootOverride.GetComponentsInChildren<MechanicHolder>(includeInactive: true);

                if (_clearExistingChildren) _holders.Clear();

                for (int i = 0; i < arrayHolders.Length; i++)
                {
                    var h = arrayHolders[i];
                    _holders.Add(h);
                }
                Debug.Log($"[MechanicTrigger] Auto-filled {_holders.Count} holders from children of '{(_childrenRootOverride != null ? _childrenRootOverride.name : name)}'");
            }
        }
        public override void OnMechanicInit()
        {
            Debug.Log($"[MechanicTrigger] Init '{name}' with {_holders.Count} holders, interval {_triggerInterval:0.00}s, mode {_selectMode}, chance {_chancePerHolder * 100f:0.##}%");
            InitHolders();
            _timer = new SimpleTimer(_triggerInterval);
            _playerTf = Ctx != null ? Ctx.Player : null;
            _groupIdCached = false;
            EnsureCaches();
            SanitizeRanges();
            SanitizeAngles();
        }

        public override void OnPhaseEnter()
        {
            if (_startDesynced)
            {
                float offset = UnityEngine.Random.value * _triggerInterval;
                _timer.Reset(_triggerInterval - offset);
            }
            else _timer.Reset(_triggerInterval);

            _playerTf = Ctx != null ? Ctx.Player : null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            SanitizeRanges();
            SanitizeAngles();
        }
#endif

        public override void Tick(float dt)
        {
            if (!_useTriggerLoop) return;

            if (!CanAct) return;
            _timer.UpdateTimer();
            if (!_timer.IsRoundCompleted) return;
            if (!ShouldAttemptThisTick()) return;

            //Debug.Log($"[MechanicTrigger] Tick '{name}' attempting activation");
            // Global cooldown gate 
            if (_useGlobalCooldown && !_ignoreGlobalCooldown)
            {
                int gid = GetGroupId();
                if (gid != 0 && !ManagerMechanics.IsGroupReady(gid))
                {
                    if (_debugLog) Debug.Log($"[{GetType().Name}] Blocked by global CD ({ManagerMechanics.GetRemaining(gid):0.00}s)");
                    return;
                }
            }

            var targets = GetSelectedTargetsBuffer();
            targets.Clear();
            bool anyEligiblePreChance = BuildTargetsDefault(targets);
            if (!anyEligiblePreChance) return; // nothing eligible at all

            if (targets.Count == 0 && _guaranteeAtLeastOne)
            {
                // force-pick one eligible
                int count = _holders != null ? _holders.Count : 0;
                int eligibleCount = 0;
                float now = Time.time;
                EnsureCaches();
                for (int i = 0; i < count; i++) if (IsHolderEligibleBasic(i, now) && IsHolderEligibleExtra(i, now)) _indices[eligibleCount++] = i;
                if (eligibleCount > 0)
                {
                    int idx = _indices[UnityEngine.Random.Range(0, eligibleCount)];
                    targets.Add(_holders[idx]);
                }
            }

            if (targets.Count == 0) return;

            ExecuteActivation(targets);

            // Stamp per-holder cooldown
            if (_minIntervalPerHolder > 0f)
            {
                float now = Time.time;
                for (int i = 0; i < targets.Count; i++)
                {
                    int idx = IndexOfHolder(targets[i]);
                    if (idx >= 0 && idx < _holderNextReady.Length)
                        _holderNextReady[idx] = now + _minIntervalPerHolder;
                }
            }

            // Push global cooldown (highest wins)
            float cd = GetActivationGlobalCooldownSeconds(targets);
            if (_useGlobalCooldown && !_ignoreGlobalCooldown && cd > 0f)
            {
                int gid = GetGroupId();
                if (gid != 0) ManagerMechanics.PushCooldown(gid, cd);
            }
        }

        /// <summary>Override for extra preconditions per tick.</summary>
        protected virtual bool ShouldAttemptThisTick() => true;

        /// <summary>Derived classes execute the actual effect (shoot, play anim, etc.).</summary>
        protected abstract void ExecuteActivation(List<MechanicHolder> targets);

        /// <summary>Override to set global cooldown contributed by this activation. Default: _triggerInterval.</summary>
        protected virtual float GetActivationGlobalCooldownSeconds(List<MechanicHolder> targets) => _triggerInterval;

        // --- Selection Core ---
        private bool BuildTargetsDefault(List<MechanicHolder> outTargets)
        {
            EnsureCaches();
            int count = _holders != null ? _holders.Count : 0;
            int eligibleCount = 0;
            float now = Time.time;

            for (int i = 0; i < count; i++)
            {
                if (IsHolderEligibleBasic(i, now) && IsHolderEligibleExtra(i, now))
                    _indices[eligibleCount++] = i;
            }

            if (eligibleCount == 0) return false; // none eligible at all

            switch (_selectMode)
            {
                case HolderSelectMode.All:
                    {
                        int added = 0;
                        for (int k = 0; k < eligibleCount; k++)
                        {
                            int idx = _indices[k];
                            if (PassChance())
                            {
                                outTargets.Add(_holders[idx]);
                                if (_maxPerActivation > 0 && ++added >= _maxPerActivation) break;
                            }
                        }
                        break;
                    }
                case HolderSelectMode.RandomOne:
                    {
                        int pick = _indices[UnityEngine.Random.Range(0, eligibleCount)];
                        if (PassChance()) outTargets.Add(_holders[pick]);
                        break;
                    }
                case HolderSelectMode.RandomSome:
                    {
                        int want = Mathf.Clamp(_randomSomeCount, 1, eligibleCount);
                        int cap = _maxPerActivation > 0 ? Mathf.Min(_maxPerActivation, want) : want;
                        for (int n = 0; n < cap; n++)
                        {
                            int r = UnityEngine.Random.Range(n, eligibleCount);
                            int tmp = _indices[n]; _indices[n] = _indices[r]; _indices[r] = tmp;
                            int idx = _indices[n];
                            if (PassChance()) outTargets.Add(_holders[idx]);
                        }
                        break;
                    }
                case HolderSelectMode.RoundRobin:
                    {
                        int added = 0;
                        for (int step = 0; step < eligibleCount; step++)
                        {
                            int i = (_rrIndex + step) % eligibleCount;
                            int idx = _indices[i];
                            if (PassChance())
                            {
                                outTargets.Add(_holders[idx]);
                                if (_maxPerActivation > 0 && ++added >= _maxPerActivation) break;
                                _rrIndex = (i + 1) % eligibleCount;
                            }
                        }
                        if (eligibleCount > 0) _rrIndex %= eligibleCount;
                        break;
                    }
            }

            return true; // had eligible before chance
        }

        /// <summary>Subclass hook for extra holder conditions (e.g., custom line-of-fire).</summary>
        protected virtual bool IsHolderEligibleExtra(int idx, float now) => true;

        private bool IsHolderEligibleBasic(int idx, float now)
        {
            if (_holders == null || idx < 0 || idx >= _holders.Count) return false;
            if (_holders[idx] == null) return false;
            if (_holderNextReady != null && idx < _holderNextReady.Length && now < _holderNextReady[idx]) return false;

            // Distance gate
            if (_limitByRange && _playerTf != null)
            {
                Transform distOriginTf = GetDistanceOriginTransform(_holders[idx].transform);
                Vector3 originPos = distOriginTf ? distOriginTf.position : _holders[idx].transform.position;
                Vector3 toPlayer = _playerTf.position - originPos;
                float distSqr = toPlayer.sqrMagnitude;

                float minSqr = _minRange <= 0f ? 0f : _minRange * _minRange;
                float maxSqr = _maxRange <= 0f ? 0f : _maxRange * _maxRange;

                if (distSqr < minSqr) return false;
                if (_maxRange > 0f && distSqr > maxSqr) return false;

                // Angle gate (XZ-plane) – uses its own origin/forward
                if (_limitByAngleToPlayer)
                {
                    Transform angOriginTf = GetAngleOriginTransform(_holders[idx].transform);
                    Vector3 angOriginPos = angOriginTf ? angOriginTf.position : _holders[idx].transform.position;
                    Vector3 d = _playerTf.position - angOriginPos; d.y = 0f;
                    Vector3 f = (angOriginTf ? angOriginTf : _holders[idx].transform).forward; f.y = 0f;
                    if (d.sqrMagnitude > 0.0001f && f.sqrMagnitude > 0.0001f)
                    {
                        float ang = Vector3.Angle(f.normalized, d.normalized); // 0..180
                        if (ang < _minAngle) return false;
                        if (_maxAngle > 0f && ang > _maxAngle) return false;
                    }
                }

                // LOS from distance origin position
                if (_requireLineOfSight)
                {
                    Vector3 origin = originPos + Vector3.up * 0.25f;
                    Vector3 target = _playerTf.position + Vector3.up * 0.5f;
                    if (Physics.Linecast(origin, target, out RaycastHit hit, _losMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform != _playerTf) return false;
                    }
                }
            }

            return true;
        }

        private bool PassChance()
        {
            if (_chancePerHolder >= 1f) return true;
            if (_chancePerHolder <= 0f) return false;
            return UnityEngine.Random.value <= _chancePerHolder;
        }

        protected List<MechanicHolder> GetSelectedTargetsBuffer()
        {
            if (_selectedBuffer == null) _selectedBuffer = new List<MechanicHolder>(Mathf.Max(4, _holders.Count));
            return _selectedBuffer;
        }
        private List<MechanicHolder> _selectedBuffer;

        private int GetGroupId()
        {
            if (!_groupIdCached)
            {
                _cachedGroupId = ManagerMechanics.ComputeGroupId(transform, _customGroupTransform, _customGroupId, _groupSource);
                _groupIdCached = true;
            }
            return _cachedGroupId;
        }

        protected int IndexOfHolder(MechanicHolder holder)
        {
            int count = _holders != null ? _holders.Count : 0;
            for (int i = 0; i < count; i++) if (_holders[i] == holder) return i;
            return -1;
        }

        private void EnsureCaches()
        {
            int count = _holders != null ? _holders.Count : 0;
            if (_indices == null || _indices.Length < count) _indices = new int[Mathf.Max(8, count)];
            if (_holderNextReady == null || _holderNextReady.Length != count) _holderNextReady = new float[count];
        }

        private void SanitizeRanges()
        {
            if (_minRange < 0f) _minRange = 0f;
            if (_maxRange < 0f) _maxRange = 0f;
            if (_maxRange > 0f && _minRange > _maxRange) _minRange = _maxRange;
        }

        private void SanitizeAngles()
        {
            if (_minAngle < 0f) _minAngle = 0f;
            if (_maxAngle < 0f) _maxAngle = 0f;
            if (_maxAngle > 0f && _minAngle > _maxAngle) _minAngle = _maxAngle;
            if (_minAngle > 180f) _minAngle = 180f;
            if (_maxAngle > 180f) _maxAngle = 180f;
        }

        private Transform GetDistanceOriginTransform(Transform holderTf)
        {
            switch (_distanceOriginSource)
            {
                case DistanceOriginSource.MechanicSelf:
                    return transform;
                case DistanceOriginSource.OwnerRoot:
                    return ManagerMechanics.FindOwnerRoot(transform);
                case DistanceOriginSource.CustomTransform:
                    return _distanceOriginOverride != null ? _distanceOriginOverride : holderTf;
                case DistanceOriginSource.Holder:
                default:
                    return holderTf;
            }
        }

        private Transform GetAngleOriginTransform(Transform holderTf)
        {
            switch (_angleOriginSource)
            {
                case AngleOriginSource.MechanicSelf:
                    return transform;
                case AngleOriginSource.OwnerRoot:
                    return ManagerMechanics.FindOwnerRoot(transform);
                case AngleOriginSource.CustomTransform:
                    return _angleOriginOverride != null ? _angleOriginOverride : holderTf;
                case AngleOriginSource.Holder:
                default:
                    return holderTf;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            // Range gizmos (independent of _debugGizmos)
            if (_debugShowRanges && _limitByRange)
            {
                if (_debugRangesPerHolder && _holders != null)
                {
                    for (int i = 0; i < _holders.Count; i++)
                    {
                        var h = _holders[i];
                        if (h == null) continue;
                        var t = GetDistanceOriginTransform(h.transform);
                        DrawRangeGizmosAt(t != null ? t.position : h.transform.position);
                    }
                }
                else
                {
                    Transform t = GetDistanceOriginTransform(transform);
                    DrawRangeGizmosAt(t != null ? t.position : transform.position);
                }
            }

            // Angle gizmos (XZ wedge)
            if (_debugShowAngle && _limitByAngleToPlayer)
            {
                if (_debugAnglePerHolder && _holders != null)
                {
                    for (int i = 0; i < _holders.Count; i++)
                    {
                        var h = _holders[i];
                        if (h == null) continue;
                        var t = GetAngleOriginTransform(h.transform);
                        DrawAngleGizmoAt(t != null ? t : h.transform);
                    }
                }
                else
                {
                    Transform t = GetAngleOriginTransform(transform);
                    DrawAngleGizmoAt(t != null ? t : transform);
                }
            }
        }

        private void DrawRangeGizmosAt(Vector3 center)
        {
            // 3D spheres
            if (_debugRangeMode == RangeGizmoMode.Sphere3D || _debugRangeMode == RangeGizmoMode.Both)
            {
                Gizmos.matrix = Matrix4x4.identity;
                if (_minRange > 0f)
                {
                    Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
                    Gizmos.DrawWireSphere(center, _minRange);
                }
                if (_maxRange > 0f)
                {
                    Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
                    Gizmos.DrawWireSphere(center, _maxRange);
                }
            }
#if UNITY_EDITOR
            // 2D circles on XZ plane (editor only via Handles)
            if (_debugRangeMode == RangeGizmoMode.CircleXZ || _debugRangeMode == RangeGizmoMode.Both)
            {
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                if (_minRange > 0f)
                {
                    UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                    UnityEditor.Handles.DrawWireDisc(center, Vector3.up, _minRange);
                }
                if (_maxRange > 0f)
                {
                    UnityEditor.Handles.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                    UnityEditor.Handles.DrawWireDisc(center, Vector3.up, _maxRange);
                }
            }
#endif
        }

        private void DrawAngleGizmoAt(Transform origin)
        {
#if UNITY_EDITOR
            if (origin == null) return;
            float radius = _maxRange > 0f ? _maxRange : Mathf.Max(_minRange, _debugAngleRadius);
            Vector3 center = origin.position;
            Vector3 forward = origin.forward; forward.y = 0f; if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            var normal = Vector3.up;
            var fromMin = Quaternion.Euler(0f, -Mathf.Max(0f, _minAngle), 0f) * forward;
            var fromMax = Quaternion.Euler(0f, -Mathf.Max(0f, _maxAngle), 0f) * forward;

            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            // Min angle arc
            if (_minAngle > 0f)
            {
                UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                UnityEditor.Handles.DrawWireArc(center, normal, fromMin, _minAngle * 2f, radius);
            }
            // Max angle arc (if set)
            if (_maxAngle > 0f)
            {
                UnityEditor.Handles.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                UnityEditor.Handles.DrawWireArc(center, normal, fromMax, _maxAngle * 2f, radius);
            }

            // Direction rays
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.DrawLine(center, center + (Quaternion.Euler(0f, _minAngle, 0f) * forward) * radius);
            UnityEditor.Handles.DrawLine(center, center + (Quaternion.Euler(0f, -_minAngle, 0f) * forward) * radius);
            if (_maxAngle > 0f)
            {
                UnityEditor.Handles.DrawLine(center, center + (Quaternion.Euler(0f, _maxAngle, 0f) * forward) * radius);
                UnityEditor.Handles.DrawLine(center, center + (Quaternion.Euler(0f, -_maxAngle, 0f) * forward) * radius);
            }
#endif
        }
#endif

        // ---------------- Editor Helper ----------------
#if UNITY_EDITOR
        [Button]
        [GUIColor(0.5f, 1f, 0.5f)]
        [BoxGroup("Ability Shooter", ShowLabel = false)]
        private void FindAllAbilityHolders()
        {
            Transform root = transform;
            for (int i = 0; i < 10; i++)
            {
                if (root == null) break;

                if (root.GetComponent<IOwner>() == null)
                {
                    root = root.parent;
                    continue;
                }

                break;
            }
            Debug.Log($"Finding all ability holders in: {root.name}");
            _holders ??= new List<MechanicHolder>(8);
            _holders.Clear();
            root.TraverseChildren<MechanicHolder>(_holders);
        }
#endif
    }
}
