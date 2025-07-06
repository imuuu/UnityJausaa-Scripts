namespace Game.SkillSystem
{
    public interface ICooldown
    {
        // public float CurrentCooldown { get; set; }
        // public float Cooldown { get; set; }

        public void SetCooldown(float cooldown);
        public float GetCooldown();

        public void SetCurrentCooldown(float currentCooldown);
        public float GetCurrentCooldown();
    }
}