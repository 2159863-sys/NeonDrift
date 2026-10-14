
using UnityEngine;

public class AsteroidMovement : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        transform.position += Vector3.back * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            PlayerHealth health = other.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage();
            }

            Destroy(gameObject);
        }
    }
}
