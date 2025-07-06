using System;
using Nova;
using UnityEngine;

namespace UI.Animations
{
    /// <summary>
    /// Various easing options for vertical movement.
    /// These mimic common DOTween easing functions.
    /// </summary>
    public enum MovementType
    {
        Linear,      // No easing; simple linear interpolation.
        Sine,        // Sine ease-out: fast start, then slow down.
        Cosine,      // Cosine ease-in: slow start, then speed up.
        SmoothStep,  // Unity’s SmoothStep for a smooth transition.
        InQuad,      // Quadratic ease-in.
        OutQuad,     // Quadratic ease-out.
        InOutQuad,   // Quadratic ease-in/out.
        InCubic,     // Cubic ease-in.
        OutCubic,    // Cubic ease-out.
        InOutCubic   // Cubic ease-in/out.
    }

    /// <summary>
    /// Animates a Transform's vertical (y-axis) position.
    /// The animation starts from the Transform's current y-position and adds a delta value (DeltaY).
    /// The movement over time can be eased using various functions.
    /// </summary>
    [Serializable]
    public struct VerticalMovementAnimation : IAnimation
    {
        /// <summary>
        /// The Transform to animate.
        /// </summary>
        public Transform Target;

        /// <summary>
        /// The vertical offset (ΔY) to add to the target's starting y-position.
        /// </summary>
        public float DeltaY;

        /// <summary>
        /// The easing function to use for vertical movement.
        /// </summary>
        public MovementType EasingType;

        /// <summary>
        /// Internal storage for the starting y-position.
        /// </summary>
        private float _startY;

        /// <summary>
        /// Updates the vertical position of the target based on the progress.
        /// </summary>
        /// <param name="percentDone">Progress from 0 (start) to 1 (complete).</param>
        public void Update(float percentDone)
        {
            // Capture the starting y-position on the first update.
            if (percentDone == 0f)
            {
                _startY = Target.position.y;
            }

            // Apply the chosen easing function to the progress.
            float easedT = ApplyEasing(percentDone, EasingType);

            // Calculate the new y-position by adding the eased delta.
            float newY = _startY + Mathf.Lerp(0f, DeltaY, easedT);

            // Update the transform's position while preserving x and z values.
            Vector3 pos = Target.position;
            pos.y = newY;
            Target.position = pos;
        }

        /// <summary>
        /// Converts linear progress into eased progress based on the selected easing type.
        /// </summary>
        /// <param name="t">Linear progress (0 to 1).</param>
        /// <param name="movementType">The selected easing type.</param>
        /// <returns>Eased progress value.</returns>
        private float ApplyEasing(float t, MovementType movementType)
        {
            switch (movementType)
            {
                case MovementType.Linear:
                    return t;

                case MovementType.Sine:
                    // Sine ease-out: fast start, then slow down.
                    return Mathf.Sin(t * Mathf.PI * 0.5f);

                case MovementType.Cosine:
                    // Cosine ease-in: slow start, then speed up.
                    return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                case MovementType.SmoothStep:
                    return Mathf.SmoothStep(0f, 1f, t);

                case MovementType.InQuad:
                    return t * t;

                case MovementType.OutQuad:
                    return 1f - (1f - t) * (1f - t);

                case MovementType.InOutQuad:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                case MovementType.InCubic:
                    return t * t * t;

                case MovementType.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);

                case MovementType.InOutCubic:
                    return t < 0.5f
                        ? 4f * t * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

                default:
                    return t;
            }
        }
    }
}
