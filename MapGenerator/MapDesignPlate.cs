using System.Collections.Generic;
using Game.ChunkSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.MapGenerator
{
    public class MapDesignPlate : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("If checked, the plate can be rotated.")]
        [SerializeField] private bool _isRotatable = true;
        //[Tooltip("Initial rotation (in steps of 90° clockwise)")]
        //[SerializeField] private int _initialRotationSteps = 0; // 0 = no rotation; 1 = 90°; etc.

        [Header("Placement Settings")]
        [Tooltip("Determines where this plate is allowed to be placed. 'Any' means no restriction.")]
        [SerializeField] private PLACEMENT_TYPE _placementType = PLACEMENT_TYPE.ANY;

        public bool IsRotatable => _isRotatable;
        public PLACEMENT_TYPE AllowedPlacement => _placementType;

        // (Assume your connection data is already set up.)
        [SerializeField]
        private DesignPlateConnections _connections;

        // Keep track of the current rotation state (0, 1, 2, or 3)
        private int _currentRotation = 0;

        [SerializeField, ReadOnly] private int _cellLinearIndex = -1;
        [SerializeField, ReadOnly] private ManagerMapDesignPlates _manager;

        private List<MapSpawnController> _spawnControllers = new();
        private List<MapSpawnItemBase> _spawnItems = new();

        // private void Awake()
        // {
        //     if (_isRotatable && _initialRotationSteps != 0)
        //     {
        //         RotatePlate(_initialRotationSteps);
        //     }
        // }

        // private void Start()
        // {
        //     if(ManagerChunks.Instance == null) return;

        //     Chunk chunk = ManagerChunks.Instance.RegisterObject(this.gameObject);

        //     if(!chunk.IsActive)
        //     {
        //         gameObject.SetActive(false);
        //     }
        // }

        public void SetCellLinearIndex(int cellNumber)
        {
            _cellLinearIndex = cellNumber;
        }

        public void SetManager(ManagerMapDesignPlates manager)
        {
            _manager = manager;
        }

        // private void OnValidate()
        // {
        //     _initialRotationSteps = _initialRotationSteps % 4;
        //     RefreshPlate();
        // }

        /// <summary>
        /// Rotates the plate by a given number of 90° steps clockwise.
        /// </summary>
        public void RotatePlate(int steps)
        {
            if (!_isRotatable)
                return;

            steps = steps % 4;
            _currentRotation = (_currentRotation + steps) % 4;
            _connections.RotateConnectionsClockwise(steps);
            RefreshPlate();
        }

        /// <summary>
        /// Refreshes the plate’s visual representation.
        /// </summary>
        public void RefreshPlate()
        {
            transform.rotation = Quaternion.Euler(0, _currentRotation * 90f, 0);
            // Additional visual updates here if needed.
        }

        /// <summary>
        /// Returns the connection data for neighbor matching.
        /// </summary>
        public DesignPlateConnections GetConnections()
        {
            return _connections;
        }

        /// <summary>
        /// Starts the recursive search and triggers spawn on the collected items.
        /// </summary>
        [Button("Trigger Spawn Items on This Plate", ButtonSizes.Medium)]
        public void TriggerSpawnItems()
        {
            _spawnControllers.Clear();
            _spawnItems.Clear();

            ManagerMapDesignPlates.TraverseChildren(transform, ref _spawnControllers, ref _spawnItems);

            Debug.Log($"Found {_spawnControllers.Count} controllers and {_spawnItems.Count} spawn items.");

            foreach (var item in _spawnItems)
            {
                Debug.Log($"Triggering spawn for spawn item: {item.name}");
                item.TriggerSpawn();
            }

            // If needed, you could also handle the controllers separately.
            // foreach (var controller in spawnControllers) { ... }

            foreach (var controller in _spawnControllers)
            {
                Debug.Log($"Triggering spawn for controller: {controller.name}");
                controller.TriggerSpawn();
            }
        }

        [Button("Re-Create This Plate", ButtonSizes.Medium), HideIf("@_manager == null")]
        public void ReCreateThisPlate()
        {
            if(_manager != null && _cellLinearIndex >= 0)
                _manager.ReCreateOnlyPlateAtIndex(_cellLinearIndex);
        }

        private void OnDrawGizmosSelected()
        {
            if(ManagerMapDesignPlates.Instance == null)
                return;

            Gizmos.color = Color.green;

            float size = ManagerMapDesignPlates.Instance.PlateSize;
            Gizmos.DrawWireCube(transform.position, Vector3.one * size);
        }
    }
}