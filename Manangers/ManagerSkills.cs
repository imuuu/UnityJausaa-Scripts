using System;
using System.Collections.Generic;
using System.Linq;
using Game.StatSystem;
using Sirenix.OdinInspector;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

namespace Game.SkillSystem
{
    public class ManagerSkills : MonoBehaviour
    {
        public static ManagerSkills Instance { get; private set; }
        [Title("Options")]
        [SerializeField] private bool _keepTwoSkillsAtStart = true;
        private const int KEEP_SKILLS_AT_START = 2;

        [Header("All Skills")] //[InlineEditor(InlineEditorObjectFieldModes.Hidden)] //this line shows whole skill
        [SerializeField] private List<SkillDefinition> _allSkills;
        [Header("Player Active Skills"), Space(20)]
        //[InlineEditor(InlineEditorObjectFieldModes.Hidden)] //this line shows whole skill
        [SerializeField] private List<SkillDefinition> _activePlayerSkills = new();

        private GameObject _player;

        [Title("Skill Execute Handler")]
        [ShowInInspector, ReadOnly]
        private SkillExecuteHandler _skillExecuteHandler = new SkillExecuteHandler();
        private SCENE_NAME _activeFightScene = SCENE_NAME.ToxicLevel;

        //private Dictionary<GameObject, StatList> _skillStatsForGameObjects = new ();

        /// <summary>
        /// Used for triggering skill group items.
        /// for example for shields, we have multiple shield skills and we only one trigger one of them at a time. 
        /// For this the id is same for all shield skills.
        /// </summary>
        private Dictionary<int, List<ISkillGroupItemTrigger>> _skillGroupItemTriggers = new();

        // ==== Event Contexts ====
        private BlockEventContext _blockEventContext;
        // ========================


        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            FillRemainingSlots();

            Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
            Events.OnBlockHappened.AddListener(OnBlockHappened);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            Events.OnPlayableSceneChangeEnter.RemoveListener(OnPlayableSceneChange);
            Events.OnBlockHappened.RemoveListener(OnBlockHappened);
        }

