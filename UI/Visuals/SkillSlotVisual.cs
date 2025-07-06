
using Nova;
using UI.Animations;
using UnityEngine;
using Game.SkillSystem;

public class SkillSlotVisual : ItemVisuals
{
    public UIBlock2D Icon;
    public UIBlock2D IconUnassigned;
    public TextBlock TextButtonNumber;

    [Header("Animations")]
    public float Duration = 0.15f;

    public BodyColorAnimation HoverAnimation;
    public BodyColorAnimation UnhoverAnimation;

    public GradientAnimation PressAnimation;
    public GradientAnimation ReleaseAnimation;

    private AnimationHandle _hoverHandle;
    private AnimationHandle _pressHandle;

    public void Bind(SkillDefinition skillDefinition)
    {
        if(skillDefinition == null)
        {
            Unbind();
            return;
        }
        SetActive(true);

        Icon.SetImage(skillDefinition.Icon);

    }

    public void Unbind()
    {
        SetActive(false);
    }

    public void SetKeyNumber(string keyNumber)
    {
        TextButtonNumber.Text = keyNumber;
    }

    private void SetActive(bool active)
    {
        // Icon.gameObject.SetActive(active);
        // IconUnassigned.gameObject.SetActive(!active);
    }

    #region Animations
    public void Hover()
    {
        _hoverHandle.Cancel();
        _hoverHandle = HoverAnimation.Run(Duration);
    }

    public void Unhover()
    {
        _hoverHandle.Cancel();
        _hoverHandle = UnhoverAnimation.Run(Duration);
    }

    public void Press()
    {
        _pressHandle.Cancel();
        _pressHandle = PressAnimation.Run(Duration);
    }

    public void Release()
    {
        _pressHandle.Cancel();
        _pressHandle = ReleaseAnimation.Run(Duration);
    }
    #endregion
}