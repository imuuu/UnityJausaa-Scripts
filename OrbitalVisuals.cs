using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic component to instantiate and orbit visual objects (e.g., shield prefabs) around a target Transform.
/// Allows for adjustable radius, spin speed, vertical sinusoidal movement, pivot height,
/// orientation modes (self-rotate, face center, face outward), and single-instance controls.
/// </summary>
public class OrbitalVisuals : MonoBehaviour
{
    public enum OrientationMode
    {
        SelfRotate,   // Visuals spin on their own axis
        FaceCenter,   // Visuals always face towards orbit center
        FaceOutward   // Visuals always face away from orbit center
    }

    [SerializeField, Tooltip("Prefabs to instantiate and spin around the target.")]
    private List<GameObject> _visualPrefabs = new List<GameObject>();

    [SerializeField, Tooltip("Target Transform around which visuals will orbit. Defaults to this GameObject.")]
    private Transform _target;

    [SerializeField, Tooltip("Radius of the orbit.")]
    private float _radius = 2f;

    [SerializeField, Tooltip("Angular speed in radians per second.")]
    private float _spinSpeed = 1f;

    [SerializeField, Tooltip("Vertical oscillation amplitude.")]
    private float _heightAmplitude = 0.5f;

    [SerializeField, Tooltip("Vertical oscillation frequency.")]
    private float _heightFrequency = 1f;

    [SerializeField, Tooltip("Vertical pivot offset from target position.")]
    private float _heightPivot = 0f;

    [SerializeField, Tooltip("If true, instantiated visuals are parented and inherit parent rotation.")]
    private bool _followParentRotation = true;

    [SerializeField, Tooltip("Orientation behavior of the visuals relative to the center.")]
    private OrientationMode _orientationMode = OrientationMode.SelfRotate;

    [SerializeField, Tooltip("If true, prevents instantiation of duplicate prefabs by name.")]
    private bool _singleInstanceMode = false;

    [SerializeField, Tooltip("If true and single-instance mode is on, removes existing instance before adding new one.")]
    private bool _removeExistingOnAdd = false;

    private readonly List<Transform> _visuals = new List<Transform>();
    private readonly List<float> _baseAngles = new List<float>();

    #region Public API
    public List<GameObject> VisualPrefabs => _visualPrefabs;
    public Transform Target
    {
        get => _target ?? transform;
        set => _target = value;
    }
    public float Radius { get => _radius; set => _radius = value; }
    public float SpinSpeed { get => _spinSpeed; set => _spinSpeed = value; }
    public float HeightAmplitude { get => _heightAmplitude; set => _heightAmplitude = value; }
    public float HeightFrequency { get => _heightFrequency; set => _heightFrequency = value; }
    public float HeightPivot { get => _heightPivot; set => _heightPivot = value; }
    public bool FollowParentRotation { get => _followParentRotation; set => _followParentRotation = value; }
    public OrientationMode Mode { get => _orientationMode; set => _orientationMode = value; }
    public bool SingleInstanceMode { get => _singleInstanceMode; set => _singleInstanceMode = value; }
    public bool RemoveExistingOnAdd { get => _removeExistingOnAdd; set => _removeExistingOnAdd = value; }

    /// <summary>
    /// Clears all instantiated visuals.
    /// </summary>
    public void ClearAll()
    {
        for (int i = _visuals.Count - 1; i >= 0; i--)
        {
            Destroy(_visuals[i].gameObject);
            _visuals.RemoveAt(i);
            _baseAngles.RemoveAt(i);
        }
    }

    /// <summary>
    /// Clears a specific visual instance by index.
    /// </summary>
    public void ClearAt(int index)
    {
        if (index < 0 || index >= _visuals.Count) return;
        Destroy(_visuals[index].gameObject);
        _visuals.RemoveAt(index);
        _baseAngles.RemoveAt(index);
    }

    public void ClearByPrefab(GameObject prefab)
    {
        string name = prefab.name;
        int index = _visuals.FindIndex(v => v.name.Contains(name));
        if (index != -1)
        {
            ClearAt(index);
        }
    }

    /// <summary>
    /// Adds a new visual prefab at runtime, respecting single-instance and removal settings.
    /// </summary>
    public void AddVisualPrefab(GameObject prefab)
    {
        string name = prefab.name;
        int existingIndex = _visuals.FindIndex(v => v.name.Contains(name));
        Debug.Log($"Adding visual prefab: {name}, existing index: {existingIndex}");
        if (_singleInstanceMode && existingIndex != -1)
        {
            if (_removeExistingOnAdd)
            {
                ClearAt(existingIndex);
            }
            else
            {
                Debug.LogWarning($"Visual with name '{name}' already exists. Use RemoveExistingOnAdd to replace it.");
                return;
            }
        }
        Debug.Assert(prefab != null, "Prefab cannot be null");
        int index = _visuals.Count;
        GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity,
            _followParentRotation ? transform : null);
        instance.name = name; // ensure consistent naming
        _visuals.Add(instance.transform);
        _baseAngles.Add(index * Mathf.PI * 2f / (_visuals.Count));
    }
    #endregion

    private void Awake()
    {
        if (_target == null)
            _target = transform;

        InstantiateVisuals();
    }

    private void InstantiateVisuals()
    {
        ClearAll();

        int count = _visualPrefabs.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = _visualPrefabs[i];
            AddVisualPrefab(prefab);
        }
        // Recalculate base angles evenly
        for (int i = 0; i < _baseAngles.Count; i++)
        {
            _baseAngles[i] = i * Mathf.PI * 2f / _baseAngles.Count;
        }
    }

    private void Update()
    {
        if(Target == null || _visuals.Count == 0)
            return;

        Vector3 center = Target.position + Vector3.up * _heightPivot;
        float time = Time.time;

        for (int i = 0; i < _visuals.Count; i++)
        {
            float angle = _baseAngles[i] + time * _spinSpeed;
            float x = Mathf.Cos(angle) * _radius;
            float z = Mathf.Sin(angle) * _radius;
            float y = Mathf.Sin(time * _heightFrequency + _baseAngles[i]) * _heightAmplitude;

            Transform vis = _visuals[i];
            Vector3 worldPos = center + new Vector3(x, y, z);
            vis.position = worldPos;

            // Orientation modes
            switch (_orientationMode)
            {
                case OrientationMode.SelfRotate:
                    vis.Rotate(Vector3.up, _spinSpeed * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
                    break;
                case OrientationMode.FaceCenter:
                    vis.LookAt(center);
                    break;
                case OrientationMode.FaceOutward:
                    vis.LookAt(worldPos + (worldPos - center));
                    break;
            }
        }
    }
}
