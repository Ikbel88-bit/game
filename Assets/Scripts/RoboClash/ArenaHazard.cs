using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public abstract class ArenaHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damagePerSecond = 35f;

        protected virtual void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        protected void Damage(ArenaDamageable target)
        {
            if (target != null)
                target.TakeDamage(damagePerSecond * Time.deltaTime);
        }

        protected void DamageOnce(ArenaDamageable target)
        {
            if (target != null)
                target.TakeDamage(damagePerSecond);
        }
    }
}
