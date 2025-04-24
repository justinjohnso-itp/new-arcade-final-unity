using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure TextMeshPro is imported

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button slotButton; // Optional: for interaction
    [SerializeField] private Image backgroundPanel; // Reference to background image

    private InventoryItemData currentItemData;
    private int currentQuantity;
    private Color defaultBackgroundColor;

    void Awake()
    {
        if (backgroundPanel != null)
        {
            defaultBackgroundColor = backgroundPanel.color;
        }
        else
        {
            defaultBackgroundColor = Color.white; // Fallback
        }

        // Ensure clean state on awake
        ClearSlot(); 
    }

    /// <summary>
    /// Updates the slot display.
    /// </summary>
    public void UpdateSlot(InventoryItemData itemData, int quantity)
    {
        currentItemData = itemData;
        currentQuantity = quantity;

        if (currentItemData != null && currentQuantity > 0)
        {
            if (iconImage != null)
            {
                iconImage.sprite = currentItemData.icon;
                iconImage.enabled = true;
            }

            // Show quantity text only if stackable and quantity > 1
            if (quantityText != null)
            {
                bool showQuantity = currentItemData.canStack && currentQuantity > 1;
                quantityText.gameObject.SetActive(showQuantity);
                if (showQuantity)
                {
                    quantityText.text = currentQuantity.ToString();
                }
            }
            // Optional: Enable button interaction
            // if (slotButton != null) slotButton.interactable = true;
        }
        else
        {
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
        // Optional: Disable button interaction
        // if (slotButton != null) slotButton.interactable = false;

        ResetBackgroundColor(); 
    }

    // Optional: Handle slot interaction
    private void OnSlotClicked()
    {
        if (currentItemData != null)
        {
            Debug.Log($"Clicked on slot containing: {currentItemData.itemName}");
            // TODO: Add logic for using/selecting the item
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

    // Optional: Add methods for drag-and-drop
}
