using System.Collections.Generic;
using UnityEngine;

// A polyline items travel along, sampled by distance from the start.
public class ConveyorPath {
    readonly List<Vector3> points = new List<Vector3>();
    readonly List<float> cumulative = new List<float>();

    public float Length { get; }

    public ConveyorPath(IReadOnlyList<Vector3> waypoints) {
        foreach (Vector3 p in waypoints) {
            if (points.Count == 0) {
                points.Add(p);
                cumulative.Add(0f);
                continue;
            }
            float step = Vector3.Distance(points[points.Count - 1], p);
            if (step < 0.001f) {
                continue;   // drop duplicates so every segment has a direction
            }
            points.Add(p);
            cumulative.Add(cumulative[cumulative.Count - 1] + step);
        }
        Length = cumulative[cumulative.Count - 1];
    }

    public void Sample(float distance, out Vector3 position, out Vector3 forward) {
        distance = Mathf.Clamp(distance, 0f, Length);
        int i = 1;
        while (i < points.Count - 1 && cumulative[i] < distance) {
            i++;
        }
        float t = (distance - cumulative[i - 1]) / (cumulative[i] - cumulative[i - 1]);
        position = Vector3.Lerp(points[i - 1], points[i], t);
        forward = (points[i] - points[i - 1]).normalized;
    }
}