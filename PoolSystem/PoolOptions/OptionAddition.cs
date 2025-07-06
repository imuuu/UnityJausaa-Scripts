using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Game.PoolSystem
{
    /// <summary>
    /// Base class for additional options that can be loaded into a PoolOptions instance.
    /// </summary>
    [System.Serializable] 
    public abstract class OptionAddition : IEnabled
    {   
        protected bool _isModifiedFromManager = false;
        public OptionAddition(bool isModifiedFromManager = false)
        {
            _isModifiedFromManager = isModifiedFromManager;
        }

        [OnValueChanged("OnEnabledChanged")]
        [SerializeField, ToggleLeft] protected bool _enabled = false;
        public bool IsEnabled()
        {
            return _enabled;
        }

        public void SetEnable(bool enable)
        {
            _enabled = enable;
        }

        /// <summary>
        /// Loads the additional values from this option addition into the provided PoolOptions.
        /// </summary>
        /// <param name="poolOptions">The target pool options to update.</param>
        public abstract void LoadAddition(PoolOptions poolOptions);

        protected virtual void OnEnabledChanged()
        {
        }

       
    }
}
