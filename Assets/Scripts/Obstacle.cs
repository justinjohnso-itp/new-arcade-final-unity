using UnityEngine;
using System.Collections; // Required for Coroutines

public class Obstacle : MonoBehaviour
{
    public float flyAwayForce = 5f;
    public float flyAwayTorque = 10f;
    public float destroyDelay = 1.5f; // Time before destroying after being hit

    private Rigidbody2D rb;
    private Collider2D col;
    private bool hit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ensure Rigidbody2D exists, add if necessary (though it's better to add it in the prefab)
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1; // Adjust gravity as needed, maybe less for a floaty effect
            Debug.LogWarning($"Obstacle {name} was missing Rigidbody2D. Added one.", this);
        }
    }

    // This method will be called by the PlayerController upon collision
    public void HandleHit(Vector2 hitDirection)
    {
        if (hit) return; // Prevent multiple hits
        hit = true;

        Debug.Log($"{name} hit!");

        // Disable collider immediately to prevent further collisions
        if (col != null)
        {
            col.enabled = false;
        }

        // Apply physics effect
        if (rb != null)
        {
            // Make it dynamic if it wasn't already (e.g., if it was kinematic)
            rb.bodyType = RigidbodyType2D.Dynamic; // Use bodyType instead of isKinematic
            rb.gravityScale = 1; // Ensure gravity affects it now

            // Apply force in the direction away from the hit
            Vector2 forceDirection = (transform.position - (Vector3)hitDirection).normalized;
            rb.AddForce(forceDirection * flyAwayForce, ForceMode2D.Impulse);

            // Apply random torque
            rb.AddTorque((Random.value > 0.5f ? 1f : -1f) * flyAwayTorque, ForceMode2D.Impulse);
        }

        // Start the destruction timer
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
