using UnityEngine;

/// <summary>
/// Manages playback of sound effects and background music using a Singleton pattern.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource for sound effects")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("AudioSource for background music")]
    [SerializeField] private AudioSource musicSource;

    [Header("Sound Effects")]
    [Tooltip("Sound played on obstacle collision")]
    public AudioClip collisionSound;
    [Tooltip("Sound played when the game over screen appears")]
    public AudioClip gameOverSound;
    [Tooltip("Sound played when a correct package is delivered")]
    public AudioClip correctDeliverySound;
    [Tooltip("Sound played when picking up a package")]
    public AudioClip pickupSound;
    [Tooltip("Sound played for UI interactions like button clicks or inventory cycle")]
    public AudioClip uiClickSound;
    [Tooltip("Sound played when an incorrect package is delivered")]
    public AudioClip incorrectDeliverySound;
    // Add more AudioClip fields here as needed

    [Header("Music Tracks")] // Added section
    [Tooltip("Music for the title screen")]
    public AudioClip titleMusic;
    [Tooltip("Music for the main game scene")]
    public AudioClip gameMusic;

    void Awake()
    {
        // Simple Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("AudioManager: Another instance found, destroying this one.");
            Destroy(gameObject);
            return; // Important: exit Awake early if destroying
        }
        else
        {
            Instance = this;
            // Keep AudioManager across scene loads
            DontDestroyOnLoad(gameObject);
            Debug.Log("AudioManager: Instance created and marked DontDestroyOnLoad.");
        }

        // Find or create AudioSources if not assigned
        EnsureAudioSources();
    }

    /// <summary>
    /// Ensures the AudioSource components exist and are assigned.
    /// </summary>
    private void EnsureAudioSources()
    {
        // Try to find existing sources if not assigned in Inspector
        if (sfxSource == null || musicSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = sources.Length > 0 ? sources[0] : null;
            }
            if (musicSource == null)
            {
                // If only one source exists, assign it to sfxSource first
                // If two exist, assign the second to musicSource
                // If none/one exist and musicSource is still null, create one
                musicSource = sources.Length > 1 ? sources[1] : null;
            }
        }

        // Create sources if they still don't exist
        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: SFX AudioSource not found/assigned. Creating one.");
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            Debug.LogWarning("AudioManager: Music AudioSource not found/assigned. Creating one.");
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true; // Music should loop by default
        }
    }

    // --- Sound Effect Methods --- 

    /// <summary>
    /// Plays the collision sound effect.
    /// </summary>
    public void PlayCollisionSound()
    {
        if (collisionSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(collisionSound);
            Debug.Log("AudioManager: Playing Collision Sound");
        }
        else Debug.LogWarning("AudioManager: Collision Sound not assigned!");
    }

    /// <summary>
    /// Plays the game over sound effect.
    /// </summary>
    public void PlayGameOverSound()
    {
        if (gameOverSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(gameOverSound);
            Debug.Log("AudioManager: Playing Game Over Sound");
        }
         else Debug.LogWarning("AudioManager: Game Over Sound not assigned!");
    }

    /// <summary>
    /// Plays the sound effect for correct package delivery.
    /// </summary>
    public void PlayCorrectDeliverySound()
    {
        if (correctDeliverySound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(correctDeliverySound);
            Debug.Log("AudioManager: Playing Correct Delivery Sound");
        }
        else Debug.LogWarning("AudioManager: Correct Delivery Sound not assigned!");
    }

    /// <summary>
    /// Plays the sound effect for picking up a package.
    /// </summary>
    public void PlayPickupSound()
    {
        if (pickupSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(pickupSound);
            Debug.Log("AudioManager: Playing Pickup Sound");
        }
        else Debug.LogWarning("AudioManager: Pickup Sound not assigned!");
    }

    /// <summary>
    /// Plays the UI click/interaction sound effect.
    /// </summary>
     public void PlayUIClickSound()
    {
        if (uiClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(uiClickSound);
            Debug.Log("AudioManager: Playing UI Click Sound");
        }
         else Debug.LogWarning("AudioManager: UI Click Sound not assigned!");
    }

    /// <summary>
    /// Plays the sound effect for incorrect package delivery.
    /// </summary>
    public void PlayIncorrectDeliverySound()
    {
        if (incorrectDeliverySound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(incorrectDeliverySound);
            Debug.Log("AudioManager: Playing Incorrect Delivery Sound");
        }
        else Debug.LogWarning("AudioManager: Incorrect Delivery Sound not assigned!");
    }

    // --- Music Methods --- 

    /// <summary>
    /// Plays the title screen music.
    /// </summary>
    public void PlayTitleMusic()
    {
        PlayMusic(titleMusic);
    }

    /// <summary>
    /// Plays the main game music.
    /// </summary>
    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    /// <summary>
    /// Helper method to play a music track.
    /// </summary>
    private void PlayMusic(AudioClip musicClip)
    {
        if (musicSource == null)
        {
            Debug.LogError("AudioManager: Music AudioSource is missing!");
            return;
        }
        if (musicClip == null)
        {
            Debug.LogWarning("AudioManager: Music clip is null, stopping music.");
            musicSource.Stop();
            return;
        }

        // Check if the requested music is already playing
        if (musicSource.isPlaying && musicSource.clip == musicClip)
        {
            Debug.Log($"AudioManager: Music '{musicClip.name}' is already playing.");
            return; // Don't restart if already playing the same clip
        }

        Debug.Log($"AudioManager: Playing music '{musicClip.name}'");
        musicSource.clip = musicClip;
        musicSource.loop = true; // Ensure looping
        musicSource.Play();
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopMusic()
    {
         if (musicSource != null)
         {
            Debug.Log("AudioManager: Stopping music.");
            musicSource.Stop();
         }
    }

    // Add more public methods here to play other sounds...
}
