using UnityEngine;
using System.Collections.Generic; // Required for Lists

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
    // Temporary list for building placement checks within a single chunk
    private List<float> currentChunkBuildingPositionsX = new List<float>();


    void Start()
    {
        cam = Camera.main.transform;
        forwardDir = GameSettings.ForwardDirection;

        // --- Validate Prefabs --- 
        if (startChunkPrefab == null)
        {
            Debug.LogError("LevelGenerator: Start Chunk Prefab is not assigned!");
            this.enabled = false; // Disable component if setup is invalid
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
            Debug.LogWarning("LevelGenerator: No random chunk prefabs assigned. Only the start chunk will be spawned initially.");
            // Allow continuing with only the start chunk
        }
        // Optionally add checks for Start/End position in random prefabs here too

        // --- Spawn Start Chunk --- 
        Vector3 currentSpawnPosition = Vector3.zero; // Start at origin
        GameObject startInstance = Instantiate(startChunkPrefab, currentSpawnPosition, Quaternion.identity, chunkParent);
        activeChunks.Add(startInstance);
        Transform lastEndPosition = FindEndPosition(startInstance);
        if (lastEndPosition == null) { /* Already checked, but defensive */ return; }

        // --- Spawn Additional Initial Chunks --- 
        for (int i = 0; i < initialChunks; i++)
        {
            if (chunkPrefabs == null || chunkPrefabs.Count == 0) break; // No random prefabs to spawn

            // Select a random prefab from the list
            int idx = Random.Range(0, chunkPrefabs.Count);
            GameObject prefabToSpawn = chunkPrefabs[idx];

            // Spawn the next chunk aligned to the previous one's end position
            GameObject newChunkInstance = SpawnChunk(prefabToSpawn, lastEndPosition.position);

            if (newChunkInstance != null)
            {
                lastEndPosition = FindEndPosition(newChunkInstance); // Update for the next iteration
                if (lastEndPosition == null)
                {
                    Debug.LogError($"LevelGenerator: Spawned chunk '{newChunkInstance.name}' is missing EndPosition. Stopping initial spawn.", newChunkInstance);
                    break;
                }
            }
            else
            {
                Debug.LogWarning($"LevelGenerator: Failed to spawn initial random chunk (Prefab: {prefabToSpawn.name}). Check prefab setup.");
                // Decide if we should stop or just skip this one
                // break; // Uncomment to stop if any random chunk fails
            }
        }

        // --- Enable Update checks ---
        canCheckSpawning = true; // Allow Update to start checking spawn/destroy conditions

        // Initialize zone counters
        chunksInCurrentGroup = 0;
        zonesInCurrentGroup = 0;
    }

    void Update()
    {
        // Wait until Start has finished initial setup and there are chunks
        if (!canCheckSpawning || activeChunks.Count == 0) return; 

        // project camera and chunks onto forwardDir to get "distance along path"
        float camDist = Vector3.Dot(cam.position, forwardDir);

        // 1) Spawn ahead when camera nears end of last chunk
        var lastChunk = activeChunks[activeChunks.Count - 1];
        if (lastChunk == null) return; // Safety check

        float lastDist = Vector3.Dot(lastChunk.transform.position, forwardDir);

        bool shouldSpawn = camDist >= lastDist - spawnTriggerDistance;

        if (shouldSpawn) // Use the calculated boolean
        {
            Transform lastEndPos = FindEndPosition(lastChunk);
            if (lastEndPos != null)
            {
                if (chunkPrefabs != null && chunkPrefabs.Count > 0)
                {
                    // Select a random prefab and spawn it
                    int idx = Random.Range(0, chunkPrefabs.Count);
                    GameObject prefabToSpawn = chunkPrefabs[idx];
                    GameObject spawnedInUpdate = SpawnChunk(prefabToSpawn, lastEndPos.position);
                }
                // else: No random prefabs defined, do nothing
            }
            else
            {
                 Debug.LogError($"LevelGenerator: Last active chunk '{lastChunk.name}' is missing 'EndPosition' child. Cannot spawn next chunk.", lastChunk);
            }
        }

        // 2) Destroy behind when chunk falls far behind camera
        var firstChunk = activeChunks[0]; // Use FirstOrDefault for safety
        if (firstChunk == null) return;

        float firstDist = Vector3.Dot(firstChunk.transform.position, forwardDir);

        if (firstDist < camDist - destroyDistanceBehind)
        {
            Destroy(firstChunk);
            activeChunks.RemoveAt(0);
        }
    }

    // Spawns a specific prefab, aligning its StartPosition to the targetAlignmentPosition.
    private GameObject SpawnChunk(GameObject prefabToSpawn, Vector3 targetAlignmentPosition)
    {
        // --- Validate Prefab ---
        Transform startPosTransform = FindStartPosition(prefabToSpawn);
        Transform endPosTransform = FindEndPosition(prefabToSpawn); // Check EndPosition on prefab too

        if (startPosTransform == null || endPosTransform == null)
        {
             Debug.LogError($"LevelGenerator: Prefab '{prefabToSpawn.name}' is missing StartPosition or EndPosition. Skipping spawn.", prefabToSpawn);
             return null;
        }

        // --- Calculate Spawn Position ---
        // Calculate offset from prefab root to StartPosition explicitly
        // This handles nested StartPosition objects correctly.
        Vector3 startOffset = prefabToSpawn.transform.InverseTransformPoint(startPosTransform.position);

        // The core alignment calculation: Spawn root so that StartPosition lands on target
        Vector3 spawnPosition = targetAlignmentPosition - startOffset;

        // --- Instantiate ---
        var chunkInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, chunkParent);
        activeChunks.Add(chunkInstance);

        // --- Spawn Obstacles ---
        SpawnObstaclesOnChunk(chunkInstance);

        // --- Spawn Buildings & Activate Zones (Only for Road chunks) ---
        // Check if the spawned chunk is a "Road" type (e.g., by name or tag - using name here)
        if (prefabToSpawn.name.Contains("LevelPart_Road")) // Adjust check if needed
        {
            SpawnBuildingsAndZonesOnChunk(chunkInstance);

            // --- Update Zone Group Counters ---
            chunksInCurrentGroup++;
            if (chunksInCurrentGroup >= zoneGroupSize)
            {
                // Reset group for the next set of chunks
                // Debug.Log($"Zone Group Reset. Zones in last group: {zonesInCurrentGroup}");
                chunksInCurrentGroup = 0;
                zonesInCurrentGroup = 0;
            }
        }

        return chunkInstance;
    }

    private void SpawnObstaclesOnChunk(GameObject chunkInstance)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0 || maxObstaclesPerChunk <= 0)
        {
            return; // No obstacles defined or spawning disabled
        }

        // Find the designated spawn area within the chunk instance
        Transform spawnAreaTransform = chunkInstance.transform.Find("ObstacleSpawnArea");
        if (spawnAreaTransform == null)
        {
            Debug.LogWarning($"Chunk '{chunkInstance.name}' is missing 'ObstacleSpawnArea' child. Cannot spawn obstacles.", chunkInstance);
            return;
        }

        // --- MODIFIED: Look for PolygonCollider2D ---
        PolygonCollider2D spawnBoundsCollider = spawnAreaTransform.GetComponent<PolygonCollider2D>();
        if (spawnBoundsCollider == null)
        {
            // --- MODIFIED: Updated warning message ---
            Debug.LogWarning($"'ObstacleSpawnArea' on chunk '{chunkInstance.name}' is missing PolygonCollider2D. Cannot determine spawn bounds.", chunkInstance);
            return;
        }

        Bounds spawnBounds = spawnBoundsCollider.bounds; // Still use AABB for initial random range

        int obstaclesSpawned = 0;
        int maxAttempts = maxObstaclesPerChunk * 5; // Increase attempts for rejection sampling
        for (int attempt = 0; attempt < maxAttempts && obstaclesSpawned < maxObstaclesPerChunk; attempt++)
        {
            // Select a random obstacle
            int obstacleIndex = Random.Range(0, obstaclePrefabs.Count);
            GameObject obstaclePrefab = obstaclePrefabs[obstacleIndex];

            // Calculate random spawn point within the AABB
            float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
            float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);
            // Assuming obstacles are placed flat on the Z=0 plane relative to the chunk.
            // If your ground has varying height, this Z calculation might need adjustment.
            Vector3 potentialSpawnPoint = new Vector3(randomX, randomY, spawnBounds.center.z);

            // --- Check if the point is INSIDE the PolygonCollider2D ---
            if (!spawnBoundsCollider.OverlapPoint(potentialSpawnPoint))
            {
                // Point is outside the polygon, try again
                continue;
            }

            // --- Optional: Overlap Check (to prevent obstacles spawning on top of each other) ---
            // Consider the size of the obstacle prefab when checking radius
            // float checkRadius = obstaclePrefab.GetComponent<Collider2D>()?.bounds.extents.magnitude ?? 1.0f;
            // Collider2D overlap = Physics2D.OverlapCircle(potentialSpawnPoint, checkRadius); // Add LayerMask if needed
            // if (overlap != null && overlap.transform != spawnAreaTransform) // Ensure we don't overlap with the area itself
            // {
            //     // Debug.Log($"Obstacle spawn point {potentialSpawnPoint} overlapped with {overlap.name}. Skipping.");
            //     continue; // Try another position
            // }
            // --- End Optional Overlap Check ---


            // Instantiate the obstacle
            Instantiate(obstaclePrefab, potentialSpawnPoint, Quaternion.identity, chunkInstance.transform); // Parent to the chunk
            obstaclesSpawned++;
        }

        if (obstaclesSpawned < maxObstaclesPerChunk)
        {
            // Optional: Log if we couldn't spawn the desired number after many attempts
            // Debug.Log($"Could only spawn {obstaclesSpawned}/{maxObstaclesPerChunk} obstacles in {chunkInstance.name} after {maxAttempts} attempts.");
        }
    }

    private void SpawnBuildingsAndZonesOnChunk(GameObject roadChunkInstance)
    {
        currentChunkBuildingPositionsX.Clear(); // Reset for this chunk

        // --- Spawn Top Buildings ---
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Top", "Wall_Bottom", topBuildingPrefabs, true);

        // --- Spawn Bottom Buildings ---
        SpawnBuildingsInArea(roadChunkInstance, "BuildingSpawnArea_Bottom", "Wall_Top", bottomBuildingPrefabs, false);
    }

    private void SpawnBuildingsInArea(GameObject roadChunkInstance, string areaName, string wallToHugName, List<GameObject> buildingPrefabs, bool hugMinY) // hugMinY = true for Top (hug bottom wall), false for Bottom (hug top wall)
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
        int candidatesToFind = 3; // Restore finding multiple candidates
        int maxPlacementAttemptsPerCandidate = 10; // Attempts to find *one* valid point
        int maxBuildingAttempts = maxBuildingsPerArea * 5; 

        for (int buildingAttempt = 0; buildingAttempt < maxBuildingAttempts && buildingsSpawnedInArea < maxBuildingsPerArea; buildingAttempt++)
        {
            // 1. Select Building Prefab
            GameObject buildingPrefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            List<Vector3> validCandidates = new List<Vector3>(); // Restore candidate list

            // 2. Find Multiple Valid Candidate Positions using Rejection Sampling
            for (int candidateNum = 0; candidateNum < candidatesToFind; candidateNum++)
            {
                for (int placementAttempt = 0; placementAttempt < maxPlacementAttemptsPerCandidate; placementAttempt++)
                {
                    // Generate random point within the AABB
                    float randomX = Random.Range(areaBounds.min.x, areaBounds.max.x);
                    float randomY = Random.Range(areaBounds.min.y, areaBounds.max.y);
                    Vector3 testPos = new Vector3(randomX, randomY, areaBounds.center.z);

                    // Check if the point is inside the actual collider shape
                    if (areaCollider.OverlapPoint(testPos))
                    {
                        validCandidates.Add(testPos); // Add valid point to list
                        break; // Found a candidate, move to the next candidateNum
                    }
                }
            }

            if (validCandidates.Count == 0)
            {
                // Debug.Log($"Could not find any valid placement points in {areaName} after multiple attempts.");
                continue; 
            }

            // 3. Select the Best Candidate (Closest to Road Edge - REVISED LOGIC)
            Vector3 bestPos = validCandidates[0];

            if (hugMinY) // Top Area: Prefer LOWEST Y (closest to road below)
            {
                for (int i = 1; i < validCandidates.Count; i++)
                {
                    if (validCandidates[i].y < bestPos.y)
                    {
                        bestPos = validCandidates[i];
                    }
                }
            }
            else // Bottom Area: Prefer HIGHEST Y (closest to road above)
            {
                for (int i = 1; i < validCandidates.Count; i++)
                {
                    if (validCandidates[i].y > bestPos.y)
                    {
                        bestPos = validCandidates[i];
                    }
                }
            }

            // --- Optional: Offset based on building size/pivot ---
            // If buildings still visually clip outside, you might need an offset.
            // This depends heavily on your building prefab pivot points.
            // Example: If pivot is centered, offset by half height towards center of area.
            // Collider2D buildingCollider = buildingPrefab.GetComponent<Collider2D>();
            // float yOffset = (buildingCollider?.bounds.size.y ?? 1f) * 0.5f;
            // bestPos.y += hugMinY ? -yOffset : yOffset; // Push away from the edge slightly
            // ---

            // 4. Check Spacing 
            bool tooClose = false;
            foreach (float existingX in currentChunkBuildingPositionsX)
            {
                if (Mathf.Abs(bestPos.x - existingX) < minBuildingSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
            {
                // Debug.Log($"Best building spawn pos {bestPos} too close to existing X. Skipping.");
                continue; 
            }

            // --- Optional Overlap Check ---
            // Vector2 buildingSize = buildingPrefab.GetComponent<Collider2D>()?.bounds.size ?? Vector2.one;
            // Collider2D overlap = Physics2D.OverlapBox(bestPos, buildingSize * 0.9f, 0f); // Check slightly smaller box
            // if (overlap != null && overlap.transform.IsChildOf(roadChunkInstance.transform) && overlap != areaCollider)
            // {
            //     // Debug.Log($"Building spawn {bestPos} overlapped with {overlap.name}. Skipping.");
            //     continue;
            // }
            // ---

            // 5. Instantiate at the chosen best position
            GameObject buildingInstance = Instantiate(buildingPrefab, bestPos, Quaternion.identity, roadChunkInstance.transform);
            currentChunkBuildingPositionsX.Add(bestPos.x); 
            buildingsSpawnedInArea++;

            // 6. Try Activate Delivery Zone
            TryActivateDeliveryZone(buildingInstance);
        }
    }


    private void TryActivateDeliveryZone(GameObject buildingInstance)
    {
        if (zonesInCurrentGroup >= maxZonesPerGroup)
        {
            // Debug.Log("Max zones for group reached.");
            return; // Group limit hit
        }

        // --- Check Inventory for Available Colors ---
        InventoryManager invManager = InventoryManager.Instance;
        if (invManager == null)
        {
            Debug.LogWarning("TryActivateDeliveryZone: InventoryManager not found!", buildingInstance);
            return; // Cannot determine required color
        }

        List<InventorySlotData> currentInventory = invManager.GetInventorySlots();
        List<Color> availableColors = new List<Color>();
        Debug.Log($"[TryActivateDeliveryZone] Checking inventory. Slot count: {currentInventory.Count}"); // Log: Inventory size
        foreach (InventorySlotData slotData in currentInventory)
        {
            if (slotData != null && slotData.itemData != null)
            {
                Color itemColor = slotData.itemData.itemColor;
                Debug.Log($"[TryActivateDeliveryZone] Found item: {slotData.itemData.itemName}, Color: {itemColor}"); // Log: Item and color
                if (!availableColors.Contains(itemColor))
                {
                    availableColors.Add(itemColor);
                    Debug.Log($"[TryActivateDeliveryZone] Added unique color: {itemColor}"); // Log: Adding unique color
                }
            }
        }

        Debug.Log($"[TryActivateDeliveryZone] Total unique available colors: {availableColors.Count}"); // Log: Final unique color count

        if (availableColors.Count == 0)
        {
            Debug.Log("[TryActivateDeliveryZone] No unique item colors found in inventory. Cannot activate zone.");
            return; // Cannot activate zone if no items/colors are available
        }

        // --- Activation Logic ---
        if (Random.value <= deliveryZoneActivationChance)
        {
            // Find the placeholder child
            Transform zonePlaceholder = buildingInstance.transform.Find("DeliveryZone_Placeholder"); // Use exact name
            if (zonePlaceholder != null)
            {
                DeliveryZone zoneScript = zonePlaceholder.GetComponent<DeliveryZone>();
                if (zoneScript == null)
                {
                    // Add the component if it's missing (should be on the prefab ideally)
                    Debug.LogWarning($"DeliveryZone component missing on placeholder for {buildingInstance.name}. Adding it.", buildingInstance);
                    zoneScript = zonePlaceholder.gameObject.AddComponent<DeliveryZone>();
                    // Potentially add/configure SpriteRenderer reference here if needed
                }

                // Pick a random color from the ones available in the inventory
                Color chosenColor = availableColors[Random.Range(0, availableColors.Count)];
                Debug.Log($"[TryActivateDeliveryZone] Randomly chosen color: {chosenColor}"); // Log: Chosen color

                // Activate the zone with the chosen color
                zoneScript.ActivateZone(chosenColor);

                zonesInCurrentGroup++;
                Debug.Log($"[TryActivateDeliveryZone] Activated Delivery Zone on {buildingInstance.name} with color {chosenColor}. Zones in group: {zonesInCurrentGroup}/{maxZonesPerGroup}");
            }
            else
            {
                Debug.LogWarning($"Building {buildingInstance.name} spawned, but 'DeliveryZone_Placeholder' child not found!", buildingInstance);
            }
        }
        else
        {
            Debug.Log("[TryActivateDeliveryZone] Activation chance failed."); // Log: Activation chance failed
        }
    }

    // Helper function to find the EndPosition transform of an INSTANCE
    private Transform FindEndPosition(GameObject chunkInstance)
    {
        if (chunkInstance == null) return null;
        // Use Find which searches children recursively by default.
        return chunkInstance.transform.Find("EndPosition");
    }

    // Helper function to find the StartPosition transform within a PREFAB
    private Transform FindStartPosition(GameObject prefab)
    {
        if (prefab == null) return null;
        // Use Find which searches children recursively by default.
        return prefab.transform.Find("StartPosition");
    }
}