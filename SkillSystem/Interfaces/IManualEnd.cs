namespace Game.SkillSystem
{
    /// <summary>
    /// If the skill has a manually ending logic, it should implement this interface
    /// The skill duration stat is now free to be used for other purposes, due to it will replace it to 99999
    /// </summary>
    public interface IManualEnd
    {
    }
}