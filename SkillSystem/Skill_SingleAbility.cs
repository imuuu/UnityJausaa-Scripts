using System.Collections.Generic;
using Game.StatSystem;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.SkillSystem
{
    public class Skill_SingleAbility : Skill
    {
        [Space(10)]
        [Title("Choose Ability")]
        [OdinSerialize, SerializeReference]
        [SerializeField] private IAbility _ability;

        private IAbility _createdAbility;
        private List<Modifier> _holdModifiers;
        public override void AwakeSkill()
        {
            base.AwakeSkill();

            //Debug.Log("Awakening Skill_SingleAbility");
            if (_ability is IDuration duration)
            {
                SetDuration(duration.GetDuration());
            }

            if (_ability is ICooldown cooldown)
            {
                SetCooldown(cooldown.GetCooldown());
            }

            _createdAbility = (IAbility)SerializationUtility.CreateCopy(_ability);
            _createdAbility.SetRootSkill(this);
            _createdAbility.SetUser(GetUser());
            _createdAbility.SetLaunchUser(GetLaunchUser());

            if (_createdAbility is IOwner owner)
                owner.SetOwner(GetOwnerType());

            _createdAbility.SetSkillName(GetSkillName());
            _createdAbility.SetInstanceID(GetInstanceID());
            _createdAbility.AwakeSkill();

            if (_createdAbility is IDuration durationAbility && GetDuration() != durationAbility.GetDuration())
            {
                Debug.Log("========> Setting duration to: " + GetDuration() + " from: " + durationAbility.GetDuration());
                SetDuration(durationAbility.GetDuration());
            }


            if (_holdModifiers == null)
                return;

            foreach (Modifier modifier in _holdModifiers)
            {
                _createdAbility.AddModifier(modifier);
            }

            _holdModifiers = null;

        }
        public override void StartSkill()
        {
            IAbility spawnedAbility = null;
            if (_createdAbility is IStaticSkill)
            {
                Debug.Log("CREATING STATIC ABILITY");
                spawnedAbility = _createdAbility;
            }
            else
            {
                //Debug.Log("CREATING NEW ABILITY");
                spawnedAbility = (IAbility)SerializationUtility.CreateCopy(_createdAbility);
            }

            // else
            // {
            //     CreateNewAbility();
            //     spawnedAbility = _createdAbility;
            // }

            spawnedAbility.SetSlot(GetSlot());
            //Debug.Log("Starting Skill_SingleAbility: with skill slot: " + GetSlot());
            ManagerSkills.Instance.ExecuteSkill(spawnedAbility);
        }

        // private void CreateNewAbility()
        // {
        //     if (_ability is IDuration duration)
        //     {
        //         SetDuration(duration.GetDuration());
        //     }

        //     if (_ability is ICooldown cooldown)
        //     {
        //         SetCooldown(cooldown.GetCooldown());
        //     }

        //     _createdAbility = (IAbility)SerializationUtility.CreateCopy(_ability);
        //     _createdAbility.SetRootSkill(this);
        //     _createdAbility.SetUser(GetUser());

        //     if (_createdAbility is IOwner owner)
        //         owner.SetOwner(GetOwnerType());

        //     _createdAbility.SetSkillName(GetSkillName());
        //     _createdAbility.SetInstanceID(GetInstanceID());
        //     _createdAbility.AwakeSkill();

        //     if (_holdModifiers == null)
        //         return;

        //     foreach (Modifier modifier in _holdModifiers)
        //     {
        //         _createdAbility.AddModifier(modifier);
        //     }

        //     _holdModifiers = null;
        // }
        public override void EndSkill()
        {

        }

        public override void UpdateSkill()
        {

        }

        public override void AddModifier(Modifier modifier)
        {
            if (!_isAwaken)
            {
                if (_holdModifiers == null)
                    _holdModifiers = new List<Modifier>();

                _holdModifiers.Add(modifier);
                return;
            }
            _createdAbility.AddModifier(modifier);
        }

        public override void ClearModifiers()
        {
            _holdModifiers = new List<Modifier>();

            if (_createdAbility == null)
                return;

            _createdAbility.ClearModifiers();
        }
        
        public IAbility GetAbility()
        {
            return _ability;
        }

        public override void OnAbilityAnimationStart()
        {
            // if (_createdAbility != null)
            // {
            //     _createdAbility.OnAbilityAnimationStart();
            // }
        }

        // public bool IsAbilityPassive()
        // {
        //     return _ability is IRecastSkill;
        // }
    }
}
