using System.Collections.Generic;
using UnityEngine;

// Visual only: hides the part of each neon rod that has moved past the big
// opening frame at the end of the tunnel, so no rods show "outside" in space.
// Added and set up automatically by NEON DRIFT > Tunnel Neon Lighting.
public class NeonRodTrimmer : MonoBehaviour
{
    public Transform[] sections;
    public float limitZ;            // world Z of the opening frame
    public float forwardSign = 1f;  // +1 if the frame is in the +Z direction from the camera

    class Rod
    {
        public Transform t;
        public Renderer r;
        public Vector3 pos;
        public Vector3 scale;
        public bool alongZ;
    }

    readonly List<Rod> rods = new List<Rod>();

    void Awake()
    {
        foreach (Transform section in sections)
        {
            if (section == null) continue;
            Transform container = section.Find("NeonLighting");
            if (container == null) continue;

            foreach (Transform child in container)
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r == null) continue;
                rods.Add(new Rod
                {
                    t = child,
                    r = r,
                    pos = child.localPosition,
                    scale = child.localScale,
                    alongZ = Mathf.Abs(child.forward.z) > 0.99f,
                });
            }
        }
    }

    void LateUpdate()
    {
        foreach (Rod rod in rods)
        {
            rod.t.localPosition = rod.pos;
            rod.t.localScale = rod.scale;
            Vector3 c = rod.t.position;

            if (!rod.alongZ)
            {
                rod.r.enabled = Inside(c.z);
                continue;
            }

            // Rod ends along the tunnel, ordered from the camera side to the frame side.
            float half = rod.scale.z * 0.5f;
            float near = c.z - half * forwardSign;
            float far = c.z + half * forwardSign;

            if (!Inside(near))
            {
                rod.r.enabled = false;
            }
            else if (!Inside(far))
            {
                float visible = Mathf.Abs(limitZ - near);
                rod.r.enabled = visible > 0.01f;
                rod.t.position = new Vector3(c.x, c.y, near + forwardSign * visible * 0.5f);
                rod.t.localScale = new Vector3(rod.scale.x, rod.scale.y, visible);
            }
            else
            {
                rod.r.enabled = true;
            }
        }
    }

    bool Inside(float z) => (limitZ - z) * forwardSign > 0f;
}
