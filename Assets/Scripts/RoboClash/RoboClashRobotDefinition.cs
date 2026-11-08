using UnityEngine;

namespace RoboClash
{
    public enum RoboClashRobotType
    {
        Volt,
        Titan,
        Shadow,
        Spark
    }

    [System.Serializable]
    public sealed class RoboClashRobotStats
    {
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float maxShield = 50f;
        [Min(0f)] public float moveSpeed = 7f;
        [Min(0f)] public float acceleration = 20f;
        [Min(0f)] public float rotationSpeed = 240f;
        [Min(0.01f)] public float damagePerShot = 18f;
        [Min(0.01f)] public float shotsPerSecond = 4f;
        [Min(0.01f)] public float reloadDuration = 1.5f;
        [Min(0.1f)] public float weaponRange = 20f;
        [Min(0f)] public float incomingDamageMultiplier = 1f;
        [Min(0f)] public float ultimateChargePerDamage = 0.5f;
        [Min(1)] public int magazineSize = 20;
        [Min(0f)] public float knockbackResistance;
    }

    public sealed class RoboClashRobotDefinition : MonoBehaviour
    {
        public RoboClashRobotType robotType;
        public RoboClashRobotStats stats = new RoboClashRobotStats();

        [Header("Appearance")]
        [Tooltip("Shared generated team emission material used by projectiles and muzzle effects.")]
        public Material emissionMaterial;
        [Tooltip("Shared transparent shimmer material used by cloak and shield visuals.")]
        public Material shimmerMaterial;
        public Color primaryColor = Color.blue;
        public Color secondaryColor = Color.white;
        public Color jointColor = new Color(0.12f, 0.16f, 0.22f);
        public Color emissionColor = Color.cyan;
        public float passiveAmount = 0.25f;
        public float abilityDuration = 4f;
        public float abilityCooldown = 12f;
        public float ultimateDuration = 8f;
        public float ultimateCooldown = 45f;
        public float damageRadius;
        public float projectileSpeed = 42f;

        [Header("Weapon")]
        public Transform weaponMount;
        public Transform muzzle;
        public Transform rotatingBarrels;
        public Transform repairDrone;

        [Header("Robot Components")]
        public Transform aimPivot;
        public Transform modelRoot;
        public Transform introCameraAnchor;
        public Transform firstPersonCameraAnchor;

        [Header("Identity")]
        public string weaponName = "Pulse Rifle";
        public string passiveName = "Energy Efficiency";
        public string abilityName = "Dash";
        public string ultimateName = "Overdrive";
        public int teamId;

