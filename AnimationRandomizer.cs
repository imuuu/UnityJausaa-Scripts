using System.Collections.Generic;
using UnityEngine;
using Animancer;

[RequireComponent(typeof(AnimancerComponent))]
public class AnimationRandomizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AnimancerComponent _animancer;

    [Header("Clips")]
    [Tooltip("Assign any number of AnimationClips to be randomly played.")]
    [SerializeField] private List<AnimationClip> _clips = new List<AnimationClip>();

    [Header("Settings")]
    [Tooltip("Fade duration when switching between clips.")]
    [SerializeField] private float _fadeDuration = 0.15f;

    [Tooltip("Random delay before playing the next clip (seconds): x = min, y = max.")]
    [SerializeField] private Vector2 _delayRange = Vector2.zero;

    [Tooltip("Avoid playing the same clip twice in a row.")]
    [SerializeField] private bool _avoidImmediateRepeat = true;

    private AnimancerState _currentState;
    private AnimationClip _lastClip;

    private void Awake()
    {
        // Automatically get the AnimancerComponent if not assigned
        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();
    }

    private void OnEnable()
    {
        if (_clips.Count > 0)
            PlayRandomNow();
    }

    private void OnDisable()
    {
        // Cancel any pending delayed calls
        CancelInvoke();

        // Detach any OnEnd event to prevent dangling references
        if (_currentState != null && _currentState.Events(this, out var events))
            events.OnEnd = null;
    }

    /// <summary>
    /// Plays a random clip immediately.
    /// </summary>
    private void PlayRandomNow()
    {
        if (_clips == null || _clips.Count == 0) return;

        var clip = GetRandomClip();
        _lastClip = clip;

        _currentState = _animancer.Play(clip, _fadeDuration);
        _currentState.NormalizedTime = 0f;

        // Attach OnEnd callback using Animancer's proper event system
        if (_currentState.Events(this, out var events))
        {
            events.OnEnd = () =>
            {
                // Wait for a random delay before playing the next clip
                float min = Mathf.Min(_delayRange.x, _delayRange.y);
                float max = Mathf.Max(_delayRange.x, _delayRange.y);
                float delay = (max > 0f) ? Random.Range(min, max) : 0f;

                if (delay > 0f)
                    Invoke(nameof(PlayRandomNow), delay);
                else
                    PlayRandomNow();
            };
        }
    }

    /// <summary>
    /// Returns a random clip from the list, optionally avoiding repeating the last one.
    /// </summary>
    private AnimationClip GetRandomClip()
    {
        if (_clips.Count == 1) return _clips[0];

        int index = Random.Range(0, _clips.Count);

        if (_avoidImmediateRepeat && _clips.Count > 1 && _lastClip != null)
        {
            // If we picked the same clip as last time, choose a different one
            if (_clips[index] == _lastClip)
            {
                index = (index + 1) % _clips.Count;
            }
        }

        return _clips[index];
    }

    /// <summary>
    /// Forces the script to immediately switch to a new random clip.
    /// </summary>
    public void Next()
    {
        CancelInvoke();
        PlayRandomNow();
    }
}
