using UnityEngine;
using Game.Mobs;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Game.Extensions;
using Game.SkillSystem;

public class BossController : Mob, IBoss
{
    [SerializeField] private bool _debug;
    [OdinSerialize, SerializeReference]
    private List<BossAbilityHolder> _abilityHolders;

    private ManagerBosses _manager;
    private BossDefinition _def;
    private BossPhaseRunner _phaseRunner;
    private BossAbilityRunner _abilityRunner;

    private MobData _mobData;

    private IHealth _health;

    public BossDefinition Definition => _def;

    [Title("Skill Execute Handler")]
    [ShowInInspector, ReadOnly]
    private SkillExecuteHandler _skillExecuteHandler;

    private IOwner _owner;

    private void Awake()
    {
        _health = GetComponent<IHealth>();
        _skillExecuteHandler = new SkillExecuteHandler();
        _owner = GetComponent<IOwner>();
    }

    private void OnEnable()
    {
        _health.OnDeath += Die;
    }

    private void OnDisable()
    {
        _health.OnDeath -= Die;
    }

    private void Update()
    {
        if (ManagerPause.IsPaused()) return;

        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

        _skillExecuteHandler.UnityUpdate();
    }

    public void Initialize(BossDefinition def, ManagerBosses mgr)
    {
        _def = def; _manager = mgr;
        _mobData = def.MobData;
        _phaseRunner = new BossPhaseRunner(def.Phases, this);
        _abilityRunner = new BossAbilityRunner(this);
    }

    public void AITick(float dt)
    {
        // Advance phase & apply phase modifiers
        _phaseRunner.Tick(this, dt);
        ApplyPhaseModifiers(_phaseRunner);

        // Ability scheduling
        _abilityRunner.Tick(dt);
    }

    private void ApplyPhaseModifiers(BossPhaseRunner pr)
    {
        //_moveSpeedMult = pr.CurrentMoveSpeedMult; _damageMult = pr.CurrentDamageMult;
    }


    private void Die()
    {
        ManagerBosses.Instance.OnBossDied(this);
        //Destroy(gameObject);
    }

    public float GetHPPercent()
    {
        //return _health.GetHealth() / _health.GetMaxHealth();
        return Mathf.Clamp01(_health.GetHealth() / _health.GetMaxHealth());
    }

    public void ExecuteSkill(SkillDefinition skillDefinition)
    {
        Debug.Log($"Executing ability: {skillDefinition.name} on boss: {name}, holders count: {_abilityHolders.Count}");
        Debug.Log($"Skill {skillDefinition.GetSkill()}");
        var ability = (skillDefinition.GetSkill() as Skill_SingleAbility).GetAbility();
        List<BossAbilityHolder> holders = new();

        
        foreach (var holder in _abilityHolders)
        {
            var holderAbilities = holder.GetAbilities();
            foreach (var holderAbility in holderAbilities)
            {
                //Debug.Log($"Checking holder: {holder.name} for ability: {holderAbility.GetType().Name} vs {ability.GetType().Name}");
                Debug.Log($"HolderAbility: {holderAbility}");
                Debug.Log($"Ability: {ability}");
                if (holderAbility.GetType() == ability.GetType())
                {
                    holders.Add(holder);
                    Debug.Log($"Found holder: {holder.name} for ability: {holderAbility.GetType().Name}");
                    break;
                }
            }
        }

        Debug.Log($"Found {holders.Count} holders for ability: {ability.GetType().Name}");

        foreach (var holder in holders)
        {
            SkillDefinition clonedSkill = skillDefinition.Clone();
            //clonedSkill.SetOwner(_owner.GetOwnerType());
            clonedSkill.SetUser(holder.gameObject);
            clonedSkill.UpdateAbilityData();
            //clonedSkill.SetInstanceID(holder.GetInstanceID());
            clonedSkill.UseSkill(-1, _skillExecuteHandler);
        }

    }

    [Button]
    [GUIColor(0.5f, 1f, 0.5f)]
    private void FindAllAbilityHolders()
    {
        _abilityHolders.Clear();
        transform.TraverseChildren<BossAbilityHolder>(_abilityHolders);
    }
}