using UnityEngine;
using Sirenix.OdinInspector;
using Game.SkillSystem;

[CreateAssetMenu(menuName = "Bosses/Phase Definition")]
public sealed class BossPhaseDefinition : ScriptableObject
{
    [SerializeField] private string _displayName;

    [Title("Trigger")]
    [SerializeField] private PHASE_TRIGGER_TYPE _triggerType = PHASE_TRIGGER_TYPE.OnHPPercentDown;
    [ShowIf("_triggerType", PHASE_TRIGGER_TYPE.OnHPPercentDown)]
    [SerializeField, Range(0f, 1f)] private float _hpPercentThreshold = 0.75f;
    [ShowIf("_triggerType", PHASE_TRIGGER_TYPE.AfterTime)]
    [SerializeField, Min(0f)] private float _timeSinceStart;
    [ShowIf("_triggerType", PHASE_TRIGGER_TYPE.OnEvent)]
    [SerializeField] private string _eventKey; // emitted by abilities/world

    [Title("Abilities")]
    [SerializeField]
    private SkillDefinition[] _skills; // weighted list

    [Title("Modifiers")]
    [SerializeField]
    private float _moveSpeedMult = 1f;
    [SerializeField] private float _damageMult = 1f;

    [Title("Flags")][SerializeField] private bool _spawnAdds;
    [ShowIf("_spawnAdds")][SerializeField] private int _addsCount;

    public string DisplayName => _displayName;
    public PHASE_TRIGGER_TYPE TriggerType => _triggerType;
    public float HPPercentThreshold => _hpPercentThreshold;
    public float TimeSinceStart => _timeSinceStart;
    public string EventKey => _eventKey;
    public SkillDefinition[] Skills => _skills;
    public float MoveSpeedMult => _moveSpeedMult;
    public float DamageMult => _damageMult;
    public bool SpawnAdds => _spawnAdds;
    public int AddsCount => _addsCount;
}