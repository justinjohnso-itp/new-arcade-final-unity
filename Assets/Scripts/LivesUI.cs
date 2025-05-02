using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // Added for Coroutines

/// <summary>
/// Displays the player's current lives in the UI.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LivesUI : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Optional Image component to display heart icon")]
    [SerializeField] private Image heartIcon;
    [Tooltip("Duration of the heart shake animation (seconds)")]
    [SerializeField] private float shakeDuration = 0.3f;
    [Tooltip("Intensity of the heart shake (pixels)")]
    [SerializeField] private float shakeIntensity = 5f;
    [Tooltip("Maximum random rotation during shake (degrees)")]
    [SerializeField] private float maxRotation = 15f;

    private TextMeshProUGUI livesText;
    private PlayerController playerController;
    private int previousLives = -1; // To detect when a life is lost
    private Coroutine shakeCoroutine = null; // To manage the animation
    private Vector3 originalHeartPosition; // Store original position

    void Awake()
    {
        livesText = GetComponent<TextMeshProUGUI>();
        if (livesText == null)
        {
            Debug.LogError("LivesUI: TextMeshProUGUI component not found!", this);
            this.enabled = false;
        }
        if (heartIcon != null)
        {
            originalHeartPosition = heartIcon.rectTransform.anchoredPosition;
        }
    }

    void Start()
    {
        // Find player controller
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("LivesUI: PlayerController not found in scene!", this);
            livesText.text = "Lives: ?";
            return;
        }

        // Subscribe to events
        playerController.OnLivesChanged += UpdateLivesText;
        playerController.OnGameOver += HandleGameOver;

        // Initialize previous lives
        if (playerController != null)
        {
            previousLives = playerController.GetCurrentLives();
            UpdateLivesText(previousLives); // Initial update
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerController != null)
        {
            playerController.OnLivesChanged -= UpdateLivesText;
            playerController.OnGameOver -= HandleGameOver;
        }
        // Stop coroutine if object is destroyed
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
    }

    /// <summary>
    /// Updates the lives counter in the UI.
    /// </summary>
    private void UpdateLivesText(int lives)
    {
        if (livesText == null) return;
        livesText.text = $"Lives: {lives}";

        // Check if a life was lost and heart icon exists
        if (heartIcon != null && lives < previousLives)
        {
            // Stop any existing shake animation before starting a new one
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                // Reset immediately before starting new shake
                heartIcon.rectTransform.anchoredPosition = originalHeartPosition;
                heartIcon.rectTransform.localRotation = Quaternion.identity;
            }
            shakeCoroutine = StartCoroutine(AnimateHeartLoss());
        }

        previousLives = lives; // Update previous lives count

        // Update heart icon color (optional visual feedback)
        if (heartIcon != null)
        {
            // Change color based on remaining lives
            if (lives <= 0)
                heartIcon.color = Color.red;
            else if (lives == 1)
                heartIcon.color = new Color(1f, 0.5f, 0f); // Orange
            else
                heartIcon.color = Color.white;
        }
    }

    /// <summary>
    /// Coroutine to animate the heart icon shaking and rotating.
    /// </summary>
    private IEnumerator AnimateHeartLoss()
    {
        float elapsedTime = 0f;
        Quaternion originalRotation = heartIcon.rectTransform.localRotation;

        while (elapsedTime < shakeDuration)
        {
            // Calculate random shake offset
            Vector3 randomOffset = Random.insideUnitCircle * shakeIntensity;
            heartIcon.rectTransform.anchoredPosition = originalHeartPosition + randomOffset;

            // Calculate random rotation
            float randomZRotation = Random.Range(-maxRotation, maxRotation);
            heartIcon.rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, randomZRotation);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Reset position and rotation after animation
        heartIcon.rectTransform.anchoredPosition = originalHeartPosition;
        heartIcon.rectTransform.localRotation = originalRotation;
        shakeCoroutine = null; // Mark coroutine as finished
    }

    /// <summary>
    /// Called when the game is over.
    /// </summary>
    private void HandleGameOver()
    {
        if (livesText == null) return;
        livesText.color = Color.red;
        livesText.text = "GAME OVER";

        if (heartIcon != null)
        {
            heartIcon.gameObject.SetActive(false);
        }
        // Stop animation if game ends
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            // Ensure reset if game over happens mid-animation
            if (heartIcon != null)
            {
                heartIcon.rectTransform.anchoredPosition = originalHeartPosition;
                heartIcon.rectTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
