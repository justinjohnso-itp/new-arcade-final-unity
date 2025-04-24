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
    [SerializeField] private Color bottomSlotHighlightColor = Color.blue; // Color for the oldest item slot

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

        // 2. Get current inventory data
        List<InventorySlotData> inventoryData = inventoryManager.GetInventorySlots();

        // 3. Instantiate new UI slots
        for (int i = 0; i < inventoryData.Count; i++)
        {
            InventorySlotData slotData = inventoryData[i];
            if (slotData?.itemData != null) // Ensure data is valid
            {
                GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
                InventorySlot slotUI = slotGO.GetComponent<InventorySlot>();

                if (slotUI != null)
                {
                    inventorySlotsUI.Add(slotUI);
                    slotUI.UpdateSlot(slotData.itemData, slotData.quantity);

                    // Highlight Bottom Slot (Index 0)
                    if (i == 0)
                    {
                        slotUI.SetBackgroundColor(bottomSlotHighlightColor);
                    }
                }
                else
                {
                    Debug.LogError("InventoryUI: Instantiated slot prefab missing InventorySlot component!", slotGO);
                    Destroy(slotGO);
                }
            }
        }
        // Grid Layout Group handles positioning.
    }
}
