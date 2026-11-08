using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashArenaVisuals
    {
        private static readonly Dictionary<string, Material> SharedMaterials =
            new Dictionary<string, Material>();

        public static Material CreateMaterial(
            string materialName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emissionColor = default,
            Texture2D baseTexture = null,
            Texture2D normalMap = null,
            Texture2D metallicSmoothnessMap = null,
            Texture2D emissionMap = null,
            Vector2 textureScale = default)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                Debug.LogError("RoboClash: Arena material needs a name.");
                return null;
            }

            if (SharedMaterials.TryGetValue(materialName, out var sharedMaterial) &&
                sharedMaterial != null)
            {
                ConfigureMaterial(sharedMaterial, baseColor, metallic, smoothness,
                    emissionColor, baseTexture, normalMap, metallicSmoothnessMap,
                    emissionMap, textureScale);
                return sharedMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("RoboClash: No compatible material shader found.");
                return null;
            }

            sharedMaterial = new Material(shader) { name = materialName };
            ConfigureMaterial(sharedMaterial, baseColor, metallic, smoothness,
                emissionColor, baseTexture, normalMap, metallicSmoothnessMap,
                emissionMap, textureScale);
            SharedMaterials.Add(materialName, sharedMaterial);
            return sharedMaterial;
        }

        public static void ConfigureMaterial(
            Material material,
            Color baseColor,
            float metallic,
            float smoothness,
            Color emissionColor = default,
            Texture2D baseTexture = null,
            Texture2D normalMap = null,
            Texture2D metallicSmoothnessMap = null,
            Texture2D emissionMap = null,
            Vector2 textureScale = default)
        {
            if (material == null)
            {
                Debug.LogError("RoboClash: Cannot configure a missing arena material.");
                return;
            }

            var scale = textureScale == Vector2.zero ? Vector2.one : textureScale;
            SetColor(material, "_BaseColor", "_Color", baseColor);
            SetFloat(material, "_Metallic", Mathf.Clamp01(metallic));
            SetFloat(material, "_Smoothness", Mathf.Clamp01(smoothness));
            SetTexture(material, "_BaseMap", "_MainTex", baseTexture, scale);
            SetTexture(material, "_BumpMap", null, normalMap, scale);
            SetTexture(material, "_MetallicGlossMap", null, metallicSmoothnessMap, scale);
            SetTexture(material, "_EmissionMap", null, emissionMap, scale);

            SetKeyword(material, "_NORMALMAP", normalMap != null);
            SetKeyword(material, "_METALLICSPECGLOSSMAP",
                metallicSmoothnessMap != null);
            SetKeyword(material, "_METALLICGLOSSMAP",
                metallicSmoothnessMap != null);
            SetKeyword(material, "_EMISSION",
                emissionColor.maxColorComponent > 0f || emissionMap != null);

            if (material.HasProperty("_BumpScale"))
                material.SetFloat("_BumpScale", normalMap != null ? 0.45f : 1f);
            if (material.HasProperty("_SmoothnessTextureChannel"))
                material.SetFloat("_SmoothnessTextureChannel", 0f);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", emissionColor);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
        }

        private static void SetColor(Material material, string primaryProperty,
            string fallbackProperty, Color color)
        {
            if (material.HasProperty(primaryProperty))
                material.SetColor(primaryProperty, color);
            if (material.HasProperty(fallbackProperty))
                material.SetColor(fallbackProperty, color);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetTexture(Material material, string primaryProperty,
            string fallbackProperty, Texture texture, Vector2 scale)
        {
            if (material.HasProperty(primaryProperty))
            {
                material.SetTexture(primaryProperty, texture);
                material.SetTextureScale(primaryProperty, scale);
            }
            if (!string.IsNullOrEmpty(fallbackProperty) &&
                material.HasProperty(fallbackProperty))
            {
                material.SetTexture(fallbackProperty, texture);
                material.SetTextureScale(fallbackProperty, scale);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
