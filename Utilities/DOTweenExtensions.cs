using System;
using DG.Tweening;
using UnityEngine;

public static class DOTweenExtensions
{
    /// <summary>
    /// Rotates the transform about a pivot point from its current rotation to the specified final rotation.
    /// The transform’s position is updated accordingly so that it rotates about the pivot.
    /// </summary>
    /// <param name="target">The transform to rotate.</param>
    /// <param name="pivot">The pivot point (in world space) around which to rotate.</param>
    /// <param name="finalRotation">The target world rotation for the transform.</param>
    /// <param name="duration">Duration of the tween in seconds.</param>
    /// <param name="ease">Optional ease type (default is Ease.OutQuad).</param>
    /// <param name="onComplete">Optional callback when the tween completes.</param>
    /// <returns>The DOTween Tween object.</returns>
    public static Tween DORotateAroundPivot(this Transform target, Vector3 pivot, Quaternion finalRotation, float duration, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        Quaternion startRotation = target.rotation;
        Vector3 offset = target.position - pivot;

        float tVal = 0f;
        Tween tween = DOTween.To(() => tVal, x => tVal = x, 1f, duration)
            .SetEase(ease)
            .OnUpdate(() =>
            {
                Quaternion currentRotation = Quaternion.Slerp(startRotation, finalRotation, tVal);
                target.rotation = currentRotation;

                Quaternion deltaRotation = currentRotation * Quaternion.Inverse(startRotation);
                target.position = pivot + deltaRotation * offset;
            })
            .OnComplete(() =>
            {
                target.rotation = finalRotation;
                Quaternion finalDelta = finalRotation * Quaternion.Inverse(startRotation);
                target.position = pivot + finalDelta * offset;
                onComplete?.Invoke();
            });

        return tween;
    }
}
