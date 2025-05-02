using UnityEngine;
using System.Collections; // Required for hit animation coroutines
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")] // dynamic speeds now from DifficultyManager )
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
    // Package spawn interval now driven by DifficultyManager

    [Header("Hit Feedback")]
    [Tooltip("Duration of the sprite shake animation (seconds)")]
    [SerializeField] private float hitShakeDuration = 0.3f;
    [Tooltip("Intensity of sprite shake (units)")]
    [SerializeField] private float hitShakeIntensity = 0.2f;
    [Tooltip("Maximum random rotation during sprite shake (degrees)")]
    [SerializeField] private float hitMaxRotation = 15f;
    [Tooltip("Factor to reduce movement speeds by during hit (1 = no slowdown)")]
    [SerializeField] private float hitSlowFactor = 1f;

    // Events
    public event Action<int> OnLivesChanged;
    public event Action OnGameOver;

    private Rigidbody2D rb;
    private int currentLives;
    private bool hasPackage = false;
    private float packageSpawnTimer = 0f;
    private float currentRotationAngle = 0f;
    private ScoreManager scoreManager;

    private Quaternion spriteOriginalLocalRotation;
    private Coroutine spriteHitCoroutine;

    void Start()
    {
        // Play Game Music when the player controller starts in the game scene
        AudioManager.Instance?.PlayGameMusic();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentLives = startingLives;
        Debug.Log($"Player starting with {currentLives} lives.");
        hasPackage = false;
        // Initialize spawn timer via DifficultyManager
        packageSpawnTimer = DifficultyManager.Instance.GetRandomAddDelay();
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

        // Store the original local rotation of the sprite
        if (spriteTransform != null)
        {
            spriteOriginalLocalRotation = spriteTransform.localRotation;
        }
    }

    void Update()
    {
        // Package spawn timer updated per frame
        if (!hasPackage)
        {
            packageSpawnTimer -= Time.deltaTime;
            if (packageSpawnTimer <= 0f)
                ReceivePackage();
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
        Vector2 screenRightDir = new Vector2(forwardDir.y, -forwardDir.x).normalized;

        // Dynamic speeds from DifficultyManager, apply slowdown factor if shaking
        float factor = (spriteHitCoroutine != null ? hitSlowFactor : 1f);
        float currentForward = DifficultyManager.Instance.GetForwardSpeed() * factor;
        float currentHorizontal = DifficultyManager.Instance.GetHorizontalSpeed() * factor;
        Vector2 forwardVelocity = forwardDir * currentForward;
        Vector2 lateralVelocity = screenRightDir * horizontalInput * currentHorizontal;

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

                // Play collision sound via Singleton
                AudioManager.Instance?.PlayCollisionSound();

                // Apply score penalty
                if (scoreManager != null)
                {
                    scoreManager.AddScore(-5);
                }

                // Shake the sprite to indicate hit
                if (spriteTransform != null && spriteHitCoroutine == null)
                {
                    spriteHitCoroutine = StartCoroutine(ShakeSprite(hitShakeDuration));
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
        // Spawn debug no longer needed
        // Debug.Log("Package received!");
        hasPackage = true;
        // Reset timer based on current difficulty
        packageSpawnTimer = DifficultyManager.Instance.GetRandomAddDelay();
        UpdatePackageVisual();
        AudioManager.Instance?.PlayPickupSound();
    }

    // Called by DeliveryZone
    public void OnPackageDelivered()
    {
        if (!hasPackage) return;
        Debug.Log("Player confirmed package delivered.");
        hasPackage = false;
        UpdatePackageVisual();
        // Delivery sound handled by InventoryManager based on correctness
    }

    private void UpdatePackageVisual()
    {
        // TODO: Implement visual change on player sprite
        // e.g., enable/disable a child GameObject
        // Spawn debug removed
        // Debug.Log($"Player Has Package: {hasPackage}"); // Placeholder
    }


    private void LoseLife()
    {
        if (currentLives <= 0) return;
        currentLives--;
        Debug.Log($"Player hit! Lives remaining: {currentLives}");
        
        // Notify subscribers that lives have changed
        OnLivesChanged?.Invoke(currentLives);
        
        // TODO: Add visual/audio feedback
        
        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    /// <summary>
    /// Returns the current number of lives the player has.
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER!");

        // Stop movement
        rb.linearVelocity = Vector2.zero;
        this.enabled = false;

        // --- Add log before invoking --- 
        Debug.Log("PlayerController: About to invoke OnGameOver event.");
        // Notify subscribers that game is over
        OnGameOver?.Invoke();

        // Game object remains in scene so other scripts can check its state
    }

    private IEnumerator ShakeSprite(float duration)
    {
        float elapsed = 0f;
        Vector3 originalPos = spriteTransform.localPosition;
        Quaternion originalRot = spriteOriginalLocalRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * hitShakeIntensity;
            float angle = UnityEngine.Random.Range(-hitMaxRotation, hitMaxRotation);
            spriteTransform.localPosition = originalPos + (Vector3)offset;
            spriteTransform.localRotation = originalRot * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        spriteTransform.localPosition = originalPos;
        spriteTransform.localRotation = originalRot;
        spriteHitCoroutine = null;
    }
}
