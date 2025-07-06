namespace Game.SkillSystem
{
    /// <summary>
    /// Interface for components that require fixed update logic.
    ///
    public interface IFixedUpdate
    {
        /// <summary>
        /// Method to be called during the fixed update phase.
        /// </summary>
        public void FixedUpdate();
    }
}