using System;
using System.Collections.Generic;
using System.Linq;
using Game.StatSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.SkillSystem
{
    [CreateAssetMenu(menuName = "Skills/SkillDefinition")]
    public class SkillDefinition : SerializedScriptableObject, IHasName
    {
        [Title("Texture")]
        [PreviewField(60), HideLabel]
        [HorizontalGroup("Split", 60)]
        public Texture2D Icon; // IDK this might be good to be sprite?? Does it matter?

        [VerticalGroup("Split/Right"), LabelWidth(100), Space(30)]
        public string Name;

        [Space(10)]
        public string DescriptionSmall;
        [Space(10)]
        [TextArea] public string Description;

        public SKILL_NAME SkillName;


        [Space(10)]
        [Title("Skill Assembly")]
        [TypeFilter(nameof(GetFilteredTypeList))]
        [SerializeField] private Skill _skill; //if we want to use ISkill, we need to little bit change the code

        //<summary> User that has this skill </summary>
        private GameObject _user;

        public void SetUser(GameObject user)
        {
            _user = user;
        }

        public void UpdateAbilityData()
        {
            if(_user == null || _skill == null)
            {
                return;
            }

            _skill.SetUser(_user);
            _skill.SetSkillName(SkillName);
            _skill.SetInstanceID(GetInstanceID());
            if(_user.TryGetComponent(out IOwner owner))
            {
                _skill.SetOwner(owner.GetOwnerType());
            }
        }
        
        public void ClearModifiers()
        {
            if(_skill == null)
            {
                Debug.LogWarning("Skill is null, cannot clear modifiers.");
                return;
            }
            
            _skill.ClearModifiers();
        }

        public void UseSkill(int slot = -1)
        {
            if (Player.Instance == null)
            {
                return;
            }

            if (ManagerSkills.Instance == null)
            {
                Debug.LogWarning("ManagerSkills is not initialized. Using skill without manager!, without logic");
                _skill.StartSkill();
                return;
            }
            _skill.SetSkillName(SkillName);
            _skill.SetInstanceID(GetInstanceID());
            ManagerSkills.Instance.ExecuteSkillDefinition(this, slot);
        }

        public void AddModifier(Modifier modifier)
        {
            if(_skill == null)
            {
                Debug.LogWarning("Skill is null, cannot add modifier.");
                return;
            }
            
            _skill.AddModifier(modifier);
        }

        public ISkill GetSkill()
        {
            return _skill;
        }

        public string GetName()
        {
            return Name;
        }

        public SkillDefinition Clone()
        {
            //Debug.Log("===== Cloning skill definition, current instance id: " + this.GetInstanceID());
            SkillDefinition clonedSkillDef = Instantiate(this);
            //Debug.Log("===== ||||Cloned instance id: " + clonedSkillDef.GetInstanceID());
            if(clonedSkillDef._skill != null)
            {
                clonedSkillDef._skill.Name = this.Name;

                Skill clonedSkill = (Skill)SerializationUtility.CreateCopy(this._skill);
                clonedSkillDef._skill = clonedSkill;
            }

            return clonedSkillDef;
        }

#region Odin
        public IEnumerable<Type> GetFilteredTypeList()
        {
            return typeof(Skill).Assembly.GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => typeof(Skill).IsAssignableFrom(t));
        }

        
        #endregion

    }
}