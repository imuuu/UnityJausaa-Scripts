using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.SkillSystem
{
    [DisallowMultipleComponent]
    public class SkillController : MonoBehaviour
    {
        private ManagerSkills _managerSkills => ManagerSkills.Instance;
        [Title("Assigned Skills")]
        [SerializeField] private List<SkillDefinition> _assignedSkills = new();

        [Title("Current Skills"), Space(10)]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        [SerializeField, ReadOnly]
        private List<SkillDefinition> _skills = new();

        private void Start()
        {
            if (IsPlayer())
            {
                _managerSkills.SetPlayer(gameObject);
                GetPlayerSkills();
                return;
            }

            // var skills = new List<SkillDefinition>(_assignedSkills);
            // _assignedSkills.Clear();
            CloneAndAssignSkills(_assignedSkills);
        }

        [Button("Use Skill")]
        public void UseSkill(int index)
        {
            if (index < 0 || index >= _skills.Count)
            {
                Debug.LogError("Invalid index");
                return;
            }

            _skills[index].UseSkill();
        }

        private void GetPlayerSkills()
        {
            _skills = _managerSkills.GetPlayerSkills();
        }

        //<summary> Clone and assign skills to the mob. NOT PLAYER! </summary>
        private void CloneAndAssignSkills(List<SkillDefinition> skills)
        {
            if (skills == null)
                return;

            foreach (SkillDefinition skill in skills)
            {
                SkillDefinition clonedDef = skill.Clone();
                clonedDef.SetUser(gameObject);
                clonedDef.UpdateAbilityData();
                _skills.Add(clonedDef);
            }
        }

        private bool IsPlayer()
        {
            return GetComponent<Player>() != null;
        }

    }
}
