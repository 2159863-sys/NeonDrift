using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only tool that adds cyan/purple neon rods and soft lights to the tunnel.
// It scans the real corridor shape with raycasts, so every rod sits flush on a
// surface (floor edges, ledges, wall panels, ceiling panels, the opening frame).
// Rods are created in a child named "NeonLighting" inside each tunnel section,
// so they move and loop with the tunnel. Nothing here runs in the game itself.
// Open it from the menu: NEON DRIFT > Tunnel Neon Lighting
public class TunnelNeonLightingTool : EditorWindow
{
    const string ContainerName = "NeonLighting";
    const string MaterialFolder = "Assets/NeonLighting";

    // Short rods in the middle floor lane: x = distance from the middle,
    // y = length, z = offset along the section. Each entry is placed on both sides.
    static readonly Vector3[] MiddleLanePattern =
    {
        new Vector3(0.75f, 0.8f, 1.3f),
        new Vector3(0.75f, 0.8f, -0.7f),
    };

    const float FlushTolerance = 0.02f;   // max gap between a rod and its surface
    const float CenterClearX = 1.2f;      // keep the ceiling middle free of rods

    Color cyan = new Color(0f, 0.8f, 1f);
    Color purple = new Color(0.62f, 0.2f, 1f);
    float cyanGlow = 2.0f;
    float purpleGlow = 1.8f;
    float thickness = 0.045f;
    bool floorRods = true;
    bool middleLane = true;
    bool wallRods = true;
    bool verticalWallRods = true;
    bool ceilingRods = true;
    bool openingFrame = true;
    bool softLights = true;
    float lightIntensity = 1.5f;

    struct Rod
    {
        public Vector3 c;   // center (world)
        public Vector3 T;   // length direction
        public Vector3 N;   // surface normal
        public float len;
        public Material mat;
        public string name;
    }

    class Face
    {
        public List<Vector2> pts = new List<Vector2>();
        public Vector2 n;
        public Vector2 A => pts[0];
        public Vector2 B => pts[pts.Count - 1];
        public float Length => Vector2.Distance(A, B);
    }

    [MenuItem("Window/Neon Drift/Tunnel Neon Lighting")]
    static void Open()
    {
        GetWindow<TunnelNeonLightingTool>("Neon Lighting");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        cyan = EditorGUILayout.ColorField("Cyan", cyan);
        purple = EditorGUILayout.ColorField("Purple", purple);
        cyanGlow = EditorGUILayout.Slider("Cyan Glow", cyanGlow, 0.5f, 5f);
        purpleGlow = EditorGUILayout.Slider("Purple Glow", purpleGlow, 0.5f, 5f);
        thickness = EditorGUILayout.Slider("Rod Thickness", thickness, 0.02f, 0.1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Where to place rods", EditorStyles.boldLabel);
        floorRods = EditorGUILayout.Toggle("Floor Edges", floorRods);
        middleLane = EditorGUILayout.Toggle("Middle Floor Lane", middleLane);
        wallRods = EditorGUILayout.Toggle("Wall Panel Edges", wallRods);
        verticalWallRods = EditorGUILayout.Toggle("Vertical Wall Rods", verticalWallRods);
        ceilingRods = EditorGUILayout.Toggle("Ceiling Panels", ceilingRods);
        openingFrame = EditorGUILayout.Toggle("Opening Frame", openingFrame);
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
            "Rods that would float, stick out or be hidden are skipped automatically. " +
            "Remember to save the scene (Ctrl+S) afterwards.", MessageType.Info);
    }

