namespace Game.SkillSystem
{
public interface ISkillGroupItemTrigger
{
    public int SkillGroupItemID { get; }
    /// <summary>
    /// Called when skill group item
    /// </summary>
    public void OnSkillGroupItemTrigger(ISkillEventContext context);

}
}
