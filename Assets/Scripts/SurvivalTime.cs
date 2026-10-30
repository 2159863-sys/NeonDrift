
using UnityEngine;

public class SurvivalTimer : MonoBehaviour
{
    private float survivalTime = 0f;
    private bool scoreSaved = false;

    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth == null)
            return;

        // Count time while the player is alive
        if (playerHealth.lives > 0)
        {
            survivalTime += Time.deltaTime;
        }
        else if (!scoreSaved)
        {
            SaveHighScore();
        }
    }

    void SaveHighScore()
    {
        int finalScore = Mathf.RoundToInt(survivalTime);

        int previousBest = PlayerPrefs.GetInt(
            "BestTimeSeconds", 0
        );

        if (finalScore > previousBest)
        {
            PlayerPrefs.SetInt(
                "BestTimeSeconds",
                finalScore
            );

            PlayerPrefs.Save();

            Debug.Log("NEW HIGH SCORE: " + finalScore);
        }

        scoreSaved = true;
    }

    void OnGUI()
    {
        GUIStyle timerStyle = new GUIStyle(GUI.skin.label);

        timerStyle.fontSize = 28;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.UpperRight;
        timerStyle.normal.textColor = Color.cyan;

        GUI.Label(
            new Rect(Screen.width - 230, 20, 210, 45),
            "TIME: " + survivalTime.ToString("F1") + "s",
            timerStyle
        );
    }
}
