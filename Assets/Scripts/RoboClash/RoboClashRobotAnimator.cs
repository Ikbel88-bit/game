using UnityEngine;

namespace RoboClash
{
    public sealed class RoboClashRobotAnimator : MonoBehaviour
    {
        [Header("Animated visual references")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;

        [Header("Animation tuning")]
        [SerializeField, Min(0f)] private float walkFrequency = 7f;
        [SerializeField, Range(0f, 45f)] private float walkAngle = 24f;
        [SerializeField, Min(0f)] private float recoilRecovery = 12f;
        [SerializeField, Min(0.01f)] private float movementBlendTime = 0.12f;

        private Vector3 visualRootScale;
        private Vector3 torsoPosition;
        private Vector3 headPosition;
        private Quaternion torsoRotation;
        private Quaternion leftLegRotation;
        private Quaternion rightLegRotation;
        private Quaternion leftArmRotation;
        private Quaternion rightArmRotation;
        private float targetSpeedFraction;
        private float speedFraction;
        private float speedBlendVelocity;
        private float lateralLean;
        private float accelerationLean;
        private float recoil;
        private float hitReaction;
        private float abilityLean;
        private float abilityLeanVelocity;
        private float abilityPoseTarget;
        private float landingPulse;
        private float stateTime;
        private float spawnPoseElapsed;
        private float spawnPoseDuration;
        private float spawnPoseWeight;
        private float abilityEndTime;
        private float deadElapsed;
        private float gaitPhase;
        private bool grounded = true;
        private bool movementStateInitialized;
        private bool dead;
        private float strideMultiplier = 1f;
        private float frequencyMultiplier = 1f;
        private float bodyLeanMultiplier = 1f;
        private float recoilMultiplier = 1f;
        private float landingMultiplier = 1f;
        private float bobMultiplier = 1f;

        private void Awake()
        {
            ApplyMovementPersonality();
            CachePose();
        }

        public void SetVisualParts(Transform model, Transform body, Transform headPivot,
            Transform leftLegPivot, Transform rightLegPivot, Transform leftArmPivot, Transform rightArmPivot)
        {
            visualRoot = model;
            torso = body;
            head = headPivot;
            leftLeg = leftLegPivot;
            rightLeg = rightLegPivot;
            leftArm = leftArmPivot;
            rightArm = rightArmPivot;
            CachePose();
        }

        public void SetMovement(float speed, float maximumSpeed, bool isGrounded)
        {
            SetMovement(Vector3.zero, speed, maximumSpeed, isGrounded);
        }

        public void SetMovement(Vector3 horizontalVelocity, float speed, float maximumSpeed, bool isGrounded)
        {
            targetSpeedFraction = maximumSpeed > 0.001f
                ? Mathf.Clamp01(speed / maximumSpeed)
                : 0f;
            if (movementStateInitialized && !grounded && isGrounded && !dead)
                landingPulse = Mathf.Max(landingPulse, 1f * landingMultiplier);

            if (movementStateInitialized)
            {
                var localVelocity = transform.InverseTransformDirection(horizontalVelocity);
                lateralLean = Mathf.Clamp(localVelocity.x / Mathf.Max(maximumSpeed, 0.1f), -1f, 1f);
                var speedDelta = targetSpeedFraction - speedFraction;
                accelerationLean = Mathf.Clamp(speedDelta * 4f, -1f, 1f);
            }

            grounded = isGrounded;
            movementStateInitialized = true;
        }

        public void TriggerRecoil(float amount)
        {
            if (!dead)
                recoil = Mathf.Clamp01(recoil + Mathf.Max(0f, amount));
        }

        public void TriggerHitReaction(float intensity)
        {
            if (!dead)
                hitReaction = Mathf.Max(hitReaction, Mathf.Clamp01(intensity));
        }

        public void TriggerSpawnReady(float duration)
        {
            if (dead)
                return;

            spawnPoseElapsed = 0f;
            spawnPoseDuration = Mathf.Max(0.1f, duration);
            spawnPoseWeight = 1f;
        }

        public void SetAbilityPose(bool active)
        {
            abilityEndTime = active ? float.PositiveInfinity : 0f;
            abilityPoseTarget = active ? 1f : 0f;
        }

        public void TriggerAbilityPose(float duration)
        {
            if (dead)
                return;

            abilityEndTime = Time.time + Mathf.Max(0.1f, duration);
            abilityPoseTarget = 1f;
        }

        public void SetDead(bool value)
        {
            dead = value;
            if (value)
            {
                deadElapsed = 0f;
                targetSpeedFraction = 0f;
                recoil = 0f;
                hitReaction = 0f;
                abilityLean = 0f;
                abilityPoseTarget = 0f;
                landingPulse = 0f;
                abilityEndTime = 0f;
                spawnPoseWeight = 0f;
            }
            else
            {
                deadElapsed = 0f;
                speedFraction = 0f;
                speedBlendVelocity = 0f;
                targetSpeedFraction = 0f;
                recoil = 0f;
                hitReaction = 0f;
                abilityLean = 0f;
                abilityLeanVelocity = 0f;
                abilityPoseTarget = 0f;
                abilityEndTime = 0f;
                landingPulse = 0.7f * landingMultiplier;
                spawnPoseWeight = 0f;
                if (visualRoot != null)
                    visualRoot.localScale = visualRootScale;
            }
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
                return;

            var deltaTime = Time.deltaTime;
            stateTime += deltaTime;
            if (spawnPoseWeight > 0f)
            {
                spawnPoseElapsed += deltaTime;
                var introProgress = Mathf.Clamp01(spawnPoseElapsed / spawnPoseDuration);
                spawnPoseWeight = 1f - introProgress * introProgress *
                                  (3f - 2f * introProgress);
            }
            speedFraction = Mathf.SmoothDamp(speedFraction, dead ? 0f : targetSpeedFraction,
                ref speedBlendVelocity, movementBlendTime, Mathf.Infinity, deltaTime);

            if (Time.time >= abilityEndTime)
                abilityPoseTarget = 0f;
            abilityLean = Mathf.SmoothDamp(abilityLean, abilityPoseTarget, ref abilityLeanVelocity,
                0.12f, Mathf.Infinity, deltaTime);

            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * deltaTime);
            hitReaction = Mathf.MoveTowards(hitReaction, 0f, 5f * deltaTime);
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 3.5f * deltaTime);
            accelerationLean = Mathf.MoveTowards(accelerationLean, 0f, 3.5f * deltaTime);

