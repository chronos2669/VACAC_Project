using UnityEngine;

public class ConveyorPiece : MonoBehaviour {
    [Header("Optional belt scrolling")]
    [SerializeField] Renderer beltRenderer;
    [SerializeField] int beltMaterialIndex = 0;
    [SerializeField] Vector2 scrollDirection = new Vector2(0f, 1f);
    [SerializeField] float scrollSpeed = 1.5f;

    [Header("Alignment gizmo (prefab editing only)")]
    [SerializeField, Min(1)] int gizmoLength = 3;
    [SerializeField] bool gizmoIsIncline;

    public ConveyorPlacement Placement { get; private set; }

    Material beltMaterial;
    bool running;

    // Positions the piece on the grid. Used by placed pieces and by the ghost preview.
    public void ApplyPlacement(ConveyorPlacement placement) {
        Placement = placement;
        Pose pose = placement.RootPose();
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    }

    public void SetRunning(bool value) {
        running = value;
        if (running && beltMaterial == null && beltRenderer != null) {
            beltMaterial = beltRenderer.materials[beltMaterialIndex];   // per-piece material instance
        }
    }

    void Update() {
        if (!running || beltMaterial == null) {
            return;
        }
        // A descending incline is the ascending model turned round, so its texture scrolls the other way.
        float sign = Placement.rise < 0 ? -1f : 1f;
        beltMaterial.mainTextureOffset += scrollDirection * (scrollSpeed * sign * Time.deltaTime);
    }

    void OnDestroy() {
        if (beltMaterial != null) {
            Destroy(beltMaterial);
        }
    }

    // Cyan box = the cells this piece should fill; yellow line = where the belt top should run.
    void OnDrawGizmos() {
        if (Application.isPlaying) {
            return;
        }
        float cell = GridMath.CellSize;
        float length = gizmoLength * cell;
        float rise = gizmoIsIncline ? GridMath.LevelHeight : 0f;
        float height = GridMath.LevelHeight + rise;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0f, height * 0.5f, length * 0.5f), new Vector3(cell, height, length));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(0f, GridMath.BeltSurfaceHeight, 0f),
                        new Vector3(0f, GridMath.BeltSurfaceHeight + rise, length));
    }
}