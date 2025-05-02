using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Game Over panel UI and transitions.
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The 'Game Over' text element")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [Tooltip("The final score text element")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [Tooltip("Text prompting the user to play again")]
    [SerializeField] private TextMeshProUGUI playAgainText;

    [Header("Settings")]
    [Tooltip("Delay before showing prompt and starting auto-return timer (seconds)")]
    [SerializeField] private float initialDelay = 1.5f; // Renamed from restartDelay
    [Tooltip("Time before automatically returning to title screen if no input (seconds)")]
    [SerializeField] private float autoReturnTime = 5.0f; // Added
    [Tooltip("Name of the main game scene")]
    [SerializeField] private string gameSceneName = "MainGame";
    [Tooltip("Name of the title screen scene")]
    [SerializeField] private string titleSceneName = "TitleScreen"; // Added

    [Header("Animation Settings")] // Added section
    [Tooltip("Speed of the pulsing animation")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [Tooltip("Magnitude of the scale pulse (e.g., 0.1 = 10%)")]
    [SerializeField] private float pulseMagnitude = 0.15f;
    [Tooltip("Speed of the rotation oscillation")]
    [SerializeField] private float rotationSpeed = 2.0f;
    [Tooltip("Maximum rotation angle (degrees)")]
    [SerializeField] private float rotationMagnitude = 5.0f;

    private ScoreManager scoreManager;
    private PlayerController playerController;
    private float currentAutoReturnTimer = 0f; // Added
    private bool isCountingDown = false; // Added
    private bool isLoadingScene = false; // Added
    private float originalGameOverScale; // Added
    private Quaternion originalGameOverRotation; // Added

    void Awake()
    {
        Debug.Log("GameOverPanel Awake: Initializing.");
        // Initially hide the panel - moved this check AFTER subscription attempt
        // gameObject.SetActive(false); 

        if (playAgainText != null)
            playAgainText.gameObject.SetActive(false); // Hide prompt initially
        else
            Debug.LogWarning("GameOverPanel Awake: Play Again Text not assigned!");

        if (finalScoreText == null)
            Debug.LogWarning("GameOverPanel Awake: Final Score Text not assigned!");

        // --- Moved Subscription Logic Here --- 
        Debug.Log("GameOverPanel Awake: Finding PlayerController.");
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("GameOverPanel Awake: PlayerController found. Subscribing to OnGameOver.");
            playerController.OnGameOver += ShowGameOverPanel;
        }
        else
        {
            Debug.LogError("GameOverPanel Awake: PlayerController not found! Panel subscription failed.", this);
        }
        // --- End Moved Logic --- 

        // Now ensure the panel starts inactive visually
        // Important: Do this AFTER attempting subscription
        gameObject.SetActive(false);
        Debug.Log($"GameOverPanel Awake: Panel active state set to: {gameObject.activeSelf}");

        // Store original scale and rotation for Game Over text
        if (gameOverText != null)
        {
            originalGameOverScale = gameOverText.transform.localScale.x;
            originalGameOverRotation = gameOverText.transform.localRotation;
        }
        else
        {
            Debug.LogWarning("GameOverPanel Awake: Game Over Text not assigned! Animation won't play.");
        }
    }

    void Start()
    {
        Debug.Log("GameOverPanel Start: Finding ScoreManager.");
        // Get reference to ScoreManager (can stay in Start)
        scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogWarning("GameOverPanel Start: ScoreManager not found! Score display might be incorrect.", this);
        }
    }

    void OnDestroy() // Good practice to unsubscribe
    {
        if (playerController != null)
        {
            Debug.Log("GameOverPanel OnDestroy: Unsubscribing from OnGameOver.");
            playerController.OnGameOver -= ShowGameOverPanel;
        }
    }

    void Update()
    {
        // Animate Game Over text only when the panel is active
        if (gameObject.activeSelf && gameOverText != null)
        {
            // Scale Pulse
            float pulse = 1.0f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
            gameOverText.transform.localScale = new Vector3(
                originalGameOverScale * pulse,
                originalGameOverScale * pulse,
                originalGameOverScale
            );

            // Rotation Oscillation
            float rotationAngle = Mathf.Sin(Time.time * rotationSpeed) * rotationMagnitude;
            gameOverText.transform.localRotation = originalGameOverRotation * Quaternion.Euler(0, 0, rotationAngle);
        }

        // --- Existing Countdown/Input Logic --- 
        if (isCountingDown && !isLoadingScene)
        {
            // Check for restart input
            if (Input.anyKeyDown)
            {
                Debug.Log("GameOverPanel Update: Input detected, restarting game.");
                isCountingDown = false; // Stop countdown
                RestartGame();
                return; // Exit update early
            }

            // Decrement timer
            currentAutoReturnTimer -= Time.deltaTime;

            // Check if timer expired
            if (currentAutoReturnTimer <= 0f)
            {
                Debug.Log("GameOverPanel Update: Auto-return timer expired, returning to title.");
                isCountingDown = false; // Stop countdown
                GoToTitleScreen();
            }
        }
    }

    /// <summary>
    /// Displays the game over panel with the final score.
    /// </summary>
    public void ShowGameOverPanel()
    {
        Debug.Log("GameOverPanel ShowGameOverPanel: Method called!");

        // Check if panel is already active (shouldn't happen but good check)
        if (gameObject.activeSelf)
        {
            Debug.LogWarning("GameOverPanel ShowGameOverPanel: Panel already active?");
            return;
        }

        Debug.Log("GameOverPanel ShowGameOverPanel: Activating panel GameObject.");
        gameObject.SetActive(true);

        // Check if activation worked
        if (!gameObject.activeSelf)
        {
            Debug.LogError("GameOverPanel ShowGameOverPanel: Failed to activate panel GameObject! Check parent Canvas?", this);
            return;
        }

        isLoadingScene = false; // Reset loading flag
        isCountingDown = false; // Ensure countdown isn't active yet

        // Play Game Over Sound via Singleton
        AudioManager.Instance?.PlayGameOverSound();

        // --- ScoreManager Check --- 
        if (scoreManager == null)
        {
            Debug.LogError("GameOverPanel ShowGameOverPanel: ScoreManager is NULL right before use. Attempting to find it again...");
            scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogError("GameOverPanel ShowGameOverPanel: Still couldn't find ScoreManager! Is it active in the scene?");
            }
            else
            {
                Debug.LogWarning("GameOverPanel ShowGameOverPanel: Found ScoreManager now, but it wasn't found in Start(). Check script execution order or activation timing.");
            }
        }
        // --- End ScoreManager Check ---

        // Update final score if ScoreManager is available
        if (scoreManager != null && finalScoreText != null)
        {
            // Ensure correct text format
            finalScoreText.text = $"HIGH SCORE: {scoreManager.GetCurrentScore()}";
            Debug.Log($"GameOverPanel ShowGameOverPanel: Set score text to: {finalScoreText.text}");
        }
        else
        {
            Debug.LogWarning("GameOverPanel ShowGameOverPanel: Could not set score text (ScoreManager reference missing or Text field missing?).");
            // Ensure correct text format for fallback
            if (finalScoreText != null) finalScoreText.text = "HIGH SCORE: ???";
        }

        // Start the initial delay before activating the countdown/prompt
        Debug.Log($"GameOverPanel ShowGameOverPanel: Invoking StartCountdown after {initialDelay} seconds.");
        Invoke(nameof(StartCountdown), initialDelay); // Changed target method
    }

    /// <summary>
    /// Called after the initial delay to show prompt and start the auto-return timer.
    /// </summary>
    private void StartCountdown() // Renamed from AllowRestart
    {
        if (isLoadingScene) return; // Don't start if already loading

        Debug.Log("GameOverPanel StartCountdown: Starting auto-return countdown.");
        currentAutoReturnTimer = autoReturnTime; // Reset timer
        isCountingDown = true; // Activate countdown logic in Update

        // Show the prompt text
        if (playAgainText != null)
        {
            playAgainText.gameObject.SetActive(true);
            if (!playAgainText.gameObject.activeSelf)
            {
                Debug.LogWarning("GameOverPanel StartCountdown: Failed to activate Play Again Text!");
            }
        }
        else
        {
            Debug.LogWarning("GameOverPanel StartCountdown: Play Again Text field is null.");
        }
    }

    /// <summary>
    /// Restart the current game scene.
    /// </summary>
    public void RestartGame()
    {
        if (isLoadingScene) return; // Prevent double load
        isLoadingScene = true;
        // Play UI Click Sound via Singleton
        AudioManager.Instance?.PlayUIClickSound();
        Debug.Log($"GameOverPanel: Loading game scene '{gameSceneName}'");
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Loads the title screen scene.
    /// </summary>
    public void GoToTitleScreen() // Added method
    {
        if (isLoadingScene) return; // Prevent double load
        isLoadingScene = true;
        Debug.Log($"GameOverPanel: Loading title scene '{titleSceneName}'");
        SceneManager.LoadScene(titleSceneName);
    }

    // Removed the old 'canRestart' boolean field
    // Note: The original AllowRestart method is replaced by StartCountdown
}
