using System.Collections;
using System;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement))]
    public sealed class RoboClashRobotAbilities : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float abilityCooldown = 12f;
        [SerializeField, Min(0f)] private float ultimateCooldown = 45f;
        [SerializeField, Min(0f)] private float ultimateChargeRequired = 100f;
        [SerializeField, Min(0f)] private float supportRange = 8f;
        [SerializeField, Min(0f)] private float supportShieldPerSecond = 1.5f;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotMovement movement;
        private RoboClashRobotCombat combat;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotVFX visualEffects;
        private RoboClashRobotAudio audioPlayer;
        private ArenaDamageable damageable;
        private float nextAbilityTime;
        private float nextUltimateTime;
        private float abilityActiveUntil;
        private float ultimateActiveUntil;
        private float ultimateCharge;
        private float temporaryDamageMultiplier = 1f;
        private float overdriveEndTime;
        private float empLockEndTime;
        private float empSlowEndTime;
        private float empSlowMultiplier = 1f;
        private ArenaDamageable preferredAITarget;
        private float nextSupportPulseTime;
        private const float SupportPulseInterval = 0.25f;
        private bool isCloaked;
        private Coroutine abilityEffect;
        private Coroutine ultimateEffect;
        private Coroutine cloakTransition;
        private Renderer[] cloakedRenderers;
        private bool[] rendererStates;
        private Renderer cloakVisual;

        public bool IsCloaked => isCloaked;
        public bool IsAbilityLocked { get; private set; }
        public float UltimateCharge => ultimateCharge;
        public float UltimateChargeRequired => ultimateChargeRequired;
        public float AbilityCooldownRemaining => Mathf.Max(0f, nextAbilityTime - Time.time);
        public float UltimateCooldownRemaining => Mathf.Max(0f, nextUltimateTime - Time.time);
        public float AbilityActiveRemaining => Mathf.Max(0f, abilityActiveUntil - Time.time);
        public float UltimateActiveRemaining => Mathf.Max(0f, ultimateActiveUntil - Time.time);
        public bool IsUltimateActive => UltimateActiveRemaining > 0f;
        public bool IsAbilityReady => AbilityCooldownRemaining <= 0f && !IsAbilityLocked &&
                                      damageable != null && !damageable.IsDead;
        public bool IsUltimateReady => UltimateCooldownRemaining <= 0f &&
                                       (ultimateChargeRequired <= 0f || ultimateCharge >= ultimateChargeRequired) &&
                                       !IsAbilityLocked && damageable != null && !damageable.IsDead;
        public bool IsAbilityActive => Time.time < abilityActiveUntil;

        public event Action<RoboClashRobotAbilities> StateChanged;
        public event Action AbilityActivated;
        public event Action UltimateActivated;

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            movement = GetComponent<RoboClashRobotMovement>();
            combat = GetComponent<RoboClashRobotCombat>();
            animator = GetComponent<RoboClashRobotAnimator>();
            visualEffects = GetComponent<RoboClashRobotVFX>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            damageable = GetComponent<ArenaDamageable>();
            abilityCooldown = definition.abilityCooldown;
            ultimateCooldown = definition.ultimateCooldown;
            ConfigureVisuals();
        }

        public void ConfigureVisuals()
        {
            if (definition == null)
                definition = GetComponent<RoboClashRobotDefinition>();
            if (definition.modelRoot != null)
            {
                cloakedRenderers = definition.modelRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var item in cloakedRenderers)
                {
                    if (item.gameObject.name == "CloakVisual")
                        cloakVisual = item;
                }
                rendererStates = new bool[cloakedRenderers.Length];
                for (var i = 0; i < cloakedRenderers.Length; i++)
                    rendererStates[i] = cloakedRenderers[i] != null && cloakedRenderers[i].enabled;
            }
        }

        private void Update()
        {
            if (damageable == null || damageable.IsDead)
                return;

            if (definition.robotType == RoboClashRobotType.Spark && Time.time >= nextSupportPulseTime)
            {
                SupportAura();
                nextSupportPulseTime = Time.time + SupportPulseInterval;
            }

            movement.ControlsLocked = Time.time < empLockEndTime;
            IsAbilityLocked = movement.ControlsLocked;
            if (!movement.PlayerControlled)
                return;

            var abilityPressed = movement.InputSource.AbilityPressed;
            var ultimatePressed = movement.InputSource.UltimatePressed;
            if (movement.ControlsLocked)
                return;

            if (abilityPressed)
                ActivateAbility();
            if (ultimatePressed)
                ActivateUltimate();
        }

        private void LateUpdate()
        {
            if (damageable == null || damageable.IsDead)
                return;

            if (isCloaked && combat != null && combat.IsFiring)
                SetCloaked(false);

        }

        public void ActivateAbility()
        {
            if (Time.time < nextAbilityTime || damageable.IsDead || movement.ControlsLocked)
                return;

            if (definition.robotType == RoboClashRobotType.Volt)
            {
                var dashDirection = movement.PlayerControlled && movement.PlayerCamera != null
                    ? movement.PlayerCamera.transform.forward
                    : transform.forward;
                var dashStart = transform.position;
                if (!movement.TryDash(dashDirection, 5f, out var dashEnd))
                    return;
                visualEffects?.PlayDash(dashStart, dashEnd, dashDirection);
                animator?.TriggerAbilityPose(0.35f);
                nextAbilityTime = Time.time + abilityCooldown;
                abilityActiveUntil = Time.time + definition.abilityDuration;
                StateChanged?.Invoke(this);
                AbilityActivated?.Invoke();
                return;
            }

            nextAbilityTime = Time.time + abilityCooldown;
            abilityActiveUntil = Time.time + definition.abilityDuration;
            visualEffects?.PlayAbility();
            animator?.TriggerAbilityPose(definition.abilityDuration);
            switch (definition.robotType)
            {
                case RoboClashRobotType.Titan:
                    StartAbilityEffect(DefensiveField(definition.abilityDuration, 0.55f));
                    break;
                case RoboClashRobotType.Shadow:
                    StartAbilityEffect(Cloak(definition.abilityDuration));
                    break;
                case RoboClashRobotType.Spark:
                    StartAbilityEffect(RepairDronePulse(definition.abilityDuration));
                    break;
            }
            StateChanged?.Invoke(this);
            AbilityActivated?.Invoke();
        }

        public void ActivateUltimate()
        {
            if (Time.time < nextUltimateTime || damageable.IsDead || movement.ControlsLocked ||
                (ultimateChargeRequired > 0f && ultimateCharge < ultimateChargeRequired))
                return;

            if (ultimateChargeRequired > 0f)
                ultimateCharge = 0f;
            nextUltimateTime = Time.time + ultimateCooldown;
            ultimateActiveUntil = Time.time + Mathf.Max(0f, definition.ultimateDuration);
            StateChanged?.Invoke(this);
            UltimateActivated?.Invoke();
            visualEffects?.PlayUltimate();
            animator?.TriggerAbilityPose(0.8f);

            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    StartUltimateEffect(Overdrive(definition.ultimateDuration));
                    break;
                case RoboClashRobotType.Titan:
                    StartUltimateEffect(RailCannonCharge());
                    break;
                case RoboClashRobotType.Shadow:
                    ShadowStrike();
                    break;
                case RoboClashRobotType.Spark:
                    EmpBlast();
                    break;
            }
        }

        public void AddUltimateCharge(float amount)
        {
            if (amount > 0f)
            {
                var previousCharge = ultimateCharge;
                ultimateCharge = Mathf.Min(ultimateChargeRequired, ultimateCharge + amount);
                if (!Mathf.Approximately(previousCharge, ultimateCharge))
                    StateChanged?.Invoke(this);
            }
        }

        public void SetAITarget(ArenaDamageable target)
        {
            preferredAITarget = target != null && !target.IsDead &&
                                target.GetComponent<RoboClashRobotDefinition>() is RoboClashRobotDefinition targetDefinition &&
                                targetDefinition.teamId != definition.teamId
                ? target
                : null;
        }

        public float GetDamageMultiplier()
        {
            var fieldMultiplier = definition.robotType == RoboClashRobotType.Titan
                ? temporaryDamageMultiplier
                : 1f;
            return Mathf.Clamp(fieldMultiplier, 0.45f, 1f);
        }

        public float GetMoveSpeedMultiplier()
        {
            var overdrive = definition.robotType == RoboClashRobotType.Volt && Time.time < overdriveEndTime ? 1.3f : 1f;
            var empSlow = Time.time < empSlowEndTime ? empSlowMultiplier : 1f;
            return overdrive * empSlow;
        }

        public float GetFireRateMultiplier()
        {
            return definition.robotType == RoboClashRobotType.Volt && Time.time < overdriveEndTime ? 1.35f : 1f;
        }

        public void ApplyEmp(float duration, float slowMultiplier)
        {
            empLockEndTime = Mathf.Max(empLockEndTime, Time.time + Mathf.Min(duration, 1.25f));
            empSlowEndTime = Mathf.Max(empSlowEndTime, Time.time + duration);
            empSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.2f, 1f);
        }

        public void CancelAll()
        {
            StopActiveEffect();
            temporaryDamageMultiplier = 1f;
            abilityActiveUntil = 0f;
            ultimateActiveUntil = 0f;
            overdriveEndTime = 0f;
            empLockEndTime = 0f;
            empSlowEndTime = 0f;
            IsAbilityLocked = false;
            if (movement != null)
                movement.ControlsLocked = false;
            empSlowMultiplier = 1f;
            visualEffects?.StopAllEffects();
        }

        private IEnumerator DefensiveField(float duration, float damageMultiplier)
        {
            temporaryDamageMultiplier = Mathf.Clamp(damageMultiplier, 0.2f, 1f);
            visualEffects?.SetShieldField(true);
            yield return new WaitForSeconds(duration);
            temporaryDamageMultiplier = 1f;
            visualEffects?.SetShieldField(false);
            abilityEffect = null;
        }

        private IEnumerator Cloak(float duration)
        {
            SetCloaked(true);
            yield return new WaitForSeconds(duration);
            SetCloaked(false);
            abilityEffect = null;
        }

        private IEnumerator Overdrive(float duration)
        {
            overdriveEndTime = Time.time + duration;
            visualEffects?.SetOverdrive(true);
            yield return new WaitForSeconds(duration);
            overdriveEndTime = 0f;
            visualEffects?.SetOverdrive(false);
            ultimateEffect = null;
        }

        private IEnumerator RepairDronePulse(float duration)
        {
            visualEffects?.SetRepairDroneDeployed(true);
            visualEffects?.SetRepairTarget(transform);
            var endTime = Time.time + duration;
            var nextVisualPulse = 0f;
            while (Time.time < endTime && !damageable.IsDead)
            {
                damageable.Heal(4f * Time.deltaTime);
                damageable.AddShield(5f * Time.deltaTime);
                if (Time.time >= nextVisualPulse)
                {
                    RoboClashVFXKit.PlayHealPulse(transform.position + Vector3.up, definition.emissionColor);
                    nextVisualPulse = Time.time + 0.45f;
                }
                yield return null;
            }

            visualEffects?.SetRepairTarget(null);
            visualEffects?.SetRepairDroneDeployed(false);
            abilityEffect = null;
        }

        private System.Collections.IEnumerator RailCannonCharge()
        {
            animator?.TriggerAbilityPose(0.9f);
            var chargePoint = definition.weaponMount != null
                ? definition.weaponMount.position
                : transform.position + Vector3.up * 1.4f;
            var chargeDirection = definition.weaponMount != null ? definition.weaponMount.forward : transform.forward;
            RoboClashCombatVFX.PlayBurst(chargePoint, chargeDirection, definition.emissionColor, 0.6f, true);
            var chargeVisual = RoboClashVFXKit.PlayRailCharge(
                definition.muzzle, definition.emissionColor, 0.7f);
            yield return new WaitForSeconds(0.7f);
            if (!damageable.IsDead)
                FireRailCannon();
            if (chargeVisual != null)
                Destroy(chargeVisual);
            ultimateEffect = null;
        }

        private void FireRailCannon()
        {
            if (definition.muzzle == null)
                return;

            var forward = movement.PlayerCamera != null
                ? movement.PlayerCamera.transform.forward
                : transform.forward;
            var start = definition.muzzle.position;
            var thirdPerson = movement.PlayerCamera != null && movement.IsThirdPerson;
            var rayOrigin = thirdPerson ? movement.GetCameraAimRayOrigin() : start;
            var end = thirdPerson
                ? movement.GetCameraAimPoint(100f)
                : rayOrigin + forward * 100f;
            if (!thirdPerson)
            {
                var nearestDistance = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(rayOrigin, forward, 100f,
                             Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null || hit.collider.isTrigger ||
                        hit.transform.root == transform.root ||
                        hit.collider.GetComponentInParent<ArenaProjectile>() != null ||
                        hit.distance >= nearestDistance)
                        continue;
                    nearestDistance = hit.distance;
                    end = hit.point;
                }
            }

            RoboClashVFXKit.PlayRailBeam(start, end, definition.emissionColor);
            movement.AddCameraKick(3f, 0.2f);
            var projectileDirection = end - start;
            if (projectileDirection.sqrMagnitude < 4f ||
                Vector3.Dot(projectileDirection, forward) < 0f)
            {
                projectileDirection = forward;
                end = start + forward * 100f;
            }
            FireAbilityProjectile(projectileDirection.normalized, 65f, 90f, definition.damageRadius, 12f,
                RoboClashProjectileStyle.RailSlug);
        }

        private void ShadowStrike()
        {
            if (abilityEffect != null)
            {
                StopCoroutine(abilityEffect);
                abilityEffect = null;
            }
            SetCloaked(false);

            RoboClashRobotDefinition target = preferredAITarget != null &&
                                              preferredAITarget.isActiveAndEnabled &&
                                              !preferredAITarget.IsDead
                ? preferredAITarget.GetComponent<RoboClashRobotDefinition>()
                : null;
            var nearestDistance = float.PositiveInfinity;
            if (target == null)
            {
                foreach (var candidate in FindObjectsByType<RoboClashRobotDefinition>())
                {
                    var candidateDamageable = candidate.GetComponent<ArenaDamageable>();
                    if (candidate == definition || candidate.teamId == definition.teamId ||
                        !candidate.isActiveAndEnabled || candidateDamageable == null ||
                        candidateDamageable.IsDead)
                        continue;
                    var candidateDistance = Vector3.Distance(transform.position, candidate.transform.position);
                    if (candidateDistance < nearestDistance)
                    {
                        target = candidate;
                        nearestDistance = candidateDistance;
                    }
                }
            }

            var destination = transform.position + transform.forward * 5f;
            if (target != null)
            {
                var behindTarget = target.transform.position - target.transform.forward * 2f;
                var direction = behindTarget - transform.position;
                var distance = direction.magnitude;
                var blocked = false;
                if (distance > 0.1f)
                {
                    foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * 0.8f,
                                 0.35f, direction.normalized, distance, Physics.DefaultRaycastLayers,
                                 QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.root == transform.root ||
                            hit.collider.GetComponentInParent<ArenaDamageable>() != null)
                            continue;
                        blocked = true;
                        break;
                    }

                    if (!blocked)
                        destination = behindTarget;
                }
            }

            var previousPosition = transform.position;
            if (movement.TryTeleport(destination))
            {
                transform.rotation = Quaternion.LookRotation(target != null
                    ? target.transform.position - transform.position
                    : transform.forward, Vector3.up);
                visualEffects?.PlayTeleport(previousPosition, transform.position, transform.forward);
            }

            var aim = target != null
                ? (target.transform.position + Vector3.up - definition.muzzle.position).normalized
                : transform.forward;
            if (movement.PlayerControlled && movement.PlayerCamera != null &&
                movement.IsThirdPerson)
                aim = (movement.GetCameraAimPoint(100f) - definition.muzzle.position).normalized;
            FireAbilityProjectile(aim, 48f, 70f, 0f, 14f,
                RoboClashProjectileStyle.ShadowBlade);
        }

        private void EmpBlast()
        {
            const float radius = 8f;
            RoboClashVFXKit.PlayEmpBlast(transform.position + Vector3.up,
                definition.emissionColor, radius);
            foreach (var candidate in FindObjectsByType<RoboClashRobotDefinition>())
            {
                var candidateVitals = candidate.GetComponent<ArenaDamageable>();
                if (candidate == definition || candidate.teamId == definition.teamId ||
                    !candidate.isActiveAndEnabled || candidateVitals == null || candidateVitals.IsDead ||
                    Vector3.Distance(transform.position, candidate.transform.position) > radius)
                    continue;

                candidate.GetComponent<RoboClashRobotAbilities>()?.ApplyEmp(3f, 0.55f);
                candidate.GetComponent<ArenaBotController>()?.ApplyDisruption(3f, 0.55f);
                candidate.GetComponent<RoboClashRobotVFX>()?.PlayEmpDisruption();
            }
        }

        private void FireAbilityProjectile(Vector3 direction, float speed, float damage, float radius,
            float knockback, RoboClashProjectileStyle style = RoboClashProjectileStyle.Pulse)
        {
            if (definition.muzzle == null)
                return;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = definition.ultimateName;
            projectile.transform.position = definition.muzzle.position;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
            projectile.transform.localScale = Vector3.one * 0.28f;
            projectile.AddComponent<Rigidbody>();
            var arenaProjectile = projectile.AddComponent<ArenaProjectile>();
            arenaProjectile.ConfigureStyle(style, definition.emissionColor);
            arenaProjectile.Launch(gameObject, direction, speed, damage, 3f, radius, knockback);
        }

        private void SupportAura()
        {
            damageable.AddShield(2.5f * SupportPulseInterval);
            foreach (var ally in FindObjectsByType<RoboClashRobotDefinition>())
            {
                if (ally == definition || ally.teamId != definition.teamId ||
                    Vector3.Distance(transform.position, ally.transform.position) > supportRange)
                    continue;
                ally.GetComponent<ArenaDamageable>()?.AddShield(supportShieldPerSecond * SupportPulseInterval);
            }
        }

        private void SetCloaked(bool cloaked)
        {
            if (isCloaked == cloaked && cloakTransition == null)
                return;

            isCloaked = cloaked;
            visualEffects?.PlayCloakTransition(cloaked);
            if (cloakTransition != null)
                StopCoroutine(cloakTransition);

            if (!isActiveAndEnabled)
            {
                ApplyCloakState(cloaked);
                cloakTransition = null;
                return;
            }

            cloakTransition = StartCoroutine(AnimateCloak(cloaked));
        }

        private IEnumerator AnimateCloak(bool cloaked)
        {
            if (cloakVisual != null)
                cloakVisual.enabled = cloaked;
            if (cloakedRenderers != null)
            {
                for (var i = 0; i < cloakedRenderers.Length; i++)
                {
                    var renderer = cloakedRenderers[i];
                    if (renderer == null || renderer == cloakVisual)
                        continue;
                    renderer.enabled = cloaked ? false : rendererStates[i];
                    yield return new WaitForSeconds(0.012f);
                }
            }

            cloakTransition = null;
        }

        private void ApplyCloakState(bool cloaked)
        {
            if (cloakedRenderers != null)
            {
                for (var i = 0; i < cloakedRenderers.Length; i++)
                {
                    var renderer = cloakedRenderers[i];
                    if (renderer != null && renderer != cloakVisual)
                        renderer.enabled = cloaked ? false : rendererStates[i];
                }
            }

            if (cloakVisual != null)
                cloakVisual.enabled = cloaked;
        }

        private void StartAbilityEffect(IEnumerator effect)
        {
            StopAbilityEffect();
            abilityEffect = StartCoroutine(effect);
        }

        private void StartUltimateEffect(IEnumerator effect)
        {
            StopUltimateEffect();
            ultimateEffect = StartCoroutine(effect);
        }

        private void StopAbilityEffect()
        {
            if (abilityEffect != null)
                StopCoroutine(abilityEffect);
            abilityEffect = null;
            temporaryDamageMultiplier = 1f;
            visualEffects?.SetShieldField(false);
            visualEffects?.SetRepairTarget(null);
            visualEffects?.SetRepairDroneDeployed(false);
            animator?.SetAbilityPose(false);
            SetCloaked(false);
        }

        private void StopUltimateEffect()
        {
            if (ultimateEffect != null)
                StopCoroutine(ultimateEffect);
            ultimateEffect = null;
            overdriveEndTime = 0f;
            visualEffects?.SetOverdrive(false);
        }

        private void StopActiveEffect()
        {
            StopAbilityEffect();
            StopUltimateEffect();
        }

        private void OnDisable()
        {
            CancelAll();
        }
    }
}
