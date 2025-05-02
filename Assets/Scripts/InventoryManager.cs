using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq methods like FindIndex, Any
using System.Collections; // Required for Coroutines

/// <summary>
/// Represents a slot in the inventory data, holding item data and quantity.
/// </summary>
[System.Serializable]
public class InventorySlotData
{
    public InventoryItemData itemData;
    public int quantity;

    public InventorySlotData(InventoryItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    // Helper to add quantity
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }
}

/// <summary>
/// Manages the player's inventory data.
/// Implements a Singleton pattern.
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
            // Optional: DontDestroyOnLoad(gameObject);
        }
    }
    // --- End Singleton Pattern ---

    [Header("Inventory Settings")]
    [Tooltip("Maximum number of distinct item slots allowed.")]
    [SerializeField] private int maxInventorySlots = 4;
    [Tooltip("All possible item types that can be randomly added.")]
    [SerializeField] private List<InventoryItemData> availableItemTypes;
    [Tooltip("Delay between random item additions (seconds). Set <= 0 to disable.")]
    [SerializeField] private float randomAddDelay = 1.0f;

    private List<InventorySlotData> inventorySlots = new List<InventorySlotData>();
    private int highlightedSlotIndex = 0; // Index of the currently selected slot for delivery

    // Event to notify UI when inventory changes (or highlight changes)
    public System.Action OnInventoryChanged;

    [Header("Dependencies")]
    [Tooltip("Reference to the ScoreManager in the scene.")]
    [SerializeField] private ScoreManager scoreManager;

    void Start()
    {
        // Find ScoreManager if not assigned
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogWarning("InventoryManager: ScoreManager not found! Scoring will not work.", this);
            }
        }

        // Start random item adding if configured
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
            }
        }
    }


    /// <summary>
    /// Attempts to add an item. Stacks if possible, otherwise adds to a new slot if space allows.
    /// </summary>
    public bool AddItem(InventoryItemData itemToAdd, int quantityToAdd = 1)
    {
        if (itemToAdd == null || quantityToAdd <= 0) return false;

        bool addedSuccessfully = false;
        int originalQuantity = quantityToAdd;

        // --- Stacking --- 
        if (itemToAdd.canStack)
        {
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

        // --- Add to New Slot --- 
        while (quantityToAdd > 0)
        {
            if (inventorySlots.Count >= maxInventorySlots)
            {
                Debug.LogWarning($"Inventory full ({inventorySlots.Count}/{maxInventorySlots})! Could not add new slot for {itemToAdd.itemName}.");
                break; // No more slots
            }

            int amountToAdd = itemToAdd.canStack ? Mathf.Min(quantityToAdd, itemToAdd.maxStackSize) : 1;
            inventorySlots.Add(new InventorySlotData(itemToAdd, amountToAdd)); 
            quantityToAdd -= amountToAdd;
            addedSuccessfully = true;
        }

        // Ensure highlight index remains valid after adding
        highlightedSlotIndex = Mathf.Clamp(highlightedSlotIndex, 0, Mathf.Max(0, inventorySlots.Count - 1));

        if (addedSuccessfully)
        {
            OnInventoryChanged?.Invoke();
        }
        else if (quantityToAdd < originalQuantity && inventorySlots.Count >= maxInventorySlots)
        { 
             Debug.LogWarning($"Inventory full! Could only add {originalQuantity - quantityToAdd} of {itemToAdd.itemName} to existing stacks.");
        }

        return addedSuccessfully; // True if *any* quantity was added
    }

    /// <summary>
    /// Attempts to remove an item. Prioritizes removing from later slots first.
    /// Note: This might need adjustment if specific slot removal is needed elsewhere.
    /// Consider if removing the *highlighted* item makes more sense universally.
    /// For now, keeping original logic but adding highlight index clamp.
    /// </summary>
    public bool RemoveItem(InventoryItemData itemToRemove, int quantityToRemove = 1)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return false;

        int quantityStillNeeded = quantityToRemove;
        bool removedAny = false;

        // Iterate backwards to safely remove slots
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (inventorySlots[i] != null && inventorySlots[i].itemData == itemToRemove)
            {
                int amountToRemoveFromSlot = Mathf.Min(quantityStillNeeded, inventorySlots[i].quantity);
                inventorySlots[i].quantity -= amountToRemoveFromSlot;
                quantityStillNeeded -= amountToRemoveFromSlot;
                removedAny = true;

                if (inventorySlots[i].quantity <= 0)
                {
                    inventorySlots.RemoveAt(i);
                }

                if (quantityStillNeeded <= 0) break; // Removed enough
            }
        }

        if (removedAny && quantityStillNeeded <= 0) // Fully removed
        {
            OnInventoryChanged?.Invoke();
            return true;
        }
        else if (removedAny) // Partially removed
        {
             // Ensure highlight index remains valid after removing
             highlightedSlotIndex = Mathf.Clamp(highlightedSlotIndex, 0, Mathf.Max(0, inventorySlots.Count - 1));
             OnInventoryChanged?.Invoke();
             Debug.LogWarning($"Could only remove {quantityToRemove - quantityStillNeeded} of {itemToRemove.itemName}. Not enough in inventory.");
             return false; 
        }
        else // Not found
        {
            Debug.LogWarning($"Item {itemToRemove.itemName} not found in inventory.");
            return false;
        }
    }


    /// <summary>
    /// Removes the item at the currently highlighted index and scores based on color match with the zone.
    /// </summary>
    public void RemoveHighlightedItemAndScore(Color zoneColor) // Renamed from RemoveOldestItemAndScore
    {
        // Check if inventory is empty or index is invalid (shouldn't happen with clamping, but good practice)
        if (inventorySlots.Count == 0 || highlightedSlotIndex < 0 || highlightedSlotIndex >= inventorySlots.Count)
        {
            Debug.Log("Inventory empty or highlighted index invalid, cannot remove item.");
            return;
        }

        InventorySlotData highlightedSlot = inventorySlots[highlightedSlotIndex];
        InventoryItemData removedItemData = highlightedSlot.itemData;

        // Remove the item at the highlighted index
        inventorySlots.RemoveAt(highlightedSlotIndex);

        Debug.Log($"Removed highlighted item: {removedItemData.itemName} at index {highlightedSlotIndex}");

        // Score based on color match
        if (removedItemData.itemColor == zoneColor)
        {
            Debug.Log($"Color match! Zone: {zoneColor}, Item: {removedItemData.itemColor}. +100 points.");
            scoreManager?.AddScore(100); // Correct delivery score
        }
        else
        {
            Debug.Log($"Color mismatch. Zone: {zoneColor}, Item: {removedItemData.itemColor}. +10 points.");
            scoreManager?.AddScore(10); // Wrong delivery bonus
        }

        // Adjust highlight index if it's now out of bounds (points past the end)
        highlightedSlotIndex = Mathf.Clamp(highlightedSlotIndex, 0, Mathf.Max(0, inventorySlots.Count - 1));

        OnInventoryChanged?.Invoke();
    }


    /// <summary>
    /// Randomly shuffles the order of items in the inventory data list.
    /// Resets the highlight index to 0.
    /// </summary>
    public void ShuffleInventory()
    {
        if (inventorySlots.Count <= 1) return;

        // Fisher-Yates shuffle
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

        highlightedSlotIndex = 0; // Reset highlight to the first item after shuffle

        Debug.Log("Inventory shuffled. Highlight reset to index 0.");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Changes the highlighted slot index instead of rotating the list.
    /// </summary>
    /// <param name="forward">True moves highlight down (increasing index), False moves highlight up (decreasing index).</param>
    public void RotateInventory(bool forward)
    {
        if (inventorySlots.Count <= 1) return; // No rotation needed for 0 or 1 item

        int count = inventorySlots.Count;
        if (forward) // Move highlight down (visually) -> increase index
        {
            highlightedSlotIndex = (highlightedSlotIndex + 1) % count;
        }
        else // Move highlight up (visually) -> decrease index
        {
            highlightedSlotIndex = (highlightedSlotIndex - 1 + count) % count;
        }

        Debug.Log($"Inventory highlight rotated. New index: {highlightedSlotIndex}");
        OnInventoryChanged?.Invoke(); // Notify UI to update the highlight
    }


    /// <summary>
    /// Gets the current inventory slots data.
    /// </summary>
    public List<InventorySlotData> GetInventorySlots()
    {
        return inventorySlots;
    }

    /// <summary>
    /// Gets the index of the currently highlighted slot.
    /// </summary>
    public int GetHighlightedSlotIndex()
    {
        return highlightedSlotIndex;
    }

    /// <summary>
    /// Gets the current number of occupied slots.
    /// </summary>
    public int GetCurrentInventoryCount()
    {
       return inventorySlots.Count;
    }
}
