using Nova;
using UI.Animations;
using UnityEngine;
using UnityEngine.Events;

namespace Game.UI
{
    public class UI_OnHoverColor : UI_ColorAnimationBase
    {
        [Header("Custom Hover Events")]
        public UnityEvent onHoverEvent;
        public UnityEvent onUnhoverEvent;

        protected override void Start()
        {
            base.Start();
            SetupGestureHandlers();
        }

        private void SetupGestureHandlers()
        {
            _mainUiBlock.Block.AddGestureHandler<Gesture.OnHover>(OnHover);
            _mainUiBlock.Block.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
        }

        private void OnHover(Gesture.OnHover evt)
        {
            ActivateAnimations();
            onHoverEvent?.Invoke(); // 🔥 Custom event
        }

        private void OnUnhover(Gesture.OnUnhover evt)
        {
            DeactivateAnimations();
            onUnhoverEvent?.Invoke(); // 🔥 Custom event
        }
    }
}
