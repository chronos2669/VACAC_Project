using UnityEngine;

public static class GridMath {
    public const float CellSize = 1f;
    public const float LevelHeight = 1f;
    public const float BeltSurfaceHeight = 0.25f;
    public const int MinLevel = 0;   // floor
    public const int MaxLevel = 2;   // two incline stages

    // Centre of a cell, at the base of its level.
    public static Vector3 CellToWorld(Vector3Int cell) {
        return new Vector3((cell.x + 0.5f) * CellSize, cell.y * LevelHeight, (cell.z + 0.5f) * CellSize);
    }

    public static Vector3Int WorldToCell(Vector3 world, int level) {
        return new Vector3Int(Mathf.FloorToInt(world.x / CellSize), level, Mathf.FloorToInt(world.z / CellSize));
    }

    // World height of the belt surface at a (possibly fractional) level.
    public static float SurfaceY(float level) {
        return level * LevelHeight + BeltSurfaceHeight;
    }
}