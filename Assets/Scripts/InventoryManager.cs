using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq methods like FindIndex, Any
using System.Collections; // Required for Coroutines

/// <summary>
/// Represents a slot in the inventory data, holding item data and quantity.
/// </summary>
[System.Serializable] // Make it visible in the Inspector if needed (though likely not directly edited)
public class InventorySlotData
{
    public InventoryItemData itemData;
    public int quantity;

    public InventorySlotData(InventoryItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    // Helper to add quantity to this slot
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }
}

/// <summary>
/// Manages the player's inventory data (items and quantities).
/// Implements a simple Singleton pattern for easy access.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // --- Singleton Pattern --- 
    public static InventoryManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Keep the InventoryManager persistent across scenes
            // DontDestroyOnLoad(gameObject);
        }
    }
    // --- End Singleton Pattern ---

    [Header("Inventory Settings")]
    [Tooltip("Maximum number of distinct item slots allowed in the inventory.")]
    [SerializeField] private int maxInventorySlots = 4; // Added max slots field
    [Tooltip("All possible item types that can be randomly added.")]
    [SerializeField] private List<InventoryItemData> availableItemTypes;
    [Tooltip("Delay between random item additions (in seconds). Set <= 0 to disable.")]
    [SerializeField] private float randomAddDelay = 1.0f;

    // The actual inventory data - using List for easy add/remove/rotate
    private List<InventorySlotData> inventorySlots = new List<InventorySlotData>();

    // Optional: Event to notify UI when inventory changes
    public System.Action OnInventoryChanged;

    // Reference to score manager (assign in Inspector or find)
    [Header("Dependencies")] // Added header for clarity
    [Tooltip("Reference to the ScoreManager in the scene.")]
    [SerializeField] private ScoreManager scoreManager;

    void Start()
    {
        // Find ScoreManager if not assigned in Inspector
        if (scoreManager == null)
        {
            // Use FindFirstObjectByType for newer Unity versions
            scoreManager = FindFirstObjectByType<ScoreManager>();
            // Fallback for older versions (or keep if preferred)
            // scoreManager = FindObjectOfType<ScoreManager>();

            if (scoreManager == null)
            {
                Debug.LogWarning("InventoryManager: ScoreManager not found in scene and not assigned! Scoring will not work.", this);
            }
        }

        // Start the random item adding coroutine if delay is positive
        if (randomAddDelay > 0 && availableItemTypes != null && availableItemTypes.Count > 0)
        {
            StartCoroutine(RandomlyAddItemRoutine());
        }
    }

    // Coroutine to add a random item periodically
    private IEnumerator RandomlyAddItemRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(randomAddDelay);

            if (availableItemTypes.Count > 0)
            {
                InventoryItemData itemToAdd = availableItemTypes[Random.Range(0, availableItemTypes.Count)];
                AddItem(itemToAdd, 1);
                // Debug.Log($"Added random item: {itemToAdd.itemName}");
            }
        }
    }


    /// <summary>
    /// Attempts to add an item to the inventory. Finds existing stacks or adds to a new slot, respecting max slot limit.
    /// </summary>
    public bool AddItem(InventoryItemData itemToAdd, int quantityToAdd = 1)
    {
        if (itemToAdd == null || quantityToAdd <= 0) return false;

        bool addedSuccessfully = false;
        int originalQuantity = quantityToAdd; // Keep track for logging

        // --- Stacking Logic --- 
        if (itemToAdd.canStack)
        {
            // Try to find existing stacks with space
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (inventorySlots[i] != null && inventorySlots[i].itemData == itemToAdd && inventorySlots[i].quantity < itemToAdd.maxStackSize)
                {
                    int canAdd = itemToAdd.maxStackSize - inventorySlots[i].quantity;
                    int amountToAdd = Mathf.Min(quantityToAdd, canAdd);

                    inventorySlots[i].AddQuantity(amountToAdd);
                    quantityToAdd -= amountToAdd;
                    addedSuccessfully = true;

                    if (quantityToAdd <= 0) break; // Added all
                }
            }
        }

        // --- Add to New Slot Logic --- 
        // If there's still quantity left, try to add new slots IF space allows
        while (quantityToAdd > 0)
        {
            // *** Check if inventory is full BEFORE adding a new slot ***
            if (inventorySlots.Count >= maxInventorySlots)
            {
                Debug.LogWarning($"Inventory full ({inventorySlots.Count}/{maxInventorySlots} slots)! Could not add new slot for {itemToAdd.itemName}.");
                break; // Exit loop if no more slots can be added
            }

            int amountToAdd = itemToAdd.canStack ? Mathf.Min(quantityToAdd, itemToAdd.maxStackSize) : 1;
            // Add to the end of the list
            inventorySlots.Add(new InventorySlotData(itemToAdd, amountToAdd)); 
            quantityToAdd -= amountToAdd;
            addedSuccessfully = true;
        }


        if (addedSuccessfully)
        {
            OnInventoryChanged?.Invoke();
        }
        // Log if partially added due to slot limit
        else if (quantityToAdd < originalQuantity && inventorySlots.Count >= maxInventorySlots)
        { 
             Debug.LogWarning($"Inventory full! Could only add {originalQuantity - quantityToAdd} of {itemToAdd.itemName} to existing stacks.");
        }


        return addedSuccessfully; // Returns true if *any* quantity was added
    }

    /// <summary>
    /// Attempts to remove an item from the inventory. Prioritizes removing from later slots first.
    /// </summary>
    public bool RemoveItem(InventoryItemData itemToRemove, int quantityToRemove = 1)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return false;

        int quantityStillNeeded = quantityToRemove;
        bool removedAny = false;

        // Iterate backwards to safely remove/clear slots and prioritize newer items
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (inventorySlots[i] != null && inventorySlots[i].itemData == itemToRemove)
            {
                int amountToRemoveFromSlot = Mathf.Min(quantityStillNeeded, inventorySlots[i].quantity);
                inventorySlots[i].quantity -= amountToRemoveFromSlot;
                quantityStillNeeded -= amountToRemoveFromSlot;
                removedAny = true;

                // If stack becomes empty, remove the slot entirely
                if (inventorySlots[i].quantity <= 0)
                {
                    inventorySlots.RemoveAt(i); // Remove from list
                }

                if (quantityStillNeeded <= 0) break; // Removed enough
            }
        }

        if (removedAny && quantityStillNeeded <= 0) // Fully removed requested amount
        {
            OnInventoryChanged?.Invoke();
            return true;
        }
        else if (removedAny) // Partially removed
        {
             OnInventoryChanged?.Invoke();
             Debug.LogWarning($"Could only remove {quantityToRemove - quantityStillNeeded} of {itemToRemove.itemName}. Not enough in inventory.");
             return false; 
        }
        else // Item not found
        {
            Debug.LogWarning($"Item {itemToRemove.itemName} not found in inventory.");
            return false;
        }
    }

    /// <summary>
    /// Removes the oldest item (at index 0) and checks if its color matches the zone color.
    /// Adds points via ScoreManager if it matches, or adds a smaller bonus if it mismatches.
    /// </summary>
    /// <param name="zoneColor">The required color of the delivery zone.</param>
    public void RemoveOldestItemAndScore(Color zoneColor)
    {
        if (inventorySlots.Count == 0 || inventorySlots[0] == null)
        {
            Debug.Log("Inventory empty, cannot remove oldest item.");
            return; // Nothing to remove
        }

        InventorySlotData oldestSlot = inventorySlots[0];
        InventoryItemData removedItemData = oldestSlot.itemData; // Store data before removing

        // Remove the item/slot at index 0
        inventorySlots.RemoveAt(0);

        Debug.Log($"Removed oldest item: {removedItemData.itemName}");

        // Check color match for scoring
        if (removedItemData.itemColor == zoneColor)
        {
            Debug.Log($"Color match! Zone: {zoneColor}, Item: {removedItemData.itemColor}. +100 points.");
            // Add points using ScoreManager
            if (scoreManager != null) // Check if scoreManager exists
            {
                scoreManager.AddScore(100); // Correct delivery score
            }
            else
            {
                Debug.LogWarning("ScoreManager reference missing in InventoryManager, cannot add score.");
            }
        }
        else
        {
            Debug.Log($"Color mismatch. Zone: {zoneColor}, Item: {removedItemData.itemColor}. +10 points.");
            // Add bonus points for wrong delivery
            if (scoreManager != null)
            {
                scoreManager.AddScore(10); // Wrong delivery bonus
            }
            else
            {
                Debug.LogWarning("ScoreManager reference missing in InventoryManager, cannot add score.");
            }
        }

        // Notify UI
        OnInventoryChanged?.Invoke();
    }


    /// <summary>
    /// Randomly shuffles the order of items currently in the inventory.
    /// </summary>
    public void ShuffleInventory()
    {
        if (inventorySlots.Count <= 1) return; // No need to shuffle empty or single-item list

        // Simple Fisher-Yates shuffle
        System.Random rng = new System.Random();
        int n = inventorySlots.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            InventorySlotData value = inventorySlots[k];
            inventorySlots[k] = inventorySlots[n];
            inventorySlots[n] = value;
        }

        Debug.Log("Inventory shuffled.");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Rotates the inventory items up or down.
    /// </summary>
    /// <param name="forward">True moves item 0 to end, False moves end item to 0.</param>
    public void RotateInventory(bool forward)
    {
        if (inventorySlots.Count <= 1) return; // No need to rotate

        if (forward) // W key / Up Arrow (Move last to first)
        {
            InventorySlotData lastItem = inventorySlots[inventorySlots.Count - 1];
            inventorySlots.RemoveAt(inventorySlots.Count - 1);
            inventorySlots.Insert(0, lastItem);
            // Debug.Log("Rotated inventory forward (W/Up)");
        }
        else // S key / Down Arrow (Move first to last)
        {
            InventorySlotData firstItem = inventorySlots[0];
            inventorySlots.RemoveAt(0);
            inventorySlots.Add(firstItem);
            // Debug.Log("Rotated inventory backward (S/Down)");
        }

        OnInventoryChanged?.Invoke();
    }


    /// <summary>
    /// Gets the current state of the inventory slots.
    /// </summary>
    public List<InventorySlotData> GetInventorySlots()
    {
        return inventorySlots;
    }

    /// <summary>
    /// Gets the number of items/slots currently in the inventory.
    /// </summary>
    public int GetCurrentInventoryCount()
    {
       return inventorySlots.Count;
    }
}
