using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashRobotVFX : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer shieldRenderer;
        [SerializeField] private LineRenderer repairBeam;
        [SerializeField] private Light muzzleLight;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotAudio audioPlayer;
        private ArenaDamageable damageable;
        private Transform beamTarget;
        private MaterialPropertyBlock shieldProperties;
        private RoboClashLightningAura overdriveAura;
        private Renderer guardShieldRenderer;
        private MaterialPropertyBlock guardShieldProperties;
        private float muzzleLightIntensity;
        private float shieldFlashEndTime;
        private Transform repairDrone;
        private Vector3 repairDroneStowedPosition;
        private Vector3 repairDroneDeployedPosition;
        private float repairDroneDeployment;
        private bool repairDroneDeployed;
        private bool overdrive;

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            damageable = GetComponent<ArenaDamageable>();
            if (damageable != null)
                damageable.DamageApplied += HandleDamageApplied;
            ConfigureVisuals();
        }

        private void OnEnable()
        {
            RoboClashGuardEvents.Changed += HandleGuardChanged;
        }

        public void ConfigureVisuals()
        {
            if (definition == null)
                definition = GetComponent<RoboClashRobotDefinition>();
            if (audioPlayer == null)
                audioPlayer = GetComponent<RoboClashRobotAudio>();
            if (damageable == null)
                damageable = GetComponent<ArenaDamageable>();
            if (definition != null && definition.repairDrone != null && repairDrone == null)
            {
                repairDrone = definition.repairDrone;
                repairDroneStowedPosition = new Vector3(0.35f, 1.55f, -0.55f);
                repairDroneDeployedPosition = new Vector3(0.9f, 2.05f, -0.55f);
                repairDrone.localPosition = repairDroneStowedPosition;
                repairDrone.localScale = Vector3.one * 0.55f;
            }
            CreateShieldField();
            CreateRepairBeam();
            CreateMuzzleLight();
        }

        public void PlayMuzzle(Vector3 position, Vector3 direction, bool heavy)
        {
            var accent = RoboClashCombatVFX.GetRobotAccent(definition);
            RoboClashVFXKit.PlayMuzzle(definition.robotType, position, direction, accent);
            if (muzzleLight != null)
            {
                muzzleLight.transform.position = position;
                muzzleLight.transform.forward = direction;
                muzzleLight.color = accent;
                muzzleLightIntensity = heavy ? 8f : 4.5f;
                muzzleLight.intensity = muzzleLightIntensity;
                muzzleLight.enabled = true;
            }
        }

        public void PlayAbility()
        {
            var point = transform.position + Vector3.up * 1.1f;
            RoboClashCombatVFX.PlayBurst(point, transform.forward, definition.emissionColor, 0.8f, false);
            audioPlayer?.Play(GetAbilityAudioEvent());
        }

        public void PlayUltimate()
        {
            var point = transform.position + Vector3.up * 1.1f;
            RoboClashCombatVFX.PlayPulse(point, definition.emissionColor, 7f);
            audioPlayer?.Play(GetUltimateAudioEvent());
        }

        public void PlayDash(Vector3 direction)
        {
            RoboClashCombatVFX.PlayDash(transform.position + Vector3.up, direction, definition.emissionColor);
            audioPlayer?.Play(RoboClashAudioEvent.Dash);
        }

        public void PlayDash(Vector3 start, Vector3 end, Vector3 direction)
        {
            RoboClashVFXKit.PlayDashGhosts(definition.modelRoot, start, end, definition.emissionColor);
            for (var i = 0; i <= 2; i++)
            {
                var point = Vector3.Lerp(start, end, i / 2f) + Vector3.up * 0.85f;
                RoboClashCombatVFX.PlayDash(point, direction, definition.emissionColor);
            }
            audioPlayer?.Play(RoboClashAudioEvent.Dash);
        }

        public void PlayTeleport(Vector3 start, Vector3 end, Vector3 facing)
        {
            RoboClashVFXKit.PlayPortal(start + Vector3.up, facing, definition.emissionColor);
            RoboClashVFXKit.PlayPortal(end + Vector3.up, facing, definition.emissionColor);
            RoboClashVFXKit.PlayDashGhosts(definition.modelRoot, start, end, definition.emissionColor);
            audioPlayer?.Play(RoboClashAudioEvent.Dash);
        }

        public void PlayRespawn()
        {
            RoboClashCombatVFX.PlayPulse(transform.position + Vector3.up, definition.emissionColor, 2.2f);
            audioPlayer?.Play(RoboClashAudioEvent.RepairPulse);
        }

        public void PlayEmpDisruption()
        {
            RoboClashCombatVFX.PlayBurst(transform.position + Vector3.up, Vector3.up,
                definition.emissionColor, 0.6f, false);
            RoboClashLightningAura.Attach(transform, definition.emissionColor, 3f, 1.9f, 1f, 5);
        }

        public void PlayCloakTransition(bool cloaked)
        {
            var color = cloaked
                ? new Color(0.65f, 0.3f, 1f, 0.9f)
                : new Color(0.75f, 0.55f, 1f, 0.8f);
            var center = definition.modelRoot != null
                ? definition.modelRoot.position + Vector3.up
                : transform.position + Vector3.up;
            for (var i = 0; i < 10; i++)
                RoboClashVFXKit.PlayCloakSparkle(center + Random.insideUnitSphere * 0.8f, color);
        }

        public void SetRepairDroneDeployed(bool deployed)
        {
            repairDroneDeployed = deployed;
        }

        public void SetShieldField(bool active)
        {
            if (shieldRenderer == null)
                return;

            shieldRenderer.enabled = active;
            if (active)
            {
                shieldFlashEndTime = 0f;
                RoboClashCombatVFX.PlayBurst(transform.position + Vector3.up,
                    Vector3.up, definition.emissionColor, 0.7f, true);
            }
        }

        public void SetOverdrive(bool active)
        {
            overdrive = active;
            if (shieldRenderer != null)
                shieldRenderer.enabled = active;
            if (active)
            {
                if (overdriveAura == null)
                    overdriveAura = RoboClashLightningAura.Attach(
                        transform, definition.emissionColor, 0f, 1.9f, 0.95f, 6);
            }
            else if (overdriveAura != null)
            {
                overdriveAura.Stop();
                overdriveAura = null;
            }
        }

        public void SetRepairTarget(Transform target)
        {
            beamTarget = target;
            if (repairBeam != null)
                repairBeam.enabled = target != null;
            if (target != null)
                audioPlayer?.Play(RoboClashAudioEvent.DroneRepair);
        }

        public void PlayImpact(Vector3 position, Vector3 normal, bool heavy)
        {
            RoboClashCombatVFX.PlayBurst(position, normal, definition.emissionColor,
                heavy ? 0.8f : 0.45f, heavy);
            audioPlayer?.Play(RoboClashAudioEvent.ProjectileImpact);
        }

        public void StopAllEffects()
        {
            SetShieldField(false);
            SetOverdrive(false);
            SetGuardShield(false);
            SetRepairTarget(null);
            SetRepairDroneDeployed(false);
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
                muzzleLightIntensity = 0f;
            }
        }

        private RoboClashAudioEvent GetAbilityAudioEvent()
        {
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan: return RoboClashAudioEvent.ShieldActivation;
                case RoboClashRobotType.Shadow: return RoboClashAudioEvent.Cloak;
                case RoboClashRobotType.Spark: return RoboClashAudioEvent.RepairPulse;
                default: return RoboClashAudioEvent.Dash;
            }
        }

        private RoboClashAudioEvent GetUltimateAudioEvent()
        {
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan:
                case RoboClashRobotType.Volt: return RoboClashAudioEvent.Charge;
                case RoboClashRobotType.Spark: return RoboClashAudioEvent.Emp;
                default: return RoboClashAudioEvent.Ultimate;
            }
        }

        private void Update()
        {
            if (muzzleLight != null && muzzleLight.enabled)
            {
                muzzleLightIntensity = Mathf.MoveTowards(muzzleLightIntensity, 0f, 55f * Time.deltaTime);
                muzzleLight.intensity = muzzleLightIntensity;
                if (muzzleLightIntensity <= 0.01f)
                    muzzleLight.enabled = false;
            }

            if (shieldRenderer != null && shieldRenderer.enabled)
            {
                var pulse = overdrive ? 1f + Mathf.Sin(Time.time * 7f) * 0.035f : 1f;
                shieldRenderer.transform.localScale = Vector3.one * (overdrive ? 1.58f : 1.48f) * pulse;
                if (shieldProperties != null)
                {
                    var baseColor = definition.emissionColor;
                    baseColor.a = 0.24f;
                    var flash = Mathf.Clamp01((shieldFlashEndTime - Time.time) / 0.2f);
                    var color = Color.Lerp(baseColor, Color.white, flash);
                    color.a = Mathf.Lerp(baseColor.a, 0.72f, flash);
                    shieldProperties.SetColor(BaseColor, color);
                    shieldRenderer.SetPropertyBlock(shieldProperties);
                }
            }

            if (repairDrone != null)
            {
                var target = repairDroneDeployed ? 1f : 0f;
                repairDroneDeployment = Mathf.MoveTowards(repairDroneDeployment, target, Time.deltaTime * 3f);
                var eased = repairDroneDeployment * repairDroneDeployment *
                            (3f - 2f * repairDroneDeployment);
                repairDrone.localPosition = Vector3.Lerp(
                    repairDroneStowedPosition, repairDroneDeployedPosition, eased);
                repairDrone.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, eased);
                repairDrone.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
            }

            if (repairBeam == null || !repairBeam.enabled || beamTarget == null ||
                definition.repairDrone == null)
                return;

            repairBeam.SetPosition(0, definition.repairDrone.position);
            repairBeam.SetPosition(1, beamTarget.position + Vector3.up);
        }

        private void HandleGuardChanged(RoboClashGuardMode mode, bool guarding)
        {
            if (mode != null && mode.gameObject == gameObject)
                SetGuardShield(guarding);
        }

        private void SetGuardShield(bool active)
        {
            if (active && guardShieldRenderer == null)
                CreateGuardShield();
            if (guardShieldRenderer != null)
                guardShieldRenderer.enabled = active;
        }

        private void CreateGuardShield()
        {
            var parent = definition != null && definition.modelRoot != null
                ? definition.modelRoot
                : transform;
            var field = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            field.name = "Guard Shield Visual";
            field.transform.SetParent(parent, false);
            field.transform.localPosition = Vector3.up * 0.96f;
            field.transform.localScale = Vector3.one * 1.65f;
            var collider = field.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            guardShieldRenderer = field.GetComponent<Renderer>();
            guardShieldRenderer.sharedMaterial = RoboClashVFXKit.FlatGlowMaterial;
            guardShieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            guardShieldRenderer.receiveShadows = false;
            guardShieldProperties = new MaterialPropertyBlock();
            guardShieldProperties.SetTexture(BaseMap, RoboClashVFXKit.HexTexture);
            guardShieldProperties.SetTexture(MainTex, RoboClashVFXKit.HexTexture);
            var color = new Color(0.16f, 0.55f, 1f, 0.22f);
            guardShieldProperties.SetColor(BaseColor, color);
            guardShieldProperties.SetColor(ColorProperty, color);
            guardShieldRenderer.SetPropertyBlock(guardShieldProperties);
            guardShieldRenderer.enabled = false;
        }

        private void CreateShieldField()
        {
            if (shieldRenderer != null || definition == null || definition.modelRoot == null)
                return;

            var field = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            field.name = "Energy Field Visual";
            field.transform.SetParent(definition.modelRoot, false);
            field.transform.localPosition = Vector3.up * 0.96f;
            field.transform.localScale = Vector3.one * 1.48f;
            var collider = field.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            shieldRenderer = field.GetComponent<Renderer>();
            shieldRenderer.sharedMaterial = RoboClashVFXKit.FlatGlowMaterial;
            shieldProperties = new MaterialPropertyBlock();
            shieldProperties.SetTexture(BaseMap, RoboClashVFXKit.HexTexture);
            shieldProperties.SetTexture(MainTex, RoboClashVFXKit.HexTexture);
            var fieldColor = definition.emissionColor;
            fieldColor.a = 0.24f;
            shieldProperties.SetColor(BaseColor, fieldColor);
            shieldProperties.SetColor("_Color", fieldColor);
            shieldRenderer.SetPropertyBlock(shieldProperties);
            shieldRenderer.enabled = false;
            shieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldRenderer.receiveShadows = false;
        }

        private void HandleDamageApplied(ArenaDamageable target, float amount, ArenaDamageable instigator)
        {
            if (target == damageable && definition.robotType == RoboClashRobotType.Titan &&
                shieldRenderer != null && shieldRenderer.enabled)
            {
                shieldFlashEndTime = Time.time + 0.2f;
                RoboClashVFXKit.PlayShieldImpact(transform.position + Vector3.up, Vector3.up,
                    definition.emissionColor);
            }
        }

        private void CreateRepairBeam()
        {
            if (repairBeam != null)
                return;
            var beam = new GameObject("Repair Beam");
            beam.transform.SetParent(transform, false);
            repairBeam = beam.AddComponent<LineRenderer>();
            repairBeam.positionCount = 2;
            repairBeam.startWidth = 0.045f;
            repairBeam.endWidth = 0.015f;
            repairBeam.useWorldSpace = true;
            repairBeam.enabled = false;
            repairBeam.sharedMaterial = definition != null && definition.emissionMaterial != null
                ? definition.emissionMaterial
                : RoboClashCombatVFX.GetParticleMaterial();
            repairBeam.startColor = definition != null ? definition.emissionColor : Color.yellow;
            repairBeam.endColor = Color.white;
        }

        private void CreateMuzzleLight()
        {
            if (muzzleLight != null)
                return;
            var lightObject = new GameObject("Muzzle Light Pulse");
            lightObject.transform.SetParent(transform, false);
            muzzleLight = lightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 0f;
            muzzleLight.enabled = false;
        }

        private void OnDisable()
        {
            RoboClashGuardEvents.Changed -= HandleGuardChanged;
            StopAllEffects();
        }

        private void OnDestroy()
        {
            RoboClashGuardEvents.Changed -= HandleGuardChanged;
            if (damageable != null)
                damageable.DamageApplied -= HandleDamageApplied;
            if (overdriveAura != null)
                overdriveAura.Stop();
        }
    }
}
