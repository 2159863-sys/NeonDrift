
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

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


        Vector3 newPosition = transform.position + movement * moveSpeed * Time.deltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, -5f, 5f);
        newPosition.y = Mathf.Clamp(newPosition.y, -3f, 3f);

        transform.position = newPosition;
        
    }
}
