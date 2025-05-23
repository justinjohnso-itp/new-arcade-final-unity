using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent object for InventorySlot prefabs.")]
    [SerializeField] private Transform slotsParent;
    [Tooltip("Prefab for a single inventory slot UI element.")]
    [SerializeField] private GameObject inventorySlotPrefab;

    [Header("Highlighting")]
    [SerializeField] private Color highlightedSlotColor = Color.blue; // Renamed from bottomSlotHighlightColor

    private InventoryManager inventoryManager;
    private List<InventorySlot> inventorySlotsUI = new List<InventorySlot>();

    void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryUI: InventoryManager instance not found!");
            this.enabled = false;
            return;
        }
        if (slotsParent == null)
        {
            Debug.LogError("InventoryUI: Slots Parent transform not assigned!");
            this.enabled = false;
            return;
        }
        if (inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryUI: Inventory Slot Prefab not assigned!");
            this.enabled = false;
            return;
        }

        inventoryManager.OnInventoryChanged += UpdateUI;
        UpdateUI(); // Initial update
    }

    void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// Clears existing slots and instantiates new ones based on InventoryManager data.
    /// Skips creating UI for null slots in the data.
    /// Highlights the slot indicated by InventoryManager.
    /// </summary>
    private void UpdateUI()
    {
        if (inventoryManager == null || slotsParent == null || inventorySlotPrefab == null) return;

        // 1. Clear existing UI slots
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        inventorySlotsUI.Clear();

        // 2. Get current inventory data and highlighted index
        List<InventorySlotData> inventoryData = inventoryManager.GetInventorySlots();
        int highlightedIndex = inventoryManager.GetHighlightedSlotIndex();

        // 3. Instantiate new UI slots ONLY for non-null data
        for (int i = 0; i < inventoryData.Count; i++)
        {
            InventorySlotData slotData = inventoryData[i];

            // *** Check if the slot data is null ***
            if (slotData == null)
            {
                // If data is null, do not create a UI element for this slot.
                // The layout group will handle the visual arrangement.
                continue; // Skip to the next iteration
            }

            // If slotData is not null, proceed to create the UI element
            if (slotData.itemData != null) // Additional check for valid item data within the slot
            {
                GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
                InventorySlot slotUI = slotGO.GetComponent<InventorySlot>();

                if (slotUI != null)
                {
                    inventorySlotsUI.Add(slotUI);
                    slotUI.UpdateSlot(slotData.itemData, slotData.quantity);

                    // Highlight the correct slot based on the index from InventoryManager
                    // This index corresponds to the position in the potentially sparse inventoryData list
                    if (i == highlightedIndex)
                    {
                        slotUI.SetBackgroundColor(highlightedSlotColor);
                    }
                    else
                    {
                        slotUI.ResetBackgroundColor(); // Ensure others are not highlighted
                    }
                }
                else
                {
                    Debug.LogError("InventoryUI: Instantiated slot prefab missing InventorySlot component!", slotGO);
                    Destroy(slotGO);
                }
            }
            else
            {
                 Debug.LogWarning($"InventoryUI: Slot data at index {i} exists but has null itemData.");
            }
        }
        // Layout Group handles positioning.
    }
}
