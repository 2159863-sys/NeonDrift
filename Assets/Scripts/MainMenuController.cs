
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public TMP_Text highScoreText;

    void Start()
    {
        // Load the saved highest survival time
        int bestTime = PlayerPrefs.GetInt("BestTimeSeconds", 0);

        if (highScoreText != null)
        {
            highScoreText.text = "HIGHEST SCORE: " + bestTime + "s";
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("MainGame");
    }
}
