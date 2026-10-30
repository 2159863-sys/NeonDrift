
using UnityEngine;

public class EnergyOrbSpawner : MonoBehaviour
{
    public GameObject energyOrbPrefab;

    // Rare collectibles: one every 1-2 minutes
    public float minSpawnInterval = 60f;
    public float maxSpawnInterval = 120f;

    private float spawnTimer;

    void Start()
    {
        spawnTimer = Random.Range(
            minSpawnInterval,
            maxSpawnInterval
        );
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnEnergyOrb();

            spawnTimer = Random.Range(
                minSpawnInterval,
                maxSpawnInterval
            );
        }
    }

    void SpawnEnergyOrb()
    {
        if (energyOrbPrefab == null) return;

        float x = Random.Range(-1.5f, 1.5f);
        float y = Random.Range(0.9f, 1.7f);

        // Same starting distance as asteroids
        Vector3 spawnPosition = new Vector3(
            x,
            y,
            20f
        );

        Instantiate(
            energyOrbPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}
