using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal (lateral) movement speed")]
    public float horizontalSpeed = 5f;
    [Tooltip("Automatic forward movement speed")]
    public float forwardSpeed = 1f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Read input for both keyboard and joystick
        float horizontalInput = Input.GetAxis("Horizontal");

        // Adjust forward direction to match the natural orientation of chunk prefabs
        // Use northeast (1,1) since the chunks aren't being rotated and likely built for this direction
        Vector2 forwardDir = new Vector2(1f, 1f).normalized;
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x).normalized;

        // Combine automatic forward motion with player steering
        Vector2 velocity = forwardDir * forwardSpeed
                         + rightDir   * horizontalInput * horizontalSpeed;
        rb.linearVelocity = velocity;
    }
}
