using UnityEngine;

namespace RoboClash
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class RoboClashLavaVisual : MonoBehaviour
    {
        [SerializeField] private Vector2 flowSpeed = new Vector2(0.018f, 0.011f);
        [SerializeField, Range(0f, 0.5f)] private float emissionPulse = 0.12f;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private Renderer targetRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Vector2 baseTextureScale;
        private Vector2 mainTextureScale;
        private Color baseEmissionColor = Color.white;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            var material = targetRenderer.sharedMaterial;
            if (material == null)
                return;

            if (material.HasProperty("_BaseMap"))
                baseTextureScale = material.GetTextureScale("_BaseMap");
            if (material.HasProperty("_MainTex"))
                mainTextureScale = material.GetTextureScale("_MainTex");
            if (material.HasProperty(EmissionColor))
                baseEmissionColor = material.GetColor(EmissionColor);
        }

        private void Update()
        {
            if (targetRenderer == null || propertyBlock == null)
                return;

            var offset = new Vector2(
                Mathf.Repeat(Time.time * flowSpeed.x, 1f),
                Mathf.Repeat(Time.time * flowSpeed.y, 1f));
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(BaseMapSt,
                new Vector4(baseTextureScale.x, baseTextureScale.y, offset.x, offset.y));
            propertyBlock.SetVector(MainTexSt,
                new Vector4(mainTextureScale.x, mainTextureScale.y, offset.x, offset.y));
            var pulse = 1f + Mathf.Sin(Time.time * 2.7f) * emissionPulse;
            propertyBlock.SetColor(EmissionColor, baseEmissionColor * pulse);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
