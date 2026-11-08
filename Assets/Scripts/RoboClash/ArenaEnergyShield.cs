using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class ArenaEnergyShield : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumEnergy = 150f;
        [SerializeField, Min(1f)] private float energy = 150f;
        [SerializeField, Min(0f)] private float rechargePerSecond = 8f;

        public float Energy => energy;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            energy = Mathf.Clamp(energy, 0f, maximumEnergy);
        }

        private void Update()
        {
            energy = Mathf.Min(maximumEnergy, energy + rechargePerSecond * Time.deltaTime);
        }

        public bool Absorb(float damage)
        {
            if (energy <= 0f || damage <= 0f)
                return false;

            energy = Mathf.Max(0f, energy - damage);
            return true;
        }
    }
}
