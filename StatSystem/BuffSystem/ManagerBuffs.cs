using System.Collections.Generic;
using Game.SkillSystem;
using Game.StatSystem;
using Game.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.BuffSystem
{
    public partial class ManagerBuffs : MonoBehaviour
    {
        public static ManagerBuffs Instance { get; private set; }

        [Title("Active Buffs")]
        [SerializeField] private List<BuffDefinition> _buffDefinitions;
        [Title("Blessings")]
        [SerializeField] private BlessingUpgradeGrouper _blessingUpgradeGrouper;

        [SerializeField] private RarityListHolder _rarityListHolder;
        [SerializeField] private StatSystemIcons _statSystemIcons;
        // [SerializeField] private List<RarityDefinition> _modRarities = new List<RarityDefinition>
        // {
        //     new RarityDefinition { Rarity = MODIFIER_RARITY.COMMON, Threshold = 0.7f, MainColor = Color.white },
        //     new RarityDefinition { Rarity = MODIFIER_RARITY.UNCOMMON, Threshold = 0.53f, MainColor = Color.green },
        //     new RarityDefinition { Rarity = MODIFIER_RARITY.RARE, Threshold = 0.4f, MainColor = Color.blue },
        //     new RarityDefinition { Rarity = MODIFIER_RARITY.EPIC, Threshold = 0.25f, MainColor = Color.magenta },
        //     new RarityDefinition { Rarity = MODIFIER_RARITY.LEGENDARY, Threshold = 0.1f, MainColor = Color.yellow }
        // };

        [SerializeField, ReadOnly] private List<Modifier> _activeBuffs = new();
        [ShowInInspector] private Dictionary<int, List<Modifier>> _rolledModifiers = new();
        [ShowInInspector] private float[] _probabilities;

        private bool _isSelectingBuffs = false;
        private bool _middleOfChoosing = false;
        [SerializeField, ReadOnly] private int _choosesLeft = 0;

        public int _chanceToGetSkill = 30;
        public const int DEFAULT_CHANCE_TO_GET_SKILL = 30;
        public const int REDUCE_SKILL_CHANCE = 3; // REDUCE_SKILL_CHANCE + REDUCE_SKILL_CHANCE_PER_ANY_BUFF
        public const int REDUCE_SKILL_CHANCE_PER_ANY_BUFF = 3;
        public const int MIN_CHANCE_TO_GET_SKILL = 3;

        public int _chanceToGetPlayerBuff = 80;
        public const int DEFAULT_CHANCE_TO_GET_PLAYER_BUFF = 80;
        public const int REDUCE_PLAYER_BUFF_CHANCE = 8; // REDUCE_PLAYER_BUFF_CHANCE + REDUCE_PLAYER_BUFF_CHANCE_PER_ANY_BUFF
        public const int REDUCE_PLAYER_BUFF_CHANCE_PER_ANY_BUFF = 5;
        public const int MIN_CHANCE_TO_GET_PLAYER_BUFF = 3;

        [SerializeField] private int _chance_to_get_second_buff = 15;
        [SerializeField] private int _chance_to_get_third_buff = 0; // player has 1 luck
        //public const int CHANCE_TO_GET_THIRD_BUFF = 2;
        public const int ROLL_AMOUNT = 5; // atm 3, but 2 extra

        private List<BuffCard> _buffCards = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _probabilities = new float[ROLL_AMOUNT];
        }

        private void Start()
        {
            Events.OnPlayerLevelChange.AddListener(OnPlayerLevelChange);
            Events.OnPlayableScenePreloadStart.AddListener(OnPlayableSceneChange);
            Events.OnGamePause.AddListener(OnGamePause);
            Events.OnGameUnPause.AddListener(OnGameUnpause);
        }

        private bool OnGameUnpause(PAUSE_REASON reason)
        {
            if (reason == PAUSE_REASON.BUFF_CARDS) return true;

            if (_middleOfChoosing)
            {
                OnChooseBuffs();
            }

            return true;
        }

        private bool OnGamePause(PAUSE_REASON reason)
        {
            if (reason == PAUSE_REASON.BUFF_CARDS) return true;

            if(_isSelectingBuffs) CloseChooseBuffs();
            return true;
        }

        private bool OnPlayableSceneChange(SCENE_NAME sceneName)
        {
            _chanceToGetSkill = DEFAULT_CHANCE_TO_GET_SKILL;
            _middleOfChoosing = false;
            _choosesLeft = 0;
            _buffCards.Clear();
            CloseChooseBuffs();
            return true;
        }

        public void ReduceChanceToGetSkill(int customAmount = 0)
        {
            if (customAmount > 0)
            {
                _chanceToGetSkill -= customAmount;
            }
            else
            {
                _chanceToGetSkill -= REDUCE_SKILL_CHANCE;
            }
            if (_chanceToGetSkill < MIN_CHANCE_TO_GET_SKILL)
            {
                _chanceToGetSkill = MIN_CHANCE_TO_GET_SKILL;
            }
        }

        public void ReduceChanceToGetPlayerBuff(int customAmount = 0)
        {
            if (customAmount > 0)
            {
                _chanceToGetPlayerBuff -= customAmount;
            }
            else
            {
                _chanceToGetPlayerBuff -= REDUCE_PLAYER_BUFF_CHANCE;
            }
        
            if (_chanceToGetPlayerBuff < MIN_CHANCE_TO_GET_PLAYER_BUFF)
            {
                _chanceToGetPlayerBuff = MIN_CHANCE_TO_GET_PLAYER_BUFF;
            }
        }

        public RarityListHolder GetRarityListHolder()
        {
            return _rarityListHolder;
        }

        public RarityDefinition GetModRarity(float probability)
        {
            return _rarityListHolder.GetRarityByThreshold(probability);
        }

        /// <summary>
        /// Activates the buff selection UI and pauses the game.
        /// /// </summary>
        public bool IsSelectingBuffs()
        {
            return _isSelectingBuffs;
        }

        public bool IsMiddleOfChoosing()
        {
            return _middleOfChoosing;
        }

        public void SetSelectingBuffs(bool isSelectingBuffs)
        {
            _isSelectingBuffs = isSelectingBuffs;
        }

        private bool OnPlayerLevelChange(int playerLevel)
        {
            Debug.Log($"<color=#1db8fb>[ManagerBuffs]</color> Player level changed: {playerLevel}");
            if (_isSelectingBuffs)
            {
                _choosesLeft++;
                return true;
            }

            if (ManagerPause.IsPaused())
            {
                _middleOfChoosing = true;
                return true;
            }

            OnChooseBuffs();
            return true;
        }

        private void OnChooseBuffs()
        {
            _isSelectingBuffs = true;
            ManagerUI.Instance.OpenPage(PAGE_TYPE.CHOOSE_BUFFS);
            Events.OnBuffCardsOpen.Invoke();
            _middleOfChoosing = true;
        }

        private void CloseChooseBuffs()
        {
            ManagerUI.Instance.ClosePage(PAGE_TYPE.CHOOSE_BUFFS);
            ManagerPause.RemovePause(PAUSE_REASON.BUFF_CARDS);
            _isSelectingBuffs = false;
        }

        public void OnChooseCardsOpen()
        {
            ManagerPause.AddPause(PAUSE_REASON.BUFF_CARDS);
        }

        public BuffCard GetRandomBuffCard()
        {
            if (Random.Range(0, 100) < _chanceToGetPlayerBuff)
            {
                BuffCard_PlayerBuff buffCard_PlayerBuff = new BuffCard_PlayerBuff();
                bool found = buffCard_PlayerBuff.Roll();

                if (found)
                    return buffCard_PlayerBuff;
            }

            if (Random.Range(0, 100) < _chanceToGetSkill)
            {
                BuffCard_Skill buffcard_skill = new BuffCard_Skill();
                bool found = buffcard_skill.Roll();

                if (found)
                    return buffcard_skill;
            }

            BuffCard_SkillBuff buffCard = new BuffCard_SkillBuff();
            buffCard.Roll();
            return buffCard;
        }

        public List<BuffCard> GetBuffCards(int count)
        {
            if (_buffCards.Count > 0)
                return _buffCards;

            ClearRolledModifiers();
            _buffCards.Clear();
            for (int i = 0; i < count; i++)
            {
                BuffCard buffCard = GetRandomBuffCard();
                _buffCards.Add(buffCard);
            }
            return _buffCards;
        }
      
        public List<BuffDefinition> GetBuffDefinitions()
        {
            return _buffDefinitions;
        }

        public BuffDefinition GetSkillBuffDefinition(SKILL_NAME skillName)
        {
            foreach (BuffDefinition buff in _buffDefinitions)
            {
                if (buff.TargetSkill == skillName && !buff.IsTargetPlayer)
                {
                    return buff;
                }
            }
            return null;
        }

        public BuffDefinition GetPlayerBuffDefinition()
        {
            foreach (BuffDefinition buff in _buffDefinitions)
            {
                if (buff.IsTargetPlayer)
                {
                    return buff;
                }
            }
            return null;
        }

        public string GetModifierString(SKILL_NAME skillname, Modifier modifier)
        {
            BuffDefinition buffDefinition = GetSkillBuffDefinition(skillname);
            if (buffDefinition != null)
            {
                return buffDefinition.GetModifierString(modifier);
            }
            else
            {
                Debug.LogWarning($"No buff definition found for skill: {skillname}");
                return modifier.ToString();
            }
        }

        public void ClearRolledModifiers()
        {
            _rolledModifiers.Clear();
            _probabilities = new float[ROLL_AMOUNT];
        }

        public void SetRollModifierForBuff(int index, List<Modifier> modifiers)
        {
            if(_rolledModifiers.ContainsKey(index))
            {
                _rolledModifiers[index] = modifiers;
            }
            else
            {
                _rolledModifiers.Add(index, modifiers);
            }
        }

        public void SetRollProbability(int index, float probability)
        {
            _probabilities[index] = probability;
        }

        public void CheckChoosesLeft()
        {
            if (_choosesLeft-- <= 0)
            {
                _isSelectingBuffs = false;
                ManagerUI.Instance.ClosePage(PAGE_TYPE.CHOOSE_BUFFS);
                ManagerPause.RemovePause(PAUSE_REASON.BUFF_CARDS);

                _choosesLeft = 0;
            }
            else
            {
                OnChooseBuffs();
            }
        }

        public StatSystemIcons GetStatSystemIcons()
        {
            return _statSystemIcons;
        }

        public float GetChanceToGetSecondBuffPercent()
        {
            float playerLuck = Player.Instance.GetStatList().GetStat(STAT_TYPE.LUCK).GetValue();
            return _chance_to_get_second_buff + playerLuck;
        }

        public float GetChanceToGetThirdBuffPercent()
        {
            float playerLuck = Player.Instance.GetStatList().GetStat(STAT_TYPE.LUCK).GetValue();
            return _chance_to_get_third_buff + playerLuck;
        }

        [Button]
        [PropertyOrder(-1)]
        public void TestGETBuff()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Getting Buffs; This method should only be called in play mode.");
                return;
            }
            
            OnPlayerLevelChange(-1);
        }

        public bool IsAbleToBuyBlessing(BlessingUpgrade currentUpgrade)
        {
            if (currentUpgrade == null)
            {
                Debug.LogWarning("Current upgrade is null, cannot check if able to buy blessing.");
                return false;
            }

            List<CurrencyAmount> cost = currentUpgrade.GetCostForCurrentRank();
            if (cost == null || cost.Count == 0)
            {
                Debug.LogWarning("No cost defined for current rank, cannot buy blessing.");
                return false;
            }

            bool hasEnough = true;
            foreach (CurrencyAmount currencyAmount in cost)
            {
                int amount = ManagerCurrency.Instance.GetBalance(currencyAmount.CurrencyType);
                if (amount < currencyAmount.Amount)
                {
                    hasEnough = false;
                    Debug.LogWarning($"Not enough {currencyAmount.CurrencyType} to buy blessing. Required: {currencyAmount.Amount}, Available: {amount}");
                    break;
                }
            }

            return hasEnough;
        }

        public void BuyBlessing(List<CurrencyAmount> cost)
        {
            if (cost == null || cost.Count == 0)
            {
                Debug.LogWarning("No cost defined for current rank, cannot buy blessing.");
                return;
            }

            bool buyHappen = ManagerCurrency.Instance.TrySpendCurrency(cost);

            if (!buyHappen)
            {
                Debug.LogWarning("Not enough currency to buy blessing.");
                return;
            }

            _blessingUpgradeGrouper.MarkDirty();
        }

        public void RefundBlessing(BlessingUpgrade currentUpgrade)
        {
            if (currentUpgrade == null)
            {
                Debug.LogWarning("Current upgrade is null, cannot refund blessing.");
                return;
            }

            List<CurrencyAmount> refundCost = currentUpgrade.GetCostForCurrentRank();
            if (refundCost == null || refundCost.Count == 0)
            {
                Debug.LogWarning("No refund cost defined for current rank, cannot refund blessing.");
                return;
            }

            ManagerCurrency.Instance.AddCurrency(refundCost);

            _blessingUpgradeGrouper.MarkDirty();
        }
        
        public List<Modifier> GetBlessings()
        {
            return _blessingUpgradeGrouper.GetAllModifiers();
        }

        [Button, PropertyOrder(100)]
        [GUIColor(1f, 0.5f, 0.5f)][PropertySpace(10)]
        public void ResetAllBlessings()
        {
            if (_blessingUpgradeGrouper == null)
            {
                Debug.LogWarning("BlessingUpgradeGrouper is not initialized, cannot reset blessings.");
                return;
            }

            _blessingUpgradeGrouper.ResetAllBlessings();
            Debug.Log("All blessings have been reset.");
        }

        /// <summary>
        /// On event call after buff is pressed and other actions are taken.
        /// </summary>
        public void OnPreSelectBuff(int index)
        {
            _buffCards.Clear();
            _middleOfChoosing = false;
            ReduceChanceToGetPlayerBuff(REDUCE_PLAYER_BUFF_CHANCE_PER_ANY_BUFF);
            ReduceChanceToGetSkill(REDUCE_SKILL_CHANCE_PER_ANY_BUFF);
        }
    }
}