        public void Configure(RoboClashRobotType type)
        {
            robotType = type;
            if (stats == null)
                stats = new RoboClashRobotStats();

            switch (type)
            {
                case RoboClashRobotType.Volt:
                    SetStats(100f, 50f, 7f, 18f, 4f, 1.5f, 20f, 20, 1f);
                    primaryColor = Hex("147DFF");
                    secondaryColor = Hex("EAF4FF");
                    jointColor = Hex("202B3A");
                    emissionColor = Hex("00D9FF");
                    weaponName = "Pulse Rifle";
                    passiveName = "Energy Efficiency";
                    abilityName = "Dash";
                    ultimateName = "Overdrive";
                    abilityDuration = 0.3f;
                    abilityCooldown = 12f;
                    ultimateDuration = 8f;
                    ultimateCooldown = 45f;
                    projectileSpeed = 44f;
                    break;
                case RoboClashRobotType.Titan:
                    SetStats(150f, 80f, 4.2f, 9f, 7f, 2.4f, 17f, 42, 0.85f);
                    stats.knockbackResistance = 0.75f;
                    primaryColor = Hex("D71932");
                    secondaryColor = Hex("30343B");
                    jointColor = Hex("181A20");
                    emissionColor = Hex("FF3B27");
                    weaponName = "Gatling Cannon";
                    passiveName = "Fortified";
                    abilityName = "Defensive Field";
                    ultimateName = "Rail Cannon";
                    abilityDuration = 4f;
                    abilityCooldown = 18f;
                    ultimateDuration = 0f;
                    ultimateCooldown = 60f;
                    damageRadius = 3.5f;
                    projectileSpeed = 58f;
                    break;
                case RoboClashRobotType.Shadow:
                    SetStats(85f, 60f, 8f, 9f, 10f, 1.3f, 10f, 30, 1f);
                    primaryColor = Hex("542080");
                    secondaryColor = Hex("171321");
                    jointColor = Hex("171321");
                    emissionColor = Hex("E044FF");
                    weaponName = "Energy SMG";
                    passiveName = "Stealth Movement";
                    abilityName = "Cloak";
                    ultimateName = "Shadow Strike";
                    abilityDuration = 5f;
                    abilityCooldown = 16f;
                    ultimateDuration = 0f;
                    ultimateCooldown = 50f;
                    projectileSpeed = 48f;
                    break;
                case RoboClashRobotType.Spark:
                    SetStats(90f, 70f, 5.8f, 45f, 0.9f, 2.5f, 23f, 4, 1f);
                    primaryColor = Hex("FFC928");
                    secondaryColor = Hex("30343B");
                    jointColor = Hex("191B20");
                    emissionColor = Hex("FFB300");
                    weaponName = "Rocket Launcher";
                    passiveName = "Support Aura";
                    abilityName = "Repair Drone";
                    ultimateName = "EMP Blast";
                    abilityDuration = 5f;
                    abilityCooldown = 20f;
                    ultimateDuration = 3f;
                    ultimateCooldown = 55f;
                    damageRadius = 3.5f;
                    projectileSpeed = 30f;
                    break;
            }
        }

        public void ApplyDamageMultiplier(ref float damage)
        {
            damage *= Mathf.Max(0f, stats.incomingDamageMultiplier);
        }

        public static Vector3 GetTargetDimensions(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Titan: return new Vector3(1.8f, 2.5f, 1.4f);
                case RoboClashRobotType.Shadow: return new Vector3(0.9f, 1.8f, 0.7f);
                case RoboClashRobotType.Spark: return new Vector3(1.1f, 1.9f, 0.9f);
                default: return new Vector3(1.1f, 2f, 0.8f);
            }
        }

        public void EnsureCameraAnchors()
        {
            var dimensions = GetTargetDimensions(robotType);
            if (introCameraAnchor == null)
                introCameraAnchor = transform.Find("Intro Camera Anchor");
            if (firstPersonCameraAnchor == null)
                firstPersonCameraAnchor = transform.Find("First Person Camera Anchor");

            if (introCameraAnchor == null)
                introCameraAnchor = CreateCameraAnchor("Intro Camera Anchor",
                    new Vector3(0f, dimensions.y * 0.9f + 0.8f,
                        -(dimensions.y * 2.5f + 1f)),
                    Quaternion.Euler(18f, 0f, 0f));
            if (firstPersonCameraAnchor == null)
                firstPersonCameraAnchor = CreateCameraAnchor("First Person Camera Anchor",
                    new Vector3(0f, dimensions.y * 0.82f, dimensions.z * 0.42f),
                    Quaternion.identity);
        }

        private Transform CreateCameraAnchor(string anchorName, Vector3 localPosition,
            Quaternion localRotation)
        {
            var anchor = new GameObject(anchorName).transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            return anchor;
        }

        private void SetStats(float health, float shield, float speed, float damage, float rate,
            float reload, float range, int magazine, float damageMultiplier)
        {
            stats.maxHealth = health;
            stats.maxShield = shield;
            stats.moveSpeed = speed;
            stats.acceleration = 20f;
            stats.rotationSpeed = 240f;
            stats.damagePerShot = damage;
            stats.shotsPerSecond = rate;
            stats.reloadDuration = reload;
            stats.weaponRange = range;
            stats.incomingDamageMultiplier = damageMultiplier;
            stats.ultimateChargePerDamage = 0.5f;
            stats.magazineSize = magazine;
            stats.knockbackResistance = 0f;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
