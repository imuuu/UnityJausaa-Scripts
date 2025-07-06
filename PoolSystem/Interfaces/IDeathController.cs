namespace Game.PoolSystem
{
    public interface IDeathController
    {
        public bool IsControlledByPool();
        public void SetControlledByPool(bool value);
        public bool IsManualControlled();
        public void OnReturnToPool();
    }
}