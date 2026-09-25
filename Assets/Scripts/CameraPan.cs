using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour {
    [SerializeField] float panSpeed = 8f;
    [Tooltip("Scroll values differ between platforms; tune until one notch feels right.")]
    [SerializeField] float zoomSpeed = 0.01f;
    [SerializeField] Vector2 heightRange = new Vector2(6f, 18f);
    [SerializeField] float roomSize = 20f;

    void Update() {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null || mouse == null) {
            return;
        }
        Vector3 move = Vector3.zero;
        if (kb.wKey.isPressed) move.z += 1f;
        if (kb.sKey.isPressed) move.z -= 1f;
        if (kb.dKey.isPressed) move.x += 1f;
        if (kb.aKey.isPressed) move.x -= 1f;

        Vector3 p = transform.position + move.normalized * (panSpeed * Time.deltaTime);
        p.y -= mouse.scroll.ReadValue().y * zoomSpeed;
        p.x = Mathf.Clamp(p.x, 1f, roomSize - 1f);
        p.z = Mathf.Clamp(p.z, 1f, roomSize - 1f);
        p.y = Mathf.Clamp(p.y, heightRange.x, heightRange.y);
        transform.position = p;
    }
}