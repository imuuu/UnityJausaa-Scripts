using System;
using JetBrains.Annotations;
using Nova;
using Sirenix.OdinInspector;
using UI.Animations;
using UnityEngine;

namespace Game.UI
{
    public abstract class UI_ColorAnimationBase : MonoBehaviour
    {
        [System.Serializable]
        public class ColorObject
        {
            public UIBlock Block;

            [ToggleLeft] public bool EnableCustomDuration = false;
            [ShowIf("EnableCustomDuration"), Min(0)] public float _customDuration = 0f;

            [ToggleLeft] public bool EnableCustomBody = false;
            [ShowIf("EnableCustomBody")] public BodyColorAnimationHidden BodyAnimation;
            [HideInInspector] public BodyColorAnimationHidden _unhoverBodyAnimation;
            public AnimationHandle _hoverBodyHandle;

            [ToggleLeft] public bool EnableCustomGradient = false;
            [ShowIf("EnableCustomGradient")] public GradientColorAnimationHidden GradientAnimation;
            [HideInInspector] public GradientColorAnimationHidden _unhoverGradientAnimation;
            public AnimationHandle _hoverGradientHandle;
        }

        [InfoBox("Everything _mainUiBlock does, will be transferred to extra blocks if customs are not set. MAIN DURATION NEEDS TO BE SET IN MAIN BLOCK.")]
        [BoxGroup("Reference")]
        [SerializeField] protected ColorObject _mainUiBlock;
        [BoxGroup("Reference"), PropertySpace(10, 10)]
        [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
        [SerializeField] protected ColorObject[] _extraBlocks;

        protected virtual void Start()
        {
            SetupMainAnimations();
            SetupExtraAnimations();
        }

        /// <summary>
        /// Sets up the main UIBlock animations using the main HoverColorObject (_uiBlock).
        /// </summary>
        protected void SetupMainAnimations()
        {
            if (_mainUiBlock != null && _mainUiBlock.Block != null)
            {
                // Body Animation
                if (!_mainUiBlock.EnableCustomBody)
                {
                    _mainUiBlock.BodyAnimation = new BodyColorAnimationHidden();
                    _mainUiBlock.BodyAnimation.Target = _mainUiBlock.Block;
                }
                else
                {
                    _mainUiBlock.BodyAnimation.Target = _mainUiBlock.Block;
                }
                _mainUiBlock._unhoverBodyAnimation = new BodyColorAnimationHidden();
                _mainUiBlock._unhoverBodyAnimation.Target = _mainUiBlock.Block;
                _mainUiBlock._unhoverBodyAnimation.TargetColor = _mainUiBlock.Block.Color;

                // Gradient Animation (only if Block is UIBlock2D)
                if (_mainUiBlock.Block is UIBlock2D mainBlock2D)
                {
                    if (!_mainUiBlock.EnableCustomGradient)
                    {
                        _mainUiBlock.GradientAnimation = new GradientColorAnimationHidden();
                        _mainUiBlock.GradientAnimation.Target = mainBlock2D;
                    }
                    else
                    {
                        _mainUiBlock.GradientAnimation.Target = mainBlock2D;
                    }
                    _mainUiBlock._unhoverGradientAnimation = new GradientColorAnimationHidden();
                    _mainUiBlock._unhoverGradientAnimation.Target = mainBlock2D;
                    _mainUiBlock._unhoverGradientAnimation.TargetGradient = mainBlock2D.Gradient.Color;
                }
            }
        }

        /// <summary>
        /// Sets up extra blocks animations.
        /// </summary>
        protected void SetupExtraAnimations()
        {
            if (_extraBlocks != null && _extraBlocks.Length > 0)
            {
                foreach (ColorObject extra in _extraBlocks)
                {
                    if (extra.Block != null)
                    {
                        // Body Animation
                        if (!extra.EnableCustomBody)
                        {
                            extra.BodyAnimation = new BodyColorAnimationHidden();
                            extra.BodyAnimation.Target = extra.Block;
                        }
                        else
                        {
                            extra.BodyAnimation.Target = extra.Block;
                        }
                        extra._unhoverBodyAnimation = new BodyColorAnimationHidden();
                        extra._unhoverBodyAnimation.Target = extra.Block;
                        extra._unhoverBodyAnimation.TargetColor = extra.Block.Color;

                        // Gradient Animation (only if Block is UIBlock2D)
                        if (extra.Block is UIBlock2D extraBlock2D)
                        {
                            if (!extra.EnableCustomGradient)
                            {
                                extra.GradientAnimation = new GradientColorAnimationHidden();
                                extra.GradientAnimation.Target = extraBlock2D;
                            }
                            else
                            {
                                extra.GradientAnimation.Target = extraBlock2D;
                            }
                            extra._unhoverGradientAnimation = new GradientColorAnimationHidden();
                            extra._unhoverGradientAnimation.Target = extraBlock2D;
                            extra._unhoverGradientAnimation.TargetGradient = extraBlock2D.Gradient.Color;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Activates the animations (for example, on hover or fade in).
        /// </summary>
        [Button("Test Animations (RUNTIME)"), PropertySpace(15)]
        public void ActivateAnimations()
        {
            if (_mainUiBlock != null && _mainUiBlock.Block != null)
            {
                float mainDuration = _mainUiBlock.EnableCustomDuration ? _mainUiBlock._customDuration : 0.15f;
                _mainUiBlock._hoverBodyHandle.Cancel();
                _mainUiBlock._hoverBodyHandle = _mainUiBlock.BodyAnimation.Run(mainDuration);

                if (_mainUiBlock.Block is UIBlock2D)
                {
                    _mainUiBlock._hoverGradientHandle.Cancel();
                    _mainUiBlock._hoverGradientHandle = _mainUiBlock.GradientAnimation.Run(mainDuration);
                }
            }

            if (_extraBlocks != null)
            {
                foreach (ColorObject extra in _extraBlocks)
                {
                    if (extra.Block != null)
                    {
                        float duration = extra.EnableCustomDuration ? extra._customDuration : 0.15f;
                        extra._hoverBodyHandle.Cancel();
                        extra._hoverBodyHandle = extra.BodyAnimation.Run(duration);
                        if (extra.Block is UIBlock2D)
                        {
                            extra._hoverGradientHandle.Cancel();
                            extra._hoverGradientHandle = extra.GradientAnimation.Run(duration);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deactivates the animations (for example, on unhover or fade out).
        /// </summary>
        protected void DeactivateAnimations()
        {
            if (_mainUiBlock != null && _mainUiBlock.Block != null)
            {
                float mainDuration = _mainUiBlock.EnableCustomDuration ? _mainUiBlock._customDuration : 0.15f;
                _mainUiBlock._hoverBodyHandle.Cancel();
                _mainUiBlock._hoverBodyHandle = _mainUiBlock._unhoverBodyAnimation.Run(mainDuration);
                if (_mainUiBlock.Block is UIBlock2D)
                {
                    _mainUiBlock._hoverGradientHandle.Cancel();
                    _mainUiBlock._hoverGradientHandle = _mainUiBlock._unhoverGradientAnimation.Run(mainDuration);
                }
            }

            if (_extraBlocks != null)
            {
                foreach (ColorObject extra in _extraBlocks)
                {
                    if (extra.Block != null)
                    {
                        float duration = extra.EnableCustomDuration ? extra._customDuration : 0.15f;
                        extra._hoverBodyHandle.Cancel();
                        extra._hoverBodyHandle = extra._unhoverBodyAnimation.Run(duration);
                        if (extra.Block is UIBlock2D)
                        {
                            extra._hoverGradientHandle.Cancel();
                            extra._hoverGradientHandle = extra._unhoverGradientAnimation.Run(duration);
                        }
                    }
                }
            }
        }

        #region Runtime Color‐Change API

        /// <summary>
        /// Change the body color of any UIBlock (main or extra) and update its unhover target.
        /// </summary>
        public void ChangeBodyColor(UIBlock block, Color newColor)
        {
            var obj = FindColorObject(block);
            if (obj == null) return;

            // apply immediately…
            obj.Block.Color = newColor;
            // …and make sure the “unhover” animation will return to this new color
            obj._unhoverBodyAnimation.TargetColor = newColor;
        }

        /// <summary>
        /// Change the gradient of any UIBlock2D (main or extra) and update its unhover target.
        /// </summary>
        public void ChangeGradient(UIBlock2D block2D, Color newGradient)
        {
            var obj = FindColorObject(block2D);
            if (obj == null) return;

            // apply immediately…
            block2D.Gradient.Color = newGradient;
            // …and make sure the “unhover” animation will return to this new gradient
            obj._unhoverGradientAnimation.TargetGradient = newGradient;
        }

        /// <summary>
        /// Convenience: change main‐block body color.
        /// </summary>
        public void ChangeMainBodyColor(Color newColor) =>
            ChangeBodyColor(_mainUiBlock.Block, newColor);

        /// <summary>
        /// Convenience: change an extra‐block’s body color by array index.
        /// </summary>
        public void ChangeExtraBodyColor(int index, Color newColor)
        {
            if (_extraBlocks == null || index < 0 || index >= _extraBlocks.Length) return;
            ChangeBodyColor(_extraBlocks[index].Block, newColor);
        }

        /// <summary>
        /// Convenience: change main‐block gradient.
        /// </summary>
        public void ChangeMainGradient(Color newGradient) =>
            ChangeGradient(_mainUiBlock.Block as UIBlock2D, newGradient);

        /// <summary>
        /// Convenience: change an extra‐block’s gradient by array index.
        /// </summary>
        public void ChangeExtraGradient(int index, Color newGradient)
        {
            if (_extraBlocks == null || index < 0 || index >= _extraBlocks.Length) return;
            ChangeGradient(_extraBlocks[index].Block as UIBlock2D, newGradient);
        }

        /// <summary>
        /// Internal helper to map any UIBlock/UIBlock2D back to its ColorObject.
        /// </summary>
        private ColorObject FindColorObject(UIBlock block)
        {
            if (_mainUiBlock != null && _mainUiBlock.Block == block)
                return _mainUiBlock;

            if (_extraBlocks != null)
                foreach (var extra in _extraBlocks)
                    if (extra.Block == block)
                        return extra;

            return null;
        }

        /// <summary>
        /// Change the hover color of the main UIBlock, updating the hover animation target.
        /// </summary>
        public void ChangeHoverBodyColor(Color hoverColor)
        {
            if (_mainUiBlock?.Block == null) return;
            _mainUiBlock.EnableCustomBody = true;
            _mainUiBlock.BodyAnimation.TargetColor = hoverColor;
        }

        /// <summary>
        /// Change the hover gradient color of the main UIBlock2D, updating the hover gradient animation target.
        /// </summary>
        public void ChangeHoverGradient(Color hoverGradientColor)
        {
            if (_mainUiBlock?.Block is UIBlock2D)
            {
                _mainUiBlock.EnableCustomGradient = true;
                _mainUiBlock.GradientAnimation.TargetGradient = hoverGradientColor;
            }
        }

        #endregion
    }
}
