using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashFadeLine : MonoBehaviour
    {
        private LineRenderer line;
        private float life = 0.2f;
        private float elapsed;
        private float startWidth = 0.1f;
        private Color color = Color.white;

        public void Begin(float lifetime, float width, Color tint)
        {
            line = GetComponent<LineRenderer>();
            life = Mathf.Max(0.02f, lifetime);
            startWidth = width;
            color = tint;
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / life);
            Apply(t);
            if (t >= 1f)
                Destroy(gameObject);
        }

        private void Apply(float t)
        {
            if (line == null)
                return;
            line.startWidth = startWidth * (1f - t);
            line.endWidth = startWidth * (1f - t) * 0.6f;
            var faded = new Color(color.r, color.g, color.b, color.a * (1f - t));
            line.startColor = faded;
            line.endColor = faded;
        }
    }

    public sealed class RoboClashTimedLight : MonoBehaviour
    {
        private Light target;
        private float duration = 0.2f;
        private float elapsed;
        private float startIntensity = 5f;

        public void Begin(float seconds, float intensity)
        {
            target = GetComponent<Light>();
            duration = Mathf.Max(0.02f, seconds);
            startIntensity = intensity;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            if (target != null)
                target.intensity = startIntensity * (1f - t) * (1f - t);
            if (t >= 1f)
                Destroy(gameObject);
        }
    }

    public sealed class RoboClashSpin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 0f, 900f);

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    public sealed class RoboClashFxAnimator : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MeshRenderer[] meshes;
        private LineRenderer[] lines;
        private MaterialPropertyBlock block;
        private Color tint = Color.white;
        private float duration = 0.3f;
        private float elapsed;
        private float startScale = 1f;
        private float endScale = 1f;
        private bool fade = true;

        public void Begin(Color color, float seconds, float fromScale, float toScale, bool fadeOut)
        {
            tint = color;
            duration = Mathf.Max(0.02f, seconds);
            startScale = fromScale;
            endScale = toScale;
            fade = fadeOut;
            meshes = GetComponentsInChildren<MeshRenderer>(true);
            lines = GetComponentsInChildren<LineRenderer>(true);
            block = new MaterialPropertyBlock();
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            Apply(t);
            if (t >= 1f)
                Destroy(gameObject);
        }

        private void Apply(float t)
        {
            var eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
            var alpha = fade ? Mathf.Pow(1f - t, 1.5f) : 1f;
            var color = new Color(tint.r, tint.g, tint.b, tint.a * alpha);
            foreach (var mesh in meshes)
            {
                if (mesh == null)
                    continue;
                mesh.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                mesh.SetPropertyBlock(block);
            }

            foreach (var line in lines)
            {
                if (line == null)
                    continue;
                line.startColor = color;
                line.endColor = color;
            }
        }
    }

    public sealed class RoboClashFadeDecal : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock block;
        private Color color = new Color(0f, 0f, 0f, 0.6f);
        private float life = 12f;
        private float elapsed;

        public void Restart(float seconds, Color tint)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (block == null)
                block = new MaterialPropertyBlock();
            life = Mathf.Max(0.5f, seconds);
            color = tint;
            elapsed = 0f;
            gameObject.SetActive(true);
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / life);
            Apply(t);
            if (t >= 1f)
                gameObject.SetActive(false);
        }

        private void Apply(float t)
        {
            if (meshRenderer == null)
                return;
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(color.r, color.g, color.b, color.a * (1f - t * t)));
            meshRenderer.SetPropertyBlock(block);
        }
    }

    public sealed class RoboClashTrailDetacher : MonoBehaviour
    {
        private void OnDestroy()
        {
            if (!Application.isPlaying || !gameObject.scene.isLoaded)
                return;

            foreach (var trail in GetComponentsInChildren<TrailRenderer>(true))
            {
                if (trail == null)
                    continue;
                trail.emitting = false;
                trail.transform.SetParent(null, true);
                Destroy(trail.gameObject, trail.time + 0.15f);
            }
        }
    }

    public sealed class RoboClashProjectileEmitter : MonoBehaviour
    {
        public enum Mode
        {
            Rocket,
            Rail,
            ShadowBlade
        }

        private Mode mode;
        private Color color = Color.white;
        private float tailOffset = 0.4f;
        private float nextEmitTime;

        public void Begin(Mode emitterMode, Color tint, float tail)
        {
            mode = emitterMode;
            color = tint;
            tailOffset = tail;
        }

        private void Update()
        {
            if (Time.time < nextEmitTime)
                return;
            nextEmitTime = Time.time + 0.025f;

            var back = -transform.forward;
            var tail = transform.position + back * tailOffset;
            switch (mode)
            {
                case Mode.Rocket:
                    RoboClashVFXKit.EmitGlow(tail,
                        back * Random.Range(2f, 5f) + Random.insideUnitSphere * 0.6f,
                        Random.Range(0.12f, 0.22f), Random.Range(0.28f, 0.42f),
                        Color.Lerp(color, new Color(1f, 0.9f, 0.5f, 1f), Random.value));
                    RoboClashVFXKit.EmitGlow(tail, Vector3.zero, 0.08f, 0.55f,
                        new Color(1f, 0.95f, 0.7f, 0.9f));
                    RoboClashVFXKit.EmitSmoke(tail,
                        back * 0.5f + Random.insideUnitSphere * 0.4f,
                        Random.Range(0.9f, 1.4f), 0.32f, new Color(0.45f, 0.45f, 0.48f, 0.45f));
                    break;
                case Mode.Rail:
                    RoboClashVFXKit.EmitGlow(tail + Random.insideUnitSphere * 0.15f,
                        Random.insideUnitSphere * 1.5f, Random.Range(0.2f, 0.4f),
                        Random.Range(0.3f, 0.5f), color);
                    RoboClashVFXKit.EmitSpark(tail,
                        back * Random.Range(2f, 8f) + Random.insideUnitSphere * 3f,
                        Random.Range(0.2f, 0.4f), 0.1f, Color.Lerp(color, Color.white, 0.5f));
                    break;
                case Mode.ShadowBlade:
                    RoboClashVFXKit.EmitSmoke(transform.position,
                        Random.insideUnitSphere * 0.6f, Random.Range(0.5f, 0.9f), 0.3f,
                        new Color(0.2f, 0.06f, 0.3f, 0.4f));
                    RoboClashVFXKit.EmitGlow(transform.position, Random.insideUnitSphere * 0.5f,
                        0.2f, 0.25f, color);
                    break;
            }
        }
    }

    public sealed class RoboClashChargeFx : MonoBehaviour
    {
        private Color color = Color.white;
        private float duration = 0.7f;
        private float elapsed;

        public void Begin(Color tint, float seconds)
        {
            color = tint;
            duration = Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var radius = Mathf.Lerp(1.4f, 0.15f, t);
            for (var i = 0; i < 3; i++)
            {
                var offset = Random.onUnitSphere * radius;
                RoboClashVFXKit.EmitGlow(transform.position + offset, -offset / 0.12f, 0.12f,
                    0.12f + 0.2f * t, Color.Lerp(color, Color.white, t));
            }
        }
    }

    public sealed class RoboClashLightningAura : MonoBehaviour
    {
        private LineRenderer[] lines;
        private float endTime = float.PositiveInfinity;
        private float nextUpdate;
        private float radius = 0.75f;
        private float height = 1.7f;

        public static RoboClashLightningAura Attach(Transform target, Color color, float duration,
            float auraHeight = 1.7f, float auraRadius = 0.75f, int arcs = 4)
        {
            var go = new GameObject("RC Lightning Aura");
            go.transform.SetParent(target, false);
            var aura = go.AddComponent<RoboClashLightningAura>();
            aura.Init(color, duration, auraHeight, auraRadius, arcs);
            return aura;
        }

        public void Stop()
        {
            Destroy(gameObject);
        }

        private void Init(Color color, float duration, float auraHeight, float auraRadius, int arcs)
        {
            radius = auraRadius;
            height = auraHeight;
            endTime = duration > 0f ? Time.time + duration : float.PositiveInfinity;
            lines = new LineRenderer[Mathf.Max(1, arcs)];
            for (var i = 0; i < lines.Length; i++)
            {
                var lineObject = new GameObject("Arc");
                lineObject.transform.SetParent(transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 7;
                line.startWidth = 0.07f;
                line.endWidth = 0.03f;
                line.numCapVertices = 2;
                line.sharedMaterial = RoboClashVFXKit.FlatGlowMaterial;
                line.startColor = color;
                line.endColor = color;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lines[i] = line;
            }
        }

        private void Update()
        {
            if (Time.time >= endTime)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time < nextUpdate || lines == null)
                return;
            nextUpdate = Time.time + 0.06f;
            foreach (var line in lines)
                Randomize(line);
        }

        private void Randomize(LineRenderer line)
        {
            var start = RandomPoint();
            var end = RandomPoint();
            var last = line.positionCount - 1;
            for (var i = 0; i <= last; i++)
            {
                var point = Vector3.Lerp(start, end, i / (float)last);
                if (i > 0 && i < last)
                    point += Random.insideUnitSphere * 0.12f;
                line.SetPosition(i, point);
            }

            line.enabled = Random.value > 0.15f;
        }

        private Vector3 RandomPoint()
        {
            var direction = Random.onUnitSphere;
            return new Vector3(direction.x * radius, 0.15f + Random.value * height, direction.z * radius);
        }
    }
}
