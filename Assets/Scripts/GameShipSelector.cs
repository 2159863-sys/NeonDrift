
using UnityEngine;

public class GameShipSelector : MonoBehaviour
{
    public GameObject[] ships;

    void Start()
    {
        // Read the spaceship chosen in MainMenu
        int selectedShip = PlayerPrefs.GetInt("SelectedShip", 0);

        if (ships == null || ships.Length == 0)
            return;

        selectedShip = Mathf.Clamp(
            selectedShip, 0, ships.Length - 1
        );

        // Show only the selected spaceship
        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i] != null)
            {
                ships[i].SetActive(i == selectedShip);
            }
        }

        Debug.Log("Selected spaceship: " + selectedShip);
    }
}
