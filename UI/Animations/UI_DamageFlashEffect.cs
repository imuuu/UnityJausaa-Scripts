using UnityEngine;
using Nova;

public class UI_DamageFlashEffect : MonoBehaviour
{
    [SerializeField] private UIBlock2D _damageFlashBlock;
    [SerializeField] private float _maxOpacity = 0.5f;
    [SerializeField] private float _flashDuration = 0.2f;
    private float _timer = 0f;
    private bool _isFlashing = false;
    private Color _baseColor;

    private void Awake()
    {
        _baseColor = _damageFlashBlock.Color;
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        Events.OnPlayerDamageTaken.AddListener(OnPlayerHitReceived);
    }

    private void OnDisable()
    {
        Events.OnPlayerDamageTaken.RemoveListener(OnPlayerHitReceived);
    }

    private bool OnPlayerHitReceived(float damage)
    {        
        StartFlash();
        return true;
    }

    private void StartFlash()
    {
        _isFlashing = true;
        _timer = 0f;
    }

    private void Update()
    {
        if (!_isFlashing)
            return;

        _timer += Time.unscaledDeltaTime;
        float halfDuration = _flashDuration / 2f;

        if (_timer <= halfDuration)
        {
            float alpha = Mathf.Lerp(0f, _maxOpacity, _timer / halfDuration);
            SetAlpha(alpha);
        }
        else if (_timer <= _flashDuration)
        {
            float alpha = Mathf.Lerp(_maxOpacity, 0f, (_timer - halfDuration) / halfDuration);
            SetAlpha(alpha);
        }
        else
        {
            SetAlpha(0f);
            _isFlashing = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        _damageFlashBlock.Color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
    }
}
