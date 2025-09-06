using System;
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
    /// A compact, serializable group of conditions. Useful to gate mechanics or transitions
    /// without making lots of small components.
    /// </summary>
    [Serializable]
    public sealed class ConditionGroup
    {
        [Tooltip("If true, ALL conditions must pass (AND). If false, ANY passing is enough (OR).")]
        public bool requireAll = true;

        [SerializeField] private Condition[] _items = Array.Empty<Condition>();

        public bool Evaluate(BossContext ctx)
        {
            int len = _items != null ? _items.Length : 0;
            if (len == 0)
            {
                return true; // no conditions == always true
            }

            if (requireAll)
            {
                for (int i = 0; i < len; i++)
                {
                    if (!_items[i].Evaluate(ctx)) return false;
                }
                return true;
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (_items[i].Evaluate(ctx)) return true;
                }
                return false;
            }
        }
    }
}
