using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.PoolSystem
{
    /// <summary>
    /// Contains the settings for a pool's death effect.
    /// </summary>
    [System.Serializable]
    public class DeathEffectOptions : OptionAddition
    {
        // A dummy field to avoid unused warnings.
        private bool _isDeathEffectParticle = false;
        private void ThisMethodIsNotUsed() { bool notUsed = _isDeathEffectParticle; } // Just to remove the warning

        #region Serialized Fields

        [BoxGroup("DeathEffect", ShowLabel = false)]
        [PropertySpace(SpaceBefore = 8, SpaceAfter = 8)]
        [ShowIf("_enabled")]
        [InfoBox("This will be spawned when the object is returned to the pool. If this object is also pooled it will be automatically get")]
        [AssetsOnly]
        [ShowInInspector]
        public GameObject DeathEffectPrefab;

        [BoxGroup("DeathEffect", ShowLabel = false)]
        [ShowIf("_enabled")]
        [ToggleLeft]
        public bool IsDeathPosition = true;

        [BoxGroup("DeathEffect", ShowLabel = false)]
        [ShowIf("_enabled")]
        [ToggleLeft]
        [Tooltip("If true, the duration of the particle system will be used as the life duration of the object")]
        [OnValueChanged("OnEnableParticleDurationCheck")]
        public bool EnableParticleDuration = false;

        [BoxGroup("DeathEffect", ShowLabel = false)]
        [ShowIf("_enabled")]
        [MinValue(0)]
        [SerializeField]
        private float _deathParticleDuration = 0f;

        #endregion

        #region Private Methods

        protected override void OnEnabledChanged()
        {

        }

        /// <summary>
        /// If particle duration is enabled, try to retrieve the duration from the prefab's ParticleSystem.
        /// </summary>
        private void OnEnableParticleDurationCheck()
        {
            if (DeathEffectPrefab == null)
                return;

            if (DeathEffectPrefab.TryGetComponent<ParticleSystem>(out ParticleSystem particleSystem))
            {
                _isDeathEffectParticle = true;
                _deathParticleDuration = particleSystem.main.duration;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the cached particle system duration.
        /// </summary>
        public float GetDeathParticleDuration() => _deathParticleDuration;

        public override void LoadAddition(PoolOptions poolOptions)
        {
            poolOptions.DeathEffectOptions = this;
        }

        #endregion
    }
}
