using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Interface defining a 2D spawn area.
/// Implement on your area (e.g. PoisonPoolMorph) to expose polygon bounds and elevation.
/// </summary>
public interface ISpawnArea
{
    /// <summary>
    /// Polygon vertices in X/Z plane defining the spawn area.
    /// </summary>
    Vector2[] Vertices { get; }

    /// <summary>
    /// Y coordinate at which objects should be placed.
    /// </summary>
    float Elevation { get; }
}

/// <summary>
/// Static utility for generating grid-based spawn positions and filtering available cells.
/// </summary>
public static class GridSpawnHelper
{
    /// <summary>
    /// Generates cell centers inside the given polygon based on cellSize.
    /// Returned as Vector3 with Y set to the provided elevation.
    /// </summary>
    public static List<Vector3> GenerateGrid(Vector2[] vertices, float cellSize, float elevation = 0f)
    {
        var points = new List<Vector3>();
        if (vertices == null || vertices.Length < 3) return points;

        float minX = vertices.Min(v => v.x);
        float maxX = vertices.Max(v => v.x);
        float minZ = vertices.Min(v => v.y);
        float maxZ = vertices.Max(v => v.y);

        int cols = Mathf.CeilToInt((maxX - minX) / cellSize);
        int rows = Mathf.CeilToInt((maxZ - minZ) / cellSize);

        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
            {
                var x = minX + (i + 0.5f) * cellSize;
                var z = minZ + (j + 0.5f) * cellSize;
                var pt2 = new Vector2(x, z);
                if (PointInPolygon(pt2, vertices))
                    points.Add(new Vector3(x, elevation, z));
            }
        return points;
    }

    /// <summary>
    /// Returns all unoccupied grid cell centers inside the area as Vector3.
    /// Optionally accepts a precomputed grid; otherwise generates one using area.Vertices and objectWidth.
    /// </summary>
    public static List<Vector3> GetAvailablePositions<TArea>(
        TArea area,
        float objectWidth,
        LayerMask occupancyMask,
        List<Vector3> precomputedGrid = null)
        where TArea : ISpawnArea
    {
        float cellSize = objectWidth;
        var grid = precomputedGrid ?? GenerateGrid(area.Vertices, cellSize, area.Elevation);
        var available = new List<Vector3>();
        foreach (var pos in grid)
        {
            var center = pos + Vector3.up * (cellSize * 0.5f);
            var halfExtents = Vector3.one * (cellSize * 0.5f);
            if (!Physics.CheckBox(center, halfExtents, Quaternion.identity, occupancyMask))
                available.Add(pos);
        }
        return available;
    }

    /// <summary>
    /// Tries to find a single random unoccupied position inside the area, minimizing physics checks.
    /// Returned as Vector3.
    /// </summary>
    public static bool TryGetRandomAvailablePosition<TArea>(
        TArea area,
        float objectWidth,
        LayerMask occupancyMask,
        out Vector3 availablePosition,
        List<Vector3> precomputedGrid = null)
        where TArea : ISpawnArea
    {
        float cellSize = objectWidth;
        var grid = precomputedGrid ?? GenerateGrid(area.Vertices, cellSize, area.Elevation);
        if (grid == null || grid.Count == 0)
        {
            availablePosition = default;
            return false;
        }

        var shuffled = grid.OrderBy(_ => UnityEngine.Random.value);
        foreach (var pos in shuffled)
        {
            var center = pos + Vector3.up * (cellSize * 0.5f);
            var halfExtents = Vector3.one * (cellSize * 0.5f);
            if (!Physics.CheckBox(center, halfExtents, Quaternion.identity, occupancyMask))
            {
                availablePosition = pos;
                return true;
            }
        }

        availablePosition = default;
        return false;
    }

    /// <summary>
    /// Standard point-in-polygon test on X/Z plane.
    /// </summary>
    private static bool PointInPolygon(Vector2 point, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; j = i++)
        {
            if (((poly[i].y > point.y) != (poly[j].y > point.y)) &&
                (point.x < (poly[j].x - poly[i].x) *
                 (point.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }
}
