using UnityEngine;

public enum GridDir { North = 0, East = 1, South = 2, West = 3 }

public static class GridDirExtensions {
    static readonly Vector3Int[] Offsets = {
        new Vector3Int(0, 0, 1),   // North
        new Vector3Int(1, 0, 0),   // East
        new Vector3Int(0, 0, -1),  // South
        new Vector3Int(-1, 0, 0)   // West
    };

    public static Vector3Int ToOffset(this GridDir dir) {
        return Offsets[(int)dir];
    }

    public static Vector3 ToVector(this GridDir dir) {
        return Offsets[(int)dir];
    }

    // Viewed from above, +90 degrees of yaw is clockwise (North -> East).
    public static GridDir RotateClockwise(this GridDir dir) {
        return (GridDir)(((int)dir + 1) % 4);
    }

    public static GridDir RotateAnticlockwise(this GridDir dir) {
        return (GridDir)(((int)dir + 3) % 4);
    }

    public static GridDir Opposite(this GridDir dir) {
        return (GridDir)(((int)dir + 2) % 4);
    }

    public static Quaternion ToRotation(this GridDir dir) {
        return Quaternion.Euler(0f, (int)dir * 90f, 0f);
    }
}