using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using RoboClash;

public static class XLArenaBuilder
{
    const string OldScene = "Assets/Scenes/RoboClashGameplay.unity";
    const string NewScene = "Assets/Scenes/RoboClashGameplay_XL.unity";
    const string Root = "Assets/RoboClashXL";
    const float Half = 100f;
    const float TexMeters = 8f;

    static Transform W;
    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
    static readonly Vector3[] Spawns =
    {
        new Vector3(0,0,-88), new Vector3(0,0,88), new Vector3(-88,0,0), new Vector3(88,0,0),
        new Vector3(-84,0,-84), new Vector3(-84,0,84), new Vector3(84,0,84), new Vector3(84,0,-84)
    };

    [MenuItem("RoboClash/XL World/Build New XL World")]
    public static void Build()
    {
        if (!File.Exists(OldScene)) { Debug.LogError("[XL] Old scene not found: " + OldScene); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Root + "/Materials");
        XLTextureFactory.GenerateAll();

        if (!File.Exists(NewScene)) AssetDatabase.CopyAsset(OldScene, NewScene);
        EditorSceneManager.OpenScene(NewScene, OpenSceneMode.Single);
        Cache.Clear();

        var old = GameObject.Find("XL World");
        if (old != null) Object.DestroyImmediate(old);
        W = new GameObject("XL World").transform;

        HideOldArena();
        BuildBoundary(); BuildHub(); BuildWorkshop(); BuildHazardChamber();
        BuildCargoYard(); BuildAnnex(); BuildScatter(); BuildSpawns(); BuildSkyline();
        var sun = BuildLighting();
        BuildAtmosphere(sun);
        BuildPost();
        SetupCameras();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[XL] Done. Scene: " + NewScene);
    }

    // ---------- helpers ----------
    static Transform Zone(string n) { var g = new GameObject(n); g.transform.SetParent(W, false); return g.transform; }

