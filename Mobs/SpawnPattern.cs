using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using Game.PathSystem;

[CreateAssetMenu(menuName = "Patterns/SpawnPattern", fileName = "New SpawnPattern")]
public class SpawnPattern : SerializedScriptableObject
{
    public enum MiddleOption
    {
        Transform,
        Player,
        Position
    }

    public enum CellType
    {
        Empty,
        Spawn,
        DirStart,
        DirEnd,
        DirMid
    }

    public enum SPAWN_LOGIC
    {
        RANDOM_CELL,
        EXACT_CELLS
    }

    public enum SPAWN_TYPE
    {
        NONE,
        GROUPING
    }

    [LabelText("Middle Type"), PropertyOrder(-20)]
    public MiddleOption Middle = MiddleOption.Transform;

    [ShowIf(nameof(Middle), MiddleOption.Transform), Indent]
    public Transform CustomMiddleTransform;

    [ShowIf(nameof(Middle), MiddleOption.Position), Indent]
    public Vector3 CustomMiddlePosition;

    [Title("Cell Settings")]
    [PropertyTooltip("Size of each cell in world-units.")]
    public Vector2 CellSize = Vector2.one;

    [EnumToggleButtons]
    public SPAWN_LOGIC SpawnLogic = SPAWN_LOGIC.RANDOM_CELL;

    public bool _spawnAroundCell = false;
    [ShowIf(nameof(_spawnAroundCell)), Indent]
    [PropertyTooltip("Radius for RANDOM_AROUND_CELL logic.")]
    public float AroundCellRadius = 5f;

    [EnumToggleButtons]
    public SPAWN_TYPE SpawnType = SPAWN_TYPE.NONE;

    [ShowIf(nameof(SpawnType), SPAWN_TYPE.GROUPING), Indent]
    public int GroupingCount = 1;

    [Title("Center Cell")]
    [PropertyTooltip("Color of the true center cell.")]
    public Color CenterCellColor = new Color(0.5f, 0f, 0.5f, 0.5f);

    [TableMatrix(
        HorizontalTitle = "Spawn / Direction Grid",
        DrawElementMethod = nameof(DrawCell),
        ResizableColumns = false,
        RowHeight = 16)]
    public CellType[,] PatternGrid;

    private static readonly Color _spawnColor = new Color(0.1f, 0.8f, 0.2f, 0.5f);
    private static readonly Color _dirStartColor = Color.yellow * 0.5f;
    private static readonly Color _dirEndColor = Color.cyan * 0.5f;
    private static readonly Color _dirMidColor = Color.blue * 0.5f;
    private static readonly Color _emptyColor = new Color(0, 0, 0, 0.3f);

    private void OnEnable()
    {
        if (PatternGrid == null || PatternGrid.GetLength(0) != 15 || PatternGrid.GetLength(1) != 15)
        {
            PatternGrid = new CellType[15, 15];
        }
    }

    private CellType DrawCell(Rect rect, CellType value, int row, int column)
    {
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            bool shift = Event.current.shift;
            bool alt = Event.current.alt;
            bool left = Event.current.button == 0;
            bool right = Event.current.button == 1;

            if (shift && left)
                value = CellType.DirStart;
            else if (shift && right)
                value = CellType.DirEnd;
            else if (alt && left)
                value = CellType.DirMid;
            else if (left && !shift && !alt)
                value = CellType.Spawn;
            else
                value = CellType.Empty;

            GUI.changed = true;
            Event.current.Use();
        }

        int rows = PatternGrid.GetLength(0);
        int cols = PatternGrid.GetLength(1);
        bool isCenter = (row == rows / 2 && column == cols / 2);

        Color drawCol = isCenter ? CenterCellColor
                      : value == CellType.Spawn ? _spawnColor
                      : value == CellType.DirStart ? _dirStartColor
                      : value == CellType.DirEnd ? _dirEndColor
                      : value == CellType.DirMid ? _dirMidColor
                                                   : _emptyColor;

#if UNITY_EDITOR
        EditorGUI.DrawRect(rect.Padding(1), drawCol);
#endif

