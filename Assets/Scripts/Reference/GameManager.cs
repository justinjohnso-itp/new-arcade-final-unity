using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public float counter = 60f;
    //Grab the ship object
    public GameObject ship;

    //Grab the invader object
    public GameObject invader;

    // UI for scorekeeping
    public static int score = 0;
    public TMP_Text scoreText;
    private int highScore;

    // Variables for tracking lives
    public static int lives = 3;
    public GameObject life;

    // Start is called before the first frame update
    void Start()
    {

        highScore = PlayerPrefs.GetInt("HighScore");

        Instantiate(ship, new Vector2(0, -3), Quaternion.identity);

        //draw 10 invaders across
        for (int i = -5; i < 5; i++)
        {
            //draw 3 rows down
            for (int j = 2; j < 5; j++)
            {
                Instantiate(invader, new Vector2(i, j), Quaternion.identity);
            }

        }

        //draw 3 life sprites
        for (int i = 0; i < lives; i++)
        {
            Instantiate(life, new Vector2(i+6, 4), Quaternion.identity);
        }

    }

    // Update is called once per frame
    void Update()
    {   
        float diff = counter - Time.time;
        scoreText.text = "Time: " + diff;
        
    }

    // Add to the score
    public void AddScore()
    {
        score += 1;
        // scoreText.text = "Score: " + score;

        if (score > highScore)
        {
            highScore = score;
        }
    }

    public void PlayerKilled()
    {
        lives -=1;
        Debug.Log(highScore);

        PlayerPrefs.SetInt("HighScore", highScore);

        if (lives < 0)
        {   
            SceneManager.LoadScene("TitleScene");
        } 
        else
        {
            SceneManager.LoadScene("SpaceScene");
        }
    }
}
