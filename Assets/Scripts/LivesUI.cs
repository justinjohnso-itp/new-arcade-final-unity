using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Displays the player's current lives in the UI.
/// Handles visual feedback for gaining/losing lives.
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

    [Header("Color Settings")]
    [Tooltip("Default color for the lives text")]
    [SerializeField] private Color defaultColor = Color.white;
    [Tooltip("Color when lives are at 2")]
    [SerializeField] private Color warningColor = new Color(1.0f, 0.75f, 0.1f); // Lighter Orange
    [Tooltip("Color when lives are at 1")]
    [SerializeField] private Color criticalColor = Color.red;
    [Tooltip("Color to flash when gaining an extra life")]
    [SerializeField] private Color extraLifeFlashColor = Color.blue;
    [Tooltip("Duration of the extra life color flash")]
    [SerializeField] private float flashDuration = 0.5f;


    private TextMeshProUGUI livesText;
    private PlayerController playerController;
    private int previousLives = -1; // To detect when a life is lost
    private Coroutine shakeCoroutine = null; // To manage the shake animation
    private Coroutine flashCoroutine = null; // To manage the flash animation
    private Vector3 originalHeartPosition; // Store original position
    private Color currentBaseColor; // Store the correct color based on lives

    void Awake()
    {
        livesText = GetComponent<TextMeshProUGUI>();
        if (livesText == null)
        {
            Debug.LogError("LivesUI: TextMeshProUGUI component not found!", this);
            this.enabled = false;
            return; // Added return to prevent further execution if component is missing
        }
        if (heartIcon != null)
        {
            originalHeartPosition = heartIcon.rectTransform.anchoredPosition;
        }
        currentBaseColor = defaultColor; // Initialize base color
        livesText.color = currentBaseColor; // Set initial text color
    }

    void Start()
    {
        // Find player controller
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("LivesUI: PlayerController not found in scene!", this);
            if (livesText != null) // Check if livesText is valid before using
            {
                livesText.text = "Lives: ?";
            }
            return; // Stop initialization if player not found
        }

        // Subscribe to events
        playerController.OnLivesChanged += UpdateLivesText;
        playerController.OnGameOver += HandleGameOver;
        playerController.OnExtraLifeGained += HandleExtraLifeGained; // Subscribe to new event

        // Initialize previous lives and update text
        previousLives = playerController.GetCurrentLives();
        UpdateLivesText(previousLives); // Initial update
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerController != null)
        {
            playerController.OnLivesChanged -= UpdateLivesText;
            playerController.OnGameOver -= HandleGameOver;
            playerController.OnExtraLifeGained -= HandleExtraLifeGained; // Unsubscribe from new event
        }
        // Stop coroutines if object is destroyed
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
    }

    /// <summary>
    /// Updates the lives counter in the UI and sets text color.
    /// </summary>
    private void UpdateLivesText(int lives)
    {
        if (livesText == null) return;
        livesText.text = $"Lives: {lives}";

        // Determine the base color based on lives count
        if (lives == 2)
        {
            currentBaseColor = warningColor;
        }
        else if (lives == 1)
        {
            currentBaseColor = criticalColor;
        }
        else
        {
            currentBaseColor = defaultColor;
        }

        // Apply the base color (unless a flash is active)
        if (flashCoroutine == null)
        {
             livesText.color = currentBaseColor;
        }
        // else: the flash coroutine will handle restoring the color

        // Check if a life was lost and heart icon exists
        if (heartIcon != null && lives < previousLives && lives >= 0) // Added check for lives >= 0
        {
            // Stop any existing shake animation before starting a new one
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                // Reset immediately before starting new shake
                ResetHeartTransform();
            }
            shakeCoroutine = StartCoroutine(AnimateHeartLoss());
        }

        // Update previous lives *after* comparison
        previousLives = lives;
    }

    /// <summary>
    /// Handles the event when an extra life is gained.
    /// </summary>
    private void HandleExtraLifeGained()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashColor(extraLifeFlashColor, flashDuration));
    }

    /// <summary>
    /// Coroutine to briefly flash the text color.
    /// </summary>
    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (livesText == null) yield break; // Exit if text component is missing

        livesText.color = flashColor;
        yield return new WaitForSeconds(duration);
        livesText.color = currentBaseColor; // Restore to the correct base color
        flashCoroutine = null; // Mark coroutine as finished
    }


    /// <summary>
    /// Handles the game over event (e.g., disable UI updates).
    /// </summary>
    private void HandleGameOver()
    {
        // Optional: Add any specific behavior needed when the game ends
        // For example, stop animations or disable interaction
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            ResetHeartTransform(); // Ensure heart is reset on game over
        }
         if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            livesText.color = currentBaseColor; // Ensure color is reset
        }
        // Consider disabling the component or further updates if needed
        // this.enabled = false;
    }

    /// <summary>
    /// Coroutine to animate the heart icon shaking.
    /// </summary>
    private IEnumerator AnimateHeartLoss()
    {
        if (heartIcon == null) yield break; // Exit if no heart icon

        float elapsed = 0f;
        Quaternion startRotation = heartIcon.rectTransform.localRotation;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / shakeDuration;

            // Calculate shake offset using Perlin noise for smoother randomness
            float offsetX = (Mathf.PerlinNoise(Time.time * 20f, 0f) * 2f - 1f) * shakeIntensity * (1f - percentComplete);
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * 20f) * 2f - 1f) * shakeIntensity * (1f - percentComplete);
            heartIcon.rectTransform.anchoredPosition = originalHeartPosition + new Vector3(offsetX, offsetY, 0f);

            // Calculate random rotation
            float randomZ = Random.Range(-maxRotation, maxRotation) * (1f - percentComplete);
            heartIcon.rectTransform.localRotation = startRotation * Quaternion.Euler(0, 0, randomZ);

            yield return null; // Wait for the next frame
        }

        // Reset position and rotation after shaking
        ResetHeartTransform();
        shakeCoroutine = null; // Mark coroutine as finished
    }

    /// <summary>
    /// Resets the heart icon's position and rotation to their original values.
    /// </summary>
    private void ResetHeartTransform()
    {
        if (heartIcon != null)
        {
            heartIcon.rectTransform.anchoredPosition = originalHeartPosition;
            heartIcon.rectTransform.localRotation = Quaternion.identity;
        }
    }
}
