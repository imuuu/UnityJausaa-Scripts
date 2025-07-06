using Nova.TMP;
using UnityEngine;

namespace Game.UI
{
    namespace Game.UI
    {
        public class UI_TimerRound : MonoBehaviour
        {
            [SerializeField]
            private TextMeshProTextBlock _timerText;
            private float _lastSeconds = 0f;

            private void OnEnable()
            {
                Events.OnRoundTimerUpdated.AddListener(UpdateTimerDisplay);
            }

            private void OnDisable()
            {
                Events.OnRoundTimerUpdated.RemoveListener(UpdateTimerDisplay);
            }

            private bool UpdateTimerDisplay(float elapsedSeconds)
            {
                int totalSeconds = Mathf.FloorToInt(elapsedSeconds);

                if (totalSeconds == _lastSeconds) return true;

                _lastSeconds = totalSeconds;

                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                return true;
            }
        }
    }
    // public class UI_TimerRound : MonoBehaviour
    // {
    //     [SerializeField] private TextMeshProTextBlock _timerText;

    //     [SerializeField] private float _targetTime = 60 * 30f;
    //     [SerializeField, ReadOnly] private bool _isRunning = false;
    //     [SerializeField, ReadOnly] private bool _isPaused = false;
    //     private float _currentTime = 0f;

    //     private void Start()
    //     {
    //         ResetTimer();
    //         StartTimer();
    //     }

    //     private void Update()
    //     {
    //         if(ManagerPause.IsPaused()) return;

    //         if (_isRunning && !_isPaused)
    //         {
    //             _currentTime += Time.deltaTime;

    //             if (_currentTime >= _targetTime)
    //             {
    //                 _currentTime = _targetTime;
    //                 _isRunning = false;
    //             }
    //             UpdateTimerDisplay();
    //         }
    //     }

    //     public void StartTimer()
    //     {
    //         _isRunning = true;
    //         _isPaused = false;
    //     }

    //     public void PauseTimer()
    //     {
    //         _isPaused = true;
    //     }

    //     public void ResumeTimer()
    //     {
    //         _isPaused = false;
    //     }

    //     public void ResetTimer()
    //     {
    //         _currentTime = 0f;
    //         _isRunning = false;
    //         _isPaused = false;
    //         UpdateTimerDisplay();
    //     }

    //     public void RestartTimer()
    //     {
    //         ResetTimer();
    //         StartTimer();
    //     }

    //     public void SetTargetTime(float newTargetTime)
    //     {
    //         _targetTime = newTargetTime;
    //     }

    //     private void UpdateTimerDisplay()
    //     {
    //         int totalSeconds = Mathf.FloorToInt(_currentTime);
    //         int minutes = totalSeconds / 60;
    //         int seconds = totalSeconds % 60;
    //         _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    //     }
    // }
}

