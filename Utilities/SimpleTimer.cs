using UnityEngine;
using System;

namespace Game.Utility
{
    /// <summary>
    /// A versatile timer that supports pausing, resuming, looping, and callback events.
    /// Call UpdateTimer() in your MonoBehaviour.Update() method.
    /// </summary>
    public class SimpleTimer
    {
        /// <summary>
        /// Total duration of the timer in seconds.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// Time that has elapsed since the timer started or was last reset.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Whether the timer should automatically restart after completion.
        /// </summary>
        public bool IsLooping { get; private set; }

        /// <summary>
        /// Indicates if the timer has completed its countdown (only for non-looping timers).
        /// </summary>
        public bool IsCompleted { get; private set; }

        public bool IsRoundCompleted { get; private set; }

        /// <summary>
        /// Indicates if the timer is currently paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Event that fires when the timer reaches its duration.
        /// </summary>
        public event Action OnTimerComplete;

        /// <summary>
        /// Creates a new SimpleTimer instance.
        /// </summary>
        /// <param name="duration">The duration of the timer in seconds.</param>
        /// <param name="looping">If true, the timer resets automatically upon completion.</param>
        public SimpleTimer(float duration, bool looping = true)
        {
            Duration = duration;
            IsLooping = looping;
            Reset();
        }

        /// <summary>
        /// Sets whether the timer should loop upon reaching its duration.
        /// </summary>
        /// <param name="looping">True to enable looping.</param>
        /// <returns>The updated SimpleTimer instance.</returns>
        public SimpleTimer SetLooping(bool looping)
        {
            IsLooping = looping;
            return this;
        }

        /// <summary>
        /// Subscribes a callback to be invoked upon timer completion.
        /// </summary>
        /// <param name="callback">The action to invoke when the timer completes.</param>
        /// <returns>The updated SimpleTimer instance.</returns>
        public SimpleTimer SetOnComplete(Action callback)
        {
            OnTimerComplete += callback;
            return this;
        }

        /// <summary>
        /// Resets the timer to zero and resumes if it was paused.
        /// </summary>
        public void Reset(float elapsedTime = 0f)
        {
            ElapsedTime = Mathf.Clamp(elapsedTime, 0f, Duration);
            IsCompleted = false;
            IsPaused = false;
            IsRoundCompleted = false;
        }

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        public void Pause() => IsPaused = true;

        /// <summary>
        /// Resumes the timer if it was paused.
        /// </summary>
        public void Resume() => IsPaused = false;

        /// <summary>
        /// Updates the timer. Call this every frame (e.g., from MonoBehaviour.Update()).
        /// </summary>
        /// <param name="deltaTime">
        /// Time increment for the timer. If not provided or negative, Time.deltaTime is used.
        /// </param>
        public void UpdateTimer(float deltaTime = -1f)
        {
            if (IsPaused || (IsCompleted && !IsLooping))
                return;
            
            IsRoundCompleted = false;

            deltaTime = deltaTime < 0f ? Time.deltaTime : deltaTime;
            ElapsedTime += deltaTime;

            if (ElapsedTime >= Duration)
            {
                IsRoundCompleted = true;
                OnTimerComplete?.Invoke();

                if (IsLooping)
                {
                    // Handle overshoot by subtracting the duration
                    ElapsedTime -= Duration;
                }
                else
                {
                    IsCompleted = true;
                    // Clamp elapsed time to duration for non-looping timers
                    ElapsedTime = Duration;
                }
            }
        }
    }
}
