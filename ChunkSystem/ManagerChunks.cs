using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using System;

namespace Game.ChunkSystem
{
    /// <summary>
    /// Manages a grid of 2D chunks covering the entire game world.
    /// The grid is automatically generated (default 30x30 chunks, centered at (0,0) in XZ) and only a configurable
    /// area (default 3x3 chunks around the player) remains active at any time.
    /// When a chunk is deactivated, its objects are disabled and ghost data is created for those implementing IMovement.
    /// </summary>
    [ExecuteAlways]
    [Title("Manager Chunks")]
    public class ManagerChunks : MonoBehaviour
    {
        public static ManagerChunks Instance { get; private set; }
        [BoxGroup("Activation")]
        [SerializeField] private bool _enableChunks = true;

        [BoxGroup("Grid Settings")]
        [Tooltip("Number of chunks per axis. The world will be square (e.g., 30 gives 30x30 chunks).")]
        [SerializeField] private int _gridSize = 30;

        [BoxGroup("Activation Settings")]
        [Tooltip("How many chunks away from the player (in each direction) remain active. E.g., 1 gives a 3x3 grid.")]
        [SerializeField] private int _activeRadiusInChunks = 1;

        [BoxGroup("Activation Settings")]
        [Tooltip("Toggle dynamic activation of chunks based on player position.")]
        [SerializeField] private bool _dynamicActivation = true;

        [BoxGroup("Chunk Settings")]
        [Tooltip("Dimensions of each chunk (X and Z).")]
        [SerializeField] private Vector2 _chunkSize = new Vector2(50, 50);

        [BoxGroup("Occupied Layers")]
        [Tooltip("Layers that are considered occupied by some objects.")]
        [SerializeField] private LayerMask _occupiedLayers;

        [SerializeField, BoxGroup("Chunk Random Spawn Objects")]
        [Tooltip("Radius is calculated from where x and z are 0 in world space")]
        private float _nonSpawnableRadius = 40f;
        [BoxGroup("Chunk Random Spawn Objects")]
        [Tooltip("Loot table for random spawnable objects in chunks. Objects will be spawned when a chunk is activated.")]
        [SerializeField] private List<ChunkItem> _spawnableObjectsLootTable;

        [BoxGroup("Debug")]
        [Tooltip("Toggle drawing chunk boundaries in the Scene view. Inactive chunks are red; active chunks are cyan.")]
        [SerializeField] private bool _debugChunks = true;

        [BoxGroup("Debug")]
        [SerializeField, ReadOnly] private Transform _playerTransform;

        private Dictionary<Vector2Int, Chunk> _chunks = new();
        private List<MovingGhostObjectData> _movingGhostObjectDataList = new();
        private NativeArray<MovingGhostObjectData> _movingGhostObjectDataNative;

        private bool _areaInitialized = false;

        private LootTable<ChunkItem> _spawnableLootTable;
        private Dictionary<Vector2Int, List<ChunkItem>> _spawnablesInChunk;

        // [System.Serializable]
        // private class SpawnableObject : IWeightedLoot
        // {
        //     public GameObject GameObject;
        //     public float Weight;

        //     public float GetWeight()
        //     {
        //         return Weight;
        //     }
        // }
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);


            if (_enableChunks)
            {
                _spawnablesInChunk = new Dictionary<Vector2Int, List<ChunkItem>>();
                _spawnableLootTable = new LootTable<ChunkItem>(_spawnableObjectsLootTable);
                GenerateChunks();
            }

            Events.OnPlayableSceneChangeEnter.AddListener(OnPlayableSceneChange);
        }

        private bool OnPlayableSceneChange(SCENE_NAME param)
        {
            return true;
        }

        private void Start()
        {
            if (!_enableChunks) return;

            Player.AssignTransformWhenAvailable((t) => _playerTransform = t);

        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && _enableChunks)
            {
                GenerateChunks();
            }
        }
