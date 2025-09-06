using UnityEngine;
using Animancer;
using UnityEvent = UnityEngine.Events.UnityEvent;

[DefaultExecutionOrder(2)]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private enum STATE { Locomotion, RunStop, Landing, Jump, Death }

    private const int OVERLAY_LAYER = 1; // one-shots (RunStop / Landing / Jump) live here

    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private AnimancerComponent _animancer;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip _clipIdle;
    [SerializeField] private AnimationClip _clipWalk;
    [SerializeField] private AnimationClip _clipRun;
    [SerializeField] private AnimationClip _clipRunStop;
    [SerializeField] private AnimationClip _clipLanding;
    [SerializeField] private AnimationClip _clipJumpToPortal;
    [SerializeField] private AnimationClip _clipDeath;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _playerMovementSpeedReference = 5f;
    [SerializeField, Min(0f)] private float _walkThreshold = 0.3f;
    [SerializeField, Min(0f)] private float _runThreshold = 2.0f;

    [Header("Landing FX")]
    [SerializeField] private GameObject _landingEffectObject;
    [SerializeField, Range(0f, 1f)] private float _landingEffectNormalizedTime = 0.43f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onPlayerDeath;
    [SerializeField] private UnityEvent _onPlayerRespawn;
    [SerializeField] private UnityEvent _onPlayerLanding;
    [SerializeField] private UnityEvent _onPortalJumpToToxic;
    [SerializeField] private UnityEvent _onPortalJump;

    // Runtime state
    private STATE _state = STATE.Locomotion;
    private float _previousSpeed;
    private bool _isDead;
    private bool _hasPlayedLandingAnimation;
    private bool _landingEffectPlayed;

    // Active Animancer states (for convenience checks)
    private AnimancerState _runStopState;
    private AnimancerState _landingState;
    private AnimancerState _jumpState;
    private AnimancerState _deathState;

    // --------------------------- Unity lifecycle ---------------------------
    private void Awake()
    {
        // Ensure overlay layer starts disabled.
        _animancer.Layers[OVERLAY_LAYER].Weight = 0f;
    }

    private void OnEnable()
    {
        Events.OnPlayerDeath.AddListener(OnPlayerDeathListener);
        Events.OnPlayableSceneChangeEnter.AddListener(OnSceneEnterListener);
    }

    private void OnDisable()
    {
        Events.OnPlayerDeath.RemoveListener(OnPlayerDeathListener);
        Events.OnPlayableSceneChangeEnter.RemoveListener(OnSceneEnterListener);
    }

    private void Update()
    {
        if (_isDead) return;

        if (_state == STATE.Landing && _landingState != null && _landingState.IsPlaying)
        {
            float t = _landingState.NormalizedTime;
            if (!_landingEffectPlayed && t >= _landingEffectNormalizedTime)
            {
                if (_landingEffectObject != null) _landingEffectObject.SetActive(true);
                _onPlayerLanding?.Invoke();
                _landingEffectPlayed = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        switch (_state)
        {
            case STATE.Locomotion: TickLocomotion(); break;
            case STATE.RunStop: TickRunStop(); break;
            case STATE.Landing: TickLanding(); break;
            case STATE.Jump: TickJump(); break;
            case STATE.Death: break;
        }

        _previousSpeed = _playerMovement != null ? _playerMovement.CurrentSpeed : 0f;
    }

    // --------------------------- Helpers ---------------------------
    private AnimancerState PlayBase(AnimationClip clip, float fade)
    {
        if (clip == null)
        {
            Debug.LogError("[Animancer] Missing AnimationClip reference.", this);
            return null;
        }
        return _animancer.Play(clip, fade);
    }

    private AnimancerState PlayOverlay(AnimationClip clip, float fade)
    {
        if (clip == null)
        {
            Debug.LogError("[Animancer] Missing Overlay AnimationClip reference.", this);
            return null;
        }

        var layer = _animancer.Layers[OVERLAY_LAYER];
        var s = layer.Play(clip);             // play on overlay layer
        layer.StartFade(1f, fade);            // fade the layer in (recommended vs. fading state)
        return s;
    }

    private void FadeOutOverlay(float fade) =>
        _animancer.Layers[OVERLAY_LAYER].StartFade(0f, fade);

    // --------------------------- State ticks ---------------------------
    private void TickLocomotion()
    {
        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false;

        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        float speed = _playerMovement != null ? _playerMovement.CurrentSpeed : 0f;

        // RunStop trigger when dropping under walk threshold
        if (_clipRunStop != null && speed < _walkThreshold && _previousSpeed >= _walkThreshold)
        {
            StartRunStop();
            return;
        }

        // Locomotion blend on base layer (layer 0)
        if (_clipRun != null && speed >= _runThreshold)
        {
            var state = PlayBase(_clipRun, _fadeDuration);
            if (state != null)
                state.Speed = Mathf.Clamp(speed / _playerMovementSpeedReference, 0.1f, 2f);
        }
        else if (_clipWalk != null && speed >= _walkThreshold)
        {
            var state = PlayBase(_clipWalk, _fadeDuration);
            if (state != null)
                state.Speed = Mathf.Clamp(speed / (_playerMovementSpeedReference * 0.5f), 0.1f, 2f);
        }
        else if (_clipIdle != null)
        {
            var state = PlayBase(_clipIdle, _fadeDuration);
            if (state != null) state.Speed = 1f;
        }
    }

    private void TickRunStop()
    {
        // If overlay state finished naturally, return to locomotion
        if (_runStopState == null || !_runStopState.IsPlaying)
        {
            _state = STATE.Locomotion;
            return;
        }

        // If player started moving again, fade out overlay layer immediately.
        float speed = _playerMovement != null ? _playerMovement.CurrentSpeed : 0f;
        if (speed >= _walkThreshold)
        {
            FadeOutOverlay(_fadeDuration);
            _state = STATE.Locomotion;
        }
    }

    private void TickLanding()
    {
        if (_landingState == null || !_landingState.IsPlaying)
        {
            _landingState = null;
            if (_animancer != null && _animancer.Animator != null)
                _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;

            FadeOutOverlay(_fadeDuration);
            _state = STATE.Locomotion;
        }
    }

    private void TickJump()
    {
        if (_jumpState == null || !_jumpState.IsPlaying)
        {
            _jumpState = null;
            if (_animancer != null && _animancer.Animator != null)
                _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;

            FadeOutOverlay(_fadeDuration);
            _state = STATE.Locomotion;
        }
    }

    // --------------------------- Starters ---------------------------
    private void StartRunStop()
    {
        _state = STATE.RunStop;

        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        _runStopState = PlayOverlay(_clipRunStop, _fadeDuration);
        if (_runStopState != null && _runStopState.Events(this, out var ev))
            ev.OnEnd = OnRunStopEnd;
        else
            _state = STATE.Locomotion;
    }

    public void StartLanding()
    {
        _state = STATE.Landing;
        _landingEffectPlayed = false;
        if (_landingEffectObject != null) _landingEffectObject.SetActive(false);

        if (_playerMovement != null) _playerMovement._isPerformingAction = true;
        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = true;

        _landingState = PlayOverlay(_clipLanding, _fadeDuration);
        if (_landingState != null && _landingState.Events(this, out var ev))
            ev.OnEnd = OnLandingEnd;
        else
        {
            if (_animancer != null && _animancer.Animator != null)
                _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;
            FadeOutOverlay(_fadeDuration);
            _state = STATE.Locomotion;
        }
    }

    public void StartJump()
    {
        _state = STATE.Jump;

        if (_playerMovement != null) _playerMovement._isPerformingAction = true;
        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = true;

        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby)
            _onPortalJump?.Invoke();

        _jumpState = PlayOverlay(_clipJumpToPortal, _fadeDuration);
        if (_jumpState != null && _jumpState.Events(this, out var ev))
            ev.OnEnd = OnJumpEnd;
        else
        {
            if (_animancer != null && _animancer.Animator != null)
                _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;
            FadeOutOverlay(_fadeDuration);
            _state = STATE.Locomotion;
        }
    }

    private void StartDeath()
    {
        _isDead = true;
        _onPlayerDeath?.Invoke();

        if (_playerMovement != null)
        {
            _playerMovement._isPerformingAction = true;
            _playerMovement.enabled = false;
        }

        // Stop everything and play Death on base layer.
        _animancer.Stop();
        FadeOutOverlay(0f);

        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false; // set true if your death clip uses RM

        _state = STATE.Death;
        _deathState = PlayBase(_clipDeath, _fadeDuration);
        if (_deathState != null)
        {
            _deathState.Speed = 1f;
            if (_deathState.Events(this, out var ev))
                ev.OnEnd = OnDeathEnd;
        }
    }

    // --------------------------- External calls ---------------------------
    private bool OnPlayerDeathListener() { StartDeath(); return true; }

    private bool OnSceneEnterListener(SCENE_NAME scene)
    {
        if (scene == SCENE_NAME.Lobby)
        {
            ResetAfterDeath();
            _hasPlayedLandingAnimation = false;
        }
        else if (scene == SCENE_NAME.ToxicLevel)
        {
            _onPortalJumpToToxic?.Invoke();
        }
        return true;
    }

    // --------------------------- Event callbacks ---------------------------
    private void OnRunStopEnd()
    {
        FadeOutOverlay(_fadeDuration);
        _state = STATE.Locomotion;
    }

    private void OnLandingEnd()
    {
        _landingState = null;
        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        FadeOutOverlay(_fadeDuration);
        _state = STATE.Locomotion;
    }

    private void OnJumpEnd()
    {
        _jumpState = null;
        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        FadeOutOverlay(_fadeDuration);
        _state = STATE.Locomotion;
    }

    private void OnDeathEnd()
    {
        if (_deathState != null)
        {
            _deathState.Time = _deathState.Length; // freeze last frame
            _deathState.Speed = 0f;
        }
    }

    // --------------------------- Public helpers ---------------------------
    private void PlayDeathAnimation() { if (!_isDead) StartDeath(); }

    private void PlayLandingEffects()
    {
        if (_landingEffectObject != null) _landingEffectObject.SetActive(true);
    }

    private void ResetAfterDeath()
    {
        _deathState?.StartFade(0f, _fadeDuration);
        _landingState = null;
        _runStopState = null;
        _jumpState = null;

        _isDead = false;
        if (_playerMovement != null)
        {
            _playerMovement._isPerformingAction = false;
            _playerMovement.enabled = true;
        }

        _landingEffectPlayed = false;
        if (_landingEffectObject != null) _landingEffectObject.SetActive(false);

        _previousSpeed = 0f;
        _state = STATE.Locomotion;

        _animancer.Stop();
        FadeOutOverlay(0f);

        if (_animancer != null && _animancer.Animator != null)
            _animancer.Animator.applyRootMotion = false;

        if (_clipIdle != null)
        {
            var s = PlayBase(_clipIdle, _fadeDuration);
            if (s != null) s.Speed = 1f;
        }

        _onPlayerRespawn?.Invoke();
    }
}
