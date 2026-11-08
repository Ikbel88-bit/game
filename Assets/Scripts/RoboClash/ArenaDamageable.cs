using System;
using UnityEngine;

namespace RoboClash
{
    public class ArenaDamageable : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float maximumShield = 150f;
        [SerializeField, Min(0f)] private float startingShield;
        private float damageTakenMultiplier = 1f;
        private float spawnProtectionEndsAt;

        public float Health { get; private set; }
        public float Shield { get; private set; }
        public float MaximumHealth => maximumHealth;
        public float MaximumShield => maximumShield;
        public bool IsDead { get; private set; }
        public bool IsSpawnProtected => Time.time < spawnProtectionEndsAt;
        public float DamageTakenMultiplier
        {
            get => damageTakenMultiplier;
            set => damageTakenMultiplier = !float.IsNaN(value) && !float.IsInfinity(value)
                ? Mathf.Max(0f, value)
                : 1f;
        }

        public event Action<ArenaDamageable, float, ArenaDamageable> DamageApplied;
        public event Action<ArenaDamageable> VitalsChanged;
        public event Action<ArenaDamageable> Eliminated;

        protected virtual void Awake()
        {
            maximumHealth = !float.IsNaN(maximumHealth) && !float.IsInfinity(maximumHealth)
                ? Mathf.Max(1f, maximumHealth)
                : 100f;
            maximumShield = !float.IsNaN(maximumShield) && !float.IsInfinity(maximumShield)
                ? Mathf.Max(0f, maximumShield)
                : 0f;
            Health = maximumHealth;
            Shield = !float.IsNaN(startingShield) && !float.IsInfinity(startingShield)
                ? Mathf.Clamp(startingShield, 0f, maximumShield)
                : 0f;
        }

        public virtual void TakeDamage(float amount)
        {
            ApplyDamage(amount, null);
        }

        public virtual void TakeDamage(float amount, ArenaDamageable instigator)
        {
            ApplyDamage(amount, instigator);
        }

        public void GrantSpawnProtection(float duration)
        {
            var safeDuration = !float.IsNaN(duration) && !float.IsInfinity(duration)
                ? Mathf.Max(0f, duration)
                : 0f;
            spawnProtectionEndsAt = Time.time + safeDuration;
        }

        private void ApplyDamage(float amount, ArenaDamageable instigator)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) ||
                IsDead || IsSpawnProtected)
                return;

            amount *= DamageTakenMultiplier;
            if (amount <= 0f)
                return;

            var totalVitalsBefore = Health + Shield;
            var absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            Health = Mathf.Max(0f, Health - (amount - absorbed));
            var effectiveDamage = totalVitalsBefore - Health - Shield;
            VitalsChanged?.Invoke(this);
            if (effectiveDamage > 0f)
                DamageApplied?.Invoke(this, effectiveDamage, instigator);

            if (Health <= 0f)
            {
                IsDead = true;
                GetComponent<RoboClashRobotAudio>()?.PlayDetached(RoboClashAudioEvent.Death);
                OnDeath();
                Eliminated?.Invoke(this);
            }
        }

        public void AddShield(float amount)
        {
            if (amount > 0f && !IsDead)
            {
                var previousShield = Shield;
                Shield = Mathf.Min(maximumShield, Shield + amount);
                if (!Mathf.Approximately(previousShield, Shield))
                    VitalsChanged?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (amount > 0f && !IsDead)
            {
                var previousHealth = Health;
                Health = Mathf.Min(maximumHealth, Health + amount);
                if (!Mathf.Approximately(previousHealth, Health))
                    VitalsChanged?.Invoke(this);
            }
        }

        public void ConfigureVitals(float health, float shield)
        {
            maximumHealth = !float.IsNaN(health) && !float.IsInfinity(health)
                ? Mathf.Max(1f, health)
                : 100f;
            maximumShield = !float.IsNaN(shield) && !float.IsInfinity(shield)
                ? Mathf.Max(0f, shield)
                : 0f;
            startingShield = maximumShield;
            Health = maximumHealth;
            Shield = maximumShield;
            IsDead = false;
            VitalsChanged?.Invoke(this);
        }

        protected virtual void OnDeath()
        {
            gameObject.SetActive(false);
        }

        protected void Restore(float health, float shield)
        {
            Health = Mathf.Clamp(health, 1f, maximumHealth);
            Shield = Mathf.Clamp(shield, 0f, maximumShield);
            IsDead = false;
            VitalsChanged?.Invoke(this);
        }
    }
}
