
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6.5f;

    // Movement boundaries
    public float minX = -2.2f;
    public float maxX = 2.5f;
    public float minY = 0.6f;
    public float maxY = 2f;

    void Update()
    {
        if (Keyboard.current == null) return;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed) y = 1f;
        if (Keyboard.current.sKey.isPressed) y = -1f;

        Vector3 movement = new Vector3(x, y, 0f).normalized;

        Vector3 newPosition =
            transform.position + movement * moveSpeed * Time.deltaTime;

        // Keep spaceship inside the tunnel
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

        transform.position = newPosition;
    }
}
