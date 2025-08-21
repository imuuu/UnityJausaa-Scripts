using UnityEngine;
using System.Collections.Generic;

public sealed class BossAbilityRunner
{
    private readonly BossController _boss;
    private readonly Dictionary<ABILITY_ID, float> _cooldowns = new Dictionary<ABILITY_ID, float>(16);
    private BossPhaseDefinition _cachedPhase;

    public BossAbilityRunner(BossController boss) { _boss = boss; }

    public void Tick(float dt)
    {
        // var phase = _boss.Definition.Phases.Length > 0 ? _boss.Definition.Phases[0] : null; // replace with runner’s current
        // // In a full hookup, read from BossPhaseRunner’s _current
        // if (phase == null) return;
        // if (!ReferenceEquals(_cachedPhase, phase)) { _cachedPhase = phase; /* reset phase state if needed */ }

        // // Cooldowns
        // var list = phase.Skills; if (list == null || list.Length == 0) return;
        // for (int i = 0; i < list.Length; i++)
        // {
        //     var def = list[i]; float t;
        //     if (_cooldowns.TryGetValue(def.Id, out t)) t -= dt; else t = 0f;
        //     if (t <= 0f)
        //     {
        //         // Weighted pick by simple roulette
        //         if (Roll(def.Weight))
        //         {
        //             Fire(def);
        //             t = def.Cooldown;
        //         }
        //     }
        //     _cooldowns[def.Id] = t;
        // }
    }

    private bool Roll(float weight) { return Random.value <= weight; }

    private void Fire(BossAbilityDefinition def)
    {
        // switch (def.Id)
        // {
        //     // case ABILITY_ID.BeamSweep: Ability_BeamSweep.Fire(_boss, def); break;
        //     // case ABILITY_ID.OrbVolley: Ability_OrbVolley.Fire(_boss, def); break;
        //     // case ABILITY_ID.Slam: Ability_Slam.Fire(_boss, def); break;
        //     // case ABILITY_ID.Nova: Ability_Nova.Fire(_boss, def); break;
        // }
    }
}