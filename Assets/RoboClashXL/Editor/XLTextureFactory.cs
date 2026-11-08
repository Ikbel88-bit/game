using System.IO;
using UnityEditor;
using UnityEngine;

public static class XLTextureFactory
{
    public const string Dir = "Assets/RoboClashXL/Textures";
    const int N = 512;
    public static readonly string[] Keys = { "XL_Floor", "XL_Wall", "XL_Rust", "XL_Hazard", "XL_Lava" };

    delegate (Color c, float h) Recipe(float u, float v);

    static float Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    static float Noise(float u, float v, int f, int s)
    {
        float x = u * f, y = v * f;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        int a = ((x0 % f) + f) % f, b = (a + 1) % f, c = ((y0 % f) + f) % f, d = (c + 1) % f;
        return Mathf.Lerp(Mathf.Lerp(Hash(a, c, s), Hash(b, c, s), fx), Mathf.Lerp(Hash(a, d, s), Hash(b, d, s), fx), fy);
    }

    static float Fbm(float u, float v, int f, int oct, int s)
    {
        float sum = 0, amp = 0.5f, norm = 0;
        for (int i = 0; i < oct; i++) { sum += amp * Noise(u, v, f, s + i); norm += amp; amp *= 0.5f; f *= 2; }
        return sum / norm;
    }

    static (Color, float) Floor(float u, float v)
    {
        float g = 2f, fu = Mathf.Repeat(u * g, 1f), fv = Mathf.Repeat(v * g, 1f);
        float eu = Mathf.Min(fu, 1f - fu), ev = Mathf.Min(fv, 1f - fv);
        float groove = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(eu, ev) / 0.02f));
        float n = Fbm(u, v, 8, 5, 11), grain = Noise(u, v, 128, 3);
        float rivet = 1f - Mathf.SmoothStep(0.012f, 0.022f, Vector2.Distance(new Vector2(eu, ev), new Vector2(0.05f, 0.05f)));
        float wear = Mathf.Clamp01((Fbm(u, v, 4, 4, 21) - 0.5f) * 3f);
        float shade = (0.22f + 0.18f * n + 0.05f * grain) * (0.55f + 0.45f * groove);
        Color c = new Color(shade, shade * 1.02f, shade * 1.08f, 1f);
        c = Color.Lerp(c, new Color(0.30f, 0.17f, 0.09f), wear * 0.35f);
        c = Color.Lerp(c, c * 1.8f, rivet);
        return (c, groove * (0.7f + 0.2f * n) + rivet * 0.3f + grain * 0.05f);
    }

    static (Color, float) Wall(float u, float v)
    {
        float g = 2f, fu = Mathf.Repeat(u * g, 1f), fv = Mathf.Repeat(v * g, 1f);
        float seam = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
        float groove = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(seam / 0.015f));
        float n = Fbm(u, v, 6, 6, 5), pores = Noise(u, v, 96, 9);
        float stain = Mathf.Clamp01((Fbm(u, v, 3, 4, 31) - 0.45f) * 2.5f);
        float shade = (0.36f + 0.2f * n - 0.15f * stain - 0.08f * pores) * (0.6f + 0.4f * groove);
        return (new Color(shade * 1.05f, shade, shade * 0.95f, 1f), groove * 0.6f + n * 0.3f - pores * 0.1f);
    }

    static (Color, float) Rust(float u, float v)
    {
        float ribs = 0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 2f * 16f);
        float patches = Fbm(u, v, 5, 5, 41);
        float rustMask = Mathf.SmoothStep(0.45f, 0.65f, patches);
        Color paint = new Color(0.12f, 0.28f, 0.32f) * (0.7f + 0.3f * ribs);
        Color rust = new Color(0.45f, 0.20f, 0.08f) * (0.6f + 0.8f * Fbm(u, v, 48, 3, 7));
        Color c = Color.Lerp(paint, rust, rustMask); c.a = 1f;
        return (c, ribs * 0.6f + rustMask * 0.2f * Noise(u, v, 64, 2));
    }

    static (Color, float) Hazard(float u, float v)
    {
        bool stripe = Mathf.Repeat((u + v) * 8f, 1f) < 0.5f;
        float wear = Fbm(u, v, 10, 5, 51);
        Color c = stripe ? new Color(0.9f, 0.7f, 0.05f) : new Color(0.06f, 0.06f, 0.06f);
        if (wear > 0.62f) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.37f), Mathf.Clamp01((wear - 0.62f) * 6f));
        c *= 0.85f + 0.15f * Noise(u, v, 128, 4); c.a = 1f;
        return (c, (stripe ? 0.5f : 0.4f) + wear * 0.2f);
    }

    static (Color, float) Lava(float u, float v)
    {
        float r = 1f - Mathf.Abs(2f * Fbm(u, v, 6, 5, 61) - 1f);
        float crack = Mathf.SmoothStep(0.72f, 0.95f, r), heat = Fbm(u, v, 12, 3, 71);
        Color hot = Color.Lerp(new Color(0.9f, 0.15f, 0.02f), new Color(1f, 0.85f, 0.2f), Mathf.Clamp01(crack * heat * 1.5f));
        Color crust = Color.Lerp(new Color(0.04f, 0.03f, 0.03f), new Color(0.12f, 0.08f, 0.07f), heat);
        Color c = Color.Lerp(crust, hot, crack); c.a = 1f;
        return (c, (1f - crack) * 0.8f + 0.1f * heat);
    }

    public static void GenerateAll()
    {
        Directory.CreateDirectory(Dir);
        Bake("XL_Floor", Floor, 5f);
        Bake("XL_Wall", Wall, 4f);
        Bake("XL_Rust", Rust, 6f);
        Bake("XL_Hazard", Hazard, 4f);
        Bake("XL_Lava", Lava, 5f);
        AssetDatabase.Refresh();
        foreach (var k in Keys) { Setup(k, false); Setup(k + "_N", true); }
    }

    static void Bake(string key, Recipe r, float strength)
    {
        var col = new Color[N * N]; var h = new float[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var (c, hh) = r((x + 0.5f) / N, (y + 0.5f) / N);
                col[y * N + x] = c; h[y * N + x] = hh;
            }
        var nc = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float L = h[y * N + (x + N - 1) % N], R = h[y * N + (x + 1) % N];
                float D = h[((y + N - 1) % N) * N + x], U = h[((y + 1) % N) * N + x];
                var n = new Vector3((L - R) * strength, (D - U) * strength, 1f).normalized;
                nc[y * N + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        Save(key, col); Save(key + "_N", nc);
    }

    static void Save(string name, Color[] px)
    {
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        t.SetPixels(px); t.Apply();
        File.WriteAllBytes(Dir + "/" + name + ".png", t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static void Setup(string name, bool normal)
    {
        var ti = AssetImporter.GetAtPath(Dir + "/" + name + ".png") as TextureImporter;
        if (ti == null) return;
        ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.filterMode = FilterMode.Trilinear;
        ti.anisoLevel = 8;
        ti.mipmapEnabled = true;
        ti.SaveAndReimport();
    }

    public static Texture2D Load(string key) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/" + key + ".png"); }
    public static Texture2D LoadNormal(string key) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/" + key + "_N.png"); }
}
