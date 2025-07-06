namespace Game.PoolSystem
{
    public enum POOL_RETURN_TYPE
    {
        MANUAL,     // Object stays active until manually returned.
        TIMED,       // Object automatically returns after a certain duration.
        DETECT_DISABLE // Object automatically returns when OnDisable is called.
    }
}