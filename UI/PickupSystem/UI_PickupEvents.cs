using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class UI_PickupEvents : MonoBehaviour, IDropItemUI, IPickupItemUI
    {
        [Title("Select Pickup Event")]
        public PickupEventType _pickupEventType;

        [ShowIf("@this._pickupEventType == PickupEventType.ON_PICKUP || this._pickupEventType == PickupEventType.ALL")]
        [SerializeField] private UnityEvent _onPickupEvent;

        [ShowIf("@this._pickupEventType == PickupEventType.ON_DROP || this._pickupEventType == PickupEventType.ALL")]
        [SerializeField] private UnityEvent _onDropEvent;

        public enum PickupEventType
        {
            NONE,
            ON_PICKUP,
            ON_DROP,
            ALL,
        }

        public bool IsAbleToDrop(ICursorPickupable item)
        {
            return true;
        }

        public void OnDropItem(ICursorPickupable item)
        {
            if(_pickupEventType == PickupEventType.ON_DROP 
            || _pickupEventType == PickupEventType.ALL)
                _onDropEvent?.Invoke();

        }

        public void OnPickup(ICursorPickupable item)
        {
            if(_pickupEventType == PickupEventType.ON_PICKUP 
            || _pickupEventType == PickupEventType.ALL)
                _onPickupEvent?.Invoke();
        }
    }

}