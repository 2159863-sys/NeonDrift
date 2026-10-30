using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only tool that adds subtle cyan/purple neon lines and soft lights
// to every tunnel section listed in TunnelLooper. Everything is created as a
// child named "NeonLighting" inside each section, so it loops with the tunnel.
// Open it from the menu: NEON DRIFT > Tunnel Neon Lighting
public class TunnelNeonLightingTool : EditorWindow
{
    const string ContainerName = "NeonLighting";
    const string MaterialFolder = "Assets/NeonLighting";
    const float FloorMarginInset = 1.3f;    // meters in from the wall
    const float CeilingMarginInset = 2f;    // meters in from the ceiling corner

    // Cyan floor rods, matched to the original NeonStripLeft/Right
    // (same width, height and material). Each entry is placed on both sides.
    const string RodMaterialPath = "Assets/NeonCyan.mat";
    const float RodWidth = 0.06f;
    const float RodY = 0.7f;
    static readonly Vector3[] RodPattern =
    {
        // x = distance from the middle, y = length, z = offset along the section
        new Vector3(2.3f, 2.0f, -0.6f),   // outer rods, near the side ledges
        new Vector3(0.75f, 0.8f, 1.3f),   // short inner rods
    };

    Color cyan = new Color(0f, 0.7f, 1f);
    Color purple = new Color(0.65f, 0.25f, 1f);
    float cyanGlow = 1.4f;          // lower = softer, less white
    float glowStrength = 2.5f;      // how bright the purple lines glow
    float lineThickness = 0.05f;    // in meters
    bool cyanRods = true;           // copies of NeonStripLeft/Right with different lengths
    bool floorLines = false;        // cyan, where floor meets wall (hidden under the side ledges)
    bool floorMargins = false;      // soft cyan lane lines (also mostly under the ledges)
    bool wallLines = true;          // purple, halfway up each wall
    bool ceilingLines = true;       // purple, where wall meets ceiling
    bool ceilingMargins = true;     // purple lines on the ceiling
    bool softLights = true;
    float lightIntensity = 1.5f;

