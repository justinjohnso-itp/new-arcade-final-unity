using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Tooltip("List of chunk prefabs to spawn")]
    public List<GameObject> chunkPrefabs;
    [Tooltip("How many chunks to preload at start")]
    public int initialChunks = 3;
    [Tooltip("Length (world units) of each chunk along travel direction")]
    public float chunkLength = 20f;
    [Tooltip("Optional parent transform for spawned chunks")]
    public Transform chunkParent;

    // internal list of currently active chunks
    private readonly List<GameObject> activeChunks = new List<GameObject>();
    private Transform cam;
    private Vector3 forwardDir;

    void Start()
    {
        cam = Camera.main.transform;
        // use global forward direction (30° from east, which is 15° east of northeast)
        forwardDir = GameSettings.ForwardDirection;

        // preload initial chunks along forwardDir
        for (int i = 0; i < initialChunks; i++)
        {
            Vector3 pos = forwardDir * i * chunkLength;
            SpawnChunk(pos);
        }
    }

    void Update()
    {
        if (activeChunks.Count == 0) return;

        // project camera and chunks onto forwardDir to get "distance along path"
        float camDist = Vector3.Dot(cam.position, forwardDir);

        // 1) Spawn ahead when camera nears end of last chunk
        var last = activeChunks[activeChunks.Count - 1];
        float lastDist = Vector3.Dot(last.transform.position, forwardDir);
        if (camDist >= lastDist)
        {
            Vector3 nextPos = last.transform.position + forwardDir * chunkLength;
            SpawnChunk(nextPos);
        }

        // 2) Destroy behind when chunk falls far behind camera
        var first = activeChunks[0];
        float firstDist = Vector3.Dot(first.transform.position, forwardDir);
        if (firstDist < camDist - chunkLength * 1.5f)
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
