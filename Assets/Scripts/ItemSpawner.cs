using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour {
    [Tooltip("Item models; one is picked at random for each spawn.")]
    [SerializeField] GameObject[] itemPrefabs;
    [SerializeField, Min(0.1f)] float spawnInterval = 1.2f;
    [Tooltip("0 = keep spawning until Reset.")]
    [SerializeField, Min(0)] int itemsToSpawn = 0;
    [Tooltip("World units per second along the belt.")]
    [SerializeField, Min(0.1f)] float beltSpeed = 1.5f;
    [Tooltip("Half the item's height, so it sits on the belt rather than in it.")]
    [SerializeField] float itemLift = 0.2f;
    [SerializeField] Transform itemsRoot;

    public int Spawned { get; private set; }
    public int Delivered { get; private set; }

    ConveyorPath path;

    public void Begin(List<Vector3> waypoints) {
        path = new ConveyorPath(waypoints);
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop() {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);
        while (itemsToSpawn == 0 || Spawned < itemsToSpawn) {
            SpawnOne();
            yield return wait;
        }
    }

    void SpawnOne() {
        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        GameObject instance = Instantiate(prefab, itemsRoot);
        // Items are driven along the path, so physics must not fight them.
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>()) {
            body.isKinematic = true;
        }
        ConveyorItem item = instance.GetComponent<ConveyorItem>();
        if (item == null) {
            item = instance.AddComponent<ConveyorItem>();
        }
        item.Launch(path, beltSpeed, itemLift, OnArrived);
        Spawned++;
    }

    void OnArrived(ConveyorItem item) {
        Delivered++;
    }
}