using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KingOfArenaZone : MonoBehaviour
    {
        private readonly Dictionary<ArenaDamageable, HashSet<Collider>> occupants =
            new Dictionary<ArenaDamageable, HashSet<Collider>>();

        public IEnumerable<ArenaDamageable> Occupants => occupants.Keys;

        public void ClearOccupants()
        {
            occupants.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var participant = other.GetComponentInParent<ArenaDamageable>();
            if (participant == null)
                return;

            if (!occupants.TryGetValue(participant, out var colliders))
            {
                colliders = new HashSet<Collider>();
                occupants.Add(participant, colliders);
            }

            colliders.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            var participant = other.GetComponentInParent<ArenaDamageable>();
            if (participant == null || !occupants.TryGetValue(participant, out var colliders))
                return;

            colliders.Remove(other);
            if (colliders.Count == 0)
                occupants.Remove(participant);
        }

        private void OnDisable()
        {
            occupants.Clear();
        }
    }
}
