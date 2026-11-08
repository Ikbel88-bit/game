using UnityEngine;

namespace RoboClash
{
    public enum RoboClashProjectileStyle
    {
        Pulse,
        Gatling,
        SmgDart,
        Rocket,
        RailSlug,
        ShadowBlade
    }

    public static class RoboClashProjectileVisuals
    {
        public static void Apply(GameObject projectile, RoboClashProjectileStyle style, Color color)
        {
            if (projectile == null)
                return;
            var baseRenderer = projectile.GetComponent<MeshRenderer>();
            if (baseRenderer != null)
                baseRenderer.enabled = false;

            projectile.transform.localScale = Vector3.one;
            var root = projectile.transform;
            var glow = RoboClashVFXKit.FlatGlowMaterial;
            var solid = RoboClashVFXKit.ProjectileSolidMaterial;
            var sphere = RoboClashVFXKit.SphereMesh;
            var cube = RoboClashVFXKit.CubeMesh;
            var cylinder = RoboClashVFXKit.CylinderMesh;
            var radius = 0.12f;

            switch (style)
            {
                case RoboClashProjectileStyle.Gatling:
                    RoboClashVFXKit.AddMesh(root, "Core", sphere, Vector3.zero, new Vector3(0.09f, 0.09f, 0.8f),
                        Quaternion.identity, glow, new Color(1f, 0.92f, 0.55f, 1f));
                    RoboClashVFXKit.AddMesh(root, "Halo", sphere, Vector3.zero, new Vector3(0.24f, 0.24f, 1.15f),
                        Quaternion.identity, glow, new Color(1f, 0.45f, 0.12f, 0.3f));
                    AddTrail(root, new Color(1f, 0.5f, 0.15f, 1f), 0.2f, 0.12f);
                    radius = 0.1f;
                    break;

                case RoboClashProjectileStyle.SmgDart:
                    RoboClashVFXKit.AddMesh(root, "Core", sphere, Vector3.zero, new Vector3(0.035f, 0.035f, 1.1f),
                        Quaternion.identity, glow, new Color(1f, 0.9f, 1f, 1f));
                    RoboClashVFXKit.AddMesh(root, "Halo", sphere, Vector3.zero, new Vector3(0.1f, 0.1f, 1.5f),
                        Quaternion.identity, glow, new Color(color.r, color.g, color.b, 0.3f));
                    AddTrail(root, color, 0.12f, 0.05f);
                    radius = 0.08f;
                    break;

                case RoboClashProjectileStyle.Rocket:
                    BuildRocket(root, color, solid, glow, sphere, cube, cylinder);
                    radius = 0.2f;
                    break;

                case RoboClashProjectileStyle.RailSlug:
                    RoboClashVFXKit.AddMesh(root, "Core", sphere, Vector3.zero, new Vector3(0.16f, 0.16f, 2f),
                        Quaternion.identity, glow, new Color(1f, 0.95f, 0.9f, 1f));
                    RoboClashVFXKit.AddMesh(root, "Halo", sphere, Vector3.zero, new Vector3(0.5f, 0.5f, 3f),
                        Quaternion.identity, glow, new Color(color.r, color.g, color.b, 0.35f));
                    AddTrail(root, color, 0.5f, 0.55f);
                    root.gameObject.AddComponent<RoboClashProjectileEmitter>()
                        .Begin(RoboClashProjectileEmitter.Mode.Rail, color, 1f);
                    radius = 0.28f;
                    break;

                case RoboClashProjectileStyle.ShadowBlade:
                    var blades = new GameObject("Blades").transform;
                    blades.SetParent(root, false);
                    RoboClashVFXKit.AddMesh(blades, "Blade A", cube, Vector3.zero,
                        new Vector3(0.9f, 0.035f, 0.2f), Quaternion.identity, solid,
                        new Color(0.9f, 0.8f, 1f, 1f), color * 2f);
                    RoboClashVFXKit.AddMesh(blades, "Blade B", cube, Vector3.zero,
                        new Vector3(0.035f, 0.9f, 0.2f), Quaternion.identity, solid,
                        new Color(0.9f, 0.8f, 1f, 1f), color * 2f);
                    RoboClashVFXKit.AddMesh(root, "Halo", sphere, Vector3.zero, Vector3.one * 0.55f,
                        Quaternion.identity, glow, new Color(color.r, color.g, color.b, 0.22f));
                    blades.gameObject.AddComponent<RoboClashSpin>().degreesPerSecond =
                        new Vector3(0f, 0f, 1100f);
                    AddTrail(root, color, 0.3f, 0.28f);
                    root.gameObject.AddComponent<RoboClashProjectileEmitter>()
                        .Begin(RoboClashProjectileEmitter.Mode.ShadowBlade, color, 0f);
                    radius = 0.22f;
                    break;

                default:
                    RoboClashVFXKit.AddMesh(root, "Core", sphere, Vector3.zero, new Vector3(0.1f, 0.1f, 0.55f),
                        Quaternion.identity, glow, Color.Lerp(color, Color.white, 0.8f));
                    RoboClashVFXKit.AddMesh(root, "Halo", sphere, Vector3.zero, new Vector3(0.26f, 0.26f, 1f),
                        Quaternion.identity, glow, new Color(color.r, color.g, color.b, 0.35f));
                    AddTrail(root, color, 0.16f, 0.09f);
                    radius = 0.12f;
                    break;
            }

            var sphereCollider = projectile.GetComponent<SphereCollider>();
            if (sphereCollider != null)
                sphereCollider.radius = radius;
            projectile.AddComponent<RoboClashTrailDetacher>();
        }

