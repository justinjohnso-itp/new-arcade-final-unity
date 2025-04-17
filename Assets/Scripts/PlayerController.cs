using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal (lateral) movement speed")]
    public float horizontalSpeed = 5f;
    [Tooltip("Automatic forward movement speed")]
    public float forwardSpeed = 5f;
    [Tooltip("Adjust forward direction slightly along the isometric Y axis (-1 to 1)")]
    [Range(-1f, 1f)]
    public float lateralAdjustment = 0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Read input for both keyboard and joystick
        float horizontalInput = Input.GetAxis("Horizontal");

        Vector2 forwardDir = GameSettings.ForwardDirection;
        Vector2 rightDir = GameSettings.IsometricYDirection;
        
        // Apply lateral adjustment to forward direction
        Vector2 adjustedForwardDir = forwardDir + rightDir * lateralAdjustment * 0.2f;
        adjustedForwardDir.Normalize(); // Ensure we maintain a unit vector
        
        // Combine adjusted forward motion with player steering
        Vector2 velocity = adjustedForwardDir * forwardSpeed
                         + rightDir * horizontalInput * horizontalSpeed;
        rb.linearVelocity = velocity;
    }
}
