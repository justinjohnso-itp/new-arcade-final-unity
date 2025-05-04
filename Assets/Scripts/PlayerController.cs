using UnityEngine;
using System.Collections; // Required for hit animation coroutines
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum steering angle (degrees) when input is fully left/right.")] // Renamed and updated tooltip
    public float maxSteeringAngle = 20f; // Renamed and set to 20
    [Tooltip("How quickly the sprite rotates towards the target steering angle.")]
    public float rotationSpeed = 10f;

    [Header("Object References")]
    [Tooltip("Transform of the child GameObject containing the player's visuals.")]
    [SerializeField] private Transform spriteTransform; // Assign in Inspector

    [Header("Gameplay")]
    [Tooltip("Starting number of lives")]
    public int startingLives = 3;

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
    public event Action OnExtraLifeGained; // Added event for UI feedback

    private Rigidbody2D rb;
    private int currentLives;
    private bool hasPackage = false;
    private float packageSpawnTimer = 0f;
    private float currentRotationAngle = 0f;
    private ScoreManager scoreManager;
    private int nextLifeScoreThreshold = 1000; // Score needed for next extra life

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
        else
        {
            // Subscribe to score updates
            scoreManager.OnScoreChanged += HandleScoreChanged;
        }

        // Store the original local rotation of the sprite
        if (spriteTransform != null)
        {
            spriteOriginalLocalRotation = spriteTransform.localRotation;
        }
    }

    void OnDestroy() // Added OnDestroy to unsubscribe
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= HandleScoreChanged;
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
        // Use GetAxis for analog input (-1.0 to 1.0)
        float horizontalInput = Input.GetAxis("Horizontal");
        HandleRotation(horizontalInput);
        HandleMovement(horizontalInput);
    }

    private void HandleRotation(float horizontalInput)
    {
        if (spriteTransform == null) return;

        // Target angle based on analog input (negative input -> positive rotation)
        // Input ranges from -1.0 to 1.0, mapping directly to -maxSteeringAngle to +maxSteeringAngle
        float targetAngle = -horizontalInput * maxSteeringAngle;

        // LerpAngle smoothly interpolates towards the target angle.
        // When input is 0, targetAngle is 0, so it smoothly returns to center.
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


    // --- Life Management ---
    private void LoseLife()
    {
        if (currentLives <= 0) return;
        currentLives--;
        Debug.Log($"Player hit! Lives remaining: {currentLives}");
        OnLivesChanged?.Invoke(currentLives);

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    public void AddLife(int amount = 1) // Added method to gain lives
    {
        currentLives += amount;
        Debug.Log($"Gained {amount} life! Lives remaining: {currentLives}");
        OnLivesChanged?.Invoke(currentLives);
        // Note: OnExtraLifeGained is invoked from HandleScoreChanged
    }

    private void HandleScoreChanged(int newScore) // Added handler for score changes
    {
        if (newScore >= nextLifeScoreThreshold)
        {
            AddLife();
            OnExtraLifeGained?.Invoke(); // Trigger UI feedback event
            nextLifeScoreThreshold += 1000; // Set threshold for the next one
            Debug.Log($"Extra life granted! Next threshold: {nextLifeScoreThreshold}");
        }
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");

        // --- Save High Score ---
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveHighScore();
        }
        else
        {
            Debug.LogWarning("GameOver: ScoreManager instance not found. Cannot save high score.");
        }
        // ---------------------

        // Trigger game over event for other systems (like GameOverPanel)
        OnGameOver?.Invoke();

        // --- Removed Scene Reload --- 
        // The GameOverPanel will now handle scene transitions (Restart or Title)
        // AudioManager.Instance?.StopMusic(); // Music stop handled by GameOverPanel transitions
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        // --------------------------

        // Optional: Disable player movement/input here if needed
        this.enabled = false; // Example: Disable the controller script
        rb.linearVelocity = Vector2.zero; // Stop movement (Use linearVelocity)
    }

    // --- Public Accessors ---
    public int GetCurrentLives()
    {
        return currentLives;
    }

    public bool HasPackage()
    {
        return hasPackage;
    }

    // Coroutine for sprite shake animation
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
