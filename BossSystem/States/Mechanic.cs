using Sirenix.OdinInspector;
using UnityEngine;

// ------------------------------------------------------------
// Boss Phase System (MonoBehaviours + Plain C# classes)
// - No ScriptableObjects needed.
// - Add BossPhaseController to your Boss prefab.
// - Add BossMechanic-derived components for individual abilities.
// - Configure Phases array in the inspector: which mechanics are
//   enabled/disabled per phase and what transitions trigger the next phase.
// - All runtime code avoids per-frame GC allocations (no LINQ, lists reused).
// ------------------------------------------------------------

namespace Game.BossSystem
{
    /// <summary>
    /// Base class for all boss mechanics/abilities. Add derived components to the boss.
    /// The controller will enable/disable them per phase.
    /// </summary>
    public abstract class Mechanic : MonoBehaviour
    {
        [FoldoutGroup("Condition"), SerializeField][SuppressInvalidAttributeError]
        private bool _useConditions = false;

        [FoldoutGroup("Condition"), ShowIf(nameof(_useConditions))]
        [SerializeField] private ConditionGroup _runWhen;

        [FoldoutGroup("Condition"), SerializeField]
        private bool _alwaysTick = false;

        private bool _active;           // set by phase toggles
        protected BossContext Ctx;      // provided by controller

        /// <summary>True if this mechanic is allowed to act this frame (phase enabled AND conditions pass).</summary>
        protected bool CanAct
        {
            get
            {
                if (!_active) return false;
                if (_runWhen == null) return true;
                return _runWhen.Evaluate(Ctx);
            }
        }

        internal void __SetContext(BossContext ctx) => Ctx = ctx;
        internal void __SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            if (_active) OnMechanicEnabled(); else OnMechanicDisabled();
        }

        internal bool __AlwaysTick => _alwaysTick;

        /// <summary>Called when the controller first initializes this mechanic.</summary>
        public virtual void OnMechanicInit() { }
        /// <summary>Called when the phase enables this mechanic.</summary>
        protected virtual void OnMechanicEnabled() { }
        /// <summary>Called when the phase disables this mechanic.</summary>
        protected virtual void OnMechanicDisabled() { }
        /// <summary>Called every frame. You MUST read CanAct if you only want to act while enabled.</summary>
        public abstract void Tick(float dt);
        /// <summary>Optional fixed-step logic.</summary>
        public virtual void FixedTick(float fixedDt) { }
        /// <summary>Optional: clear internal cooldowns/state when a phase starts.</summary>
        public virtual void OnPhaseEnter() { }
        /// <summary>Optional: cleanup when a phase exits.</summary>
        public virtual void OnPhaseExit() { }
    }
}
