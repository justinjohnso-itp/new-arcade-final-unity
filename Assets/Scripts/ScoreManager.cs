using UnityEngine;
using System;

/// <summary>
/// Manages the player's score and provides events for UI updates.
/// Implements a Singleton pattern.
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
            // Optional: DontDestroyOnLoad(gameObject);
        }
    }
    // -- End Singleton Pattern --

    [Header("Score Settings")]
    [SerializeField] private int currentScore = 0;

    // Event triggered when the score changes
    public event Action<int> OnScoreChanged;

    void Start()
    {
        // Trigger initial event for UI setup
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Adds the specified amount to the current score.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"Score changed by {amount}. New score: {currentScore}");
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Resets the score to zero.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log("Score reset to 0.");
    }
}
