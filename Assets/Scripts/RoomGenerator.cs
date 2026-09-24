using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RoomGenerator : MonoBehaviour {
    [Header("References")]
    [SerializeField] GridPoint startPoint;
    [SerializeField] GridPoint endPoint;
    [SerializeField] Material floorMaterial;
    [SerializeField] Material wallMaterial;
    [SerializeField] Material obstacleMaterial;

    [Header("Room")]
    [SerializeField] float wallThickness = 0.2f;

    [Header("Obstacles")]
    [SerializeField, Min(0)] int obstacleCount = 30;
    [Tooltip("Cube edge in cells. 1 can be bridged at level 1, 2 at level 2, 3 must be routed around.")]
    [SerializeField] Vector2Int cubeSizeRange = new Vector2Int(1, 2);
    [Tooltip("Cells around the start, the start's output and the end that stay clear.")]
    [SerializeField, Min(0)] int clearRadius = 1;
    [Tooltip("0 = a different layout every run.")]
    [SerializeField] int seed = 0;
    [SerializeField, Min(1)] int maxLayoutAttempts = 25;
    [Tooltip("Lengths of the flat pieces, used to prove each layout is solvable.")]
    [SerializeField] int[] routeCheckLengths = { 3, 2 };

    struct Obstacle {
        public Vector2Int corner;   // lowest (x, z) cell
        public int size;            // edge length in cells
    }

    Transform roomRoot;
    Transform obstacleRoot;

    public void Generate() {
        GridOccupancy grid = GridOccupancy.Instance;
        int size = grid.RoomSize;
        if (!InRoom(startPoint.ExitCell2D, size)) {
            Debug.LogError("The start point faces out of the room.");
        }
        BuildShell(size);

        System.Random rng = seed == 0 ? new System.Random() : new System.Random(seed);
        HashSet<Vector2Int> reserved = ReservedCells();

        List<Obstacle> layout = null;
        for (int attempt = 0; attempt < maxLayoutAttempts && layout == null; attempt++) {
            List<Obstacle> candidate = PlanObstacles(size, rng, reserved, out HashSet<Vector2Int> blocked);
            if (GroundRouteExists(size, blocked)) {
                layout = candidate;
            }
        }
        if (layout == null) {
            Debug.LogWarning("No solvable obstacle layout found; using an empty room.");
            layout = new List<Obstacle>();
        }

        grid.Clear();
        grid.Occupy(startPoint.Cell, startPoint);
        grid.Occupy(endPoint.Cell, endPoint);
        SpawnObstacles(grid, layout);
    }

    // ---------- Planning ----------

    HashSet<Vector2Int> ReservedCells() {
        HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
        AddSquare(reserved, startPoint.Cell2D, clearRadius);
        AddSquare(reserved, startPoint.ExitCell2D, clearRadius);
        AddSquare(reserved, endPoint.Cell2D, clearRadius);
        return reserved;
    }

    static void AddSquare(HashSet<Vector2Int> set, Vector2Int centre, int radius) {
        for (int dx = -radius; dx <= radius; dx++) {
            for (int dz = -radius; dz <= radius; dz++) {
                set.Add(centre + new Vector2Int(dx, dz));
            }
        }
    }

    List<Obstacle> PlanObstacles(int size, System.Random rng, HashSet<Vector2Int> reserved,
                                 out HashSet<Vector2Int> blocked) {
        List<Obstacle> obstacles = new List<Obstacle>();
        blocked = new HashSet<Vector2Int> { startPoint.Cell2D, endPoint.Cell2D };

        for (int i = 0; i < obstacleCount; i++) {
            for (int tries = 0; tries < 20; tries++) {
                int edge = rng.Next(cubeSizeRange.x, cubeSizeRange.y + 1);
                Vector2Int corner = new Vector2Int(rng.Next(0, size - edge + 1), rng.Next(0, size - edge + 1));
                if (Overlaps(corner, edge, reserved, blocked)) {
                    continue;
                }
                obstacles.Add(new Obstacle { corner = corner, size = edge });
                for (int x = 0; x < edge; x++) {
                    for (int z = 0; z < edge; z++) {
                        blocked.Add(corner + new Vector2Int(x, z));
                    }
                }
                break;
            }
        }
        return obstacles;
    }

    static bool Overlaps(Vector2Int corner, int edge, HashSet<Vector2Int> reserved, HashSet<Vector2Int> blocked) {
        for (int x = 0; x < edge; x++) {
            for (int z = 0; z < edge; z++) {
                Vector2Int cell = corner + new Vector2Int(x, z);
                if (reserved.Contains(cell) || blocked.Contains(cell)) {
                    return true;
                }
            }
        }
        return false;
    }

    // Breadth-first search over (open cell, incoming direction) states, placing only flat
    // straight pieces on the floor with the same rules as ConveyorBuilder.
    bool GroundRouteExists(int size, HashSet<Vector2Int> blocked) {
        Vector2Int end = endPoint.Cell2D;
        (Vector2Int cell, GridDir dir) first = (startPoint.ExitCell2D, startPoint.Facing);
        HashSet<(Vector2Int, GridDir)> visited = new HashSet<(Vector2Int, GridDir)> { first };
        Queue<(Vector2Int cell, GridDir dir)> queue = new Queue<(Vector2Int cell, GridDir dir)>();
        queue.Enqueue(first);

        while (queue.Count > 0) {
            (Vector2Int cell, GridDir incoming) = queue.Dequeue();
            for (int d = 0; d < 4; d++) {
                GridDir dir = (GridDir)d;
                if (dir == incoming.Opposite()) {
                    continue;
                }
                Vector3Int offset = dir.ToOffset();
                Vector2Int step = new Vector2Int(offset.x, offset.z);
                foreach (int length in routeCheckLengths) {
                    if (!RunIsClear(cell, step, length, size, blocked)) {
                        continue;
                    }
                    Vector2Int exit = cell + step * length;
                    if (exit == end) {
                        return true;
                    }
                    if (!InRoom(exit, size) || blocked.Contains(exit)) {
                        continue;
                    }
                    if (visited.Add((exit, dir))) {
                        queue.Enqueue((exit, dir));
                    }
                }
            }
        }
        return false;
    }

    static bool RunIsClear(Vector2Int from, Vector2Int step, int length, int size, HashSet<Vector2Int> blocked) {
        for (int i = 0; i < length; i++) {
            Vector2Int cell = from + step * i;
            if (!InRoom(cell, size) || blocked.Contains(cell)) {
                return false;
            }
        }
        return true;
    }

    static bool InRoom(Vector2Int cell, int size) {
        return cell.x >= 0 && cell.y >= 0 && cell.x < size && cell.y < size;
    }

    // ---------- Building ----------

    void BuildShell(int size) {
        if (roomRoot != null) {
            Destroy(roomRoot.gameObject);
        }
        roomRoot = new GameObject("Room").transform;
        roomRoot.SetParent(transform, false);

        float span = size * GridMath.CellSize;   // cubic: width = depth = height
        float half = span * 0.5f;
        float t = wallThickness;

        CreateBlock("Floor", new Vector3(half, -t * 0.5f, half), new Vector3(span, t, span), floorMaterial, roomRoot, true);
        CreateBlock("Ceiling", new Vector3(half, span + t * 0.5f, half), new Vector3(span, t, span), wallMaterial, roomRoot, false);
        CreateBlock("Wall North", new Vector3(half, half, span + t * 0.5f), new Vector3(span + 2f * t, span, t), wallMaterial, roomRoot, false);
        CreateBlock("Wall South", new Vector3(half, half, -t * 0.5f), new Vector3(span + 2f * t, span, t), wallMaterial, roomRoot, false);
        CreateBlock("Wall East", new Vector3(span + t * 0.5f, half, half), new Vector3(t, span, span), wallMaterial, roomRoot, false);
        CreateBlock("Wall West", new Vector3(-t * 0.5f, half, half), new Vector3(t, span, span), wallMaterial, roomRoot, false);
    }

    void SpawnObstacles(GridOccupancy grid, List<Obstacle> layout) {
        if (obstacleRoot != null) {
            Destroy(obstacleRoot.gameObject);
        }
        obstacleRoot = new GameObject("Obstacles").transform;
        obstacleRoot.SetParent(transform, false);

        foreach (Obstacle o in layout) {
            float edge = o.size * GridMath.CellSize;
            Vector3 centre = new Vector3((o.corner.x + o.size * 0.5f) * GridMath.CellSize,
                                         edge * 0.5f,
                                         (o.corner.y + o.size * 0.5f) * GridMath.CellSize);
            CreateBlock("Obstacle", centre, Vector3.one * edge, obstacleMaterial, obstacleRoot, true);

            int levels = Mathf.CeilToInt(edge / GridMath.LevelHeight);
            for (int x = 0; x < o.size; x++) {
                for (int z = 0; z < o.size; z++) {
                    for (int level = 0; level < levels; level++) {
                        grid.Occupy(new Vector3Int(o.corner.x + x, level, o.corner.y + z), this);
                    }
                }
            }
        }
    }

    static void CreateBlock(string name, Vector3 centre, Vector3 scale, Material material,
                            Transform parent, bool castShadows) {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = centre;
        block.transform.localScale = scale;
        MeshRenderer mr = block.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }
}