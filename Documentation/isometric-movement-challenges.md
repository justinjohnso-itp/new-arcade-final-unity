# Log: Basic Movement, Chunk Spawning, and Refining Steering

Okay, starting to get the core systems in place for the New Arcade infinite scroller. The first thing I needed was *some* kind of player movement, even super basic, just so I could test the procedural level generation. I threw together a quick script to just move the player forward at a constant speed along the isometric axis. Didn't even bother with steering yet.

```csharp
// Very early PlayerController.cs snippet (conceptual)
void FixedUpdate()
{
    // Assume forwardSpeed is set and ForwardDirection comes from GameSettings
    rb.linearVelocity = GameSettings.ForwardDirection * forwardSpeed;
}
```

Even this super basic movement required accounting for the isometric perspective. Unlike a standard top-down or side-scrolling game where "up" might be `Vector2(0, 1)` and "right" is `Vector2(1, 0)`, in our 2:1 isometric view, "forward" (visually up-right) and "right" (visually down-right) correspond to diagonal vectors in world space. That's why I set up `GameSettings` early on to define these directions:

```csharp
// GameSettings.cs - Isometric axes (defined earlier)
static GameSettings()
{
    // Standard isometric projection uses 2:1 ratio (x:y)
    Vector2 isoXAxis = new Vector2(2f, 1f).normalized; // Our visual "forward"
    Vector2 isoYAxis = new Vector2(2f, -1f).normalized; // Our visual "right"
    ForwardDirection = isoXAxis;
    IsometricYDirection = isoYAxis;
}
```
So, even just moving straight forward meant using `GameSettings.ForwardDirection` instead of a simple `Vector2.up` or `Vector2.right`.

With that minimal movement in place, I could tackle the "infinite" part: chunk spawning. Since the game follows a fixed path like Paperboy, a chunk-based system made sense. The plan is to spawn pre-designed level sections (chunks) ahead of the player.

I followed Code Monkey's tutorial on endless runner level generation ([https://www.youtube.com/watch?v=NtY_R0g8L8E](https://www.youtube.com/watch?v=NtY_R0g8L8E)) to get the basic structure. It involves:
1.  A `LevelGenerator` script to manage spawning.
2.  Trigger zones at the end of each chunk prefab.
3.  The player triggering the next spawn when they hit the end zone.

The `LevelGenerator` instantiates the next chunk at the connection point of the current one. I also added a list to keep track of active chunks, though I haven't implemented despawning old ones yet.

```csharp
// Simplified concept in LevelGenerator.cs
public class LevelGenerator : MonoBehaviour
{
    public GameObject[] levelChunks; // Prefabs
    public Transform playerTransform;
    private Vector3 lastSpawnPosition;
    private List<GameObject> activeChunks = new List<GameObject>();

    void Start()
    {
        // Initial spawn logic...
        SpawnChunk();
    }

    public void SpawnNextChunk(Vector3 connectionPoint)
    {
        GameObject nextChunkPrefab = levelChunks[Random.Range(0, levelChunks.Length)];
        GameObject newChunk = Instantiate(nextChunkPrefab, connectionPoint, Quaternion.identity);
        activeChunks.Add(newChunk);
        // TODO: Implement despawning old chunks
    }
}

// Attached to the trigger at the end of a chunk prefab
public class ChunkEndTrigger : MonoBehaviour
{
    public LevelGenerator levelGenerator;
    public Transform connectionPoint; // Where the next chunk should attach

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Assuming player has "Player" tag
        {
            levelGenerator.SpawnNextChunk(connectionPoint.position);
            gameObject.SetActive(false); // Disable trigger after use
        }
    }
}
```

Getting this basic generation loop working felt good. The world now builds itself ahead of the player.

---

**Revisiting Movement: Adding Steering & Fixing Speed Issues**

Now that chunks were spawning, I needed to make the player movement more interactive by adding steering using the arcade cabinet's wheel. This is where the non-perpendicular nature of those isometric axes in world space really started causing problems.

As soon as I added lateral movement based on input (using `GameSettings.IsometricYDirection` as the base for "right"), I noticed the speed inconsistency: steering left slowed the truck down overall, steering right sped it up. This wasn't right; the forward speed needs to be consistent (and eventually ramp up).

My hunch was it was related to the isometric math. Our `GameSettings` defines the axes based on the 2:1 projection:

```csharp
// GameSettings.cs - Isometric axes
static GameSettings()
{
    Vector2 isoXAxis = new Vector2(2f, 1f).normalized; // Forward
    Vector2 isoYAxis = new Vector2(2f, -1f).normalized; // Right (Perpendicular in iso view)
    ForwardDirection = isoXAxis;
    IsometricYDirection = isoYAxis;
}
```
These axes aren't perpendicular in world space. So, simply adding the `forwardMotion` vector and the `lateralMotion` vector resulted in a combined vector whose magnitude changed depending on the steering angle.

My first attempt to fix this was just normalizing the final combined vector:

```csharp
// PlayerController.cs - Attempt 1 (Incorrect)
Vector2 forwardMotion = adjustedForwardDir * forwardSpeed;
Vector2 lateralMotion = rightDir * horizontalInput * horizontalSpeed;
Vector2 combinedNaive = forwardMotion + lateralMotion;
rb.linearVelocity = combinedNaive.normalized * forwardSpeed; // Forces magnitude
```

This kept the *overall* speed constant, but it killed the *forward* momentum when steering. The truck slowed its progress along the road, which felt wrong.

I needed a way to maintain constant speed *along the intended forward axis* while allowing lateral steering. I recalled seeing techniques using the dot product for this kind of problem. The idea is to calculate how the naive combination of forward and lateral motion projects onto the *actual* forward direction, and then adjust *only* the forward component to compensate.

The steps are:
1.  Calculate the naive combined velocity (`forwardMotion + lateralMotion`).
2.  Project this naive velocity onto the *true* forward direction using `Vector2.Dot` to find the current actual forward speed component (`currentProjectedForwardSpeed`).
3.  Calculate how much to scale *only* the `forwardMotion` component (`speedAdjustment = forwardSpeed / currentProjectedForwardSpeed`).
4.  Apply this scaled `forwardMotion` and add the original `lateralMotion` to get the final velocity.

```csharp
// PlayerController.cs - Relevant FixedUpdate logic (Corrected)
void FixedUpdate()
{
    // ... input reading, getting adjustedForwardDir/rightDir ...

    Vector2 forwardMotion = adjustedForwardDir * forwardSpeed;
    Vector2 lateralMotion = rightDir * horizontalInput * horizontalSpeed;

    if (horizontalInput != 0) // Only adjust if steering
    {
        // Project the naive combined velocity onto the forward direction
        float currentProjectedForwardSpeed = Vector2.Dot(forwardMotion + lateralMotion, adjustedForwardDir);

        // Calculate adjustment factor (avoid division by zero)
        float speedAdjustment = (currentProjectedForwardSpeed != 0) ? forwardSpeed / currentProjectedForwardSpeed : 1f;

        // Apply adjustment ONLY to forward motion, then add lateral
        Vector2 velocity = (forwardMotion * speedAdjustment) + lateralMotion;
        rb.linearVelocity = velocity;
    }
    else
    {
        // No steering, just use base forward motion
        rb.linearVelocity = forwardMotion;
    }
}
```

This feels much better in testing. The forward speed stays consistent along the road, and steering adds lateral movement without the weird speed changes. Getting these two core systems (chunk spawning and predictable movement) working is a big step towards having something playable for testing next week.