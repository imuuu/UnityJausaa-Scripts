using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Spawns a configurable number of IStatistics prefabs randomly inside a PoisonPoolMorph area on a grid,
/// with a delay between each spawn. Ensures unique cells, occupancy checks, and no overlapping waves.
/// Uses either a specific prefab list or waves defined in ManagerSpawning.
/// </summary>
public class PoisonPoolSpawner : MonoBehaviour
{
    [Tooltip("Reference to the PoisonPoolMorph component defining the area")]
    [SerializeField] private PoisonPoolMorph _pool;

    [Tooltip("Optional list of specific prefabs to spawn instead of wave-based ones")]
    [SerializeField] private List<GameObject> _specificSpawnPrefabs = new List<GameObject>();

    [Tooltip("Optional parent for spawned instances")]
    [SerializeField] private Transform _spawnParent;

    [Header("Wave Settings")]
    [Tooltip("Total number of spawns to perform each wave")]
    [SerializeField] private int _spawnCount = 10;
    [SerializeField] private int _spawnedGuardingCount = 3;

    [Tooltip("Percentage of the pool radius to trigger spawning")]
    [SerializeField] private float _triggerRadiusPercent = 1.2f;

    [Tooltip("Delay between individual spawns within a wave (seconds)")]
    [SerializeField] private float _spawnInterval = 0.3f;
    [SerializeField] private float _spawnDelay = 3f;

    [Tooltip("Amount of pool fill reduced per spawned entity, scaled by radius of the pool _eachMobReduceArea / radius of the pool")]
    [SerializeField] private float _eachMobReduceArea = 0.15f;
    [SerializeField] private bool _destroyOnWaveEnd = true;

    private List<Vector2> _gridPoints = new List<Vector2>();
    private float _cellSize;
    private bool _isSpawning = false;

    private const float MIN_FILLOUT = 0.01f; // Minimum fill reduction per entity
    private bool _triggered = false;
    private Transform _player;
    private List<GameObject> _guardingEntities = new ();
    private void Start()
    {
        // Determine grid cell size from width of spawning objects
        if (_specificSpawnPrefabs != null && _specificSpawnPrefabs.Count > 0)
        {
            _cellSize = _specificSpawnPrefabs
                .Select(p => p.GetComponentInChildren<IStatistics>())
                .Where(s => s != null)
                .Select(s => s.Width)
                .DefaultIfEmpty(1f)
                .Max();
        }
        else
        {
            // use wave definitions from ManagerSpawning
            var waves = ManagerSpawning.Instance.GetToxicPoolWaves();
            _cellSize = waves
                .SelectMany(w => w.GetPrefabs())
                .Select(entry => entry.Prefab.GetComponentInChildren<IStatistics>())
                .Where(s => s != null)
                .Select(s => s.Width)
                .DefaultIfEmpty(1f)
                .Max();
        }

        if (_spawnedGuardingCount > 0)
        {
            // Spawn guarding entities at the pool center
            for (int i = 0; i < _spawnedGuardingCount; i++)
            {
                var guardPosition = new Vector2(_pool.transform.position.x, _pool.transform.position.z);
                if (IsCellUnoccupied(guardPosition))
                {
                    var guard = GetGameObject(guardPosition);
                    if (guard.TryGetComponent(out ChaseMovement chaseMovement))
                    {
                        chaseMovement.SetPatrol(_pool.transform, _pool.GetRadius() * 0.9f);
                    }
                    _guardingEntities.Add(guard);
                }
                else
                {
                    Debug.LogWarning("Guard position is occupied, skipping spawn.");
                }
            }
        }

        Player.AssignTransformWhenAvailable((player) =>
        {
            _player = player;
        });
    }

    private void Update()
    {
        if (_triggered) return;

        if (_player == null || _pool == null) return;
        
        if(Vector3.Distance(_player.position, _pool.transform.position) < _pool.GetRadius() * _triggerRadiusPercent)
        {
            _triggered = true;
            foreach (var guard in _guardingEntities)
            {
                if(guard == null) continue;

                if (guard.TryGetComponent(out ChaseMovement chaseMovement))
                {
                    chaseMovement.DisablePatrol();
                }
            }
            _guardingEntities.Clear();
            SpawnWave();
        }
    }

