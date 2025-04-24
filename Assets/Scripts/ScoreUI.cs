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
            Debug.LogError("ScoreUI: TextMeshProUGUI component not found!", this);
            this.enabled = false;
        }
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
            // Update text with initial score
            UpdateScoreText(ScoreManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogError("ScoreUI: ScoreManager instance not found!", this);
            scoreText.text = "Score Error";
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }

    /// <summary>
    /// Updates the score text display.
    /// </summary>
    private void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
    }
}
