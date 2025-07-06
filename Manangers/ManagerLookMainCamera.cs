using UnityEngine;
using System.Collections.Generic;

public class ManagerLookMainCamera : MonoBehaviour
{
    private static List<LookAtMainCamera> _registeredObjects = new List<LookAtMainCamera>();
    private static Camera _cachedCamera;

    [SerializeField]
    private float _refreshInterval = 0f;

    private float _timer;

    private void Awake()
    {
        _cachedCamera = Camera.main;
    }

    private void Update()
    {
        if (_refreshInterval > 0)
        {
            _timer += Time.deltaTime;
            if (_timer < _refreshInterval)
                return;
            _timer = 0f;
        }

        // Re-cache camera if needed
        _cachedCamera = Camera.main;
        if (_cachedCamera == null) return;

        // Update all registered objects in a single loop
        for (int i = 0; i < _registeredObjects.Count; i++)
        {
            _registeredObjects[i].TriggerLookAt(_cachedCamera.transform);
        }
    }

    public static void Register(LookAtMainCamera obj)
    {
        if (!_registeredObjects.Contains(obj))
            _registeredObjects.Add(obj);
    }

    public static void Unregister(LookAtMainCamera obj)
    {
        if (_registeredObjects.Contains(obj))
            _registeredObjects.Remove(obj);
    }
}
