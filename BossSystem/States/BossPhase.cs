using System;
using UnityEngine;
namespace Game.BossSystem
{
    /// <summary>
    /// A single phase in a boss fight. Configure which mechanics are enabled/disabled,
    /// what behaviors run while active, and what transitions can trigger the next phase.
    /// </summary>
    [Serializable]
    public sealed class BossPhase
    {
        [Tooltip("Optional label for clarity.")] public string name;

        [Tooltip("Mechanic toggles applied when entering this phase.")]
        public MechanicToggle[] toggles;

        [Tooltip("Transitions checked every frame to leave this phase.")]
        public PhaseTransition[] transitions;
    }
}