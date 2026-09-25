using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildUI : MonoBehaviour {
    [SerializeField] ConveyorBuilder builder;
    [SerializeField] GameFlow flow;
    [SerializeField] ItemSpawner spawner;
    [Tooltip("Same order as the builder's palette: Generic, Short, Incline.")]
    [SerializeField] Button[] pieceButtons;
    [SerializeField] Button beginButton;
    [SerializeField] Button resetButton;
    [SerializeField] TMP_Text statusText;
    [SerializeField] Color selectedColour = new Color(1f, 0.85f, 0.3f);
    [SerializeField] Color normalColour = Color.white;

    void Start() {
        for (int i = 0; i < pieceButtons.Length; i++) {
            int index = i;   // copy for the lambda
            pieceButtons[i].onClick.AddListener(() => builder.SelectIndex(index));
        }
        beginButton.onClick.AddListener(flow.Begin);
        resetButton.onClick.AddListener(flow.ResetLevel);
    }

    void Update() {
        beginButton.interactable = flow.CanBegin;
        for (int i = 0; i < pieceButtons.Length; i++) {
            pieceButtons[i].image.color = i == builder.SelectedIndex ? selectedColour : normalColour;
            pieceButtons[i].interactable = !flow.IsRunning;
        }
        statusText.text = BuildStatus();
    }

    string BuildStatus() {
        if (flow.IsRunning) {
            return $"Running - delivered {spawner.Delivered} of {spawner.Spawned} spawned";
        }
        ConveyorDefinition selected = builder.Selected;
        string piece = selected != null ? selected.displayName : "-";
        if (selected != null && selected.isIncline) {
            piece += builder.InclineDescending ? " (down)" : " (up)";
        }
        return $"{piece} facing {builder.GhostDirection} | {builder.Chain.Count} placed\n{builder.StatusMessage}";
    }
}