using UnityEngine;

/// <summary>
/// Defines the static data for an inventory item.
/// Create instances of this via Assets > Create > Inventory > Item Data
/// </summary>
[CreateAssetMenu(fileName = "New ItemData", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Tooltip("The name of the item displayed in the UI.")]
    public string itemName = "New Item";
    [Tooltip("A brief description of the item.")]
    [TextArea]
    public string description = "Item Description";
    [Tooltip("The icon representing the item in the UI.")]
    public Sprite icon = null;
    [Tooltip("The color associated with this item, used for matching zones.")]
    public Color itemColor = Color.white;
    [Tooltip("Can multiple instances of this item stack in one slot?")]
    public bool canStack = false;
    [Tooltip("If stackable, what is the maximum number per stack?")]
    public int maxStackSize = 1;
    // Add other relevant static data like item type, value, effects, etc.
}
