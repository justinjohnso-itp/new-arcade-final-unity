using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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
    [Tooltip("The RectTransform of the truck sprite to animate")] // Added
    [SerializeField] private RectTransform truckSpriteTransform; // Added

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

    private float originalTitleScale;
    private Quaternion originalTitleRotation; // Added
    private bool isLoading = false; // Prevent multiple scene loads
    private Vector2 originalTruckPosition; // Added
    private Vector3 originalTruckScale; // Added

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
            // Vertical Shake
            float randomYOffset = Random.Range(-truckShakeIntensity, truckShakeIntensity);
            truckSpriteTransform.anchoredPosition = new Vector2(originalTruckPosition.x, originalTruckPosition.y + randomYOffset);

            // Horizontal Scale Pulse
            float scalePulse = 1.0f + Mathf.Sin(Time.time * truckScalePulseSpeed) * truckScalePulseMagnitude;
            truckSpriteTransform.localScale = new Vector3(originalTruckScale.x * scalePulse, originalTruckScale.y, originalTruckScale.z);
        }

        // Check for any key press to start the game
        if (!isLoading && Input.anyKeyDown)
        {
            StartGame();
        }
    }

    /// <summary>
    /// Starts the main game scene.
    /// </summary>
    public void StartGame()
    {
        if (isLoading) return; // Prevent multiple loads
        isLoading = true;

        // Play UI Click Sound via Singleton
        AudioManager.Instance?.PlayUIClickSound();

        // Optional: Hide prompt immediately
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);

        // Stop Title Music before loading game scene
        AudioManager.Instance?.StopMusic();

        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }
}
