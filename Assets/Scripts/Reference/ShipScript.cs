using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipScript : MonoBehaviour
{

    //ship speed
    public float speed = 10.0f;


    //horizontal vector to store our input values
    private Vector2 horizontal;

    // create a bullet object
    public GameObject bullet;

    private AudioSource laserSound;

    void Start()
    {
       laserSound = GetComponent<AudioSource>(); // get audio source from the ship object
    }


    void Update()
    {

        //Check for inputs, set equal to vector's X component
        horizontal.x = Input.GetAxisRaw("Horizontal");

        //move ship along the X axis according to speed
        transform.Translate(horizontal * speed * Time.deltaTime);
        
        // if we press spacebar, initialize a bullet
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(bullet, transform.position, Quaternion.Euler(0, 0, 90));
            laserSound.Play();
        }
    }
}