        return value;
    }

    public List<Vector3> GetSpawnPositions()
    {
        // Determine center point
        Vector3 center;
        switch (Middle)
        {
            case MiddleOption.Transform:
                center = CustomMiddleTransform != null ? CustomMiddleTransform.position : Vector3.zero;
                break;
            case MiddleOption.Player:
                center = Player.Instance != null ? Player.Instance.transform.position : Vector3.zero;
                break;
            case MiddleOption.Position:
                center = CustomMiddlePosition;
                break;
            default:
                center = Vector3.zero;
                break;
        }
        center.y = 0f;

        int rows = PatternGrid.GetLength(0);
        int cols = PatternGrid.GetLength(1);
        var offsets = new List<Vector3>();

        float centerRow = (rows - 1) * 0.5f;
        float centerCol = (cols - 1) * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (PatternGrid[r, c] == CellType.Spawn)
                {
                    float localX = (r - centerRow) * CellSize.x;
                    float localZ = (c - centerCol) * CellSize.y;

                    localZ = -localZ;

                    offsets.Add(new Vector3(localX, 0f, localZ));
                    Debug.Log($"Spawn @[{r},{c}] → offset ({localX:F2},0,{localZ:F2})");
                }
            }
        }

        var positions = offsets.Select(o => center + o).ToList();

        // Apply spawn logic
        switch (SpawnLogic)
        {
            case SPAWN_LOGIC.RANDOM_CELL:
                if (positions.Count > 0)
                    positions = new List<Vector3> { positions[Random.Range(0, positions.Count)] };
                break;
            // case SPAWN_LOGIC.RANDOM_AROUND_CELL:
            //     if (positions.Count > 0)
            //     {
            //         var randomCell = positions[Random.Range(0, positions.Count)];
            //         var randomCircle = Random.insideUnitCircle * AroundCellRadius;
            //         var randomPos = new Vector3(randomCell.x + randomCircle.x, 0f, randomCell.z + randomCircle.y);
            //         positions = new List<Vector3> { randomPos };
            //     }
            //     break;
            // case SPAWN_LOGIC.RANDOM_CELL_NOT_SAME:
            //     for (int i = positions.Count - 1; i > 0; i--)
            //     {
            //         int j = Random.Range(0, i + 1);
            //         var tmp = positions[i]; positions[i] = positions[j]; positions[j] = tmp;
            //     }
            //     break;
            case SPAWN_LOGIC.EXACT_CELLS:
                break;
        }

        if(_spawnAroundCell)
        {
            positions = positions.Select(p => p + Random.insideUnitSphere * AroundCellRadius).ToList();
        }

        // Apply viewport adjustment: scale pattern so its closest spawn cell lies on the viewport boundary
        // if (SpawnAdjustment == SPAWN_ADJUSTMENT.OUTSIDE_VIEW_PORT && positions.Count > 0)
        // {
        //     // Compute radial vectors from center to each position
        //     var radialVectors = positions.Select(p => p - center).ToList();
        //     // Find the minimal radial distance (closest to center)
        //     float minRadialDist = radialVectors.Min(v => v.magnitude);
        //     if (minRadialDist > 0f)
        //     {
        //         // Compute scale factor so the closest distance reaches ViewportRadius
        //         float scaleFactor = ViewportRadius / minRadialDist;
        //         // Scale each position outward preserving layout
        //         positions = radialVectors
        //             .Select(rv => center + rv * scaleFactor)
        //             .ToList();
        //     }
        // }

        // Apply grouping
        if (SpawnType == SPAWN_TYPE.GROUPING && GroupingCount > 0 && positions.Count > GroupingCount)
        {
            positions = positions.Take(GroupingCount).ToList();
        }

        return positions;
    }




    [Button("TEST", ButtonSizes.Large)]
    public void TEST()
    {
        var positions = GetSpawnPositions();
        foreach (var pos in positions)
        {
            Debug.Log($"Spawn Position: {pos}");
            MarkHelper.DrawSphereTimed(pos, 0.4f, 2, Color.red);
        }
    }

    [Button("TEST PATH", ButtonSizes.Large)]
    public void TEST_PATH()
    {
        var gen = new PathGenerator(GetSpawnPositions(), 100, true);
        gen.Generate();
        gen.DrawDebugLines(5f);

        var points = gen.GetAllPoints();
        for (int i = 0; i < points.Count - 1; i++)
        {
            MarkHelper.DrawSphereTimed(points[i], 0.1f, 2, Color.red);
        }
    }
}
