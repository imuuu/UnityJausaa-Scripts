using Game.StatSystem;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.SkillSystem
{   
    /// <summary>
    /// Base class for magic shield abilities.
    /// The ManagerSkills handdles the triggering of this ability. Trigger will be randomly selected from all ISkillGroupItemTrigger
    /// </summary>
    public abstract class Ability_MagicShield : Ability, IManualEnd, ISkillGroupItemTrigger
    {   
        [InfoBox("This is a single instance. Meaning first one will be instantiate of all shield skills")]
        [SerializeField] private GameObject _orbitalVisualPrefab;
        [SerializeField] private GameObject _shieldVisualPrefab;

        // IF THIS is for mobs, it doesnt work, cant have single orbital visuals for each mob
        private static OrbitalVisuals _orbitalVisual;

        public int SkillGroupItemID => (int)SKILL_GROUP_TAG.MAGIC_SHIELD;

        public override void AwakeSkill()
        {
            base.AwakeSkill();
        }

        public override void StartSkill()
        {
            GameObject target = GetUserTransform().gameObject;
            if (_orbitalVisual == null)
            {
                GameObject go = GameObject.Instantiate(_orbitalVisualPrefab);
                _orbitalVisual = go.GetOrAdd<OrbitalVisuals>();
                _orbitalVisual.Target = target.transform;
                _orbitalVisual.transform.localPosition = Vector3.zero;
                _orbitalVisual.SingleInstanceMode = true;
                _orbitalVisual.RemoveExistingOnAdd = true;
                //_orbitalVisual.HeightPivot = 1f;
            }

            _orbitalVisual.AddVisualPrefab(_shieldVisualPrefab);

            //Events.OnBlockHappened.AddListener(OnBlockHappened);

            Stat blockChanceStat = _baseStats.GetStat(STAT_TYPE.BLOCK_CHANCE);

            float blockChance = blockChanceStat.GetValue();
            Modifier blockChanceModifier = blockChanceStat.CreateEmptyModifier(MODIFIER_TYPE.FLAT, blockChance);
            blockChanceModifier.ID = this.GetInstanceID();

            IMainStats mainStats = GetUserTransform().GetComponent<IMainStats>();

            mainStats.GetStatList().AddOrReplaceModifier(blockChanceModifier);
            Debug.Log("STAT SET FROM MAGIC SHIELD");
        }

        public override void EndSkill()
        {
            base.EndSkill();
            if (_orbitalVisual != null)
            {
                _orbitalVisual.ClearByPrefab(_shieldVisualPrefab);
            }
        }

        protected abstract void OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver);

        public void OnSkillGroupItemTrigger(ISkillEventContext context)
        {
            if (!(context is BlockEventContext blockContext))
            {
                Debug.LogWarning("Invalid context type for OnSkillGroupItemTrigger.");
                return;
            }

            OnBlockHappened(blockContext.DamageDealer, blockContext.DamageReceiver);
        }
    }
}