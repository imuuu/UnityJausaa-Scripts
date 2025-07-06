using Nova;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    public class UI_OnHover : MonoBehaviour
    {
        [SerializeField] private UIBlock _uiBlock;

        [SerializeField] private UnityEvent OnHoverEvent;
        [SerializeField] private UnityEvent OnUnhoverEvent;
        private void Start()
        {
            _uiBlock.AddGestureHandler<Gesture.OnHover>(OnHover);
            _uiBlock.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
        }

        private void OnUnhover(Gesture.OnUnhover evt)
        {
            OnUnhoverEvent?.Invoke();
        }

        private void OnHover(Gesture.OnHover evt)
        {
            OnHoverEvent?.Invoke();
        }
    }
}
