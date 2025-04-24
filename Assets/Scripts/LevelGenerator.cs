using UnityEngine;
using System.Collections.Generic; // Required for Lists
using System.Linq; // Required for LINQ methods like Where, Select, Distinct

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Tooltip("The specific prefab to always start the level with.")]
    public GameObject startChunkPrefab; // Should be LevelPart_Start
    [Tooltip("List of chunk prefabs to spawn randomly AFTER the start chunk.")]
    public List<GameObject> chunkPrefabs; // Should contain LevelPart_Road
    [Tooltip("How many additional random chunks to preload at start (after the start chunk).")]
    public int initialChunks = 2;
    [Tooltip("Optional parent transform for spawned chunks")]
    public Transform chunkParent;
    [Tooltip("How close (in world units along the forward direction) the camera must be to the last chunk's origin before spawning the next one.")]
    public float spawnTriggerDistance = 30f;
    [Tooltip("How far behind the camera (in world units along the forward direction) a chunk must be before it is destroyed.")]
    public float destroyDistanceBehind = 50f;

    [Header("Obstacle Settings")]
    [Tooltip("List of obstacle prefabs to randomly spawn within chunks.")]
    public List<GameObject> obstaclePrefabs;
    [Tooltip("Maximum number of obstacles to attempt spawning per chunk.")]
    public int maxObstaclesPerChunk = 3;

    [Header("Building Settings")]
    [Tooltip("Building prefabs allowed in the 'Top' area (e.g., BlueHouse, Blacksmith). Must have DeliveryZone_Placeholder child (inactive).")]
    public List<GameObject> topBuildingPrefabs;
    [Tooltip("Building prefabs allowed in the 'Bottom' area (e.g., WoodHouse). Must have DeliveryZone_Placeholder child (inactive).")]
    public List<GameObject> bottomBuildingPrefabs;
    [Tooltip("Minimum distance between spawned buildings within the same chunk.")]
    public float minBuildingSpacing = 5f;
    [Tooltip("Maximum number of buildings to attempt spawning per area (Top/Bottom) in a chunk.")]
    public int maxBuildingsPerArea = 1; // Adjust if you want more density

    [Header("Delivery Zone Settings")]
    [Tooltip("How many 'Road' chunks form a group for zone counting.")]
    public int zoneGroupSize = 2;
    [Tooltip("Maximum number of delivery zones allowed within one group of chunks.")]
    public int maxZonesPerGroup = 4;
    [Tooltip("Chance (0-1) to activate a delivery zone on a spawned building, if group limit not reached.")]
    [Range(0f, 1f)]
    public float deliveryZoneActivationChance = 0.6f; // 60% chance

    // --- Private Fields ---
    private readonly List<GameObject> activeChunks = new List<GameObject>();
    private Transform cam;
    private Vector3 forwardDir;
    private bool canCheckSpawning = false;
    // Zone Group Tracking
    private int chunksInCurrentGroup = 0;
    private int zonesInCurrentGroup = 0;
    // Temp list for building placement checks
    private List<float> currentChunkBuildingPositionsX = new List<float>();


    void Start()
    {
        cam = Camera.main.transform;
        forwardDir = GameSettings.ForwardDirection;

        // --- Validate Prefabs --- 
        if (startChunkPrefab == null)
        {
            Debug.LogError("LevelGenerator: Start Chunk Prefab is not assigned!");
            this.enabled = false;
            return;
        }
        if (FindStartPosition(startChunkPrefab) == null || FindEndPosition(startChunkPrefab) == null)
        {
             Debug.LogError($"LevelGenerator: Start Chunk Prefab '{startChunkPrefab.name}' is missing StartPosition or EndPosition child.", startChunkPrefab);
             this.enabled = false;
             return;
        }
        if (chunkPrefabs == null || chunkPrefabs.Count == 0)
        {
            Debug.LogWarning("LevelGenerator: No random chunk prefabs assigned.");
        }

        // --- Spawn Start Chunk --- 
        Vector3 currentSpawnPosition = Vector3.zero;
        GameObject startInstance = Instantiate(startChunkPrefab, currentSpawnPosition, Quaternion.identity, chunkParent);
        activeChunks.Add(startInstance);
        Transform lastEndPosition = FindEndPosition(startInstance);
        if (lastEndPosition == null) return;

        // --- Spawn Additional Initial Chunks --- 
        for (int i = 0; i < initialChunks; i++)
        {
            if (chunkPrefabs == null || chunkPrefabs.Count == 0) break;

            int idx = Random.Range(0, chunkPrefabs.Count);
            GameObject prefabToSpawn = chunkPrefabs[idx];
            GameObject newChunkInstance = SpawnChunk(prefabToSpawn, lastEndPosition.position);

            if (newChunkInstance != null)
            {
                lastEndPosition = FindEndPosition(newChunkInstance);
                if (lastEndPosition == null)
                {
                    Debug.LogError($"LevelGenerator: Spawned chunk '{newChunkInstance.name}' is missing EndPosition. Stopping initial spawn.", newChunkInstance);
                    break;
                }
            }
            else
            {
                Debug.LogWarning($"LevelGenerator: Failed to spawn initial random chunk (Prefab: {prefabToSpawn.name}).");
                // break; // Optionally stop if any random chunk fails
            }
        }

        canCheckSpawning = true;
        chunksInCurrentGroup = 0;
        zonesInCurrentGroup = 0;
    }

    void Update()
    {
        if (!canCheckSpawning || activeChunks.Count == 0) return; 

        float camDist = Vector3.Dot(cam.position, forwardDir);

        // 1) Spawn ahead
        var lastChunk = activeChunks[activeChunks.Count - 1];
        if (lastChunk == null) return;
        float lastDist = Vector3.Dot(lastChunk.transform.position, forwardDir);
        bool shouldSpawn = camDist >= lastDist - spawnTriggerDistance;

        if (shouldSpawn)
        {
            Transform lastEndPos = FindEndPosition(lastChunk);
            if (lastEndPos != null)
            {
                if (chunkPrefabs != null && chunkPrefabs.Count > 0)
                {
                    int idx = Random.Range(0, chunkPrefabs.Count);
                    GameObject prefabToSpawn = chunkPrefabs[idx];
                    GameObject spawnedInUpdate = SpawnChunk(prefabToSpawn, lastEndPos.position);
                }
            }
            else
            {
                 Debug.LogError($"LevelGenerator: Last active chunk '{lastChunk.name}' is missing 'EndPosition'. Cannot spawn next.", lastChunk);
            }
        }

        // 2) Destroy behind
        var firstChunk = activeChunks[0];
        if (firstChunk == null) return;
        float firstDist = Vector3.Dot(firstChunk.transform.position, forwardDir);

        if (firstDist < camDist - destroyDistanceBehind)
        {
            Destroy(firstChunk);
            activeChunks.RemoveAt(0);
        }
    }

    // Spawns a prefab, aligning its StartPosition to the targetAlignmentPosition.
    private GameObject SpawnChunk(GameObject prefabToSpawn, Vector3 targetAlignmentPosition)
    {
        Transform startPosTransform = FindStartPosition(prefabToSpawn);
        Transform endPosTransform = FindEndPosition(prefabToSpawn);

        if (startPosTransform == null || endPosTransform == null)
        {
             Debug.LogError($"LevelGenerator: Prefab '{prefabToSpawn.name}' is missing StartPosition or EndPosition. Skipping.", prefabToSpawn);
             return null;
        }

        Vector3 startOffset = prefabToSpawn.transform.InverseTransformPoint(startPosTransform.position);
        Vector3 spawnPosition = targetAlignmentPosition - startOffset;

        var chunkInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, chunkParent);
        activeChunks.Add(chunkInstance);

        SpawnObstaclesOnChunk(chunkInstance);

        // Spawn Buildings & Activate Zones only for Road chunks
        if (prefabToSpawn.name.Contains("LevelPart_Road"))
        {
            SpawnBuildingsAndZonesOnChunk(chunkInstance);

            // Update Zone Group Counters
            chunksInCurrentGroup++;
            if (chunksInCurrentGroup >= zoneGroupSize)
            {
                chunksInCurrentGroup = 0;
                zonesInCurrentGroup = 0;
            }
        }

        return chunkInstance;
    }

    private void SpawnObstaclesOnChunk(GameObject chunkInstance)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0 || maxObstaclesPerChunk <= 0) return;

        Transform spawnAreaTransform = chunkInstance.transform.Find("ObstacleSpawnArea");
        if (spawnAreaTransform == null)
        {
            Debug.LogWarning($"Chunk '{chunkInstance.name}' is missing 'ObstacleSpawnArea'. Cannot spawn obstacles.", chunkInstance);
            return;
        }

        PolygonCollider2D spawnBoundsCollider = spawnAreaTransform.GetComponent<PolygonCollider2D>();
        if (spawnBoundsCollider == null)
        {
            Debug.LogWarning($"'ObstacleSpawnArea' on chunk '{chunkInstance.name}' is missing PolygonCollider2D.", chunkInstance);
            return;
        }

        Bounds spawnBounds = spawnBoundsCollider.bounds;
        int obstaclesSpawned = 0;
        int maxAttempts = maxObstaclesPerChunk * 5; // Allow more attempts for rejection sampling

        for (int attempt = 0; attempt < maxAttempts && obstaclesSpawned < maxObstaclesPerChunk; attempt++)
        {
            int obstacleIndex = Random.Range(0, obstaclePrefabs.Count);
            GameObject obstaclePrefab = obstaclePrefabs[obstacleIndex];

            float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
            float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);
            Vector3 potentialSpawnPoint = new Vector3(randomX, randomY, spawnBounds.center.z);

            // Check if the point is INSIDE the PolygonCollider2D
            if (!spawnBoundsCollider.OverlapPoint(potentialSpawnPoint))
            {
                continue; // Outside polygon, try again
            }

            // Optional: Overlap Check (to prevent stacking)
            // ... (code removed for brevity) ...

            Instantiate(obstaclePrefab, potentialSpawnPoint, Quaternion.identity, chunkInstance.transform);
            obstaclesSpawned++;
        }
    }

    private void SpawnBuildingsAndZonesOnChunk(GameObject roadChunkInstance)
    {
        currentChunkBuildingPositionsX.Clear();
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Top", "Wall_Bottom", topBuildingPrefabs, true);
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Bottom", "Wall_Top", bottomBuildingPrefabs, false);
    }

    private void SpawnBuildingsInArea(GameObject roadChunkInstance, string areaName, string wallToHugName, List<GameObject> buildingPrefabs, bool hugMinY)
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0) return;

        Transform areaTransform = roadChunkInstance.transform.Find(areaName);
        Collider2D areaCollider = areaTransform?.GetComponent<Collider2D>();

        if (areaCollider == null)
        {
            Debug.LogWarning($"'{areaName}' or its Collider2D not found on {roadChunkInstance.name}", roadChunkInstance);
            return;
        }

        Bounds areaBounds = areaCollider.bounds;
        int buildingsSpawnedInArea = 0;
        int candidatesToFind = 3;
        int maxPlacementAttemptsPerCandidate = 10;
        int maxBuildingAttempts = maxBuildingsPerArea * 5; 

        for (int buildingAttempt = 0; buildingAttempt < maxBuildingAttempts && buildingsSpawnedInArea < maxBuildingsPerArea; buildingAttempt++)
        {
            GameObject buildingPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            List<Vector3> validCandidates = new List<Vector3>();

            // Find Multiple Valid Candidate Positions using Rejection Sampling
            for (int candidateNum = 0; candidateNum < candidatesToFind; candidateNum++)
            {
                for (int placementAttempt = 0; placementAttempt < maxPlacementAttemptsPerCandidate; placementAttempt++)
                {
                    float randomX = Random.Range(areaBounds.min.x, areaBounds.max.x);
                    float randomY = Random.Range(areaBounds.min.y, areaBounds.max.y);
                    Vector3 testPos = new Vector3(randomX, randomY, areaBounds.center.z);

                    if (areaCollider.OverlapPoint(testPos))
                    {
                        validCandidates.Add(testPos);
                        break; // Found one
                    }
                }
            }

            if (validCandidates.Count == 0) continue; 

            // Select the Best Candidate (Closest to Road Edge)
            Vector3 bestPos = validCandidates[0];
            if (hugMinY) // Top Area: Prefer LOWEST Y
            {
                for (int i = 1; i < validCandidates.Count; i++) { if (validCandidates[i].y < bestPos.y) bestPos = validCandidates[i]; }
            }
            else // Bottom Area: Prefer HIGHEST Y
            {
                for (int i = 1; i < validCandidates.Count; i++) { if (validCandidates[i].y > bestPos.y) bestPos = validCandidates[i]; }
            }

            // Optional: Offset based on building size/pivot
            // ... (code removed for brevity) ...

            // Check Spacing 
            bool tooClose = false;
            foreach (float existingX in currentChunkBuildingPositionsX)
            {
                if (Mathf.Abs(bestPos.x - existingX) < minBuildingSpacing) { tooClose = true; break; }
            }
            if (tooClose) continue; 

            // Optional Overlap Check
            // ... (code removed for brevity) ...

            // Instantiate
            GameObject buildingInstance = Instantiate(buildingPrefab, bestPos, Quaternion.identity, roadChunkInstance.transform);
            currentChunkBuildingPositionsX.Add(bestPos.x); 
            buildingsSpawnedInArea++;

            // Try Activate Delivery Zone
            TryActivateDeliveryZone(buildingInstance);
        }
    }


    private void TryActivateDeliveryZone(GameObject buildingInstance)
    {
        if (zonesInCurrentGroup >= maxZonesPerGroup) return; // Group limit hit

        InventoryManager invManager = InventoryManager.Instance;
        if (invManager == null) {
            Debug.LogWarning("TryActivateDeliveryZone: InventoryManager not found!", buildingInstance);
            return;
        }

        // Get unique colors currently in inventory
        List<InventorySlotData> currentInventory = invManager.GetInventorySlots();
        List<Color> availableColors = currentInventory
            .Where(slotData => slotData?.itemData != null)
            .Select(slotData => slotData.itemData.itemColor)
            .Distinct()
            .ToList();

        if (availableColors.Count == 0) {
            Debug.Log("[TryActivateDeliveryZone] No unique item colors in inventory. Cannot activate zone.");
            return;
        }

        // Activation Logic
        if (Random.value <= deliveryZoneActivationChance)
        {
            Transform zonePlaceholder = buildingInstance.transform.Find("DeliveryZone_Placeholder");
            if (zonePlaceholder != null)
            {
                DeliveryZone zoneScript = zonePlaceholder.GetComponent<DeliveryZone>();
                if (zoneScript == null)
                {
                    Debug.LogWarning($"DeliveryZone component missing on placeholder for {buildingInstance.name}. Adding it.", buildingInstance);
                    zoneScript = zonePlaceholder.gameObject.AddComponent<DeliveryZone>();
                }

                Color chosenColor = availableColors[Random.Range(0, availableColors.Count)];
                zoneScript.ActivateZone(chosenColor);
                zonesInCurrentGroup++;
                Debug.Log($"[TryActivateDeliveryZone] Activated Zone on {buildingInstance.name} with color {chosenColor}. Zones in group: {zonesInCurrentGroup}/{maxZonesPerGroup}");
            }
            else
            {
                Debug.LogWarning($"Building {buildingInstance.name} spawned, but 'DeliveryZone_Placeholder' not found!", buildingInstance);
            }
        }
        else
        {
            Debug.Log("[TryActivateDeliveryZone] Activation chance failed.");
        }
    }

    // Helper to find EndPosition transform
    private Transform FindEndPosition(GameObject chunkInstance)
    {
        return chunkInstance?.transform.Find("EndPosition");
    }

    // Helper to find StartPosition transform
    private Transform FindStartPosition(GameObject prefab)
    {
        return prefab?.transform.Find("StartPosition");
    }
}