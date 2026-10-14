
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject asteroidPrefab;

    private float spawnInterval = 5f;
    private float spawnTimer = 3f;
    private float elapsedTime = 0f;

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Increase the number of asteroids over time
        if (elapsedTime < 20f)
        {
            spawnInterval = 5f;
        }
        else if (elapsedTime < 40f)
        {
            spawnInterval = 3f;
        }
        else
        {
            spawnInterval = 2f;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnAsteroid();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnAsteroid()
    {
        if (asteroidPrefab == null) return;

        float x = Random.Range(-2f, 2f);
        float y = Random.Range(0.5f, 2f);

        Vector3 spawnPosition = new Vector3(x, y, 20f);

        Instantiate(asteroidPrefab, spawnPosition,
            Quaternion.identity);
    }
}
