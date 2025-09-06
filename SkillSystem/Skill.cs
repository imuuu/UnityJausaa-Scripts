
using Game.StatSystem;
using UnityEngine;

namespace Game.SkillSystem
{
    public abstract class Skill : ISkill, ICooldown, IDuration, IHasName, IOwner
    {
        private int _instanceID = -1;
        public string Name { get; set; }
        private GameObject _user;
        private GameObject _launchUser;
        private OWNER_TYPE _ownerType;
        private ISkill _rootSkill;

        private SKILL_NAME _skillName = SKILL_NAME.NONE;

        // >>>> REMEMBER Assignint these values in ur inherited class!
        private float _cooldown;
        private float _duration;
        // <<<< REMEMBER Assignint these values in ur inherited class!
        private float _currentCooldown;
        private float _currentDuration;

        private int _skillSlot; //player only
        protected bool _isAwaken = false;

        private AbilityAnimation _abilityAnimation;

        public virtual void AwakeSkill()
        {
            _isAwaken = true;
        }

        public abstract void StartSkill();
        public abstract void UpdateSkill();
        public abstract void EndSkill();

        public abstract void AddModifier(Modifier modifier);
        public abstract void ClearModifiers();

        public virtual bool IsSkillUsable()
        {
            return true;
        }

        public bool HasAwaken()
        {
            return _isAwaken;
        }

        public void EndTheSkill()
        {
            ManagerSkills.Instance.EndTheSkill(this);
        }

        public bool IsRecastable()
        {
            return this is IRecastSkill && _ownerType == OWNER_TYPE.PLAYER;
        }

        public abstract void OnAbilityAnimationStart();

        #region Getters and Setters

        /// <summary>
        /// Returns the user of the ability. 
        /// </summary>
        /// <returns></returns>
        public GameObject GetUser()
        {
            return _user;
        }

        public void SetUser(GameObject user)
        {
            _user = user;
        }

        /// <summary>
        /// Returns the launch user of the ability. If not set, returns the user.
        /// </summary>
        /// <returns></returns>
        public GameObject GetLaunchUser()
        {
            if (_launchUser != null)
                return _launchUser;
            return _user;
        }

        public void SetLaunchUser(GameObject launchUser)
        {
            _launchUser = launchUser;
        }

        public GameObject GetGameObject()
        {
            return GetLaunchUser();
        }

        public IOwner GetRootOwner()
        {
            return this;
        }

        public AbilityAnimation GetAbilityAnimation()
        {
            return _abilityAnimation;
        }

        public OWNER_TYPE GetOwnerType()
        {
            return _ownerType;
        }

        public void SetOwner(OWNER_TYPE userType)
        {
            _ownerType = userType;
        }

        public string GetName()
        {
            return Name;
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
        }

        public float GetDuration()
        {
            return _duration;
        }

        public void SetCooldown(float cooldown)
        {
            _cooldown = cooldown;
        }

        public float GetCooldown()
        {
            return _cooldown;
        }

        public void SetCurrentDuration(float currentDuration)
        {
            _currentDuration = currentDuration;
        }

        public float GetCurrentDuration()
        {
            return _currentDuration;
        }

        public void SetCurrentCooldown(float currentCooldown)
        {
            _currentCooldown = currentCooldown;
        }

        public float GetCurrentCooldown()
        {
            return _currentCooldown;
        }

        public int GetSlot()
        {
            return _skillSlot;
        }

        public virtual void SetSlot(int slot)
        {
            _skillSlot = slot;
        }

        public SKILL_NAME GetSkillName()
        {
            return _skillName;
        }

        public void SetSkillName(SKILL_NAME skillName)
        {
            _skillName = skillName;
        }

        public int GetInstanceID()
        {
            return _instanceID;
        }

        public void SetInstanceID(int instanceID)
        {
            _instanceID = instanceID;
        }

        public ISkill GetRootSkill()
        {
            return _rootSkill;
        }

        public void SetRootSkill(ISkill rootSkill)
        {
            _rootSkill = rootSkill;
        }

        public bool IsManipulated()
        {
            return false; // Skills are not manipulated like owners, so this returns false.
        }

        public IOwner GetManipulatedOwner()
        {
            throw new System.NotImplementedException("Skills do not have manipulated owners.");
        }

        public void SetManipulatedOwner(IOwner owner)
        {
            throw new System.NotImplementedException("Skills do not have manipulated owners.");
        }

        #endregion Getters and Setters
    }
}

