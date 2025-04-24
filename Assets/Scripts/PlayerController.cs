using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal (lateral) movement speed")]
    public float horizontalSpeed = 5f;
    [Tooltip("Automatic forward movement speed")]
    public float forwardSpeed = 5f;
    [Tooltip("Adjust forward direction slightly along the isometric Y axis (-1 to 1)")]
    [Range(-1f, 1f)]
    public float lateralAdjustment = 0f;

    [Header("Gameplay")]
    [Tooltip("Starting number of lives")]
    public int startingLives = 3;
    [Tooltip("Time (seconds) between automatic package spawns.")]
    public float packageSpawnInterval = 10f; // E.g., new package every 10 seconds

    private Rigidbody2D rb;
    private int currentLives; // Internal counter
    private bool hasPackage = false; // Does the player currently have a package?
    private float packageSpawnTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLives = startingLives;
        Debug.Log($"Player starting with {currentLives} lives.");
        hasPackage = false; // Start without a package
        packageSpawnTimer = packageSpawnInterval; // Start timer ready for first spawn
        UpdatePackageVisual(); // Update visual state initially
    }

    void Update() // Use Update for timer logic, not physics-dependent
    {
        // --- Package Spawning Timer ---
        if (!hasPackage)
        {
            packageSpawnTimer -= Time.deltaTime;
            if (packageSpawnTimer <= 0f)
            {
                ReceivePackage();
            }
        }
    }

    void FixedUpdate()
    {
        // --- Use GetAxisRaw for immediate input ---
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector2 forwardDir = GameSettings.ForwardDirection;
        Vector2 rightDir = GameSettings.IsometricYDirection;

        // Apply lateral adjustment to forward direction
        Vector2 adjustedForwardDir = forwardDir + rightDir * lateralAdjustment * 0.2f;
        adjustedForwardDir.Normalize();

        // Calculate the INTENDED direction vector based on inputs
        Vector2 forwardComponent = adjustedForwardDir; // Direction only
        // Scale lateral input relative to forward speed for consistent feel
        Vector2 lateralComponent = rightDir * horizontalInput * (horizontalSpeed / forwardSpeed);

        // Combine direction components
        Vector2 desiredDirection = (forwardComponent + lateralComponent).normalized;

        // Apply the constant forward speed to the final direction
        if (desiredDirection.sqrMagnitude > 0.01f) // Avoid normalizing zero vector if no input
        {
             rb.linearVelocity = desiredDirection * forwardSpeed;
        }
        else // If somehow input is zero, just move forward
        {
             rb.linearVelocity = adjustedForwardDir * forwardSpeed;
        }
    }

    // --- Collision & Trigger Handling ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Log all collisions
        Debug.Log($"Player collided with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        // Check if we collided with an obstacle
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Obstacle obstacle = collision.gameObject.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                // Calculate approximate hit direction from player center to contact point
                Vector2 hitDirection = (collision.contacts[0].point - (Vector2)transform.position).normalized;

                // Tell the obstacle it was hit
                obstacle.HandleHit(hitDirection);

                // Decrease player lives
                LoseLife();
            }
        }
        // Add other collision checks here if needed (e.g., for delivery zones)
    }

    // --- Package Handling Methods ---

    public bool HasPackage() // Public method for DeliveryZone to check
    {
        return hasPackage;
    }

    private void ReceivePackage()
    {
        if (hasPackage) return; // Already have one

        Debug.Log("Package received!");
        hasPackage = true;
        packageSpawnTimer = packageSpawnInterval; // Reset timer for next potential spawn
        UpdatePackageVisual();

        // TODO: Add sound effect for receiving package
    }

    // Called by DeliveryZone after successful delivery
    public void OnPackageDelivered()
    {
        if (!hasPackage) return; // Shouldn't happen if logic is correct

        Debug.Log("Player confirmed package delivered.");
        hasPackage = false;
        // Timer starts counting down automatically in Update()
        UpdatePackageVisual();

        // TODO: Add sound effect for delivery confirmation
    }

    private void UpdatePackageVisual()
    {
        // TODO: Implement visual change on player sprite/model
        // e.g., enable/disable a child GameObject representing the package
        // Transform packageVisual = transform.Find("PackageSprite");
        // if (packageVisual != null)
        // {
        //     packageVisual.gameObject.SetActive(hasPackage);
        // }
        Debug.Log($"Player Has Package: {hasPackage}"); // Placeholder visual
    }


    private void LoseLife()
    {
        if (currentLives <= 0) return; // Already game over

        currentLives--;
        Debug.Log($"Player hit! Lives remaining: {currentLives}");

        // TODO: Add visual/audio feedback for losing a life (e.g., flash sprite, sound effect)

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER!");
        // TODO: Implement actual game over logic (e.g., stop movement, show UI, load menu scene)
        rb.linearVelocity = Vector2.zero; // Stop movement
        this.enabled = false; // Disable player controller script
    }
}
