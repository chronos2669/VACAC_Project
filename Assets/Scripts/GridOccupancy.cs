using System.Collections.Generic;
using UnityEngine;

public class GridOccupancy : MonoBehaviour {
    public static GridOccupancy Instance { get; private set; }

    [SerializeField, Min(8)] int roomSize = 20;   // cells per side; the room is roomSize units cubed

    readonly Dictionary<Vector3Int, Object> cells = new Dictionary<Vector3Int, Object>();

    public int RoomSize => roomSize;

    void Awake() {
        Instance = this;
    }

    public bool InBounds(Vector3Int cell) {
        return cell.x >= 0 && cell.x < roomSize
            && cell.z >= 0 && cell.z < roomSize
            && cell.y >= GridMath.MinLevel && cell.y <= GridMath.MaxLevel;
    }

    public bool IsFree(Vector3Int cell) {
        return !cells.ContainsKey(cell);
    }

    public void Occupy(Vector3Int cell, Object owner) {
        cells[cell] = owner;
    }

    public void Release(Vector3Int cell) {
        cells.Remove(cell);
    }

    public void Clear() {
        cells.Clear();
    }
}