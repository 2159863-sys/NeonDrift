
using UnityEngine;

public class TunnelLooper : MonoBehaviour
{
    public Transform[] sections;
    public float moveSpeed = 3f;
    public float sectionLength = 4f;

    void Update()
    {
        // Move tunnel sections toward the player
        foreach (Transform section in sections)
        {
            if (section == null) continue;

            section.localPosition +=
                Vector3.forward * moveSpeed * Time.deltaTime;
        }

        // Recycle sections that pass the player
        foreach (Transform section in sections)
        {
            if (section == null) continue;

            if (section.localPosition.z >= sectionLength)
            {
                float lastZ = float.PositiveInfinity;

                foreach (Transform other in sections)
                {
                    if (other != null && other != section)
                    {
                        lastZ = Mathf.Min(
                            lastZ, other.localPosition.z);
                    }
                }

                if (float.IsPositiveInfinity(lastZ))
                    continue;

                Vector3 position = section.localPosition;
                position.z = lastZ - sectionLength;
                section.localPosition = position;
            }
        }
    }
}