    static Material LoadOrCreate(string path, Shader sh)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    static Material Mat(string key, Vector2 tile)
    {
        int ix = Mathf.Max(1, Mathf.RoundToInt(tile.x * 2f)), iy = Mathf.Max(1, Mathf.RoundToInt(tile.y * 2f));
        string id = key + "_" + ix + "_" + iy;
        if (Cache.TryGetValue(id, out var m)) return m;
        m = LoadOrCreate(Root + "/Materials/" + id + ".mat", Shader.Find("Universal Render Pipeline/Lit"));
        m.SetTexture("_BaseMap", XLTextureFactory.Load(key));
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BumpMap", XLTextureFactory.LoadNormal(key));
        m.EnableKeyword("_NORMALMAP");
        m.SetTextureScale("_BaseMap", new Vector2(ix / 2f, iy / 2f));
        float metal = 0.3f, smooth = 0.4f;
        switch (key)
        {
            case "XL_Floor": metal = 0.7f; smooth = 0.55f; break;
            case "XL_Wall": metal = 0.05f; smooth = 0.2f; break;
            case "XL_Rust": metal = 0.6f; smooth = 0.35f; break;
            case "XL_Hazard": metal = 0.3f; smooth = 0.45f; break;
            case "XL_Lava": metal = 0f; smooth = 0.15f; break;
        }
        m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
        if (key == "XL_Lava")
        {
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", XLTextureFactory.Load(key));
            m.SetColor("_EmissionColor", new Color(2.5f, 1.2f, 0.4f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        Cache[id] = m;
        return m;
    }

    static Material GlowMat(Color c, float intensity)
    {
        string id = "XL_Glow_" + ColorUtility.ToHtmlStringRGB(c) + "_" + Mathf.RoundToInt(intensity * 10);
        if (Cache.TryGetValue(id, out var m)) return m;
        m = LoadOrCreate(Root + "/Materials/" + id + ".mat", Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c * 0.15f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        Cache[id] = m;
        return m;
    }

    static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, string mat,
                           Vector3 euler = default(Vector3), bool collider = true, Vector2? tile = null)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.transform.localScale = size;
        Vector2 t = tile ?? (size.y <= 1.2f
            ? new Vector2(size.x / TexMeters, size.z / TexMeters)
            : new Vector2(Mathf.Max(size.x, size.z) / TexMeters, size.y / TexMeters));
        g.GetComponent<Renderer>().sharedMaterial = Mat(mat, t);
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.isStatic = true;
        return g;
    }

    static GameObject Cube(string n, Transform p, Vector3 pos, Vector3 size, string mat, Vector3 euler = default(Vector3), bool collider = true, Vector2? tile = null)
    { return Make(PrimitiveType.Cube, n, p, pos, size, mat, euler, collider, tile); }

    static GameObject Cyl(string n, Transform p, Vector3 pos, float diameter, float height, string mat, bool collider = true)
    { return Make(PrimitiveType.Cylinder, n, p, pos, new Vector3(diameter, height / 2f, diameter), mat, default(Vector3), collider); }

    static GameObject Glow(string n, Transform p, Vector3 pos, Vector3 size, Color c, float intensity = 3f,
                           Vector3 euler = default(Vector3), PrimitiveType type = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(euler); g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = GlowMat(c, intensity);
        g.isStatic = true;
        return g;
    }

    static void PointLight(Transform p, Vector3 pos, Color c, float range, float intensity)
    {
        var g = new GameObject("Light"); g.transform.SetParent(p, false); g.transform.localPosition = pos;
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity;
        l.shadows = LightShadows.None;
    }

    static void SetFloat(Object o, string field, float v)
    {
        var so = new SerializedObject(o); var p = so.FindProperty(field);
        if (p != null && p.propertyType == SerializedPropertyType.Float) { p.floatValue = v; so.ApplyModifiedProperties(); }
        else Debug.LogWarning("[XL] Field '" + field + "' not found on " + o.GetType().Name + " (default value kept).");
    }

    static void EnsureKinematicBody(GameObject g)
    {
        var rb = g.GetComponent<Rigidbody>();
        if (rb == null)
            rb = g.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
    }

    // ---------- old arena (hidden only inside the XL copy) ----------
    static void HideOldArena()
    {
        foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r == W.gameObject) continue;
            bool keep = r.GetComponentInChildren<RoboClashMatchManager>(true) != null
                     || r.GetComponentInChildren<Canvas>(true) != null
                     || r.GetComponentInChildren<Camera>(true) != null
                     || r.GetComponentInChildren<EventSystem>(true) != null
                     || r.GetComponentInChildren<ArenaDamageable>(true) != null;
            if (keep)
            {
                if (r.GetComponentInChildren<RoboClashMatchManager>(true) != null)
                    HideLegacyGeometry(r.transform);
                Debug.Log("[XL] kept gameplay root: " + r.name);
                continue;
            }
            bool geometry = r.GetComponentInChildren<Renderer>(true) != null
                         || r.GetComponentInChildren<Collider>(true) != null
                         || r.GetComponentInChildren<ArenaSpawnZone>(true) != null;
            if (!geometry) continue;
            r.SetActive(false);
            Debug.Log("[XL] hid old arena root (XL copy only): " + r.name);
        }
    }

