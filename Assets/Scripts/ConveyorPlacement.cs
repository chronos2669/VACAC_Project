using System.Collections.Generic;
using UnityEngine;

public struct ConveyorPlacement {
    public ConveyorDefinition definition;
    public Vector3Int origin;   // first (input) cell; origin.y is the level the belt starts on
    public GridDir dir;         // flow direction
    public int rise;            // 0 = flat, +1 = ascending incline, -1 = descending incline

    public int Length => definition.lengthCells;
    public int ExitLevel => origin.y + rise;

    // The cell the next piece must start in.
    public Vector3Int ExitCell {
        get {
            Vector3Int flat = origin + dir.ToOffset() * Length;
            return new Vector3Int(flat.x, ExitLevel, flat.z);
        }
    }

    // Every cell this piece fills. Inclines fill both levels they span,
    // so nothing can be built through the slope.
    public IEnumerable<Vector3Int> FootprintCells() {
        int low = Mathf.Min(origin.y, ExitLevel);
        int high = Mathf.Max(origin.y, ExitLevel);
        for (int i = 0; i < Length; i++) {
            Vector3Int flat = origin + dir.ToOffset() * i;
            for (int level = low; level <= high; level++) {
                yield return new Vector3Int(flat.x, level, flat.z);
            }
        }
    }

    // Where the prefab root goes. Descending inclines reuse the ascending model:
    // it is placed at the low (output) end and turned to face backwards.
    public Pose RootPose() {
        Vector3 step = dir.ToVector() * GridMath.CellSize;
        Vector3 originCentre = GridMath.CellToWorld(origin);
        if (rise >= 0) {
            return new Pose(originCentre - step * 0.5f, dir.ToRotation());
        }
        Vector3 lowEnd = originCentre + step * (Length - 0.5f);
        lowEnd.y = ExitLevel * GridMath.LevelHeight;
        return new Pose(lowEnd, dir.Opposite().ToRotation());
    }

    // A point on the belt surface, measured in cells from the input edge (0) to the output edge (Length).
    public Vector3 SurfacePoint(float distanceCells) {
        Vector3 forward = dir.ToVector() * GridMath.CellSize;
        Vector3 point = GridMath.CellToWorld(origin) - forward * 0.5f + forward * distanceCells;
        float t = distanceCells / Length;
        point.y = GridMath.SurfaceY(origin.y + rise * t);
        return point;
    }

    // Adds this piece's part of the item path: each cell centre, then the output edge.
    public void AppendWaypoints(List<Vector3> path) {
        for (int i = 0; i < Length; i++) {
            path.Add(SurfacePoint(i + 0.5f));
        }
        path.Add(SurfacePoint(Length));
    }
}