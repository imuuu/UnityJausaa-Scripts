namespace Game.UI
{
    public interface IDetachable
    {   
        public bool IsDetachable();
        public void OnDetach();
    }
}