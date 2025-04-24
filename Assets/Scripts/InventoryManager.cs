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

    // Event to notify UI when inventory changes
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
    /// Removes the oldest item and scores based on color match with the zone.
    /// </summary>
    public void RemoveOldestItemAndScore(Color zoneColor)
    {
        if (inventorySlots.Count == 0 || inventorySlots[0] == null)
        {
            Debug.Log("Inventory empty, cannot remove oldest item.");
            return;
        }

        InventorySlotData oldestSlot = inventorySlots[0];
        InventoryItemData removedItemData = oldestSlot.itemData;

        inventorySlots.RemoveAt(0);

        Debug.Log($"Removed oldest item: {removedItemData.itemName}");

        // Score based on color match
        if (removedItemData.itemColor == zoneColor)
        {
            Debug.Log($"Color match! Zone: {zoneColor}, Item: {removedItemData.itemColor}. +100 points.");
            if (scoreManager != null)
            {
                scoreManager.AddScore(100); // Correct delivery score
            }
            else
            {
                Debug.LogWarning("ScoreManager missing, cannot add score.");
            }
        }
        else
        {
            Debug.Log($"Color mismatch. Zone: {zoneColor}, Item: {removedItemData.itemColor}. +10 points.");
            if (scoreManager != null)
            {
                scoreManager.AddScore(10); // Wrong delivery bonus
            }
            else
            {
                Debug.LogWarning("ScoreManager missing, cannot add score.");
            }
        }

        OnInventoryChanged?.Invoke();
    }


    /// <summary>
    /// Randomly shuffles the order of items.
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

        Debug.Log("Inventory shuffled.");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Rotates the inventory items.
    /// </summary>
    /// <param name="forward">True moves last item to first, False moves first item to last.</param>
    public void RotateInventory(bool forward)
    {
        if (inventorySlots.Count <= 1) return;

        if (forward) // Move last to first
        {
            InventorySlotData lastItem = inventorySlots[inventorySlots.Count - 1];
            inventorySlots.RemoveAt(inventorySlots.Count - 1);
            inventorySlots.Insert(0, lastItem);
        }
        else // Move first to last
        {
            InventorySlotData firstItem = inventorySlots[0];
            inventorySlots.RemoveAt(0);
            inventorySlots.Add(firstItem);
        }

        OnInventoryChanged?.Invoke();
    }


    /// <summary>
    /// Gets the current inventory slots.
    /// </summary>
    public List<InventorySlotData> GetInventorySlots()
    {
        return inventorySlots;
    }

    /// <summary>
    /// Gets the current number of occupied slots.
    /// </summary>
    public int GetCurrentInventoryCount()
    {
       return inventorySlots.Count;
    }
}
