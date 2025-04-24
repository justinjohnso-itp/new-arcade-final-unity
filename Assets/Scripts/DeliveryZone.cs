using UnityEngine;

/// <summary>
/// Manages the state and appearance of a delivery zone.
/// Attach this to the 'DeliveryZone_Placeholder' GameObject.
/// </summary>
public class DeliveryZone : MonoBehaviour
{
    [Tooltip("Visual element to change color (e.g., SpriteRenderer, UI Image)")]
    [SerializeField] private SpriteRenderer zoneVisual;
    // Or use: [SerializeField] private UnityEngine.UI.Image zoneUIImage;

    public Color RequiredColor { get; private set; } = Color.clear; // Default to clear/inactive

    /// <summary>
    /// Activates the zone and sets its required color and visual appearance.
    /// </summary>
    public void ActivateZone(Color requiredColor)
    {
        RequiredColor = requiredColor;
        gameObject.SetActive(true);

        if (zoneVisual != null)
        {
            zoneVisual.color = RequiredColor;
        }
        // else if (zoneUIImage != null)
        // {
        //     zoneUIImage.color = RequiredColor;
        // }
        else
        {
            Debug.LogWarning("DeliveryZone: No visual component assigned to change color.", this);
        }
        // Consider adding a Collider2D here if it's not already on the placeholder
        // Ensure it's set as a Trigger
    }

    /// <summary>
    /// Deactivates the zone.
    /// </summary>
    public void DeactivateZone()
    {
        RequiredColor = Color.clear;
        gameObject.SetActive(false);
    }

    // Optional: Reset on disable
    void OnDisable()
    {
        // Ensure color is reset if deactivated externally
        // RequiredColor = Color.clear; 
    }
}
