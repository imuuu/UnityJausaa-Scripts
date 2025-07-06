using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Game.SkillSystem;
using System;
using Unity.VisualScripting;

namespace Game.UI
{
    [DefaultExecutionOrder(100)]
    public class UI_ActionBar : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private GameObject _actionBarParent;
        [SerializeField] private GameObject _skillSlotPrefab;
        [SerializeField] private UI_SkillSlot[] _skillSlots;

        private void Start()
        {
            //InitializeButtonNumbers();
        }

        private void OnEnable()
        {
            Events.OnAddPlayerSkill.AddListener(OnAddSkill);
            Events.OnRemovePlayerSkill.AddListener(OnRemoveSkill);
            ActionScheduler.RunNextFrame(() => GetPlayerSkillsTest());
        }

        private void OnDisable()
        {
            Events.OnAddPlayerSkill.RemoveListener(OnAddSkill);
            Events.OnRemovePlayerSkill.RemoveListener(OnRemoveSkill);
        }

        private void InitializeButtonNumbers()
        {
            for (int i = 0; i < _skillSlots.Length; i++)
            {
                if (i >= CONSTANTS.MAX_PLAYER_SKILLS)
                {
                    Debug.LogWarning($"Maximun number of player skills is {CONSTANTS.MAX_PLAYER_SKILLS}, current number of player skills is {i}.");
                    break;
                }
                string key = ManagerButtons.Instance.GetSkillKey(i);
                _skillSlots[i].SetSlotIndex(i);
                _skillSlots[i].SetKeyNumber(key);
            }
        }

        private bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < _skillSlots.Length;
        }

        private bool OnAddSkill(int slot, SkillDefinition skillDefinition)
        {
            if (skillDefinition == null) return false;

            if (!IsValidSlot(slot))
            {
                AddNewSlot();
            }

            if(!IsValidSlot(slot))
            {
                Debug.LogWarning($"Invalid skill slot {slot}.");
                return false;
            }


            _skillSlots[slot].BindSkill(skillDefinition);
            _skillSlots[slot].gameObject.SetActive(true);
            return true;
        }

        private bool OnRemoveSkill(int index, SkillDefinition param2)
        {
            if (index < 0 || index >= _skillSlots.Length)
            {
                Debug.LogWarning($"Invalid skill slot {index}.");
                return true;
            }

            _skillSlots[index].UnbindSkill();

            _skillSlots[index].gameObject.SetActive(false);
            return true;
        }

        [Button("Add Action Bar")]
        private void AddNewSlot()
        {
            GameObject newSlotObject = Instantiate(_skillSlotPrefab, _actionBarParent.transform);
            UI_SkillSlot newSlot = newSlotObject.GetComponent<UI_SkillSlot>();
            newSlot.SetSlotIndex(_skillSlots.Length);
            Array.Resize(ref _skillSlots, _skillSlots.Length + 1);
            _skillSlots[_skillSlots.Length - 1] = newSlot;
        }

        [Button("Get Player Skills Test")]
        private void GetPlayerSkillsTest()
        {
            List<SkillDefinition> playerSkills = ManagerSkills.Instance.GetPlayerSkills();

            if (playerSkills == null || playerSkills.Count == 0)
            {
                Debug.LogWarning("No player skills found.");
                return;
            }

            for (int i = 0; i < _skillSlots.Length; i++)
            {
                _skillSlots[i].gameObject.SetActive(false);
                _skillSlots[i].UnbindSkill();
            }

            for (int i = 0; i < playerSkills.Count; i++)
            {
                if( playerSkills[i] == null)
                {
                    continue;
                }

                if (i >= _skillSlots.Length)
                {
                    AddNewSlot();
                    // Debug.LogWarning("Not enough skill slots to bind all player skills.");
                    // break;
                }

                _skillSlots[i].gameObject.SetActive(true);
                _skillSlots[i].BindSkill(playerSkills[i]);
            }
        }
    }
}
