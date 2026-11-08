using UnityEngine;

namespace RoboClash
{
    public struct RoboClashAIProfile
    {
        public string label;
        public string description;
        public float reactionDelayMultiplier;  // >1 = slower reactions
        public float aimErrorDegrees;           // random cone added to AI aim (0 = perfect)
        public float fireRateMultiplier;        // <1 = fires less often
        public float damageMultiplier;          // damage dealt by AI robots
        public float moveSpeedMultiplier;
        public float abilityUseChance;          // 0..1 chance to use an ability when available
        public float healthMultiplier;          // AI max health/shield
    }

    public static class RoboClashAIProfiles
    {
        public static RoboClashAIProfile Get(RoboClashAIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case RoboClashAIDifficulty.Easy:
                    return new RoboClashAIProfile
                    {
                        label = "AMATEUR",
                        description = "Slow reactions, poor aim. Good for learning.",
                        reactionDelayMultiplier = 2.0f,
                        aimErrorDegrees = 9f,
                        fireRateMultiplier = 0.6f,
                        damageMultiplier = 0.6f,
                        moveSpeedMultiplier = 0.8f,
                        abilityUseChance = 0.25f,
                        healthMultiplier = 0.8f
                    };
                case RoboClashAIDifficulty.Normal:
                    return new RoboClashAIProfile
                    {
                        label = "NORMAL",
                        description = "Fair fight. Decent aim, uses abilities sometimes.",
                        reactionDelayMultiplier = 1.4f,
                        aimErrorDegrees = 4.5f,
                        fireRateMultiplier = 0.8f,
                        damageMultiplier = 0.8f,
                        moveSpeedMultiplier = 0.9f,
                        abilityUseChance = 0.6f,
                        healthMultiplier = 0.9f
                    };
                default:
                    return new RoboClashAIProfile
                    {
                        label = "HARD",
                        description = "Fast, accurate and aggressive.",
                        reactionDelayMultiplier = 1f,
                        aimErrorDegrees = 0f,
                        fireRateMultiplier = 1f,
                        damageMultiplier = 1f,
                        moveSpeedMultiplier = 1f,
                        abilityUseChance = 1f,
                        healthMultiplier = 1f
                    };
            }
        }
    }
}
