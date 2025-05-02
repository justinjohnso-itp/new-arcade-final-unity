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
    [Header("Input Settings")]
    [Tooltip("How much vertical input is needed to trigger a rotation.")]
    [SerializeField] private float rotationInputThreshold = 0.5f;
    private bool rotationInputCooldown = false; // Prevents rapid rotation from holding key

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
        HandleRotationInput();
    }

    private void HandleRotationInput()
    {
        if (inventoryManager == null) return;

        // --- Using Old Input Manager ---
        float verticalInput = Input.GetAxisRaw("Vertical");

        // --- Using New Input System (Example - requires setup) ---
        // var keyboard = Keyboard.current;
        // if (keyboard == null) return; // No keyboard connected
        // float verticalInput = 0f;
        // if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalInput = 1f;
        // else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalInput = -1f;
        // ---

        if (Mathf.Abs(verticalInput) > rotationInputThreshold)
        {
            // Check cooldown to prevent multiple rotations per key press
            if (!rotationInputCooldown)
            {
                bool rotateForward = verticalInput > 0; // W or Up rotates forward (last to first)
                inventoryManager.RotateInventory(rotateForward);
                rotationInputCooldown = true;
            }
        }
        else
        {
            // Reset cooldown when input is released
            rotationInputCooldown = false;
        }
    }

    // --- Collision Detection for Obstacles ---
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (inventoryManager == null) return;

        // Check if the collided object has the obstacle tag
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            Debug.Log("Player hit an obstacle! Shuffling inventory.");
            inventoryManager.ShuffleInventory();
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