    /// <summary>
    /// Scans the pool mesh and builds grid of valid cell centers inside the polygon.
    /// </summary>
    private void GenerateGrid()
    {
        _gridPoints.Clear();

        var mf = _pool.GetComponent<MeshFilter>();
        var mesh = mf.mesh;
        var verts = mesh.vertices
            .Skip(1)
            .Select(v => _pool.transform.TransformPoint(v))
            .Select(w => new Vector2(w.x, w.z))
            .ToArray();

        float minX = verts.Min(v => v.x);
        float maxX = verts.Max(v => v.x);
        float minZ = verts.Min(v => v.y);
        float maxZ = verts.Max(v => v.y);

        int cols = Mathf.CeilToInt((maxX - minX) / _cellSize);
        int rows = Mathf.CeilToInt((maxZ - minZ) / _cellSize);

        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
            {
                var pt = new Vector2(
                    minX + (i + 0.5f) * _cellSize,
                    minZ + (j + 0.5f) * _cellSize
                );
                if (PointInPolygon(pt, verts))
                    _gridPoints.Add(pt);
            }
    }

    /// <summary>
    /// Starts a spawning wave. Will not run if a previous wave is still spawning.
    /// </summary>
    [Button]
    public void SpawnWave()
    {
        if (_isSpawning) { Debug.LogWarning("SpawnWave called while previous wave still in progress"); return; }
        if (_pool == null) { Debug.LogError("Pool reference missing"); return; }

        GenerateGrid();
        if (_gridPoints.Count == 0)
        {
            Debug.LogWarning("No valid spawn positions inside pool.");
            return;
        }

        var valid = _gridPoints.Where(IsCellUnoccupied).ToList();
        if (valid.Count == 0)
        {
            Debug.LogWarning("No unoccupied cells available.");
            return;
        }

        int spawnedCount = Mathf.CeilToInt(_spawnCount * _pool.GetFill());
        //Debug.Log($"Spawning {spawnedCount} entities in pool with fill {Mathf.RoundToInt(_pool.GetFill() * 100)}%");
        var chosen = valid
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(Mathf.Min(spawnedCount, valid.Count))
            .ToList();

        _isSpawning = true;

        // schedule each spawn
        for (int i = 0; i < chosen.Count; i++)
        {
            var pt = chosen[i];
            float delay = _spawnInterval * i;
            ActionScheduler.RunAfterDelay(delay, () => SpawnAt(pt));
        }

        ActionScheduler.RunAfterDelay(_spawnInterval * chosen.Count, WaveCompleted);
    }

    private void WaveCompleted()
    {
        _isSpawning = false;
        if(_pool.GetFill() <= 0.13f)
        {
            Debug.Log("All entities spawned, pool is empty.");
            _pool.ReduceFill(1f);

            if (_destroyOnWaveEnd)
                Destroy(this.gameObject);
            else
                this.gameObject.SetActive(false);

            _triggered = false;
            return;
        }

        ActionScheduler.RunAfterDelay(_spawnDelay, () =>
        {
            SpawnWave();
        });

    }

    /// <summary>
    /// Attempts to spawn one entity at the given grid cell, re-checking occupancy.
    /// </summary>
    private void SpawnAt(Vector2 pt)
    {
        if (!IsCellUnoccupied(pt)) return;

        GetGameObject(pt);

        float fillOut = _eachMobReduceArea / _pool.GetRadius();
        if(fillOut < MIN_FILLOUT)
        {
            fillOut = MIN_FILLOUT;
        }
        _pool.ReduceFill(fillOut);
    }

    private GameObject GetGameObject(Vector2 pt)
    {
        GameObject go;
        if (_specificSpawnPrefabs != null && _specificSpawnPrefabs.Count > 0)
        {
            go = Instantiate(
                _specificSpawnPrefabs[UnityEngine.Random.Range(0, _specificSpawnPrefabs.Count)],
                ToWorld(pt),
                Quaternion.identity,
                _spawnParent
            );
        }
        else
        {
            go = ManagerSpawning.Instance.GetSpawnedEnemyToxicPools();
            go.transform.position = ToWorld(pt);
        }

        return go;
    }

    private Vector3 ToWorld(Vector2 pt)
        => new Vector3(pt.x, _pool.transform.position.y, pt.y);

    /// <summary>
    /// Checks if a grid cell is free of colliders in the occupancy mask.
    /// </summary>
    private bool IsCellUnoccupied(Vector2 pt)
    {
        var center = new Vector3(pt.x, _pool.transform.position.y + 0.5f, pt.y);
        var half = new Vector3(_cellSize * 0.5f, 0.5f, _cellSize * 0.5f);
        return !Physics.CheckBox(center, half, Quaternion.identity, ManagerSpawning.Instance.GetUnallowedSpawnLayers());
    }

    /// <summary>
    /// Standard point-in-polygon test.
    /// </summary>
    private bool PointInPolygon(Vector2 point, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; j = i++)
        {
            if (((poly[i].y > point.y) != (poly[j].y > point.y)) &&
                (point.x < (poly[j].x - poly[i].x) * (point.y - poly[i].y)
                 / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }
}
