using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ArenaProjectile : MonoBehaviour
    {
        private float damage;
        private GameObject owner;
        private bool consumed;
        private float explosionRadius;
        private float knockback;
        private RoboClashProjectileStyle style = RoboClashProjectileStyle.Pulse;

        public bool IsOwnedBy(GameObject candidate)
        {
            return candidate != null && owner == candidate;
        }

        public void Cancel()
        {
            consumed = true;
            Destroy(gameObject);
        }

        public void Launch(GameObject firingOwner, Vector3 direction, float speed, float impactDamage,
            float lifetime = 4f, float areaRadius = 0f, float impactKnockback = 0f)
        {
            owner = firingOwner;
            damage = impactDamage;
            explosionRadius = Mathf.Max(0f, areaRadius);
            knockback = Mathf.Max(0f, impactKnockback);

            var body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            direction.Normalize();
            body.linearVelocity = direction * speed;

            var projectileCollider = GetComponent<Collider>();
            if (owner != null)
            {
                foreach (var ownerCollider in owner.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }

            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount > 0)
            {
                var contact = collision.GetContact(0);
                Hit(collision.collider, contact.point, contact.normal);
            }
            else
                Hit(collision.collider, transform.position, -transform.forward);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger && other.GetComponentInParent<ArenaEnergyShield>() == null)
                return;

            Hit(other, transform.position, -transform.forward);
        }

        public void ConfigureVisual(Color color, bool rocket)
        {
            RoboClashCombatVFX.ConfigureProjectileVisual(gameObject, color, rocket);
        }

        public void ConfigureStyle(RoboClashProjectileStyle value, Color color)
        {
            style = value;
            RoboClashProjectileVisuals.Apply(gameObject, value, color);
        }

        private void Hit(Collider other, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (consumed || (owner != null && other.transform.root.gameObject == owner))
                return;

            var shield = other.GetComponentInParent<ArenaEnergyShield>();
            var target = shield == null ? other.GetComponentInParent<ArenaDamageable>() : null;
            if (target != null && IsFriendlyTarget(target))
            {
                IgnoreTargetCollisions(target);
                return;
            }

            consumed = true;
            if (shield != null)
            {
                if (!shield.Absorb(damage) && explosionRadius <= 0f)
                {
                    PlayImpact(impactPoint, impactNormal, true, null);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                if (target != null)
                {
                    if (!target.IsDead && damage > 0f)
                    {
                        var vitalsBefore = target.Health + target.Shield;
                        target.TakeDamage(damage, GetOwnerDamageable());
                        var effectiveDamage = Mathf.Max(0f, vitalsBefore - target.Health - target.Shield);
                        if (effectiveDamage > 0f)
                        {
                            owner?.GetComponent<RobotArenaController>()?.RegisterHit();
                            if (target.GetComponent<RobotArenaController>() == null)
                                target.GetComponent<RoboClashRobotAnimator>()?.TriggerHitReaction(0.35f);
                            AwardUltimateCharge(effectiveDamage);
                            ApplyKnockback(target, impactPoint);
                        }
                    }
                }
            }

            if (explosionRadius > 0f)
                ApplyAreaDamage(impactPoint, other);

            PlayImpact(impactPoint, impactNormal, explosionRadius > 0f, shield, target != null);
            Destroy(gameObject);
        }

        private void PlayImpact(Vector3 point, Vector3 normal, bool heavy, ArenaEnergyShield shield,
            bool onRobot = false)
        {
            var color = GetOwnerAccentColor();
            if (shield != null)
                RoboClashVFXKit.PlayShieldImpact(point, normal, color);
            else
                RoboClashVFXKit.PlayImpact(style, point, normal, color, explosionRadius, onRobot);
            owner?.GetComponent<RoboClashRobotAudio>()?.PlayDetachedAt(
                shield != null ? RoboClashAudioEvent.ShieldImpact : RoboClashAudioEvent.ProjectileImpact,
                point);
        }

        private Color GetOwnerAccentColor()
        {
            var definition = owner != null ? owner.GetComponent<RoboClashRobotDefinition>() : null;
            return definition != null ? definition.emissionColor : new Color(1f, 0.55f, 0.2f);
        }

        private void ApplyAreaDamage(Vector3 point, Collider directHit)
        {
            var affected = new HashSet<ArenaDamageable>();
            var directTarget = directHit.GetComponentInParent<ArenaDamageable>();
            if (directTarget != null)
                affected.Add(directTarget);

            foreach (var collider in Physics.OverlapSphere(point, explosionRadius, Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Ignore))
            {
                var target = collider.GetComponentInParent<ArenaDamageable>();
                if (target == null || target.IsDead || IsFriendlyTarget(target) ||
                    !affected.Add(target))
                    continue;

                var targetPoint = target.transform.position + Vector3.up;
                if (Physics.Linecast(point, targetPoint, out var obstruction, Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore) &&
                    obstruction.transform.root != target.transform.root &&
                    obstruction.collider != directHit)
                    continue;

                var distanceFactor = 1f - Mathf.Clamp01(Vector3.Distance(point, target.transform.position) / explosionRadius);
                var appliedDamage = damage * Mathf.Lerp(0.35f, 1f, distanceFactor);
                var vitalsBefore = target.Health + target.Shield;
                target.TakeDamage(appliedDamage, GetOwnerDamageable());
                var effectiveDamage = Mathf.Max(0f, vitalsBefore - target.Health - target.Shield);
                if (effectiveDamage > 0f)
                {
                    owner?.GetComponent<RobotArenaController>()?.RegisterHit();
                    AwardUltimateCharge(effectiveDamage);
                    ApplyKnockback(target, point);
                }
            }
        }

        private bool IsFriendlyTarget(ArenaDamageable target)
        {
            if (target == null || owner == null)
                return false;

            if (target.transform.root.gameObject == owner)
                return true;

            var ownerDefinition = owner.GetComponent<RoboClashRobotDefinition>();
            var targetDefinition = target.GetComponent<RoboClashRobotDefinition>();
            return ownerDefinition != null && targetDefinition != null &&
                   ownerDefinition.teamId == targetDefinition.teamId;
        }

        private ArenaDamageable GetOwnerDamageable()
        {
            return owner != null ? owner.GetComponent<ArenaDamageable>() : null;
        }

        private void IgnoreTargetCollisions(ArenaDamageable target)
        {
            var projectileCollider = GetComponent<Collider>();
            if (projectileCollider == null)
                return;

            foreach (var targetCollider in target.GetComponentsInChildren<Collider>())
            {
                if (targetCollider != null)
                    Physics.IgnoreCollision(projectileCollider, targetCollider);
            }
        }

        private void ApplyKnockback(ArenaDamageable target, Vector3 point)
        {
            if (knockback <= 0f)
                return;

            var movement = target.GetComponent<RoboClashRobotMovement>();
            if (movement == null)
                return;

            var direction = target.transform.position - point;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                movement.ApplyImpulse(direction.normalized * knockback);
        }

        private void AwardUltimateCharge(float amount)
        {
            if (owner == null)
                return;

            var abilities = owner.GetComponent<RoboClashRobotAbilities>();
            var definition = owner.GetComponent<RoboClashRobotDefinition>();
            if (abilities != null && definition != null)
                abilities.AddUltimateCharge(amount * definition.stats.ultimateChargePerDamage);
        }

    }
}
