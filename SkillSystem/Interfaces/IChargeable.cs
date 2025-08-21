namespace Game.SkillSystem
{
    public interface IChargeable
    {
        public float GetChargeTime();
        public void OnChargingStart();
        public void OnChargingEnd();
        public void OnChargingUpdate(float chargeProgress);
    }
}