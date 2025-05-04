using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleScript : MonoBehaviour
{

    private int highScore;
    public TMP_Text highScoreText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore");
        highScoreText.text = "High Score: " + highScore;

        GameManager.lives = 3;      
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.anyKeyDown)
        {
            // Load the game scene
            SceneManager.LoadScene("SpaceScene");
        }
        
    }
}
