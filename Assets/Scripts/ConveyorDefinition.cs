using UnityEngine;

[CreateAssetMenu(fileName = "Conveyor_", menuName = "Conveyor/Conveyor Definition")]
public class ConveyorDefinition : ScriptableObject {
    public string displayName = "Generic";
    public ConveyorPiece prefab;
    [Min(1)] public int lengthCells = 3;
    [Tooltip("Climbs one level over its length. F flips it to descend.")]
    public bool isIncline;
}