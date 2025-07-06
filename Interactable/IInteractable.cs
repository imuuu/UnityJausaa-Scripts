namespace Game.Interactable
{
    public interface IInteractable
    {
        /// <summary>
        /// What happens when the player interacts (e.g. presses the key).
        /// Return true if interaction succeeded (optional).
        /// </summary>
        public bool Interact();
    }

}