    static void HideLegacyGeometry(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            bool keep = child.GetComponentInChildren<Canvas>(true) != null
                     || child.GetComponentInChildren<Camera>(true) != null
                     || child.GetComponentInChildren<EventSystem>(true) != null
                     || child.GetComponentInChildren<ArenaDamageable>(true) != null
                     || child.GetComponentInChildren<RoboClashMatchManager>(true) != null;
            if (keep)
            {
                HideLegacyGeometry(child);
                continue;
            }

            bool geometry = child.GetComponentInChildren<Renderer>(true) != null
                         || child.GetComponentInChildren<Collider>(true) != null
                         || child.GetComponentInChildren<ArenaSpawnZone>(true) != null;
            if (geometry)
                child.gameObject.SetActive(false);
        }
    }

    // ---------- boundary ----------
    static void BuildBoundary()
    {
        var t = Zone("Boundary");
        Cube("XL Continuous Arena Floor", t, new Vector3(0, -0.5f, 0), new Vector3(2 * Half, 1, 2 * Half), "XL_Floor",
             tile: new Vector2(2 * Half / TexMeters, 2 * Half / TexMeters)).AddComponent<ArenaFloor>();

        float wh = 14f;
        void Wall(string n, Vector3 p, Vector3 s) { Cube(n, t, p, s, "XL_Wall").AddComponent<ArenaWall>(); }
        Wall("Wall North", new Vector3(0, wh / 2, Half - 0.5f), new Vector3(2 * Half, wh, 1));
        Wall("Wall South", new Vector3(0, wh / 2, -Half + 0.5f), new Vector3(2 * Half, wh, 1));
        Wall("Wall East", new Vector3(Half - 0.5f, wh / 2, 0), new Vector3(1, wh, 2 * Half - 2));
        Wall("Wall West", new Vector3(-Half + 0.5f, wh / 2, 0), new Vector3(1, wh, 2 * Half - 2));

        Color cyan = new Color(0.1f, 0.8f, 1f), amber = new Color(1f, 0.55f, 0.1f);
        for (float a = -87.5f; a <= 87.5f; a += 25f)
        {
            Cube("Buttress N", t, new Vector3(a, wh / 2, Half - 2f), new Vector3(3, wh, 3), "XL_Wall").AddComponent<ArenaWall>();
            Cube("Buttress S", t, new Vector3(a, wh / 2, -Half + 2f), new Vector3(3, wh, 3), "XL_Wall").AddComponent<ArenaWall>();
            Cube("Buttress E", t, new Vector3(Half - 2f, wh / 2, a), new Vector3(3, wh, 3), "XL_Wall").AddComponent<ArenaWall>();
            Cube("Buttress W", t, new Vector3(-Half + 2f, wh / 2, a), new Vector3(3, wh, 3), "XL_Wall").AddComponent<ArenaWall>();
        }
        Glow("Trim N", t, new Vector3(0, wh - 1f, Half - 1.05f), new Vector3(2 * Half - 4, 0.3f, 0.1f), cyan);
        Glow("Trim S", t, new Vector3(0, wh - 1f, -Half + 1.05f), new Vector3(2 * Half - 4, 0.3f, 0.1f), cyan);
        Glow("Trim E", t, new Vector3(Half - 1.05f, wh - 1f, 0), new Vector3(0.1f, 0.3f, 2 * Half - 4), cyan);
        Glow("Trim W", t, new Vector3(-Half + 1.05f, wh - 1f, 0), new Vector3(0.1f, 0.3f, 2 * Half - 4), cyan);
        Glow("Base N", t, new Vector3(0, 0.3f, Half - 1.05f), new Vector3(2 * Half - 4, 0.2f, 0.1f), amber, 2f);
        Glow("Base S", t, new Vector3(0, 0.3f, -Half + 1.05f), new Vector3(2 * Half - 4, 0.2f, 0.1f), amber, 2f);
        Glow("Base E", t, new Vector3(Half - 1.05f, 0.3f, 0), new Vector3(0.1f, 0.2f, 2 * Half - 4), amber, 2f);
        Glow("Base W", t, new Vector3(-Half + 1.05f, 0.3f, 0), new Vector3(0.1f, 0.2f, 2 * Half - 4), amber, 2f);

        Cube("Wasteland", t, new Vector3(0, -1.5f, 0), new Vector3(1200, 1, 1200), "XL_Wall", collider: false,
             tile: new Vector2(1200 / TexMeters, 1200 / TexMeters));
    }

    // ---------- hub (center 10x10 stays clear for King of the Arena) ----------
    static void BuildHub()
    {
        var t = Zone("Central Hub");
        Color cyan = new Color(0.1f, 0.8f, 1f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Glow("Guide Line " + i, t, new Vector3(Mathf.Sin(a) * 40f, 0.03f, Mathf.Cos(a) * 40f),
                 new Vector3(0.5f, 0.02f, 70f), new Color(1f, 0.6f, 0.1f), 2.5f, new Vector3(0, i * 90f, 0));
            Cube("Hub Cover " + i, t, new Vector3(Mathf.Sin(a) * 22f, 1.5f, Mathf.Cos(a) * 22f),
                 new Vector3(8, 3, 1), "XL_Wall", new Vector3(0, i * 90f, 0)).AddComponent<ArenaWall>();
        }
        float[] sx = { -1, 1, -1, 1 }, sz = { -1, -1, 1, 1 };
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(sx[i] * 10f, 0, sz[i] * 10f);
            Cyl("Reactor Tower " + i, t, p + Vector3.up * 7f, 5f, 14f, "XL_Floor");
            for (int k = 1; k <= 3; k++)
                Glow("Ring", t, p + Vector3.up * (k * 3.6f), new Vector3(5.4f, 0.1f, 5.4f), cyan, 4f, default(Vector3), PrimitiveType.Cylinder);
            Glow("Core", t, p + Vector3.up * 15.5f, Vector3.one * 3f, cyan, 6f, default(Vector3), PrimitiveType.Sphere);
            PointLight(t, p + Vector3.up * 15f, cyan, 40f, 8f);
        }
    }

    // ---------- workshop (north-west) ----------
    static void BuildWorkshop()
    {
        var t = Zone("Workshop");
        Cube("Workshop Back", t, new Vector3(-55, 4.5f, 72.5f), new Vector3(44, 9, 1), "XL_Wall").AddComponent<ArenaWall>();
        Cube("Workshop West", t, new Vector3(-76.5f, 4.5f, 55), new Vector3(1, 9, 36), "XL_Wall").AddComponent<ArenaWall>();
        Cube("Workshop East", t, new Vector3(-33.5f, 4.5f, 50), new Vector3(1, 9, 26), "XL_Wall").AddComponent<ArenaWall>();
        Cube("Workshop Front A", t, new Vector3(-66, 1.5f, 37), new Vector3(22, 3, 1), "XL_Wall");
        Cube("Workshop Front B", t, new Vector3(-39, 1.5f, 37), new Vector3(12, 3, 1), "XL_Wall");

        Cube("Catwalk", t, new Vector3(-55, 3.75f, 69.5f), new Vector3(30, 0.5f, 6), "XL_Floor");
        foreach (float x in new[] { -68f, -55f, -42f }) Cube("Support", t, new Vector3(x, 1.75f, 67f), new Vector3(0.8f, 3.5f, 0.8f), "XL_Rust");
        Cube("Rail Front", t, new Vector3(-52.5f, 4.5f, 66.4f), new Vector3(25, 1, 0.25f), "XL_Hazard");
        Cube("Rail End", t, new Vector3(-69.9f, 4.5f, 69.5f), new Vector3(0.25f, 1, 6), "XL_Hazard");
        Cube("Ramp South", t, new Vector3(-67, 2f, 61.1f), new Vector3(4, 0.4f, 10.77f), "XL_Floor", new Vector3(-21.8f, 0, 0));
        Cube("Ramp East", t, new Vector3(-34.6f, 2f, 69.5f), new Vector3(10.77f, 0.4f, 5), "XL_Floor", new Vector3(0, 0, -21.8f));

        Vector3[] press = { new Vector3(-70, 2, 44), new Vector3(-70, 2, 52), new Vector3(-62, 2, 48) };
        foreach (var p in press)
        {
            Cube("Press", t, p, new Vector3(5, 4, 5), "XL_Rust");
            Cyl("Hydraulic Ram", t, p + new Vector3(0, 4.5f, 0), 1f, 5f, "XL_Floor");
            Glow("Press Light", t, p + new Vector3(0, 2.2f, 2.55f), new Vector3(3, 0.2f, 0.05f), new Color(1f, 0.5f, 0.1f), 4f);
        }
        for (int i = 0; i < 6; i++)
            Cube("Tool Crate", t, new Vector3(-40 + (i % 2) * 2.2f, 1, 41 + (i / 2) * 2.2f), new Vector3(2, 2, 2), "XL_Rust", new Vector3(0, i * 17f, 0));

        Glow("Workshop Sign", t, new Vector3(-55, 7, 71.9f), new Vector3(30, 0.5f, 0.1f), new Color(1f, 0.55f, 0.1f), 3f);
        foreach (float x in new[] { -68f, -55f, -42f }) PointLight(t, new Vector3(x, 7.5f, 60f), new Color(1f, 0.7f, 0.4f), 22f, 6f);
    }

    // ---------- hazard chamber (south-east): 4 lava segments + 2 spike traps ----------
    static void BuildHazardChamber()
    {
        var t = Zone("Hazard Chamber");
        Lava(t, "Lava North", new Vector3(47, 0, -35), 22, 5);
        Lava(t, "Lava East", new Vector3(75, 0, -47), 5, 22);
        Lava(t, "Lava South", new Vector3(63, 0, -75), 22, 5);
        Lava(t, "Lava West", new Vector3(35, 0, -63), 5, 22);
        Spikes(t, "Spikes A", new Vector3(55, 0, -50), 16, 3);
        Spikes(t, "Spikes B", new Vector3(55, 0, -60), 16, 3);
        Glow("Risk Route", t, new Vector3(55, 0.03f, -55), new Vector3(1, 0.02f, 26), new Color(1f, 0.15f, 0.05f), 2f);
        Glow("Safe Route", t, new Vector3(45, 0.03f, -55), new Vector3(1, 0.02f, 26), new Color(0.2f, 1f, 0.3f), 2f);
    }

    static void Lava(Transform p, string n, Vector3 pos, float w, float d)
    {
        var g = new GameObject(n); g.transform.SetParent(p, false); g.transform.localPosition = pos;
        var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(w, 2f, d); bc.center = new Vector3(0, 1f, 0); bc.isTrigger = true;
        EnsureKinematicBody(g);
        var lava = g.AddComponent<LavaPit>();
        SetFloat(lava, "damagePerSecond", 18f);
        var v = Cube("Lava Surface", g.transform, new Vector3(0, 0.04f, 0), new Vector3(w, 0.1f, d), "XL_Lava", collider: false);
        v.isStatic = false; v.AddComponent<XLLavaScroll>();
        Cube("Curb A", g.transform, new Vector3(0, 0.2f, d / 2 + 0.3f), new Vector3(w + 1.2f, 0.4f, 0.6f), "XL_Hazard", collider: false);
        Cube("Curb B", g.transform, new Vector3(0, 0.2f, -d / 2 - 0.3f), new Vector3(w + 1.2f, 0.4f, 0.6f), "XL_Hazard", collider: false);
        Cube("Curb C", g.transform, new Vector3(w / 2 + 0.3f, 0.2f, 0), new Vector3(0.6f, 0.4f, d), "XL_Hazard", collider: false);
        Cube("Curb D", g.transform, new Vector3(-w / 2 - 0.3f, 0.2f, 0), new Vector3(0.6f, 0.4f, d), "XL_Hazard", collider: false);
        PointLight(g.transform, new Vector3(0, 2.5f, 0), new Color(1f, 0.4f, 0.1f), 24f, 7f);
    }

    static void Spikes(Transform p, string n, Vector3 pos, float w, float d)
    {
        var g = new GameObject(n); g.transform.SetParent(p, false); g.transform.localPosition = pos;
        var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(w, 1.2f, d); bc.center = new Vector3(0, 0.6f, 0); bc.isTrigger = true;
        EnsureKinematicBody(g);
        g.AddComponent<SpikeTrap>();
        Cube("Plate", g.transform, new Vector3(0, 0.07f, 0), new Vector3(w, 0.15f, d), "XL_Hazard", collider: false);
        for (float x = -w / 2 + 0.8f; x <= w / 2 - 0.7f; x += 1.2f)
            for (float z = -d / 2 + 0.8f; z <= d / 2 - 0.7f; z += 1.2f)
                Cube("Spike", g.transform, new Vector3(x, 0.55f, z), new Vector3(0.35f, 0.9f, 0.35f), "XL_Floor", new Vector3(0, 45, 0), false);
    }

    // ---------- cargo yard (north-east) ----------
    static void BuildCargoYard()
    {
        var t = Zone("Cargo Yard"); var rnd = new System.Random(1337);
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                var p = new Vector3(55 + (c - 1) * 22f, 1.5f, 55 + (r - 1) * 18f);
                float yaw = rnd.NextDouble() < 0.5 ? 0f : 90f;
                Cube("Container", t, p, new Vector3(12, 3, 3), "XL_Rust", new Vector3(0, yaw, 0));
                if (rnd.NextDouble() < 0.4)
                    Cube("Container Top", t, p + Vector3.up * 3f, new Vector3(12, 3, 3), "XL_Rust", new Vector3(0, yaw, 0));
                Glow("Tag", t, p + new Vector3(0, 1.2f, 0), new Vector3(1.5f, 0.2f, 3.1f), new Color(1f, 0.55f, 0.1f), 2f, new Vector3(0, yaw, 0));
            }
        PointLight(t, new Vector3(55, 10, 55), new Color(1f, 0.75f, 0.5f), 45f, 7f);
    }

    // ---------- annex (south-west): pillar grid ----------
    static void BuildAnnex()
    {
        var t = Zone("Reactor Annex"); Color cyan = new Color(0.1f, 0.8f, 1f);
        float[] g = { -18, -6, 6, 18 };
        foreach (float x in g) foreach (float z in g)
        {
            var p = new Vector3(-55 + x, 0, -55 + z);
            Cube("Annex Pillar", t, p + Vector3.up * 5f, new Vector3(2.4f, 10, 2.4f), "XL_Wall");
            Glow("Pillar Ring", t, p + Vector3.up * 0.4f, new Vector3(3f, 0.15f, 3f), cyan, 4f, default(Vector3), PrimitiveType.Cylinder);
            Glow("Pillar Cap", t, p + Vector3.up * 10.1f, new Vector3(2.6f, 0.2f, 2.6f), cyan, 3f);
        }
        PointLight(t, new Vector3(-55, 9, -55), cyan, 45f, 7f);
    }

    // ---------- random cover in the open lanes ----------
    static bool Blocked(Vector3 p)
    {
        if (p.magnitude < 30f) return true;
        if (Mathf.Abs(p.x + 55) < 30 && Mathf.Abs(p.z - 55) < 26) return true;
        if (Mathf.Abs(p.x - 55) < 34 && Mathf.Abs(p.z + 55) < 34) return true;
        if (Mathf.Abs(p.x - 55) < 34 && Mathf.Abs(p.z - 55) < 30) return true;
        if (Mathf.Abs(p.x + 55) < 26 && Mathf.Abs(p.z + 55) < 26) return true;
        foreach (var s in Spawns) if ((p - s).magnitude < 14f) return true;
        return false;
    }

    static void BuildScatter()
    {
        var t = Zone("Cover Scatter"); var rnd = new System.Random(2026); var placed = new List<Vector3>();
        for (int i = 0; i < 600 && placed.Count < 70; i++)
        {
            var p = new Vector3((float)(rnd.NextDouble() * 180 - 90), 0, (float)(rnd.NextDouble() * 180 - 90));
            if (Blocked(p)) continue;
            bool tooClose = false;
            foreach (var q in placed) if ((q - p).magnitude < 9f) { tooClose = true; break; }
            if (tooClose) continue;
            placed.Add(p);
            bool barrier = rnd.NextDouble() < 0.55;
            var size = barrier ? new Vector3(6, 2.5f, 1) : new Vector3(3, 3, 3);
            float yaw = rnd.Next(0, 12) * 15f;
            Cube(barrier ? "Armor Barrier" : "Crate", t, p + Vector3.up * size.y / 2f, size, barrier ? "XL_Wall" : "XL_Rust", new Vector3(0, yaw, 0));
            if (barrier) Glow("Barrier Strip", t, p + Vector3.up * (size.y + 0.03f), new Vector3(size.x, 0.06f, 0.2f), new Color(1f, 0.55f, 0.1f), 3f, new Vector3(0, yaw, 0));
        }
    }

    // ---------- spawns ----------
    static void BuildSpawns()
    {
        var t = Zone("Spawn Zones"); Color green = new Color(0.2f, 1f, 0.4f);
        for (int i = 0; i < Spawns.Length; i++)
        {
            var g = new GameObject("XL Spawn " + i); g.transform.SetParent(t, false);
            g.transform.localPosition = Spawns[i]; g.transform.LookAt(new Vector3(0, 0, 0));
            var z = g.AddComponent<ArenaSpawnZone>();
            var so = new SerializedObject(z); var it = so.GetIterator(); bool set = false;
            while (it.NextVisible(true))
                if (it.propertyType == SerializedPropertyType.Integer && it.name.ToLower().Contains("index")) { it.intValue = i; set = true; break; }
            so.ApplyModifiedProperties();
            if (!set) Debug.LogWarning("[XL] Could not find spawn index field on ArenaSpawnZone.");
            Glow("Pad", g.transform, new Vector3(0, 0.04f, 0), new Vector3(6, 0.04f, 6), green, 2.5f, default(Vector3), PrimitiveType.Cylinder);
            Glow("Beam", g.transform, new Vector3(0, 4f, 0), new Vector3(0.3f, 4f, 0.3f), green, 3f, default(Vector3), PrimitiveType.Cylinder);
            PointLight(g.transform, new Vector3(0, 3f, 0), green, 14f, 4f);
        }
    }

    // ---------- distant skyline (no colliders, fades into fog) ----------
    static void BuildSkyline()
    {
        var t = Zone("Distant Skyline"); var rnd = new System.Random(77);
        for (int i = 0; i < 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2f, r = 150f + (float)rnd.NextDouble() * 120f;
            float h = 18f + (float)rnd.NextDouble() * 60f, w = 10f + (float)rnd.NextDouble() * 22f;
            bool tower = rnd.NextDouble() < 0.4;
            var pos = new Vector3(Mathf.Cos(a) * r, h / 2 - 1f, Mathf.Sin(a) * r);
            var yaw = new Vector3(0, (float)rnd.NextDouble() * 180f, 0);
            if (tower) Make(PrimitiveType.Cylinder, "Skyline", t, pos, new Vector3(w * 0.6f, h / 2, w * 0.6f), "XL_Wall", yaw, false);
            else Make(PrimitiveType.Cube, "Skyline", t, pos, new Vector3(w, h, w * 0.7f), "XL_Wall", yaw, false);
        }
    }

    // ---------- lighting, atmosphere sphere, post ----------
    static Light BuildLighting()
    {
        var sg = new GameObject("XL Sun"); sg.transform.SetParent(W, false);
        sg.transform.rotation = Quaternion.Euler(38, -35, 0);
        var sun = sg.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.82f, 0.62f); sun.intensity = 1.6f; sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;

        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (l.type == LightType.Directional && !l.transform.IsChildOf(W)) l.enabled = false;

        var sky = LoadOrCreate(Root + "/Materials/XL_ReflectionSky.mat", Shader.Find("Skybox/Procedural"));
        RenderSettings.skybox = sky;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.45f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.28f, 0.25f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = new Color(0.78f, 0.52f, 0.40f) * 0.8f;
        return sun;
    }

    static void BuildAtmosphere(Light sun)
    {
        var sh = Shader.Find("RoboClashXL/Atmosphere");
        if (sh == null) { Debug.LogError("[XL] Atmosphere shader not found. Create File 1 first and let Unity compile."); return; }
        var m = LoadOrCreate(Root + "/Materials/XL_Atmosphere.mat", sh);
        m.SetVector("_SunDir", -sun.transform.forward);
        EditorUtility.SetDirty(m);

        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "XL Atmosphere Sphere"; s.transform.SetParent(W, false); s.transform.localScale = Vector3.one * 800f;
        Object.DestroyImmediate(s.GetComponent<Collider>());
        var r = s.GetComponent<MeshRenderer>();
        r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        s.AddComponent<XLAtmosphereFollow>();
    }

    static void BuildPost()
    {
        string path = Root + "/XL_PostProfile.asset";
        AssetDatabase.DeleteAsset(path);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, path);

        var bloom = prof.Add<Bloom>(true);
        bloom.intensity.Override(0.9f); bloom.threshold.Override(1f); bloom.scatter.Override(0.65f);
        var tone = prof.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
        var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.28f); vig.smoothness.Override(0.5f);
        var ca = prof.Add<ColorAdjustments>(true);
        ca.contrast.Override(10f); ca.saturation.Override(10f); ca.postExposure.Override(0.2f);
        foreach (var c in prof.components) AssetDatabase.AddObjectToAsset(c, prof);
        EditorUtility.SetDirty(prof);

        var g = new GameObject("XL Post Volume"); g.transform.SetParent(W, false);
        var vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10; vol.sharedProfile = prof;
    }

    static void SetupCameras()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            c.farClipPlane = Mathf.Max(c.farClipPlane, 1500f);
            var d = c.GetUniversalAdditionalCameraData();
            if (d != null) d.renderPostProcessing = true;
            EditorUtility.SetDirty(c);
        }
    }
}
