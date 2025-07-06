using UnityEngine;
using Animancer;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Animation Settings")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private AnimationClip _idleAnimation;
    [SerializeField] private AnimationClip _walkAnimation;
    [SerializeField] private AnimationClip _runAnimation;
    [SerializeField] private AnimationClip _attackAnimation;
    [SerializeField] private AnimationClip _runStopAnimation;
    [SerializeField] private AnimationClip _jumpToPortalAnimation;
    [SerializeField] private AnimationClip _landingAnimation;

    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _playerMovementSpeedReference = 5f;
    [SerializeField] private float _walkThreshold = 0.3f;
    [SerializeField] private float _runThreshold = 2.0f;

    [SerializeField] private GameObject _landingEffectObject;

    private AnimancerState _landingState;
    private bool _landingEffectPlayed = false;

    private float _previousSpeed;
    private bool _isRunStopping = false;
    private AnimancerState _runStopState;
    //private bool _hasPlayedJumpAnimation = false;

    private bool _hasPlayedLandingAnimation = false;

    private void Update()
    {
        if (_landingState != null && _landingState.IsPlaying)
        {
            float time = _landingState.NormalizedTime;

            if (!_landingEffectPlayed && time > 0.43f)
            {
                PlayLandingEffects();
                _landingEffectPlayed = true;
            }
        }
    }
    private void FixedUpdate()
    {
        HandleAnimations();
        _previousSpeed = _playerMovement.CurrentSpeed;
    }

    private void HandleAnimations()
    {
        // If the landing animation is playing, wait until it finishes
        if (_landingState != null)
        {
            if (!_landingState.IsPlaying)
            {
                Debug.Log("Landing animation finished!");
                _landingState = null;
                _animancer.Animator.applyRootMotion = false;
            }
            else
            {
                // Don't continue until landing animation is done
                return;
            }
        }

        // If player is not in the lobby and hasn't played the landing animation yet
        //if (!_playerMovement.isInLobby && !_hasPlayedLandingAnimation)
        if (SceneLoader.GetCurrentScene() == SCENE_NAME.ToxicLevel && !_hasPlayedLandingAnimation)
        {
            _playerMovement._isPerformingAction = true;
            Debug.Log("Playing landing animation");
            _hasPlayedLandingAnimation = true;

            _animancer.Animator.applyRootMotion = true;
            _landingState = _animancer.Play(_landingAnimation);

            if (_landingState.Events(this, out var events))
            {
                events.OnEnd = () =>
                {
                    Debug.Log("Landing animation ended via event");
                    _landingState = null;
                    _animancer.Animator.applyRootMotion = false;
                };
            }

            return;
        }

        //if (_playerMovement._isPerformingAction && !_hasPlayedJumpAnimation && _playerMovement.isInLobby)
        if (_playerMovement._isPerformingAction && SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby)
        {
           /*  _hasPlayedJumpAnimation = true; // Set immediately to prevent re-entering */
            _animancer.Animator.applyRootMotion = true;
            var jumpState = _animancer.Play(_jumpToPortalAnimation);            

            if (jumpState.Events(this, out var events))
            {
                events.OnEnd = () =>
                {
                    // Logic after jump animation (FadeOut -> move to battle)

                    /* _playerMovement._isPerformingAction = false; */
                };
            }
            return;
        }


        // No need for root motion after landing & jumping animations
        _animancer.Animator.applyRootMotion = false;
        _playerMovement._isPerformingAction = false;

        float currentSpeed = _playerMovement.CurrentSpeed;

        if (_isRunStopping && currentSpeed >= _walkThreshold)
        {
            _isRunStopping = false;
            _runStopState?.Stop();
        }

        if (!_isRunStopping && currentSpeed < _walkThreshold && _previousSpeed >= _walkThreshold)
        {
            if (_runStopAnimation != null)
            {
                _isRunStopping = true;
                _runStopState = _animancer.Play(_runStopAnimation, _fadeDuration);
                _runStopState.Speed = 1f;

                if (_runStopState.Events(this, out var events))
                {
                    events.OnEnd = () =>
                    {
                        _isRunStopping = false;
                        _animancer.Play(_idleAnimation, _fadeDuration);
                    };
                }

                return;
            }
        }

        if (_isRunStopping) return;

        if (currentSpeed >= _runThreshold)
        {
            var runState = _animancer.Play(_runAnimation, _fadeDuration);
            runState.Speed = Mathf.Clamp(currentSpeed / _playerMovementSpeedReference, 0.1f, 2f);
        }
        else if (currentSpeed >= _walkThreshold)
        {
            var walkState = _animancer.Play(_walkAnimation, _fadeDuration);
            walkState.Speed = Mathf.Clamp(currentSpeed / (_playerMovementSpeedReference * 0.5f), 0.1f, 2f);
        }
        else
        {
            var idleState = _animancer.Play(_idleAnimation, _fadeDuration);
            idleState.Speed = 1f;
        }
    }

    private void PlayLandingEffects()
    {
        _landingEffectObject.SetActive(true);
    }    
}
