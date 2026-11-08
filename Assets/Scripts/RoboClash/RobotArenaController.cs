using System;
using System.Collections;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RobotArenaController : ArenaDamageable
    {
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.8f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ArenaSpawnZone spawnZone;
        [SerializeField] private float weaponDamage = 20f;
        [SerializeField] private float projectileSpeed = 32f;
        [SerializeField] private float fireInterval = 0.4f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 12f;
        private float nextFireTime;
        private bool dead;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContactThisMove;
        private RoboClashRobotDefinition robotDefinition;
        private RoboClashRobotMovement robotMovement;
        private RoboClashRobotCombat robotCombat;
        private RoboClashRobotAbilities robotAbilities;
        private RoboClashRobotAnimator robotAnimator;
        private RoboClashRobotAudio robotAudio;
        private RoboClashRobotVFX robotVfx;
        private RoboClashPlayerInputRouter inputRouter;
        private KeyboardMouseInputSource fallbackInputSource;
        private int respawnsRemaining;
        private bool unlimitedMatchRespawns;
        private Coroutine respawnCoroutine;
        private bool matchRespawnsConfigured;
        private bool hasMatchRespawnLocation;
        private Vector3 matchRespawnPosition;
        private Quaternion matchRespawnRotation = Quaternion.identity;

        public Camera PlayerCamera => playerCamera;
        public ArenaSpawnZone SpawnZone => spawnZone;
        public bool IsRespawning { get; private set; }
        public bool HasPlayerControl => robotMovement != null
            ? robotMovement.PlayerControlled
            : playerCamera != null;

        public event Action HitConfirmed;
        public event Action<RobotArenaController> Respawned;

        protected override void Awake()
        {
            base.Awake();
            characterController = GetComponent<CharacterController>();
            robotDefinition = GetComponent<RoboClashRobotDefinition>();
            robotMovement = GetComponent<RoboClashRobotMovement>();
            robotCombat = GetComponent<RoboClashRobotCombat>();
            robotAbilities = GetComponent<RoboClashRobotAbilities>();
            robotAnimator = GetComponent<RoboClashRobotAnimator>();
            robotAudio = GetComponent<RoboClashRobotAudio>();
            robotVfx = GetComponent<RoboClashRobotVFX>();
            inputRouter = GetComponent<RoboClashPlayerInputRouter>();
            if (robotDefinition != null)
                ConfigureVitals(robotDefinition.stats.maxHealth, robotDefinition.stats.maxShield);

            if (playerCamera == null && robotMovement == null)
                playerCamera = Camera.main;

            yaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            if (IsGameplayScene() && playerCamera != null && robotMovement == null &&
                CanControlCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Configure(Camera camera, Transform weaponMuzzle, ArenaSpawnZone startingZone)
        {
            if (robotDefinition == null)
                robotDefinition = GetComponent<RoboClashRobotDefinition>();
            if (robotMovement == null)
                robotMovement = GetComponent<RoboClashRobotMovement>();
            if (robotCombat == null)
                robotCombat = GetComponent<RoboClashRobotCombat>();

            playerCamera = camera;
            muzzle = weaponMuzzle != null ? weaponMuzzle : robotDefinition != null ? robotDefinition.muzzle : null;
            spawnZone = startingZone;
            if (robotMovement != null)
                robotMovement.ConfigurePlayer(camera, camera != null);
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(camera != null);
            robotAudio?.SetLocalPlayer(camera != null);
        }

        public void DisablePlayerControl()
        {
            playerCamera = null;
            if (robotMovement != null)
                robotMovement.ConfigurePlayer(null, false);
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(false);
        }

        public void ConfigureMatchRespawns(int maximumRespawns)
        {
            matchRespawnsConfigured = true;
            unlimitedMatchRespawns = false;
            respawnsRemaining = Mathf.Max(0, maximumRespawns);
        }

        public void ConfigureUnlimitedMatchRespawns()
        {
            matchRespawnsConfigured = true;
            unlimitedMatchRespawns = true;
            respawnsRemaining = 0;
        }

        public void ConfigureMatchRespawnLocation(Vector3 position, Quaternion rotation)
        {
            hasMatchRespawnLocation = true;
            matchRespawnPosition = position;
            matchRespawnRotation = rotation;
        }

        public void LockForMatchEnd()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            IsRespawning = false;
            if (robotMovement != null)
            {
                robotMovement.SetControlled(false);
                robotMovement.SetMatchInputLocked(true);
                robotMovement.SetCameraDisabled();
            }
            if (robotCombat != null)
            {
                robotCombat.ConfigurePlayer(false);
                robotCombat.Cancel();
            }
            if (robotAbilities != null)
                robotAbilities.CancelAll();
        }

        public void RegisterHit()
        {
            HitConfirmed?.Invoke();
        }

        private void Update()
        {
            if (!IsGameplayScene() || dead || playerCamera == null)
                return;

            if (robotMovement != null)
                return;

            var inputSource = InputSource;
            if (inputSource is KeyboardMouseInputSource keyboardInput && CanControlCursor &&
                keyboardInput.CursorUnlockPressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (inputSource is KeyboardMouseInputSource captureInput && CanControlCursor &&
                     captureInput.CursorCapturePressed && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (inputSource is KeyboardMouseInputSource && Cursor.lockState != CursorLockMode.Locked)
                return;

            Look(inputSource);
            Move(inputSource);

            if (inputSource.FireHeld && Time.time >= nextFireTime)
                Fire();
        }

        private void LateUpdate()
        {
            if (robotMovement != null)
                return;

            if (playerCamera == null || dead)
                return;

            var lookRotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = transform.position + Vector3.up * 1.35f;
            playerCamera.transform.position = focus - lookRotation * Vector3.forward * 7f;
            playerCamera.transform.rotation = lookRotation;
        }

        private bool IsGameplayScene()
        {
            return RoboClashSceneUtility.IsGameplayScene(gameObject.scene);
        }

        private IRoboClashInputSource InputSource
        {
            get
            {
                if (inputRouter == null)
                    inputRouter = GetComponent<RoboClashPlayerInputRouter>();
                if (inputRouter != null && inputRouter.Source != null)
                    return inputRouter.Source;
                if (fallbackInputSource == null)
                    fallbackInputSource = new KeyboardMouseInputSource(mouseSensitivity);
                return fallbackInputSource;
            }
        }

        private bool CanControlCursor
        {
            get
            {
                if (inputRouter == null)
                    inputRouter = GetComponent<RoboClashPlayerInputRouter>();
                return inputRouter == null || inputRouter.PlayerIndex == 0;
            }
        }

        private void Look(IRoboClashInputSource inputSource)
        {
            var delta = inputSource.LookDelta;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -10f, 55f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void Move(IRoboClashInputSource inputSource)
        {
            var input = inputSource.Move;

            var motion = (transform.right * input.x + transform.forward * input.y).normalized * moveSpeed;
            if (characterController.isGrounded)
            {
                verticalVelocity = -1f;
                if (inputSource.JumpPressed)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            motion.y = verticalVelocity;
            platformContactThisMove = null;
            var platformMotion = currentPlatform != null && characterController.isGrounded
                ? currentPlatform.Velocity * Time.deltaTime
                : Vector3.zero;
            characterController.Move(motion * Time.deltaTime + platformMotion);
            currentPlatform = platformContactThisMove;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContactThisMove = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }

        private void Fire()
        {
            if (muzzle == null)
                return;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Energy Bolt";
            projectile.transform.position = muzzle.position;
            projectile.transform.localScale = Vector3.one * 0.22f;
            var collider = projectile.GetComponent<SphereCollider>();
            collider.isTrigger = false;
            projectile.AddComponent<Rigidbody>();
            var behavior = projectile.AddComponent<ArenaProjectile>();
            behavior.Launch(gameObject, playerCamera.transform.forward, projectileSpeed, weaponDamage);
            nextFireTime = Time.time + fireInterval;
        }

        public void EquipWeapon(float damage, float speed, float interval)
        {
            weaponDamage = Mathf.Max(1f, damage);
            projectileSpeed = Mathf.Max(1f, speed);
            fireInterval = Mathf.Max(0.05f, interval);
            if (robotDefinition != null)
            {
                robotDefinition.stats.damagePerShot = weaponDamage;
                robotDefinition.projectileSpeed = projectileSpeed;
                robotDefinition.stats.shotsPerSecond = 1f / fireInterval;
            }
        }

        public override void TakeDamage(float amount)
        {
            ApplyIncomingDamage(amount, null, false);
        }

        public override void TakeDamage(float amount, ArenaDamageable instigator)
        {
            ApplyIncomingDamage(amount, instigator, true);
        }

        private void ApplyIncomingDamage(float amount, ArenaDamageable instigator, bool playFeedback)
        {
            if (dead)
                return;

            var shieldBeforeDamage = Shield;
            var originalDamage = amount;
            if (robotDefinition != null)
            {
                var multiplier = robotDefinition.stats.incomingDamageMultiplier;
                if (robotAbilities != null)
                    multiplier *= robotAbilities.GetDamageMultiplier();
                amount *= Mathf.Clamp(multiplier, 0.45f, 1f);
            }

            base.TakeDamage(amount, instigator);
            if (amount > 0f && !dead && playFeedback)
            {
                robotAudio?.Play(shieldBeforeDamage > 0f
                    ? RoboClashAudioEvent.ShieldImpact
                    : RoboClashAudioEvent.Damage);
                var reactionScale = robotDefinition != null &&
                                    robotDefinition.stats.maxHealth > 0f
                    ? amount / robotDefinition.stats.maxHealth
                    : 0.45f;
                robotAnimator?.TriggerHitReaction(Mathf.Clamp(reactionScale * 1.5f, 0.2f, 0.8f));
                robotMovement?.AddCameraKick(0f, Mathf.Clamp01(amount / 40f));
            }

            if (robotAbilities != null && robotDefinition != null)
                robotAbilities.AddUltimateCharge(originalDamage * robotDefinition.stats.ultimateChargePerDamage);
        }

        protected override void OnDeath()
        {
            dead = true;
            if (robotMovement != null)
            {
                robotMovement.SetControlled(false);
                robotMovement.SetCameraDead();
            }
            if (robotCombat != null)
            {
                robotCombat.ConfigurePlayer(false);
                robotCombat.Cancel();
            }
            if (robotAbilities != null)
                robotAbilities.CancelAll();
            robotAnimator?.SetDead(true);
            robotVfx?.StopAllEffects();
            if (robotDefinition != null)
            {
                var center = transform.position + Vector3.up;
                RoboClashCombatVFX.PlayBurst(center, Vector3.up,
                    robotDefinition.emissionColor, 1.6f, true);
                RoboClashCombatVFX.PlayPulse(center, robotDefinition.emissionColor, 5f);
            }
            characterController.enabled = false;
            if (!matchRespawnsConfigured || unlimitedMatchRespawns || respawnsRemaining > 0)
            {
                if (matchRespawnsConfigured && !unlimitedMatchRespawns)
                    respawnsRemaining--;
                IsRespawning = true;
                respawnCoroutine = StartCoroutine(Respawn());
            }
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(2f);
            respawnCoroutine = null;
            var matchManager = RoboClashMatchManager.Active;
            if (matchManager != null &&
                matchManager.TryGetRespawnLocation(this, out var plannedPosition, out var plannedRotation))
            {
                transform.SetPositionAndRotation(plannedPosition, plannedRotation);
                yaw = plannedRotation.eulerAngles.y;
                pitch = 12f;
            }
            else if (hasMatchRespawnLocation)
            {
                transform.SetPositionAndRotation(matchRespawnPosition, matchRespawnRotation);
                yaw = matchRespawnRotation.eulerAngles.y;
                pitch = 12f;
            }
            else if (spawnZone != null)
            {
                transform.SetPositionAndRotation(spawnZone.transform.position + Vector3.up * 0.35f,
                    spawnZone.transform.rotation);
                yaw = spawnZone.transform.eulerAngles.y;
                pitch = 12f;
            }

            Restore(MaximumHealth, robotDefinition != null ? robotDefinition.stats.maxShield : 0f);
            GrantSpawnProtection(2f);
            verticalVelocity = 0f;
            dead = false;
            IsRespawning = false;
            characterController.enabled = true;
            robotAnimator?.SetDead(false);
            robotVfx?.PlayRespawn();
            var inputRouter = GetComponent<RoboClashPlayerInputRouter>();
            var isHumanPlayer = inputRouter != null && inputRouter.PlayerIndex >= 0;
            if (robotMovement != null)
            {
                robotMovement.ResetMotion();
                robotMovement.SetMatchInputLocked(false);
                robotMovement.SetControlled(isHumanPlayer);
                if (playerCamera != null)
                    robotMovement.BeginRespawnCameraTransition();
                else if (isHumanPlayer)
                    robotMovement.ConfigurePlayer(null, true);
            }
            if (robotCombat != null)
                robotCombat.ConfigurePlayer(isHumanPlayer);
            Respawned?.Invoke(this);
        }

    }
}
