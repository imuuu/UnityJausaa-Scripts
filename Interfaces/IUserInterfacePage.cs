
namespace Game.UI
{
    public interface IUserInterfacePage
    {
        public PAGE_TYPE GetPageType();
        public bool IsVisible();
        public void SetActive(bool active);

        //<summary>Register this page to the ManagerUI</summary>
        public void RegisterToManager();

        //<summary>Unregister this page from the ManagerUI</summary>
        public void UnregisterFromManager();
    }
}