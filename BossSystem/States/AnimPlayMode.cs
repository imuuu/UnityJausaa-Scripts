// Framework additions:
// - ManagerMechanics: global, static group cooldowns by groupId (int)
// - MechanicTrigger (abstract): base for trigger-style mechanics (shooters, animations, etc.)
//   * Holds optional MechanicHolders
//   * Integrates with global cooldown groups
//   * Provides timed triggering via SimpleTimer and hooks for building targets + executing
// - AbilityShooterMechanic : MechanicTrigger (refactored)
// - AnimationTriggerMechanic : MechanicTrigger (new)
//
// Notes:
// - "Highest cooldown wins": pushing cooldown extends group ready time to the MAX of current vs (now + duration)
// - You can ignore global cooldown per mechanic
// - Group id sources: AutoOwnerRoot (default), CustomTransform, CustomInt, or None

namespace Game.BossSystem
{
    public enum AnimPlayMode : byte { PlayClip, CrossFadeState }
}
