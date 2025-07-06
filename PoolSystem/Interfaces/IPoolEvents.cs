
namespace Game.PoolSystem
{
    public interface IPoolEvents
    {
        /// <summary>
        /// Called immediately after the object is activated/spawned from the pool.
        /// </summary>
        public void OnSpawnedFromPool();

        /// <summary>
        /// Called right before the object is deactivated/returned to the pool.
        /// </summary>
        public void OnReturnedToPool();
    }
}
