namespace Game.SkillSystem
{
    /// <summary>
    /// The Skill Executate handler will use the same instance of the skill, so it will not create a new one.
    /// so it doesn't trigger for example Skill_SingleAbility StartSkill method. Only the ability StartSkill method will be called.
    /// Might be use full to save performance, but not sure if its worth it.
    /// </summary>
    public interface IReUseSkill
    {

    }
}
