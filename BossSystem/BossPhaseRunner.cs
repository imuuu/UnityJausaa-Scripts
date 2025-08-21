using Game.SkillSystem;

public class BossPhaseRunner
{
    private readonly BossPhaseDefinition[] _phases;
    private int _index;
    private float _timeSinceStart;
    private float _timeInPhase;
    private BossPhaseDefinition _current;

    private BossController _bossController;
    public BossPhaseRunner(BossPhaseDefinition[] phases, BossController bossController)
    {
        _phases = phases;
        _index = -1;
        _current = null;
        _bossController = bossController;
    }

    public void Tick(BossController boss, float dt)
    {
        _timeSinceStart += dt; _timeInPhase += dt;
        if (_current == null) { Advance(boss); return; }

        // Check next phase trigger
        if (TryShouldAdvance(boss)) Advance(boss);
    }

    private bool TryShouldAdvance(BossController boss)
    {
        // End when no more phases
        if (_index >= _phases.Length - 1) return false;
        var next = _phases[_index + 1];
        switch (next.TriggerType)
        {
            case PHASE_TRIGGER_TYPE.OnHPPercentDown: return boss.GetHPPercent() <= next.HPPercentThreshold;
            case PHASE_TRIGGER_TYPE.AfterTime: return _timeSinceStart >= next.TimeSinceStart;
            case PHASE_TRIGGER_TYPE.OnEvent: return false; // hook event bus
        }
        return false;
    }

    private void Advance(BossController boss)
    {
        _index++;
        if (_index < _phases.Length) _current = _phases[_index]; else { _current = null; return; }
        _timeInPhase = 0f;

        var skills = _current.Skills;

        foreach (var skillDef in skills)
        {
            boss.ExecuteSkill(skillDef);
        }
    }
}