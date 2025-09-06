using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Collections;
using System.Collections.Generic;
using System;
using Game.Mobs;

[DefaultExecutionOrder(-100)]
public class ManagerMob : MonoBehaviour
{
    public static ManagerMob Instance { get; private set; }
    [SerializeField] private MobLibrary mobLibrary;
    private Transform _player;
    private List<Transform> _enemyList = new();
    private int _numberOfClosest = 50;
    private int _numberOfFarthest = 10;
    private float _updateInterval = 0.1f;
    private Transform[] _closestEnemies;
    private Transform[] _farthestEnemies;

    private List<EnemyRangeRequest> _pendingRequests = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void Start()
    {
        Player.AssignTransformWhenAvailable((playerTransform) =>
        {
            _player = playerTransform;
        });

        StartCoroutine(UpdateClosestEnemiesCoroutine());

        Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
    }


    private bool OnPlayableSceneChange(SCENE_NAME param)
    {
        foreach (var enemy in _enemyList.ToArray())
        {
            if (enemy != null)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        return true;
    }

    private void Update()
    {
        // Poll all pending asynchronous search requests.
        // Process those whose job has been completed.
        for (int i = _pendingRequests.Count - 1; i >= 0; i--)
        {
            EnemyRangeRequest request = _pendingRequests[i];
            if (request.jobHandle.IsCompleted)
            {
                request.jobHandle.Complete();

                // Process job results: filter indices where distance <= request.radius.
                List<int> indicesInRange = new List<int>();
                for (int j = 0; j < request.enemyCount; j++)
                {
                    if (request.distances[j] <= request.radius)
                    {
                        indicesInRange.Add(j);
                    }
                }

                // Sort indices based on the computed distances.
                int[] indicesArray = indicesInRange.ToArray();
                Array.Sort(indicesArray, (i1, i2) => request.distances[i1].CompareTo(request.distances[i2]));

                int resultCount = Mathf.Min(request.maxCount, indicesArray.Length);
                Transform[] result = new Transform[resultCount];
                for (int j = 0; j < resultCount; j++)
                {
                    result[j] = request.enemyTransforms[indicesArray[j]];
                }

                // Dispose of NativeArrays to avoid memory leaks.
                request.enemyPositions.Dispose();
                request.distances.Dispose();

                // Callback with results.
                request.callback(result);

                // Remove processed request.
                _pendingRequests.RemoveAt(i);
            }
        }
    }


    /// <summary>
    /// Coroutine that updates closestEnemies every updateInterval seconds.
    /// </summary>
    private IEnumerator UpdateClosestEnemiesCoroutine()
    {
        while (true)
        {
            if (_player != null && _enemyList.Count > 0)
            {
                // Calculate and store the result.
                _closestEnemies = CalculateClosestEnemies();
            }
            else
            {
                _closestEnemies = new Transform[0];
            }
            yield return new WaitForSeconds(_updateInterval);
        }
    }

    public void DespawnAllNonBossMobs()
    {
        foreach (Transform enemy in _enemyList.ToArray())
        {
            Mob mob = enemy.GetComponent<Mob>();

            if (mob is IBoss) continue;

            enemy.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Returns the current cached closest enemy transforms.
    /// </summary>
    public Transform[] GetClosestEnemies()
    {
        return _closestEnemies;
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!_enemyList.Contains(enemy))
        {
            _enemyList.Add(enemy);
        }
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (_enemyList.Contains(enemy))
        {
            _enemyList.Remove(enemy);
        }
    }

    /// <summary>
    /// Uses Unity's Job System to calculate the closest enemies based on actual vector distances.
    /// </summary>
    private Transform[] CalculateClosestEnemies()
    {
        int enemyCount = _enemyList.Count;
        if (enemyCount == 0 || _player == null)
            return new Transform[0];

        // Create NativeArrays for enemy positions and their computed distances.
        NativeArray<Vector3> enemyPositions = new NativeArray<Vector3>(enemyCount, Allocator.TempJob);
        NativeArray<float> enemyWidths = new NativeArray<float>(enemyCount, Allocator.TempJob);
        NativeArray<float> distances = new NativeArray<float>(enemyCount, Allocator.TempJob);

        // Copy the positions of each enemy into the NativeArray.
        for (int i = 0; i < enemyCount; i++)
        {
            enemyPositions[i] = _enemyList[i].position;
            enemyWidths[i] = _enemyList[i].GetComponent<IStatistics>()?.Width * 0.5f ?? 0f;
        }

        Vector3 playerPos = _player.position;

        // Create and schedule the job that computes the actual distances.
        DistanceJob job = new DistanceJob
        {
            PlayerPosition = playerPos,
            EnemyPositions = enemyPositions,
            EnemyWidths = enemyWidths,
            Distances = distances
        };

        JobHandle handle = job.Schedule(enemyCount, 64);
        handle.Complete();

        // Create an array of indices so that we can sort them based on computed distance.
        int[] indices = new int[enemyCount];
        for (int i = 0; i < enemyCount; i++)
        {
            indices[i] = i;
        }
        System.Array.Sort(indices, (i1, i2) => distances[i1].CompareTo(distances[i2]));


        //TODO
        int resultCount = Mathf.Min(_numberOfClosest, enemyCount);
        Transform[] result = new Transform[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            result[i] = _enemyList[indices[i]];
        }

        // TODO 
        // this might be bad pratice, due to returns closest enemies and farthest enemies at the same time
        int fCount = Mathf.Min(_numberOfFarthest, enemyCount);
        _farthestEnemies = new Transform[fCount];

        for (int i = 0; i < fCount; i++)
            _farthestEnemies[i] = _enemyList[indices[enemyCount - 1 - i]];

        enemyPositions.Dispose();
        enemyWidths.Dispose();
        distances.Dispose();

        return result;
    }

    public bool CanStartNewSearch()
    {
        return _pendingRequests.Count == 0;
    }

    /// <summary>
    /// Asynchronously searches for enemies within a given radius without using coroutines.
    /// Results are delivered via a callback when the job completes.
    /// </summary>
    /// <param name="radius">Search radius.</param>
    /// <param name="maxCount">Maximum number of enemies to return.</param>
    /// <param name="callback">Callback to be invoked with the resulting enemy transforms.</param>
    public void FindEnemiesInRangeAsync(float radius, int maxCount, Action<Transform[]> callback)
    {
        // Take a snapshot of the current enemy list.
        Transform[] enemyTransforms = _enemyList.ToArray();
        int enemyCount = enemyTransforms.Length;

        if (enemyCount == 0 || _player == null)
        {
            callback(new Transform[0]);
            return;
        }

        // Create persistent NativeArrays as they'll be disposed once the job is processed.
        NativeArray<Vector3> enemyPositions = new NativeArray<Vector3>(enemyCount, Allocator.Persistent);
        NativeArray<float> enemyWidths = new NativeArray<float>(enemyCount, Allocator.Persistent);
        NativeArray<float> distances = new NativeArray<float>(enemyCount, Allocator.Persistent);

        for (int i = 0; i < enemyCount; i++)
        {
            enemyPositions[i] = enemyTransforms[i].position;
            enemyWidths[i] = enemyTransforms[i].GetComponent<IStatistics>()?.Width * 0.5f ?? 0f;
        }

        Vector3 playerPos = _player.position;
        DistanceJob job = new DistanceJob
        {
            PlayerPosition = playerPos,
            EnemyPositions = enemyPositions,
            EnemyWidths = enemyWidths,
            Distances = distances
        };

        // Schedule the job.
        JobHandle handle = job.Schedule(enemyCount, 64);

        // Create and store the async request.
        EnemyRangeRequest request = new EnemyRangeRequest
        {
            radius = radius,
            maxCount = maxCount,
            callback = callback,
            enemyPositions = enemyPositions,
            distances = distances,
            jobHandle = handle,
            enemyCount = enemyCount,
            enemyTransforms = enemyTransforms
        };

        _pendingRequests.Add(request);
    }

    public Transform[] GetFarthestEnemies()
    {
        return _farthestEnemies;
    }

    public MobLibrary GetMobLibrary()
    {
        return mobLibrary;
    }

    /// <summary>
    /// A job that calculates the distance from the player's position to each enemy's position.
    /// </summary>
    struct DistanceJob : IJobParallelFor
    {
        public Vector3 PlayerPosition;
        [ReadOnly] public NativeArray<Vector3> EnemyPositions;
        [ReadOnly] public NativeArray<float> EnemyWidths;
        public NativeArray<float> Distances;

        public void Execute(int index)
        {
            Vector3 diff = EnemyPositions[index] - PlayerPosition;
            Distances[index] = diff.magnitude - EnemyWidths[index];
        }
    }

    /// <summary>
    /// Structure to hold data for an asynchronous enemy search request.
    /// </summary>
    struct EnemyRangeRequest
    {
        public float radius;
        public int maxCount;
        public Action<Transform[]> callback;
        public NativeArray<Vector3> enemyPositions;
        public NativeArray<float> distances;
        public JobHandle jobHandle;
        public int enemyCount;
        public Transform[] enemyTransforms;
    }
}