        private bool OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver)
        {
            if (_skillGroupItemTriggers.TryGetValue((int)SKILL_GROUP_TAG.MAGIC_SHIELD, out List<ISkillGroupItemTrigger> triggers) && triggers.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, triggers.Count);
                ISkillGroupItemTrigger randomTrigger = triggers[randomIndex];
                Debug.Log("Block index and count: " + randomIndex + " / " + triggers.Count);

                if (_blockEventContext == null) _blockEventContext = new BlockEventContext(dealer, receiver);
                else { _blockEventContext.DamageDealer = dealer; _blockEventContext.DamageReceiver = receiver; }

                randomTrigger.OnSkillGroupItemTrigger(_blockEventContext);

            }
            return true;
        }

        public void RegisterSkillGroupItemTrigger(ISkillGroupItemTrigger trigger)
        {
            if (trigger == null) return;

            int skillGroupItemID = trigger.SkillGroupItemID;

            if (!_skillGroupItemTriggers.ContainsKey(skillGroupItemID))
            {
                _skillGroupItemTriggers[skillGroupItemID] = new List<ISkillGroupItemTrigger>();
            }

            if (!_skillGroupItemTriggers[skillGroupItemID].Contains(trigger))
            {
                Debug.Log($"Registering trigger with ID {trigger.SkillGroupItemID} for skill group item trigger: {trigger}");
                _skillGroupItemTriggers[skillGroupItemID].Add(trigger);
            }
            else
            {
                Debug.LogWarning($"Trigger with ID {trigger} is already registered.");
            }
        }

        public void RemoveSkillGroupItemTrigger(ISkillGroupItemTrigger trigger)
        {
            int skillGroupItemID = trigger.SkillGroupItemID;

            if (trigger == null || !_skillGroupItemTriggers.ContainsKey(skillGroupItemID)) return;

            _skillGroupItemTriggers[skillGroupItemID].Remove(trigger);

            if (_skillGroupItemTriggers[skillGroupItemID].Count == 0)
            {
                _skillGroupItemTriggers.Remove(skillGroupItemID);
            }
        }

        // public void SetSkillStatForGameObject(GameObject go, Stat stat)
        // {
        //     if (go == null || stat == null) return;

        //     if (!_skillStatsForGameObjects.ContainsKey(go))
        //     {
        //         _skillStatsForGameObjects[go] = new StatList();
        //     }

        //     _skillStatsForGameObjects[go].AddStat(stat);
        // }

        // public Stat GetSkillStatForGameObject(GameObject go, STAT_TYPE statType)
        // {
        //     if (go == null) return null;

        //     if (_skillStatsForGameObjects.TryGetValue(go, out StatList statList))
        //     {
        //         return statList.GetStat(statType);
        //     }

        //     return null;
        // }

        private bool OnPlayableSceneChange(SCENE_NAME sceneName)
        {
            _skillExecuteHandler.ClearAllSkills();

            //_skillStatsForGameObjects.Clear();

            if (sceneName != SCENE_NAME.Lobby)
            {
                ClearAllModifiers();
                KeepTwoSkillsAtStart();
                HandleAllFirstAttackSkills();
            }

            return true;
        }

        private void OnEnable()
        {
            Events.OnSkillButtonDown.AddListener(UsePlayerSkill);
            Events.OnSkillButtonUp.AddListener(_skillExecuteHandler.OnButtonUp);
        }

        private void OnDisable()
        {
            Events.OnSkillButtonDown.RemoveListener(UsePlayerSkill);
            Events.OnSkillButtonUp.RemoveListener(_skillExecuteHandler.OnButtonUp);
        }

        private void Update()
        {
            if (ManagerPause.IsPaused()) return;

            if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

            _skillExecuteHandler.UnityUpdate();
        }

        private void FixedUpdate()
        {
            if (ManagerPause.IsPaused()) return;

            if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

            _skillExecuteHandler.UnityFixedUpdate();
        }

        public void SetPlayer(GameObject player)
        {
            _player = player;
            foreach (SkillDefinition skillDef in _allSkills)
            {
                UpdateSkillData(skillDef);
            }

            foreach (SkillDefinition skillDef in _activePlayerSkills)
            {
                if (skillDef == null) continue;

                UpdateSkillData(skillDef);
            }
        }

        private void UpdateSkillData(SkillDefinition skillDef)
        {
            skillDef.SetUser(_player);
            skillDef.UpdateAbilityData();
        }

        private void FillRemainingSlots()
        {
            for (int i = 0; i < CONSTANTS.MAX_PLAYER_SKILLS; i++)
            {
                if (_activePlayerSkills.Count <= i)
                {
                    _activePlayerSkills.Add(null);
                    continue;
                }

                AddPlayerSkill(i, _activePlayerSkills[i]);
            }
        }

        private void KeepTwoSkillsAtStart()
        {
            if (_keepTwoSkillsAtStart)
            {
                for (int i = 0; i < _activePlayerSkills.Count; i++)
                {
                    if (i < KEEP_SKILLS_AT_START) continue;

                    Events.OnRemovePlayerSkill.Invoke(i, _activePlayerSkills[i]);
                    _activePlayerSkills[i] = null;
                }
            }
        }

        private void ClearAllModifiers()
        {
            foreach (SkillDefinition skill in _activePlayerSkills)
            {
                if (skill == null) continue;

                skill.ClearModifiers();
            }
        }

        private bool UsePlayerSkill(int slot)
        {
            if (ManagerPause.IsPaused()) return false;

            if (!IsValidSlot(slot)) return false;

            if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return false;


            SkillDefinition skill = _activePlayerSkills[slot];

            if (skill == null)
            {
                Debug.LogWarning("==> No skill in slot");
                return false;
            }

            Debug.Log($"Using skill in slot {slot}: " + skill.Name);

            skill.UseSkill(slot);
            return true;
        }

        public void AddPlayerSkill(int slot, SkillDefinition skill)
        {
            if (skill == null) return;

            if (!IsValidSlot(slot)) return;

            SkillDefinition clonedDef = skill.Clone();
            if (!Events.OnAddPlayerSkill.Invoke(slot, clonedDef))
            {
                Debug.Log("Event was canceled; skill not added for some reason");
                return;
            }

            if (_player != null)
                UpdateSkillData(clonedDef);

            _activePlayerSkills[slot] = clonedDef;

            HandleFirstAttackSkill(_activePlayerSkills[slot]);
        }

        public int GetNextValidSlot()
        {
            for (int i = 0; i < CONSTANTS.MAX_PLAYER_SKILLS; i++)
            {
                if (_activePlayerSkills.Count <= i || _activePlayerSkills[i] == null)
                {
                    return i;
                }
            }

            Debug.LogWarning("No valid slot found for adding a new skill.");
            return -1;
        }

        public SkillDefinition GetSkill(SKILL_NAME skillName)
        {
            foreach (SkillDefinition skill in _allSkills)
            {
                if (skill.SkillName == skillName)
                {
                    return skill;
                }
            }

            return null;
        }

        private void HandleAllFirstAttackSkills()
        {
            foreach (SkillDefinition skill in _activePlayerSkills)
            {
                if (skill == null) continue;

                HandleFirstAttackSkill(skill);
            }
        }

        private void HandleFirstAttackSkill(SkillDefinition skill)
        {
            ActionScheduler.RunWhenTrue(() => IsValidHandleFirstAttack(), () =>
            {
                ActionScheduler.RunAfterDelay(1f, () =>
                {
                    //    if (skill.GetSkill() is Skill_SingleAbility singleAbility && singleAbility.IsAbilityPassive())
                    //    {
                    //        ExecuteSkillDefinition(skill, -999);
                    //    }
                    Debug.Log("==> Executing first attack skill: " + skill.Name);
                    ExecuteSkillDefinition(skill, -999);
                });
            });
        }

        private bool IsValidHandleFirstAttack()
        {
            return Player.Instance != null && SceneLoader.GetCurrentScene() == _activeFightScene;
        }

        public void RemovePlayerSkill(int slot)
        {
            if (!IsValidSlot(slot)) return;

            SkillDefinition skill = _activePlayerSkills[slot];

            if (skill == null) return;

            if (!Events.OnRemovePlayerSkill.Invoke(slot, skill))
            {
                Debug.Log("Event was canceled; skill not removed for some reason");
                return;
            }

            _activePlayerSkills.RemoveAt(slot);
        }

        private bool IsValidSlot(int slot)
        {
            bool isValid = slot >= 0 && slot < CONSTANTS.MAX_PLAYER_SKILLS;

            if (!isValid) Debug.LogWarning($"Invalid slot: {slot}");

            return isValid;
        }
        public List<SkillDefinition> GetPlayerSkills()
        {
            return _activePlayerSkills;
        }

        public SkillDefinition GetRandomPlayerSkill()
        {
            var nonNullSkills = _activePlayerSkills.Where(skill => skill != null).ToList();
            if (nonNullSkills.Count == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, nonNullSkills.Count);
            return nonNullSkills[randomIndex];
        }

        /// <summary>
        /// Triggered from ExecuteSkillHandler when a skill starts.
        /// </summary>
        /// <param name="skill"></param>
        public void OnAbilityStarted(ISkill skill)
        {
            if (skill is ISkillGroupItemTrigger skillGroupItemTrigger)
            {
                RegisterSkillGroupItemTrigger(skillGroupItemTrigger);
            }
        }

        /// <summary>
        /// Triggered from ExecuteSkillHandler when a skill ends.
        /// summary>
        public void OnAbilityEnded(ISkill skill)
        {
            if (skill is ISkillGroupItemTrigger skillGroupItemTrigger)
            {
                RemoveSkillGroupItemTrigger(skillGroupItemTrigger);
            }
        }

        // public SkillDefinition GetRandomNotActivePlayerSkill()
        // {
        //     if (_allSkills.Count == 0) return null;
        //     int randomIndex = UnityEngine.Random.Range(0, _allSkills.Count);
        //     SkillDefinition skill = _allSkills[randomIndex];

        //     int maxAttempts = 1000;
        //     while (IsSkillActive(skill.SkillName))
        //     {
        //         randomIndex = UnityEngine.Random.Range(0, _allSkills.Count);
        //         skill = _allSkills[randomIndex];
        //         maxAttempts--;

        //         if (maxAttempts <= 0)
        //         {
        //             Debug.LogError("Max attempts reached while trying to find a valid skill definition.");
        //             break;
        //         }
        //     }
        //     return skill;
        // }

        public SkillDefinition GetRandomNotActivePlayerSkill()
        {
            List<SkillDefinition> inactiveSkills = _allSkills
                .Where(s => !IsSkillActive(s.SkillName))
                .ToList();

            if (inactiveSkills.Count == 0)
                return null;

            int idx = UnityEngine.Random.Range(0, inactiveSkills.Count);
            return inactiveSkills[idx];
        }

        [Button("Get Random Not Active Player Skill")]
        public bool IsSkillActive(SKILL_NAME skillName)
        {
            bool b = _activePlayerSkills.Any(skill => skill != null && skill.SkillName == skillName);
            //Debug.Log($"Is skill {skillName} active: {b}");
            return b;
        }

        public List<SkillDefinition> GetAllSkills()
        {
            return _allSkills;
        }

        public void ExecuteSkillDefinition(SkillDefinition skillDefinition, int slot)
        {
            _skillExecuteHandler.ExecuteSkillDefinition(skillDefinition, slot);
        }

        public void ExecuteSkill(ISkill skill)
        {
            _skillExecuteHandler.ExecuteSkill(skill);
        }

        public void EndManualSkill(IManualEnd skill)
        {
            _skillExecuteHandler.EndManualSkill(skill);
        }

        public SkillDefinition GetActivePlayerSkillDef(SKILL_NAME SKILL_NAME)
        {
            foreach (SkillDefinition skill in _activePlayerSkills)
            {
                if (skill.SkillName == SKILL_NAME)
                {
                    return skill;
                }
            }

            Debug.LogWarning($"Skill {SKILL_NAME} not found in active player skills list.");
            return null;
        }

        public SkillDefinition GetSkillDefinition(SKILL_NAME targetSkill)
        {
            foreach (SkillDefinition skill in _allSkills)
            {
                if (skill.SkillName == targetSkill)
                {
                    return skill;
                }
            }

            Debug.LogWarning($"Skill {targetSkill} not found in all skills list.");
            return null;
        }

        public void RemoveExecutedSkill(ISkill skill)
        {
            _skillExecuteHandler.RemoveSkillFromLists(skill);
        }

        public void EndTheSkill(ISkill skill)
        {
            _skillExecuteHandler.EndTheSkill(skill);
        }
    }
}