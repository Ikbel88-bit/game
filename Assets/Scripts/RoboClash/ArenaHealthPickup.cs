using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ArenaHealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float healAmount = 50f;
        [SerializeField, Min(0f)] private float respawnDelay = 20f;

        private Renderer[] renderers;
        private SphereCollider pickupCollider;

        private void Awake()
        {
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
            transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<ArenaDamageable>();
            if (!(target is RobotArenaController) || target.IsDead ||
                target.Health >= target.MaximumHealth)
                return;

            target.Heal(healAmount);
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
            pickupCollider.enabled = available;
            foreach (var item in renderers)
                item.enabled = available;
        }
    }
}
