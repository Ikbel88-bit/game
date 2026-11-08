using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpikeTrap : ArenaHazard
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<ArenaDamageable>();
            if (target == null)
                return;

            DamageOnce(target);
            target.GetComponent<RoboClashRobotAudio>()?.Play(RoboClashAudioEvent.Damage);
        }
    }
}
