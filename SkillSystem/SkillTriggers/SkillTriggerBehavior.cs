using UnityEngine;

namespace Game.SkillSystem
{
    [RequireComponent(typeof(SkillController))]
    public abstract class SkillTriggerBehavior : MonoBehaviour
    {
        public SkillController SkillController { get; private set; }

        protected virtual void Awake()
        {
            SkillController = GetComponent<SkillController>();
        }

        public void UseSkill()
        {
            if (SkillController == null)
            {
                Debug.LogError("SkillController is not assigned or missing.");
                return;
            }

            
            SkillController.UseSkill(0);
        }
    }
}
