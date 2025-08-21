using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-999)]
public class ManagerGame : MonoBehaviour
{
    public static ManagerGame Instance { get; private set; }
    private Player _player;

    [Header("Round Timer Settings")]
    [SerializeField]
    [Tooltip("Target round duration in seconds")]
    private float _roundLength = 60 * 30f;

    [SerializeField]
    [Tooltip("Name of the scene where a round should start")]
    private string _roundSceneName = "ToxicLevel";

    private RoundTimer _roundTimer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("There should only be one ManagerGame in the scene.");
            Destroy(this);
        }

        _roundTimer = new RoundTimer(_roundLength);
        //_roundTimer.OnTimerUpdated += time => OnRoundTimerUpdated?.Invoke(time);
        //_roundTimer.OnTimerUpdated += time => Events.OnRoundTimerUpdated.Invoke(time);

        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    public void SetPlayer(Player player)
    {
        _player = player;
        Events.OnPlayerSet.Invoke(_player);
    }

    public Player GetPlayer()
    {
        return _player;
    }

    private void Update()
    {
        if (ManagerPause.Instance != null && ManagerPause.IsPaused()) return;

        _roundTimer.Update(Time.deltaTime);
        Events.OnRoundTimerUpdated.Invoke(_roundTimer.CurrentTime);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(_roundSceneName, StringComparison.OrdinalIgnoreCase))
        {
            _roundTimer.Restart();
        }
    }

    /// <summary>
    /// Expose the round timer instance for UI and other listeners.
    /// </summary>
    public RoundTimer GetRoundTimer() => _roundTimer;

    public float GetCurrentRoundTimeSeconds() => _roundTimer.CurrentTime;
    public float GetCurrentRoundTimeMinutes()
    {
        if (_roundTimer.CurrentTime <= 0) return 0;

        return _roundTimer.CurrentTime / 60f;
    }

    public void SafeDestroyMob(GameObject gameObject)
    {
        if (gameObject.GetComponent<DeathController>() != null) return; 

        gameObject.SetActive(false);

        ActionScheduler.RunAfterDelay(1f, () =>
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        });
 
    }



    // public void AssignPlayerTranform(ref Transform assignTransform)
    // {
    //     ActionScheduler.RunWhenTrue(() => Player.Instance != null, () =>
    //     {
    //         assignTransform = Player.Instance.transform;
    //     });
    // }



}