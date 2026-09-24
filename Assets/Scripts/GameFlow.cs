using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlow : MonoBehaviour {
    [SerializeField] RoomGenerator room;
    [SerializeField] ConveyorBuilder builder;
    [SerializeField] ItemSpawner spawner;

    public bool IsRunning { get; private set; }
    public bool CanBegin => !IsRunning && builder.IsComplete;

    void Start() {
        room.Generate();
    }

    public void Begin() {
        if (!CanBegin) {
            return;
        }
        IsRunning = true;
        builder.SetBuildEnabled(false);
        foreach (ConveyorPiece piece in builder.Chain) {
            piece.SetRunning(true);
        }
        spawner.Begin(builder.BuildPath());
    }

    // Reloading the scene is the simplest full reset, and gives a fresh obstacle layout.
    public void ResetLevel() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}