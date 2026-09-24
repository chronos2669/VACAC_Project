using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class ConveyorBuilder : MonoBehaviour {
    [SerializeField] Camera cam;
    [SerializeField] GridPoint startPoint;
    [SerializeField] GridPoint endPoint;
    [Tooltip("Order maps to keys 1, 2, 3: Generic, Short, Incline.")]
    [SerializeField] ConveyorDefinition[] palette;
    [SerializeField] Material ghostValidMaterial;
    [SerializeField] Material ghostInvalidMaterial;
    [SerializeField] float snapRadiusCells = 2.5f;
    [SerializeField] Transform piecesRoot;
    [Tooltip("Optional marker shown on the cell where the next piece must start.")]
    [SerializeField] Transform openEndMarker;

    readonly List<ConveyorPiece> chain = new List<ConveyorPiece>();

    ConveyorDefinition selected;
    GridDir ghostDir;
    bool inclineDescending;
    bool buildEnabled = true;

    ConveyorPiece ghost;
    Renderer[] ghostRenderers;
    bool? ghostShowsValid;
    ConveyorPlacement candidate;
    bool candidateValid;

    public IReadOnlyList<ConveyorPiece> Chain => chain;
    public ConveyorDefinition Selected => selected;
    public int SelectedIndex => System.Array.IndexOf(palette, selected);
    public GridDir GhostDirection => ghostDir;
    public bool InclineDescending => inclineDescending;
    public bool IsComplete { get; private set; }
    public string StatusMessage { get; private set; } = "";

    // The open end: the cell the next piece must start in, and the flow direction arriving there.
    Vector3Int OpenCell => chain.Count == 0 ? startPoint.ExitCell : chain[chain.Count - 1].Placement.ExitCell;
    GridDir OpenDir => chain.Count == 0 ? startPoint.Facing : chain[chain.Count - 1].Placement.dir;

    void Start() {
        ghostDir = startPoint.Facing;
        SelectIndex(0);
        UpdateOpenEndMarker();
    }

    void Update() {
        if (!buildEnabled) {
            return;
        }
        HandleKeys();
        if (IsComplete) {
            StatusMessage = "Chain complete - press Begin, or undo to keep building.";
        } else {
            UpdateGhost();
        }
        HandleMouse();
    }

    // ---------- Input ----------

    void HandleKeys() {
        Keyboard kb = Keyboard.current;
        if (kb == null) {
            return;
        }
        if (kb.digit1Key.wasPressedThisFrame) SelectIndex(0);
        if (kb.digit2Key.wasPressedThisFrame) SelectIndex(1);
        if (kb.digit3Key.wasPressedThisFrame) SelectIndex(2);
        if (kb.qKey.wasPressedThisFrame) ghostDir = ghostDir.RotateAnticlockwise();
        if (kb.eKey.wasPressedThisFrame) ghostDir = ghostDir.RotateClockwise();
        if (kb.fKey.wasPressedThisFrame) inclineDescending = !inclineDescending;
        if (kb.backspaceKey.wasPressedThisFrame) UndoLast();
    }

    void HandleMouse() {
        Mouse mouse = Mouse.current;
        if (mouse == null || IsPointerOverUI()) {
            return;
        }
        if (mouse.leftButton.wasPressedThisFrame && !IsComplete && candidateValid) {
            Place(candidate);
        }
        if (mouse.rightButton.wasPressedThisFrame) {
            UndoLast();
        }
    }

    static bool IsPointerOverUI() {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public void SelectIndex(int index) {
        if (index < 0 || index >= palette.Length || palette[index] == selected) {
            return;
        }
        selected = palette[index];
        RebuildGhost();
    }

    // ---------- Ghost and validation ----------

    void UpdateGhost() {
        Vector3Int open = OpenCell;
        bool hovering = TryGetHoveredCell(open.y, out Vector3Int hovered);
        bool snapped = hovering && HorizontalDistance(hovered, open) <= snapRadiusCells;

        candidate = new ConveyorPlacement {
            definition = selected,
            origin = snapped || !hovering ? open : hovered,
            dir = ghostDir,
            rise = selected.isIncline ? (inclineDescending ? -1 : 1) : 0
        };

        string reason;
        if (!snapped) {
            candidateValid = false;
            reason = "Move the mouse nearer the end marker to snap.";
        } else {
            candidateValid = Validate(candidate, out reason);
        }
        StatusMessage = reason;

        ghost.gameObject.SetActive(hovering);
        ghost.ApplyPlacement(candidate);
        SetGhostMaterial(candidateValid);
    }

    bool Validate(ConveyorPlacement p, out string reason) {
        GridOccupancy grid = GridOccupancy.Instance;

        if (p.dir == OpenDir.Opposite()) {
            reason = "Belts can't reverse back into the chain.";
            return false;
        }
        if (p.ExitLevel < GridMath.MinLevel) {
            reason = "Can't go below floor level - press F to climb instead.";
            return false;
        }
        if (p.ExitLevel > GridMath.MaxLevel) {
            reason = $"Maximum height is {GridMath.MaxLevel} incline stages - press F to descend.";
            return false;
        }
        foreach (Vector3Int cell in p.FootprintCells()) {
            if (!grid.InBounds(cell)) {
                reason = "That would pass through a wall.";
                return false;
            }
            if (!grid.IsFree(cell)) {
                reason = "Blocked by an obstacle or another belt.";
                return false;
            }
        }
        Vector3Int exit = p.ExitCell;
        if (exit == endPoint.Cell) {
            reason = "Click to connect to the end point!";
            return true;
        }
        if (!grid.InBounds(exit) || !grid.IsFree(exit)) {
            reason = "Its output would face a wall or something solid.";
            return false;
        }
        reason = "Click to place.";
        return true;
    }

    bool TryGetHoveredCell(int level, out Vector3Int cell) {
        cell = default;
        Mouse mouse = Mouse.current;
        if (mouse == null) {
            return false;
        }
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, level * GridMath.LevelHeight, 0f));
        if (!plane.Raycast(ray, out float enter)) {
            return false;
        }
        cell = GridMath.WorldToCell(ray.GetPoint(enter), level);
        return true;
    }

    static float HorizontalDistance(Vector3Int a, Vector3Int b) {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    void RebuildGhost() {
        if (ghost != null) {
            Destroy(ghost.gameObject);
        }
        ghost = Instantiate(selected.prefab, transform);
        ghost.name = "Ghost";
        foreach (Collider col in ghost.GetComponentsInChildren<Collider>()) {
            col.enabled = false;
        }
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in ghostRenderers) {
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        ghostShowsValid = null;   // force a material refresh
        ghost.gameObject.SetActive(false);
    }

    void SetGhostMaterial(bool valid) {
        if (ghostShowsValid == valid) {
            return;
        }
        ghostShowsValid = valid;
        Material material = valid ? ghostValidMaterial : ghostInvalidMaterial;
        foreach (Renderer r in ghostRenderers) {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) {
                mats[i] = material;
            }
            r.sharedMaterials = mats;
        }
    }

    // ---------- Placing and undoing ----------

    void Place(ConveyorPlacement placement) {
        ConveyorPiece piece = Instantiate(placement.definition.prefab, piecesRoot);
        piece.name = $"{placement.definition.displayName} {chain.Count + 1}";
        piece.ApplyPlacement(placement);
        foreach (Vector3Int cell in placement.FootprintCells()) {
            GridOccupancy.Instance.Occupy(cell, piece);
        }
        chain.Add(piece);
        ghostDir = placement.dir;   // keep heading the same way by default
        RefreshCompletion();
    }

    public void UndoLast() {
        if (!buildEnabled || chain.Count == 0) {
            return;
        }
        ConveyorPiece last = chain[chain.Count - 1];
        foreach (Vector3Int cell in last.Placement.FootprintCells()) {
            GridOccupancy.Instance.Release(cell);
        }
        chain.RemoveAt(chain.Count - 1);
        Destroy(last.gameObject);
        ghostDir = OpenDir;
        RefreshCompletion();
    }

    void RefreshCompletion() {
        IsComplete = chain.Count > 0 && chain[chain.Count - 1].Placement.ExitCell == endPoint.Cell;
        ghost.gameObject.SetActive(!IsComplete);
        UpdateOpenEndMarker();
    }

    void UpdateOpenEndMarker() {
        if (openEndMarker == null) {
            return;
        }
        openEndMarker.gameObject.SetActive(buildEnabled && !IsComplete);
        openEndMarker.position = GridMath.CellToWorld(OpenCell);
    }

    // ---------- Run phase ----------

    public void SetBuildEnabled(bool value) {
        buildEnabled = value;
        ghost.gameObject.SetActive(value && !IsComplete);
        UpdateOpenEndMarker();
    }

    // Start centre -> every piece's waypoints -> end centre.
    public List<Vector3> BuildPath() {
        List<Vector3> path = new List<Vector3> { startPoint.SurfaceCentre, startPoint.ExitEdge };
        foreach (ConveyorPiece piece in chain) {
            piece.Placement.AppendWaypoints(path);
        }
        path.Add(endPoint.SurfaceCentre);
        return path;
    }
}