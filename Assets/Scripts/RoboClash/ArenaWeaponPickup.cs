using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaWeaponPickup : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float damage = 40f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 32f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.22f;
        [SerializeField, Min(0f)] private float respawnDelay = 12f;

        private Renderer[] renderers;
        private SphereCollider pickupCollider;
        private Vector3 initialPosition;

        private void Awake()
        {
            initialPosition = transform.position;
            renderers = GetComponentsInChildren<Renderer>();
            pickupCollider = GetComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Update()
        {
            transform.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
            transform.position = initialPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.2f);
        }

        private void OnTriggerEnter(Collider other)
        {
            var robot = other.GetComponentInParent<RobotArenaController>();
            if (robot == null || robot.IsDead)
                return;

            var combat = robot.GetComponent<RoboClashRobotCombat>();
            if (combat != null)
                combat.ApplyDamageBuff(1.5f, 15f);
            else
                robot.EquipWeapon(damage, projectileSpeed, fireInterval);
            SetAvailable(false);
            if (respawnDelay > 0f)
                Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            SetAvailable(true);
        }

        private void SetAvailable(bool available)
        {
            if (pickupCollider != null)
                pickupCollider.enabled = available;

            foreach (var item in renderers)
                item.enabled = available;
        }
    }
}
