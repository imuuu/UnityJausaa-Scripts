namespace Game.UI
{
    public interface ICursorDropReceiver
    {
        public bool CanDropItem(ICursorPickupable item);
        public void OnDropItem(ICursorPickupable item);

    }
}