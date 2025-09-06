using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SkillSystem
{
    [Serializable]
    public class SkillExecuteHandler
    {
        [SerializeField] private LinkedList<ISkill> _activeSkills = new();
        [SerializeField] private LinkedList<ISkill> _expiringSkills = new();
        [SerializeField] private LinkedList<ICooldown> _cooldownSkills = new();
        [SerializeField] private Dictionary<SkillDefinition, ISkill> _skillDefinitions = new();

        [SerializeField] private LinkedList<IFixedUpdate> _skillsFixedUpdate = new();
        [SerializeField] private LinkedList<ChargeEntry> _chargingSkills = new();
        [SerializeField] private LinkedList<AnimEntry> _animatingSkills = new();

        private Dictionary<int, ISkillButtonUp> _waitButtonUp = new();

        private sealed class ChargeEntry
        {
            public ISkill Skill;
            public IChargeable Charge;
            public float TimeLeft;
            public float TotalTime;

            public ChargeEntry(ISkill s, IChargeable c, float t)
            {
                Skill = s; Charge = c; TimeLeft = t; TotalTime = t;
            }
        }

        private sealed class AnimEntry
        {
            public ISkill Skill;
            public AbilityAnimation Anim;
            public float TimeLeft;
            public float TotalTime;

            public AnimEntry(ISkill s, AbilityAnimation a, float t)
            {
                Skill = s; Anim = a; TimeLeft = t; TotalTime = t;
            }
        }


        public bool OnButtonUp(int slot)
        {
            if (_waitButtonUp.TryGetValue(slot, out ISkillButtonUp skill))
            {
                skill.OnButtonUp();
                _waitButtonUp.Remove(slot);
            }
            return true;
        }

        public void UnityUpdate()
        {
            UpdateExpiringSkills();
            UpdateAnimatingSkills();
            UpdateChargingSkills();
            UpdateActiveSkills();
            UpdateCooldownSkills();
        }

        public void UnityFixedUpdate()
        {
            LinkedListNode<IFixedUpdate> node = _skillsFixedUpdate.First;
            while (node != null)
            {
                LinkedListNode<IFixedUpdate> next = node.Next;
                IFixedUpdate skill = node.Value;

                skill.FixedUpdate();

                node = next;
            }
        }

        public void RemoveSkillFromLists(ISkill skill)
        {
            LinkedListNode<ISkill> node = _activeSkills.First;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                Debug.Log($"|||||ACTIVE Checking skill {node.Value.GetSkillName()} against {skill.GetSkillName()} {skill.GetInstanceID()}");
                if (node.Value.GetInstanceID() == skill.GetInstanceID())
                {
                    Debug.Log($"||||||Removing skill {skill.GetSkillName()} from active list. ADDING TO EXPIRED LIST");
                    _activeSkills.Remove(node);
                    if (skill is IDuration duration && duration.GetCurrentDuration() > 0 && duration.GetCurrentDuration() < 10)
                    {
                        _expiringSkills.AddLast(skill);
                    }
                    else
                    {
                        OnEndSkill(node.Value);
                    }
                }
                node = next;
            }

            LinkedListNode<ICooldown> node2 = _cooldownSkills.First;
            while (node2 != null)
            {
                LinkedListNode<ICooldown> next = node2.Next;
                ISkill skill2 = node2.Value as ISkill;

                Debug.Log($"|||||COOLDOWN Checking skill {skill2.GetSkillName()} against {skill.GetSkillName()} {skill.GetInstanceID()}");
                if (skill2.GetInstanceID() == skill.GetInstanceID())
                {
                    Debug.Log($"|||||Removing skill {skill.GetSkillName()} from cooldown list.");
                    _cooldownSkills.Remove(node2);
                }
                node2 = next;
            }

            LinkedListNode<ChargeEntry> node3 = _chargingSkills.First;
            while (node3 != null)
            {
                LinkedListNode<ChargeEntry> next = node3.Next;
                if (node3.Value.Skill.GetInstanceID() == skill.GetInstanceID())
                {
                    _chargingSkills.Remove(node3);
                }
                node3 = next;
            }

            LinkedListNode<AnimEntry> node4 = _animatingSkills.First;
            while (node4 != null)
            {
                LinkedListNode<AnimEntry> next = node4.Next;
                if (node4.Value.Skill.GetInstanceID() == skill.GetInstanceID())
                    _animatingSkills.Remove(node4);
                node4 = next;
            }
        }

        #region Skill Execute Logic
        public bool ExecuteSkillDefinition(SkillDefinition skillDef, int slot)
        {
            ISkill skill = skillDef.GetSkill();

            skill.SetSlot(slot);
            return ExecuteSkill(skill);
        }

        /// <summary>
        /// Execute the skill with Logic of awakening, starting, updating and ending the skill.
        /// /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>//         
        public bool ExecuteSkill(ISkill skill)
        {
            //Debug.Log($"@@Executing skill: {skill.GetType().Name} + " + skill.GetSkillName() + " slot: " + skill.GetSlot());
            //Due to the way the skill system is designed, we need to check if the skill is already active or on cooldown before executing it.
            // This is only due to button presses, so that doesnt trigger multiple times
            // this probably can be removed in future, tho it doesnt trigger only once like it should
            if (skill is Skill_SingleAbility singleAbility && skill.IsRecastable())
            {
                if (IsSkillActive(singleAbility))
                {
                    Debug.Log($"========= Skill {(skill is IHasName ? ((IHasName)skill).GetName() + " " : "")}is already active.");
                    return false;
                }

                if (skill is ICooldown cd && IsSkillOnCooldown(cd))
                {
                    Debug.Log($"======== Skill {(skill is IHasName ? ((IHasName)skill).GetName() + " " : "")}is on cooldown.");
                    return false;
                }
            }

            if (!skill.HasAwaken())
            {
                skill.AwakeSkill();
            }

            if (skill.GetSlot() > -1)
            {
                if (skill is ISkillButtonDown buttonDown)
                {
                    buttonDown.OnButtonDown();
                }

                if (skill is ISkillButtonUp buttonUp)
                {
                    if (_waitButtonUp.ContainsKey(skill.GetSlot()))
                    {
                        _waitButtonUp[skill.GetSlot()] = buttonUp;
                    }
                    else
                    {
                        _waitButtonUp.Add(skill.GetSlot(), buttonUp);
                    }
                }
            }

            if (skill is ICooldown cooldown && cooldown.GetCurrentCooldown() > 0f) return false;

            AbilityAnimation anim = skill.GetAbilityAnimation();
            if (anim != null && anim.TriggerDelay > 0f)
            {
                if (IsSkillAnimating(skill)) return false; // already gating this instance

                skill.OnAbilityAnimationStart();
                _animatingSkills.AddLast(new AnimEntry(skill, anim, anim.TriggerDelay));

                return true; // accepted; will proceed after delay
            }

            if (skill is IChargeable chargeable)
            {
                if (IsSkillCharging(skill)) return false; // already charging this instance
                float t = chargeable.GetChargeTime();
                if (t <= 0f)
                {
                    // zero-time charge -> end immediately and start
                    chargeable.OnChargingStart();
                    chargeable.OnChargingUpdate(1f);
                    chargeable.OnChargingEnd();
                    return ActivateSkill(skill);
                }

                chargeable.OnChargingStart();
                _chargingSkills.AddLast(new ChargeEntry(skill, chargeable, t));
                return true; // accepted; will activate when done
            }

            return ActivateSkill(skill); ;
        }

        private bool ActivateSkill(ISkill skill)
        {
            //Debug.Log($"================> Activating skill: {skill.GetType().Name}");
            if (!skill.IsSkillUsable())
            {
                Debug.Log($"Skill {(skill is IHasName ? ((IHasName)skill).GetName() + " " : "")}is not usable at the moment.");
                return false;
            }

            if (skill is ICooldown)
            {
                AddToCooldownList((ICooldown)skill);
            }

            if(!skill.GetLaunchUser().activeInHierarchy) return false;

            skill.StartSkill();

            if (skill is Ability) ManagerSkills.Instance.OnAbilityStarted(skill);

            if (skill is IDuration duration)
            {
                duration.SetCurrentDuration((skill is IManualEnd) ? 99999 : duration.GetDuration());
            }

            // if (skill is IPassive passive)
            // {
            //     Debug.Log($"Adding passive skill {(passive is IHasName ? ((IHasName)passive).GetName() + " " : "")}to passive list.");
            //     _passiveSkills.AddLast(passive);
            //     return true;
            // }

            

            if (skill is IFixedUpdate fixedUpdateSkill)
            {
                if (!_skillsFixedUpdate.Contains(fixedUpdateSkill))
                {
                    _skillsFixedUpdate.AddLast(fixedUpdateSkill);
                }
            }

            _activeSkills.AddLast(skill);
            return true;
        }
        /// <summary>
        /// Update all active skills each frame.  
        /// - Decrement duration  
        /// - If duration hits 0, end the skill and move it to the cooldown list.  
        /// </summary>
        private void UpdateActiveSkills()
        {
            LinkedListNode<ISkill> node = _activeSkills.First;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                skill.UpdateSkill();

                if (skill is IDuration duration && HandleDuration(duration))
                {
                    //Debug.Log($"====================> Skill {(skill is IHasName ? ((IHasName)skill).GetName() +" ": "")} has ended.");
                    OnEndSkill(skill);
                    _activeSkills.Remove(node);
                }

                node = next;
            }
        }

        private void UpdateChargingSkills()
        {
            LinkedListNode<ChargeEntry> node = _chargingSkills.First;
            while (node != null)
            {
                LinkedListNode<ChargeEntry> next = node.Next;
                ChargeEntry entry = node.Value;

                // progress 0..1 (clamped)
                entry.TimeLeft -= Time.deltaTime;
                float denom = (entry.TotalTime <= 0f) ? 1f : entry.TotalTime;
                float progress = (entry.TotalTime - Mathf.Max(entry.TimeLeft, 0f)) / denom;
                if (progress < 0f) progress = 0f; else if (progress > 1f) progress = 1f;

                entry.Charge.OnChargingUpdate(progress);

                if (entry.TimeLeft <= 0f)
                {
                    entry.Charge.OnChargingEnd();
                    _chargingSkills.Remove(node);
                    ActivateSkill(entry.Skill);
                }

                node = next;
            }
        }

        private void UpdateExpiringSkills()
        {
            LinkedListNode<ISkill> node = _expiringSkills.First;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                skill.UpdateSkill();

                if (skill is IDuration duration && HandleDuration(duration))
                {
                    //Debug.Log($"====================> Skill {(skill is IHasName ? ((IHasName)skill).GetName() +" ": "")} has ended.");
                    OnEndSkill(skill);
                    _expiringSkills.Remove(node);
                }

                node = next;
            }
        }

        private void UpdateAnimatingSkills()
        {
            LinkedListNode<AnimEntry> node = _animatingSkills.First;
            while (node != null)
            {
                LinkedListNode<AnimEntry> next = node.Next;
                AnimEntry entry = node.Value;

                entry.TimeLeft -= Time.deltaTime;

                // (Optional progress calc if you want to drive VFX/UI)
                float denom = (entry.TotalTime <= 0f) ? 1f : entry.TotalTime;
                float progress = (entry.TotalTime - Mathf.Max(entry.TimeLeft, 0f)) / denom;
                if (progress < 0f) progress = 0f; else if (progress > 1f) progress = 1f;
                // You could notify here via an optional interface if desired.

                if (entry.TimeLeft <= 0f)
                {
                    _animatingSkills.Remove(node);

                    // After animation gate completes, honor charge gate if present; else activate.
                    if (entry.Skill is IChargeable ch)
                    {
                        float t = ch.GetChargeTime();
                        if (t <= 0f)
                        {
                            ch.OnChargingStart();
                            ch.OnChargingUpdate(1f);
                            ch.OnChargingEnd();
                            ActivateSkill(entry.Skill);
                        }
                        else
                        {
                            ch.OnChargingStart();
                            _chargingSkills.AddLast(new ChargeEntry(entry.Skill, ch, t));
                        }
                    }
                    else
                    {
                        ActivateSkill(entry.Skill);
                    }
                }

                node = next;
            }
        }

        private void AddToCooldownList(ICooldown cooldown)
        {
            //Debug.Log($"Adding skill {(cooldown is IHasName ? ((IHasName)cooldown).GetName() +" ": "")} to cooldown list. Cooldown: {cooldown.GetCooldown()}");
            cooldown.SetCurrentCooldown(cooldown.GetCooldown());
            _cooldownSkills.AddLast(cooldown);
        }

        ///<summary> 
        /// Handle the duration. If duration is <= 0 return true 
        ///</summary>
        private bool HandleDuration(IDuration duration)
        {
            //duration.CurrentDuration -= Time.deltaTime;
            duration.SetCurrentDuration(duration.GetCurrentDuration() - Time.deltaTime);
            if (duration.GetCurrentDuration() <= 0f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle the cooldown. If cooldown is <= 0 return true
        /// </summary> 
        private bool HandleCooldown(ICooldown cooldown)
        {
            cooldown.SetCurrentCooldown(cooldown.GetCurrentCooldown() - Time.deltaTime);
            if (cooldown.GetCurrentCooldown() <= 0f)
            {
                //Debug.Log($"Skill {(cooldown is IHasName ? ((IHasName)cooldown).GetName() +" ": "")} is off cooldown.");
                return true;
            }

            return false;
        }

        private bool IsSkillActive(ISkill skill)
        {
            LinkedListNode<ISkill> node = _activeSkills.First;
            while (node != null)
            {
                if (node.Value == skill)
                {
                    return true;
                }
                node = node.Next;
            }
            return false;
        }

        private bool IsSkillOnCooldown(ICooldown skill)
        {
            LinkedListNode<ICooldown> node = _cooldownSkills.First;
            while (node != null)
            {
                if (node.Value == skill)
                {
                    return true;
                }
                node = node.Next;
            }
            return false;
        }

        private bool IsSkillCharging(ISkill skill)
        {
            LinkedListNode<ChargeEntry> node = _chargingSkills.First;
            while (node != null)
            {
                if (node.Value.Skill == skill) return true;
                node = node.Next;
            }
            return false;
        }

        private bool IsSkillAnimating(ISkill skill)
        {
            LinkedListNode<AnimEntry> node = _animatingSkills.First;
            while (node != null)
            {
                if (node.Value.Skill == skill) return true;
                node = node.Next;
            }
            return false;
        }

        /// <summary>
        /// Update all cooldown skills each frame.  
        /// - Decrement cooldown  
        /// - If cooldown hits 0, remove the skill from the cooldown list.  
        ///   (Skill can then be considered usable again.)  
        /// </summary>
        private void UpdateCooldownSkills()
        {
            LinkedListNode<ICooldown> node = _cooldownSkills.First;
            while (node != null)
            {
                LinkedListNode<ICooldown> next = node.Next;
                ICooldown cooldown = node.Value;

                if (HandleCooldown(cooldown))
                {
                    _cooldownSkills.Remove(node);

                    if (cooldown is IReUseSkill && cooldown is ISkill skill1)
                    {
                        // NOT tested, but should work, due to this was old system 
                        ActivateSkill(skill1);
                    }
                    //else if (cooldown is IRecastSkill passive && passive is ISkill skill2)
                    else if (cooldown is ISkill skill2 && skill2.IsRecastable())
                    {
                        // Debug.Log($"Skill {(passive is IHasName ? ((IHasName)passive).GetName() + " " : "")}is off cooldown.");
                        ActivateSkill(skill2.GetRootSkill());
                        //ActivateSkill(passive as ISkill);
                    }
                }
                node = next;
            }
        }

        public void EndManualSkill(IManualEnd manualEndSkill)
        {
            LinkedListNode<ISkill> node = _activeSkills.First;
            bool found = false;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                if (skill is IManualEnd manualEnd && manualEnd == manualEndSkill)
                {
                    OnEndSkill(skill);
                    _activeSkills.Remove(node);
                    found = true;
                    break;
                }

                node = next;
            }

            if (found) return;

            node = _expiringSkills.First;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                if (skill is IManualEnd manualEnd && manualEnd == manualEndSkill)
                {
                    OnEndSkill(skill);
                    _expiringSkills.Remove(node);
                    found = true;
                    break;
                }

                node = next;
            }

            if (found) return;

            LinkedListNode<ChargeEntry> cnode = _chargingSkills.First;
            while (cnode != null)
            {
                LinkedListNode<ChargeEntry> next = cnode.Next;
                if (cnode.Value.Skill is IManualEnd me && me == manualEndSkill)
                {
                    _chargingSkills.Remove(cnode);
                    found = true;
                    break;
                }
                cnode = next;
            }

            LinkedListNode<AnimEntry> anode = _animatingSkills.First;   // << NEW
            while (anode != null)
            {
                LinkedListNode<AnimEntry> next = anode.Next;
                if (anode.Value.Skill is IManualEnd me && me == manualEndSkill)
                {
                    _animatingSkills.Remove(anode);
                    return;
                }
                anode = next;
            }

            if (!found)
            {
                if (manualEndSkill is ISkill skill)
                {
                    Debug.LogWarning($"Skill {skill.GetSkillName()} NOT found in active, expiring, or charging lists.");
                }
            }
        }

        public void EndTheSkill(ISkill endSkill)
        {
            LinkedListNode<ISkill> node = _activeSkills.First;
            bool found = false;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                if (skill == endSkill)
                {
                    OnEndSkill(skill);
                    _activeSkills.Remove(node);
                    found = true;
                    break;
                }

                node = next;
            }

            if (found) return;

            node = _expiringSkills.First;
            while (node != null)
            {
                LinkedListNode<ISkill> next = node.Next;
                ISkill skill = node.Value;

                if (skill == endSkill)
                {
                    OnEndSkill(skill);
                    _expiringSkills.Remove(node);
                    found = true;
                    break;
                }

                node = next;
            }

            if (found) return;

            LinkedListNode<ChargeEntry> cnode = _chargingSkills.First;
            while (cnode != null)
            {
                LinkedListNode<ChargeEntry> next = cnode.Next;
                if (cnode.Value.Skill == endSkill)
                {
                    _chargingSkills.Remove(cnode);
                    found = true;
                    break;
                }
                cnode = next;
            }

            LinkedListNode<AnimEntry> anode = _animatingSkills.First;   // << NEW
            while (anode != null)
            {
                LinkedListNode<AnimEntry> next = anode.Next;
                if (anode.Value.Skill == endSkill)
                {
                    _animatingSkills.Remove(anode);
                    return;
                }
                anode = next;
            }

            if (!found)
            {
                Debug.LogWarning($"END Skill {endSkill.GetSkillName()} NOT found in active, expiring, or charging lists.");
            }
        }

        /// <summary>
        /// Calls the end skill method on the skill, which should handle any cleanup or state reset.
        /// </summary>
        private void OnEndSkill(ISkill skill)
        {
            if(skill is IFixedUpdate fixedUpdateSkill)
            {
                _skillsFixedUpdate.Remove(fixedUpdateSkill);
            }

            if (skill is Ability) ManagerSkills.Instance.OnAbilityEnded(skill);

            skill.EndSkill();
        }

        private void ClearCooldown(ISkill skill)
        {
            if (skill is ICooldown cooldown)
            {
                cooldown.SetCurrentCooldown(0);
            }
        }

        /// <summary>
        /// Clears all active, expiring, and cooldown skills, ending any running skills and resetting state.
        /// </summary>
        public void ClearAllSkills()
        {
            foreach (var skill in _activeSkills)
            {
                ClearCooldown(skill);
                OnEndSkill(skill);
            }
            _activeSkills.Clear();

            foreach (var skill in _expiringSkills)
            {
                ClearCooldown(skill);
                OnEndSkill(skill);
            }
            _expiringSkills.Clear();

            _cooldownSkills.Clear();
            _chargingSkills.Clear();
            _animatingSkills.Clear();

            _waitButtonUp.Clear();
        }
        #endregion Skill Execute Logic
    }
}