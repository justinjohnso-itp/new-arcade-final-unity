using UnityEngine;
// If using the new Input System, uncomment the next line
// using UnityEngine.InputSystem; 

/// <summary>
/// Handles player input and collisions related to inventory management.
/// Attach this script to the player GameObject.
/// Assumes the player GameObject has a Collider2D (for obstacles) and a Rigidbody2D.
/// </summary>
public class PlayerInventoryController : MonoBehaviour
{
    // [Header("Input Settings")] // Removed vertical input specific settings
    // [Tooltip("How much vertical input is needed to trigger a rotation.")]
    // [SerializeField] private float rotationInputThreshold = 0.5f;
    // private bool rotationInputCooldown = false; // Prevents rapid rotation from holding key

    [Header("Collision Settings")]
    [Tooltip("The tag assigned to obstacle GameObjects.")]
    [SerializeField] private string obstacleTag = "Obstacle"; // Make sure your obstacles have this tag

    private InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("PlayerInventoryController: InventoryManager instance not found!", this);
            this.enabled = false; // Disable script if manager is missing
        }
    }

    void Update()
    {
        HandleInventoryCycleInput(); // Renamed for clarity
    }

    private void HandleInventoryCycleInput() // Renamed and modified
    {
        if (inventoryManager == null) return;

        // Check for a single button press (e.g., Spacebar, often mapped to "Jump")
        if (Input.GetButtonDown("Jump")) 
        {
            inventoryManager.RotateInventory(true); // Always rotate forward
            AudioManager.Instance?.PlayUIClickSound();
        }
    }

    // --- Collision Detection for Obstacles ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (inventoryManager == null) return;

        // Check if the collided object has the obstacle tag
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            Debug.Log("Player hit an obstacle! Shuffling disabled."); // Updated log message
            // inventoryManager.ShuffleInventory(); // <-- Disabled shuffling
            // Optional: Add other effects like knockback, damage, sound, etc.
        }
    }

    // --- Trigger Detection for Delivery Zones ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (inventoryManager == null) return;

        // Check if the trigger object has a DeliveryZone component
        DeliveryZone zone = other.GetComponent<DeliveryZone>();
        if (zone != null && zone.gameObject.activeInHierarchy) // Check if the zone is active
        {
            Debug.Log($"Player entered active Delivery Zone requiring color: {zone.RequiredColor}");
            // Call the updated method to remove the HIGHLIGHTED item
            inventoryManager.RemoveHighlightedItemAndScore(zone.RequiredColor);

            // Optional: Deactivate the zone after use?
            // zone.DeactivateZone();
            // Or maybe just play a success effect/sound
        }
    }
}
