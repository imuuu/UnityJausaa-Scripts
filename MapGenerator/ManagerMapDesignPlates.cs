using UnityEngine;
using System.Collections.Generic;
using Game.MapGenerator;
using Sirenix.OdinInspector;
using RayFire;
using Game.ChunkSystem;

[ExecuteInEditMode]
public class ManagerMapDesignPlates : MonoBehaviour
{
    private static ManagerMapDesignPlates _instance;
    public static ManagerMapDesignPlates Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ManagerMapDesignPlates>();
            }
            return _instance;
        }
        set => _instance = value;
    }
    public int MapWidth = 4;
    public int MapHeight = 3;
    public float PlateSize = 30f;

    [Tooltip("List of available map design plate prefabs.")]
    public List<GameObject> PlatePrefabs = new ();

    private List<MapDesignPlate> _plates = new ();

    private bool _hidePlates = false;


    [PropertySpace(10,10)]
    [Button("Generate Map", ButtonSizes.Large)]
    [GUIColor("#6f8ff7")]
    [BoxGroup("Buttons")]
    public void GenerateMap()
    {
        ClearMap();
        _plates.Clear();
        bool success = FillCell(0);
        if (!success)
        {
            Debug.LogError("Could not complete a valid map configuration.");
        }

        TriggerSpawnItemsOnAllPlates();
    }

    private void Start()
    {
        foreach (Transform child in this.transform)
        {
            Chunk chunk = ManagerChunks.Instance.RegisterObject(child.gameObject);
            child.gameObject.SetActive(chunk.IsActive);
        }
    }

    private void OnDisable()
    {
        if(_hidePlates)
        {
            HideAllPlates();
        }
    }

    [Button("Hide All Plates (Optimized)", ButtonSizes.Large)]
    [GUIColor("#3fbcf3")]
    [BoxGroup("Buttons")]
    public void HideAllPlates()
    {
        _hidePlates = false;
        foreach(Transform child in this.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    [Button("Show All Plates", ButtonSizes.Large)]
    [GUIColor("#f0a567")]
    [BoxGroup("Buttons")]
    public void ShowAllPlates()
    {
        _hidePlates = true;
        foreach(Transform child in this.transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    // [Button("Hide Chunk Activators", ButtonSizes.Medium)]
    // public void HideChunkActivators()
    // {
    //     foreach(Transform child in this.transform)
    //     {
    //         RayfireRigid[] rayfireRigids = child.GetComponentsInChildren<RayfireRigid>();
    //         foreach (RayfireRigid rigid in rayfireRigids)
    //         {
    //             rigid.gameObject.SetActive(false);
    //         }
    //     }
    // }

    // [Button("Show Chunk Activators", ButtonSizes.Medium)]
    // public void ShowChunkActivators()
    // {
    //     foreach(Transform child in this.transform)
    //     {
    //         RayfireRigid[] rayfireRigids = child.GetComponentsInChildren<RayfireRigid>();
    //         foreach (RayfireRigid rigid in rayfireRigids)
    //         {
    //             rigid.gameObject.SetActive(true);
    //         }
    //     }
    // }

    /// <summary>
    /// Recursively fills the cell at the given index (0 .. mapWidth*mapHeight-1).
    /// </summary>
    /// <param name="cellIndex">Linear index for the grid cell.</param>
    /// <returns>True if the grid from this cell onward can be filled; otherwise, false.</returns>
    private bool FillCell(int cellIndex, bool onlyThisIndex = false)
    {
        if (cellIndex >= MapWidth * MapHeight)
            return true;

        int x = cellIndex % MapWidth;
        int y = cellIndex / MapWidth;
        Vector3 pos = ComputeCellPosition(x, y);

        // Get a shuffled copy of the candidate pool.
        List<GameObject> candidatePool = new List<GameObject>(PlatePrefabs);
        Shuffle(candidatePool);

        foreach (GameObject candidatePrefab in candidatePool)
        {
            GameObject candidateInstance
            = SpawnPlate(candidatePrefab, pos, cellIndex, out MapDesignPlate candidatePlate);
            if (candidatePlate == null)
            {
                DestroyImmediate(candidateInstance);
                continue;
            }

            if (!DoesCandidatePlacementMatch(candidatePlate, x, y))
            {
                DestroyImmediate(candidateInstance);
                continue;
            }

            int rotationsToTest = candidatePlate.IsRotatable ? 4 : 1;

            bool candidateFits = false;
            for (int r = 0; r < rotationsToTest; r++)
            {
                if (DoesCandidateFitAt(candidatePlate, x, y))
                {
                    candidateFits = true;
                    break; // Candidate fits in this orientation.
                }
                if (candidatePlate.IsRotatable)
                    candidatePlate.RotatePlate(1);
            }

            if (candidateFits)
            {
                _plates.Add(candidatePlate);
                if (onlyThisIndex)
                {
                    return true;
                }
                else if (FillCell(cellIndex + 1))
                {
                    return true;
                }
                // Backtrack if subsequent cells could not be filled.
                _plates.RemoveAt(_plates.Count - 1);
                DestroyImmediate(candidateInstance);
            }
            else
            {
                DestroyImmediate(candidateInstance);
            }
        }
        // No candidate from the pool worked for this cell. Backtrack.
        return false;
    }

    /// <summary>
    /// Recreates only the plate at the given cell index (without affecting other cells).
    /// This version checks all four neighbors.
    /// </summary>
    [Button("Recreate Plate at Index", ButtonSizes.Medium)]
    [BoxGroup("Buttons")]
    [PropertySpace(10,10)]
    [PropertyOrder(1)]
    public bool ReCreateOnlyPlateAtIndex(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= _plates.Count)
        {
            Debug.LogError($"Invalid cell index: {cellIndex}");
            return false;
        }

        int x = cellIndex % MapWidth;
        int y = cellIndex / MapWidth;
        Vector3 pos = ComputeCellPosition(x, y);

        MapDesignPlate oldPlate = _plates[cellIndex];
        MapDesignPlate newPlate = null;

        List<GameObject> candidatePool = new List<GameObject>(PlatePrefabs);
        Shuffle(candidatePool);

        foreach (GameObject candidatePrefab in candidatePool)
        {
            GameObject candidateInstance 
            = SpawnPlate(candidatePrefab, pos, cellIndex, out MapDesignPlate candidatePlate);

            if (candidatePlate == null)
            {
                DestroyImmediate(candidateInstance);
                continue;
            }

            if (!DoesCandidatePlacementMatch(candidatePlate, x, y))
            {
                DestroyImmediate(candidateInstance);
                continue;
            }

            int rotationsToTest = candidatePlate.IsRotatable ? 4 : 1;
            bool candidateFits = false;
            for (int r = 0; r < rotationsToTest; r++)
            {
                if (DoesCandidateFitAtFull(candidatePlate, x, y))
                {
                    candidateFits = true;
                    break;
                }
                if (candidatePlate.IsRotatable)
                    candidatePlate.RotatePlate(1);
            }

            if (candidateFits)
            {
                newPlate = candidatePlate;
                break;
            }
            else
            {
                DestroyImmediate(candidateInstance);
            }
        }

        if (newPlate == null)
        {
            Debug.LogError($"Could not recreate plate at cell index {cellIndex}.");
            return false;
        }

        newPlate.TriggerSpawnItems();
        _plates[cellIndex] = newPlate;
        if (oldPlate != null)
            DestroyImmediate(oldPlate.gameObject);
        
        Debug.Log($"Recreated plate at cell index {cellIndex}. Success!");
        return true;
    }

    private GameObject SpawnPlate(GameObject platePrefab, Vector3 position, int cellIndex, out MapDesignPlate plate)
    {
        GameObject plateInstance = Instantiate(platePrefab, position, Quaternion.identity, transform);
        plateInstance.SetActive(true);
        plate = plateInstance.GetComponent<MapDesignPlate>();
        plate.SetCellLinearIndex(cellIndex);
        plate.SetManager(this);

// #if RAYFIRE

//         if(!_applyRayfireChunkActivator) return plateInstance;

//         RayfireRigid[] rayfireRigids = plateInstance.GetComponentsInChildren<RayfireRigid>();
//         Debug.Log($"Found {rayfireRigids.Length} RayfireRigids in {plateInstance.name}");
//         foreach (RayfireRigid rigid in rayfireRigids)
//         {
//             Transform target = rigid.transform.parent;

//             if(target.GetComponent<RayfireChunkActivator>() == null)
//             {
//                 target.gameObject.AddComponent<RayfireChunkActivator>();
//             }

//         }
// #endif
        return plateInstance;
    }

    /// <summary>
    /// Checks candidate plate at (x,y) against all four neighbors.
    /// </summary>
    private bool DoesCandidateFitAtFull(MapDesignPlate candidate, int x, int y)
    {
        DesignPlateConnections connections = candidate.GetConnections();

        // Check boundaries.
        if (x == 0 && !IsSideClosed(connections, Side.Left)) return false;
        if (x == MapWidth - 1 && !IsSideClosed(connections, Side.Right)) return false;
        if (y == 0 && !IsSideClosed(connections, Side.Bottom)) return false;
        if (y == MapHeight - 1 && !IsSideClosed(connections, Side.Top)) return false;

        // Check left neighbor.
        if (x > 0)
        {
            MapDesignPlate leftPlate = _plates[y * MapWidth + (x - 1)];
            if (leftPlate != null && !connections.IsCompatibleWith(leftPlate.GetConnections(), Side.Left))
                return false;
        }
        // Check right neighbor.
        if (x < MapWidth - 1)
        {
            MapDesignPlate rightPlate = _plates[y * MapWidth + (x + 1)];
            if (rightPlate != null && !connections.IsCompatibleWith(rightPlate.GetConnections(), Side.Right))
                return false;
        }
        // Check bottom neighbor.
        if (y > 0)
        {
            MapDesignPlate bottomPlate = _plates[(y - 1) * MapWidth + x];
            if (bottomPlate != null && !connections.IsCompatibleWith(bottomPlate.GetConnections(), Side.Bottom))
                return false;
        }
        // Check top neighbor.
        if (y < MapHeight - 1)
        {
            MapDesignPlate topPlate = _plates[(y + 1) * MapWidth + x];
            if (topPlate != null && !connections.IsCompatibleWith(topPlate.GetConnections(), Side.Top))
                return false;
        }
        return true;
    }

    private void TriggerSpawnItemsOnAllPlates()
    {
        foreach (var plate in _plates)
        {
            plate.TriggerSpawnItems();
        }
    }

    /// <summary>
    /// Computes the world position for a given cell (x,y), centering the grid at (0,0).
    /// </summary>
    private Vector3 ComputeCellPosition(int x, int y)
    {
        float totalWidth = MapWidth * PlateSize;
        float totalHeight = MapHeight * PlateSize;
        return new Vector3(
            x * PlateSize - (totalWidth * 0.5f) + (PlateSize * 0.5f),
            0f,
            y * PlateSize - (totalHeight * 0.5f) + (PlateSize * 0.5f)
        );
    }

    /// <summary>
    /// Checks whether a candidate plate fits at grid position (x,y) based on boundary and neighbor matching.
    /// </summary>
    private bool DoesCandidateFitAt(MapDesignPlate candidate, int x, int y)
    {
        DesignPlateConnections connections = candidate.GetConnections();

        // Boundary conditions: if this cell is at the border, the outward side must be closed.
        if (x == 0 && !IsSideClosed(connections, Side.Left)) return false;
        if (x == MapWidth - 1 && !IsSideClosed(connections, Side.Right)) return false;
        if (y == 0 && !IsSideClosed(connections, Side.Bottom)) return false;
        if (y == MapHeight - 1 && !IsSideClosed(connections, Side.Top)) return false;

        // Neighbor matching: check already placed neighbors (left and bottom).
        if (x > 0)
        {
            MapDesignPlate leftPlate = _plates[(y * MapWidth) + (x - 1)];
            if (!connections.IsCompatibleWith(leftPlate.GetConnections(), Side.Left))
                return false;
        }
        if (y > 0)
        {
            MapDesignPlate bottomPlate = _plates[((y - 1) * MapWidth) + x];
            if (!connections.IsCompatibleWith(bottomPlate.GetConnections(), Side.Bottom))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if every connection in the specified side is CLOSED.
    /// </summary>
    private bool IsSideClosed(DesignPlateConnections connections, Side side)
    {
        List<CONNECTION_TYPE> sideConnections = connections.GetConnections(side);
        foreach (var conn in sideConnections)
        {
            if (conn != CONNECTION_TYPE.CLOSE)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if the candidate plate's allowed placement type matches the type for cell (x,y).
    /// </summary>
    private bool DoesCandidatePlacementMatch(MapDesignPlate candidate, int x, int y)
    {
        PLACEMENT_TYPE allowed = candidate.AllowedPlacement;
        if (allowed == PLACEMENT_TYPE.ANY)
            return true;
        if (allowed == PLACEMENT_TYPE.CORNER_ONLY && IsCellCorner(x, y))
            return true;
        if (allowed == PLACEMENT_TYPE.SIDE_ONLY && IsCellSide(x, y))
            return true;
        if (allowed == PLACEMENT_TYPE.MIDDLE_ONLY && IsCellMiddle(x, y))
            return true;
        return false;
    }

    /// <summary>
    /// Returns true if the cell at (x,y) is a corner (both x and y are at borders).
    /// </summary>
    private bool IsCellCorner(int x, int y)
    {
        return (x == 0 || x == MapWidth - 1) && (y == 0 || y == MapHeight - 1);
    }

    /// <summary>
    /// Returns true if the cell at (x,y) is a side cell (on a border but not a corner).
    /// </summary>
    private bool IsCellSide(int x, int y)
    {
        return ((x == 0 || x == MapWidth - 1) || (y == 0 || y == MapHeight - 1)) && !IsCellCorner(x, y);
    }

    /// <summary>
    /// Returns true if the cell at (x,y) is in the interior.
    /// </summary>
    private bool IsCellMiddle(int x, int y)
    {
        return (x > 0 && x < MapWidth - 1 && y > 0 && y < MapHeight - 1);
    }

    /// <summary>
    /// Simple in-place shuffle of a list.
    /// </summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Recursively traverses the children of the given transform.
    /// If a child has a MapSpawnController, it is added to the controllers list and its children are skipped.
    /// Otherwise, if it has a MapSpawnItemBase, it is added to the spawnItems list.
    /// </summary>
    /// <param name="parent">The transform whose children are being processed.</param>
    public static void TraverseChildren(Transform parent, ref List<MapSpawnController> spawnControllers, ref List<MapSpawnItemBase> spawnItems)
    {
        foreach (Transform child in parent)
        {
            MapSpawnController controller = child.GetComponent<MapSpawnController>();
            if (controller != null)
            {
                spawnControllers.Add(controller);
                Debug.Log($"Found controller: {child.name}");
                continue;
            }

            MapSpawnItemBase spawnItem = child.GetComponent<MapSpawnItemBase>();
            if (spawnItem != null)
            {
                spawnItems.Add(spawnItem);
                Debug.Log($"Found spawn item: {child.name}");
            }

            TraverseChildren(child, ref spawnControllers, ref spawnItems);
        }
    }

    [Button("Clear Map Fully", ButtonSizes.Large)]
    [BoxGroup("Buttons")]
    [GUIColor("#cf1f39")]
    [PropertySpace(10,10)]
    public void ClearMap()
    {
        List<GameObject> children = new ();

        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        foreach (var child in children)
        {
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
        children.Clear();
        _plates.Clear();
    }
}
