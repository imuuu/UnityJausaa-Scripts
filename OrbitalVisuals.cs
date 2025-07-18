using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Generic component to instantiate and orbit visual objects (e.g., shield prefabs) around a target Transform.
/// Allows for adjustable radius, spin speed, vertical sinusoidal movement, pivot height,
/// orientation modes (self-rotate, face center, face outward), and single-instance controls.
/// </summary>
public class OrbitalVisuals : MonoBehaviour
{
    public enum OrientationMode
    {
        SELF_ROTATE,   // Visuals spin on their own axis
        FACE_CENTER,   // Visuals always face towards orbit center
        FACE_OUTWARD   // Visuals always face away from orbit center
    }

    [SerializeField, Tooltip("Prefabs to instantiate and spin around the target.")]
    private List<GameObject> _visualPrefabs = new ();

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
    private OrientationMode _orientationMode = OrientationMode.SELF_ROTATE;

    [SerializeField, Tooltip("If true, prevents instantiation of duplicate prefabs by name.")]
    private bool _singleInstanceMode = false;

    [SerializeField, Tooltip("If true and single-instance mode is on, removes existing instance before adding new one.")]
    private bool _removeExistingOnAdd = false;

    [ReadOnly] private List<Transform> _visuals = new ();
    [ReadOnly] private List<float> _baseAngles = new ();

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

    public void ClearAll()
    {
        for (int i = _visuals.Count - 1; i >= 0; i--)
        {
            Destroy(_visuals[i].gameObject);
            _visuals.RemoveAt(i);
            _baseAngles.RemoveAt(i);
        }
    }

    public void ClearAt(int index)
    {
        if (index < 0 || index >= _visuals.Count) return;
        Destroy(_visuals[index].gameObject);
        _visuals.RemoveAt(index);
        _baseAngles.RemoveAt(index);

        CalculateBaseAngles();
    }

    private void CalculateBaseAngles()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            float angle = i * Mathf.PI * 2f / _visuals.Count;
            _baseAngles[i] = angle;
        }
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
        int index = _visuals.Count;
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity,
            _followParentRotation ? transform : null);
        go.name = name;
        _visuals.Add(go.transform);
        _baseAngles.Add(index * Mathf.PI * 2f / _visuals.Count);
    }

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

            switch (_orientationMode)
            {
                case OrientationMode.SELF_ROTATE:
                    vis.Rotate(Vector3.up, _spinSpeed * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
                    break;
                case OrientationMode.FACE_CENTER:
                    vis.LookAt(center);
                    break;
                case OrientationMode.FACE_OUTWARD:
                    vis.LookAt(worldPos + (worldPos - center));
                    break;
            }
        }
    }
}
