using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for Linq methods like FindIndex, Any, Count, Where
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
    public int maxInventorySlots = 4;
    [Tooltip("All possible item types that can be randomly added.")]
    [SerializeField] private List<InventoryItemData> availableItemTypes;
    // Item spawn delay now driven by DifficultyManager

    // List can now contain nulls to represent empty slots
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
        if (availableItemTypes != null && availableItemTypes.Count > 0 && DifficultyManager.Instance.GetRandomAddDelay() > 0)
        {
            StartCoroutine(RandomlyAddItemRoutine());
        }
    }

    // Coroutine to add a random item periodically
    private IEnumerator RandomlyAddItemRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(DifficultyManager.Instance.GetRandomAddDelay());

            if (availableItemTypes != null && availableItemTypes.Count > 0)
            {
                var itemToAdd = availableItemTypes[Random.Range(0, availableItemTypes.Count)];
                AddItem(itemToAdd, 1);
            }
        }
    }


    /// <summary>
    /// Attempts to add an item. Stacks if possible, fills empty (null) slots first,
    /// then adds to a new slot if space allows based on non-null item count.
    /// Finally, consolidates stacks.
    /// </summary>
    public bool AddItem(InventoryItemData itemToAdd, int quantityToAdd = 1)
    {
        if (itemToAdd == null || quantityToAdd <= 0) return false;

        bool addedSuccessfully = false;
        int originalQuantity = quantityToAdd;
        int nonNullItemCount = GetNonNullItemCount(); // Get current count before adding

        // --- Stacking ---
        if (itemToAdd.canStack)
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                // Check if slot exists, is not null, matches item, and has space
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

        // --- Fill Empty (null) Slots or Add to New Slot ---
        while (quantityToAdd > 0)
        {
            // Find the first null slot
            int nullIndex = inventorySlots.FindIndex(slot => slot == null);

            if (nullIndex != -1) // Found an empty slot
            {
                int amountToAdd = itemToAdd.canStack ? Mathf.Min(quantityToAdd, itemToAdd.maxStackSize) : 1;
                inventorySlots[nullIndex] = new InventorySlotData(itemToAdd, amountToAdd);
                quantityToAdd -= amountToAdd;
                addedSuccessfully = true;
                nonNullItemCount++; // Increment count as we filled a slot
                Debug.Log($"Filled empty slot at index {nullIndex} with {itemToAdd.itemName}");
            }
            else // No empty slots, try adding a new one if space allows
            {
                if (nonNullItemCount >= maxInventorySlots)
                {
                    // Use nonNullItemCount for the warning message
                    Debug.LogWarning($"Inventory full ({nonNullItemCount}/{maxInventorySlots})! Could not add new slot for {itemToAdd.itemName}.");
                    break; // No more slots allowed
                }

                // Add to the end of the list
                int amountToAdd = itemToAdd.canStack ? Mathf.Min(quantityToAdd, itemToAdd.maxStackSize) : 1;
                inventorySlots.Add(new InventorySlotData(itemToAdd, amountToAdd));
                quantityToAdd -= amountToAdd;
                addedSuccessfully = true;
                nonNullItemCount++; // Increment count as we added a new slot
                Debug.Log($"Added new slot at index {inventorySlots.Count - 1} for {itemToAdd.itemName}");
            }
        }

        bool consolidationMadeChange = false;
        if (addedSuccessfully)
        {
            consolidationMadeChange = ConsolidateStacks(); // Consolidate after adding
            // No need to invoke OnInventoryChanged here, ConsolidateStacks will do it if needed,
            // or the final check below will.
        }
        // Check if any change happened (either adding or consolidating)
        if (addedSuccessfully || consolidationMadeChange)
        {
             OnInventoryChanged?.Invoke();
        }
        else if (quantityToAdd < originalQuantity && GetNonNullItemCount() >= maxInventorySlots) // Check non-null count again
        {
             Debug.LogWarning($"Inventory full! Could only add {originalQuantity - quantityToAdd} of {itemToAdd.itemName} to existing stacks.");
        }

        return addedSuccessfully; // True if *any* quantity was initially added (before consolidation)
    }

    /// <summary>
    /// Attempts to remove an item. Prioritizes removing from later slots first.
    /// Sets the slot to null instead of removing it from the list if quantity reaches zero.
    /// </summary>
    public bool RemoveItem(InventoryItemData itemToRemove, int quantityToRemove = 1)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return false;

        int quantityStillNeeded = quantityToRemove;
        bool removedAny = false;

        // Iterate backwards
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            // Check if slot exists and is not null
            if (inventorySlots[i] != null && inventorySlots[i].itemData == itemToRemove)
            {
                int amountToRemoveFromSlot = Mathf.Min(quantityStillNeeded, inventorySlots[i].quantity);
                inventorySlots[i].quantity -= amountToRemoveFromSlot;
                quantityStillNeeded -= amountToRemoveFromSlot;
                removedAny = true;

                if (inventorySlots[i].quantity <= 0)
                {
                    inventorySlots[i] = null; // Set slot to null instead of removing
                    Debug.Log($"Set slot {i} to null after removing {itemToRemove.itemName}.");
                }

                if (quantityStillNeeded <= 0) break;
            }
        }

        if (removedAny) // If any removal happened (partial or full)
        {
             // Notify UI of removal
             OnInventoryChanged?.Invoke();

             // Consolidate any split stacks so items of the same type combine bottom-up
             bool consolidationMadeChange = ConsolidateStacks();
             if (consolidationMadeChange)
             {
                 Debug.Log("Consolidated stacks after removal.");
                 OnInventoryChanged?.Invoke();
             }

             if (quantityStillNeeded > 0)
             {
                 Debug.LogWarning($"Could only remove {quantityToRemove - quantityStillNeeded} of {itemToRemove.itemName}. Not enough in inventory.");
                 return false; // Indicate partial removal
             }
             return true; // Indicate full removal
        }
        else // Not found
        {
            Debug.LogWarning($"Item {itemToRemove.itemName} not found in inventory.");
            return false;
        }
    }


    /// <summary>
    /// Sets the highlighted item slot to null, scores based on color match,
    /// finds the next appropriate non-null slot to highlight, and consolidates stacks.
    /// </summary>
    public void RemoveHighlightedItemAndScore(Color zoneColor)
    {
        int countBeforeRemoval = inventorySlots.Count;

        if (countBeforeRemoval == 0 || highlightedSlotIndex < 0 || highlightedSlotIndex >= countBeforeRemoval)
        {
            Debug.Log($"Inventory empty or highlighted index invalid ({highlightedSlotIndex}). Cannot remove item.");
            return;
        }

        int removedIndex = highlightedSlotIndex;
        InventorySlotData removedSlot = inventorySlots[removedIndex];
        int deliveredQuantity = removedSlot.quantity; // capture stack count for scoring
        InventoryItemData removedItemData = removedSlot.itemData;

        // Remove item and shift others down
        inventorySlots.RemoveAt(removedIndex);
        Debug.Log($"Removed item: {removedItemData.itemName} at index {removedIndex}.");

        // Score based on color match multiplied by stack size
        {
             int basePoints = (removedItemData.itemColor == zoneColor) ? 100 : 10;
             int totalScore = basePoints * deliveredQuantity;
             scoreManager?.AddScore(totalScore);
             Debug.Log($"Scored {totalScore} points for delivering {deliveredQuantity}x {removedItemData.itemName} ({removedItemData.itemColor} vs {zoneColor}).");
            // Play delivery sound: correct or incorrect
            if (removedItemData.itemColor == zoneColor)
                AudioManager.Instance?.PlayCorrectDeliverySound();
            else
                AudioManager.Instance?.PlayIncorrectDeliverySound();
        }

        // Adjust highlight index
        int countAfterRemoval = inventorySlots.Count;
        if (countAfterRemoval == 0)
        {
            highlightedSlotIndex = 0;
        }
        else if (removedIndex >= countAfterRemoval)
        {
            // Removed last item, move highlight to new last
            highlightedSlotIndex = countAfterRemoval - 1;
        }
        else
        {
            // Keep same index, now pointing to next item
            highlightedSlotIndex = removedIndex;
        }

        // Consolidate stacks bottom-up so same items merge wherever possible
        bool consolidationMadeChange = ConsolidateStacks();
        if (consolidationMadeChange)
        {
            Debug.Log("Consolidated stacks after delivery.");
        }

        // Notify UI of data change and any consolidation
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Compacts the inventory (removes nulls), shuffles the order of items,
    /// and resets the highlight index to 0.
    /// </summary>
    public void ShuffleInventory()
    {
        // Filter out null slots
        List<InventorySlotData> nonNullItems = inventorySlots.Where(slot => slot != null).ToList();

        if (nonNullItems.Count <= 1) return; // No shuffle needed

        // Fisher-Yates shuffle on the non-null items
        System.Random rng = new System.Random();
        int n = nonNullItems.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            InventorySlotData value = nonNullItems[k];
            nonNullItems[k] = nonNullItems[n];
            nonNullItems[n] = value;
        }

        // Replace the old list with the compacted and shuffled list
        inventorySlots = nonNullItems;
        highlightedSlotIndex = 0; // Reset highlight to the first item

        Debug.Log($"Inventory compacted and shuffled. Highlight reset to index 0. New count: {inventorySlots.Count}");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Changes the highlighted slot index, skipping over null slots.
    /// </summary>
    /// <param name="forward">True moves highlight down (increasing index), False moves highlight up (decreasing index).</param>
    public void RotateInventory(bool forward)
    {
        int nonNullCount = GetNonNullItemCount();
        if (nonNullCount <= 1) return; // No rotation needed

        int currentListSize = inventorySlots.Count;
        if (currentListSize == 0) return; // Should not happen if nonNullCount > 1, but safe check

        int originalIndex = highlightedSlotIndex;
        int attempts = 0; // Prevent infinite loop in unlikely scenarios

        do
        {
            if (forward) // Move highlight down (visually) -> increase index
            {
                highlightedSlotIndex = (highlightedSlotIndex + 1) % currentListSize;
            }
            else // Move highlight up (visually) -> decrease index
            {
                highlightedSlotIndex = (highlightedSlotIndex - 1 + currentListSize) % currentListSize;
            }

            attempts++;
            // Break if we found a non-null slot OR we've looped entirely
            if (inventorySlots[highlightedSlotIndex] != null || attempts >= currentListSize)
            {
                break;
            }
        } while (highlightedSlotIndex != originalIndex);

        // If after checking all slots, we only found nulls (or the original),
        // and nonNullCount > 0, something is wrong, but we default to the first non-null.
        // This usually means we landed back on the original valid index if it was the only one.
        if (inventorySlots[highlightedSlotIndex] == null && nonNullCount > 0)
        {
             // Fallback: find the very first non-null index
             int firstNonNull = inventorySlots.FindIndex(slot => slot != null);
             if(firstNonNull != -1) highlightedSlotIndex = firstNonNull;
             else highlightedSlotIndex = 0; // Should be impossible if nonNullCount > 0
        }


        if (highlightedSlotIndex != originalIndex || nonNullCount == 1) // Only log/invoke if changed or only one item
        {
             Debug.Log($"Inventory highlight rotated. New index: {highlightedSlotIndex}");
             OnInventoryChanged?.Invoke(); // Notify UI to update the highlight
        }
    }


    /// <summary>
    /// Gets the current inventory slots data list (may contain nulls).
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
    /// Gets the current number of non-null item slots.
    /// </summary>
    public int GetCurrentInventoryCount()
    {
       // Use the helper method
       return GetNonNullItemCount();
    }

    /// <summary>
    /// Helper method to count non-null slots.
    /// </summary>
    private int GetNonNullItemCount()
    {
        return inventorySlots.Count(slot => slot != null);
    }

    /// <summary>
    /// Iterates through the inventory and merges stacks of the same item type
    /// where possible, freeing up slots by setting merged-from slots to null.
    /// </summary>
    /// <returns>True if any consolidation occurred, false otherwise.</returns>
    private bool ConsolidateStacks()
    {
        bool madeChange = false;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            // Skip empty slots or non-stackable items
            if (inventorySlots[i] == null || !inventorySlots[i].itemData.canStack) continue;

            // Skip full stacks
            if (inventorySlots[i].quantity >= inventorySlots[i].itemData.maxStackSize) continue;

            // Look for other stacks of the same item *after* this one
            for (int j = i + 1; j < inventorySlots.Count; j++)
            {
                // Skip empty slots or different items
                if (inventorySlots[j] == null || inventorySlots[j].itemData != inventorySlots[i].itemData) continue;

                // Calculate how much can be transferred to the current stack (slot i)
                int canTransfer = Mathf.Min(inventorySlots[j].quantity, inventorySlots[i].itemData.maxStackSize - inventorySlots[i].quantity);

                if (canTransfer > 0)
                {
                    inventorySlots[i].quantity += canTransfer;
                    inventorySlots[j].quantity -= canTransfer;
                    madeChange = true;
                    Debug.Log($"Consolidated {canTransfer} of {inventorySlots[i].itemData.itemName} from slot {j} to slot {i}.");


                    // If the source stack (slot j) is now empty, null it out
                    if (inventorySlots[j].quantity <= 0)
                    {
                        inventorySlots[j] = null;
                        Debug.Log($"Slot {j} emptied during consolidation.");
                        // Note: We don't need to adjust highlight index here,
                        // as it's handled after the main action (add/remove) completes.
                    }

                    // If the target stack (slot i) is now full, stop trying to add to it
                    if (inventorySlots[i].quantity >= inventorySlots[i].itemData.maxStackSize)
                    {
                        break; // Move to the next primary slot (i)
                    }
                }
            }
        }

        // No need to call OnInventoryChanged here - the calling methods will handle it.
        return madeChange;
    }

}
