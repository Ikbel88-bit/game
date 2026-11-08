using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RoboClashVFXMaterialCreator
{
    private const string Folder = "Assets/Resources/RoboClash/VFX";
    private const string MaterialPath = Folder + "/ParticleUnlit.mat";

    [MenuItem("Tools/RoboClash/Create VFX Materials")]
    public static void Create()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogError("URP Particles/Unlit shader not found. Is the URP package installed?");
            return;
        }

        Directory.CreateDirectory(Folder);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "ParticleUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);

        AddAlwaysIncludedShader("Universal Render Pipeline/Particles/Unlit");
        AddAlwaysIncludedShader("Universal Render Pipeline/Lit");
        AssetDatabase.SaveAssets();
        Debug.Log("RoboClash VFX material created at " + MaterialPath);
    }

    private static void AddAlwaysIncludedShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null)
            return;

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
