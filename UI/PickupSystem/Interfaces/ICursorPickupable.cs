using Nova;

namespace Game.UI
{
    public interface ICursorPickupable
    {
        public bool IsEnable();
        public UIBlock GetItem();
        public bool IsCloneOnPickup();
        public bool AutoDisableInteractable();
        public void OnPickup();
        //public void OnDrop();
    }
}