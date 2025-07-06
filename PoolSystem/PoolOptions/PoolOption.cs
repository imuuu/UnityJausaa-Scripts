using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Game.PoolSystem
{

    [System.Serializable]
    [DisableInPlayMode]
    public class PoolOptions
    {
        [PropertySpace(SpaceBefore = 2, SpaceAfter = 7)]
        [BoxGroup("Pool Size", CenterLabel = true)]
        [HorizontalGroup("Pool Size/MinMax")]
        [LabelWidth(30)]
        [MinValue(1)]
        [OnValueChanged("MinValueChanged")]
        public int Min = 10;
        [HorizontalGroup("Pool Size/MinMax")]
        [LabelWidth(30)]
        [MinValue(0)]
        [HideIf("PoolType", POOL_TYPE.DYNAMIC)]
        public int Max = 0;

        [EnumToggleButtons]
        [PropertySpace(SpaceBefore = 3, SpaceAfter = 3)]
        [BoxGroup("Pool type", ShowLabel = false)]
        public POOL_TYPE PoolType = POOL_TYPE.DYNAMIC;
        [EnumToggleButtons]
        [PropertySpace(SpaceBefore = 7, SpaceAfter = 3)]
        [BoxGroup("Return", ShowLabel = false)]
        public POOL_RETURN_TYPE ReturnType = POOL_RETURN_TYPE.MANUAL;

        [PropertySpace(SpaceBefore = 2, SpaceAfter = 5)]
        [BoxGroup("Return", ShowLabel = false)]
        [ShowIf("ReturnType", POOL_RETURN_TYPE.TIMED)]
        [MinValue(0)]
        public float ReturnDelay = 0f;

        [PropertySpace(SpaceBefore = 2, SpaceAfter = 5)]
        [BoxGroup("Event Trigger", ShowLabel = false)]
        [EnumToggleButtons]
        public POOL_EVENT_TRIGGER_TYPE EventTriggerType = POOL_EVENT_TRIGGER_TYPE.NONE;

        //====================================================================================================

        [BoxGroup("LifeTime", ShowLabel = false)]
        [ToggleLeft]
        [OnValueChanged("EnableLifeTimeChanged")]
        [SerializeField] private bool EnableLifeTime = false;

        [BoxGroup("LifeTime", ShowLabel = false)]
        [ToggleLeft]
        [ShowIf("EnableLifeTime")]
        [Tooltip("If false, the death effect will not be triggered")]
        public bool IsLifeTimeDeathEffect = false;
        [BoxGroup("LifeTime", ShowLabel = false)]
        [ShowIf("EnableLifeTime")]
        [MinValue(0)]
        public float LifeTimeDuration = 0f;

        //====================================================================================================

        // [BoxGroup("DelayBeforeReturn", ShowLabel = false)]
        // [ToggleLeft]
        // [OnValueChanged("EnableDelayBeforeDeathChanged")]
        // [SerializeField] private bool EnableDelayBeforeDeath = false;

        // [BoxGroup("DelayBeforeReturn", ShowLabel = false)]
        // [ShowIf("EnableDelayBeforeDeath")]
        // [MinValue(0)]
        // public float DelayBeforeDeath = 0f;
        [BoxGroup("DelayBeforeReturn Options", ShowLabel = false)]
        [SerializeField] public DelayBeforeReturn DelayBeforeReturn = new DelayBeforeReturn();

        //====================================================================================================
        // private bool IsDeathEffectParticle = false; private void ThisMethodIsNotUsed() { bool notUsed = IsDeathEffectParticle; } // Just to remove the warning

        // [BoxGroup("DeathEffect", ShowLabel = false)]
        // [ToggleLeft]
        // [OnValueChanged("EnableDeathEffectChanged")]
        // [SerializeField] private bool EnableDeathEffect = false;

        // [BoxGroup("DeathEffect", ShowLabel = false)]
        // [PropertySpace(SpaceBefore = 8, SpaceAfter = 8)]
        // [ShowIf("EnableDeathEffect")]
        // [InfoBox("This will be spawned when the object is returned to the pool. If this object is also pooled it will be automatically get")]
        // [AssetsOnly]
        // [ShowInInspector] public GameObject DeathEffectPrefab;

        // [BoxGroup("DeathEffect", ShowLabel = false)]
        // [ShowIf("EnableDeathEffect")]
        // [ToggleLeft]
        // public bool IsDeathPosition = true;

        // [BoxGroup("DeathEffect", ShowLabel = false)]
        // [ShowIf("EnableDeathEffect")]
        // [ToggleLeft]
        // [Tooltip("if true, the duration of the particle system will be used as the duration of life of the object")]
        // [OnValueChanged("EnableParticleDurationCheck")]
        // [MinValue(0)]
        // public bool EnableParticleDuration = false;

        // [BoxGroup("DeathEffect", ShowLabel = false)]
        // [ShowIf("EnableDeathEffect")]
        // [MinValue(0)]
        // [SerializeField] private float _deathParticleDuration = 0;

        //====================================================================================================

        [BoxGroup("DeathEffect Options", ShowLabel = false)]
        [SerializeField]
        public DeathEffectOptions DeathEffectOptions = new ();

        [BoxGroup("HealthDetection", ShowLabel = false)]
        [ToggleLeft]
        public bool EnableHealthDetection = false;

        [BoxGroup("HealthDetection", ShowLabel = false)]
        [ShowIf("EnableHealthDetection")]
        public HealthTriggerThreshold[] HealthThresholds;

        //====================================================================================================

        // private void EnableDeathEffectChanged()
        // {
        //     if (!EnableDeathEffect)
        //     {
        //         DeathEffectPrefab = null;
        //     }
        // }
        private void EnableLifeTimeChanged()
        {
            if (!EnableLifeTime)
            {
                LifeTimeDuration = 0f;
            }
        }


        private void MinValueChanged()
        {
            if (Max < Min)
            {
                Max = Min;
            }
        }

        // public void EnableParticleDurationCheck()
        // {
        //     if(DeathEffectPrefab == null) return;

            
        //     if(DeathEffectPrefab.TryGetComponent<ParticleSystem>(out ParticleSystem particleSystem))
        //     {
        //         IsDeathEffectParticle = true;
        //         _deathParticleDuration = particleSystem.main.duration;
        //     }

        // }

        // public float GetDeathParticleDuration()
        // {
        //     return _deathParticleDuration;
        // }

        // public void SetDeathEffectPrefab(PoolDeathEffect poolDeathEffect)
        // {
        //     if(prefab == null) 
        //     {
        //         DeathEffectPrefab = null;
        //         EnableDeathEffect = false;
        //         return;
        //     }

        //     DeathEffectPrefab = prefab;
        //     EnableDeathEffect = true;
        // }

    }
}