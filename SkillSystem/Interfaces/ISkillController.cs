namespace Game.SkillSystem
{
    public interface ISkillController
    {
        public OWNER_TYPE GetUserType();
        public void OnSkillUpdate();

    }
}