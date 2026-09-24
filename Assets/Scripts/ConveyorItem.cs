using System;
using UnityEngine;

public class ConveyorItem : MonoBehaviour {
    ConveyorPath path;
    float speed;
    float lift;
    float travelled;
    Action<ConveyorItem> onArrived;

    public void Launch(ConveyorPath path, float speed, float lift, Action<ConveyorItem> onArrived) {
        this.path = path;
        this.speed = speed;
        this.lift = lift;
        this.onArrived = onArrived;
        travelled = 0f;
        Move(snapRotation: true);
    }

    void Update() {
        if (path == null) {
            return;
        }
        travelled += speed * Time.deltaTime;
        Move(snapRotation: false);
        if (travelled >= path.Length) {
            onArrived?.Invoke(this);
            Destroy(gameObject);
        }
    }

    // Faces along the belt, tilting on inclines and easing round corners.
    void Move(bool snapRotation) {
        path.Sample(travelled, out Vector3 position, out Vector3 forward);
        Quaternion target = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = snapRotation ? target : Quaternion.Slerp(transform.rotation, target, 12f * Time.deltaTime);
        transform.position = position + transform.up * lift;
    }
}