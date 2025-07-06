using Game.StatSystem;
using UnityEngine;

namespace Game.SkillSystem
{
    public interface ISkill
    {
        public SKILL_NAME GetSkillName();
        public void SetSkillName(SKILL_NAME skillName);
        public bool IsSkillUsable();
        public int GetInstanceID();
        public void SetInstanceID(int instanceID);

        public bool IsRecastable();

#region Getters and Setters
        public GameObject GetUser();
        public void SetUser(GameObject user);

        public int GetSlot();
        public void SetSlot(int slot);
        public void AddModifier(Modifier modifier);

        public void ClearModifiers();

        //<summary> Returns second of the SkillDefinition, meaning this isn't SkillDefinition! Most cases its Skill_SingleAbility</summary>
        public ISkill GetRootSkill();
        public void SetRootSkill(ISkill rootSkill); 

        public void EndTheSkill();

#endregion Getters and Setters

        #region Logic
        public bool HasAwaken();
        //<summary> Awake the skill always when skill is created</summary>
        public void AwakeSkill();

        //<summary> Start the skill always when skill is started</summary>
        public void StartSkill();

        //<summary> Update the skill on Unity Update</summary>
        public void UpdateSkill();

        //<summary> End the skill always when skill is ended</summary>
        public void EndSkill();
        #endregion Logic

    }
}
