using UnityEngine;
using UnityEngine.Events;
namespace Game.Interactable
{
    public class Interactable_Events : InteractableArea
    {
        [SerializeField] private UnityEvent _onInteract;
        public override bool Interact()
        {
            _onInteract.Invoke();
            return true;
        }
    }
}
