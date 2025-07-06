using Nova;
using UnityEngine;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class UI_CursorDropReceiver : MonoBehaviour, ICursorDropReceiver
    {
        private UI_HandlerCursorItems _handlerCursorItems => UI_HandlerCursorItems.Instance;
        [SerializeField] private UIBlock _uiBlock;

        private void Start() 
        {
            _uiBlock.AddGestureHandler<Gesture.OnClick>(OnClick);
        }

        private void OnClick(Gesture.OnClick evt)
        {
            Debug.Log("Clicked on Drop Receiver");
            bool success = _handlerCursorItems.TryToDropItem(this);
        }

        public virtual bool CanDropItem(ICursorPickupable item)
        {
            IDropItemUI[] dropItemUIs = _uiBlock.GetComponents<IDropItemUI>();

            foreach (IDropItemUI dropItemUI in dropItemUIs)
            {
                if (!dropItemUI.IsAbleToDrop(item))
                {
                    return false;
                }
            }
            
            return true;
        }

        public void OnDropItem(ICursorPickupable item)
        {
            Debug.Log("Dropped item HERE");

            IDropItemUI[] dropItemUIs = _uiBlock.GetComponents<IDropItemUI>();

            foreach (IDropItemUI dropItemUI in dropItemUIs)
            {
                dropItemUI.OnDropItem(item);
            }
        }


    }
}