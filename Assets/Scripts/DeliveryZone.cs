using UnityEngine;

/// <summary>
/// Manages the state and appearance of a delivery zone.
/// Attach this to the 'DeliveryZone_Placeholder' GameObject.
/// </summary>
public class DeliveryZone : MonoBehaviour
{
    [Tooltip("Visual element to change color (e.g., SpriteRenderer)")]
    [SerializeField] private SpriteRenderer zoneVisual;

    public Color RequiredColor { get; private set; } = Color.clear; // Default to inactive

    /// <summary>
    /// Activates the zone, setting its required color and visual appearance.
    /// </summary>
    public void ActivateZone(Color requiredColor)
    {
        RequiredColor = requiredColor;
        gameObject.SetActive(true);

        if (zoneVisual != null)
        {
            zoneVisual.color = RequiredColor;
        }
        else
        {
            Debug.LogWarning("DeliveryZone: No visual component assigned.", this);
        }
        // Ensure Collider2D is present and set as Trigger on the placeholder prefab.
    }

    /// <summary>
    /// Deactivates the zone.
    /// </summary>
    public void DeactivateZone()
    {
        RequiredColor = Color.clear;
        gameObject.SetActive(false);
    }

    // Optional: Reset state if deactivated externally
    // void OnDisable()
    // {
    //     RequiredColor = Color.clear; 
    // }
}