            var frequency = walkFrequency * frequencyMultiplier *
                            Mathf.Lerp(0.55f, 1.25f, speedFraction);
            gaitPhase += deltaTime * frequency;
            var stride = Mathf.Sin(gaitPhase * Mathf.PI * 2f) * walkAngle *
                         speedFraction * strideMultiplier;
            var bob = Mathf.Sin(gaitPhase * Mathf.PI * 4f) * 0.014f *
                      speedFraction * bobMultiplier;
            var idleBreath = dead ? 0f : Mathf.Sin(stateTime * 1.7f) * 0.006f;
            var airborneLean = grounded ? 0f : 3f;
            var hitRoll = Mathf.Sin(stateTime * 18f) * hitReaction * 2.2f;

            if (leftLeg != null)
                leftLeg.localRotation = leftLegRotation * Quaternion.Euler(stride, 0f, 0f);
            if (rightLeg != null)
                rightLeg.localRotation = rightLegRotation * Quaternion.Euler(-stride, 0f, 0f);
            if (leftArm != null)
                leftArm.localRotation = leftArmRotation *
                    Quaternion.Euler(-stride * 0.34f + recoil * recoilMultiplier * 2f +
                                     hitReaction * 2f + spawnPoseWeight * 3f,
                        0f, -1.5f * speedFraction);
            if (rightArm != null)
                rightArm.localRotation = rightArmRotation *
                    Quaternion.Euler(stride * 0.2f - recoil * recoilMultiplier * 8f -
                                     hitReaction * 4f + spawnPoseWeight * 3f,
                        0f, 1.5f * speedFraction);

            if (torso != null)
            {
                torso.localPosition = torsoPosition + Vector3.up *
                    (bob + idleBreath - landingPulse * 0.04f - spawnPoseWeight * 0.055f);
                torso.localRotation = torsoRotation * Quaternion.Euler(
                    -speedFraction * 3.5f * bodyLeanMultiplier -
                    accelerationLean * 2.5f * bodyLeanMultiplier -
                    abilityLean * 10f + landingPulse * 4f + airborneLean +
                    hitReaction * 5f - spawnPoseWeight * 5f,
                    0f,
                    Mathf.Sin(stateTime * 0.8f) * 0.8f +
                    lateralLean * speedFraction * 3f * bodyLeanMultiplier + hitRoll);
            }

            if (head != null)
                head.localPosition = headPosition + Vector3.up *
                    (idleBreath * 0.45f + recoil * recoilMultiplier * 0.01f +
                     hitReaction * 0.018f);

            if (dead)
            {
                deadElapsed += deltaTime;
                var collapse = Mathf.Clamp01(deadElapsed / 0.45f);
                visualRoot.localScale = Vector3.Lerp(visualRootScale,
                    visualRootScale * 0.04f, collapse * collapse);
            }
        }

        private void ApplyMovementPersonality()
        {
            var definition = GetComponent<RoboClashRobotDefinition>();
            if (definition == null)
                return;

            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    frequencyMultiplier = 1.08f;
                    strideMultiplier = 0.95f;
                    bodyLeanMultiplier = 1f;
                    recoilMultiplier = 1.05f;
                    landingMultiplier = 1f;
                    break;
                case RoboClashRobotType.Titan:
                    frequencyMultiplier = 0.78f;
                    strideMultiplier = 0.76f;
                    bodyLeanMultiplier = 0.72f;
                    recoilMultiplier = 1.35f;
                    landingMultiplier = 1.35f;
                    break;
                case RoboClashRobotType.Shadow:
                    frequencyMultiplier = 1.2f;
                    strideMultiplier = 1.05f;
                    bodyLeanMultiplier = 1.12f;
                    recoilMultiplier = 0.82f;
                    landingMultiplier = 0.82f;
                    break;
                case RoboClashRobotType.Spark:
                    frequencyMultiplier = 0.92f;
                    strideMultiplier = 0.82f;
                    bodyLeanMultiplier = 0.52f;
                    recoilMultiplier = 0.9f;
                    landingMultiplier = 0.92f;
                    bobMultiplier = 0.6f;
                    break;
            }
        }

        private void CachePose()
        {
            visualRootScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            torsoPosition = torso != null ? torso.localPosition : Vector3.zero;
            headPosition = head != null ? head.localPosition : Vector3.zero;
            torsoRotation = torso != null ? torso.localRotation : Quaternion.identity;
            leftLegRotation = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            rightLegRotation = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            leftArmRotation = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmRotation = rightArm != null ? rightArm.localRotation : Quaternion.identity;
        }
    }
}
