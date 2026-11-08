using System;
using UnityEngine;
using UnityEngine.AI;

namespace RoboClash
{
    public enum RoboClashAIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    [Serializable]
    public sealed class RoboClashAIDifficultyProfile
    {
        [Min(0.05f)] public float decisionInterval = 0.25f;
        [Min(0.1f)] public float targetReevaluationInterval = 0.6f;
        [Min(4f)] public float sensingRange = 36f;
        [Min(1f)] public float aimTurnSpeed = 180f;
        [Range(0f, 20f)] public float aimErrorDegrees = 4f;
        [Range(0f, 1f)] public float aggression = 0.65f;
        [Range(0f, 1f)] public float strafeAmount = 0.55f;
        [Range(0f, 1f)] public float hazardAwareness = 0.7f;

        public RoboClashAIDifficultyProfile Clone()
        {
            return (RoboClashAIDifficultyProfile)MemberwiseClone();
        }
    }

    public enum RoboClashAIState
    {
        Idle,
        Searching,
        Approaching,
        Attacking,
        Evading,
        UsingAbility,
        Recovering,
        Dead
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoboClashRobotDefinition), typeof(RoboClashRobotMovement),
        typeof(RoboClashRobotCombat))]
    [RequireComponent(typeof(ArenaDamageable))]
    public sealed class RoboClashRobotAI : MonoBehaviour
    {
        [SerializeField] private ArenaDamageable target;
        [SerializeField] private RoboClashAIState state = RoboClashAIState.Idle;

        private RoboClashRobotMovement movement;
        private RoboClashRobotCombat combat;
        private RoboClashRobotDefinition definition;
        private ArenaDamageable ownVitals;
        private RoboClashRobotAbilities abilities;
        private RoboClashRobotAbilities targetAbilities;
        private RoboClashMatchManager matchManager;
        private CharacterController characterController;
        private RoboClashAIDifficultyProfile profile = new RoboClashAIDifficultyProfile();
        private System.Random random;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[16];
        private readonly Collider[] nearbyColliders = new Collider[24];
        private NavMeshPath navPath;
        private float nextPathTime;
        private Vector3 pathDirection;
        private bool hasPath;
        private Vector3 movementIntent;
        private Vector3 aimIntent;
        private Vector3 searchDirection;
        private Vector3 previousTargetPosition;
        private Vector3 targetVelocity;
        private float nextDecisionTime;
        private float nextTargetRefreshTime;
        private float nextBlockedCheckTime;
        private float nextSearchDirectionTime;
        private float lastDamageTakenTime = float.NegativeInfinity;
        private float targetDistance;
        private float preferredRange;
        private int strafeDirection = 1;
        private bool wasBlocked;

        public RoboClashAIState State => state;
        public ArenaDamageable Target => target;
        public RoboClashAIDifficulty Difficulty { get; private set; } = RoboClashAIDifficulty.Normal;

        private void Awake()
        {
            targetAbilities = target != null ? target.GetComponent<RoboClashRobotAbilities>() : null;
            movement = GetComponent<RoboClashRobotMovement>();
            combat = GetComponent<RoboClashRobotCombat>();
            definition = GetComponent<RoboClashRobotDefinition>();
            ownVitals = GetComponent<ArenaDamageable>();
            abilities = GetComponent<RoboClashRobotAbilities>();
            characterController = GetComponent<CharacterController>();
            var seed = unchecked(Environment.TickCount * 31 +
                                  Mathf.RoundToInt(transform.position.x * 1000f) +
                                  Mathf.RoundToInt(transform.position.z * 1000f));
            random = new System.Random(seed);
            preferredRange = GetPreferredRange(definition.robotType);
            ownVitals.DamageApplied += OnDamageApplied;
        }

        private void OnDestroy()
        {
            if (ownVitals != null)
                ownVitals.DamageApplied -= OnDamageApplied;
        }

        public void Configure(RoboClashMatchManager manager, RoboClashAIDifficulty difficulty,
            RoboClashAIDifficultyProfile difficultyProfile)
        {
            matchManager = manager;
            Difficulty = difficulty;
            profile = difficultyProfile != null
                ? difficultyProfile.Clone()
                : new RoboClashAIDifficultyProfile();
            SetCurrentTarget(null);
            state = RoboClashAIState.Searching;
            nextDecisionTime = Time.time + NextReactionDelay();
            nextTargetRefreshTime = 0f;
            movementIntent = Vector3.zero;
            aimIntent = transform.forward;
            searchDirection = Vector3.zero;
            nextSearchDirectionTime = Time.time;
        }

        public void SetTarget(ArenaDamageable newTarget)
        {
            SetCurrentTarget(IsValidTarget(newTarget) ? newTarget : null);
            nextTargetRefreshTime = 0f;
            targetVelocity = Vector3.zero;
            if (target != null)
                previousTargetPosition = target.transform.position;
        }

        private void Update()
        {
            if (movement == null || combat == null || definition == null || ownVitals == null ||
                characterController == null || ownVitals.IsDead || !gameObject.activeInHierarchy)
            {
                SetState(RoboClashAIState.Dead);
                return;
            }

            if (!characterController.enabled)
                return;

            if (movement.PlayerControlled || matchManager == null ||
                matchManager.State != RoboClashMatchState.Playing)
            {
                if (matchManager != null && matchManager.State == RoboClashMatchState.Paused)
                    SetState(RoboClashAIState.Idle);
                return;
            }

            if (movement.ControlsLocked)
            {
                movement.DriveAI(Vector3.zero, aimIntent, profile.aimTurnSpeed);
                return;
            }

            if (Time.time >= nextTargetRefreshTime)
                RefreshTarget();

            if (!IsValidTarget(target))
            {
                SetState(RoboClashAIState.Searching);
                UpdateSearchMovement();
                movement.DriveAI(movementIntent, aimIntent, profile.aimTurnSpeed);
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                Decide();
                nextDecisionTime = Time.time + NextReactionDelay();
            }

            movement.DriveAI(movementIntent, aimIntent, profile.aimTurnSpeed);
            if (state == RoboClashAIState.Approaching || state == RoboClashAIState.Attacking ||
                state == RoboClashAIState.Evading || state == RoboClashAIState.Recovering)
                TryAttack();
        }

        private void UpdateSearchMovement()
        {
            if (Time.time >= nextSearchDirectionTime || searchDirection.sqrMagnitude < 0.01f)
            {
                searchDirection = new Vector3(NextRandomRange(-1f, 1f), 0f,
                    NextRandomRange(-1f, 1f));
                if (searchDirection.sqrMagnitude < 0.01f)
                    searchDirection = transform.forward;
                searchDirection.Normalize();
                nextSearchDirectionTime = Time.time + NextRandomRange(1.5f, 3.5f);
            }

            movementIntent = ChooseSafeDirection(searchDirection);
            if (movementIntent.sqrMagnitude < 0.01f)
            {
                searchDirection = Vector3.zero;
                nextSearchDirectionTime = Time.time;
                aimIntent = transform.forward;
                return;
            }

            searchDirection = movementIntent;
            aimIntent = movementIntent;
        }

        private void Decide()
        {
            var targetPosition = target.transform.position;
            var elapsed = Mathf.Max(Time.deltaTime, profile.decisionInterval);
            targetVelocity = (targetPosition - previousTargetPosition) / elapsed;
            previousTargetPosition = targetPosition;

            var difference = targetPosition - transform.position;
            difference.y = 0f;
            targetDistance = difference.magnitude;
            if (targetDistance < 0.05f)
                difference = transform.forward;
            var toTarget = difference.normalized;
            var healthRatio = ownVitals.MaximumHealth > 0f
                ? ownVitals.Health / ownVitals.MaximumHealth
                : 1f;
            var underThreat = Time.time - lastDamageTakenTime < 2.2f;
            var weaponRange = Mathf.Min(definition.stats.weaponRange, profile.sensingRange);
            var hasLineOfSight = HasLineOfSight(target, weaponRange);
            var desiredRange = Mathf.Min(preferredRange, weaponRange * 0.85f);
            var urgent = healthRatio < 0.27f && underThreat;

            if (TryUseTacticalAbility(healthRatio, underThreat, hasLineOfSight, toTarget))
                return;

            if (urgent || (definition.robotType == RoboClashRobotType.Shadow && underThreat &&
                           healthRatio < 0.48f))
            {
                SetState(RoboClashAIState.Evading);
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) * strafeDirection);
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            if (!hasLineOfSight && targetDistance <= weaponRange * 0.95f)
            {
                SetState(RoboClashAIState.Recovering);
                movementIntent = ChooseSafeDirection(GetNavDirection(toTarget) +
                    Vector3.Cross(Vector3.up, toTarget) * (strafeDirection * 0.3f));
                aimIntent = toTarget;
                return;
            }

            if (targetDistance > desiredRange * 1.12f)
            {
                SetState(RoboClashAIState.Approaching);
                movementIntent = ChooseSafeDirection(GetNavDirection(toTarget) +
                    Vector3.Cross(Vector3.up, toTarget) *
                    (profile.strafeAmount * 0.35f * strafeDirection));
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            if (targetDistance < desiredRange * 0.52f)
            {
                SetState(RoboClashAIState.Evading);
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) *
                    (profile.strafeAmount * strafeDirection));
                aimIntent = toTarget;
                TryUseUltimate(hasLineOfSight);
                return;
            }

            SetState(hasLineOfSight ? RoboClashAIState.Attacking : RoboClashAIState.Recovering);
            movementIntent = ChooseSafeDirection(Vector3.Cross(Vector3.up, toTarget) *
                (profile.strafeAmount * strafeDirection));
            aimIntent = toTarget;
            TryUseUltimate(hasLineOfSight);
        }

        private Vector3 GetNavDirection(Vector3 fallback)
        {
            if (Time.time >= nextPathTime)
            {
                nextPathTime = Time.time + 0.4f;
                hasPath = false;
                navPath ??= new NavMeshPath();
                if (target != null &&
                    NavMesh.SamplePosition(transform.position, out var from, 3f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(target.transform.position, out var to, 3f, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, navPath) &&
                    navPath.status != NavMeshPathStatus.PathInvalid)
                {
                    var corners = navPath.corners;
                    if (corners.Length < 2)
                        return fallback;

                    var corner = corners[1];
                    if (corners.Length > 2 &&
                        (corner - transform.position).sqrMagnitude < 1f)
                        corner = corners[2];

                    var direction = corner - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        pathDirection = direction.normalized;
                        hasPath = true;
                    }
                }
            }

            return hasPath ? pathDirection : fallback;
        }

        private void RefreshTarget()
        {
            nextTargetRefreshTime = Time.time + profile.targetReevaluationInterval;
            var currentTarget = IsTargetVisible(target) ? target : null;
            if (matchManager == null ||
                !matchManager.TryFindNearestOpponent(ownVitals, profile.sensingRange, out var candidate))
            {
                SetCurrentTarget(currentTarget);
                return;
            }

            if (currentTarget != null && candidate != currentTarget)
            {
                var currentDistance = (currentTarget.transform.position - transform.position).sqrMagnitude;
                var candidateDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (candidateDistance >= currentDistance * 0.72f)
                    candidate = currentTarget;
            }

            SetCurrentTarget(candidate);
            if (target != null)
            {
                previousTargetPosition = target.transform.position;
                targetVelocity = Vector3.zero;
            }
        }

        private bool TryUseTacticalAbility(float healthRatio, bool underThreat, bool hasLineOfSight,
            Vector3 toTarget)
        {
            if (abilities == null || !abilities.IsAbilityReady)
                return false;

            var useAbility = false;
            switch (definition.robotType)
            {
                case RoboClashRobotType.Volt:
                    useAbility = (targetDistance > preferredRange * 1.35f && hasLineOfSight) ||
                                 (healthRatio < 0.34f && underThreat);
                    break;
                case RoboClashRobotType.Titan:
                    useAbility = underThreat && healthRatio < 0.72f;
                    break;
                case RoboClashRobotType.Shadow:
                    useAbility = (underThreat && healthRatio < 0.56f) ||
                                 (!hasLineOfSight && targetDistance > preferredRange);
                    break;
                case RoboClashRobotType.Spark:
                    useAbility = healthRatio < 0.76f ||
                                 ownVitals.Shield < ownVitals.MaximumShield * 0.52f;
                    break;
            }

            if (!useAbility)
                return false;

            if (definition.robotType == RoboClashRobotType.Volt && healthRatio < 0.34f)
                movementIntent = ChooseSafeDirection(-toTarget + Vector3.Cross(Vector3.up, toTarget) * strafeDirection);

            SetState(RoboClashAIState.UsingAbility);
            abilities.ActivateAbility();
            return true;
        }

        private void TryUseUltimate(bool hasLineOfSight)
        {
            if (abilities == null || !abilities.IsUltimateReady)
                return;

            var useful = hasLineOfSight && targetDistance <= definition.stats.weaponRange * 0.9f;
            if (definition.robotType == RoboClashRobotType.Volt)
                useful &= targetDistance <= preferredRange * 1.4f;
            else if (definition.robotType == RoboClashRobotType.Titan)
                useful &= Vector3.Angle(transform.forward, target.transform.position - transform.position) <= 8f;
            else if (definition.robotType == RoboClashRobotType.Shadow)
                useful &= targetDistance <= profile.sensingRange * 0.8f;
            else if (definition.robotType == RoboClashRobotType.Spark)
                useful &= targetDistance <= 7.5f;

            if (!useful)
                return;

            abilities.SetAITarget(target);
            abilities.ActivateUltimate();
        }

        private void TryAttack()
        {
            if (!IsValidTarget(target) || definition.muzzle == null || combat.IsReloading ||
                combat.Ammunition <= 0 || targetDistance > definition.stats.weaponRange ||
                !HasLineOfSight(target, definition.stats.weaponRange))
                return;

            var flatToTarget = target.transform.position - transform.position;
            flatToTarget.y = 0f;
            if (flatToTarget.sqrMagnitude > 0.01f &&
                Vector3.Angle(transform.forward, flatToTarget) > 25f)
                return;

            if (targetAbilities != null && targetAbilities.IsCloaked)
                return;

            var aimPoint = target.transform.position + Vector3.up;
            var predictedPosition = aimPoint + targetVelocity * Mathf.Clamp(
                Vector3.Distance(definition.muzzle.position, aimPoint) /
                Mathf.Max(definition.projectileSpeed, 1f), 0f, 0.55f);
            var error = profile.aimErrorDegrees;
            if (error > 0.01f)
            {
                var yawError = NextRandomRange(-error, error);
                var pitchError = NextRandomRange(-error * 0.45f, error * 0.45f);
                var rotation = Quaternion.Euler(pitchError, yawError, 0f);
                predictedPosition = definition.muzzle.position +
                                    rotation * (predictedPosition - definition.muzzle.position);
            }

            combat.FireAt(predictedPosition);
        }

        private Vector3 ChooseSafeDirection(Vector3 desired)
        {
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                wasBlocked = false;
                return Vector3.zero;
            }

            desired.Normalize();
            if (Time.time >= nextBlockedCheckTime)
            {
                nextBlockedCheckTime = Time.time + profile.decisionInterval;
                var blocked = IsBlocked(desired);
                if (blocked && !wasBlocked)
                    strafeDirection = -strafeDirection;
                wasBlocked = blocked;
                if (blocked)
                {
                    var left = Quaternion.AngleAxis(55f * strafeDirection, Vector3.up) * desired;
                    var right = Quaternion.AngleAxis(-55f * strafeDirection, Vector3.up) * desired;
                    var leftSafe = IsSafeDirection(left);
                    var rightSafe = IsSafeDirection(right);
                    if (leftSafe || rightSafe)
                        desired = leftSafe && rightSafe
                            ? (strafeDirection > 0 ? left : right)
                            : leftSafe ? left : right;
                    else
                        desired = -desired;
                }
            }

            return IsSafeDirection(desired) ? desired : Vector3.zero;
        }

        private bool IsBlocked(Vector3 direction)
        {
            var origin = transform.position + Vector3.up * Mathf.Max(characterController.radius, 0.45f);
            var radius = Mathf.Max(0.2f, characterController.radius * 0.7f);
            var hitCount = Physics.SphereCastNonAlloc(origin, radius, direction, obstructionHits, 1.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = obstructionHits[i];
                if (hit.transform.root == transform.root ||
                    hit.collider.GetComponentInParent<ArenaDamageable>() != null)
                    continue;
                return true;
            }

            return false;
        }

        private bool IsSafeDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return true;

            var lookAhead = transform.position + direction.normalized * 1.6f;
            var radius = Mathf.Max(0.25f, characterController.radius * 0.65f);
            var overlapCount = Physics.OverlapSphereNonAlloc(lookAhead + Vector3.up * 0.35f,
                radius, nearbyColliders, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < overlapCount; i++)
            {
                var collider = nearbyColliders[i];
                if (collider.transform.root == transform.root ||
                    collider.GetComponentInParent<ArenaDamageable>() != null)
                    continue;
                if (profile.hazardAwareness > 0f &&
                    collider.GetComponentInParent<ArenaHazard>() != null)
                    return false;
            }

            if (profile.hazardAwareness >= 0.35f &&
                !Physics.Raycast(lookAhead + Vector3.up * 1.5f, Vector3.down, out var groundHit,
                    4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        private bool HasLineOfSight(ArenaDamageable candidate, float range)
        {
            if (candidate == null || definition.muzzle == null)
                return false;

            var aimPoint = candidate.transform.position + Vector3.up;
            var difference = aimPoint - definition.muzzle.position;
            if (difference.sqrMagnitude > range * range)
                return false;

            if (!Physics.Raycast(definition.muzzle.position, difference.normalized, out var hit,
                    difference.magnitude + 0.25f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform.root == candidate.transform.root;
        }

        private bool IsTargetVisible(ArenaDamageable candidate)
        {
            if (!IsValidTarget(candidate))
                return false;
            var targetAbilities = candidate.GetComponent<RoboClashRobotAbilities>();
            return (targetAbilities == null || !targetAbilities.IsCloaked) &&
                   HasLineOfSight(candidate, profile.sensingRange);
        }

        private bool IsValidTarget(ArenaDamageable candidate)
        {
            return matchManager != null && matchManager.IsValidOpponent(ownVitals, candidate);
        }

        private void SetCurrentTarget(ArenaDamageable candidate)
        {
            target = candidate;
            targetAbilities = candidate != null
                ? candidate.GetComponent<RoboClashRobotAbilities>()
                : null;
        }

        private void OnDamageApplied(ArenaDamageable _, float amount, ArenaDamageable instigator)
        {
            if (amount <= 0f || instigator == null || instigator == ownVitals)
                return;

            lastDamageTakenTime = Time.time;
            if (!IsValidTarget(target) || matchManager.IsValidOpponent(ownVitals, instigator))
            {
                SetCurrentTarget(instigator);
                targetVelocity = Vector3.zero;
                previousTargetPosition = instigator.transform.position;
                nextTargetRefreshTime = Time.time + profile.targetReevaluationInterval;
            }
        }

        private void SetState(RoboClashAIState next)
        {
            if (state == next)
                return;
            state = next;
        }

        private float NextReactionDelay()
        {
            return Mathf.Max(0.05f, profile.decisionInterval) *
                   Mathf.Lerp(1.5f, 0.72f, profile.aggression);
        }

        private float NextRandomRange(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private static float GetPreferredRange(RoboClashRobotType robotType)
        {
            switch (robotType)
            {
                case RoboClashRobotType.Titan: return 7f;
                case RoboClashRobotType.Shadow: return 6f;
                case RoboClashRobotType.Spark: return 12f;
                default: return 9f;
            }
        }
    }
}
