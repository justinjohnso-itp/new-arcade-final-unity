using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // Added for Button
using System.Collections; // Added for Coroutine

/// <summary>
/// Controls the title screen UI and scene transitions.
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The main title text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Text prompting the user to start")]
    [SerializeField] private TextMeshProUGUI startPromptText;
    [Tooltip("Text displaying game instructions")]
    [SerializeField] private TextMeshProUGUI instructionsText;
    [Tooltip("The RectTransform of the truck sprite to animate")]
    [SerializeField] private RectTransform truckSpriteTransform;
    [Tooltip("Button used to reset the high score")]
    [SerializeField] private Button highScoreResetButton;
    [Tooltip("Text component of the high score reset button")]
    [SerializeField] private TextMeshProUGUI highScoreResetButtonText;

    [Header("Title Animation Settings")]
    [Tooltip("Speed of the pulsing animation")]
    [SerializeField] private float pulseSpeed = 1.5f; // Slightly faster
    [Tooltip("Magnitude of the scale pulse (e.g., 0.1 = 10%)")]
    [SerializeField] private float pulseMagnitude = 0.15f; // Increased magnitude
    [Tooltip("Speed of the rotation oscillation")]
    [SerializeField] private float rotationSpeed = 2.0f;
    [Tooltip("Maximum rotation angle (degrees)")]
    [SerializeField] private float rotationMagnitude = 5.0f;

    [Header("Truck Animation Settings")] // Added section
    [Tooltip("Intensity of the truck's vertical shake")]
    [SerializeField] private float truckShakeIntensity = 2.0f;
    [Tooltip("Speed of the truck's horizontal scale pulse")]
    [SerializeField] private float truckScalePulseSpeed = 5.0f;
    [Tooltip("Magnitude of the truck's horizontal scale pulse (e.g., 0.05 = 5%)")]
    [SerializeField] private float truckScalePulseMagnitude = 0.05f;

    [Header("Settings")]
    [Tooltip("Name of the main game scene to load")]
    [SerializeField] private string gameSceneName = "MainGame";
    [Tooltip("Duration for the reset button fade out effect")][SerializeField] private float resetButtonFadeDuration = 1.0f;

    private float originalTitleScale;
    private Quaternion originalTitleRotation;
    private bool isLoading = false;
    private Vector2 originalTruckPosition;
    private Vector3 originalTruckScale;
    private int highScoreResetClickCount = 0;
    private Color originalResetButtonColor;
    private Color originalResetTextColor;
    private Coroutine fadeCoroutine = null;

    void Start()
    {
        isLoading = false;
        // Ensure start prompt is visible
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(true);

        // Ensure instructions text is visible (content set in Editor)
        if (instructionsText != null)
        {
            instructionsText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("TitleScreenManager: Instructions Text not assigned!");
        }

        // Store original title scale and rotation for animation
        if (titleText != null)
        {
            originalTitleScale = titleText.transform.localScale.x;
            originalTitleRotation = titleText.transform.localRotation;
        }

        // Store original truck transform properties
        if (truckSpriteTransform != null)
        {
            originalTruckPosition = truckSpriteTransform.anchoredPosition;
            originalTruckScale = truckSpriteTransform.localScale;
        }
        else
        {
            Debug.LogWarning("TitleScreenManager: Truck Sprite Transform not assigned! Animation won't play.");
        }

        // Setup High Score Reset Button
        if (highScoreResetButton != null && highScoreResetButtonText != null)
        {
            // Store original colors
            originalResetButtonColor = highScoreResetButton.image.color;
            originalResetTextColor = highScoreResetButtonText.color;

            // Make initially transparent
            highScoreResetButton.image.color = new Color(originalResetButtonColor.r, originalResetButtonColor.g, originalResetButtonColor.b, 0f);
            highScoreResetButtonText.color = new Color(originalResetTextColor.r, originalResetTextColor.g, originalResetTextColor.b, 0f);
            highScoreResetButtonText.text = ""; // Clear initial text

            // Assign the click listener (ensure this method exists)
            highScoreResetButton.onClick.AddListener(HandleHighScoreResetClick);
        }
        else
        {
            Debug.LogWarning("TitleScreenManager: High Score Reset Button or its Text not assigned!");
        }

        // Play Title Music
        AudioManager.Instance?.PlayTitleMusic();
    }

    void Update()
    {
        // Animate title text
        if (titleText != null)
        {
            // Scale Pulse
            float pulse = 1.0f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
            titleText.transform.localScale = new Vector3(
                originalTitleScale * pulse,
                originalTitleScale * pulse,
                originalTitleScale
            );

            // Rotation Oscillation
            float rotationAngle = Mathf.Sin(Time.time * rotationSpeed) * rotationMagnitude;
            titleText.transform.localRotation = originalTitleRotation * Quaternion.Euler(0, 0, rotationAngle);
        }

        // Animate truck sprite
        if (truckSpriteTransform != null)
        {
            // Vertical Shake (using Perlin noise for smoother randomness)
            float shakeOffsetY = (Mathf.PerlinNoise(Time.time * 10f, 0f) * 2f - 1f) * truckShakeIntensity;
            truckSpriteTransform.anchoredPosition = originalTruckPosition + new Vector2(0, shakeOffsetY);

            // Horizontal Scale Pulse
            float scalePulseX = 1.0f + Mathf.Sin(Time.time * truckScalePulseSpeed) * truckScalePulseMagnitude;
            truckSpriteTransform.localScale = new Vector3(originalTruckScale.x * scalePulseX, originalTruckScale.y, originalTruckScale.z);
        }

        // Check for start input
        if (!isLoading && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            // Ignore clicks on the reset button itself for starting the game
            if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == highScoreResetButton?.gameObject)
            {
                return; // Don't start game if clicking the reset button
            }
            StartGame();
        }
    }

    /// <summary>
    /// Handles clicks on the hidden high score reset button.
    /// </summary>
    public void HandleHighScoreResetClick()
    {
        if (isLoading || fadeCoroutine != null) return; // Prevent clicks during load or fade

        highScoreResetClickCount++;

        switch (highScoreResetClickCount)
        {
            case 1:
                // Make button visible and set initial text
                highScoreResetButton.image.color = originalResetButtonColor; // Restore original alpha (usually 1)
                highScoreResetButtonText.color = originalResetTextColor;
                highScoreResetButtonText.text = "Reset High Score?";
                break;

            case 2:
                // Confirm action
                highScoreResetButtonText.text = "ARE YOU SURE?";
                break;

            case 3:
                // Reset the score and give feedback
                ScoreManager.Instance?.ResetHighScore();
                highScoreResetButtonText.text = "High Score Reset!";
                // Start fade out
                fadeCoroutine = StartCoroutine(FadeOutResetButton(resetButtonFadeDuration));
                highScoreResetClickCount = 0; // Reset count for next time
                break;

            default:
                // Should not happen, but reset just in case
                highScoreResetClickCount = 0;
                highScoreResetButton.image.color = new Color(originalResetButtonColor.r, originalResetButtonColor.g, originalResetButtonColor.b, 0f);
                highScoreResetButtonText.color = new Color(originalResetTextColor.r, originalResetTextColor.g, originalResetTextColor.b, 0f);
                highScoreResetButtonText.text = "";
                break;
        }
    }

    /// <summary>
    /// Coroutine to fade out the reset button's image and text.
    /// </summary>
    private IEnumerator FadeOutResetButton(float duration)
    {
        float elapsed = 0f;
        Color startButtonColor = highScoreResetButton.image.color;
        Color startTextColor = highScoreResetButtonText.color;
        Color endButtonColor = new Color(startButtonColor.r, startButtonColor.g, startButtonColor.b, 0f);
        Color endTextColor = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (elapsed / duration));
            highScoreResetButton.image.color = Color.Lerp(endButtonColor, startButtonColor, alpha);
            highScoreResetButtonText.color = Color.Lerp(endTextColor, startTextColor, alpha);
            yield return null;
        }

        // Ensure final state
        highScoreResetButton.image.color = endButtonColor;
        highScoreResetButtonText.color = endTextColor;
        highScoreResetButtonText.text = ""; // Clear text after fade
        fadeCoroutine = null; // Mark as finished
    }


    /// <summary>
    /// Starts loading the main game scene.
    /// </summary>
    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;

        Debug.Log("Starting game...");
        // Optional: Add fade out effect here
        if (startPromptText != null)
            startPromptText.text = "Loading...";

        // Stop Title Music before loading next scene
        AudioManager.Instance?.StopMusic();

        SceneManager.LoadScene(gameSceneName);
    }
}
