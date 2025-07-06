namespace Game.SkillSystem
{
    public interface IDuration
    {
        // public float Duration { get; set; }
        // public float CurrentDuration { get; set; }

        public void SetDuration(float duration);
        public float GetDuration();

        public void SetCurrentDuration(float currentDuration);
        public float GetCurrentDuration();
    }
}