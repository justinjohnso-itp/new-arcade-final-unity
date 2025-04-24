using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent object where InventorySlot prefabs will be instantiated.")]
    [SerializeField] private Transform slotsParent;
    [Tooltip("The prefab for a single inventory slot UI element.")]
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Highlighting")]
    [SerializeField] private Color bottomSlotHighlightColor = Color.blue; // Color for the oldest item slot

    private InventoryManager inventoryManager;
    // Keep the list to potentially manage instantiated slots if needed later, but primary update is now instantiation
    private List<InventorySlot> inventorySlotsUI = new List<InventorySlot>();

    void Start()
    {
        inventoryManager = InventoryManager.Instance; // Get singleton instance
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryUI: InventoryManager instance not found!");
            this.enabled = false;
            return;
        }
        if (slotsParent == null)
        {
            Debug.LogError("InventoryUI: Slots Parent transform is not assigned!");
            this.enabled = false;
            return;
        }
        if (inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryUI: Inventory Slot Prefab is not assigned!");
            this.enabled = false;
            return;
        }

        // Remove initialization from children
        // InitializeSlotsFromChildren(); 

        // --- Subscribe to Inventory Changes --- 
        inventoryManager.OnInventoryChanged += UpdateUI;

        // --- Initial UI Update --- 
        UpdateUI(); // Update UI with initial inventory state (will likely be empty)
    }

    void OnDestroy()
    {
        // --- Unsubscribe from events --- 
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// Clears existing slots and instantiates new ones based on the InventoryManager's data.
    /// </summary>
    private void UpdateUI()
    {
        if (inventoryManager == null || slotsParent == null || inventorySlotPrefab == null) return;

        // 1. Clear existing UI slots
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        inventorySlotsUI.Clear(); // Clear the list of references

        // 2. Get current inventory data
        List<InventorySlotData> inventoryData = inventoryManager.GetInventorySlots();

        // 3. Instantiate new UI slots based on data
        for (int i = 0; i < inventoryData.Count; i++)
        {
            InventorySlotData slotData = inventoryData[i];
            if (slotData != null && slotData.itemData != null) // Ensure data is valid
            {
                GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
                InventorySlot slotUI = slotGO.GetComponent<InventorySlot>();

                if (slotUI != null)
                {
                    inventorySlotsUI.Add(slotUI); // Add reference to our list
                    slotUI.UpdateSlot(slotData.itemData, slotData.quantity);

                    // --- Highlight Bottom Slot (Index 0) --- 
                    if (i == 0)
                    {
                        slotUI.SetBackgroundColor(bottomSlotHighlightColor);
                    }
                    // --- End Highlight ---
                }
                else
                {
                    Debug.LogError("InventoryUI: Instantiated slot prefab is missing InventorySlot component!", slotGO);
                    Destroy(slotGO); // Clean up invalid instance
                }
            }
            // If slotData is null or itemData is null, we simply don't instantiate a slot for it.
        }
        
        // The Grid Layout Group component on slotsParent will handle positioning.
    }
}
