using Nova;
using UnityEngine;
using UnityEngine.UI;
using UI.Animations;
using Sirenix.OdinInspector;
using TMPro;

public class UI_WorldIndicator : MonoBehaviour
{
    [SerializeField] private UIBlock2D _uiBlock;

    [SerializeField] private UIBlock2D _icon;
    [SerializeField] private TextBlock _text;

    [Title("Animations")]
    [SerializeField] private bool _enableVerticalMoveAnimation = true;

    [SerializeField]
    [ShowIf(nameof(_enableVerticalMoveAnimation))]
    private VerticalUIblockAnimation _verticalMoveAnimation;

    [SerializeField] private bool _enableColorAnimation = false;
    [SerializeField]
    [ShowIf(nameof(_enableColorAnimation))]
    private BodyColorAnimation _colorAnim;

    [SerializeField] private bool _enableScaleAnimation = false;
    [SerializeField]
    [ShowIf(nameof(_enableScaleAnimation))]
    private ScaleAnimation _scaleAnim;

    private AnimationHandle _moveHandle;
    private AnimationHandle _colorHandle;
    private AnimationHandle _scaleHandle;

    private float _duration;

    private void OnDisable()
    {
        _moveHandle.Cancel();
        _colorHandle.Cancel();
        _scaleHandle.Cancel();
    }

    /// <summary>
    /// Called by the manager immediately after pooling.
    /// </summary>
    public void Initialize(
        float amount,
        Sprite iconSprite,
        float duration)
    {
        _duration = duration;

        if (_icon != null)
        {
            _icon.SetImage(iconSprite);
        }

        if (_text != null)
        {
            _text.Text = Mathf.RoundToInt(amount).ToString();
        }

        if (_enableVerticalMoveAnimation)
        {
            _moveHandle.Cancel();
            _moveHandle = _verticalMoveAnimation.Run(_duration);
        }

        if (_enableColorAnimation)
        {
            _colorHandle.Cancel();
            _colorHandle = _colorAnim.Run(_duration);
        }

        if (_enableScaleAnimation)
        {
            _scaleHandle.Cancel();
            _scaleHandle = _scaleAnim.Run(_duration);
        }

    }

    public void SetTextColor(Color color)
    {
        if (_text != null)
        {
            _text.Color = color;
        }
    }

    public void SetColorGradient(VertexGradient gradient)
    {
        if (_text != null)
        {
            _text.TMP.colorGradient = gradient;
        }
    }

    public void SetText(string text)
    {
        if (_text != null)
        {
            _text.Text = text;
        }
    }
}
