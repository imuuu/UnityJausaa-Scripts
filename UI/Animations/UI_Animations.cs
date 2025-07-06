using Nova;

using System;
using UnityEngine;

namespace UI.Animations
{
    [Serializable]
    public struct BodyColorAnimation : IAnimation
    {
        public UIBlock Target;
        public Color TargetColor;
        private Color _startColor;

        public void Update(float percentDone)
        {
            if(percentDone == 0)
            {
                _startColor = Target.Color;
            }

            Target.Color = Color.Lerp(_startColor, TargetColor, percentDone);
        }
    }

    [Serializable]
    public struct BodyColorAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock Target;
        public Color TargetColor;
        private Color _startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startColor = Target.Color;
            }

            Target.Color = Color.Lerp(_startColor, TargetColor, percentDone);
        }
    }

    [Serializable]
    public struct GradientAnimation : IAnimation
    {
        public UIBlock2D Target;
        public Color TargetGradient;
        private Color _startGradient;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startGradient = Target.Gradient.Color;
            }

            Target.Gradient.Color = Color.Lerp(_startGradient, TargetGradient, percentDone);
        }
    }

    [Serializable]
    public struct GradientColorAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock2D Target;
        public Color TargetGradient;
        private Color _startGradient;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startGradient = Target.Gradient.Color;
            }

            Target.Gradient.Color = Color.Lerp(_startGradient, TargetGradient, percentDone);
        }
    }

    [Serializable]
    public struct BodyFadeoutAnimation : IAnimation
    {
        public UIBlock Target;
        private Color _startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startColor = Target.Color;
            }
            // Create a target color with the same RGB values but 0 alpha.
            Color targetFadeColor = new Color(_startColor.r, _startColor.g, _startColor.b, 0f);
            Target.Color = Color.Lerp(_startColor, targetFadeColor, percentDone);
        }
    }

    [Serializable]
    public struct BodyFadeoutAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock Target;
        private Color _startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startColor = Target.Color;
            }
            Color targetFadeColor = new Color(_startColor.r, _startColor.g, _startColor.b, 0f);
            Target.Color = Color.Lerp(_startColor, targetFadeColor, percentDone);
        }
    }

    [Serializable]
    public struct GradientFadeoutAnimation : IAnimation
    {
        public UIBlock2D Target;
        private Color _startGradient;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startGradient = Target.Gradient.Color;
            }
            // Fade out the gradient by interpolating the alpha to 0.
            Color targetFadeColor = new Color(_startGradient.r, _startGradient.g, _startGradient.b, 0f);
            Target.Gradient.Color = Color.Lerp(_startGradient, targetFadeColor, percentDone);
        }
    }

    [Serializable]
    public struct GradientFadeoutAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock2D Target;
        private Color _startGradient;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startGradient = Target.Gradient.Color;
            }
            Color targetFadeColor = new Color(_startGradient.r, _startGradient.g, _startGradient.b, 0f);
            Target.Gradient.Color = Color.Lerp(_startGradient, targetFadeColor, percentDone);
        }
    }

    [Serializable]
    public struct BodyFadeinAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock Target;
        public Color TargetColor; // This should be the original color
        private Color _startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startColor = Target.Color; // expected to be transparent
                _startColor.a = 0f;
            }
            Target.Color = Color.Lerp(_startColor, TargetColor, percentDone);
        }
    }

    [Serializable]
    public struct GradientFadeinAnimationHidden : IAnimation
    {
        [HideInInspector] public UIBlock2D Target;
        public Color TargetGradient; // This should be the original gradient color
        private Color _startGradient;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                _startGradient = Target.Gradient.Color; // expected to be transparent
                _startGradient.a = 0f;
            }
            Target.Gradient.Color = Color.Lerp(_startGradient, TargetGradient, percentDone);
        }
    }

    /// <summary>
    /// Used to animate the scale of a Transform to a specified target scale.
    /// </summary>
    [Serializable]
    public struct ScaleAnimation : IAnimation
    {
        public Transform Target;
        public Vector3 TargetScale;
        private Vector3 _startScale;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
                _startScale = Target.localScale;

            Target.localScale = Vector3.Lerp(_startScale, TargetScale, percentDone);
        }
    }

    /// <summary>
    /// The hidden version of ScaleAnimation for cases when you don't want the field visible in the inspector.
    /// </summary>
    [Serializable]
    public struct ScaleAnimationHidden : IAnimation
    {
        [HideInInspector] public Transform Target;
        [HideInInspector] public Vector3 TargetScale;
        private Vector3 _startScale;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
                _startScale = Target.localScale;

            Target.localScale = Vector3.Lerp(_startScale, TargetScale, percentDone);
        }
    }

    /// <summary>
    /// Animates the scale to fade out the object by scaling it down to zero.
    /// </summary>
    [Serializable]
    public struct ScaleFadeoutAnimation : IAnimation
    {
        public Transform Target;
        private Vector3 _startScale;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
                _startScale = Target.localScale;

            // Fade out by scaling to zero
            Target.localScale = Vector3.Lerp(_startScale, Vector3.zero, percentDone);
        }
    }

    /// <summary>
    /// Hidden version of the fadeout scale animation.
    /// </summary>
    [Serializable]
    public struct ScaleFadeoutAnimationHidden : IAnimation
    {
        [HideInInspector] public Transform Target;
        private Vector3 _startScale;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
                _startScale = Target.localScale;

            Target.localScale = Vector3.Lerp(_startScale, Vector3.zero, percentDone);
        }
    }

    /// <summary>
    /// Hidden version to animate a fade in effect for scale, scaling from a starting value (often zero) to the target scale.
    /// </summary>
    [Serializable]
    public struct ScaleFadeinAnimationHidden : IAnimation
    {
        [HideInInspector] public Transform Target;
        public Vector3 TargetScale; // The full/original scale that you want to reach.
        private Vector3 _startScale;

        public void Update(float percentDone)
        {
            if (percentDone == 0)
            {
                // Capture the current scale as the starting point.
                _startScale = Target.localScale;
                // Optionally, if you expect a true fade-in effect, you might force _startScale to zero:
                // _startScale = Vector3.zero;
            }
            Target.localScale = Vector3.Lerp(_startScale, TargetScale, percentDone);
        }
    }

}
