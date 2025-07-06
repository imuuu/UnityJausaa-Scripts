using Nova;
using Sirenix.OdinInspector;
using UI.Animations;
using UnityEngine;

public class UI_ToggleColor : MonoBehaviour
{
    [SerializeField] private UIBlock2D _targetBlock;
    [SerializeField] private float _duration;
    [SerializeField] private BodyColorAnimation _enableBodyAnimation;

    [SerializeField] private bool _disableColorIsCurrentColor = true;
    [SerializeField, HideIf(nameof(_disableColorIsCurrentColor))] private BodyColorAnimation _disableBodyAnimation;
    private AnimationHandle _bodyAnimationHandle;
    public bool IsToggled = false;
    private void Awake()
    {
        CheckTargets();
    }

    private void OnDisable()
    {
        _bodyAnimationHandle.Cancel();
    }

    [Button("Toggle Color Test")]
    public void ToggleColor(bool isOn)
    {
        if (isOn)
        {
            ActivateAnimations();
        }
        else
        {
            DeactivateAnimations();
        }
    }

    public void ToggleColorInsta(bool isOn)
    {
        IsToggled = isOn;

        if (isOn)
        {
            _targetBlock.Color = _enableBodyAnimation.TargetColor;
        }
        else
        {
            _targetBlock.Color = _disableBodyAnimation.TargetColor;
        }
    }

    private void DeactivateAnimations()
    {
        _bodyAnimationHandle.Cancel();

        CheckTargets();

        _bodyAnimationHandle = _disableBodyAnimation.Run(_duration);
    }

    private void ActivateAnimations()
    {
        _bodyAnimationHandle.Cancel();

        CheckTargets();

        _bodyAnimationHandle = _enableBodyAnimation.Run(_duration);
    }

    private void CheckTargets()
    {
        if (_enableBodyAnimation.Target == null)
        {
            _enableBodyAnimation.Target = _targetBlock;
        }

        if (_disableBodyAnimation.Target == null)
        {
            _disableBodyAnimation.Target = _targetBlock;

            if (_disableColorIsCurrentColor) _disableBodyAnimation.TargetColor = _disableBodyAnimation.Target.Color;
        }
    }
}