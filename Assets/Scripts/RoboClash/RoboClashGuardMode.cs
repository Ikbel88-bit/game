using UnityEngine;

namespace RoboClash
{
    [DisallowMultipleComponent]
    public sealed class RoboClashGuardMode : MonoBehaviour
    {
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float speedMultiplier = 0.6f;

        private ArenaDamageable damageable;
        private RoboClashRobotCombat combat;
        private RoboClashRobotMovement movement;
        private RoboClashPhoneInputServer phoneServer;
        private RoboClashMatchManager matchManager;
        private RobotArenaController robotController;
        private bool guardReleaseRequired;

        public bool IsGuarding { get; private set; }

        private void Awake()
        {
            damageable = GetComponent<ArenaDamageable>();
            combat = GetComponent<RoboClashRobotCombat>();
            movement = GetComponent<RoboClashRobotMovement>();
            phoneServer = RoboClashPhoneInputServer.Active;
            matchManager = FindAnyObjectByType<RoboClashMatchManager>();
            robotController = GetComponent<RobotArenaController>();
        }

        private void OnEnable()
        {
            if (damageable != null)
                damageable.Eliminated += HandleEliminated;
            if (robotController != null)
                robotController.Respawned += HandleRespawned;
        }

        private void Update()
        {
            phoneServer = RoboClashPhoneInputServer.Active;
            var guardRequested = phoneServer != null && phoneServer.CurrentInput.Guard;
            if (!guardRequested)
                guardReleaseRequired = false;

            var matchPlaying = matchManager == null ||
                               matchManager.State == RoboClashMatchState.Playing;
            var want = matchPlaying && guardRequested && !guardReleaseRequired &&
                       damageable != null && !damageable.IsDead &&
                       movement != null && !movement.ControlsLocked;
            if (want != IsGuarding)
                Apply(want);
        }

        private void Apply(bool guarding)
        {
            IsGuarding = guarding;
            if (damageable != null)
                damageable.DamageTakenMultiplier = guarding ? damageMultiplier : 1f;
            if (combat != null)
                combat.FireBlocked = guarding;
            if (movement != null)
                movement.SpeedMultiplier = guarding ? speedMultiplier : 1f;
            RoboClashGuardEvents.RaiseChanged(this, guarding);
        }

        private void OnDisable()
        {
            if (damageable != null)
                damageable.Eliminated -= HandleEliminated;
            if (robotController != null)
                robotController.Respawned -= HandleRespawned;
            if (IsGuarding)
                Apply(false);
        }

        private void HandleEliminated(ArenaDamageable eliminated)
        {
            if (eliminated == damageable)
                ReleaseGuardUntilInputIsReleased();
        }

        private void HandleRespawned(RobotArenaController respawned)
        {
            if (respawned == robotController)
                ReleaseGuardUntilInputIsReleased();
        }

        private void ReleaseGuardUntilInputIsReleased()
        {
            guardReleaseRequired = true;
            if (IsGuarding)
                Apply(false);
        }
    }

    public static class RoboClashGuardEvents
    {
        public static event System.Action<RoboClashGuardMode, bool> Changed;

        public static void RaiseChanged(RoboClashGuardMode mode, bool guarding)
        {
            Changed?.Invoke(mode, guarding);
        }
    }
}
