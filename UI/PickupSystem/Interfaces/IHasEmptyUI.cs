namespace UI
{
    public interface IHasEmptyUI
    {
        public bool IsPossibleToEmpty();

        //<summary>Called when the UI is empty</summary>
        public void OnEmpty();

        public bool IsEmpty();

        //<summary>Called when the UI is restored</summary>
        public void OnRestore();
    }
}