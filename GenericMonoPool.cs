using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A generic pooling class that handles any MonoBehaviour that you pass in.
/// </summary>
/// <typeparam name="T">A MonoBehaviour type (e.g. a ParticleSystem, custom AI, etc.)</typeparam>
public class GenericMonoPool<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Prefab Reference")]
    public T prefab; // The T-based prefab to pool

    [Header("Pool Configuration")]
    public int initialPoolSize = 10;

    private Queue<T> _poolQueue;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        _poolQueue = new Queue<T>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            T newObj = Instantiate(prefab);
            newObj.gameObject.SetActive(false);
            _poolQueue.Enqueue(newObj);
        }
    }

    /// <summary>
    /// Retrieves an object from the pool.
    /// Creates a new one if the pool is empty (optional behavior).
    /// </summary>
    public T GetFromPool()
    {
        if (_poolQueue.Count == 0)
        {
            // Optional: Expand the pool
            T newObj = Instantiate(prefab);
            newObj.gameObject.SetActive(true);
            return newObj;
        }
        else
        {
            T pooledObj = _poolQueue.Dequeue();
            pooledObj.gameObject.SetActive(true);
            return pooledObj;
        }
    }

    /// <summary>
    /// Returns an object to the pool, deactivating it.
    /// </summary>
    /// <param name="obj">The T-based object to return.</param>
    public void ReturnToPool(T obj)
    {
        obj.gameObject.SetActive(false);
        _poolQueue.Enqueue(obj);
    }
}
