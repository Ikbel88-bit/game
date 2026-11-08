using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(CharacterController), typeof(ArenaDamageable))]
    public sealed class ArenaBotController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(1f)] private float engagementRange = 24f;
        [SerializeField, Min(0.1f)] private float fireInterval = 1.15f;
        [SerializeField, Min(1f)] private float projectileDamage = 8f;

        private CharacterController characterController;
        private ArenaDamageable damageable;
        private RobotArenaController target;
        private float verticalVelocity;
        private float nextShotTime;
        private ArenaMovingPlatform currentPlatform;
        private ArenaMovingPlatform platformContactThisMove;
        private float disruptedUntil;
        private float slowedUntil;
        private float disruptionSpeedMultiplier = 1f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            damageable = GetComponent<ArenaDamageable>();
        }

        private void Update()
        {
            if (target == null || !target.isActiveAndEnabled || target.IsDead || !target.HasPlayerControl)
                target = FindPlayerTarget();

            if (target == null || damageable.IsDead)
                return;

            var targetAbilities = target.GetComponent<RoboClashRobotAbilities>();
            if (targetAbilities != null && targetAbilities.IsCloaked)
                return;

            var offset = target.transform.position - transform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance < 0.01f)
                return;

            var direction = offset / distance;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 6f * Time.deltaTime);

            platformContactThisMove = null;
            var platformMotion = currentPlatform != null && characterController.isGrounded
                ? currentPlatform.Velocity * Time.deltaTime
                : Vector3.zero;
            if (Time.time < disruptedUntil)
                characterController.Move(platformMotion);
            else if (distance > 8f)
            {
                var speed = Time.time < slowedUntil ? disruptionSpeedMultiplier : 1f;
                characterController.Move(direction * (moveSpeed * speed * Time.deltaTime) + platformMotion);
            }
            else
                characterController.Move(platformMotion);

            if (characterController.isGrounded)
                verticalVelocity = -1f;
            else
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
            currentPlatform = platformContactThisMove;

            if (distance <= engagementRange && Time.time >= nextShotTime && Time.time >= disruptedUntil)
                FireAtTarget();
        }

        private RobotArenaController FindPlayerTarget()
        {
            foreach (var candidate in FindObjectsByType<RobotArenaController>())
            {
                if (candidate != null && candidate.isActiveAndEnabled &&
                    candidate.HasPlayerControl && !candidate.IsDead)
                    return candidate;
            }

            return null;
        }

        public void ApplyDisruption(float duration, float slowMultiplier)
        {
            disruptedUntil = Mathf.Max(disruptedUntil, Time.time + Mathf.Min(duration, 1.25f));
            slowedUntil = Mathf.Max(slowedUntil, Time.time + duration);
            disruptionSpeedMultiplier = Mathf.Clamp(slowMultiplier, 0.2f, 1f);
        }

        private void FireAtTarget()
        {
            var muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.7f;
            var destination = target.transform.position + Vector3.up * 1.2f;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Bot Energy Bolt";
            projectile.transform.position = muzzle;
            projectile.transform.localScale = Vector3.one * 0.18f;
            projectile.AddComponent<Rigidbody>();
            projectile.AddComponent<ArenaProjectile>().Launch(gameObject, destination - muzzle, 24f, projectileDamage);
            nextShotTime = Time.time + fireInterval;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
                platformContactThisMove = hit.collider.GetComponentInParent<ArenaMovingPlatform>();
        }
    }
}
