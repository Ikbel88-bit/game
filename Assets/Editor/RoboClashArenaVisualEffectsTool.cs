using UnityEditor;
using UnityEditor.SceneManagement;
using RoboClash;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RoboClashArenaVisualEffectsTool
{
    private const string VolumeObjectName = "RoboClash Arena Post Processing";
    private const string ReflectionObjectName = "RoboClash Arena Reflection Probe";
    private const string ProfileFolder = "Assets/Generated/RoboClash/Volumes";
    private const string ProfilePath = ProfileFolder + "/ArenaVolumeProfile.asset";

    [MenuItem("Tools/RoboClash/Configure Arena Visual Effects")]
    public static void Configure()
    {
        EnsureProfileFolder();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "RoboClash Arena Volume Profile";
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        ConfigureProfile(profile);
        ConfigureVolume(profile);
        ConfigureReflectionProbe();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("RoboClash arena post-processing and reflection probe are configured.");
    }

    private static void EnsureProfileFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/RoboClash"))
            AssetDatabase.CreateFolder("Assets/Generated", "RoboClash");
        if (!AssetDatabase.IsValidFolder(ProfileFolder))
            AssetDatabase.CreateFolder("Assets/Generated/RoboClash", "Volumes");
    }

    private static void ConfigureProfile(VolumeProfile profile)
    {
        profile.components.Clear();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.65f);
        bloom.scatter.Override(0.55f);

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(12f);
        color.saturation.Override(6f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.45f);
        EditorUtility.SetDirty(profile);
    }

    private static void ConfigureVolume(VolumeProfile profile)
    {
        var volumeObject = FindSceneObject(VolumeObjectName);
        if (volumeObject == null)
            volumeObject = new GameObject(VolumeObjectName);

        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
            volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volumeObject);
    }

    private static void ConfigureReflectionProbe()
    {
        var probeObject = FindSceneObject(ReflectionObjectName);
        if (probeObject == null)
            probeObject = new GameObject(ReflectionObjectName);

        var probe = probeObject.GetComponent<ReflectionProbe>();
        if (probe == null)
            probe = probeObject.AddComponent<ReflectionProbe>();

        var arena = GameObject.Find("RoboClash Arena");
        var floorCollider = arena != null ? arena.GetComponentInChildren<ArenaFloor>()?.GetComponent<Collider>() : null;
        var bounds = floorCollider != null ? floorCollider.bounds : new Bounds(Vector3.zero, new Vector3(120f, 20f, 120f));
        probeObject.transform.position = bounds.center + Vector3.up * 4f;
        probe.center = Vector3.zero;
        probe.size = new Vector3(bounds.size.x, Mathf.Max(16f, bounds.size.y + 12f), bounds.size.z);
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = 128;
        probe.boxProjection = true;
        probe.intensity = 0.8f;
        EditorUtility.SetDirty(probeObject);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (candidate.scene.IsValid() && candidate.scene.isLoaded && candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
