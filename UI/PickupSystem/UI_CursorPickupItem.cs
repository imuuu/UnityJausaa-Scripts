using Nova;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class UI_CursorPickupItem : MonoBehaviour, ICursorPickupable
    {
        private UI_HandlerCursorItems _handlerCursorItems => UI_HandlerCursorItems.Instance;
        [Title("References")]
        [SerializeField] private UIBlock _item;
        [Title("Settings")]
        [SerializeField] private bool _isEnable = true;
        [SerializeField] private bool _isCloneOnPickup = false;
        [SerializeField] private bool _enableUnityEvents = false;
        
        [Title("Events")]
        [ShowIf("@this._enableUnityEvents")]
        [SerializeField] private UnityEvent _onPickupEvent;

        private void Start() 
        {
            _item.AddGestureHandler<Gesture.OnClick>(HandleClick);
        }

        private void HandleClick(Gesture.OnClick evt)
        {
            bool isPickedUp = _handlerCursorItems.TryToPickupItem(this);

            Debug.Log("Clicked and Picked up: " + isPickedUp);
        }

        public UIBlock GetItem()
        {
            return _item;
        }

        public bool IsCloneOnPickup()
        {
            return _isCloneOnPickup;
        }

        public virtual void OnPickup()
        {
            if(!_enableUnityEvents) return;

            _onPickupEvent?.Invoke();
        }

        public bool IsEnable()
        {
            return _isEnable;
        }

        public bool AutoDisableInteractable()
        {
            return true;
        }

       
    }
}
