using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LavaPit : ArenaHazard
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            Damage(other.GetComponentInParent<ArenaDamageable>());
        }
    }
}