        private static void BuildRocket(Transform root, Color color, Material solid, Material glow,
            Mesh sphere, Mesh cube, Mesh cylinder)
        {
            var alongZ = Quaternion.Euler(90f, 0f, 0f);
            RoboClashVFXKit.AddMesh(root, "Body", cylinder, Vector3.zero, new Vector3(0.16f, 0.32f, 0.16f),
                alongZ, solid, new Color(0.78f, 0.8f, 0.84f, 1f));
            RoboClashVFXKit.AddMesh(root, "Band", cylinder, new Vector3(0f, 0f, -0.12f),
                new Vector3(0.17f, 0.04f, 0.17f), alongZ, solid, color, color);
            RoboClashVFXKit.AddMesh(root, "Nose", sphere, new Vector3(0f, 0f, 0.36f),
                new Vector3(0.16f, 0.16f, 0.26f), Quaternion.identity, solid,
                new Color(1f, 0.55f, 0.15f, 1f));
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.1f;
                var scale = i % 2 == 0
                    ? new Vector3(0.12f, 0.02f, 0.15f)
                    : new Vector3(0.02f, 0.12f, 0.15f);
                RoboClashVFXKit.AddMesh(root, "Fin", cube, new Vector3(offset.x, offset.y, -0.26f),
                    scale, Quaternion.identity, solid, new Color(0.18f, 0.19f, 0.22f, 1f));
            }

            RoboClashVFXKit.AddMesh(root, "Tail Glow", sphere, new Vector3(0f, 0f, -0.4f),
                Vector3.one * 0.26f, Quaternion.identity, glow,
                new Color(1f, 0.7f, 0.3f, 0.8f));
            var lightObject = new GameObject("Rocket Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, -0.45f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.25f, 1f);
            light.range = 7f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            root.gameObject.AddComponent<RoboClashProjectileEmitter>()
                .Begin(RoboClashProjectileEmitter.Mode.Rocket, color, 0.42f);
        }

        private static void AddTrail(Transform parent, Color color, float time, float width)
        {
            var go = new GameObject("Trail");
            go.transform.SetParent(parent, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 2;
            trail.sharedMaterial = RoboClashVFXKit.FlatGlowMaterial;
            trail.startColor = new Color(color.r, color.g, color.b, 0.9f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.autodestruct = false;
        }
    }
}
