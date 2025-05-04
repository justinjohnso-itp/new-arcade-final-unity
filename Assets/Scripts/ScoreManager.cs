using UnityEngine;
using System;

/// <summary>
/// Manages the player's score and high score, providing events for UI updates.
/// Implements a Singleton pattern.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static ScoreManager Instance { get; private set; }

    // --- High Score ---
    private const string HighScoreKey = "HighScore"; // Key for PlayerPrefs
    private int highScore = 0;
    public event Action<int> OnHighScoreChanged; // Event for high score updates

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject);

            // Load high score when the manager initializes
            LoadHighScore();
        }
    }
    // -- End Singleton Pattern --

    [Header("Score Settings")]
    [SerializeField] private int currentScore = 0;

    // Event triggered when the score changes
    public event Action<int> OnScoreChanged;

    void Start()
    {
        // Trigger initial events for UI setup
        OnScoreChanged?.Invoke(currentScore);
        OnHighScoreChanged?.Invoke(highScore); // Trigger high score update too
    }

    /// <summary>
    /// Adds the specified amount to the current score and updates high score if needed.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"Score changed by {amount}. New score: {currentScore}");
        OnScoreChanged?.Invoke(currentScore);

        // Check and update high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            OnHighScoreChanged?.Invoke(highScore);
            Debug.Log($"New High Score: {highScore}");
            // Note: High score is saved explicitly on game over, not every time it changes.
        }
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Gets the current high score.
    /// </summary>
    public int GetHighScore()
    {
        return highScore;
    }

    /// <summary>
    /// Resets the current score to zero. Does not reset the high score.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log("Current score reset to 0.");
    }

    /// <summary>
    /// Loads the high score from PlayerPrefs.
    /// </summary>
    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0); // Default to 0 if not found
        Debug.Log($"Loaded High Score: {highScore}");
    }

    /// <summary>
    /// Saves the current high score to PlayerPrefs.
    /// Should be called when the game ends or at appropriate checkpoints.
    /// </summary>
    public void SaveHighScore()
    {
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save(); // Ensure data is written to disk
        Debug.Log($"Saved High Score: {highScore}");
    }

    /// <summary>
    /// Resets the high score to 0 both in memory and in PlayerPrefs.
    /// </summary>
    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();
        OnHighScoreChanged?.Invoke(highScore); // Notify listeners (like UI)
        Debug.Log("High Score has been reset to 0.");
    }
}
