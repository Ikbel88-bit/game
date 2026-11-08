using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RoboClashProjectileMaterialCreator
{
    private const string Folder = "Assets/Resources/RoboClash/VFX";

    [MenuItem("Tools/RoboClash/Create Projectile Materials")]
    public static void Create()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "RoboClash");
        EnsureFolder("Assets/Resources/RoboClash", "VFX");

        var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (particleShader == null || litShader == null)
        {
            Debug.LogError("RoboClash projectile materials require URP Particles/Unlit and URP/Lit shaders.");
            return;
        }

        CreateParticleMaterial("GlowAdditive", particleShader, true, true);
        CreateParticleMaterial("SmokeAlpha", particleShader, true, false);
        CreateParticleMaterial("ParticleSolid", particleShader, false, false);
        CreateLitMaterial("ProjectileSolid", litShader);

        AddAlwaysIncludedShader(particleShader);
        AddAlwaysIncludedShader(litShader);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("RoboClash projectile materials created in " + Folder);
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void CreateParticleMaterial(string name, Shader shader, bool transparent, bool additive)
    {
        var path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetFloat("_Surface", transparent ? 1f : 0f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
        material.SetFloat("_DstBlend", transparent
            ? (additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha)
            : (float)BlendMode.Zero);
        material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        material.SetFloat("_Cull", 0f);
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
        if (transparent)
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = transparent
            ? (int)RenderQueue.Transparent
            : (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
    }

    private static void CreateLitMaterial(string name, Shader shader)
    {
        var path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetFloat("_Metallic", 0.6f);
        material.SetFloat("_Smoothness", 0.65f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(material);
    }

    private static void AddAlwaysIncludedShader(Shader shader)
    {
        var settings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (settings == null)
            return;

        var serialized = new SerializedObject(settings);
        var list = serialized.FindProperty("m_AlwaysIncludedShaders");
        if (list == null)
            return;

        for (var i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                return;
        }

        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        serialized.ApplyModifiedProperties();
    }
}
