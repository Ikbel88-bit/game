using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashPulseRing : MonoBehaviour
    {
        private const int Segments = 48;
        private LineRenderer line;
        private Color tint;
        private float maximumRadius;
        private float duration;
        private float elapsed;

        public static void Create(Vector3 position, Color color, float radius, float effectDuration)
        {
            var ringObject = new GameObject("RoboClash Pulse Ring");
            ringObject.hideFlags = HideFlags.DontSave;
            ringObject.transform.position = position;
            var ring = ringObject.AddComponent<RoboClashPulseRing>();
            ring.Initialize(color, radius, effectDuration);
        }

        private void Initialize(Color color, float radius, float effectDuration)
        {
            tint = color;
            maximumRadius = Mathf.Max(0.5f, radius);
            duration = Mathf.Max(0.1f, effectDuration);
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.sharedMaterial = RoboClashCombatVFX.GetParticleMaterial();
            line.startColor = color;
            line.endColor = color;

            for (var i = 0; i < Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, maximumRadius, progress);
            var fadedColor = tint;
            fadedColor.a = 1f - progress;
            line.startColor = fadedColor;
            line.endColor = fadedColor;
            line.startWidth = line.endWidth = Mathf.Lerp(0.11f, 0.015f, progress);

            if (progress >= 1f)
                Destroy(gameObject);
        }
    }
}