#endif

        /// <summary>
        /// Generates a square grid of chunks.
        /// The grid is centered at (0,0) with indices from negative to positive.
        /// For an even GridSize, indices range from -GridSize/2 to GridSize/2 - 1.
        /// </summary>
        public void GenerateChunks()
        {
            _chunks.Clear();
            int halfGrid = _gridSize / 2;
            int min = -halfGrid;
            int max = (_gridSize % 2 == 0) ? (halfGrid - 1) : halfGrid;
            for (int x = min; x <= max; x++)
            {
                for (int y = min; y <= max; y++)
                {
                    Vector2Int index = new Vector2Int(x, y);
                    Rect area = new Rect(x * _chunkSize.x, y * _chunkSize.y, _chunkSize.x, _chunkSize.y);
                    _chunks.Add(index, new Chunk(index, area));

                    if(!Application.isPlaying) continue;

                    if(Vector2.Distance(Vector2.zero, new Vector2(area.center.x, area.center.y)) < _nonSpawnableRadius)
                    {
                        continue;
                    }

                    ChunkItem randomChunkItems = _spawnableLootTable.GetRandomItem();
                    if (randomChunkItems.GameObject == null) continue;

                    AddRandomSpawnableToChunk(randomChunkItems, index);
                }
            }
        }

        

        public void AddRandomSpawnableToChunk(ChunkItem item, Vector2Int chunkIndex)
        {
            if (!_spawnablesInChunk.TryGetValue(chunkIndex, out List<ChunkItem> chunkItems))
            {
                chunkItems = new List<ChunkItem>();
                _spawnablesInChunk.Add(chunkIndex, chunkItems);
            }
            chunkItems.Add(item);
        }

        public void RemoveRandomSpawnableFromChunk(ChunkItem item, Vector2Int chunkIndex)
        {
            if (_spawnablesInChunk.TryGetValue(chunkIndex, out List<ChunkItem> chunkItems))
            {
                chunkItems.Remove(item);
                if (chunkItems.Count == 0)
                {
                    _spawnablesInChunk.Remove(chunkIndex);
                }
            }
        }

        public void RemoveRandomSpawnablesFromChunk(Vector2Int chunkIndex)
        {
            if (_spawnablesInChunk.TryGetValue(chunkIndex, out List<ChunkItem> chunkItems))
            {
                chunkItems.Clear();
                _spawnablesInChunk.Remove(chunkIndex);
            }
        }
        // [SerializeField] private RayfireChunkActivator[] _chunkActivators;
        // private Dictionary<Vector2Int, List<RayfireChunkActivator>> _chunkActivatorsChunks = new ();
        // private async Task FindAllChunkActivatorsAsync()
        // {
        //     // Optionally yield to let the frame finish before starting
        //     await Task.Yield();
        //     await Task.Yield();

        //     // Find all objects of type RayfireChunkActivator, including disabled ones.
        //     // This overload (with the 'true' parameter) makes sure inactive objects are included.
        //     _chunkActivators = FindObjectsByType<RayfireChunkActivator>(findObjectsInactive:FindObjectsInactive.Include, FindObjectsSortMode.None);

        //     // Process each activator, yielding after each one to avoid frame hitches if many objects are found.
        //     foreach (var activator in _chunkActivators)
        //     {
        //         Chunk chunk = GetChunk(activator.transform);
        //         if (!_chunkActivatorsChunks.ContainsKey(chunk.ChunkIndex))
        //         {
        //             _chunkActivatorsChunks.Add(chunk.ChunkIndex, new List<RayfireChunkActivator>());
        //         }

        //         _chunkActivatorsChunks[chunk.ChunkIndex].Add(activator);

        //         await Task.Yield();
        //     }
        // }

        // [Button]
        // private void TEST()
        // {
        //     _ = FindAllChunkActivatorsAsync();
        // }

        private void Update()
        {
            if (!_enableChunks) return;

            if (SceneLoader.GetCurrentScene() == SCENE_NAME.Lobby) return;

            if (!SceneLoader.IsCurrentScenePlayable()) return;

            if (_dynamicActivation)
            {
                UpdateActiveChunks();
            }
            UpdateMovingGhostObjects();
        }

        /// <summary>
        /// Activates chunks within the active radius around the player's chunk and deactivates others.
        /// </summary>
        private void UpdateActiveChunks()
        {
            if (_playerTransform == null) return;
            Chunk playerChunk = GetChunk(_playerTransform);

            if (!_areaInitialized)
            {
                foreach (var kvp in _chunks)
                {
                    Chunk chunk = kvp.Value;
                    int dx = Mathf.Abs(chunk.ChunkIndex.x - playerChunk.ChunkIndex.x);
                    int dy = Mathf.Abs(chunk.ChunkIndex.y - playerChunk.ChunkIndex.y);
                    bool shouldBeActive = dx <= _activeRadiusInChunks && dy <= _activeRadiusInChunks;

                    if (chunk.IsActive != shouldBeActive)
                    {
                        chunk.IsActive = shouldBeActive;
                        if (shouldBeActive)
                        {
                            ActivateChunk(chunk);
                        }
                        else
                        {
                            DeactivateChunk(chunk);
                        }
                    }
                }

                _areaInitialized = true;
                return;
            }

            Vector2Int pos = playerChunk.ChunkIndex;

            // ==== How This works
            // When _activeRadiusInChunks is 1, the inner active area is 3x3.
            // We'll update a 5x5 area (the active area plus a 1-chunk border).
            // ==== Example

            int innerRange = _activeRadiusInChunks;
            int outerBorder = 1;
            int outerRange = innerRange + outerBorder;

            for (int x = pos.x - outerRange; x <= pos.x + outerRange; x++)
            {
                for (int y = pos.y - outerRange; y <= pos.y + outerRange; y++)
                {
                    Vector2Int index = GetChunkIndex(x, y);
                    if (!_chunks.TryGetValue(index, out Chunk chunk))
                    {
                        continue;
                    }

                    bool inActiveArea = x >= pos.x - innerRange && x <= pos.x + innerRange &&
                                        y >= pos.y - innerRange && y <= pos.y + innerRange;

                    if (inActiveArea)
                    {
                        if (!chunk.IsActive)
                        {
                            chunk.IsActive = true;
                            ActivateChunk(chunk);
                        }
                    }
                    else
                    {
                        if (chunk.IsActive)
                        {
                            chunk.IsActive = false;
                            DeactivateChunk(chunk);
                        }
                    }
                }
            }
        }

        public Chunk GetChunk(Transform transformObject)
        {
            Vector2Int index = GetChunkIndex(transformObject.position);

            if (!_chunks.TryGetValue(index, out Chunk chunk))
            {
                Debug.LogWarning("Object is outside the chunk grid: " + transformObject.name);
                return null;
            }
            return chunk;
        }

        //TODO pool
        private Vector2Int GetChunkIndex(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _chunkSize.x),
                Mathf.FloorToInt(position.z / _chunkSize.y));
        }

        //TODO pool
        private Vector2Int GetChunkIndex(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Enables all GameObjects registered to the chunk.
        /// </summary>
        private void ActivateChunk(Chunk chunk)
        {
            // if (_chunkActivatorsChunks.TryGetValue(chunk.ChunkIndex, out List<RayfireChunkActivator> activators))
            // {
            //     Debug.Log($"Activating {activators.Count} activators in chunk {chunk.ChunkIndex}");
            //     foreach (var activator in activators)
            //     {
            //         activator.gameObject.SetActive(true);
            //         activator.Activate();
            //     }
            // }

            Vector2Int index = chunk.ChunkIndex;
            if (_spawnablesInChunk.TryGetValue(index, out List<ChunkItem> chunkItems))
            {
                foreach (ChunkItem item in chunkItems)
                {
                    IWidth width = item.GameObject.GetComponent<IWidth>();
                    if (width == null)
                    {
                        if (_debugChunks) Debug.LogWarning($"ChunkItem {item.GameObject.name} does not implement IWidth. Skipping spawn.");
                        continue;
                    }
                    List<Vector3> grid = GridSpawnHelper.GenerateGrid(chunk.Vertices, width.Width);

                    Vector3 availablePosition;
                    if (!GridSpawnHelper.TryGetRandomAvailablePosition(chunk, width.Width, _occupiedLayers, out availablePosition, grid))
                    {
                        if (_debugChunks) Debug.LogWarning($"No available position for {item.GameObject.name} in chunk {index}. Skipping spawn.");
                        continue;
                    }

                    availablePosition.y = item.GameObject.transform.position.y;
                    GameObject go = Instantiate(item.GameObject, availablePosition, Quaternion.identity);

                    if(go.GetComponent<ChunkItemMono>() == null) chunk.RegisterObject(go);

                    if(_debugChunks) Debug.Log($"Activating {item.GameObject.name} in chunk {index}");
                }

                RemoveRandomSpawnablesFromChunk(index);
            }

            foreach (ChunkItem obj in chunk.Objects)
            {
                if (obj.GameObject != null && !obj.GameObject.activeSelf)
                {
                    obj.GameObject.SetActive(true);
                }
            }

            chunk.MovingGhostObjects.Clear();

            if (_areaInitialized)
                Events.OnChunkLoad.Invoke(chunk);

        }

        /// <summary>
        /// Disables all GameObjects in the chunk and creates moving ghost data for those with an IMovement component.
        /// </summary>
        private void DeactivateChunk(Chunk chunk)
        {
            foreach (ChunkItem obj in chunk.Objects)
            {
                GameObject go = obj.GameObject;
                if (go != null && go.activeSelf)
                {
                    IMovement movement = go.GetComponent<IMovement>();
                    if (movement != null)
                    {
                        MovingGhostObjectData ghost = new MovingGhostObjectData
                        {
                            Position = go.transform.position,
                            Speed = movement.GetSpeed(),
                            OriginalObjectInstanceID = go.GetInstanceID()
                        };
                        _movingGhostObjectDataList.Add(ghost);
                    }
                    go.SetActive(false);
                }
            }

            if (_areaInitialized)
                Events.OnChunkUnload.Invoke(chunk);
        }

        /// <summary>
        /// Updates moving ghost object positions using a Burst–compiled job.
        /// These objects move toward the player based on their defined speed.
        /// </summary>
        private void UpdateMovingGhostObjects()
        {
            if (_movingGhostObjectDataList.Count == 0) return;
            if (_movingGhostObjectDataNative.IsCreated)
                _movingGhostObjectDataNative.Dispose();
            _movingGhostObjectDataNative = new NativeArray<MovingGhostObjectData>(_movingGhostObjectDataList.ToArray(), Allocator.TempJob);

            MovingGhostObjectJob job = new MovingGhostObjectJob
            {
                GhostData = _movingGhostObjectDataNative,
                DeltaTime = Time.deltaTime,
                PlayerPosition = _playerTransform != null ? (Vector3)_playerTransform.position : float3.zero
            };

            JobHandle handle = job.Schedule(_movingGhostObjectDataNative.Length, 64);
            handle.Complete();

            for (int i = 0; i < _movingGhostObjectDataNative.Length; i++)
            {
                _movingGhostObjectDataList[i] = _movingGhostObjectDataNative[i];
                // Optionally, check if the ghost object has entered an active chunk and re-enable its GameObject.
            }
            _movingGhostObjectDataNative.Dispose();
        }

        /// <summary>
        /// Checks if a given Transform is within the specified Chunk.
        /// </summary>
        public bool IsInChunk(Transform t, Chunk chunk)
        {
            return chunk.IsInChunk(t.position);
        }

        /// <summary>
        /// Registers a GameObject with the chunk corresponding to its world position.
        /// Returns if chunk is active.
        /// </summary>
        public Chunk RegisterObject(GameObject obj)
        {
            //Vector3 pos = obj.transform.position;
            // int chunkX = Mathf.FloorToInt(pos.x / _chunkSize.x);
            // int chunkY = Mathf.FloorToInt(pos.z / _chunkSize.y);
            Vector2Int index = GetChunkIndex(obj.transform.position);
            if (!_chunks.TryGetValue(index, out Chunk chunk))
            {
                Debug.LogWarning("Object is outside the chunk grid: " + obj.name);
                return null;
            }
            chunk.RegisterObject(obj);
            return chunk;
        }

        public Chunk RegisterObject(ChunkItem chunkItem)
        {
            Vector2Int index = GetChunkIndex(chunkItem.GameObject.transform.position);
            if (!_chunks.TryGetValue(index, out Chunk chunk))
            {
                Debug.LogWarning("Object is outside the chunk grid: " + chunkItem.GameObject.name);
                return null;
            }
            chunk.RegisterObject(chunkItem);
            return chunk;
        }

        public void UnregisterObject(GameObject obj)
        {
            Vector2Int index = GetChunkIndex(obj.transform.position);
            if (!_chunks.TryGetValue(index, out Chunk chunk))
            {
                Debug.LogWarning("Object is outside the chunk grid: " + obj.name);
                return;
            }
            chunk.UnregisterObject(obj);
        }

        public void UnregisterObject(ChunkItem chunkItem)
        {
            Vector2Int index = GetChunkIndex(chunkItem.GameObject.transform.position);
            if (!_chunks.TryGetValue(index, out Chunk chunk))
            {
                Debug.LogWarning("Object is outside the chunk grid: " + chunkItem.GameObject.name);
                return;
            }
            chunk.UnregisterObject(chunkItem);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_debugChunks || _chunks == null) return;
            foreach (var kvp in _chunks)
            {
                Chunk chunk = kvp.Value;
                Gizmos.color = chunk.IsActive ? Color.cyan : Color.red;
                Vector2 center2D = chunk.Area.center;
                Vector3 center3D = new Vector3(center2D.x, 0, center2D.y);
                Vector3 size3D = new Vector3(chunk.Area.width, 0.1f, chunk.Area.height);
                Gizmos.DrawWireCube(center3D, size3D);
            }
        }

        #region Movement Bounds
        private int _cachedBorderChunks = -1;
        private int _cachedGridSize = -1;
        private Vector2 _cachedChunkSize;
        private Vector3 _cachedLowerLeft;
        private Vector3 _cachedUpperRight;
        /// <summary>
        /// Computes (and caches) the world‐space bounds inset from the full grid by <paramref name="borderChunks"/> chunks.
        /// Subsequent calls with the same parameters return the cached corners.
        /// </summary>
        public void GetMovementBounds(int borderChunks, out Vector3 lowerLeft, out Vector3 upperRight)
        {
            if (borderChunks == _cachedBorderChunks
                && _gridSize == _cachedGridSize
                && _chunkSize == _cachedChunkSize)
            {
                lowerLeft = _cachedLowerLeft;
                upperRight = _cachedUpperRight;
                return;
            }

            _cachedBorderChunks = borderChunks;
            _cachedGridSize = _gridSize;
            _cachedChunkSize = _chunkSize;

            int halfGrid = _gridSize / 2;
            int minIndex = -halfGrid + borderChunks;
            int maxIndex = (_gridSize % 2 == 0 ? halfGrid - 1 : halfGrid) - borderChunks;

            float minX = minIndex * _chunkSize.x;
            float minZ = minIndex * _chunkSize.y;
            float maxX = (maxIndex + 1) * _chunkSize.x;
            float maxZ = (maxIndex + 1) * _chunkSize.y;

            _cachedLowerLeft = new Vector3(minX, 0f, minZ);
            _cachedUpperRight = new Vector3(maxX, 0f, maxZ);

            lowerLeft = _cachedLowerLeft;
            upperRight = _cachedUpperRight;
        }

        [Button]
        public void TEST_GETMOVEMENT_BOUNDS()
        {
            GetMovementBounds(1, out Vector3 lowerLeft, out Vector3 upperRight);
            Debug.Log($"Lower Left: {lowerLeft}, Upper Right: {upperRight}");

            MarkHelper.DrawSphereTimed(lowerLeft, 10f, 10f, Color.red);
            MarkHelper.DrawSphereTimed(upperRight, 10f, 10f, Color.green);
        }
        #endregion
    }
}
