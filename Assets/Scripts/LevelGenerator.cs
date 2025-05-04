using UnityEngine.SceneManagement; // For scene load callbacks
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
    public int zoneGroupSize = 2; // remains local grouping, zone count via manager

    // --- Private Fields ---
    private readonly List<GameObject> activeChunks = new List<GameObject>();
    private Transform cam;
    private Vector3 forwardDir;
    private bool canCheckSpawning = false;
    // Zone Group Tracking
    private int chunksInCurrentGroup = 0;
    private int zonesInCurrentGroup = 0;
    // Temp list for building placement checks within the current chunk
    private List<Bounds> currentChunkBuildingBounds = new List<Bounds>(); // Changed from List<float>


    // Initialize or reset level generation (spawn start and initial chunks)
    private void InitializeLevel()
    {
        // Acquire camera and forward direction
        cam = Camera.main?.transform;
        forwardDir = GameSettings.ForwardDirection;

        // Validate critical prefabs
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

        // Clear any existing chunks
        foreach (var chunk in activeChunks) if (chunk != null) Destroy(chunk);
        activeChunks.Clear();
        chunksInCurrentGroup = 0;
        zonesInCurrentGroup = 0;

        // Spawn start chunk
        Vector3 startPos = Vector3.zero;
        GameObject startInstance = Instantiate(startChunkPrefab, startPos, Quaternion.identity, chunkParent);
        activeChunks.Add(startInstance);
        Transform lastEnd = FindEndPosition(startInstance);
        if (lastEnd == null) return;

        // Spawn additional initial chunks
        for (int i = 0; i < initialChunks; i++)
        {
            if (chunkPrefabs == null || chunkPrefabs.Count == 0) break;
            int idx = Random.Range(0, chunkPrefabs.Count);
            var prefab = chunkPrefabs[idx];
            GameObject newChunk = SpawnChunk(prefab, lastEnd.position);
            if (newChunk == null) continue;
            lastEnd = FindEndPosition(newChunk);
            if (lastEnd == null)
            {
                Debug.LogError($"LevelGenerator: Spawned chunk '{newChunk.name}' is missing EndPosition. Stopping initial spawn.", newChunk);
                break;
            }
        }
        canCheckSpawning = true;
    }

    void Start()
    {
        // Initial scene load
        InitializeLevel();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reinitialize level on scene reload
        InitializeLevel();
    }

    void Update()
    {
        // Reacquire camera if lost and ensure spawning enabled
        if (cam == null)
        {
            cam = Camera.main?.transform;
        }
        if (cam == null || !canCheckSpawning)
        {
            return;
        }
        // Remove destroyed chunk entries
        activeChunks.RemoveAll(chunk => chunk == null);
        if (activeChunks.Count == 0) return;

        float camDist = Vector3.Dot(cam.position, forwardDir);

        // 1) Spawn ahead
        var lastChunk = activeChunks[activeChunks.Count - 1];
        // Guard against destroyed or missing transform
        if (lastChunk == null || lastChunk.transform == null) return;
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
        if (firstChunk == null || firstChunk.transform == null) return;
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
            // Clear bounds list before processing a new chunk
            currentChunkBuildingBounds.Clear();
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
        int maxObs = DifficultyManager.Instance.GetMaxObstaclesPerChunk();
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0 || maxObs <= 0) return;

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
        int maxAttempts = maxObs * 5; // Allow more attempts for rejection sampling

        for (int attempt = 0; attempt < maxAttempts && obstaclesSpawned < maxObs; attempt++)
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

            // Check for overlap with any existing DeliveryZone to give zones spawn priority
            Collider2D[] overlapHits = Physics2D.OverlapPointAll(potentialSpawnPoint);
            if (overlapHits.Any(c => c.GetComponentInParent<DeliveryZone>() != null))
            {
                continue; // Skip spawning obstacle here to avoid zone overlap
            }
            
            Instantiate(obstaclePrefab, potentialSpawnPoint, Quaternion.identity, chunkInstance.transform);
            obstaclesSpawned++;
        }
    }

    // Renamed from original SpawnBuildingsAndZonesOnChunk to avoid confusion
    private void SpawnBuildingsAndZonesOnChunk(GameObject roadChunkInstance)
    {
        // This method now just calls SpawnBuildingsInArea for top and bottom
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Top", topBuildingPrefabs, true);
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Bottom", bottomBuildingPrefabs, false);
    }

    private void SpawnBuildingsInArea(GameObject roadChunkInstance, string areaName, List<GameObject> buildingPrefabs, bool hugMinY)
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
        int candidatesToFind = 3; // How many random points to test inside the area
        int maxPlacementAttemptsPerCandidate = 10; // How many tries to find a point inside the polygon
        int maxBuildingAttempts = maxBuildingsPerArea * 5; // Total attempts to place buildings in this area

        for (int buildingAttempt = 0; buildingAttempt < maxBuildingAttempts && buildingsSpawnedInArea < maxBuildingsPerArea; buildingAttempt++)
        {
            GameObject buildingPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            // --- CHANGE: Look for Collider2D in children ---
            Collider2D prefabCollider = buildingPrefab.GetComponentInChildren<Collider2D>(); // Changed from GetComponent

            if (prefabCollider == null)
            {
                // Updated warning message
                Debug.LogWarning($"Building prefab '{buildingPrefab.name}' or its children are missing a Collider2D needed for bounds calculation. Skipping.", buildingPrefab);
                continue;
            }
            // Note: prefabCollider.bounds is in world space relative to prefab's origin (0,0,0) if prefab is not in scene.
            // We only need the size here, which should be correct regardless of position.
            Vector3 prefabSize = prefabCollider.bounds.size;

            List<Vector3> validCandidates = new List<Vector3>();

            // Find Multiple Valid Candidate Positions using Rejection Sampling
            for (int candidateNum = 0; candidateNum < candidatesToFind; candidateNum++)
            {
                for (int placementAttempt = 0; placementAttempt < maxPlacementAttemptsPerCandidate; placementAttempt++)
                {
                    float randomX = Random.Range(areaBounds.min.x, areaBounds.max.x);
                    float randomY = Random.Range(areaBounds.min.y, areaBounds.max.y);
                    // Use Z from area center, assuming buildings are placed flat relative to the area
                    Vector3 testPos = new Vector3(randomX, randomY, areaBounds.center.z);

                    // Check if the center point is inside the polygon collider
                    if (areaCollider.OverlapPoint(testPos))
                    {
                        validCandidates.Add(testPos);
                        break; // Found one candidate position, move to next candidate
                    }
                }
            }

            if (validCandidates.Count == 0) continue; // No valid points found in the area, try next building attempt

            // Select the Best Candidate (Closest to Road Edge - lowest Y for Top, highest Y for Bottom)
            Vector3 bestPos = validCandidates[0];
            if (hugMinY) // Top Area: Prefer LOWEST Y
            {
                for (int i = 1; i < validCandidates.Count; i++) { if (validCandidates[i].y < bestPos.y) bestPos = validCandidates[i]; }
            }
            else // Bottom Area: Prefer HIGHEST Y
            {
                for (int i = 1; i < validCandidates.Count; i++) { if (validCandidates[i].y > bestPos.y) bestPos = validCandidates[i]; }
            }

            // --- Spacing Check using Bounds ---
            Bounds potentialBounds = new Bounds(bestPos, prefabSize);
            Bounds checkBounds = potentialBounds;
            checkBounds.Expand(minBuildingSpacing); // Expand the area to check for clearance

            bool tooClose = false;
            foreach (Bounds existingBounds in currentChunkBuildingBounds)
            {
                if (checkBounds.Intersects(existingBounds))
                {
                    tooClose = true;
                    Debug.Log($"[Building Placement] Position {bestPos} for {buildingPrefab.name} too close to existing building at {existingBounds.center}. Spacing failed.");
                    break;
                }
            }
            if (tooClose) continue; // Skip if too close, try next building attempt

            // --- Instantiate ---
            GameObject buildingInstance = Instantiate(buildingPrefab, bestPos, Quaternion.identity, roadChunkInstance.transform);
            // --- CHANGE: Look for Collider2D in children of the instance ---
            Collider2D instanceCollider = buildingInstance.GetComponentInChildren<Collider2D>(); // Changed from GetComponent

            // Record the actual bounds of the placed building
            if (instanceCollider != null)
            {
                // instanceCollider.bounds is now correctly in world space for the instantiated object
                currentChunkBuildingBounds.Add(instanceCollider.bounds);
                Debug.Log($"[Building Placement] Placed {buildingInstance.name} at {bestPos}. Recorded bounds: {instanceCollider.bounds}");
            }
            else
            {
                 // Updated warning message
                 Debug.LogWarning($"Placed building '{buildingInstance.name}' or its children have no Collider2D to record bounds.", buildingInstance);
                 // Optionally add bounds based on prefab size as a fallback:
                 // currentChunkBuildingBounds.Add(potentialBounds);
            }

            buildingsSpawnedInArea++;

            // Try Activate Delivery Zone
            TryActivateDeliveryZone(buildingInstance);
        }
    }


    private void TryActivateDeliveryZone(GameObject buildingInstance)
    {
        // Trigger delivery zone based on chance and current inventory colors
        if (Random.value <= DifficultyManager.Instance.GetDeliveryChance())
         {
            int maxZones = DifficultyManager.Instance.GetMaxZonesPerGroup();
            if (zonesInCurrentGroup >= maxZones) return; // Group limit hit

            InventoryManager invManager = InventoryManager.Instance;
            if (invManager == null)
            {
                Debug.LogWarning("TryActivateDeliveryZone: InventoryManager not found!", buildingInstance);
                return;
            }

            var currentInventory = invManager.GetInventorySlots();
            var availableColors = currentInventory
                .Where(slot => slot?.itemData != null)
                .Select(slot => slot.itemData.itemColor)
                .Distinct()
                .ToList();

            if (availableColors.Count == 0)
            {
                Debug.Log("[TryActivateDeliveryZone] No unique item colors in inventory. Cannot activate zone.");
                return;
            }

            Transform zonePlaceholder = buildingInstance.transform.Find("DeliveryZone_Placeholder");
            if (zonePlaceholder != null)
            {
                DeliveryZone zoneScript = zonePlaceholder.GetComponent<DeliveryZone>();
                if (zoneScript != null)
                {
                    Color chosenColor = availableColors[Random.Range(0, availableColors.Count)];
                    zoneScript.ActivateZone(chosenColor);
                    zonesInCurrentGroup++;
                    Debug.Log($"[TryActivateDeliveryZone] Activated Zone on {buildingInstance.name} with color {chosenColor}. Zones in group: {zonesInCurrentGroup}/{maxZones}");
                }
                else
                {
                    Debug.LogWarning("TryActivateDeliveryZone: DeliveryZone component missing on placeholder!", zonePlaceholder.gameObject);
                }
            }
        }
    }

    // Helper methods for chunk alignment
    private Transform FindStartPosition(GameObject obj)
    {
        return obj.transform.Find("StartPosition");
    }

    private Transform FindEndPosition(GameObject obj)
    {
        return obj.transform.Find("EndPosition");
    }
}