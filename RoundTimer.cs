using System;

public class RoundTimer
{
    private float _currentTime;
    private float _targetTime;
    private bool _isRunning;
    private bool _isPaused;

    /// <summary>
    /// Event invoked every Update tick when the timer has updated. Provides current elapsed time in seconds.
    /// </summary>
    public event Action<float> OnTimerUpdated;

    public float CurrentTime => _currentTime;
    public float TargetTime => _targetTime;
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;

    public RoundTimer(float targetTimeSeconds)
    {
        _targetTime = targetTimeSeconds;
        Reset();
    }

    /// <summary>
    /// Call this each frame, passing in the unscaled deltaTime.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isRunning || _isPaused) return;

        _currentTime += deltaTime;
        if (_currentTime >= _targetTime)
        {
            _currentTime = _targetTime;
            _isRunning = false;
        }

        OnTimerUpdated?.Invoke(_currentTime);
    }

    public void Start() { _isRunning = true; _isPaused = false; }
    public void Pause() { if (_isRunning) _isPaused = true; }
    public void Resume() { if (_isRunning) _isPaused = false; }
    public void Reset()
    {
        _currentTime = 0f;
        _isRunning = false;
        _isPaused = false;
        OnTimerUpdated?.Invoke(_currentTime);
    }
    public void Restart()
    {
        Reset();
        Start();
    }
    public void SetTarget(float seconds)
    {
        _targetTime = seconds;
        if (_currentTime > _targetTime)
            _currentTime = _targetTime;
    }
}