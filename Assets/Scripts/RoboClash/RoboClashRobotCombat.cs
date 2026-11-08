using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement))]
    public sealed class RoboClashRobotCombat : MonoBehaviour
    {
        [SerializeField] private bool playerControlled;
        [SerializeField, Min(0f)] private float knockback = 6f;
        [SerializeField, Min(0.1f)] private int maximumActiveProjectiles = 32;
        [SerializeField] private Transform recoilTransform;
        [SerializeField] private Renderer muzzleFlash;

        private RoboClashRobotDefinition definition;
        private RoboClashRobotMovement movement;
        private RoboClashRobotAnimator animator;
        private RoboClashRobotVFX visualEffects;
        private RoboClashRobotAudio audioPlayer;
        private ArenaDamageable damageable;
        private readonly HashSet<ArenaProjectile> activeProjectiles = new HashSet<ArenaProjectile>();
        private float nextShotTime;
        private float reloadReadyTime;
        private float reloadDuration;
        private float recoil;
        private Vector3 recoilRestPosition;
        private float sustainedMovement;
        private float stoppedMovement;
        private float movementReloadBonus;
        private float firingUntil;
        private float damageBuffMultiplier = 1f;
        private float damageBuffEndTime;
        private int ammunition;
        private bool firing;

        public bool FireBlocked { get; set; }
        public int Ammunition => ammunition;
        public int MagazineCapacity => definition != null && definition.stats != null
            ? definition.stats.magazineSize
            : 0;
        public bool IsReloading => Time.time < reloadReadyTime;
        public float ReloadRemaining => Mathf.Max(0f, reloadReadyTime - Time.time);
        public float ReloadProgress => reloadDuration > 0f
            ? 1f - Mathf.Clamp01(ReloadRemaining / reloadDuration)
            : 1f;
        public bool IsFiring => firing;
        public float DamageBuffMultiplier => Time.time < damageBuffEndTime
            ? damageBuffMultiplier
            : 1f;

        public event System.Action<RoboClashRobotCombat> StateChanged;

        public void ApplyDamageBuff(float multiplier, float duration)
        {
            damageBuffMultiplier = Mathf.Max(1f, multiplier);
            damageBuffEndTime = Time.time + Mathf.Max(0f, duration);
            StateChanged?.Invoke(this);
        }

        private void Awake()
        {
            definition = GetComponent<RoboClashRobotDefinition>();
            movement = GetComponent<RoboClashRobotMovement>();
            animator = GetComponent<RoboClashRobotAnimator>();
            visualEffects = GetComponent<RoboClashRobotVFX>();
            audioPlayer = GetComponent<RoboClashRobotAudio>();
            damageable = GetComponent<ArenaDamageable>();
            ammunition = definition.stats.magazineSize;
            if (recoilTransform != null)
                recoilRestPosition = recoilTransform.localPosition;
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void ConfigurePlayer(bool enabledForPlayer)
        {
            playerControlled = enabledForPlayer;
            if (!enabledForPlayer)
                firing = false;
        }

        public void SetVisualParts(Transform weapon, Renderer flash)
        {
            recoilTransform = weapon;
            muzzleFlash = flash;
            recoilRestPosition = recoilTransform != null ? recoilTransform.localPosition : Vector3.zero;
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        public void NotifyMovement(float speed, float maximumSpeed)
        {
            if (definition.robotType != RoboClashRobotType.Volt)
                return;

            if (speed >= maximumSpeed * 0.75f)
            {
                sustainedMovement += Time.deltaTime;
                stoppedMovement = 0f;
                movementReloadBonus = Mathf.Min(0.25f, sustainedMovement / 12f);
            }
            else
            {
                stoppedMovement += Time.deltaTime;
                if (stoppedMovement >= 1.5f)
                {
                    sustainedMovement = 0f;
                    movementReloadBonus = 0f;
                }
            }
        }

        public bool Fire()
        {
            var playerCamera = movement.PlayerCamera;
            if (FireBlocked || damageable == null || damageable.IsDead ||
                movement.ControlsLocked || Time.time < nextShotTime || IsReloading ||
                ammunition <= 0 || definition.muzzle == null ||
                activeProjectiles.Count >= maximumActiveProjectiles)
                return false;

            var aimDirection = playerCamera != null
                ? playerCamera.transform.forward
                : movement.AimDirection;
            if (playerCamera != null && movement.IsThirdPerson)
            {
                var cameraPosition = playerCamera.transform.position;
                const float range = 100f;
                var aimPoint = movement.GetCameraAimPoint(range);

                var muzzlePosition = definition.muzzle.position;
                var toTarget = aimPoint - muzzlePosition;
                var targetTooClose = toTarget.sqrMagnitude < 4f ||
                                     Vector3.Dot(toTarget, aimDirection) < 0f;
                var shotDirection = targetTooClose ? aimDirection : toTarget.normalized;
                var targetPoint = targetTooClose
                    ? muzzlePosition + aimDirection * range
                    : aimPoint;
#if UNITY_EDITOR
                Debug.DrawLine(cameraPosition, aimPoint, Color.green, 0.1f);
                Debug.DrawLine(muzzlePosition, targetPoint, Color.red, 0.1f);
#endif
                return FireTowardPoint(targetPoint, shotDirection);
            }

            var aimOrigin = playerCamera != null
                ? playerCamera.transform.position
                : definition.muzzle.position;
            var aimTarget = aimOrigin + aimDirection * definition.stats.weaponRange;
            if (TryGetExternalHit(aimOrigin, aimDirection,
                    definition.stats.weaponRange, out var firstPersonAimHit))
                aimTarget = firstPersonAimHit.point;

            return FireTowardPoint(aimTarget, aimDirection);
        }

        public bool FireAt(Vector3 worldPosition)
        {
            if (definition.muzzle == null)
                return false;
            return FireTowardPoint(worldPosition,
                worldPosition - definition.muzzle.position);
        }

        private bool FireTowardPoint(Vector3 aimTarget, Vector3 fallbackDirection)
        {
            if (FireBlocked || damageable == null || damageable.IsDead || movement.ControlsLocked ||
                fallbackDirection.sqrMagnitude < 0.0001f ||
                Time.time < nextShotTime || IsReloading || ammunition <= 0 ||
                definition.muzzle == null ||
                activeProjectiles.Count >= maximumActiveProjectiles)
                return false;

            var muzzle = definition.muzzle.position;
            var direction = CalculateLaunchDirection(muzzle, aimTarget, fallbackDirection);

            if (TryGetExternalHit(muzzle, direction, definition.stats.weaponRange, out var obstruction))
                direction = CalculateLaunchDirection(muzzle, obstruction.point, direction);

            var speed = definition.projectileSpeed;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = definition.robotType == RoboClashRobotType.Spark ? "Spark Rocket" : definition.weaponName + " Bolt";
            projectile.transform.SetPositionAndRotation(muzzle, Quaternion.LookRotation(direction));
            projectile.transform.localScale = Vector3.one * (definition.robotType == RoboClashRobotType.Spark ? 0.3f : 0.16f);
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            var shot = projectile.AddComponent<ArenaProjectile>();
            shot.ConfigureStyle(GetProjectileStyle(definition.robotType), definition.emissionColor);
            shot.Launch(gameObject, direction, speed, definition.stats.damagePerShot * DamageBuffMultiplier,
                definition.stats.weaponRange / Mathf.Max(speed, 0.1f), definition.damageRadius, knockback);
            activeProjectiles.Add(shot);

            ammunition--;
            StateChanged?.Invoke(this);
            var abilities = GetComponent<RoboClashRobotAbilities>();
            var rateMultiplier = abilities != null ? abilities.GetFireRateMultiplier() : 1f;
            nextShotTime = Time.time + 1f / (definition.stats.shotsPerSecond * rateMultiplier);
            recoil = 1f;
            if (playerControlled)
                movement.AddCameraKick(
                    definition.robotType == RoboClashRobotType.Titan ? 0.25f :
                    definition.robotType == RoboClashRobotType.Spark ? 2.2f : 0.55f, 0f);
            firingUntil = Time.time + 0.1f;
            animator?.TriggerRecoil(definition.robotType == RoboClashRobotType.Titan ? 1f : 0.65f);
            audioPlayer?.Play(RoboClashAudioEvent.WeaponFire);
            visualEffects?.PlayMuzzle(muzzle, direction, definition.robotType == RoboClashRobotType.Titan);
            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = true;
                CancelInvoke(nameof(HideMuzzleFlash));
                Invoke(nameof(HideMuzzleFlash), 0.06f);
            }

            if (ammunition == 0)
                Reload();
            return true;
        }

        private static Vector3 CalculateLaunchDirection(
            Vector3 muzzlePosition, Vector3 targetPoint, Vector3 fallbackForward)
        {
            var direction = targetPoint - muzzlePosition;
            if (direction.sqrMagnitude < 0.0001f)
                direction = fallbackForward;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            return direction.normalized;
        }

        private static RoboClashProjectileStyle GetProjectileStyle(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Titan: return RoboClashProjectileStyle.Gatling;
                case RoboClashRobotType.Shadow: return RoboClashProjectileStyle.SmgDart;
                case RoboClashRobotType.Spark: return RoboClashProjectileStyle.Rocket;
                default: return RoboClashProjectileStyle.Pulse;
            }
        }

        public void Reload()
        {
            if (!IsReloading && ammunition < definition.stats.magazineSize)
            {
                var duration = definition.stats.reloadDuration * (1f - movementReloadBonus);
                reloadDuration = duration;
                reloadReadyTime = Time.time + duration;
                audioPlayer?.Play(RoboClashAudioEvent.ReloadStart);
                Invoke(nameof(FinishReload), duration);
                StateChanged?.Invoke(this);
            }
        }

        public void Cancel()
        {
            CancelInvoke();
            damageBuffEndTime = 0f;
            firing = false;
            firingUntil = 0f;
            recoil = 0f;
            reloadReadyTime = 0f;
            reloadDuration = 0f;
            nextShotTime = 0f;
            ammunition = definition.stats.magazineSize;
            sustainedMovement = 0f;
            stoppedMovement = 0f;
            movementReloadBonus = 0f;
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                    Destroy(projectile.gameObject);
            }
            activeProjectiles.Clear();
            StateChanged?.Invoke(this);
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
            if (definition.rotatingBarrels != null)
                definition.rotatingBarrels.localRotation = Quaternion.identity;
            if (recoilTransform != null)
                recoilTransform.localPosition = recoilRestPosition;
            visualEffects?.StopAllEffects();
        }

        private void Update()
        {
            if (damageable != null && damageable.IsDead)
            {
                firing = false;
                return;
            }

            firing = Time.time < firingUntil;
            if (playerControlled && !movement.ControlsLocked)
            {
                if (movement.InputSource.FireHeld)
                    Fire();

                if (movement.InputSource.ReloadPressed)
                    Reload();
            }
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 8f);
            if (recoilTransform != null)
                recoilTransform.localPosition = recoilRestPosition + Vector3.back * (0.06f * recoil);

            activeProjectiles.RemoveWhere(projectile => projectile == null);
            if (definition.robotType == RoboClashRobotType.Titan && firing &&
                definition.rotatingBarrels != null)
                definition.rotatingBarrels.Rotate(0f, 0f, 850f * Time.deltaTime, Space.Self);
        }

        private void FinishReload()
        {
            ammunition = definition.stats.magazineSize;
            reloadReadyTime = 0f;
            reloadDuration = 0f;
            audioPlayer?.Play(RoboClashAudioEvent.ReloadComplete);
            StateChanged?.Invoke(this);
        }

        private void HideMuzzleFlash()
        {
            if (muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        private bool TryGetExternalHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            closestHit = default;
            var closestDistance = float.PositiveInfinity;
            var hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger ||
                    hit.transform.root == transform.root ||
                    hit.collider.GetComponentInParent<ArenaProjectile>() != null ||
                    hit.distance >= closestDistance)
                    continue;

                closestHit = hit;
                closestDistance = hit.distance;
            }

            return closestDistance < float.PositiveInfinity;
        }
    }
}
