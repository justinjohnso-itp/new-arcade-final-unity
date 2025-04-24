using UnityEngine;
using TMPro; // Required for TextMeshPro

/// <summary>
/// Updates a TextMeshPro UI element to display the current score from ScoreManager.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI scoreText;

    void Awake()
    {
        scoreText = GetComponent<TextMeshProUGUI>();
        if (scoreText == null)
        {
            Debug.LogError("ScoreUI: TextMeshProUGUI component not found on this GameObject!", this);
            this.enabled = false;
        }
    }

    void Start()
    {
        // Subscribe to the score change event
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
            // Update text with initial score
            UpdateScoreText(ScoreManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogError("ScoreUI: ScoreManager instance not found!", this);
            // Optionally display an error state on the UI
            scoreText.text = "Score Error";
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks when this object is destroyed
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }

    /// <summary>
    /// Callback function to update the text when the score changes.
    /// </summary>
    /// <param name="newScore">The new score value.</param>
    private void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
        {
            // Format the score text (e.g., "Score: 100")
            scoreText.text = $"Score: {newScore}";
        }
    }
}
