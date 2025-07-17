using UnityEngine;
using Nova;
using UI.Animations;
using Sirenix.OdinInspector;

public class UI_FloatingDamageNumber : MonoBehaviour
{
    [SerializeField] private TextBlock _textBlock;
    
    [Title("Animation Settings")]
    [SerializeField] private float _duration = 1f;
    [SerializeField] private VerticalTransformAnimation _verticalMovementAnimation;
    [SerializeField] private BodyColorAnimation _textBodyColorAnimation;
    private AnimationHandle _bodyColorAnimationHandle;
    private AnimationHandle _verticalAnimationHandle;

    private Color _bodyColor;
    private void Awake()
    {
        _bodyColor = _textBlock.Color;
    }

    private void OnDisable() 
    {
        _bodyColorAnimationHandle.Cancel();
        _verticalAnimationHandle.Cancel();
    }
    
    public void ShowDamage(float damage = 33)
    {
        _textBlock.Text = RoundToNearest(damage, 1).ToString();

        _textBlock.Color = _bodyColor;

        _bodyColorAnimationHandle.Cancel();
        _bodyColorAnimationHandle = _textBodyColorAnimation.Run(_duration);

        _verticalAnimationHandle.Cancel();
        _verticalAnimationHandle = _verticalMovementAnimation.Run(_duration);
    }

    private float RoundToNearest(float value, float nearest)
    {
        return Mathf.Round(value / nearest) * nearest;
    }

    public float GetDuration()
    {
        return _duration;
    }

}
