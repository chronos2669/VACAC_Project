using UnityEngine;

public class GridPoint : MonoBehaviour {
    [Tooltip("(x, z) cell on the floor.")]
    [SerializeField] Vector2Int cell = new Vector2Int(2, 2);
    [Tooltip("Start: the direction items leave in. End: ignored, it accepts from any side.")]
    [SerializeField] GridDir facing = GridDir.East;

    public GridDir Facing => facing;
    public Vector2Int Cell2D => cell;
    public Vector3Int Cell => new Vector3Int(cell.x, 0, cell.y);
    public Vector3Int ExitCell => Cell + facing.ToOffset();
    public Vector2Int ExitCell2D => new Vector2Int(ExitCell.x, ExitCell.z);

    public Vector3 SurfaceCentre {
        get {
            Vector3 point = GridMath.CellToWorld(Cell);
            point.y = GridMath.SurfaceY(0);
            return point;
        }
    }

    public Vector3 ExitEdge => SurfaceCentre + facing.ToVector() * (GridMath.CellSize * 0.5f);

    void Awake() {
        SnapToGrid();
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid() {
        transform.SetPositionAndRotation(GridMath.CellToWorld(Cell), facing.ToRotation());
    }

    void OnDrawGizmos() {
        Vector3 centre = GridMath.CellToWorld(Cell) + Vector3.up * 0.05f;
        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawCube(centre, new Vector3(GridMath.CellSize, 0.1f, GridMath.CellSize));
        Gizmos.DrawLine(centre, centre + facing.ToVector() * GridMath.CellSize);
    }
}