    static TunnelLooper FindLooper()
    {
        TunnelLooper looper = Object.FindFirstObjectByType<TunnelLooper>();
        if (looper == null || looper.sections == null || looper.sections.Length == 0 || looper.sections[0] == null)
        {
            EditorUtility.DisplayDialog("Neon Lighting",
                "Couldn't find a TunnelLooper with sections in the open scene.\n" +
                "Open the MainGame scene and try again.", "OK");
            return null;
        }
        return looper;
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
        TunnelLooper looper = FindLooper();
        if (looper == null) return;
        RemoveContainers(looper.transform);
        NeonRodTrimmer trimmer = looper.GetComponent<NeonRodTrimmer>();
        if (trimmer != null) Undo.DestroyObjectImmediate(trimmer);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Adds the visual-only script that hides rod parts that slide past the opening frame.
    void SetUpTrimmer(TunnelLooper looper)
    {
        Transform end = root.Find("End");
        if (end == null || !TryGetBounds(end, out Bounds b)) return;

        Camera cam = Camera.main;
        float camZ = cam != null ? cam.transform.position.z : 0f;
        bool endIsAhead = b.center.z > camZ;

        NeonRodTrimmer trimmer = looper.GetComponent<NeonRodTrimmer>();
        if (trimmer == null) trimmer = Undo.AddComponent<NeonRodTrimmer>(looper.gameObject);
        Undo.RecordObject(trimmer, "Set Up Neon Rod Trimmer");
        trimmer.sections = looper.sections;
        trimmer.forwardSign = endIsAhead ? 1f : -1f;
        trimmer.limitZ = endIsAhead ? b.min.z : b.max.z;   // the frame face nearest the camera
        Debug.Log($"Neon Lighting: rods are hidden past the opening frame at z = {trimmer.limitZ:F2}.");
    }

    static void RemoveContainers(Transform root)
    {
        List<GameObject> old = new List<GameObject>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == ContainerName) old.Add(t.gameObject);
        foreach (GameObject go in old)
            if (go != null) Undo.DestroyObjectImmediate(go);
    }

    // ------------------------------------------------------------------ build

    Transform root;       // Scifi-corridor-01 (has the TunnelLooper)
    Transform section0;   // the section we scan; results are copied to the others
    float axisX, axisY, floorY, ceilY, zMin, zMax;
    float W => thickness;          // rod width
    float D => thickness * 0.6f;   // rod depth (how far it rises from the surface)

