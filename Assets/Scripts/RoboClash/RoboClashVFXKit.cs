using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashVFXKit
    {
        private const string ResourcePath = "RoboClash/VFX/";
        private const int MaxDecals = 24;

        private static Texture2D softTexture;
        private static Texture2D hexTexture;
        private static Material glowMaterial;
        private static Material flatGlowMaterial;
        private static Material smokeMaterial;
        private static Material particleSolidMaterial;
        private static Material projectileSolidMaterial;
        private static Mesh cubeMesh;
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;
        private static ParticleSystem glowSystem;
        private static ParticleSystem smokeSystem;
        private static ParticleSystem sparkSystem;
        private static ParticleSystem debrisSystem;
        private static ParticleSystem casingSystem;
        private static readonly Queue<Transform> Decals = new Queue<Transform>();
        private static readonly HashSet<string> MissingMaterials = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            softTexture = null;
            hexTexture = null;
            glowMaterial = null;
            flatGlowMaterial = null;
            smokeMaterial = null;
            particleSolidMaterial = null;
            projectileSolidMaterial = null;
            cubeMesh = null;
            sphereMesh = null;
            cylinderMesh = null;
            glowSystem = null;
            smokeSystem = null;
            sparkSystem = null;
            debrisSystem = null;
            casingSystem = null;
            Decals.Clear();
            MissingMaterials.Clear();
        }

        public static Texture2D SoftTexture =>
            softTexture != null ? softTexture : (softTexture = BuildSoftTexture());
        public static Texture2D HexTexture =>
            hexTexture != null ? hexTexture : (hexTexture = BuildHexTexture());

        public static Material GlowMaterial =>
            glowMaterial != null ? glowMaterial : (glowMaterial = CreateMaterial("GlowAdditive", SoftTexture, true));
        public static Material FlatGlowMaterial =>
            flatGlowMaterial != null ? flatGlowMaterial : (flatGlowMaterial = CreateMaterial("GlowAdditive", null, true));
        public static Material SmokeMaterial =>
            smokeMaterial != null ? smokeMaterial : (smokeMaterial = CreateMaterial("SmokeAlpha", SoftTexture, true));
        public static Material ParticleSolidMaterial =>
            particleSolidMaterial != null ? particleSolidMaterial : (particleSolidMaterial = CreateMaterial("ParticleSolid", null, false));
        public static Material ProjectileSolidMaterial =>
            projectileSolidMaterial != null ? projectileSolidMaterial : (projectileSolidMaterial = CreateMaterial("ProjectileSolid", null, false));

        public static Mesh CubeMesh => cubeMesh != null ? cubeMesh : (cubeMesh = ExtractMesh(PrimitiveType.Cube));
        public static Mesh SphereMesh => sphereMesh != null ? sphereMesh : (sphereMesh = ExtractMesh(PrimitiveType.Sphere));
        public static Mesh CylinderMesh => cylinderMesh != null ? cylinderMesh : (cylinderMesh = ExtractMesh(PrimitiveType.Cylinder));

        private static Mesh ExtractMesh(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            return mesh;
        }

        private static Material CreateMaterial(string assetName, Texture texture, bool doubleSided)
        {
            var source = Resources.Load<Material>(ResourcePath + assetName);
            if (source == null)
            {
                if (MissingMaterials.Add(assetName))
                    Debug.LogError($"RoboClash VFX material '{assetName}' is missing. Run Tools > RoboClash > Create Projectile Materials.");
                return null;
            }

            var material = new Material(source)
            {
                name = "RC " + assetName + " (runtime)",
                hideFlags = HideFlags.DontSave
            };
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }

            if (doubleSided && material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            return material;
        }

        private static Texture2D BuildSoftTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "RC Soft Glow",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildHexTexture()
        {
            const int size = 256;
            const float cells = 4f;
            const float rowHeight = 1.7320508f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "RC Hex Grid",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = (x + 0.5f) / size * cells;
                    var py = (y + 0.5f) / size * rowHeight * 2f;
                    var ax = Mathf.Repeat(px, 1f) - 0.5f;
                    var ay = Mathf.Repeat(py, rowHeight) - rowHeight * 0.5f;
                    var bx = Mathf.Repeat(px - 0.5f, 1f) - 0.5f;
                    var by = Mathf.Repeat(py - rowHeight * 0.5f, rowHeight) - rowHeight * 0.5f;
                    var useA = ax * ax + ay * ay < bx * bx + by * by;
                    var gx = Mathf.Abs(useA ? ax : bx);
                    var gy = Mathf.Abs(useA ? ay : by);
                    var cellEdge = Mathf.Max(gx, gx * 0.5f + gy * 0.8660254f);
                    var line = Mathf.Clamp01(1f - Mathf.Abs(0.5f - cellEdge) * 18f);
                    var alpha = Mathf.Max(line, 0.07f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        private static ParticleSystem Glow =>
            EnsureSystem(ref glowSystem, "Glow", GlowMaterial, ParticleSystemRenderMode.Billboard, 2500, 0f, true, 0.2f);
        private static ParticleSystem Smoke =>
            EnsureSystem(ref smokeSystem, "Smoke", SmokeMaterial, ParticleSystemRenderMode.Billboard, 900, 0f, true, 3f);
        private static ParticleSystem Sparks =>
            EnsureSystem(ref sparkSystem, "Sparks", GlowMaterial, ParticleSystemRenderMode.Stretch, 1500, 0.8f, true, 0.3f);
        private static ParticleSystem Debris =>
            EnsureSystem(ref debrisSystem, "Debris", ParticleSolidMaterial, ParticleSystemRenderMode.Mesh, 400, 1.5f, false, 0.4f, CubeMesh, true);
        private static ParticleSystem Casings =>
            EnsureSystem(ref casingSystem, "Casings", ParticleSolidMaterial, ParticleSystemRenderMode.Mesh, 150, 2f, false, 1f, CubeMesh, true);

        private static ParticleSystem EnsureSystem(ref ParticleSystem system, string systemName, Material material,
            ParticleSystemRenderMode mode, int maxParticles, float gravity, bool fade, float growth,
            Mesh mesh = null, bool spin = false)
        {
            if (system != null)
                return system;
            if (material == null)
                return null;

            var go = new GameObject("RoboClash VFX " + systemName);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = gravity;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 1f;

            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;

            if (fade)
            {
                var colorModule = particles.colorOverLifetime;
                colorModule.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f),
                        new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f)
                    });
                colorModule.color = gradient;
            }

            var sizeModule = particles.sizeOverLifetime;
            sizeModule.enabled = true;
            sizeModule.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, growth)));

            if (spin)
            {
                var rotation = particles.rotationOverLifetime;
                rotation.enabled = true;
                rotation.separateAxes = true;
                rotation.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (mesh != null)
                renderer.mesh = mesh;
            if (mode == ParticleSystemRenderMode.Stretch)
            {
                renderer.velocityScale = 0.06f;
                renderer.lengthScale = 2f;
            }

            particles.Play();
            system = particles;
            return particles;
        }

        private static void Emit(ParticleSystem particles, Vector3 position, Vector3 velocity, float lifetime,
            float size, Color color)
        {
            if (particles == null)
                return;
            var parameters = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startLifetime = lifetime,
                startSize = size,
                startColor = color
            };
            particles.Emit(parameters, 1);
        }

        public static void EmitGlow(Vector3 position, Vector3 velocity, float lifetime, float size, Color color)
        {
            Emit(Glow, position, velocity, lifetime, size, color);
        }

        public static void EmitSmoke(Vector3 position, Vector3 velocity, float lifetime, float size, Color color)
        {
            Emit(Smoke, position, velocity, lifetime, size, color);
        }

        public static void EmitSpark(Vector3 position, Vector3 velocity, float lifetime, float size, Color color)
        {
            Emit(Sparks, position, velocity, lifetime, size, color);
        }

        public static void EmitDebris(Vector3 position, Vector3 velocity, float lifetime, float size, Color color)
        {
            var parameters = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startLifetime = lifetime,
                startSize = size,
                startColor = color,
                rotation3D = Random.insideUnitSphere * 360f
            };
            var particles = Debris;
            if (particles != null)
                particles.Emit(parameters, 1);
        }

        private static void SparkSpray(Vector3 position, Vector3 direction, int count, float minSpeed,
            float maxSpeed, float cone, Color color)
        {
            for (var i = 0; i < count; i++)
            {
                var directionWithSpread = (direction + Random.insideUnitSphere * cone).normalized;
                EmitSpark(position, directionWithSpread * Random.Range(minSpeed, maxSpeed),
                    Random.Range(0.15f, 0.35f), 0.09f, color);
            }
        }

        private static void EjectCasing(Vector3 position, Vector3 direction)
        {
            var right = Vector3.Cross(Vector3.up, direction);
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.right;
            right.Normalize();
            var parameters = new ParticleSystem.EmitParams
            {
                position = position - direction * 0.1f,
                velocity = right * Random.Range(2f, 3.5f) + Vector3.up * Random.Range(1.5f, 2.5f) - direction * 0.5f,
                startLifetime = 1f,
                startSize = 0.07f,
                startColor = new Color(0.85f, 0.65f, 0.25f, 1f),
                rotation3D = Random.insideUnitSphere * 360f
            };
            var particles = Casings;
            if (particles != null)
                particles.Emit(parameters, 1);
        }

        public static MeshRenderer AddMesh(Transform parent, string objectName, Mesh mesh, Vector3 localPosition,
            Vector3 localScale, Quaternion localRotation, Material material, Color color, Color emission = default)
        {
            if (parent == null || mesh == null || material == null)
                return null;
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            if (emission.maxColorComponent > 0f)
                block.SetColor("_EmissionColor", emission);
            renderer.SetPropertyBlock(block);
            return renderer;
        }

        private static void SpawnFlash(Vector3 position, Color color, float range, float intensity, float duration)
        {
            var go = new GameObject("RC Flash");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            go.AddComponent<RoboClashTimedLight>().Begin(duration, intensity);
        }

        public static void PlayMuzzle(RoboClashRobotType type, Vector3 position, Vector3 direction, Color color)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            var hot = Color.Lerp(color, Color.white, 0.55f);
            switch (type)
            {
                case RoboClashRobotType.Titan:
                    EmitGlow(position + direction * 0.3f, direction * 2f, 0.07f, 1f, new Color(1f, 0.65f, 0.25f, 0.9f));
                    EmitGlow(position, Vector3.zero, 0.05f, 0.55f, Color.white);
                    SparkSpray(position, direction, 5, 8f, 18f, 0.5f, new Color(1f, 0.75f, 0.3f, 1f));
                    EmitSmoke(position + direction * 0.5f, direction * 1.2f + Vector3.up * 0.6f,
                        Random.Range(0.6f, 0.9f), 0.28f, new Color(0.4f, 0.4f, 0.42f, 0.35f));
                    EjectCasing(position, direction);
                    break;
                case RoboClashRobotType.Shadow:
                    EmitGlow(position + direction * 0.15f, direction, 0.05f, 0.45f, hot);
                    SparkSpray(position, direction, 2, 6f, 14f, 0.35f, color);
                    break;
                case RoboClashRobotType.Spark:
                    EmitGlow(position + direction * 0.4f, direction * 3f, 0.12f, 1.6f, new Color(1f, 0.7f, 0.25f, 0.95f));
                    EmitGlow(position, Vector3.zero, 0.08f, 0.9f, Color.white);
                    SparkSpray(position, direction, 8, 6f, 20f, 0.7f, new Color(1f, 0.7f, 0.3f, 1f));
                    for (var i = 0; i < 6; i++)
                        EmitSmoke(position - direction * 0.5f,
                            -direction * Random.Range(2f, 6f) + Random.insideUnitSphere * 1.2f,
                            Random.Range(0.7f, 1.2f), 0.45f, new Color(0.7f, 0.7f, 0.72f, 0.4f));
                    break;
                default:
                    EmitGlow(position + direction * 0.2f, direction * 1.5f, 0.06f, 0.65f, hot);
                    EmitGlow(position, Vector3.zero, 0.04f, 0.35f, Color.white);
                    SparkSpray(position, direction, 3, 8f, 16f, 0.3f, color);
                    break;
            }
        }

        public static void PlayImpact(RoboClashProjectileStyle style, Vector3 point, Vector3 normal, Color color,
            float explosionRadius, bool onRobot)
        {
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.up;
            normal.Normalize();
            switch (style)
            {
                case RoboClashProjectileStyle.Rocket:
                    PlayExplosion(point, normal, color, Mathf.Max(explosionRadius, 2.5f), onRobot);
                    break;
                case RoboClashProjectileStyle.RailSlug:
                    PlayExplosion(point, normal, color, Mathf.Max(explosionRadius, 3f), onRobot);
                    RoboClashPulseRing.Create(point + normal * 0.1f, Color.white, 4.5f, 0.3f);
                    break;
                case RoboClashProjectileStyle.Gatling:
                    EmitGlow(point + normal * 0.1f, Vector3.zero, 0.1f, 0.7f, new Color(1f, 0.7f, 0.3f, 0.9f));
                    SparkSpray(point, normal, 10, 4f, 12f, 0.9f, new Color(1f, 0.75f, 0.3f, 1f));
                    EmitSmoke(point + normal * 0.1f, normal * 0.8f + Vector3.up * 0.6f, 0.9f, 0.25f,
                        new Color(0.3f, 0.3f, 0.32f, 0.35f));
                    if (!onRobot)
                        PlayScorch(point, normal, 0.3f);
                    break;
                case RoboClashProjectileStyle.ShadowBlade:
                    EmitGlow(point + normal * 0.1f, Vector3.zero, 0.2f, 1.2f, new Color(color.r, color.g, color.b, 0.7f));
                    for (var i = 0; i < 3; i++)
                        EmitSmoke(point, normal * 1.2f + Random.insideUnitSphere, Random.Range(0.6f, 1.1f), 0.4f,
                            new Color(0.2f, 0.06f, 0.3f, 0.5f));
                    SparkSpray(point, normal, 6, 4f, 10f, 1f, color);
                    break;
                case RoboClashProjectileStyle.SmgDart:
                    EmitGlow(point + normal * 0.05f, Vector3.zero, 0.07f, 0.4f, color);
                    SparkSpray(point, normal, 4, 4f, 9f, 0.9f, color);
                    break;
                default:
                    EmitGlow(point + normal * 0.05f, Vector3.zero, 0.1f, 0.7f, new Color(color.r, color.g, color.b, 0.8f));
                    SparkSpray(point, normal, 7, 4f, 11f, 1f, color);
                    break;
            }
        }

        public static void PlayShieldImpact(Vector3 point, Vector3 normal, Color color)
        {
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.up;
            EmitGlow(point + normal.normalized * 0.1f, Vector3.zero, 0.18f, 1.3f,
                new Color(color.r, color.g, color.b, 0.8f));
            SparkSpray(point, normal.normalized, 8, 4f, 10f, 1f, color);
        }

        public static void PlayExplosion(Vector3 position, Vector3 normal, Color accent, float radius, bool onRobot)
        {
            radius = Mathf.Clamp(radius, 1.5f, 8f);
            var fire = new Color(1f, 0.55f, 0.15f, 0.95f);
            var yellow = new Color(1f, 0.9f, 0.45f, 0.95f);
            EmitGlow(position + normal * 0.2f, Vector3.zero, 0.14f, radius * 2.4f,
                new Color(1f, 0.95f, 0.8f, 0.95f));
            for (var i = 0; i < 16; i++)
            {
                var direction = (normal * 0.6f + Random.onUnitSphere).normalized;
                EmitGlow(position + direction * radius * 0.15f, direction * Random.Range(0.5f, 1.6f) * radius,
                    Random.Range(0.3f, 0.6f), Random.Range(0.6f, 1.1f) * radius * 0.7f,
                    Color.Lerp(fire, yellow, Random.value));
            }

            for (var i = 0; i < 10; i++)
            {
                var direction = normal + Random.insideUnitSphere * 0.8f;
                EmitSmoke(position + normal * 0.3f,
                    direction * Random.Range(0.8f, 2.2f) + Vector3.up * 1.2f,
                    Random.Range(1.2f, 2.2f), Random.Range(0.8f, 1.4f) * radius * 0.6f,
                    new Color(0.2f, 0.2f, 0.22f, 0.55f));
            }

            SparkSpray(position, normal, 26, 7f, 22f, 1.1f, new Color(1f, 0.8f, 0.4f, 1f));
            for (var i = 0; i < 8; i++)
                EmitDebris(position + normal * 0.2f,
                    (normal + Random.insideUnitSphere * 0.9f).normalized * Random.Range(5f, 12f),
                    Random.Range(1.2f, 2f), Random.Range(0.07f, 0.18f), new Color(0.2f, 0.2f, 0.22f, 1f));

            RoboClashPulseRing.Create(position + normal * 0.15f, Color.Lerp(accent, fire, 0.5f), radius * 1.4f, 0.35f);
            SpawnFlash(position + normal * 0.8f, fire, radius * 3f, 10f, 0.22f);
            if (!onRobot)
                PlayScorch(position, normal, radius * 0.55f);
        }

        private static void PlayScorch(Vector3 position, Vector3 normal, float size)
        {
            while (Decals.Count > 0 && Decals.Peek() == null)
                Decals.Dequeue();

            Transform quad;
            if (Decals.Count >= MaxDecals)
                quad = Decals.Dequeue();
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "RC Scorch";
                Object.Destroy(go.GetComponent<Collider>());
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = SmokeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                go.AddComponent<RoboClashFadeDecal>();
                quad = go.transform;
            }

            quad.position = position + normal * 0.03f;
            quad.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            quad.localScale = Vector3.one * size * 2f;
            quad.GetComponent<RoboClashFadeDecal>().Restart(14f, new Color(0.02f, 0.02f, 0.025f, 0.6f));
            Decals.Enqueue(quad);
        }

        public static void PlayRailBeam(Vector3 start, Vector3 end, Color color)
        {
            AddBeam(start, end, new Color(color.r, color.g, color.b, 0.55f), 0.55f, 0.35f);
            AddBeam(start, end, new Color(1f, 0.95f, 0.9f, 0.95f), 0.16f, 0.22f);
            for (var i = 0; i < 24; i++)
                EmitGlow(Vector3.Lerp(start, end, Random.value), Random.insideUnitSphere * 2f,
                    Random.Range(0.25f, 0.5f), Random.Range(0.25f, 0.5f), color);
        }

        private static void AddBeam(Vector3 start, Vector3 end, Color color, float width, float life)
        {
            var go = new GameObject("RC Beam");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.sharedMaterial = FlatGlowMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<RoboClashFadeLine>().Begin(life, width, color);
        }

        public static void PlayArc(Vector3 start, Vector3 end, Color color, float life = 0.15f,
            float width = 0.1f, float jitter = 0.35f)
        {
            var direction = end - start;
            if (direction.magnitude < 0.01f)
                return;
            var go = new GameObject("RC Arc");
            var line = go.AddComponent<LineRenderer>();
            const int points = 9;
            line.positionCount = points;
            line.useWorldSpace = true;
            line.numCapVertices = 2;
            line.sharedMaterial = FlatGlowMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var side = Vector3.Cross(direction, Random.onUnitSphere).normalized;
            var other = Vector3.Cross(direction.normalized, side);
            for (var i = 0; i < points; i++)
            {
                var point = Vector3.Lerp(start, end, i / (float)(points - 1));
                if (i > 0 && i < points - 1)
                    point += (side * Random.Range(-1f, 1f) + other * Random.Range(-1f, 1f)) * jitter;
                line.SetPosition(i, point);
            }

            go.AddComponent<RoboClashFadeLine>().Begin(life, width, color);
        }

        public static void PlayEmpBlast(Vector3 center, Color color, float radius)
        {
            var dome = new GameObject("RC EMP Dome");
            dome.transform.position = center;
            AddMesh(dome.transform, "Shell", SphereMesh, Vector3.zero, Vector3.one,
                Quaternion.identity, FlatGlowMaterial, new Color(color.r, color.g, color.b, 0.35f));
            dome.AddComponent<RoboClashFxAnimator>().Begin(
                new Color(color.r, color.g, color.b, 0.4f), 0.55f, 0.3f, radius * 2f, true);
            RoboClashPulseRing.Create(new Vector3(center.x, center.y - 1.1f, center.z), color, radius, 0.55f);
            for (var i = 0; i < 12; i++)
                PlayArc(center, center + Random.onUnitSphere * radius * Random.Range(0.55f, 0.95f),
                    Color.Lerp(color, Color.white, 0.5f), 0.22f, 0.12f, 0.5f);
            EmitGlow(center, Vector3.zero, 0.2f, radius * 0.9f, new Color(1f, 0.95f, 0.8f, 0.7f));
            for (var i = 0; i < 30; i++)
                EmitSpark(center, Random.onUnitSphere * Random.Range(6f, 14f), 0.5f, 0.1f, color);
            SpawnFlash(center, color, radius * 2f, 6f, 0.3f);
        }

        public static void PlayPortal(Vector3 position, Vector3 facing, Color color)
        {
            if (facing.sqrMagnitude < 0.001f)
                facing = Vector3.forward;
            var go = new GameObject("RC Portal");
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            for (var ring = 0; ring < 2; ring++)
            {
                var lineObject = new GameObject("Ring");
                lineObject.transform.SetParent(go.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                const int points = 40;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = points;
                line.startWidth = ring == 0 ? 0.14f : 0.06f;
                line.endWidth = line.startWidth;
                line.sharedMaterial = FlatGlowMaterial;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var scale = ring == 0 ? 1f : 0.72f;
                for (var i = 0; i < points; i++)
                {
                    var angle = i / (float)points * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.8f * scale,
                        Mathf.Sin(angle) * 1.2f * scale, 0f));
                    line.startColor = color;
                    line.endColor = color;
                }
            }

            go.AddComponent<RoboClashFxAnimator>().Begin(
                new Color(color.r, color.g, color.b, 0.95f), 0.45f, 0.2f, 1.25f, true);
            var right = go.transform.right;
            var up = go.transform.up;
            for (var i = 0; i < 18; i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var point = position + right * Mathf.Cos(angle) * 0.9f + up * Mathf.Sin(angle) * 1.3f;
                EmitGlow(point, (position - point) * 3f, 0.3f, Random.Range(0.15f, 0.3f), color);
            }

            for (var i = 0; i < 5; i++)
                EmitSmoke(position, Random.insideUnitSphere * 1.2f, Random.Range(0.6f, 1.1f),
                    0.5f, new Color(0.2f, 0.06f, 0.3f, 0.5f));
        }

        public static GameObject PlayRailCharge(Transform muzzle, Color color, float duration)
        {
            if (muzzle == null)
                return null;
            var go = new GameObject("RC Rail Charge");
            go.transform.SetParent(muzzle, false);
            AddMesh(go.transform, "Orb", SphereMesh, Vector3.zero, Vector3.one,
                Quaternion.identity, FlatGlowMaterial, new Color(color.r, color.g, color.b, 0.9f));
            go.AddComponent<RoboClashFxAnimator>().Begin(
                new Color(1f, 0.8f, 0.7f, 0.85f), duration, 0.1f, 0.6f, false);
            go.AddComponent<RoboClashChargeFx>().Begin(color, duration);
            return go;
        }

        public static void PlayDashGhosts(Transform modelRoot, Vector3 start, Vector3 end, Color color)
        {
            if (modelRoot == null)
                return;
            const int count = 4;
            var modelOffset = modelRoot.position - end;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)count;
                var ghost = Object.Instantiate(modelRoot.gameObject);
                ghost.name = "RC Dash Afterimage";
                ghost.transform.SetParent(null, true);
                ghost.transform.position = start + (end - start) * t + modelOffset;
                ghost.transform.rotation = modelRoot.rotation;
                StripGhost(ghost);
                ghost.AddComponent<RoboClashFxAnimator>().Begin(
                    new Color(color.r, color.g, color.b, 0.16f), 0.28f + i * 0.08f, 1f, 1f, true);
            }
        }

        private static void StripGhost(GameObject ghost)
        {
            foreach (var component in ghost.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component is Transform || component is MeshFilter)
                    continue;
                if (component is MeshRenderer renderer)
                {
                    var objectName = renderer.gameObject.name;
                    if (objectName.Contains("Field") || objectName.Contains("Cloak") ||
                        objectName.Contains("MuzzleFlash"))
                    {
                        renderer.enabled = false;
                        continue;
                    }

                    renderer.enabled = true;
                    renderer.sharedMaterial = FlatGlowMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    continue;
                }
                Object.Destroy(component);
            }
        }

        public static void PlayHealPulse(Vector3 position, Color accent)
        {
            var green = new Color(0.45f, 1f, 0.55f, 0.9f);
            for (var i = 0; i < 12; i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(0.3f, 0.8f);
                EmitGlow(position + offset + Vector3.up * 0.1f, Vector3.up * Random.Range(1.2f, 2.2f),
                    Random.Range(0.7f, 1.1f), Random.Range(0.14f, 0.26f), Color.Lerp(green, accent, 0.25f));
            }
            EmitGlow(position + Vector3.up * 0.05f, Vector3.zero, 0.35f, 2.2f,
                new Color(green.r, green.g, green.b, 0.25f));
        }

        public static void PlayCloakSparkle(Vector3 position, Color color)
        {
            EmitGlow(position, Random.insideUnitSphere * 0.8f, 0.5f, 0.2f, color);
            EmitSmoke(position, Vector3.up * 0.5f, 0.8f, 0.35f, new Color(0.18f, 0.05f, 0.25f, 0.4f));
        }
    }
}
