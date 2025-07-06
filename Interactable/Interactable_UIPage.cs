using Game.UI;
using UnityEngine;
namespace Game.Interactable
{
    public class Interactable_UIPage : InteractableArea
    {
        [Tooltip("Which UI page this book should toggle.")]
        [SerializeField]
        PAGE_TYPE pageType = PAGE_TYPE.NONE;
        [SerializeField]
        private bool _openOnEntry = false;

        public override bool Interact()
        {
            ManagerUI.Instance.TogglePage(pageType);
            return true;
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            if (_openOnEntry)
            {
                ManagerUI.Instance.OpenPage(pageType);
            }
        }

        protected override void OnExit()
        {
            base.OnExit();

            ManagerUI.Instance.ClosePage(pageType);
        }
    }
}
