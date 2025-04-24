using UnityEngine;
using System;

/// <summary>
/// Manages the player's score and provides events for UI updates.
/// Implements a simple Singleton pattern.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // --- Singleton Pattern --- 
    public static ScoreManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Keep persistent across scenes if needed
            // DontDestroyOnLoad(gameObject);
        }
    }
    // -- End Singleton Pattern --

    [Header("Score Settings")]
    [SerializeField] private int currentScore = 0;

    // Event triggered when the score changes, passing the new score
    public event Action<int> OnScoreChanged;

    void Start()
    {
        // Initialize score (optional, could start at 0)
        // currentScore = 0; 
        // Trigger initial event for UI setup
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Adds the specified amount to the current score.
    /// </summary>
    /// <param name="amount">The amount to add (can be negative).</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"Score changed by {amount}. New score: {currentScore}");
        // Trigger the event to notify listeners (like the UI)
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    // Optional: Method to reset score
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log("Score reset to 0.");
    }
}
