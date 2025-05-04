using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvaderScript : MonoBehaviour
{
    public static float speed = 7.0f;
    private GameManager gm; // Reference to the GameManager script

    void Start()
    {
        // Find the GameManager object and get the GameManager script component
        // We need this to access the PlayerKilled method
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
    }


    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        //check to see if we collide with the wall
        if (collision.gameObject.tag == "Wall")
        {
            speed = speed*-1; //reverse speed

            //move all invaders down at once:
            GameObject[] invaders = GameObject.FindGameObjectsWithTag("Invader");

            foreach (GameObject i in invaders)
            {
                i.transform.Translate(Vector2.down * 10 * Time.deltaTime);
            }
        }

        //check to see if we collide with the player
        if (collision.gameObject.tag == "Player")
        {

            // if we collide with the player, run "PlayerKilled" from the GameManager script
            gm.PlayerKilled();
            Destroy(collision.gameObject);
        }

    }

}
