using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal (lateral) movement speed")]
    public float horizontalSpeed = 5f;
    [Tooltip("Automatic forward movement speed")]
    public float forwardSpeed = 5f;
    [Tooltip("Maximum sprite rotation angle (degrees) when steering.")]
    public float maxRotationAngle = 15f;
    [Tooltip("How quickly the sprite rotates towards the target angle.")]
    public float rotationSpeed = 10f;

    [Header("Object References")]
    [Tooltip("Transform of the child GameObject containing the player's visuals.")]
    [SerializeField] private Transform spriteTransform; // Assign in Inspector

    [Header("Gameplay")]
    [Tooltip("Starting number of lives")]
    public int startingLives = 3;
    [Tooltip("Time (seconds) between automatic package spawns.")]
    public float packageSpawnInterval = 10f;

    private Rigidbody2D rb;
    private int currentLives;
    private bool hasPackage = false;
    private float packageSpawnTimer = 0f;
    private float currentRotationAngle = 0f;
    private ScoreManager scoreManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLives = startingLives;
        Debug.Log($"Player starting with {currentLives} lives.");
        hasPackage = false;
        packageSpawnTimer = packageSpawnInterval;
        UpdatePackageVisual();

        if (spriteTransform == null)
        {
            Debug.LogWarning("PlayerController: Sprite Transform not assigned! Rotation disabled.", this);
        }

        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogWarning("PlayerController: ScoreManager not found! Score penalties disabled.", this);
        }
    }

    void Update()
    {
        // Package Spawning Timer
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
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        HandleRotation(horizontalInput);
        HandleMovement(horizontalInput);
    }

    private void HandleRotation(float horizontalInput)
    {
        if (spriteTransform == null) return;

        // Target angle based on input (negative input -> positive rotation)
        float targetAngle = -horizontalInput * maxRotationAngle;
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetAngle, Time.fixedDeltaTime * rotationSpeed);
        spriteTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotationAngle);
    }

    private void HandleMovement(float horizontalInput)
    {
        Vector2 forwardDir = GameSettings.ForwardDirection; // Isometric "up-right"
        // Perpendicular screen-right direction relative to forward scroll
        Vector2 screenRightDir = new Vector2(forwardDir.y, -forwardDir.x).normalized;

        Vector2 forwardVelocity = forwardDir * forwardSpeed;
        Vector2 lateralVelocity = screenRightDir * horizontalInput * horizontalSpeed;

        // Combine velocities directly
        rb.linearVelocity = forwardVelocity + lateralVelocity;
    }


    // --- Collision & Trigger Handling ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Player collided with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Obstacle obstacle = collision.gameObject.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                Vector2 hitDirection = (collision.contacts[0].point - (Vector2)transform.position).normalized;
                obstacle.HandleHit(hitDirection);
                LoseLife();

                // Apply score penalty
                if (scoreManager != null)
                {
                    scoreManager.AddScore(-5);
                }
            }
        }
    }

    // --- Package Handling Methods ---

    public bool HasPackage()
    {
        return hasPackage;
    }

    private void ReceivePackage()
    {
        if (hasPackage) return;
        Debug.Log("Package received!");
        hasPackage = true;
        packageSpawnTimer = packageSpawnInterval; // Reset timer
        UpdatePackageVisual();
        // TODO: Add sound effect
    }

    // Called by DeliveryZone
    public void OnPackageDelivered()
    {
        if (!hasPackage) return;
        Debug.Log("Player confirmed package delivered.");
        hasPackage = false;
        UpdatePackageVisual();
        // TODO: Add sound effect
    }

    private void UpdatePackageVisual()
    {
        // TODO: Implement visual change on player sprite
        // e.g., enable/disable a child GameObject
        Debug.Log($"Player Has Package: {hasPackage}"); // Placeholder
    }


    private void LoseLife()
    {
        if (currentLives <= 0) return;
        currentLives--;
        Debug.Log($"Player hit! Lives remaining: {currentLives}");
        // TODO: Add visual/audio feedback

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER!");
        // TODO: Implement game over logic (stop movement, show UI, etc.)
        rb.linearVelocity = Vector2.zero;
        this.enabled = false;
    }
}
