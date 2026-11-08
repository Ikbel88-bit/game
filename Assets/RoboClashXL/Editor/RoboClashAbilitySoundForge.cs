using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoboClashAbilitySoundForge
{
    const int SR = 44100;
    const string AudioRoot = "Assets/Resources/RoboClash/Audio";
    const string BackupRoot = "Assets/RoboClashXL/AudioBackup";
    const string PreviewRoot = "Assets/RoboClashXL/AudioPreview";
    static System.Random rng = new System.Random(1234);

    // ---------- DSP building blocks ----------
    sealed class Osc
    {
        double ph;
        public float Sin(float hz) { ph += hz / SR; ph -= Math.Floor(ph); return (float)Math.Sin(ph * 6.283185307179586); }
        public float Saw(float hz) { ph += hz / SR; ph -= Math.Floor(ph); return (float)(ph * 2.0 - 1.0); }
    }

    sealed class Svf // state-variable filter: Low / Band / High outputs
    {
        float low, band;
        public float Low, Band, High;
        public void Run(float x, float hz, float damp)
        {
            float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(hz, 20f, 8000f) / SR);
            low += f * band;
            High = x - low - damp * band;
            band += f * High;
            Low = low; Band = band;
        }
    }

    static float N() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
    static float Chance(float p) { return rng.NextDouble() < p ? 1f : 0f; }
    static float Ad(float t, float a, float d) { if (t < 0f) return 0f; return t < a ? t / a : Mathf.Exp(-(t - a) / d); }
    static float Ramp(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }
    static float Lerp(float a, float b, float x) { return a + (b - a) * Mathf.Clamp01(x); }
    static float[] Buf(float seconds) { return new float[(int)(seconds * SR)]; }
    const float TAU = 6.2831853f;

    static float[] Finish(float[] b, float fadeOut = 0.04f, float peak = 0.9f)
    {
        int fi = (int)(0.004f * SR), fo = (int)(fadeOut * SR);
        float max = 1e-6f;
        for (int i = 0; i < b.Length; i++)
        {
            float v = (float)Math.Tanh(b[i] * 1.2f); // soft clip
            if (i < fi) v *= i / (float)fi;
            int r = b.Length - 1 - i;
            if (r < fo) v *= r / (float)fo;
            b[i] = v;
            max = Math.Max(max, Math.Abs(v));
        }
        float g = peak / max;
        for (int i = 0; i < b.Length; i++) b[i] *= g;
        return b;
    }

    // ---------- VOLT ----------
    static float[] VoltDash()
    {
        var b = Buf(0.6f); var th = new Osc(); var zp = new Osc(); var bp = new Svf(); var hp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            bp.Run(N(), Lerp(350f, 5500f, Mathf.Pow(t / 0.6f, 0.7f)), 0.4f);
            float whoosh = bp.Band * 0.45f * Ad(t, 0.05f, 0.2f);
            float thump = th.Sin(Lerp(150f, 45f, t / 0.12f)) * Ad(t, 0.003f, 0.07f) * 1.1f;
            float zap = zp.Saw(Lerp(2600f, 500f, t / 0.3f)) * Ad(t, 0.002f, 0.07f) * 0.35f;
            hp.Run(N(), 5000f, 0.6f);
            float crackle = hp.High * Chance(0.03f) * Ad(t, 0.002f, 0.25f) * 1.6f;
            b[i] = whoosh + thump + zap + crackle;
        }
        return Finish(b, 0.08f);
    }

    static float[] VoltUltimate() // Overdrive: charge up, burst, humming afterglow
    {
        var b = Buf(2.0f); var a = new Osc(); var c = new Osc(); var bo = new Osc(); var hu = new Osc();
        var lp = new Svf(); var hp = new Svf();
        float tp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float r = Ramp(t, 0f, 1.0f);
            float f = Lerp(110f, 1200f, r * r);
            tp += Lerp(8f, 60f, r) / SR;
            float trem = 0.65f + 0.35f * Mathf.Sin(tp * TAU);
            float chargeEnv = t < 1.0f ? (0.15f + 0.6f * r * r) : 0.75f * Mathf.Exp(-(t - 1f) / 0.03f);
            float charge = (a.Saw(f) * 0.3f + c.Sin(f * 2f) * 0.3f) * chargeEnv * trem;

            float tt = t - 1.0f;
            float burst = N() * Ad(tt, 0.002f, 0.12f) * 1.2f;
            float boom = bo.Sin(Lerp(120f, 40f, tt / 0.3f)) * Ad(tt, 0.003f, 0.45f) * 1.3f;
            lp.Run(hu.Saw(95f), 600f, 0.8f);
            float hum = lp.Low * (0.6f + 0.4f * Mathf.Sin(t * TAU * 50f)) * Ad(tt, 0.02f, 0.5f) * 0.7f;
            hp.Run(N(), 4500f, 0.6f);
            float crackle = hp.High * Chance(0.04f) * Ad(tt, 0.01f, 0.7f) * 1.4f;
            b[i] = charge + burst + boom + hum + crackle;
        }
        return Finish(b, 0.1f);
    }

    // ---------- TITAN ----------
    static float[] TitanShield()
    {
        var b = Buf(1.2f);
        var th = new Osc(); var h1 = new Osc(); var h2 = new Osc(); var h3 = new Osc(); var r1 = new Osc(); var r2 = new Osc();
        var bp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float thump = th.Sin(Lerp(220f, 65f, t / 0.15f)) * Ad(t, 0.003f, 0.12f) * 1.0f;
            float trem = 1f + 0.25f * Mathf.Sin(t * TAU * 6f);
            float env = Ramp(t, 0f, 0.12f) * Mathf.Exp(-Mathf.Max(0f, t - 0.2f) / 0.7f);
            float hum = (h1.Sin(110f) + 0.6f * h2.Sin(165f) + 0.4f * h3.Sin(220f * (1f + 0.004f * Mathf.Sin(t * 31f)))) * 0.35f * env * trem;
            bp.Run(N(), 2200f, 0.25f);
            float shimmer = bp.Band * 0.12f * Ad(t, 0.3f, 0.35f);
            float ring = (r1.Sin(880f) * r2.Sin(1320f)) * Ad(t, 0.002f, 0.35f) * 0.35f;
            b[i] = thump + hum + shimmer + ring;
        }
        return Finish(b, 0.12f);
    }

    static float[] TitanUltimate() // Rail Cannon: ~1.75 s charge, then the shot
    {
        var b = Buf(2.6f); const float fire = 1.75f;
        var w1 = new Osc(); var w2 = new Osc(); var bm = new Osc(); var r1 = new Osc(); var r2 = new Osc();
        var hp = new Svf(); var lp = new Svf();
        float tickPh = 0f, tickEnv = 0f, tickDecay = (float)Math.Exp(-1.0 / (0.004 * SR));
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float r = Ramp(t, 0f, fire);
            float f = Lerp(70f, 2200f, Mathf.Pow(r, 2.2f));
            float whineAmp = (0.12f + 0.55f * r * r) * (t < fire ? 1f : Mathf.Exp(-(t - fire) / 0.02f));
            float whine = (w1.Saw(f) * 0.25f + w2.Sin(f * 1.5f) * 0.2f) * whineAmp;
            tickPh += (6f + 34f * r) / SR;
            if (tickPh >= 1f) { tickPh -= 1f; tickEnv = 1f; }
            tickEnv *= tickDecay;
            float tick = N() * tickEnv * 0.35f * (t < fire ? 1f : 0f);

            float tt = t - fire;
            hp.Run(N(), 2500f, 0.5f);
            float crack = hp.High * Ad(tt, 0.001f, 0.06f) * 1.8f;
            float boom = bm.Sin(Lerp(95f, 26f, tt / 0.5f)) * Ad(tt, 0.002f, 0.7f) * 1.6f;
            float ring = (r1.Sin(520f) + 0.7f * r2.Sin(781f)) * Ad(tt, 0.002f, 0.9f) * 0.18f;
            lp.Run(N(), 700f, 0.7f);
            float tail = lp.Low * Ad(tt, 0.01f, 0.5f) * 1.2f;
            b[i] = whine + tick + crack + boom + ring + tail;
        }
        return Finish(b, 0.2f);
    }

    // ---------- SHADOW ----------
    static float[] ShadowCloak()
    {
        var b = Buf(1.3f);
        var c1 = new Osc(); var c2 = new Osc(); var c3 = new Osc(); var sub = new Osc(); var ping = new Osc();
        var bp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            bp.Run(N(), Lerp(6500f, 500f, Mathf.Pow(t / 1.3f, 0.6f)), 0.3f);
            float air = bp.Band * 0.35f * Ad(t, 0.12f, 0.5f);
            float baseF = Lerp(1400f, 260f, t / 1.2f) * (1f + 0.01f * Mathf.Sin(t * TAU * 5.5f));
            float chorus = (c1.Sin(baseF) + c2.Sin(baseF * 1.007f) + 0.8f * c3.Sin(baseF * 0.5f)) * 0.12f * Ad(t, 0.06f, 0.5f);
            float subv = sub.Sin(55f) * Ad(t, 0.08f, 0.25f) * 0.5f;
            float pg = ping.Sin(3300f) * Ad(t, 0.002f, 0.1f) * 0.2f;
            b[i] = air + chorus + subv + pg;
        }
        return Finish(b, 0.2f);
    }

    static float[] ShadowUltimate() // Shadow Strike: swell, blink-snap, slash
    {
        var b = Buf(1.1f); const float snap = 0.38f;
        var ro = new Osc(); var zo = new Osc(); var th = new Osc(); var sh = new Osc();
        var bp = new Svf(); var hp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float r = Ramp(t, 0f, snap);
            bp.Run(N(), Lerp(300f, 7000f, r * r), 0.35f);
            float pre = (t < snap ? 1f : 0f) * (bp.Band * 0.5f * r * r + ro.Sin(Lerp(200f, 1800f, r * r)) * 0.25f * r * r);

            float tt = t - snap;
            float click = N() * Ad(tt, 0.0005f, 0.01f) * 1.4f;
            float zip = zo.Sin(Lerp(3200f, 150f, tt / 0.14f)) * Ad(tt, 0.001f, 0.12f) * 0.7f;
            hp.Run(N(), 3500f, 0.5f);
            float slash = hp.High * Ad(tt - 0.02f, 0.003f, 0.1f) * 0.9f;
            float thud = th.Sin(Lerp(110f, 45f, tt / 0.15f)) * Ad(tt, 0.003f, 0.22f) * 1.0f;
            float shim = sh.Sin(1250f) * (0.6f + 0.4f * Mathf.Sin(t * TAU * 9f)) * Ad(tt, 0.01f, 0.3f) * 0.1f;
            b[i] = pre + click + zip + slash + thud + shim;
        }
        return Finish(b, 0.12f);
    }

    // ---------- SPARK ----------
    static float[] SparkEMP()
    {
        var b = Buf(1.6f);
        var th = new Osc(); var ex = new Osc(); var wh = new Osc();
        var lp = new Svf(); var hp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float thump = th.Sin(Lerp(120f, 35f, t / 0.2f)) * Ad(t, 0.002f, 0.3f) * 1.3f;
            lp.Run(N(), 2200f, 0.6f);
            float burst = lp.Low * Ad(t, 0.003f, 0.15f) * 1.0f;
            float wob = 0.7f + 0.3f * Mathf.Sin(t * TAU * 38f);
            float expand = ex.Sin(Lerp(1700f, 70f, Mathf.Pow(t / 1.3f, 0.5f))) * Ad(t, 0.01f, 0.55f) * 0.5f * wob;
            hp.Run(N(), 4200f, 0.6f);
            float sparks = hp.High * Chance(0.025f) * Ad(t, 0.01f, 0.6f) * 1.5f;
            float whine = wh.Sin(3200f + 400f * Mathf.Sin(t * TAU * 13f)) * Ad(t, 0.005f, 0.6f) * 0.12f;
            b[i] = thump + burst + expand + sparks + whine;
        }
        return Finish(b, 0.15f);
    }

    static float[] SparkRepair() // generic "Ability" clip: ascending chime arpeggio
    {
        var b = Buf(1.1f);
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        var o1 = new Osc[4]; var o2 = new Osc[4];
        for (int k = 0; k < 4; k++) { o1[k] = new Osc(); o2[k] = new Osc(); }
        var hm = new Osc(); var hp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR, sum = 0f;
            for (int k = 0; k < 4; k++)
                sum += (o1[k].Sin(notes[k]) + 0.3f * o2[k].Sin(notes[k] * 2f)) * Ad(t - k * 0.12f, 0.005f, 0.35f) * 0.3f;
            float hum = hm.Sin(220f) * Ramp(t, 0f, 0.2f) * Mathf.Exp(-t / 0.9f) * 0.15f;
            hp.Run(N(), 7000f, 0.5f);
            float sparkle = hp.High * Chance(0.02f) * Ad(t, 0.05f, 0.5f) * 0.5f;
            b[i] = sum + hum + sparkle;
        }
        return Finish(b, 0.15f);
    }

    static float[] RepairPulse() // Spark ability activation + respawn
    {
        var b = Buf(0.95f);
        var o1 = new Osc(); var o2 = new Osc(); var b1 = new Osc(); var b2 = new Osc(); var bp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float p1 = o1.Sin(Lerp(300f, 600f, t / 0.25f)) * Ad(t, 0.02f, 0.18f) * 0.5f;
            float p2 = o2.Sin(Lerp(450f, 900f, (t - 0.22f) / 0.25f)) * Ad(t - 0.22f, 0.02f, 0.2f) * 0.5f;
            float bell = (b1.Sin(1318.5f) + 0.6f * b2.Sin(1976f)) * Ad(t - 0.45f, 0.003f, 0.35f) * 0.25f;
            bp.Run(N(), 3000f, 0.4f);
            float air = bp.Band * 0.1f * Ramp(t, 0f, 0.1f) * Mathf.Exp(-t / 0.5f);
            b[i] = p1 + p2 + bell + air;
        }
        return Finish(b, 0.12f);
    }

    static float[] DroneRepair() // repair beam hum
    {
        var b = Buf(1.5f);
        var c1 = new Osc(); var c2 = new Osc(); var c3 = new Osc(); var hp = new Svf();
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float f = Lerp(180f, 260f, t / 0.2f) + Mathf.Sin(t * TAU * 5.5f) * 6f;
            float beam = c1.Sin(f) * 0.5f + c2.Sin(f * 2f) * 0.25f
                       + c3.Sin(f * 4f) * 0.1f * (0.5f + 0.5f * Mathf.Sin(t * TAU * 3f));
            hp.Run(N(), 3000f, 0.6f);
            float crack = hp.High * Chance(0.006f) * 0.8f;
            float env = Ramp(t, 0f, 0.15f) * (1f - Ramp(t, 1.2f, 1.5f));
            b[i] = (beam + crack) * env;
        }
        return Finish(b, 0.05f);
    }

    // ---------- clip table (paths match RoboClashRobotAudio's Resources mapping) ----------
    static readonly Tuple<string, Func<float[]>>[] Clips =
    {
        Tuple.Create<string, Func<float[]>>("Abilities/VoltDash", VoltDash),
        Tuple.Create<string, Func<float[]>>("Abilities/VoltUltimate", VoltUltimate),
        Tuple.Create<string, Func<float[]>>("Abilities/TitanShield", TitanShield),
        Tuple.Create<string, Func<float[]>>("Abilities/TitanUltimate", TitanUltimate),
        Tuple.Create<string, Func<float[]>>("Abilities/ShadowCloak", ShadowCloak),
        Tuple.Create<string, Func<float[]>>("Abilities/ShadowUltimate", ShadowUltimate),
        Tuple.Create<string, Func<float[]>>("Abilities/SparkEMP", SparkEMP),
        Tuple.Create<string, Func<float[]>>("Abilities/SparkRepair", SparkRepair),
        Tuple.Create<string, Func<float[]>>("Robots/RepairPulse", RepairPulse),
        Tuple.Create<string, Func<float[]>>("Robots/DroneRepair", DroneRepair),
    };

    // ---------- menu commands ----------
    [MenuItem("RoboClash/Ability Sounds/Preview Only (does not touch game)")]
    public static void Preview()
    {
        EnsureFolder(PreviewRoot);
        foreach (var c in Clips)
        {
            rng = new System.Random(1234);
            WriteWav(PreviewRoot + "/" + Path.GetFileName(c.Item1) + ".wav", c.Item2());
        }
        AssetDatabase.Refresh();
        Debug.Log("[Sounds] Previews written to " + PreviewRoot + " (not used by the game).");
    }

    [MenuItem("RoboClash/Ability Sounds/Generate and Install")]
    public static void Install()
    {
        foreach (var c in Clips)
        {
            string ogg = AudioRoot + "/" + c.Item1 + ".ogg";
            string wav = AudioRoot + "/" + c.Item1 + ".wav";
            EnsureFolder(Path.GetDirectoryName(wav).Replace('\\', '/'));
            if (File.Exists(ogg))
            {
                string bak = BackupRoot + "/" + c.Item1 + ".ogg";
                EnsureFolder(Path.GetDirectoryName(bak).Replace('\\', '/'));
                if (File.Exists(bak)) AssetDatabase.DeleteAsset(ogg);
                else
                {
                    string err = AssetDatabase.MoveAsset(ogg, bak);
                    if (!string.IsNullOrEmpty(err)) { Debug.LogError("[Sounds] Backup failed for " + ogg + ": " + err); continue; }
                }
            }
            rng = new System.Random(1234);
            WriteWav(wav, c.Item2());
            AssetDatabase.ImportAsset(wav, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(wav) as AudioImporter;
            if (imp != null)
            {
                imp.forceToMono = true; // mono so 3D spatialisation works properly
                imp.loadInBackground = false;
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.preloadAudioData = true;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.85f;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
            Debug.Log("[Sounds] Installed " + wav);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Sounds] Done. Old .ogg files are in " + BackupRoot);
    }

    [MenuItem("RoboClash/Ability Sounds/Restore Original Sounds")]
    public static void Restore()
    {
        foreach (var c in Clips)
        {
            string ogg = AudioRoot + "/" + c.Item1 + ".ogg";
            string wav = AudioRoot + "/" + c.Item1 + ".wav";
            string bak = BackupRoot + "/" + c.Item1 + ".ogg";
            if (!File.Exists(bak)) continue;
            AssetDatabase.DeleteAsset(wav);
            string err = AssetDatabase.MoveAsset(bak, ogg);
            if (!string.IsNullOrEmpty(err)) Debug.LogError("[Sounds] Restore failed: " + err);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Sounds] Original sounds restored.");
    }

    // ---------- helpers ----------
    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    static void WriteWav(string path, float[] s)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int dataLen = s.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
            foreach (var f in s) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }
}
