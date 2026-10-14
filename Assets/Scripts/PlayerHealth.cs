
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int lives = 3;

    public void TakeDamage()
    {
        if (lives <= 0) return;

        lives--;

        Debug.Log("Lives remaining: " + lives);

        if (lives <= 0)
        {
            Debug.Log("GAME OVER!");
        }
    }
}