    void Build()
    {
        TunnelLooper looper = FindLooper();
        if (looper == null) return;
        root = looper.transform;
        section0 = looper.sections[0];

        RemoveContainers(root);

        Material cyanMat = GetNeonMaterial("NeonRod_Cyan", cyan, cyanGlow);
        Material purpleMat = GetNeonMaterial("NeonRod_Purple", purple, purpleGlow);

        List<MeshCollider> temp = AddTempColliders(root);
        Physics.SyncTransforms();
        try
        {
            if (!Measure(looper.sectionLength))
            {
                Debug.LogWarning("Neon Lighting: couldn't measure the tunnel section.");
                return;
            }

            List<Rod> rods = new List<Rod>();
            int floorCount = 0, laneCount = 0, wallCount = 0, vertCount = 0, ceilCount = 0;

            List<Face> faces = new List<Face>();
            foreach (float f in new[] { 0.3f, 0.5f, 0.7f })
                faces.AddRange(Profile(section0, Mathf.Lerp(zMin, zMax, f), -90f, 90f, 1f));

            foreach (LineCandidate lc in LineCandidates(faces, cyanMat, purpleMat))
            {
                if (lc.kind == "floor" && !floorRods) continue;
                if (lc.kind == "wall" && !wallRods) continue;
                if (lc.kind == "ceiling" && !ceilingRods) continue;
                int added = AddLine(rods, lc);
                if (lc.kind == "floor") floorCount += added;
                else if (lc.kind == "wall") wallCount += added;
                else ceilCount += added;
            }

            if (middleLane) laneCount = AddMiddleLane(rods, cyanMat);
            if (verticalWallRods) vertCount = AddVerticalWallRods(rods, faces, cyanMat);

            // Copy the scanned layout into every section.
            foreach (Transform section in looper.sections)
            {
                if (section == null) continue;
                Transform container = CreateContainer(section);
                foreach (Rod r in rods)
                {
                    Vector3 c = section.TransformPoint(section0.InverseTransformPoint(r.c));
                    Quaternion rot = section.rotation * Quaternion.Inverse(section0.rotation) *
                                     Quaternion.LookRotation(r.T, r.N);
                    CreateRod(container, r.name, r.mat, c, rot, new Vector3(W, D, r.len));
                }
                if (softLights)
                {
                    Vector3 mid = section.TransformPoint(section0.InverseTransformPoint(
                        new Vector3(axisX, 0f, (zMin + zMax) * 0.5f)));
                    float range = Mathf.Max(ceilY - floorY, 6f);
                    CreateLight(container, "SoftLight_Cyan", cyan, new Vector3(mid.x, floorY + 0.5f, mid.z), range);
                    CreateLight(container, "SoftLight_Purple", purple, new Vector3(mid.x, ceilY - 0.5f, mid.z), range);
                }
            }

            int frameCount = openingFrame ? BuildOpeningFrame(purpleMat) : 0;
            SetUpTrimmer(looper);

            Debug.Log($"Neon Lighting: per section {floorCount} floor edge, {laneCount} middle lane, " +
                      $"{wallCount} wall, {vertCount} vertical wall, {ceilCount} ceiling rods; " +
                      $"{frameCount} opening frame rods. Floor {floorY:F2} m, ceiling {ceilY:F2} m.");
        }
        finally
        {
            foreach (MeshCollider c in temp) if (c != null) Object.DestroyImmediate(c);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Finds the tunnel's middle, floor and ceiling in the scanned section.
    bool Measure(float sectionLength)
    {
        if (!TryGetBounds(section0, out Bounds b)) return false;
        float zc = b.center.z;
        zMin = zc - sectionLength * 0.5f;
        zMax = zc + sectionLength * 0.5f;
        axisX = b.center.x;

        Vector3 start = new Vector3(axisX, b.center.y, zc);
        floorY = Cast(start, Vector3.down, b.size.y, section0, out RaycastHit hf) ? hf.point.y : b.min.y;
        ceilY = Cast(start, Vector3.up, b.size.y, section0, out RaycastHit hc) ? hc.point.y : b.max.y;
        axisY = floorY + (ceilY - floorY) * 0.45f;
        return true;
    }

    // ---------------------------------------------------------- cross-section

    // Casts rays outward from the tunnel's middle (right side, mirrored to the left)
    // and groups the hits into flat faces. Only points that match on both sides are
    // kept, so everything built from these faces is symmetrical.
    List<Face> Profile(Transform filter, float z, float fromDeg, float toDeg, float stepDeg)
    {
        List<Face> faces = new List<Face>();
        Face current = null;
        Vector2 axis = new Vector2(axisX, axisY);
        float maxDist = 30f;

        for (float a = fromDeg; a <= toDeg + 0.001f; a += stepDeg)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector3 dirR = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 dirL = new Vector3(-dirR.x, dirR.y, 0f);
            Vector3 o = new Vector3(axisX, axisY, z);

            bool ok = Cast(o, dirR, maxDist, filter, out RaycastHit hr) &
                      Cast(o, dirL, maxDist, filter, out RaycastHit hl);
            ok = ok && Mathf.Abs(hr.distance - hl.distance) < 0.05f &&
                 Mathf.Abs(hr.normal.z) < 0.3f && Mathf.Abs(hl.normal.z) < 0.3f &&
                 Mathf.Abs(hr.normal.x + hl.normal.x) < 0.1f && Mathf.Abs(hr.normal.y - hl.normal.y) < 0.1f;

            if (!ok) { current = null; continue; }

            float d = (hr.distance + hl.distance) * 0.5f;
            Vector2 p = axis + new Vector2(dirR.x, dirR.y) * d;
            Vector2 n = new Vector2((hr.normal.x - hl.normal.x) * 0.5f, (hr.normal.y + hl.normal.y) * 0.5f).normalized;

            if (current != null &&
                (Vector2.Angle(current.n, n) > 8f || Vector2.Distance(current.B, p) > 0.25f))
                current = null;

            if (current == null)
            {
                current = new Face { n = n };
                faces.Add(current);
            }
            current.pts.Add(p);
        }

        faces.RemoveAll(f => f.pts.Count < 2);
        return faces;
    }

    struct LineCandidate
    {
        public Vector2 q;     // point on the surface (right side)
        public Vector2 n;     // surface normal
        public Material mat;
        public bool dashed;
        public string kind;   // floor, wall, ceiling
    }

    // Picks spots along face edges (floor edges, ledges, panel seams, corners).
    IEnumerable<LineCandidate> LineCandidates(List<Face> faces, Material cyanMat, Material purpleMat)
    {
        List<LineCandidate> list = new List<LineCandidate>();
        const float edgeInset = 0.12f;

        foreach (Face f in faces)
        {
            if (f.Length < 0.3f) continue;
            Vector2 dir = (f.B - f.A).normalized;
            Vector2 nearA = f.A + dir * edgeInset;
            Vector2 nearB = f.B - dir * edgeInset;

            if (f.n.y > 0.75f)
            {
                bool mainFloor = Mathf.Abs(f.A.y - floorY) < 0.05f;
                if (mainFloor)
                    list.Add(new LineCandidate { q = nearB, n = f.n, mat = cyanMat, kind = "floor" });
                else
                {
                    // ledge top: line along the edge nearest the middle
                    Vector2 inner = Mathf.Abs(f.A.x - axisX) < Mathf.Abs(f.B.x - axisX) ? nearA : nearB;
                    list.Add(new LineCandidate { q = inner, n = f.n, mat = purpleMat, kind = "floor" });
                }
            }
            else if (f.n.y < -0.75f)
            {
                Vector2 outer = Mathf.Abs(f.A.x - axisX) > Mathf.Abs(f.B.x - axisX) ? nearA : nearB;
                Vector2 inner = outer == nearA ? nearB : nearA;
                if (Mathf.Abs(outer.x - axisX) >= CenterClearX)
                    list.Add(new LineCandidate { q = outer, n = f.n, mat = purpleMat, kind = "ceiling" });
                if (f.Length > 1.5f && Mathf.Abs(inner.x - axisX) >= CenterClearX)
                    list.Add(new LineCandidate { q = inner, n = f.n, mat = purpleMat, dashed = true, kind = "ceiling" });
            }
            else
            {
                Vector2 low = f.A.y < f.B.y ? nearA : nearB;
                Vector2 high = low == nearA ? nearB : nearA;
                if (f.Length >= 1f)
                {
                    list.Add(new LineCandidate { q = low, n = f.n, mat = cyanMat, kind = "wall" });
                    list.Add(new LineCandidate { q = high, n = f.n, mat = purpleMat, kind = "wall" });
                }
                else
                {
                    list.Add(new LineCandidate { q = (f.A + f.B) * 0.5f, n = f.n, mat = purpleMat, dashed = true, kind = "wall" });
                }
            }
        }

        // Remove near-duplicates (the profile is scanned at a few depths).
        List<LineCandidate> unique = new List<LineCandidate>();
        foreach (LineCandidate c in list)
        {
            bool dup = false;
            foreach (LineCandidate u in unique)
                if (Vector2.Distance(u.q, c.q) < 0.25f) { dup = true; break; }
            if (!dup) unique.Add(c);
        }
        return unique;
    }

    // Builds rods running along the tunnel wherever the surface is flat and continuous.
    int AddLine(List<Rod> rods, LineCandidate lc)
    {
        const float step = 0.08f;
        Vector3 n = new Vector3(lc.n.x, lc.n.y, 0f);
        List<Vector2> runs = new List<Vector2>();
        float runStart = float.NaN;

        for (float z = zMin + 0.05f; z <= zMax - 0.05f + 0.001f; z += step)
        {
            Vector3 p = new Vector3(lc.q.x, lc.q.y, z);
            bool ok = OnSurface(p, n, section0) && OnSurface(Mirror(p), MirrorDir(n), section0);
            if (ok && float.IsNaN(runStart)) runStart = z;
            if (!ok && !float.IsNaN(runStart)) { runs.Add(new Vector2(runStart, z - step)); runStart = float.NaN; }
        }
        if (!float.IsNaN(runStart)) runs.Add(new Vector2(runStart, zMax - 0.05f));

        int added = 0;
        foreach (Vector2 run in runs)
        {
            if (run.y - run.x < 0.6f) continue;
            if (lc.dashed)
            {
                for (float s = run.x; s + 0.8f <= run.y; s += 1.3f)
                    added += TryAddPair(rods, Along(lc, s, s + 0.8f, n), section0) ? 1 : 0;
            }
            else
            {
                added += TryAddPair(rods, Along(lc, run.x + 0.04f, run.y - 0.04f, n), section0) ? 1 : 0;
            }
        }
        return added;
    }

    Rod Along(LineCandidate lc, float z0, float z1, Vector3 n)
    {
        return new Rod
        {
            c = new Vector3(lc.q.x, lc.q.y, (z0 + z1) * 0.5f) + n * (D * 0.5f),
            T = Vector3.forward,
            N = n,
            len = z1 - z0,
            mat = lc.mat,
            name = "Rod_" + lc.kind,
        };
    }

    // Short cyan rods lying flat in the middle floor lane.
    int AddMiddleLane(List<Rod> rods, Material cyanMat)
    {
        int added = 0;
        float zc = (zMin + zMax) * 0.5f;
        foreach (Vector3 lane in MiddleLanePattern)
        {
            Vector3 o = new Vector3(axisX + lane.x, floorY + 0.5f, zc + lane.z);
            if (!Cast(o, Vector3.down, 1f, section0, out RaycastHit hit)) continue;
            Rod r = new Rod
            {
                c = hit.point + Vector3.up * (D * 0.5f),
                T = Vector3.forward,
                N = Vector3.up,
                len = lane.y,
                mat = cyanMat,
                name = "Rod_lane",
            };
            if (TryAddPair(rods, r, section0)) added++;
        }
        return added;
    }

    // Cyan rods running up the main wall panel, placed next to panel seams.
    int AddVerticalWallRods(List<Rod> rods, List<Face> faces, Material cyanMat)
    {
        Face wall = null;
        foreach (Face f in faces)
            if (Mathf.Abs(f.n.y) < 0.6f && f.Length >= 0.8f && (wall == null || f.Length > wall.Length))
                wall = f;
        if (wall == null) return 0;

        Vector2 m = (wall.A + wall.B) * 0.5f;
        Vector3 n = new Vector3(wall.n.x, wall.n.y, 0f);

        // Find seams: places along the tunnel where the wall depth changes.
        List<float> candidates = new List<float>();
        float prevDepth = float.NaN;
        for (float z = zMin; z <= zMax; z += 0.04f)
        {
            Vector3 o = new Vector3(m.x, m.y, z) + n * 0.3f;
            float depth = Cast(o, -n, 0.8f, section0, out RaycastHit h) ? h.distance : -1f;
            if (!float.IsNaN(prevDepth) && (Mathf.Abs(depth - prevDepth) > 0.03f))
            {
                candidates.Add(z + 0.12f);
                candidates.Add(z - 0.16f);
            }
            prevDepth = depth;
        }
        float zc = (zMin + zMax) * 0.5f;
        candidates.Add(zc - 1f);
        candidates.Add(zc + 1f);

        int added = 0;
        List<float> used = new List<float>();
        float theta = Mathf.Atan2(m.y - axisY, m.x - axisX) * Mathf.Rad2Deg;

        foreach (float z in candidates)
        {
            if (added >= 2) break;
            if (z < zMin + 0.15f || z > zMax - 0.15f) continue;
            bool tooClose = false;
            foreach (float u in used) if (Mathf.Abs(u - z) < 1.2f) tooClose = true;
            if (tooClose) continue;

            // Measure the panel's exact shape at this depth.
            Face best = null;
            foreach (Face f in Profile(section0, z, theta - 30f, theta + 30f, 0.5f))
            {
                if (Vector2.Dot(f.n, wall.n) < 0.9f || f.Length < 0.7f) continue;
                if (best == null || Vector2.Distance((f.A + f.B) * 0.5f, m) <
                                    Vector2.Distance((best.A + best.B) * 0.5f, m))
                    best = f;
            }
            if (best == null) continue;

            Vector2 dir = (best.B - best.A).normalized;
            float len = Mathf.Min(best.Length - 0.3f, 2.5f);
            if (len < 0.5f) continue;
            Vector2 mid = (best.A + best.B) * 0.5f;
            Vector3 fn = new Vector3(best.n.x, best.n.y, 0f);

            Rod r = new Rod
            {
                c = new Vector3(mid.x, mid.y, z) + fn * (D * 0.5f),
                T = new Vector3(dir.x, dir.y, 0f),
                N = fn,
                len = len,
                mat = cyanMat,
                name = "Rod_vertical",
            };
            if (TryAddPair(rods, r, section0)) { added++; used.Add(z); }
        }
        return added;
    }

    // Purple rods on the front face of the big opening's frame, tracing its shape.
    int BuildOpeningFrame(Material purpleMat)
    {
        Transform end = root.Find("End");
        if (end == null || !TryGetBounds(end, out Bounds b)) return 0;

        Camera cam = Camera.main;
        float camZ = cam != null ? cam.transform.position.z : b.min.z - 10f;
        float near = Mathf.Abs(b.min.z - camZ) < Mathf.Abs(b.max.z - camZ) ? b.min.z : b.max.z;
        float far = near == b.min.z ? b.max.z : b.min.z;
        float s = Mathf.Sign(far - near);
        Vector3 facing = new Vector3(0f, 0f, -s);   // the frame face looking at the camera

        // Find the narrowest ring of the opening (the frame's inner edge).
        float bestZ = float.NaN, bestMean = float.MaxValue;
        for (float z = Mathf.Min(near, far); z <= Mathf.Max(near, far); z += 0.1f)
        {
            int hits = 0; float sum = 0f;
            for (float a = 0f; a < 360f; a += 10f)
            {
                Vector3 dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f);
                if (Cast(new Vector3(axisX, axisY, z), dir, 30f, end, out RaycastHit h)) { hits++; sum += h.distance; }
            }
            if (hits >= 27 && sum / hits < bestMean) { bestMean = sum / hits; bestZ = z; }
        }
        if (float.IsNaN(bestZ)) return 0;

        // Points on the frame's front face, just outside the opening edge (right half).
        List<Vector3> pts = new List<Vector3>();
        for (float a = -90f; a <= 90f + 0.001f; a += 2f)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector3 dirR = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 dirL = new Vector3(-dirR.x, dirR.y, 0f);
            Vector3 o = new Vector3(axisX, axisY, bestZ);
            if (!Cast(o, dirR, 30f, end, out RaycastHit hr) || !Cast(o, dirL, 30f, end, out RaycastHit hl))
            { pts.Add(Vector3.positiveInfinity); continue; }

            float r = Mathf.Min(hr.distance, hl.distance) + 0.14f;
            Vector3 start = new Vector3(axisX + dirR.x * r, axisY + dirR.y * r, near - s * 0.5f);
            if (Cast(start, new Vector3(0f, 0f, s), Mathf.Abs(far - near) + 1f, end, out RaycastHit hf) &&
                Vector3.Dot(hf.normal, facing) > 0.9f)
                pts.Add(hf.point);
            else
                pts.Add(Vector3.positiveInfinity);
        }

