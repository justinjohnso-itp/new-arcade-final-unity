using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure TextMeshPro is imported

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button slotButton; // Optional: for interaction
    [SerializeField] private Image backgroundPanel; // Added background reference

    private InventoryItemData currentItemData;
    private int currentQuantity;

    // Store default background color
    private Color defaultBackgroundColor;

    void Awake()
    {
        // Store the default background color on awake
        if (backgroundPanel != null)
        {
            defaultBackgroundColor = backgroundPanel.color;
        }
        else
        {
            defaultBackgroundColor = Color.white; // Fallback
        }

        // Ensure quantity text is hidden initially if empty
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }
        // Ensure icon is cleared initially
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        // Optional: Add listener for button clicks
        // slotButton?.onClick.AddListener(OnSlotClicked);

        ClearSlot(); // Ensure clean state on awake
    }

    /// <summary>
    /// Updates the slot display with the given item data and quantity.
    /// </summary>
    public void UpdateSlot(InventoryItemData itemData, int quantity)
    {
        currentItemData = itemData;
        currentQuantity = quantity;

        if (currentItemData != null && currentQuantity > 0)
        {
            // Update Icon
            if (iconImage != null)
            {
                iconImage.sprite = currentItemData.icon;
                iconImage.enabled = true;
            }

            // Update Quantity Text (only show if stackable and quantity > 1)
            if (quantityText != null)
            {
                bool showQuantity = currentItemData.canStack && currentQuantity > 1;
                quantityText.gameObject.SetActive(showQuantity);
                if (showQuantity)
                {
                    quantityText.text = currentQuantity.ToString();
                }
            }
            // Enable button interaction if needed
            // if (slotButton != null) slotButton.interactable = true;
        }
        else
        {
            // Clear the slot if no item or zero quantity
            ClearSlot();
        }
    }

    /// <summary>
    /// Clears the visual representation of the slot.
    /// </summary>
    public void ClearSlot()
    {
        currentItemData = null;
        currentQuantity = 0;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (quantityText != null)
        { 
            quantityText.gameObject.SetActive(false);
        }
        // Disable button interaction if needed
        // if (slotButton != null) slotButton.interactable = false;

        // Reset background color on clear
        ResetBackgroundColor(); 
    }

    // Optional: Handle slot interaction
    private void OnSlotClicked()
    {
        if (currentItemData != null)
        {
            Debug.Log($"Clicked on slot containing: {currentItemData.itemName}");
            // Add logic for using/selecting the item
        }
    }

    /// <summary>
    /// Sets the background color of the slot.
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.color = color;
        }
    }

    /// <summary>
    /// Resets the background color to its default.
    /// </summary>
    public void ResetBackgroundColor()
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.color = defaultBackgroundColor;
        }
    }

    // Optional: Add methods for drag-and-drop if needed
}
