using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashCombatVFX
    {
        private const int MaximumPooledEffects = 32;
        private static readonly List<ParticleSystem> Pool = new List<ParticleSystem>(MaximumPooledEffects);
        private static Material particleMaterial;

        public static Quaternion SurfaceRotation(Vector3 normal)
        {
            return normal.sqrMagnitude < 0.001f
                ? Quaternion.identity
                : Quaternion.LookRotation(normal.normalized);
        }

        public static Color GetRobotAccent(RoboClashRobotDefinition definition)
        {
            return definition != null ? definition.emissionColor : Color.white;
        }

        public static void PlayBurst(Vector3 position, Vector3 direction, Color color, float size,
            bool heavy = false)
        {
            var effect = GetAvailableEffect();
            effect.transform.SetPositionAndRotation(position,
                direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity);
            effect.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
            var main = effect.main;
            main.startColor = color;
            main.startSize = heavy ? new ParticleSystem.MinMaxCurve(0.045f, 0.16f) :
                new ParticleSystem.MinMaxCurve(0.025f, 0.09f);
            main.startLifetime = heavy ? new ParticleSystem.MinMaxCurve(0.2f, 0.55f) :
                new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startSpeed = heavy ? new ParticleSystem.MinMaxCurve(1.5f, 5f) :
                new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            var emission = effect.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(heavy ? 20 : 10)) });
            effect.Clear(true);
            effect.Play(true);
        }

        public static void PlayMuzzle(Vector3 position, Vector3 forward, Color color, float size)
        {
            PlayBurst(position, forward, color, size, false);
        }

        public static void PlayDash(Vector3 position, Vector3 direction, Color color)
        {
            PlayBurst(position, direction, color, 1.15f, true);
        }

        public static void PlayPulse(Vector3 position, Color color, float radius)
        {
            PlayBurst(position, Vector3.up, color, Mathf.Clamp(radius * 0.35f, 1f, 3f), true);
            RoboClashPulseRing.Create(position, color, radius, 0.55f);
        }

        public static void ConfigureProjectileVisual(GameObject projectile, Color color, bool rocket)
        {
            if (projectile == null)
                return;

            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetParticleMaterial();
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
            }

            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = rocket ? 0.48f : 0.24f;
            trail.startWidth = rocket ? 0.16f : 0.085f;
            trail.endWidth = 0.015f;
            trail.minVertexDistance = 0.04f;
            trail.numCapVertices = 2;
            trail.sharedMaterial = GetParticleMaterial();
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.autodestruct = false;
        }

        private static ParticleSystem GetAvailableEffect()
        {
            for (var i = 0; i < Pool.Count; i++)
            {
                var candidate = Pool[i];
                if (candidate == null)
                    continue;
                if (!candidate.IsAlive(true))
                    return candidate;
            }

            if (Pool.Count == MaximumPooledEffects)
            {
                var reused = Pool[0];
                Pool.RemoveAt(0);
                if (reused != null)
                {
                    reused.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Pool.Add(reused);
                    return reused;
                }
            }

            var instance = new GameObject("RoboClash VFX");
            instance.hideFlags = HideFlags.DontSave;
            var particles = instance.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(particles);
            Pool.Add(particles);
            return particles;
        }

        private static void ConfigureParticleSystem(ParticleSystem particles)
        {
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.12f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.sortingFudge = 0.1f;
        }

        internal static Material GetParticleMaterial()
        {
            if (particleMaterial != null)
                return particleMaterial;

            particleMaterial = Resources.Load<Material>("RoboClash/VFX/ParticleUnlit");
            if (particleMaterial != null)
                return particleMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                         Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("RoboClash: ParticleUnlit material is missing. Run Tools > RoboClash > Create VFX Materials.");
                return null;
            }

            particleMaterial = new Material(shader)
            {
                name = "RoboClash Runtime VFX",
                hideFlags = HideFlags.DontSave
            };
            return particleMaterial;
        }
    }
}
