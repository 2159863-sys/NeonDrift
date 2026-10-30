
using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int energyPoints = 0;
    public int batteryCharge = 0;
    public int orbsPerLife = 3;

    // Raised for every collected energy orb, including the one that fills the bar (used by NeonDriftAudio)
    public event System.Action EnergyCollected;

    private PlayerHealth playerHealth;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    public void AddPoints(int amount)
    {
        energyPoints += amount;
        EnergyCollected?.Invoke();

        batteryCharge = Mathf.Min(
            batteryCharge + 1,
            orbsPerLife
        );

        TryUseFullBattery();
    }

    public void TryUseFullBattery()
    {
        if (playerHealth == null) return;

        if (batteryCharge >= orbsPerLife)
        {
            if (playerHealth.HealOneLife())
            {
                batteryCharge = 0;
                Debug.Log("Life restored!");
            }
        }
    }
}
