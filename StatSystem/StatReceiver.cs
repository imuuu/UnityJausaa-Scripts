using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.StatSystem
{
    public class StatReceiver : MonoBehaviour, IStatReceiver
    {   

        [BoxGroup("Receiver Target")]
        [SerializeField] private STAT_TYPE _target;

        [BoxGroup("Scale")]
        [SerializeField, ToggleLeft] private bool _applyStatToScale = false;
        [BoxGroup("Scale")]
        [SerializeField, ShowIf("_applyStatToScale")] private bool _applyScaleToX = false;
        [BoxGroup("Scale")]
        [SerializeField, ShowIf("_applyStatToScale")] private bool _applyScaleToY = false;
        [BoxGroup("Scale")]
        [SerializeField, ShowIf("_applyStatToScale")] private bool _applyScaleToZ = false;
        private Stat _stat;

        private StatList _statList;

        public void SetStat(Stat stat)
        {
            _stat = stat;
            OnStatChange(_stat);
        }

        protected virtual void OnStatChange(Stat stat)
        {
            if (_applyStatToScale)
            {
                //float value = stat.GetValue() * 2f; idk why 2
                float value = stat.GetValue();

                Vector3 scale = transform.localScale;
                if (_applyScaleToX) scale.x = value;
                if (_applyScaleToY) scale.y = value;
                if (_applyScaleToZ) scale.z = value;
                transform.localScale = scale;
            }
        }

        public bool HasStat(STAT_TYPE type)
        {
            if (_stat == null)
                return false;

            return _stat.GetTags().Contains(type);
        }

        public StatList GetStats()
        {
            if (_stat == null)
            {
                _statList = new StatList();
            }
            _statList.AddStat(_stat);
            return _statList;
        }

        public void SetStats(StatList statList)
        {
            _statList = statList;
        }
    }
   
}