        // Group the points into straight runs and place one rod per run.
        List<Rod> frame = new List<Rod>();
        int i = 0;
        while (i < pts.Count - 1)
        {
            if (float.IsInfinity(pts[i].x)) { i++; continue; }
            int j = i + 1;
            while (j < pts.Count && !float.IsInfinity(pts[j].x) && IsStraight(pts, i, j)) j++;
            j--;
            if (j > i)
            {
                Vector3 a = pts[i], c = pts[j];
                float len = Vector3.Distance(a, c) - 0.1f;
                if (len >= 0.4f)
                {
                    Rod r = new Rod
                    {
                        c = (a + c) * 0.5f + facing * (D * 0.5f),
                        T = (c - a).normalized,
                        N = facing,
                        len = len,
                        mat = purpleMat,
                        name = "Rod_frame",
                    };
                    TryAddPair(frame, r, end);
                }
            }
            i = j > i ? j : i + 1;
        }

        Transform container = CreateContainer(end);
        foreach (Rod r in frame)
            CreateRod(container, r.name, r.mat, r.c, Quaternion.LookRotation(r.T, r.N), new Vector3(W, D, r.len));
        return frame.Count / 2;
    }

    static bool IsStraight(List<Vector3> pts, int i, int j)
    {
        Vector3 a = pts[i], b = pts[j];
        if (Mathf.Abs(a.z - b.z) > 0.02f) return false;
        Vector3 ab = b - a;
        float l = ab.magnitude;
        if (l < 0.001f) return true;
        for (int k = i + 1; k < j; k++)
        {
            float dist = Vector3.Cross(ab / l, pts[k] - a).magnitude;
            if (dist > 0.02f || Mathf.Abs(pts[k].z - a.z) > 0.02f) return false;
        }
        return true;
    }

    // ------------------------------------------------------------- checks

    // Adds the rod and its left-side mirror only if both are flush, don't clip,
    // and can be seen from the main camera.
    bool TryAddPair(List<Rod> rods, Rod r, Transform filter)
    {
        Rod m = r;
        m.c = Mirror(r.c);
        m.T = MirrorDir(r.T);
        m.N = MirrorDir(r.N);
        r.name += "_Right";
        m.name += "_Left";

        if (!IsFlush(r, filter) || !IsFlush(m, filter)) return false;
        if (Clips(r) || Clips(m)) return false;
        if (filter == section0 && (!IsVisible(r) || !IsVisible(m))) return false;

        rods.Add(r);
        rods.Add(m);
        return true;
    }

    // Every part of the rod's underside must touch the surface (no floating, no overhang).
    bool IsFlush(Rod r, Transform filter)
    {
        Vector3 side = Vector3.Cross(r.N, r.T).normalized;
        float half = r.len * 0.5f;
        int steps = Mathf.Max(2, Mathf.CeilToInt(r.len / 0.08f));
        for (int i = 0; i <= steps; i++)
        {
            float s = Mathf.Lerp(-half, half, i / (float)steps);
            foreach (float w in new[] { -0.4f, 0f, 0.4f })
            {
                Vector3 contact = r.c + r.T * s + side * (W * w) - r.N * (D * 0.5f);
                if (!OnSurface(contact, r.N, filter)) return false;
            }
        }
        return true;
    }

    static bool OnSurface(Vector3 p, Vector3 n, Transform filter)
    {
        const float back = 0.2f;
        if (!Cast(p + n * back, -n, back * 2f, filter, out RaycastHit h)) return false;
        return Mathf.Abs(h.distance - back) < FlushTolerance && Vector3.Dot(h.normal, n) > 0.93f;
    }

    // The rod's body must not cut into any corridor geometry.
    bool Clips(Rod r)
    {
        Vector3 center = r.c + r.N * 0.006f;
        Vector3 halfSize = new Vector3(W * 0.5f - 0.004f, D * 0.5f - 0.004f, r.len * 0.5f - 0.004f);
        Collider[] hits = Physics.OverlapBox(center, halfSize, Quaternion.LookRotation(r.T, r.N),
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider c in hits)
            if (c.transform.IsChildOf(root)) return true;
        return false;
    }

    // Checks the rod against the camera in every section position.
    bool IsVisible(Rod r)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 camPos = cam.transform.position;
        int total = 0, seen = 0;

        foreach (Transform section in Object.FindFirstObjectByType<TunnelLooper>().sections)
        {
            if (section == null) continue;
            foreach (float s in new[] { -0.4f, 0f, 0.4f })
            {
                Vector3 local = section0.InverseTransformPoint(r.c + r.T * (r.len * s) + r.N * (D * 0.5f + 0.01f));
                Vector3 p = section.TransformPoint(local);
                Vector3 toP = p - camPos;
                if (Vector3.Dot(cam.transform.forward, toP) < 0.5f) continue;
                total++;
                bool blocked = false;
                foreach (RaycastHit h in Physics.RaycastAll(camPos, toP.normalized, toP.magnitude - 0.03f))
                    if (h.collider.transform.IsChildOf(root)) { blocked = true; break; }
                if (!blocked) seen++;
            }
        }
        return total == 0 || seen >= total * 0.35f;
    }

    Vector3 Mirror(Vector3 p) => new Vector3(2f * axisX - p.x, p.y, p.z);
    static Vector3 MirrorDir(Vector3 v) => new Vector3(-v.x, v.y, v.z);

    // Closest hit on the given object (or its children) only.
    static bool Cast(Vector3 origin, Vector3 dir, float maxDist, Transform filter, out RaycastHit hit)
    {
        hit = default;
        float best = float.MaxValue;
        foreach (RaycastHit h in Physics.RaycastAll(origin, dir, maxDist, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.distance < best && h.collider.transform.IsChildOf(filter))
            {
                best = h.distance;
                hit = h;
            }
        }
        return best < float.MaxValue;
    }

    // ------------------------------------------------------------- helpers

    static bool TryGetBounds(Transform t, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
        {
            if (r.GetComponentInParent<Light>() != null) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    static List<MeshCollider> AddTempColliders(Transform t)
    {
        List<MeshCollider> added = new List<MeshCollider>();
        foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.hideFlags = HideFlags.HideAndDontSave;
            mc.sharedMesh = mf.sharedMesh;
            added.Add(mc);
        }
        return added;
    }

    // A child that cancels the corridor's scaling, so rods can be rotated without stretching.
    static Transform CreateContainer(Transform parent)
    {
        GameObject go = new GameObject(ContainerName);
        Undo.RegisterCreatedObjectUndo(go, "Add Neon Lighting");
        go.transform.SetParent(parent, false);
        Vector3 s = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Abs(s.x), 1f / Mathf.Abs(s.y), 1f / Mathf.Abs(s.z));
        return go.transform;
    }

    static void CreateRod(Transform container, string name, Material mat, Vector3 pos, Quaternion rot, Vector3 size)
    {
        GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(rod.GetComponent<Collider>()); // never affects gameplay
        rod.name = name;
        rod.transform.SetParent(container, false);
        rod.transform.position = pos;
        rod.transform.rotation = rot;
        rod.transform.localScale = size;

        MeshRenderer mr = rod.GetComponent<MeshRenderer>();
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

    // Creates (or updates) a glowing URP Unlit material in Assets/NeonLighting.
    // Unlit + HDR color always glows with Bloom, no matter how the scene is lit.
    static Material GetNeonMaterial(string name, Color color, float glow)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "NeonLighting");

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(unlit);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.shader != unlit) mat.shader = unlit;

        Color hdr = color * glow;
        hdr.a = 1f;
        mat.SetColor("_BaseColor", hdr);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
