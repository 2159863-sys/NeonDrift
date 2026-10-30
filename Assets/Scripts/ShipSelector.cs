
using UnityEngine;

public class ShipSelector : MonoBehaviour
{
    public GameObject[] ships;

    private int selectedIndex = 0;

    void Start()
    {
        ShowSelectedShip();
    }

    void ShowSelectedShip()
    {
        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i] != null)
            {
                ships[i].SetActive(i == selectedIndex);
            }
        }

        PlayerPrefs.SetInt("SelectedShip", selectedIndex);
    }

    public void NextShip()
    {
        if (ships.Length == 0) return;

        selectedIndex = (selectedIndex + 1) % ships.Length;
        ShowSelectedShip();
    }

    public void PreviousShip()
    {
        if (ships.Length == 0) return;

        selectedIndex = (selectedIndex - 1 + ships.Length) % ships.Length;
        ShowSelectedShip();
    }
}
