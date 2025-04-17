using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Tooltip("List of chunk prefabs to spawn")]
    public List<GameObject> chunkPrefabs;
    [Tooltip("How many chunks to preload at start")]
    public int initialChunks = 3;
    [Tooltip("Vertical size (world units) of each chunk")]
    public float chunkLength = 20f;
    [Tooltip("Optional parent transform for spawned chunks")]
    public Transform chunkParent;

    // internal list of currently active chunks
    private readonly List<GameObject> activeChunks = new List<GameObject>();
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
        // preload initial chunks stacked vertically
        for (int i = 0; i < initialChunks; i++)
        {
            Vector3 pos = Vector3.up * i * chunkLength;
            SpawnChunk(pos);
        }
    }

    void Update()
    {
        if (activeChunks.Count == 0) return;

        // 1) Spawn ahead when camera nears the end of the last chunk
        var last = activeChunks[activeChunks.Count - 1];
        float spawnY = last.transform.position.y + (chunkLength * 0.5f);
        if (cam.position.y >= spawnY)
        {
            Vector3 nextPos = last.transform.position + Vector3.up * chunkLength;
            SpawnChunk(nextPos);
        }

        // 2) Destroy behind when a chunk falls far below the camera
        var first = activeChunks[0];
        float destroyY = cam.position.y - (chunkLength * 1.5f);
        if (first.transform.position.y < destroyY)
        {
            Destroy(first);
            activeChunks.RemoveAt(0);
        }
    }

    private void SpawnChunk(Vector3 position)
    {
        int idx = Random.Range(0, chunkPrefabs.Count);
        var chunk = Instantiate(chunkPrefabs[idx], position, Quaternion.identity, chunkParent);
        activeChunks.Add(chunk);
    }
}
