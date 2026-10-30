
using UnityEngine;

public class EnergyOrbMovement : MonoBehaviour
{
    public float speed = 6f;
    public int points = 10;

    void Update()
    {
        // Move the orb toward the player
        transform.position += Vector3.back * speed * Time.deltaTime;

        // Destroy the orb after it passes the player
        if (transform.position.z < -5f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerScore playerScore =
                other.GetComponent<PlayerScore>();

            if (playerScore != null)
            {
                playerScore.AddPoints(points);

                Debug.Log("Energy collected! +10");
            }

            Destroy(gameObject);
        }
    }
}
