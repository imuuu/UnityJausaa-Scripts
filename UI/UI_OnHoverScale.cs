using System;
using Nova;
using UnityEngine;
using UI.Animations;

namespace Game.UI
{
    public class UI_OnHoverScale : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("UIBlock component that handles gesture events.")]
        [SerializeField] private UIBlock _block;

        [Header("Scale Settings")]
        [Tooltip("Multiplier applied to the original scale on hover.")]
        [SerializeField] private float _scaleMultiplier = 1.2f;
        [SerializeField] private float _scaleDuration = 0.15f;

        private ScaleAnimationHidden _hoverScaleAnimation;
        private ScaleAnimationHidden _unhoverScaleAnimation;

        public AnimationHandle _hoverScaleHandle;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (_block == null)
            {
                _block = GetComponent<UIBlock>();
            }
            if (_block == null)
            {
                Debug.LogError("UIBlock reference not found for UI_OnHoverScale on " + gameObject.name);
            }

            _originalScale = transform.localScale;

            _hoverScaleAnimation.Target = transform;
            _hoverScaleAnimation.TargetScale = _originalScale * _scaleMultiplier;

            _unhoverScaleAnimation.Target = transform;
            _unhoverScaleAnimation.TargetScale = _originalScale;
        }

        private void Start()
        {
            SetupGestureHandlers();
        }

        private void SetupGestureHandlers()
        {
            _block.AddGestureHandler<Gesture.OnHover>(OnHover);
            _block.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
        }

        private void OnHover(Gesture.OnHover evt)
        {
            ActivateAnimations();
            // Optionally, fire custom events here.
        }

        private void OnUnhover(Gesture.OnUnhover evt)
        {
            DeactivateAnimations();
        }

        public void ActivateAnimations()
        {
            if (_hoverScaleHandle != null)
            {
                _hoverScaleHandle.Cancel();
            }
            _hoverScaleHandle = _hoverScaleAnimation.Run(_scaleDuration);
        }

        public void DeactivateAnimations()
        {
            if (_hoverScaleHandle != null)
            {
                _hoverScaleHandle.Cancel();
            }
            _hoverScaleHandle = _unhoverScaleAnimation.Run(_scaleDuration);
        }
    }
}
