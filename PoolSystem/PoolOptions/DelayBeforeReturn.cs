using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.PoolSystem
{
    [System.Serializable]
    public class DelayBeforeReturn : OptionAddition
    {   
        [BoxGroup("Data", ShowLabel = false)]
        [PropertySpace(SpaceBefore = 8, SpaceAfter = 8)]
        [ShowIf("_enabled"), ToggleLeft]
        [Tooltip("If true, the object will be disabled before returning to the pool, for example, to stop the movement")]
        public bool TriggerSetDisableToComponents = true;

        [BoxGroup("Data", ShowLabel = false)]
        [PropertySpace(SpaceBefore = 8, SpaceAfter = 8)]
        [ShowIf("_enabled")]
        [MinValue(0)]
        public float Delay = 0f;

        [Space(5)]
        [BoxGroup("Events", ShowLabel = false)]
        [ToggleLeft]
        [ShowIf("_enabled")]
        public bool _enableEvents = false;

        [Space(10)]
        [BoxGroup("Events", ShowLabel = false)]
        [HideIf("@this._isModifiedFromManager || this._enableEvents == false")]
        public UnityEvent OnReturnStart;

        [BoxGroup("Events", ShowLabel = false)]
        [HideIf("@this._isModifiedFromManager || this._enableEvents == false")]
        public UnityEvent OnReturnEnd;

        public void OnReturnStartInvoke(GameObject gameObject)
        {
            if(TriggerSetDisableToComponents) 
            {
                IEnabled[] enableds = gameObject.GetComponents<IEnabled>();
                foreach (IEnabled enabled in enableds)
                {
                    enabled.SetEnable(false);
                }
            }
           

            if(_enableEvents) OnReturnStart?.Invoke();
        }

        public void OnReturnEndInvoke(GameObject gameObject)
        {
            if (_enableEvents) OnReturnEnd?.Invoke();
        }

        public override void LoadAddition(PoolOptions poolOptions)
        {
            poolOptions.DelayBeforeReturn = this;
        }


    }
}