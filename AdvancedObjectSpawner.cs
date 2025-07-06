using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AdvancedObjectSpawner : MonoBehaviour
{
    public enum SpawnMode { Continuous, Burst, Manual }
    public enum SpawnAreaType { Cube, Sphere }

    [Header("Core Settings")]
    [Tooltip("Prefab to spawn")]
    public GameObject spawnPrefab;
    [Tooltip("Spawner operation mode")]
    public SpawnMode spawnMode = SpawnMode.Continuous;

    [Header("Timing Settings")]
    [Tooltip("Initial delay before spawning starts")]
    public float initialDelay = 0f;
    [Tooltip("Minimum time between spawns")]
    public float minSpawnInterval = 1f;
    [Tooltip("Maximum time between spawns")]
    public float maxSpawnInterval = 3f;

    [Header("Burst Mode Settings")]
    [Tooltip("Number of objects per burst")]
    public int burstCount = 5;
    [Tooltip("Time between bursts")]
    public float burstInterval = 5f;

    [Header("Position Settings")]
    public SpawnAreaType spawnAreaType = SpawnAreaType.Cube;
    [Tooltip("Base position offset")]
    public Vector3 positionOffset = Vector3.zero;
    [Tooltip("Cube spawn area dimensions")]
    public Vector3 cubeDimensions = Vector3.one;
    [Tooltip("Sphere spawn radius")]
    public float sphereRadius = 1f;
    public bool randomizePosition = true;

    [Header("Rotation Settings")]
    public bool randomizeRotation = false;
    [Tooltip("Minimum rotation angles for each axis")]
    public Vector3 minRotation = Vector3.zero;
    [Tooltip("Maximum rotation angles for each axis")]
    public Vector3 maxRotation = new Vector3(360, 360, 360);

    [Header("Scale Settings")]
    public bool randomizeScale = false;
    public bool uniformScaling = true;
    [Tooltip("Minimum scale values")]
    public Vector3 minScale = Vector3.one;
    [Tooltip("Maximum scale values")]
    public Vector3 maxScale = Vector3.one;

    [Header("Pooling Settings")]
    [Tooltip("Initial pool size")]
    public int poolSize = 20;
    [Tooltip("Whether pool can grow if needed")]
    public bool expandablePool = true;
    [Tooltip("Maximum allowed spawned objects (0 = unlimited)")]
    public int maxSpawnedObjects = 0;

    [Header("Events")]
    public UnityEvent<GameObject> OnSpawned;

    private Queue<GameObject> objectPool;
    private Coroutine spawningCoroutine;
    private int activeSpawnedCount = 0;

    void Awake()
    {
        InitializePool();
    }

    void OnEnable()
    {
        if (spawnMode != SpawnMode.Manual)
        {
            StartSpawning();
        }
    }

    void OnDisable()
    {
        StopSpawning();
    }

    void InitializePool()
    {
        objectPool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledObject();
        }
    }

    GameObject CreatePooledObject()
    {
        if (!spawnPrefab) return null;

        GameObject obj = Instantiate(spawnPrefab);
        obj.SetActive(false);
        obj.AddComponent<PooledObject>().spawner = this;
        objectPool.Enqueue(obj);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        objectPool.Enqueue(obj);
        activeSpawnedCount--;
    }

    public void StartSpawning()
    {
        if (spawningCoroutine == null)
        {
            spawningCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    public void StopSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    public void SpawnManual(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnObject();
        }
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            switch (spawnMode)
            {
                case SpawnMode.Continuous:
                    SpawnObject();
                    yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
                    break;

                case SpawnMode.Burst:
                    yield return StartCoroutine(BurstSpawn());
                    break;
            }
        }
    }

    IEnumerator BurstSpawn()
    {
        for (int i = 0; i < burstCount; i++)
        {
            SpawnObject();
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
        }
        yield return new WaitForSeconds(burstInterval);
    }

    void SpawnObject()
    {
        if (maxSpawnedObjects > 0 && activeSpawnedCount >= maxSpawnedObjects) return;
        if (objectPool.Count == 0 && !expandablePool) return;

        GameObject obj = GetPooledObject();
        if (!obj) return;

        SetupObject(obj);
        activeSpawnedCount++;
        OnSpawned.Invoke(obj);
    }

    GameObject GetPooledObject()
    {
        if (objectPool.Count > 0) return objectPool.Dequeue();
        if (expandablePool) return CreatePooledObject();
        return null;
    }

    void SetupObject(GameObject obj)
    {
        obj.transform.position = GetSpawnPosition();
        obj.transform.rotation = GetSpawnRotation();
        obj.transform.localScale = GetSpawnScale();
        obj.SetActive(true);
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 basePosition = transform.position + positionOffset;

        if (!randomizePosition) return basePosition;

        return spawnAreaType switch
        {
            SpawnAreaType.Cube => basePosition + new Vector3(
                Random.Range(-cubeDimensions.x / 2, cubeDimensions.x / 2),
                Random.Range(-cubeDimensions.y / 2, cubeDimensions.y / 2),
                Random.Range(-cubeDimensions.z / 2, cubeDimensions.z / 2)),

            SpawnAreaType.Sphere => basePosition + Random.insideUnitSphere * sphereRadius,

            _ => basePosition
        };
    }

    Quaternion GetSpawnRotation()
    {
        if (!randomizeRotation) return transform.rotation;

        return Quaternion.Euler(
            Random.Range(minRotation.x, maxRotation.x),
            Random.Range(minRotation.y, maxRotation.y),
            Random.Range(minRotation.z, maxRotation.z)
        );
    }

    Vector3 GetSpawnScale()
    {
        if (!randomizeScale) return spawnPrefab.transform.localScale;

        return uniformScaling
            ? Vector3.one * Random.Range(minScale.x, maxScale.x)
            : new Vector3(
                Random.Range(minScale.x, maxScale.x),
                Random.Range(minScale.y, maxScale.y),
                Random.Range(minScale.z, maxScale.z)
            );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 position = transform.position + positionOffset;

        if (spawnAreaType == SpawnAreaType.Cube)
        {
            Gizmos.DrawWireCube(position, cubeDimensions);
        }
        else
        {
            Gizmos.DrawWireSphere(position, sphereRadius);
        }
    }
}

public class PooledObject : MonoBehaviour
{
    public AdvancedObjectSpawner spawner;

    void OnDisable()
    {
        if (spawner != null)
        {
            spawner.ReturnToPool(gameObject);
        }
    }
}