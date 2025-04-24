using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal (lateral) movement speed")]
    public float horizontalSpeed = 5f;
    [Tooltip("Automatic forward movement speed")]
    public float forwardSpeed = 5f;
    [Tooltip("The maximum angle (degrees) the sprite will rotate when steering fully left or right.")]
    public float maxRotationAngle = 15f;
    [Tooltip("How quickly the sprite rotates towards the target angle.")]
    public float rotationSpeed = 10f;

    [Header("Object References")] // Added Header
    [Tooltip("The Transform of the child GameObject containing the player's sprite/visuals.")]
    [SerializeField] private Transform spriteTransform; // Assign in Inspector

    [Header("Gameplay")]
    [Tooltip("Starting number of lives")]
    public int startingLives = 3;
    [Tooltip("Time (seconds) between automatic package spawns.")]
    public float packageSpawnInterval = 10f; // E.g., new package every 10 seconds

    private Rigidbody2D rb;
    private int currentLives;
    private bool hasPackage = false;
    private float packageSpawnTimer = 0f;
    private float currentRotationAngle = 0f;
    private ScoreManager scoreManager; // Add reference to ScoreManager

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLives = startingLives;
        Debug.Log($"Player starting with {currentLives} lives.");
        hasPackage = false; // Start without a package
        packageSpawnTimer = packageSpawnInterval; // Start timer ready for first spawn
        UpdatePackageVisual(); // Update visual state initially

        if (spriteTransform == null)
        {
            Debug.LogWarning("PlayerController: Sprite Transform is not assigned! Rotation will not work.", this);
        }

        // Find ScoreManager
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogWarning("PlayerController: ScoreManager not found! Score penalties will not work.", this);
        }
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
        // --- Input ---
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // --- Rotation ---
        HandleRotation(horizontalInput);

        // --- Movement ---
        HandleMovement(horizontalInput);
    }

    private void HandleRotation(float horizontalInput)
    {
        if (spriteTransform == null) return; // Don't rotate if no sprite assigned

        // Calculate target angle: Negative input -> positive rotation, Positive input -> negative rotation
        float targetAngle = -horizontalInput * maxRotationAngle;

        // Smoothly interpolate towards the target angle
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetAngle, Time.fixedDeltaTime * rotationSpeed);

        // Apply the rotation (around Z axis for 2D)
        spriteTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotationAngle);
    }

    private void HandleMovement(float horizontalInput)
    {
        // --- Define Directions ---
        Vector2 forwardDir = GameSettings.ForwardDirection;
        // This is the isometric "up-right" direction on screen

        // Calculate the actual perpendicular "right" direction relative to the forward scroll
        // If forward is (x, y), perpendicular is (-y, x) or (y, -x).
        // We need the one pointing screen-right relative to forwardDir.
        // Original was (-y, x), which pointed screen-left. Correct is (y, -x).
        Vector2 screenRightDir = new Vector2(forwardDir.y, -forwardDir.x).normalized;

        // --- Calculate Velocity Components ---
        // Base forward velocity
        Vector2 forwardVelocity = forwardDir * forwardSpeed;

        // Lateral velocity based on input and screen-relative right direction
        Vector2 lateralVelocity = screenRightDir * horizontalInput * horizontalSpeed;

        // --- Combine Velocities ---
        // Directly add the components. Do NOT normalize here, as we want independent control over speeds.
        rb.linearVelocity = forwardVelocity + lateralVelocity;

        // --- Optional: Lateral Adjustment (Consider if still needed) ---
        // The previous 'lateralAdjustment' was tied to the isometric Y axis.
        // If you still want a slight drift up/down the isometric grid unrelated to steering,
        // you could add a small component based on GameSettings.IsometricYDirection here,
        // but it might feel less intuitive now that steering uses screenRightDir.
        // Example: rb.linearVelocity += GameSettings.IsometricYDirection * lateralAdjustment * someFactor;
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

                // Apply score penalty for hitting obstacle
                if (scoreManager != null)
                {
                    scoreManager.AddScore(-5); // Deduct 5 points
                }
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
