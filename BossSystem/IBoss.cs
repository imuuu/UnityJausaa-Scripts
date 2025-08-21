public interface IBoss
{
    void Initialize(BossDefinition def, ManagerBosses mgr);
    float GetHPPercent();
}

public interface IBossPhase
{
    void OnEnter(BossController boss);
    void Tick(float dt);
    void OnExit();
}
public interface IBossAbility
{
    void Initialize(BossController boss, BossAbilityDefinition def);
    bool CanFire(float timeNow);
    void Fire(float timeNow);
    void Tick(float dt);
    float GetCooldown();
}