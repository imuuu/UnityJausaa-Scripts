using UnityEngine;
using Nova;
using Game.SkillSystem;
namespace Game.UI
{
    public class UI_SkillSlot : MonoBehaviour, IPickupItemUI, IDropItemUI
    {
        [SerializeField] private ItemView _itemView;
        [SerializeField] private SLOT_UI_PLACE _slotUIPlace;

        private SkillDefinition _skillDefinition;
        private int _slotIndex = -1;

        public enum SLOT_UI_PLACE
        {
            NONE,
            SKILL_SLOT,
        }

        public void BindSkill(SkillDefinition skillDefinition)
        {
            SkillSlotVisual skillSlotVisual = _itemView.Visuals as SkillSlotVisual;

            _skillDefinition = skillDefinition;
            skillSlotVisual.Bind(skillDefinition);
        }

        public void UnbindSkill()
        {
            SkillSlotVisual skillSlotVisual = _itemView.Visuals as SkillSlotVisual;
            _skillDefinition = null;
            skillSlotVisual.Unbind();
        }

        public void SetKeyNumber(string keyNumber)
        {
            SkillSlotVisual skillSlotVisual = _itemView.Visuals as SkillSlotVisual;
            skillSlotVisual.SetKeyNumber(keyNumber);
        }

        public void SetSlotIndex(int slotIndex)
        {
            _slotIndex = slotIndex;
        }

        public void SetSkillDefinition(SkillDefinition skillDefinition)
        {
            _skillDefinition = skillDefinition;
        }

        public void SetSlotUIPlace(SLOT_UI_PLACE slotUIPlace)
        {
            _slotUIPlace = slotUIPlace;
        }

        public SkillDefinition GetSkillDefinition()
        {
            return _skillDefinition;
        }

        public void OnDropItem(ICursorPickupable item)
        {
            UI_SkillSlot skillSlot = item.GetItem().gameObject.GetComponent<UI_SkillSlot>();
            if(skillSlot == null) return;

            SkillDefinition skillDefinition = skillSlot.GetSkillDefinition();
            
            if(_slotIndex >= 0)
                ManagerSkills.Instance.AddPlayerSkill(_slotIndex, skillDefinition);

            Destroy(item.GetItem().gameObject);
        }

        public bool IsAbleToDrop(ICursorPickupable item)
        {

            bool isSkillSlot = item.GetItem().gameObject.GetComponent<UI_SkillSlot>() != null;

            return isSkillSlot;
        }

        public void OnPickup(ICursorPickupable item)
        {
            UI_SkillSlot skillSlot = item.GetItem().gameObject.GetComponent<UI_SkillSlot>();

            SetSkillDefinition(skillSlot.GetSkillDefinition());
        }
    }
}
