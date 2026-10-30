
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int lives = 3;
    public int maxLives = 3;
    public float invincibilityTime = 1.5f;

    // Raised each time an asteroid hit actually removes a life (used by NeonDriftAudio)
    public event System.Action LifeLost;

    private float nextDamageTime = 0f;
    private PlayerScore playerScore;

    void Awake()
    {
        playerScore = GetComponent<PlayerScore>();
    }

    void Start()
    {
        Time.timeScale = 1f;
    }

    public void TakeDamage()
    {
        if (lives <= 0 || Time.time < nextDamageTime)
            return;

        lives--;
        nextDamageTime = Time.time + invincibilityTime;
        LifeLost?.Invoke();

        if (playerScore != null)
            playerScore.TryUseFullBattery();

        Debug.Log("Lives remaining: " + lives);

        if (lives <= 0)
        {
            Debug.Log("GAME OVER!");
            Time.timeScale = 0f;
        }
    }

    public bool HealOneLife()
    {
        if (lives >= maxLives)
            return false;

        lives++;
        Debug.Log("Life restored! Lives: " + lives);
        return true;
    }

    void OnGUI()
    {
        // Permanent lives display
        GUIStyle livesStyle = new GUIStyle(GUI.skin.label);
        livesStyle.fontSize = 28;
        livesStyle.fontStyle = FontStyle.Bold;
        livesStyle.normal.textColor = Color.cyan;

        GUI.Label(
            new Rect(20, 20, 350, 45),
            "LIVES: " + lives + "/" + maxLives,
            livesStyle
        );

        // Permanent cyan charging bar
        float x = 20f;
        float y = 75f;
        float width = 220f;
        float height = 28f;

        int charge = playerScore != null
            ? playerScore.batteryCharge : 0;

        int required = playerScore != null
            ? Mathf.Max(1, playerScore.orbsPerLife) : 3;

        float fill = Mathf.Clamp01(
            (float)charge / required
        );

        Color oldColor = GUI.color;

        // Cyan border
        GUI.color = Color.cyan;
        GUI.DrawTexture(
            new Rect(x, y, width, height),
            Texture2D.whiteTexture
        );

        // Dark background
        GUI.color = new Color(0.08f, 0.12f, 0.18f);
        GUI.DrawTexture(
            new Rect(x + 3, y + 3, width - 6, height - 6),
            Texture2D.whiteTexture
        );

        // Cyan fill
        GUI.color = Color.cyan;
        GUI.DrawTexture(
            new Rect(
                x + 3,
                y + 3,
                (width - 6) * fill,
                height - 6
            ),
            Texture2D.whiteTexture
        );

        GUI.color = oldColor;

        // Game Over
        if (lives <= 0)
        {
            GUI.Box(
                new Rect(0, 0, Screen.width, Screen.height),
                ""
            );

            GUIStyle gameOverStyle =
                new GUIStyle(GUI.skin.label);

            gameOverStyle.fontSize = 42;
            gameOverStyle.fontStyle = FontStyle.Bold;
            gameOverStyle.alignment =
                TextAnchor.MiddleCenter;
            gameOverStyle.normal.textColor =
                Color.magenta;

            GUI.Label(
                new Rect(
                    Screen.width / 2f - 200,
                    Screen.height / 2f - 100,
                    400,
                    70
                ),
                "GAME OVER",
                gameOverStyle
            );

            if (GUI.Button(
                new Rect(
                    Screen.width / 2f - 100,
                    Screen.height / 2f,
                    200,
                    60
                ),
                "RESTART"
            ))
            {
                RestartGame();
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}
