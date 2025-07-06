
namespace Game.UI
{
    public interface IDropItemUI
    {
        /// <summary>
        /// Check if the item can be dropped on this UI
        /// </summary>
        public bool IsAbleToDrop(ICursorPickupable item);

        /// <summary>
        /// Called when the item is dropped on this UI
        /// 
        public void OnDropItem(ICursorPickupable item);
    }
}
