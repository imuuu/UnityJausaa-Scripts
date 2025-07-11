using Game.StatSystem;
using UnityEngine;
namespace Game.SkillSystem
{
    public abstract class Ability_MagicShield : Ability, IManualEnd, ISkillGroupItemTrigger
    {
        [SerializeField] private GameObject _shieldVisualPrefab;

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
                GameObject go = new GameObject("MagicShield_OrbitalVisuals");
                _orbitalVisual = go.AddComponent<OrbitalVisuals>();
                _orbitalVisual.Target = target.transform;
                _orbitalVisual.transform.localPosition = Vector3.zero;
                _orbitalVisual.SingleInstanceMode = true;
                _orbitalVisual.RemoveExistingOnAdd = true;
                _orbitalVisual.HeightPivot = 1f;
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
            if (_orbitalVisual != null)
            {
                _orbitalVisual.ClearByPrefab(_shieldVisualPrefab);
            }

            //Events.OnBlockHappened.RemoveListener(OnBlockHappened);
            
            Debug.Log("Magic Shield ended, removing block chance modifier.");
        }

        protected abstract void OnBlockHappened(IDamageDealer dealer, IDamageReceiver receiver);

        public void OnSkillGroupItemTrigger(ISkillEventContext context)
        {
            if (!(context is BlockEventContext blockContext))
            {
                Debug.LogWarning("Invalid context type for OnSkillGroupItemTrigger.");
                return;
            }

            IDamageDealer dealer = blockContext.DamageDealer;
            IDamageReceiver receiver = blockContext.DamageReceiver;
            OnBlockHappened(dealer, receiver);
        }
    }
}