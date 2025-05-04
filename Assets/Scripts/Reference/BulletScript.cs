using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed = 10.0f;
    private GameManager gm; // Reference to the GameManager script

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        // Find the GameManager object and get the GameManager script component
        // We need this to access the AddScore method
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        Destroy(gameObject, 3);   
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime, Space.World);
            // Space.World moves the bullet in the __world space__
            // rather than the __local space__ of the bullet
            // (which is currently roated 90 degrees)
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Invader")
        {
            gm.AddScore(); // Add to the score
            Destroy(collider.gameObject); // Destroy the invader
            Destroy(gameObject); // Destroy the bullet
            // Nothing happens after this
        }
    }
}
