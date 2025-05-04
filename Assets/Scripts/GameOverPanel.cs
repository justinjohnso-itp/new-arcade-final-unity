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
    [Tooltip("The final score text element (for the current session)")] // Clarified tooltip
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [Tooltip("The high score text element")] // Added
    [SerializeField] private TextMeshProUGUI highScoreText; // Added
    [Tooltip("Text prompting the user to play again")]
    [SerializeField] private TextMeshProUGUI playAgainText;

    [Header("Settings")]
    [Tooltip("Delay before showing prompt and starting auto-return timer (seconds)")]
    [SerializeField] private float initialDelay = 1.5f;
    [Tooltip("Time before automatically returning to title screen if no input (seconds)")]
    [SerializeField] private float autoReturnTime = 5.0f;
    [Tooltip("Name of the main game scene")]
    [SerializeField] private string gameSceneName = "MainGame";
    [Tooltip("Name of the title screen scene")]
    [SerializeField] private string titleSceneName = "TitleScreen";

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMagnitude = 0.15f;
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private float rotationMagnitude = 5.0f;

    private ScoreManager scoreManager;
    private PlayerController playerController;
    private float currentAutoReturnTimer = 0f;
    private bool isCountingDown = false;
    private bool isLoadingScene = false;
    private float originalGameOverScale;
    private Quaternion originalGameOverRotation;

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
        if (highScoreText == null) // Added check
            Debug.LogWarning("GameOverPanel Awake: High Score Text not assigned!");

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
    /// Displays the game over panel with the final score and high score.
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

        // --- ScoreManager Check & Score Display --- 
        if (scoreManager == null)
        {
            Debug.LogError("GameOverPanel ShowGameOverPanel: ScoreManager is NULL. Attempting to find it again...");
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (scoreManager != null)
        {
            int currentScore = scoreManager.GetCurrentScore();
            int highScore = scoreManager.GetHighScore(); // Get high score from manager

            if (finalScoreText != null)
            {
                finalScoreText.text = $"Score: {currentScore}"; // Updated format
                Debug.Log($"GameOverPanel ShowGameOverPanel: Set final score text to: {finalScoreText.text}");
            }
            else
            {
                Debug.LogWarning("GameOverPanel ShowGameOverPanel: Final Score Text field missing.");
            }

            if (highScoreText != null)
            {
                highScoreText.text = $"High Score: {highScore}"; // Set high score text
                Debug.Log($"GameOverPanel ShowGameOverPanel: Set high score text to: {highScoreText.text}");
            }
            else
            {
                Debug.LogWarning("GameOverPanel ShowGameOverPanel: High Score Text field missing.");
            }
        }
        else
        {
            Debug.LogError("GameOverPanel ShowGameOverPanel: ScoreManager not found! Cannot display scores.");
            if (finalScoreText != null) finalScoreText.text = "Score: ???";
            if (highScoreText != null) highScoreText.text = "High Score: ???";
        }
        // --- End Score Display ---

        // Start the initial delay before activating the countdown/prompt
        Debug.Log($"GameOverPanel ShowGameOverPanel: Invoking StartCountdown after {initialDelay} seconds.");
        Invoke(nameof(StartCountdown), initialDelay);
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
        AudioManager.Instance?.PlayUIClickSound();
        Debug.Log($"GameOverPanel: Loading game scene '{gameSceneName}'");
        // Stop any game music before reloading
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Loads the title screen scene.
    /// </summary>
    public void GoToTitleScreen()
    {
        if (isLoadingScene) return; // Prevent double load
        isLoadingScene = true;
        AudioManager.Instance?.PlayUIClickSound();
        Debug.Log($"GameOverPanel: Loading title scene '{titleSceneName}'");
        // Stop any game music before loading title
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene(titleSceneName);
    }
}
