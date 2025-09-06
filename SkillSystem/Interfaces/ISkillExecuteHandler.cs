using System.Collections.Generic;

namespace Game.SkillSystem
{
    public interface ISkillExecuteHandler
    {
        /// <summary>
        /// Called by the skill system to execute a skill.
        /// </summary>
        /// <param name="skill">The skill to execute.</param>
        public void ExecuteSkill(SkillDefinition skill, List<MechanicHolder> mechanicHolders);

    }
}