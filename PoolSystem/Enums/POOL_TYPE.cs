namespace Game.PoolSystem
{
    public enum POOL_TYPE
    {
        FIXED,      // Pool has a fixed max size. If full, can't spawn unless "Recycling" is used.
        DYNAMIC,    // Pool can grow if needed.
        RECYCLING   // Pool has a fixed max size but if full, re-use the oldest active object.
    }
}
