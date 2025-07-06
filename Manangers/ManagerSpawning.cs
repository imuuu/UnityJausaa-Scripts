using System.Collections.Generic;
using Game.PoolSystem;
using Sirenix.OdinInspector;
using UnityEngine;


public class ManagerSpawning : MonoBehaviour
{   
    public static ManagerSpawning Instance { get; private set; }
    [Header("Settings")]
    [SerializeField] private bool _enableSpawn = true;
    [Tooltip("Distance from the player where enemies will spawn.")]
    [SerializeField] private float _spawnRadius = 10f;

    [SerializeField] private LayerMask _unallowedSpawnLayers;

    [Header("Wave Configs")]
    [Tooltip("List of waves to cycle through. The manager will handle them in order.")]
    [SerializeField] private List<SpawnWave> _waves = new ();

    [Title("Toxic Pool Waves")]
    [Tooltip("")]
    [SerializeField] private List<SpawnWave> _toxicPoolWaves = new();

    private int _currentWaveIndex = 0;
    private float _waveTimer = 0f;
    private int _currentActiveSpawns = 0;
    private Transform _player;

    private int _spawnedCount = 0; // Count of enemies spawned in the current wave

    private SCENE_NAME _sceneName = SCENE_NAME.ToxicLevel;
    private bool _poolsCreated = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        Events.OnPlayerSet.AddListenerOnce(OnPlayerSet);
        Events.OnPlayableSceneChange.AddListener(OnSceneChange);
    }

    private bool OnSceneChange(SCENE_NAME sceneName)
    {
        Debug.Log("Scene changed to: " + sceneName);
        if (sceneName == SCENE_NAME.ToxicLevel)
        {
            RestartSpawning();
            _enableSpawn = true;
            _sceneName = sceneName;
            return true;
        }
        else
        {
            _enableSpawn = false;
            return true;
        }
    }

    private void Start()
    {
        ActionScheduler.RunAfterDelay(0.1f, () =>
        {
            CreatePools();
        });
        
    }
    
    public LayerMask GetUnallowedSpawnLayers()
    {
        return _unallowedSpawnLayers;
    }

    private PoolOptions GetPoolOptions()
    {
        PoolOptions poolOptions = new PoolOptions()
        {
            Min = 10,
            Max = 0,
            PoolType = POOL_TYPE.DYNAMIC,
            ReturnType = POOL_RETURN_TYPE.MANUAL,
            EventTriggerType = POOL_EVENT_TRIGGER_TYPE.NONE,
            IsLifeTimeDeathEffect = false,
            LifeTimeDuration = 0f
        };

        return poolOptions;
    }

    private void CreatePools()
    {
        foreach (SpawnWave wave in _waves)
        {
            foreach (SpawnWave.WeightedPrefab weightedPrefab in wave.GetPrefabs())
            {
                if (weightedPrefab == null) continue;
                
                if(ManagerPrefabPooler.Instance.HasPoolCreated(weightedPrefab.Prefab)) continue;

                ManagerPrefabPooler.Instance.CreatePrefabPool(weightedPrefab.Prefab, GetPoolOptions());
            }
        }

        foreach (SpawnWave wave in _toxicPoolWaves)
        {
            foreach (SpawnWave.WeightedPrefab weightedPrefab in wave.GetPrefabs())
            {
                if (weightedPrefab == null) continue;
                
                if(ManagerPrefabPooler.Instance.HasPoolCreated(weightedPrefab.Prefab)) continue;

                ManagerPrefabPooler.Instance.CreatePrefabPool(weightedPrefab.Prefab, GetPoolOptions());
            }
        }
        _poolsCreated = true;
    }

    private bool OnPlayerSet(Player player)
    {
        this._player = player.transform;
        return true;
    }
    
    private void RestartSpawning()
    {
        _currentWaveIndex = 0;
        _waveTimer = 0f;
        _currentActiveSpawns = 0;
        _spawnedCount = 0;
        _enableSpawn = true;

        Debug.Log("Restarting spawning.");
    }

    public void Update()
    {
        if (!_enableSpawn) return;
        if (_player == null) return;
        if (_currentWaveIndex >= _waves.Count) return;

        if (!_poolsCreated) return;

        if (ManagerPause.IsPaused()) return;

        _waveTimer += Time.deltaTime;
        SpawnWave currentWave = _waves[_currentWaveIndex];

        if (_waveTimer >= currentWave.WaveDuration)
        {
            _currentWaveIndex++;
            if (_currentWaveIndex < _waves.Count)
            {
                StartWave(_currentWaveIndex);
            }
            else
            {
                Debug.Log("All waves completed!");
            }
            return;
        }

        float normalizedTime = _waveTimer / currentWave.WaveDuration;

        // Evaluate the curve to get a cumulative spawn fraction.
        // IMPORTANT: For a linear spawn, set your curve keys to (0, 0) and (1, 1)
        float spawnFraction = currentWave.SpawnFrequency.Evaluate(normalizedTime);

        float targetSpawnCount = spawnFraction * currentWave.TotalSpawns;

        while (_spawnedCount < targetSpawnCount && _spawnedCount < currentWave.TotalSpawns)
        {
            // Optional: Limit concurrent active spawns if desired.
            if (_currentActiveSpawns < currentWave.TotalSpawns)
            {
                SpawnEnemy(currentWave);
                _spawnedCount++;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Initializes a wave's parameters.
    /// </summary>
    private void StartWave(int waveIndex)
    {
        _waveTimer = 0f;
        _currentActiveSpawns = 0;
        _spawnedCount = 0;

        Debug.Log("Starting wave: " + _waves[waveIndex].WaveName);
    }

    /// <summary>
    /// Spawns a single enemy from the wave's prefabs.
    /// </summary>
    private void SpawnEnemy(SpawnWave wave)
    {
        SpawnWave.WeightedPrefab enemyPrefab = wave.GetRandomPrefab();

        if (enemyPrefab == null)
            return;
        bool validSpawn = false;

        Vector3 spawnPosition = GenerateSpawnPosition(enemyPrefab);


        int attempts = 100;
        while (validSpawn == false)
        {
            validSpawn = IsValidSpawnPosition(spawnPosition, enemyPrefab.Prefab);

            if (!validSpawn)
            {
                spawnPosition = GenerateSpawnPosition(enemyPrefab);
                // MarkHelper.DrawSphereTimed(spawnPosition, 2, 5, Color.red);
                // Debug.Log("Invalid spawn position, trying again.");
            }

            attempts--;
            if (attempts <= 0)
            {
                Debug.LogWarning("Failed to find a valid spawn position after 100 attempts.");
                return;
            }

        }
        GameObject spawned = ManagerPrefabPooler.Instance.GetFromPool(enemyPrefab.Prefab);
        spawned.transform.position = spawnPosition;
        //GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        _currentActiveSpawns++;
    }

    public GameObject GetSpawnedEnemyToxicPools()
    {
        if (_toxicPoolWaves.Count == 0)
        {
            Debug.LogWarning("No toxic pool waves available.");
            return null;
        }

        SpawnWave wave = _toxicPoolWaves[Random.Range(0, _toxicPoolWaves.Count)];
        SpawnWave.WeightedPrefab enemyPrefab = wave.GetRandomPrefab();

        if (enemyPrefab == null)
        {
            Debug.LogWarning("No valid prefab found in toxic pool wave.");
            return null;
        }

        GameObject spawned = ManagerPrefabPooler.Instance.GetFromPool(enemyPrefab.Prefab);

        return spawned;
    }

    public List<SpawnWave> GetToxicPoolWaves()
    {
        return _toxicPoolWaves;
    }

    private Vector3 GenerateSpawnPosition(SpawnWave.WeightedPrefab enemyPrefab)
    {
        Vector3 spawnPosition = Vector3.zero;
        if (enemyPrefab.UseSpawnPattern)
        {
            spawnPosition = enemyPrefab.SpawnPattern.GetSpawnPositions()[0];
            return spawnPosition;
        }
        Vector2 randomCircle = Random.insideUnitCircle.normalized * _spawnRadius;
        spawnPosition = new Vector3(
            _player.position.x + randomCircle.x,
            _player.position.y,
            _player.position.z + randomCircle.y);

        return spawnPosition;
    }

    private bool IsValidSpawnPosition(Vector3 position, GameObject prefab)
    {
        EntityStatistics stats = prefab.GetComponent<EntityStatistics>();
        if (stats == null)
            return true;

        float radius = stats.Width * 0.6f;
        return !Physics.CheckSphere(position, radius, _unallowedSpawnLayers);
    }

    private List<string> GetAllActiveScenes()
    {
        List<string> activeScenes = new ();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            activeScenes.Add(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name);
        }
        return activeScenes;
    }

    private bool IsGameSceneLoaded()
    {
        return GetAllActiveScenes().Contains(_sceneName.ToString());
    }

}
