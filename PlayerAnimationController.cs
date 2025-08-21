using UnityEngine;
using Animancer;
using UnityEvent = UnityEngine.Events.UnityEvent;

[DefaultExecutionOrder(2)]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private enum STATE { Locomotion, RunStop, Landing, Jump, Death }

    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private NamedAnimancerComponent _namedComponents;

    [Header("Anim Names")]
    private string _keyIdle = "Kairos_Idle";
    private string _keyWalk = "Kairos_Walk";
    private string _keyRun = "Kairos_Running";
    private string _keyRunStop = "Kairos_RunStop";
    private string _keyLanding = "Kairos_Landing";
    private string _keyJumpToPortal = "Kairos_JumpToPortal";
    private string _keyDeath = "Kairos_Death";

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
    [SerializeField] private UnityEvent _onPortalJumpToToxic; // new
    [SerializeField] private UnityEvent _onPortalJump;        // new

    // --- Runtime state ---
    private STATE _state = STATE.Locomotion;

    private float _previousSpeed;
    private bool _isDead;

    private bool _hasPlayedLandingAnimation;
    private bool _landingEffectPlayed;

    // active Animancer states
    private AnimancerState _runStopState;
    private AnimancerState _landingState;
    private AnimancerState _jumpState;
    private AnimancerState _deathState;

    // --------------------------- Unity lifecycle ---------------------------
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

    // --------------------------- State ticks ---------------------------
    private void TickLocomotion()
    {
        _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        float speed = _playerMovement != null ? _playerMovement.CurrentSpeed : 0f;

        // RunStop trigger when dropping under walk threshold
        if (_keyRunStop.Length != 0 && speed < _walkThreshold && _previousSpeed >= _walkThreshold)
        {
            StartRunStop();
            return;
        }

        // Locomotion blend
        if (_keyRun.Length != 0 && speed >= _runThreshold)
        {
            AnimancerState state = _namedComponents.TryPlay(_keyRun, _fadeDuration);
            state.Speed = Mathf.Clamp(speed / _playerMovementSpeedReference, 0.1f, 2f);
        }
        else if (_keyWalk.Length != 0 && speed >= _walkThreshold)
        {
            AnimancerState state = _namedComponents.TryPlay(_keyWalk, _fadeDuration);
            state.Speed = Mathf.Clamp(speed / (_playerMovementSpeedReference * 0.5f), 0.1f, 2f);
        }
        else if (_keyIdle.Length != 0)
        {
            AnimancerState state = _namedComponents.TryPlay(_keyIdle, _fadeDuration);
            state.Speed = 1f;
        }
    }

    private void TickRunStop()
    {
        if (_runStopState == null || !_runStopState.IsPlaying)
        {
            _state = STATE.Locomotion;
            return;
        }

        float speed = _playerMovement != null ? _playerMovement.CurrentSpeed : 0f;
        if (speed >= _walkThreshold)
        {
            _runStopState.Stop();
            _state = STATE.Locomotion;
        }
    }

    private void TickLanding()
    {
        if (_landingState == null || !_landingState.IsPlaying)
        {
            _landingState = null;
            _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;
            _state = STATE.Locomotion;
        }
    }

    private void TickJump()
    {
        if (_jumpState == null || !_jumpState.IsPlaying)
        {
            _jumpState = null;
            _animancer.Animator.applyRootMotion = false;
            if (_playerMovement != null) _playerMovement._isPerformingAction = false;
            _state = STATE.Locomotion;
        }
    }

    // --------------------------- Starters ---------------------------
    private void StartRunStop()
    {
        _state = STATE.RunStop;
        _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;

        _runStopState = _namedComponents.TryPlay(_keyRunStop, _fadeDuration);
        _runStopState.Speed = 1f;
        if (_runStopState.Events(this, out AnimancerEvent.Sequence ev)) ev.OnEnd = OnRunStopEnd; // cached method, no alloc
    }

    public void StartLanding()
    {
        _state = STATE.Landing;
        _landingEffectPlayed = false;
        if (_landingEffectObject != null) _landingEffectObject.SetActive(false);

        if (_playerMovement != null) _playerMovement._isPerformingAction = true;
        _animancer.Animator.applyRootMotion = true;

        _landingState = _namedComponents.TryPlay(_keyLanding, _fadeDuration);
        if (_landingState.Events(this, out AnimancerEvent.Sequence ev)) ev.OnEnd = OnLandingEnd;
    }

    public void StartJump()
    {
        _state = STATE.Jump;
        if (_playerMovement != null) _playerMovement._isPerformingAction = true;
        _animancer.Animator.applyRootMotion = true;

        // fire portal jump event when starting jump in Lobby (matches old behavior)
        if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby)
            _onPortalJump?.Invoke();

        _jumpState = _namedComponents.TryPlay(_keyJumpToPortal, _fadeDuration);
        if (_jumpState.Events(this, out AnimancerEvent.Sequence ev)) ev.OnEnd = OnJumpEnd;
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

        _animancer.Stop();
        _animancer.Animator.applyRootMotion = false; // set true if your death clip has RM you want

        _state = STATE.Death;
        _deathState = _namedComponents.TryPlay(_keyDeath, _fadeDuration);
        _deathState.Speed = 1f;
        if (_deathState.Events(this, out AnimancerEvent.Sequence ev)) ev.OnEnd = OnDeathEnd;
    }

    // --------------------------- External calls ---------------------------
    private bool OnPlayerDeathListener() { StartDeath(); return true; }

    private bool OnSceneEnterListener(SCENE_NAME scene)
    {
        if (scene == SCENE_NAME.Lobby)
        {
            ResetAfterDeath();
            _hasPlayedLandingAnimation = false; // ensure landing can play next ToxicLevel
        }
        else if (scene == SCENE_NAME.ToxicLevel)
        {
            _onPortalJumpToToxic?.Invoke();
        }
        return true;
    }

    // --------------------------- Event callbacks (no lambdas) ---------------------------
    private void OnRunStopEnd() { _state = STATE.Locomotion; }

    private void OnLandingEnd()
    {
        _landingState = null;
        _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;
        _state = STATE.Locomotion;
    }

    private void OnJumpEnd()
    {
        _jumpState = null;
        _animancer.Animator.applyRootMotion = false;
        if (_playerMovement != null) _playerMovement._isPerformingAction = false;
        _state = STATE.Locomotion;
    }

    private void OnDeathEnd()
    {
        _deathState.Time = _deathState.Length; // freeze last frame
        _deathState.Speed = 0f;
    }

    // --------------------------- Public helpers ---------------------------
    private void PlayDeathAnimation() { if (!_isDead) StartDeath(); }

    private void PlayLandingEffects() { if (_landingEffectObject != null) _landingEffectObject.SetActive(true); }

    private void ResetAfterDeath()
    {
        _deathState?.Stop();
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
        _animancer.Animator.applyRootMotion = false;
        _animancer.Animator.Rebind();
        _animancer.Animator.Update(0f);

        if (_keyIdle.Length != 0)
        {
            AnimancerState s = _namedComponents.TryPlay(_keyIdle, _fadeDuration);
            s.Speed = 1f;
        }

        _onPlayerRespawn?.Invoke();
    }
}
