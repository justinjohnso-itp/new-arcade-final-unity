// filepath: /Users/justin/Library/CloudStorage/Dropbox/NYU/Semester 2 ('25 Spring)/New Arcade/final/[ARC] Infinite Scroller/Assets/Scripts/DeliveryZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Ensure it has a trigger collider
public class DeliveryZone : MonoBehaviour
{
    [Tooltip("Score awarded for a successful delivery.")]
    public int scoreValue = 100;

    [Tooltip("Optional: Particle effect to play on successful delivery.")]
    public ParticleSystem successEffect;

    [Tooltip("Optional: Audio clip to play on successful delivery.")]
    public AudioClip successSound;

    private Collider2D zoneCollider;
    private bool isActive = true; // Tracks if this zone can still be used

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        // Ensure the collider is set to be a trigger
        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning($"DeliveryZone {name} collider was not set to 'Is Trigger'. Forcing it.", this);
            zoneCollider.isTrigger = true;
        }

        // The zone starts enabled only if its GameObject is active
        // (controlled by LevelGenerator)
        isActive = gameObject.activeSelf;
    }

    // Note: This script assumes the DeliveryZone GameObject itself is
    // enabled/disabled by the LevelGenerator.
    // If the GameObject is inactive, OnTriggerEnter2D won't fire.

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return; // Don't process if already used or inactive

        // Check if the object entering is the Player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if the player *has* a package to deliver
            // We'll need to add package tracking to PlayerController later
            // For now, let's assume they always have one for testing
            bool playerHasPackage = player.HasPackage(); // Call the new method on PlayerController

            if (playerHasPackage)
            {
                DeliverPackage(player);
            }
            else
            {
                Debug.Log("Player entered delivery zone but has no package.");
                // Optional: Add feedback if player enters without a package
            }
        }
    }

    private void DeliverPackage(PlayerController player)
    {
        Debug.Log($"Package delivered to {name}! Score +{scoreValue}");
        isActive = false; // Deactivate this zone after one use

        // --- Add Score ---
        // TODO: Implement score handling (e.g., call a method on a GameManager)
        // GameManager.Instance.AddScore(scoreValue);

        // --- Trigger Effects ---
        if (successEffect != null)
        {
            Instantiate(successEffect, transform.position, Quaternion.identity);
        }
        if (successSound != null)
        {
            // Use AudioSource.PlayClipAtPoint for simple one-shot sounds
            AudioSource.PlayClipAtPoint(successSound, transform.position);
        }

        // --- Visual Feedback ---
        // TODO: Change appearance (e.g., disable sprite renderer, change color)
        SpriteRenderer sr = GetComponent<SpriteRenderer>(); // If using a sprite for the zone
        if (sr != null)
        {
             sr.color = Color.gray; // Example: Dim the color
             // Or disable it entirely: sr.enabled = false;
        }

        // Optional: Disable the collider completely if needed, though isActive flag prevents re-triggering
        // zoneCollider.enabled = false;

        // Tell the PlayerController the package was delivered
        player.OnPackageDelivered(); // We'll add this method next
    }

    // Optional: If you want the zone to visually re-enable when the GameObject is activated
    // void OnEnable()
    // {
    //     isActive = true;
    //     // Reset visual state if needed
    //     SpriteRenderer sr = GetComponent<SpriteRenderer>();
    //     if (sr != null)
    //     {
    //         sr.color = Color.white; // Reset color
    //         sr.enabled = true;
    //     }
    // }
}