    [MenuItem("NEON DRIFT/Tunnel Neon Lighting")]
    static void Open()
    {
        GetWindow<TunnelNeonLightingTool>("Neon Lighting");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        cyan = EditorGUILayout.ColorField("Cyan", cyan);
        purple = EditorGUILayout.ColorField("Purple", purple);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
        cyanGlow = EditorGUILayout.Slider("Cyan Glow", cyanGlow, 0.5f, 6f);
        glowStrength = EditorGUILayout.Slider("Purple Glow", glowStrength, 0.5f, 6f);
        lineThickness = EditorGUILayout.Slider("Line Thickness", lineThickness, 0.02f, 0.15f);
        cyanRods = EditorGUILayout.Toggle("Cyan Floor Rods", cyanRods);
        floorLines = EditorGUILayout.Toggle("Floor Corners (Cyan)", floorLines);
        floorMargins = EditorGUILayout.Toggle("Floor Margins (Cyan)", floorMargins);
        wallLines = EditorGUILayout.Toggle("Wall Middle (Purple)", wallLines);
        ceilingLines = EditorGUILayout.Toggle("Ceiling Corners (Purple)", ceilingLines);
        ceilingMargins = EditorGUILayout.Toggle("Ceiling Margins (Purple)", ceilingMargins);
        softLights = EditorGUILayout.Toggle("Soft Colored Lights", softLights);
        if (softLights)
            lightIntensity = EditorGUILayout.Slider("Light Intensity", lightIntensity, 0.2f, 4f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Add / Rebuild Neon Lighting", GUILayout.Height(30)))
            Build();
        if (GUILayout.Button("Remove Neon Lighting"))
            Remove();

        EditorGUILayout.Space();
        if (GUILayout.Button("Hide Old Left Purple Rods (NeonWalletLeft)"))
            SetOldRodsActive(false);
        if (GUILayout.Button("Show Old Left Purple Rods Again"))
            SetOldRodsActive(true);

        EditorGUILayout.HelpBox(
            "Rebuild replaces the old neon lighting, so you can tweak values and press it again. " +
            "Remember to save the scene (Ctrl+S) afterwards.", MessageType.Info);
    }

    static Transform[] FindSections()
    {
        TunnelLooper looper = Object.FindFirstObjectByType<TunnelLooper>();
        if (looper == null || looper.sections == null || looper.sections.Length == 0)
        {
            EditorUtility.DisplayDialog("Neon Lighting",
                "Couldn't find a TunnelLooper with sections in the open scene.\n" +
                "Open the MainGame scene and try again.", "OK");
            return null;
        }
        return looper.sections;
    }

    // Hides (doesn't delete) the old static purple rods that only sit on the left wall.
    static void SetOldRodsActive(bool active)
    {
        int count = 0;
        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (!t.name.StartsWith("NeonWalletLeft") || t.gameObject.scene.name == null) continue;
            Undo.RecordObject(t.gameObject, active ? "Show Old Rods" : "Hide Old Rods");
            t.gameObject.SetActive(active);
            count++;
        }
        Debug.Log($"Neon Lighting: {(active ? "showed" : "hid")} {count} old NeonWalletLeft object(s).");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void Remove()
    {
        Transform[] sections = FindSections();
        if (sections == null) return;

        foreach (Transform section in sections)
        {
            if (section == null) continue;
            Transform old = section.Find(ContainerName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void Build()
    {
        Transform[] sections = FindSections();
        if (sections == null) return;

        Remove();

        Material cyanMat = GetNeonMaterial("NeonLine_Cyan", cyan, cyanGlow);
        Material cyanSoftMat = GetNeonMaterial("NeonLine_CyanSoft", cyan, cyanGlow * 0.6f);
        Material purpleMat = GetNeonMaterial("NeonLine_Purple", purple, glowStrength);

        foreach (Transform section in sections)
        {
            if (section == null) continue;
            BuildSection(section, cyanMat, cyanSoftMat, purpleMat);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Measures the inside of the tunnel section and places the lines.
    void BuildSection(Transform section, Material cyanMat, Material cyanSoftMat, Material purpleMat)
    {
        if (!TryGetBounds(section, out Bounds bounds))
        {
            Debug.LogWarning("Neon Lighting: section has no visible meshes: " + section.name);
            return;
        }

        float zMin = bounds.min.z;
        float zMax = bounds.max.z;
        float length = zMax - zMin;
        float centerX = bounds.center.x;
        float centerY = bounds.center.y;

        // Temporarily add mesh colliders so we can raycast against the walls.
        List<MeshCollider> tempColliders = AddTempColliders(section);
        Physics.SyncTransforms();

        float floorY = MeasureDistance(section, bounds, new Vector3(centerX, centerY, 0), Vector3.down, out bool hitFloor);
        floorY = hitFloor ? centerY - floorY : bounds.min.y;

        float ceilY = MeasureDistance(section, bounds, new Vector3(centerX, centerY, 0), Vector3.up, out bool hitCeil);
        ceilY = hitCeil ? centerY + ceilY : bounds.max.y;

        float lowY = floorY + 0.25f;
        float highY = ceilY - 0.25f;

        float leftLow = WallX(section, bounds, centerX, lowY, -1);
        float rightLow = WallX(section, bounds, centerX, lowY, 1);
        float leftHigh = WallX(section, bounds, centerX, highY, -1);
        float rightHigh = WallX(section, bounds, centerX, highY, 1);

        float midY = (floorY + ceilY) * 0.5f;
        float leftMid = WallX(section, bounds, centerX, midY, -1);
        float rightMid = WallX(section, bounds, centerX, midY, 1);

        // Ceiling height measured near the margins (the middle of the ceiling may be open).
        float marginCeilX = (rightHigh - leftHigh) * 0.5f - CeilingMarginInset;
        float dL = MeasureDistance(section, bounds, new Vector3(centerX - marginCeilX, centerY, 0), Vector3.up, out bool hitL);
        float dR = MeasureDistance(section, bounds, new Vector3(centerX + marginCeilX, centerY, 0), Vector3.up, out bool hitR);
        float marginCeilY = (hitL && hitR) ? centerY + Mathf.Min(dL, dR) : ceilY;

        foreach (MeshCollider c in tempColliders) Object.DestroyImmediate(c);

        // Mirror both sides around the tunnel's middle so left and right match exactly.
        float midX = (leftLow + rightLow + leftHigh + rightHigh) * 0.25f;
        float halfLow = (rightLow - leftLow) * 0.5f;
        float halfHigh = (rightHigh - leftHigh) * 0.5f;
        float halfMid = (rightMid - leftMid) * 0.5f;
        leftLow = midX - halfLow; rightLow = midX + halfLow;
        leftHigh = midX - halfHigh; rightHigh = midX + halfHigh;
        centerX = midX;

        Debug.Log($"Neon Lighting: {section.name} width at floor {halfLow * 2f:F2} m, " +
                  $"at ceiling {halfHigh * 2f:F2} m, height {ceilY - floorY:F2} m");

        GameObject container = new GameObject(ContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Add Neon Lighting");
        container.transform.SetParent(section, false);

        const float inset = 0.06f; // keep lines just in front of the walls
        float zCenter = (zMin + zMax) * 0.5f;
        float t = lineThickness;

        if (floorLines)
        {
            float y = floorY + t * 0.5f + 0.02f;
            CreateBar(container.transform, "FloorLine_Left", cyanMat,
                new Vector3(leftLow + inset, y, zCenter), new Vector3(t, t, length));
            CreateBar(container.transform, "FloorLine_Right", cyanMat,
                new Vector3(rightLow - inset, y, zCenter), new Vector3(t, t, length));
        }

        if (ceilingLines)
        {
            float y = ceilY - t * 0.5f - 0.02f;
            CreateBar(container.transform, "CeilingLine_Left", purpleMat,
                new Vector3(leftHigh + inset, y, zCenter), new Vector3(t, t, length));
            CreateBar(container.transform, "CeilingLine_Right", purpleMat,
                new Vector3(rightHigh - inset, y, zCenter), new Vector3(t, t, length));
        }

        if (cyanRods)
        {
            Material rodMat = AssetDatabase.LoadAssetAtPath<Material>(RodMaterialPath);
            if (rodMat == null) rodMat = cyanMat;
            float zMid = bounds.center.z;
            foreach (Vector3 rod in RodPattern)
            {
                Vector3 size = new Vector3(RodWidth, RodWidth, rod.y);
                CreateBar(container.transform, "CyanRod_Left", rodMat,
                    new Vector3(-rod.x, RodY, zMid + rod.z), size);
                CreateBar(container.transform, "CyanRod_Right", rodMat,
                    new Vector3(rod.x, RodY, zMid + rod.z), size);
            }
        }

        if (floorMargins)
        {
            // Lane-edge lines on the floor, a bit in from each wall.
            float y = floorY + t * 0.5f + 0.02f;
            float x = halfLow - FloorMarginInset;
            CreateBar(container.transform, "FloorMargin_Left", cyanSoftMat,
                new Vector3(midX - x, y, zCenter), new Vector3(t, t, length));
            CreateBar(container.transform, "FloorMargin_Right", cyanSoftMat,
                new Vector3(midX + x, y, zCenter), new Vector3(t, t, length));
        }

        if (wallLines)
        {
            float x = halfMid - inset;
            CreateBar(container.transform, "WallLine_Left", purpleMat,
                new Vector3(midX - x, midY, zCenter), new Vector3(t, t, length));
            CreateBar(container.transform, "WallLine_Right", purpleMat,
                new Vector3(midX + x, midY, zCenter), new Vector3(t, t, length));
        }

        if (ceilingMargins)
        {
            float y = marginCeilY - t * 0.5f - 0.02f;
            CreateBar(container.transform, "CeilingMargin_Left", purpleMat,
                new Vector3(midX - marginCeilX, y, zCenter), new Vector3(t, t, length));
            CreateBar(container.transform, "CeilingMargin_Right", purpleMat,
                new Vector3(midX + marginCeilX, y, zCenter), new Vector3(t, t, length));
        }

        if (softLights)
        {
            float range = Mathf.Max(rightLow - leftLow, ceilY - floorY);
            CreateLight(container.transform, "SoftLight_Cyan", cyan,
                new Vector3(centerX, floorY + 0.5f, zCenter), range);
            CreateLight(container.transform, "SoftLight_Purple", purple,
                new Vector3(centerX, ceilY - 0.5f, zCenter), range);
        }
    }

    // Finds the wall's X position at a given height (median of a few samples along the section).
    static float WallX(Transform section, Bounds bounds, float centerX, float y, int side)
    {
        float d = MeasureDistance(section, bounds, new Vector3(centerX, y, 0), Vector3.right * side, out bool hit);
        if (!hit) return side < 0 ? bounds.min.x : bounds.max.x;
        return centerX + d * side;
    }

    // Casts several rays along the section's length and returns the median hit distance.
    // Only hits that belong to this section count (ship, asteroids, etc. are ignored).
    static float MeasureDistance(Transform section, Bounds bounds, Vector3 origin, Vector3 dir, out bool hit)
    {
        List<float> distances = new List<float>();
        float maxDist = bounds.size.magnitude;

        for (int i = 1; i <= 5; i++)
        {
            origin.z = Mathf.Lerp(bounds.min.z, bounds.max.z, i / 6f);
            RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist);
            float closest = float.MaxValue;
            foreach (RaycastHit h in hits)
            {
                if (h.collider.transform.IsChildOf(section) && h.distance < closest)
                    closest = h.distance;
            }
            if (closest < float.MaxValue) distances.Add(closest);
        }

        hit = distances.Count > 0;
        if (!hit) return 0f;
        distances.Sort();
        return distances[distances.Count / 2];
    }

    static bool TryGetBounds(Transform section, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (Renderer r in section.GetComponentsInChildren<Renderer>())
        {
            if (r.transform.IsChildOf(section) && r.transform.parent != null &&
                r.transform.parent.name == ContainerName) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    static List<MeshCollider> AddTempColliders(Transform section)
    {
        List<MeshCollider> added = new List<MeshCollider>();
        foreach (MeshFilter mf in section.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.hideFlags = HideFlags.HideAndDontSave;
            mc.sharedMesh = mf.sharedMesh;
            added.Add(mc);
        }
        return added;
    }

    // Creates a thin glowing box. Position and size are in world space; the size is
    // converted so it stays correct even though the corridor is scaled.
    static void CreateBar(Transform parent, string name, Material mat, Vector3 worldPos, Vector3 worldSize)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(bar.GetComponent<Collider>()); // never affects gameplay
        bar.name = name;
        bar.transform.SetParent(parent, false);
        bar.transform.position = worldPos;
        bar.transform.rotation = Quaternion.identity;

        Vector3 s = parent.lossyScale;
        bar.transform.localScale = new Vector3(
            worldSize.x / Mathf.Abs(s.x),
            worldSize.y / Mathf.Abs(s.y),
            worldSize.z / Mathf.Abs(s.z));

        MeshRenderer mr = bar.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void CreateLight(Transform parent, string name, Color color, Vector3 worldPos, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = lightIntensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    // Creates (or updates) a glowing URP Lit material in Assets/NeonLighting.
    static Material GetNeonMaterial(string name, Color color, float glow)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "NeonLighting");

        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        Color baseColor = color * 0.3f;
        baseColor.a = 1f;
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", 0.5f);
        mat.EnableKeyword("_EMISSION");
        Color emission = color * glow;
        emission.a = 1f;
        mat.SetColor("_EmissionColor", emission);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
