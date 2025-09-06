using UnityEngine;

namespace Game.BuffSystem
{
    public class TriggerBuffChoose : MonoBehaviour
    {
        [SerializeField] private BUFF_OPEN_TYPE _buffOpenType = BUFF_OPEN_TYPE.NONE;
        
        public void TriggerChooseBuffs()
        {
            TriggerChooseBuffs(_buffOpenType);    
        }

        public void TriggerChooseBuffs(BUFF_OPEN_TYPE openType)
        {
            if (openType == BUFF_OPEN_TYPE.NONE)
            {
                return;
            }

            Debug.Log($"<color=#1db8fb>[TriggerBuffChoose]</color> Triggering choose buffs of type: {openType}");
            ManagerBuffs.Instance.TriggerChooseBuffs(openType);
        }
    }
}