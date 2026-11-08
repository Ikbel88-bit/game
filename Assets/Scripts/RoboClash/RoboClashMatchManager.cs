using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace RoboClash
{
    public static class RoboClashSceneUtility
    {
        public const string GameplayScenePath = "Assets/Scenes/RoboClashGameplay.unity";
        public const string XLGameplayScenePath = "Assets/Scenes/RoboClashGameplay_XL.unity";
        public const string ActiveGameplaySceneName = "RoboClashGameplay_XL";

        public static bool IsGameplayScene(Scene scene)
        {
            return string.Equals(scene.path, GameplayScenePath, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scene.path, XLGameplayScenePath, StringComparison.OrdinalIgnoreCase);
        }

    }

    public enum RoboClashMatchState
    {
        Initializing,
        Ready,
        Countdown,
        Playing,
        Paused,
        Victory,
        Defeat,
        Draw
    }

    public enum RoboClashGameMode
    {
        FreeForAll = 0,
        Duel = 1,
        Survival = 2,
        KillRace = 3,
        TimedDeathmatch = 4,
        KingOfTheArena = 5,
        Knockout = 6,
        Multiplayer = 7
    }

    [Serializable]
    public sealed class RoboClashMatchSettings
    {
        [Min(1f)] public float matchDurationSeconds = 180f;
        [Min(0f)] public float countdownDurationSeconds = 3f;
        public RoboClashGameMode gameMode = RoboClashGameMode.FreeForAll;
        [Range(1, 7)] public int aiOpponents = 3;
        public bool enableTimeLimit = true;
        [Range(1, 24)] public int startingSurvivalEnemies = 3;
        [Min(1)] public int additionalEnemiesPerWave = 1;
        [Range(1, 24)] public int maximumSurvivalEnemiesPerWave = 24;
        [Min(0f)] public float waveBreakSeconds = 5f;
        public bool respawnEnabled;
        [Min(0)] public int maximumRespawns;
        [Min(1)] public int killTarget = 10;
        [Range(1, 10)] public int knockoutLives = 3;
        [Range(2, 4)] public int localPlayerCount = 2;
        public RoboClashRobotType[] multiplayerRobots =
        {
            RoboClashRobotType.Volt,
            RoboClashRobotType.Titan,
            RoboClashRobotType.Shadow,
            RoboClashRobotType.Spark
        };
        public bool duelUsesKillTarget = true;
        public bool duelRespawnsEnabled = true;
        public RoboClashRobotType playerRobotType = RoboClashRobotType.Volt;
        public RoboClashAIDifficulty aiDifficulty = RoboClashAIDifficulty.Normal;
        public RoboClashAIDifficultyProfile easyAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.55f,
            targetReevaluationInterval = 1.1f,
            sensingRange = 24f,
            aimTurnSpeed = 100f,
            aimErrorDegrees = 10f,
            aggression = 0.35f,
            strafeAmount = 0.25f,
            hazardAwareness = 0.35f
        };
        public RoboClashAIDifficultyProfile normalAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.28f,
            targetReevaluationInterval = 0.55f,
            sensingRange = 36f,
            aimTurnSpeed = 180f,
            aimErrorDegrees = 4f,
            aggression = 0.65f,
            strafeAmount = 0.55f,
            hazardAwareness = 0.7f
        };
        public RoboClashAIDifficultyProfile hardAI = new RoboClashAIDifficultyProfile
        {
            decisionInterval = 0.14f,
            targetReevaluationInterval = 0.28f,
            sensingRange = 48f,
            aimTurnSpeed = 300f,
            aimErrorDegrees = 1.2f,
            aggression = 0.88f,
            strafeAmount = 0.8f,
            hazardAwareness = 0.95f
        };
        public bool useRandomSeed;
        public int randomSeed = 12345;
    }

    [Serializable]
    public struct RoboClashMatchStatistics
    {
        public int PlayerEliminations { get; private set; }
        public int PlayerDeaths { get; private set; }
        public float PlayerDamageDealt { get; private set; }
        public float PlayerDamageReceived { get; private set; }
        public float MatchDurationSeconds { get; private set; }
        public RoboClashMatchState FinalResult { get; private set; }
        public RoboClashGameMode GameMode { get; private set; }
        public int HighestWaveReached { get; private set; }
        public int HighestWaveCompleted { get; private set; }
        public int PlayerPlacement { get; private set; }

        internal void RecordElimination()
        {
            PlayerEliminations++;
        }

        internal void RecordPlayerDeath()
        {
            PlayerDeaths++;
        }

        internal void RecordDamageDealt(float amount)
        {
            PlayerDamageDealt += amount;
        }

        internal void RecordDamageReceived(float amount)
        {
            PlayerDamageReceived += amount;
        }

        internal void Complete(float duration, RoboClashMatchState result)
        {
            MatchDurationSeconds = Mathf.Max(0f, duration);
            FinalResult = result;
        }

        internal void SetGameMode(RoboClashGameMode gameMode)
        {
            GameMode = gameMode;
        }

        internal void RecordWave(int wave)
        {
            HighestWaveReached = Mathf.Max(HighestWaveReached, wave);
        }

        internal void RecordWaveCompleted(int wave)
        {
            HighestWaveCompleted = Mathf.Max(HighestWaveCompleted, wave);
        }

        internal void SetPlayerPlacement(int placement)
        {
            PlayerPlacement = Mathf.Max(0, placement);
        }
    }

    public sealed class RoboClashMatchManager : MonoBehaviour
    {
        private const string MainMenuSceneName = "RoboClashMainMenu";
        public const float KnockoutTimeLimitSeconds = 300f;
        private static RoboClashMatchManager activeInstance;

        [SerializeField] private RoboClashMatchSettings settings = new RoboClashMatchSettings();
        [SerializeField] private RoboClashProgressionRewards progressionRewards =
            new RoboClashProgressionRewards();
        [SerializeField] private GameObject[] robotPrefabs = new GameObject[4];

        private sealed class Participant
        {
            public string Id;
            public RobotArenaController Controller;
            public ArenaDamageable Damageable;
            public bool IsPlayer;
            public int PlayerIndex = -1;
            public bool DeathProcessed;
            public bool IsEliminated;
            public int Kills;
            public int Deaths;
            public int LivesRemaining;
            public float KingScore;
            public bool AIEnabled;
            public bool AbilitiesEnabled;
            public bool CombatEnabled;
        }

        private static readonly Vector3[] FallbackSpawns =
        {
            new Vector3(-12.5f, 0f, -12.5f),
            new Vector3(12.5f, 0f, -12.5f),
            new Vector3(-12.5f, 0f, 12.5f),
            new Vector3(12.5f, 0f, 12.5f)
        };

        private readonly List<Participant> participants = new List<Participant>();
        private readonly Dictionary<GameObject, bool> priorObjectStates = new Dictionary<GameObject, bool>();
        private readonly Dictionary<ArenaDamageable, ArenaDamageable> lastDamageSources =
            new Dictionary<ArenaDamageable, ArenaDamageable>();
        private readonly List<ArenaDamageable> currentWaveEnemies = new List<ArenaDamageable>();
        private readonly List<string> archivedScoreRows = new List<string>();
        private readonly List<Vector3> spawnCandidates = new List<Vector3>();
        private readonly List<GameObject> multiplayerHudObjects = new List<GameObject>();

        private Transform participantRoot;
        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private Vector3 originalCameraLocalScale;
        private bool cameraWasDetached;
        private float originalShadowDistance;
        private bool hasOriginalShadowDistance;
        private RobotArenaController player;
        private Camera playerCamera;
        private RoboClashSplitScreenManager splitScreenManager;
        private NavMeshSurface navSurface;
        private Coroutine countdownRoutine;
        private Coroutine eliminationRoutine;
        private Coroutine waveRoutine;
        private bool eliminationEvaluationPending;
        private float remainingTime;
        private float elapsedMatchSeconds;
        private float waveBreakEndsAt;
        private float nextModeStatusUpdate;
        private int currentWave;
        private bool waveBreakActive;
        private float timeScaleBeforePause = 1f;
        private CursorLockMode cursorLockBeforePause;
        private bool cursorVisibleBeforePause;
        private int countdownValue;
        private bool initializationComplete;
        private bool hasPausedTime;
        private int nextParticipantId;
        private KingOfArenaZone kingZone;
        private Material kingZoneMaterial;
        private ArenaDamageable kingController;
        private bool kingContested;
        private RoboClashMatchStatistics statistics;

        public RoboClashMatchState State { get; private set; } = RoboClashMatchState.Initializing;
        public int WinnerPlayerIndex { get; private set; } = -1;
        public static RoboClashMatchManager Active => activeInstance;
        public float RemainingTime => remainingTime;
        public float ElapsedMatchSeconds => elapsedMatchSeconds;
        public float NextWaveCountdown => waveBreakActive
            ? Mathf.Max(0f, waveBreakEndsAt - Time.time)
            : 0f;
        public int CurrentWave => currentWave;
        public int EnemiesRemaining => settings.gameMode == RoboClashGameMode.Survival
            ? CountLivingWaveEnemies()
            : settings.gameMode == RoboClashGameMode.Multiplayer
                ? AliveCombatantCount
                : CountAliveOpponents();
        public bool IsWaveBreakActive => waveBreakActive;
        public RoboClashGameMode GameMode => settings != null
            ? SanitizeGameMode(settings.gameMode)
            : RoboClashGameMode.TimedDeathmatch;
        public bool HasMatchTimer => settings != null && settings.enableTimeLimit && IsTimedMatch();
        public bool DuelUsesKillTarget => settings != null && settings.duelUsesKillTarget;
        public RoboClashAIDifficulty AIDifficulty => settings.aiDifficulty;
        public int CountdownValue => countdownValue;
        public RoboClashMatchStatistics Statistics => statistics;
        public RobotArenaController PlayerController => player;
        public RoboClashRobotType PlayerRobotType => player != null
            ? player.GetComponent<RoboClashRobotDefinition>().robotType
            : settings.playerRobotType;
        public int ParticipantCount => participants.Count;
        public int AliveOpponentCount => CountAliveOpponents();
        public int AliveCombatantCount => CountAliveCombatants();
        public int KillTarget => settings.killTarget;
        public int KnockoutLives => settings.knockoutLives;
        public bool RespawnsEnabled => settings.respawnEnabled;
        public string KingControllerName => kingContested ? "CONTESTED" : GetKingControllerName();
        public int KingControllerScore => GetKingScore(kingController);
        public int PlayerKills => GetParticipant(player != null ? (ArenaDamageable)player : null)?.Kills ?? 0;
        public int PlayerDeaths => GetParticipant(player != null ? (ArenaDamageable)player : null)?.Deaths ?? 0;
        public int PlayerLivesRemaining => GetParticipant(player != null ? (ArenaDamageable)player : null)?.LivesRemaining ?? 0;
        public float PlayerKingControlSeconds =>
            GetParticipant(player != null ? (ArenaDamageable)player : null)?.KingScore ?? 0f;
        public int KnockoutRobotsRemaining
        {
            get
            {
                var count = 0;
                foreach (var participant in participants)
                {
                    if (!participant.IsEliminated)
                        count++;
                }

                return count;
            }
        }
        public int RequiredSpawnZoneCount => settings == null
            ? 0
            : settings.gameMode == RoboClashGameMode.Survival
                ? 1
                : settings.gameMode == RoboClashGameMode.Multiplayer
                    ? Mathf.Clamp(settings.localPlayerCount, 2, 4)
                    : GetInitialOpponentCount() + 1;

        public event Action<RoboClashMatchState> MatchStateChanged;
        public event Action<float> MatchTimerChanged;
        public event Action<int> CountdownChanged;
        public event Action MatchStarted;
        public event Action<RoboClashMatchStatistics> MatchEnded;
        public event Action<RoboClashMatchStatistics> StatisticsChanged;
        public event Action<RobotArenaController> PlayerRegistered;
        public event Action<ArenaDamageable, ArenaDamageable> ParticipantEliminated;
        public event Action ModeStatusChanged;

        public static RoboClashGameMode SanitizeGameMode(RoboClashGameMode mode)
        {
            return Enum.IsDefined(typeof(RoboClashGameMode), mode) &&
                   mode != RoboClashGameMode.FreeForAll
                ? mode
                : RoboClashGameMode.TimedDeathmatch;
        }

        public string GetPlayerObjectiveProgress()
        {
            if (GameMode == RoboClashGameMode.Multiplayer)
            {
                var scores = new List<int>(participants.Count);
                foreach (var participant in participants)
                {
                    if (participant == null || !participant.IsPlayer || participant.PlayerIndex < 0)
                        continue;
                    scores.Add(participant.Kills);
                }

                return RoboClashObjectives.Progress(scores, settings.killTarget);
            }

            var playerParticipant = FindParticipant(player);
            return RoboClashObjectives.Progress(GameMode, playerParticipant?.Kills ?? 0,
                settings != null && (GameMode != RoboClashGameMode.Duel ||
                                     settings.duelUsesKillTarget)
                    ? settings.killTarget
                    : 0,
                currentWave, EnemiesRemaining,
                playerParticipant != null ? playerParticipant.KingScore : 0f,
                playerParticipant != null ? playerParticipant.LivesRemaining : 0,
                GameMode == RoboClashGameMode.Knockout ? KnockoutRobotsRemaining : AliveCombatantCount);
        }

        public int GetPlayerKills(int playerIndex)
        {
            return FindParticipantByPlayerIndex(playerIndex)?.Kills ?? 0;
        }

        public RobotArenaController GetPlayerController(int playerIndex)
        {
            return FindParticipantByPlayerIndex(playerIndex)?.Controller;
        }

        public string GetMultiplayerScoreboardText()
        {
            if (GameMode != RoboClashGameMode.Multiplayer || settings == null)
                return string.Empty;

            var count = Mathf.Clamp(settings.localPlayerCount, 2, 4);
            var rows = new List<string>(count);
            for (var index = 0; index < count; index++)
                rows.Add($"P{index + 1} {GetPlayerKills(index)}");
            return string.Join("  ", rows) + $" (first to {settings.killTarget})";
        }

        private Participant FindParticipantByPlayerIndex(int playerIndex)
        {
            if (playerIndex < 0)
                return null;

            foreach (var participant in participants)
            {
                if (participant != null && participant.IsPlayer &&
                    participant.PlayerIndex == playerIndex)
                    return participant;
            }

            return null;
        }

        public string GetObjectiveDescription()
        {
            return settings == null
                ? string.Empty
                : RoboClashObjectives.Describe(settings, settings.matchDurationSeconds);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeInstance = null;
        }

        private void Awake()
        {
            if (!enabled)
                return;

            if (!RoboClashSceneUtility.IsGameplayScene(gameObject.scene))
            {
                Debug.LogError(
                    $"RoboClashMatchManager must only initialize in a supported gameplay scene, but was found in " +
                    $"scene '{gameObject.scene.name}' at '{gameObject.scene.path}'.",
                    this);
                enabled = false;
                return;
            }

            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogError("Only one RoboClashMatchManager may be active at a time.", this);
                enabled = false;
                return;
            }

            activeInstance = this;
        }

        private void OnValidate()
        {
            if (settings == null)
                settings = new RoboClashMatchSettings();
            settings.gameMode = SanitizeGameMode(settings.gameMode);
            if (progressionRewards == null)
                progressionRewards = new RoboClashProgressionRewards();

            settings.matchDurationSeconds = IsFinite(settings.matchDurationSeconds)
                ? Mathf.Max(1f, settings.matchDurationSeconds)
                : 180f;
            settings.countdownDurationSeconds = IsFinite(settings.countdownDurationSeconds)
                ? Mathf.Max(0f, settings.countdownDurationSeconds)
                : 3f;
            settings.aiOpponents = Mathf.Clamp(settings.aiOpponents, 1, 7);
            settings.maximumSurvivalEnemiesPerWave = Mathf.Clamp(settings.maximumSurvivalEnemiesPerWave, 1, 24);
            settings.startingSurvivalEnemies = Mathf.Clamp(settings.startingSurvivalEnemies, 1,
                settings.maximumSurvivalEnemiesPerWave);
            settings.additionalEnemiesPerWave = Mathf.Max(1, settings.additionalEnemiesPerWave);
            settings.waveBreakSeconds = IsFinite(settings.waveBreakSeconds)
                ? Mathf.Max(0f, settings.waveBreakSeconds)
                : 5f;
            settings.maximumRespawns = Mathf.Max(0, settings.maximumRespawns);
            settings.killTarget = Mathf.Max(1, settings.killTarget);
            settings.knockoutLives = Mathf.Clamp(settings.knockoutLives, 1, 10);
            settings.localPlayerCount = Mathf.Clamp(settings.localPlayerCount, 2, 4);
            if (settings.multiplayerRobots == null || settings.multiplayerRobots.Length != 4)
                settings.multiplayerRobots = new[]
                {
                    RoboClashRobotType.Volt, RoboClashRobotType.Titan,
                    RoboClashRobotType.Shadow, RoboClashRobotType.Spark
                };
            if (settings.easyAI == null)
                settings.easyAI = new RoboClashAIDifficultyProfile();
            if (settings.normalAI == null)
                settings.normalAI = new RoboClashAIDifficultyProfile();
            if (settings.hardAI == null)
                settings.hardAI = new RoboClashAIDifficultyProfile();
        }

        private void Start()
        {
            var hasMatchSelection = RoboClashSession.ConsumeMatchSelection(out var selectedRobot, out var opponents,
                    out var difficulty, out var gameMode, out var survivalGrowth,
                    out var selectedWaveBreakSeconds, out var selectedTimerOption,
                    out var selectedKillTargetOption, out var selectedKnockoutLives,
                    out var allowRespawns, out var duelKillTarget);
            if (!hasMatchSelection)
            {
                if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
                {
                    Debug.LogError(
                        $"Gameplay was opened without a menu match selection, and menu scene " +
                        $"'{MainMenuSceneName}' is not available in Build Settings.",
                        this);
                    enabled = false;
                    return;
                }

                Debug.Log(
                    "Gameplay was opened without a menu match selection; returning to the main menu.",
                    this);
                SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
                return;
            }

            settings.playerRobotType = selectedRobot;
            settings.aiOpponents = Mathf.Clamp(opponents, 1, 7);
            settings.localPlayerCount = RoboClashSession.LocalPlayerCount;
            settings.multiplayerRobots = RoboClashSession.MultiplayerRobots;
            settings.startingSurvivalEnemies = Mathf.Clamp(opponents, 1,
                settings.maximumSurvivalEnemiesPerWave);
            settings.matchDurationSeconds =
                new[] { 180f, 300f, 420f }[Mathf.Clamp(selectedTimerOption, 0, 2)];
            settings.enableTimeLimit = true;
            settings.aiDifficulty = difficulty;
            settings.gameMode = SanitizeGameMode(gameMode);
            WinnerPlayerIndex = -1;
            if (settings.gameMode == RoboClashGameMode.Knockout)
                settings.matchDurationSeconds = KnockoutTimeLimitSeconds;
            settings.additionalEnemiesPerWave = survivalGrowth;
            settings.waveBreakSeconds = selectedWaveBreakSeconds;
            settings.killTarget = new[] { 5, 10, 15, 20 }[Mathf.Clamp(selectedKillTargetOption, 0, 3)];
            settings.knockoutLives = selectedKnockoutLives;
            settings.respawnEnabled = allowRespawns;
            settings.duelUsesKillTarget = duelKillTarget;
            settings.duelRespawnsEnabled = allowRespawns;

            settings.enableTimeLimit = IsTimedMatch();
            InitializeMatch();
        }

        private void Update()
        {
            if (State == RoboClashMatchState.Playing)
            {
                elapsedMatchSeconds += Time.deltaTime;
                UpdateKingControl();
                if (settings.enableTimeLimit && IsTimedMatch())
                {
                    remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
                    MatchTimerChanged?.Invoke(remainingTime);

                    if (remainingTime <= 0f && !eliminationEvaluationPending)
                        ResolveTimeout();
                }
                else if (settings.gameMode == RoboClashGameMode.Survival)
                {
                    MatchTimerChanged?.Invoke(elapsedMatchSeconds);
                }

                if (Time.time >= nextModeStatusUpdate)
                {
                    nextModeStatusUpdate = Time.time + 0.2f;
                    ModeStatusChanged?.Invoke();
                }
            }

            var pausePressed = false;
            foreach (var participant in participants)
            {
                if (!participant.IsPlayer || participant.Controller == null)
                    continue;

                var inputRouter = participant.Controller.GetComponent<RoboClashPlayerInputRouter>();
                if (inputRouter != null && inputRouter.Source != null &&
                    inputRouter.Source.PausePressed)
                {
                    pausePressed = true;
                    break;
                }
            }

            if (!pausePressed)
                return;

            if (State == RoboClashMatchState.Playing)
                PauseMatch();
            else if (State == RoboClashMatchState.Paused)
                ResumeMatch();
        }

        private void OnDestroy()
        {
            StopWaveRoutine();
            if (hasPausedTime)
                Time.timeScale = timeScaleBeforePause;

            if (State == RoboClashMatchState.Paused)
            {
                SetParticipantInputLock(false);
                SetParticipantSimulationPaused(false);
                RestoreCursorState();
            }

            UnsubscribeParticipants();
            RestoreCameraParent();
            if (hasOriginalShadowDistance)
                QualitySettings.shadowDistance = originalShadowDistance;
            RestorePreviousSceneObjects();
            if (kingZoneMaterial != null)
                Destroy(kingZoneMaterial);
            if (activeInstance == this)
                activeInstance = null;
        }

        public void ConfigureRobotPrefabs(GameObject[] prefabs)
        {
            robotPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : Array.Empty<GameObject>();
        }

        public bool TryFindNearestOpponent(ArenaDamageable seeker, float range,
            out ArenaDamageable nearestOpponent)
        {
            nearestOpponent = null;
            if (seeker == null || State != RoboClashMatchState.Playing)
                return false;

            var seekerDefinition = seeker.GetComponent<RoboClashRobotDefinition>();
            if (seekerDefinition == null)
                return false;

            var nearestDistance = Mathf.Max(0f, range) * Mathf.Max(0f, range);
            foreach (var participant in participants)
            {
                var candidate = participant.Damageable;
                if (!IsValidOpponent(seeker, candidate, seekerDefinition))
                    continue;

                var distance = (candidate.transform.position - seeker.transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestOpponent = candidate;
                nearestDistance = distance;
            }

            return nearestOpponent != null;
        }

        public bool IsValidOpponent(ArenaDamageable seeker, ArenaDamageable candidate)
        {
            if (seeker == null)
                return false;
            return IsValidOpponent(seeker, candidate, seeker.GetComponent<RoboClashRobotDefinition>());
        }

        private bool IsValidOpponent(ArenaDamageable seeker, ArenaDamageable candidate,
            RoboClashRobotDefinition seekerDefinition)
        {
            if (State != RoboClashMatchState.Playing || seekerDefinition == null ||
                candidate == null || candidate == seeker || candidate.IsDead ||
                !candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
                return false;

            var candidateDefinition = candidate.GetComponent<RoboClashRobotDefinition>();
            if (candidateDefinition == null || candidateDefinition.teamId == seekerDefinition.teamId)
                return false;

            var candidateAbilities = candidate.GetComponent<RoboClashRobotAbilities>();
            if (candidateAbilities != null && candidateAbilities.IsCloaked)
                return false;

            foreach (var participant in participants)
            {
                if (participant.Damageable == candidate)
                    return true;
            }

            return false;
        }
        private RoboClashAIDifficultyProfile GetAIDifficultyProfile(RoboClashAIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case RoboClashAIDifficulty.Easy: return settings.easyAI;
                case RoboClashAIDifficulty.Hard: return settings.hardAI;
                default: return settings.normalAI;
            }
        }

        public void ReturnToMainMenu()
        {
            const string menuSceneName = "RoboClashMainMenu";
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                Debug.LogError(
                    "Cannot return to the main menu: RoboClashMainMenu is not included in Build Settings. " +
                    "Use Tools > RoboClash > Create Main Menu UI to create and register it.",
                    this);
                return;
            }

            if (hasPausedTime)
            {
                Time.timeScale = timeScaleBeforePause;
                hasPausedTime = false;
            }

            SceneManager.LoadScene(menuSceneName);
        }

        public bool ValidateConfiguration(out string message)
        {
            if (settings == null)
            {
                message = "Match settings are missing.";
                return false;
            }

            var expectedPrefabCount = Enum.GetValues(typeof(RoboClashRobotType)).Length;
            if (!Enum.IsDefined(typeof(RoboClashRobotType), settings.playerRobotType))
            {
                message = "The selected player robot type is invalid.";
                return false;
            }
            if (!Enum.IsDefined(typeof(RoboClashGameMode), settings.gameMode))
            {
                message = "The selected game mode is invalid.";
                return false;
            }
            if (settings.gameMode == RoboClashGameMode.Survival &&
                (settings.startingSurvivalEnemies < 1 ||
                 settings.maximumSurvivalEnemiesPerWave < settings.startingSurvivalEnemies ||
                 settings.additionalEnemiesPerWave < 1 || settings.waveBreakSeconds < 0f ||
                 !IsFinite(settings.waveBreakSeconds)))
            {
                message = "Survival wave settings must use a positive starting size/growth, " +
                          "a cap at least as large as the starting size, and a finite non-negative break.";
                return false;
            }
            if (settings.killTarget < 1 || settings.knockoutLives < 1 || settings.knockoutLives > 10 ||
                (settings.gameMode == RoboClashGameMode.KingOfTheArena &&
                 !IsFinite(settings.matchDurationSeconds)))
            {
                message = "Kill targets, Knockout lives, and timed mode durations must be valid finite values.";
                return false;
            }
            if (settings.gameMode == RoboClashGameMode.Multiplayer &&
                (settings.localPlayerCount < 2 || settings.localPlayerCount > 4 ||
                 settings.multiplayerRobots == null || settings.multiplayerRobots.Length != 4))
            {
                message = "Local multiplayer requires 2-4 players and four robot selections.";
                return false;
            }
            if (settings.gameMode == RoboClashGameMode.Multiplayer)
            {
                foreach (var type in settings.multiplayerRobots)
                {
                    if (!Enum.IsDefined(typeof(RoboClashRobotType), type))
                    {
                        message = "A local multiplayer robot selection is invalid.";
                        return false;
                    }
                }
            }
            if (robotPrefabs == null || robotPrefabs.Length != expectedPrefabCount)
            {
                message = $"Expected {expectedPrefabCount} generated robot prefab references.";
                return false;
            }

            for (var i = 0; i < expectedPrefabCount; i++)
            {
                var prefab = robotPrefabs[i];
                if (prefab == null)
                {
                    message = $"Robot prefab slot {i} is empty.";
                    return false;
                }

                if (prefab.GetComponents<ArenaDamageable>().Length != 1)
                {
                    message = $"Robot prefab '{prefab.name}' has duplicate damage components. " +
                              "Run Tools > RoboClash > Repair Generated Robot Damage Components.";
                    return false;
                }

                if (prefab.GetComponent<RoboClashRobotDefinition>() == null ||
                    prefab.GetComponent<RobotArenaController>() == null ||
                    prefab.GetComponent<RoboClashRobotMovement>() == null ||
                    prefab.GetComponent<RoboClashRobotCombat>() == null ||
                    prefab.GetComponent<RoboClashRobotAbilities>() == null ||
                    prefab.GetComponent<RoboClashRobotAI>() == null ||
                    prefab.GetComponent<CharacterController>() == null)
                {
                    message = $"Robot prefab '{prefab.name}' is missing a required gameplay component.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        public string GetScoreboardText()
        {
            var ordered = new List<Participant>(participants);
            ordered.Sort((left, right) =>
            {
                if (settings.gameMode == RoboClashGameMode.Multiplayer)
                    return left.PlayerIndex.CompareTo(right.PlayerIndex);
                var scoreComparison = settings.gameMode == RoboClashGameMode.KingOfTheArena
                    ? right.KingScore.CompareTo(left.KingScore)
                    : right.Kills.CompareTo(left.Kills);
                return scoreComparison != 0 ? scoreComparison : string.CompareOrdinal(left.Id, right.Id);
            });

            var lines = new List<string>(ordered.Count);
            foreach (var participant in ordered)
            {
                var definition = participant.Controller != null
                    ? participant.Controller.GetComponent<RoboClashRobotDefinition>()
                    : null;
                var name = definition != null ? definition.robotType.ToString().ToUpperInvariant() : participant.Id;
                var line = settings.gameMode == RoboClashGameMode.Multiplayer
                    ? $"PLAYER {participant.PlayerIndex + 1}  {name}  {participant.Kills}K / {participant.Deaths}D"
                    : $"{(participant.IsPlayer ? "> " : string.Empty)}{name}  {participant.Kills}K / {participant.Deaths}D";
                if (settings.gameMode == RoboClashGameMode.Knockout)
                    line += $"  {participant.LivesRemaining}L";
                else if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
                    line += $"  {Mathf.FloorToInt(participant.KingScore)}s";
                lines.Add(line);
            }
            if (settings.gameMode == RoboClashGameMode.Survival)
                lines.AddRange(archivedScoreRows);

            return string.Join("\n", lines);
        }

        public int GetKingScore(ArenaDamageable participant)
        {
            var entry = GetParticipant(participant);
            return entry != null ? Mathf.FloorToInt(entry.KingScore) : 0;
        }

        private void CreateKingZone()
        {
            if (kingZone != null)
            {
                kingZone.ClearOccupants();
                return;
            }

            var zoneObject = new GameObject("King of the Arena Control Zone");
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = Vector3.zero;
            kingZone = zoneObject.AddComponent<KingOfArenaZone>();
            var trigger = zoneObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * 1.5f;
            trigger.size = new Vector3(10f, 3f, 10f);

            var zoneShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (zoneShader == null)
            {
                Debug.LogError("King of the Arena zone visuals could not be created: no supported material shader was found.",
                    this);
                return;
            }
            kingZoneMaterial = new Material(zoneShader) { name = "RoboClash King Zone" };
            kingZoneMaterial.color = new Color(0.04f, 0.62f, 0.85f, 1f);
            if (kingZoneMaterial.HasProperty("_EmissionColor"))
            {
                kingZoneMaterial.EnableKeyword("_EMISSION");
                kingZoneMaterial.SetColor("_EmissionColor", new Color(0.02f, 0.35f, 0.65f));
            }

            CreateKingZoneVisual("Control Zone Platform", zoneObject.transform,
                Vector3.up * 0.22f, new Vector3(10f, 0.12f, 10f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone North Marker", zoneObject.transform,
                new Vector3(0f, 0.24f, 4.75f), new Vector3(10f, 0.12f, 0.5f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone South Marker", zoneObject.transform,
                new Vector3(0f, 0.24f, -4.75f), new Vector3(10f, 0.12f, 0.5f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone East Marker", zoneObject.transform,
                new Vector3(4.75f, 0.24f, 0f), new Vector3(0.5f, 0.12f, 10f), kingZoneMaterial);
            CreateKingZoneVisual("Control Zone West Marker", zoneObject.transform,
                new Vector3(-4.75f, 0.24f, 0f), new Vector3(0.5f, 0.12f, 10f), kingZoneMaterial);
        }

        private static void CreateKingZoneVisual(
            string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = size;
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void UpdateKingControl()
        {
            if (settings.gameMode != RoboClashGameMode.KingOfTheArena || kingZone == null)
                return;

            ArenaDamageable sole = null;
            kingContested = false;
            foreach (var occupant in kingZone.Occupants)
            {
                if (occupant == null || occupant.IsDead || !IsParticipant(occupant))
                    continue;

                if (sole != null)
                {
                    kingContested = true;
                    sole = null;
                    break;
                }

                sole = occupant;
            }

            kingController = sole;
            if (sole == null)
                return;
            var participant = FindParticipant(sole);
            if (participant == null)
                return;
            participant.KingScore += Time.deltaTime;
        }

        private bool IsTimedMatch()
        {
            return settings.gameMode == RoboClashGameMode.FreeForAll ||
                   settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                   settings.gameMode == RoboClashGameMode.KingOfTheArena ||
                   settings.gameMode == RoboClashGameMode.Knockout ||
                   (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget);
        }

        private bool IsKillTargetMode()
        {
            return settings.gameMode == RoboClashGameMode.KillRace ||
                   settings.gameMode == RoboClashGameMode.Multiplayer ||
                   (settings.gameMode == RoboClashGameMode.Duel && settings.duelUsesKillTarget);
        }

        public void PauseMatch()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            cursorLockBeforePause = Cursor.lockState;
            cursorVisibleBeforePause = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hasPausedTime = true;
            SetParticipantInputLock(true);
            SetParticipantSimulationPaused(true);
            Time.timeScale = 0f;
            SetState(RoboClashMatchState.Paused);
            Debug.Log("RoboClash match paused.", this);
        }

        public void ResumeMatch()
        {
            if (State != RoboClashMatchState.Paused)
                return;

            Time.timeScale = hasPausedTime ? timeScaleBeforePause : 1f;
            hasPausedTime = false;
            SetState(RoboClashMatchState.Playing);
            SetParticipantInputLock(false);
            SetParticipantSimulationPaused(false);
            RestoreCursorState();
            if (player != null && !player.IsDead)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            Debug.Log("RoboClash match resumed.", this);
        }

        public void RestartMatch()
        {
            if (State == RoboClashMatchState.Initializing)
                return;

            Debug.Log("Restarting RoboClash match.", this);
            if (hasPausedTime)
            {
                Time.timeScale = timeScaleBeforePause;
                hasPausedTime = false;
                RestoreCursorState();
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }
            if (eliminationRoutine != null)
            {
                StopCoroutine(eliminationRoutine);
                eliminationRoutine = null;
            }
            StopWaveRoutine();

            UnsubscribeParticipants();
            participants.Clear();
            currentWaveEnemies.Clear();
            archivedScoreRows.Clear();
            lastDamageSources.Clear();
            player = null;
            PlayerRegistered?.Invoke(null);
            initializationComplete = false;
            currentWave = 0;
            elapsedMatchSeconds = 0f;
            waveBreakActive = false;
            waveBreakEndsAt = 0f;
            kingController = null;
            kingContested = false;
            kingZone?.ClearOccupants();
            SetState(RoboClashMatchState.Initializing);
            if (participantRoot != null)
                Destroy(participantRoot.gameObject);

            StartCoroutine(RestartAfterCleanup());
        }

        private IEnumerator RestartAfterCleanup()
        {
            yield return null;
            InitializeMatch();
        }

        private void InitializeMatch()
        {
            if (initializationComplete)
                return;

            WinnerPlayerIndex = -1;
            settings.gameMode = SanitizeGameMode(settings.gameMode);
            if (settings.gameMode == RoboClashGameMode.Knockout)
                settings.matchDurationSeconds = KnockoutTimeLimitSeconds;
            if (settings.gameMode == RoboClashGameMode.Multiplayer &&
                !hasOriginalShadowDistance)
            {
                originalShadowDistance = QualitySettings.shadowDistance;
                hasOriginalShadowDistance = true;
            }
            if (!ValidateConfiguration(out var configurationIssue))
            {
                Debug.LogError("RoboClash match initialization failed: " + configurationIssue, this);
                return;
            }

            playerCamera = FindMainCamera();
            if (playerCamera == null)
            {
                Debug.LogError("RoboClash match initialization failed: exactly one active MainCamera is required.", this);
                return;
            }

            var zones = FindSpawnZones();
            if (!HasPlayableSurface(zones))
            {
                Debug.LogError("RoboClash match initialization failed: no arena floor or spawn zones were found.", this);
                return;
            }

            if (settings.gameMode == RoboClashGameMode.Multiplayer)
                QualitySettings.shadowDistance = Mathf.Min(originalShadowDistance, 35f);

            Debug.Log($"Initializing RoboClash {settings.gameMode} match.", this);
            if (Time.timeScale <= 0f)
                Time.timeScale = 1f;
            DetachCameraFromRobot();
            participantRoot = new GameObject("RoboClash Match Participants").transform;
            participantRoot.SetParent(transform.root, false);
            DeactivateExistingRobotInstances();
            BuildNavigationMesh();
            BuildSpawnCandidates(zones);
            nextParticipantId = 0;
            kingController = null;
            kingContested = false;
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
            {
                CreateKingZone();
                ExcludeKingZoneSpawnCandidates();
            }
            statistics = default;
            statistics.SetGameMode(settings.gameMode);
            remainingTime = settings.matchDurationSeconds;
            elapsedMatchSeconds = 0f;
            currentWave = settings.gameMode == RoboClashGameMode.Survival ? 1 : 0;
            waveBreakActive = false;
            countdownValue = 0;
            eliminationEvaluationPending = false;
            currentWaveEnemies.Clear();
            archivedScoreRows.Clear();
            try
            {
                var opponentCount = GetInitialOpponentCount();
                var opponentTypes = settings.gameMode == RoboClashGameMode.Multiplayer
                    ? GetMultiplayerRobotTypes()
                    : GetOpponentRobotTypes(opponentCount);
                var spawnPositions = GetSpawnPositions(opponentTypes);
                if (spawnPositions == null)
                {
                    Debug.LogError(
                        "RoboClash match initialization failed: could not find enough safe, distinct spawn positions.",
                        this);
                    return;
                }

                SpawnParticipants(spawnPositions, opponentTypes);
                var expectedParticipantCount = settings.gameMode == RoboClashGameMode.Multiplayer
                    ? settings.localPlayerCount
                    : opponentCount + 1;
                if (participants.Count != expectedParticipantCount || player == null)
                {
                    Debug.LogError(
                        "RoboClash match initialization failed: one or more participants could not be created.",
                        this);
                    return;
                }

                if (settings.gameMode == RoboClashGameMode.Multiplayer &&
                    settings.localPlayerCount >= 2)
                    SetupMultiplayerPresentation();

                initializationComplete = true;
                if (settings.gameMode == RoboClashGameMode.Survival)
                {
                    statistics.RecordWave(currentWave);
                    StatisticsChanged?.Invoke(statistics);
                    ModeStatusChanged?.Invoke();
                }
                MatchTimerChanged?.Invoke(remainingTime);
                StatisticsChanged?.Invoke(statistics);
                SetState(RoboClashMatchState.Ready);
                Debug.Log(settings.gameMode == RoboClashGameMode.Multiplayer
                    ? $"Spawned {participants.Count} local human players."
                    : $"Spawned the player and {opponentCount} AI opponent(s).", this);
                countdownRoutine = StartCoroutine(RunCountdown());
            }
            finally
            {
                if (!initializationComplete)
                {
                    UnsubscribeParticipants();
                    participants.Clear();
                    player = null;
                    if (participantRoot != null)
                        Destroy(participantRoot.gameObject);
                    participantRoot = null;
                    RestorePreviousSceneObjects();
                }
            }
        }

        private void BuildNavigationMesh()
        {
            if (navSurface != null && navSurface.navMeshData != null)
                return;

            foreach (var lava in FindObjectsByType<LavaPit>())
            {
                if (lava.gameObject.scene == gameObject.scene)
                    AddBlockedVolume(lava.gameObject);
            }
            foreach (var spikes in FindObjectsByType<SpikeTrap>())
            {
                if (spikes.gameObject.scene == gameObject.scene)
                    AddBlockedVolume(spikes.gameObject);
            }

            navSurface = gameObject.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();
        }

        private static void AddBlockedVolume(GameObject hazard)
        {
            var box = hazard.GetComponent<BoxCollider>();
            if (box == null || hazard.GetComponent<NavMeshModifierVolume>() != null)
                return;

            var volume = hazard.AddComponent<NavMeshModifierVolume>();
            volume.center = box.center;
            volume.size = box.size;
            volume.area = 1;
        }

        private void SpawnParticipants(Vector3[] spawnPositions, RoboClashRobotType[] opponentTypes)
        {
            var zones = FindSpawnZones();
            if (settings.gameMode == RoboClashGameMode.Multiplayer)
            {
                for (var i = 0; i < settings.localPlayerCount; i++)
                {
                    var type = settings.multiplayerRobots[i];
                    SpawnParticipant(type, spawnPositions[i],
                        FindNearestSpawnZone(zones, spawnPositions[i]), true, false, i);
                }
                return;
            }

            SpawnParticipant(settings.playerRobotType, spawnPositions[0],
                FindNearestSpawnZone(zones, spawnPositions[0]), true);
            for (var i = 0; i < opponentTypes.Length; i++)
            {
                var zone = FindNearestSpawnZone(zones, spawnPositions[i + 1]);
                var damageable = SpawnParticipant(opponentTypes[i], spawnPositions[i + 1], zone, false);
                if (settings.gameMode == RoboClashGameMode.Survival && damageable != null)
                    currentWaveEnemies.Add(damageable);
            }
        }

        private Camera CreateLocalPlayerCamera(GameObject robot, int playerIndex)
        {
            if (playerIndex == 0)
                return playerCamera;

            var cameraObject = new GameObject($"Player {playerIndex + 1} Camera",
                typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(robot.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.CopyFrom(playerCamera);
            camera.enabled = true;
            camera.targetTexture = null;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            var listener = cameraObject.GetComponent<AudioListener>();
            listener.enabled = false;
            DisableCameraPostProcessing(camera);
            return camera;
        }

        private static void DisableCameraPostProcessing(Camera camera)
        {
            if (camera == null)
                return;

            var additionalData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalData == null)
                additionalData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.renderPostProcessing = false;
        }

        private void SetupMultiplayerPresentation()
        {
            var playerMovements = new List<RoboClashRobotMovement>(settings.localPlayerCount);
            for (var i = 0; i < settings.localPlayerCount; i++)
                playerMovements.Add(null);
            foreach (var participant in participants)
            {
                if (participant == null || !participant.IsPlayer || participant.Controller == null)
                    continue;

                var movement = participant.Controller.GetComponent<RoboClashRobotMovement>();
                if (participant.PlayerIndex >= 0 &&
                    participant.PlayerIndex < playerMovements.Count)
                    playerMovements[participant.PlayerIndex] = movement;
                DisableCameraPostProcessing(movement != null ? movement.PlayerCamera : null);
            }

            if (splitScreenManager == null)
                splitScreenManager = GetComponent<RoboClashSplitScreenManager>();
            if (splitScreenManager == null)
                splitScreenManager = gameObject.AddComponent<RoboClashSplitScreenManager>();
            splitScreenManager.Setup(playerMovements, this);

            var globalHud = GetComponent<RoboClash.UI.RoboClashHUD>();
            globalHud?.ConfigureMultiplayerGlobalOverlay();

            foreach (var oldHud in multiplayerHudObjects)
            {
                if (oldHud == null)
                    continue;
                oldHud.SetActive(false);
                Destroy(oldHud);
            }
            multiplayerHudObjects.Clear();

            foreach (var movement in playerMovements)
            {
                if (movement == null || movement.PlayerCamera == null)
                    continue;

                var index = movement.GetComponent<RoboClashPlayerInputRouter>()?.PlayerIndex ?? -1;
                if (index < 0)
                    continue;

                var hudObject = new GameObject($"Player {index + 1} HUD");
                hudObject.transform.SetParent(transform, false);
                multiplayerHudObjects.Add(hudObject);
                var hud = hudObject.AddComponent<RoboClash.UI.RoboClashHUD>();
                hud.ConfigureMultiplayerPlayer(GetPlayerController(index),
                    movement.PlayerCamera, index);
            }
        }

        private ArenaDamageable SpawnParticipant(RoboClashRobotType type, Vector3 position,
            ArenaSpawnZone spawnZone, bool isPlayer, bool facePlayer = false, int playerIndex = 0)
        {
            var prefab = robotPrefabs[(int)type];
            if (prefab == null)
            {
                Debug.LogError($"Cannot spawn {type}: its generated prefab reference is missing.", this);
                return null;
            }

            var spawnTarget = facePlayer && player != null
                ? player.transform.position
                : GetArenaCenter();
            var spawnRotation = RoboClashSpawnPlanner.FaceTarget(position, spawnTarget);
            var robot = Instantiate(prefab, position, spawnRotation, participantRoot);
            var participantIndex = participants.Count;
            robot.name = isPlayer
                ? settings.gameMode == RoboClashGameMode.Multiplayer
                    ? $"Player {playerIndex + 1} {type.ToString().ToUpperInvariant()}"
                    : $"Player {type.ToString().ToUpperInvariant()}" :
                $"AI Wave {Mathf.Max(1, currentWave)} {participantIndex} {type.ToString().ToUpperInvariant()}";

            var controller = robot.GetComponent<RobotArenaController>();
            var definition = robot.GetComponent<RoboClashRobotDefinition>();
            var movement = robot.GetComponent<RoboClashRobotMovement>();
            var combat = robot.GetComponent<RoboClashRobotCombat>();
            var abilities = robot.GetComponent<RoboClashRobotAbilities>();
            var ai = robot.GetComponent<RoboClashRobotAI>();
            var damageable = robot.GetComponent<ArenaDamageable>();
            var inputRouter = robot.GetComponent<RoboClashPlayerInputRouter>();
            if (inputRouter == null)
                inputRouter = robot.AddComponent<RoboClashPlayerInputRouter>();
            inputRouter.PlayerIndex = isPlayer ? playerIndex : -1;
            inputRouter.Source = isPlayer
                ? settings.gameMode == RoboClashGameMode.Multiplayer
                    ? RoboClashInputAssignment.For(playerIndex, settings.localPlayerCount)
                    : new KeyboardMouseInputSource()
                : null;
            damageable.GrantSpawnProtection(2f);

            definition.teamId = participantIndex;
            switch (settings.gameMode)
            {
                case RoboClashGameMode.KillRace:
                case RoboClashGameMode.TimedDeathmatch:
                case RoboClashGameMode.Multiplayer:
                    controller.ConfigureUnlimitedMatchRespawns();
                    break;
                case RoboClashGameMode.Duel:
                    if (settings.duelRespawnsEnabled)
                        controller.ConfigureUnlimitedMatchRespawns();
                    else
                        controller.ConfigureMatchRespawns(0);
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    if (settings.respawnEnabled)
                        controller.ConfigureUnlimitedMatchRespawns();
                    else
                        controller.ConfigureMatchRespawns(0);
                    break;
                case RoboClashGameMode.Knockout:
                    controller.ConfigureMatchRespawns(settings.knockoutLives - 1);
                    break;
                default:
                    var allowRespawns = settings.gameMode != RoboClashGameMode.Survival &&
                                        settings.respawnEnabled;
                    controller.ConfigureMatchRespawns(allowRespawns ? settings.maximumRespawns : 0);
                    break;
            }
            var participantCamera = isPlayer
                ? settings.gameMode == RoboClashGameMode.Multiplayer
                    ? CreateLocalPlayerCamera(robot, playerIndex)
                    : playerCamera
                : null;
            controller.Configure(participantCamera,
                definition.muzzle, spawnZone);
            controller.ConfigureMatchRespawnLocation(position, spawnRotation);
            movement.SetMatchInputLocked(true);
            movement.SetControlled(isPlayer);
            combat.ConfigurePlayer(isPlayer);
            if (isPlayer && robot.GetComponent<RoboClashGuardMode>() == null)
                robot.AddComponent<RoboClashGuardMode>();
            if (!isPlayer)
            {
                var difficulty = GetAIDifficultyForCurrentWave();
                ai.Configure(this, difficulty, GetAIDifficultyProfile(difficulty));
            }
            ai.enabled = !isPlayer;

            var participant = new Participant
            {
                Id = $"participant-{nextParticipantId++:D2}",
                Controller = controller,
                Damageable = damageable,
                IsPlayer = isPlayer,
                PlayerIndex = isPlayer ? playerIndex : -1,
                LivesRemaining = settings.gameMode == RoboClashGameMode.Knockout
                    ? settings.knockoutLives
                    : int.MaxValue,
                AIEnabled = ai.enabled,
                AbilitiesEnabled = abilities.enabled,
                CombatEnabled = combat.enabled
            };
            participants.Add(participant);
            damageable.DamageApplied += OnParticipantDamageApplied;
            damageable.Eliminated += OnParticipantEliminated;
            controller.Respawned += OnParticipantRespawned;

            if (isPlayer && (settings.gameMode != RoboClashGameMode.Multiplayer || playerIndex == 0))
            {
                player = controller;
                PlayerRegistered?.Invoke(controller);
            }

            Debug.Log(settings.gameMode == RoboClashGameMode.Multiplayer
                    ? $"Spawned player {playerIndex + 1} ({type}) at {robot.transform.position}."
                    : $"Spawned {(isPlayer ? "player" : "AI opponent")} {type} at {robot.transform.position}.",
                robot);
            return damageable;
        }

        public bool TryGetRespawnLocation(RobotArenaController controller, out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            var participant = FindParticipant(controller);
            var definition = controller != null ? controller.GetComponent<RoboClashRobotDefinition>() : null;
            if (participant == null || definition == null || spawnCandidates.Count == 0)
                return false;

            var safeCandidates = new List<Vector3>(spawnCandidates.Count);
            foreach (var candidate in spawnCandidates)
            {
                if (IsSafeSpawn(candidate, definition.robotType))
                    safeCandidates.Add(candidate);
            }

            if (safeCandidates.Count == 0)
                return false;

            var enemyPositions = new List<Vector3>();
            foreach (var other in participants)
            {
                if (other == participant || !IsLiving(other) ||
                    !IsValidOpponent(controller, other.Damageable))
                    continue;
                enemyPositions.Add(other.Damageable.transform.position);
            }

            position = RoboClashSpawnPlanner.PickRespawn(safeCandidates, enemyPositions,
                controller.transform.position);
            var target = GetArenaCenter();
            var nearestEnemyDistance = float.PositiveInfinity;
            foreach (var enemyPosition in enemyPositions)
            {
                var distance = (enemyPosition - position).sqrMagnitude;
                if (distance >= nearestEnemyDistance)
                    continue;
                nearestEnemyDistance = distance;
                target = enemyPosition;
            }

            rotation = RoboClashSpawnPlanner.FaceTarget(position, target);
            return true;
        }

        private static ArenaSpawnZone FindNearestSpawnZone(ArenaSpawnZone[] zones, Vector3 position)
        {
            ArenaSpawnZone nearest = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var zone in zones)
            {
                var distance = (zone.transform.position - position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearest = zone;
                nearestDistance = distance;
            }

            return nearest;
        }

        private IEnumerator RunCountdown()
        {
            SetState(RoboClashMatchState.Countdown);
            var elapsed = 0f;
            var previousValue = -1;
            while (elapsed < settings.countdownDurationSeconds)
            {
                var value = Mathf.CeilToInt(settings.countdownDurationSeconds - elapsed);
                if (value != previousValue)
                {
                    countdownValue = value;
                    CountdownChanged?.Invoke(value);
                    previousValue = value;
                }

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            countdownValue = 0;
            CountdownChanged?.Invoke(0);
            SetState(RoboClashMatchState.Playing);
            foreach (var participant in participants)
            {
                if (IsLiving(participant))
                    participant.Damageable.GrantSpawnProtection(2f);
            }
            SetParticipantInputLock(false);
            countdownRoutine = null;
            Debug.Log("RoboClash match started.", this);
            MatchStarted?.Invoke();
        }

        private void OnParticipantDamageApplied(ArenaDamageable target, float amount, ArenaDamageable instigator)
        {
            if (State != RoboClashMatchState.Playing || !IsParticipant(target))
                return;

            if (instigator != null)
                lastDamageSources[target] = instigator;
            else
                lastDamageSources.Remove(target);

            if (settings.gameMode != RoboClashGameMode.Multiplayer && instigator == player)
                statistics.RecordDamageDealt(amount);
            if (settings.gameMode != RoboClashGameMode.Multiplayer && target == player)
                statistics.RecordDamageReceived(amount);

            StatisticsChanged?.Invoke(statistics);
        }

        private void OnParticipantEliminated(ArenaDamageable eliminated)
        {
            if (State != RoboClashMatchState.Playing || !IsParticipant(eliminated))
                return;

            var participant = FindParticipant(eliminated);
            if (participant == null || participant.DeathProcessed)
                return;

            participant.DeathProcessed = true;
            participant.Deaths++;
            var isMultiplayer = settings.gameMode == RoboClashGameMode.Multiplayer;
            if (settings.gameMode == RoboClashGameMode.Knockout)
                participant.LivesRemaining = Mathf.Max(0, participant.LivesRemaining - 1);

            CancelProjectilesOwnedBy(eliminated.gameObject);
            lastDamageSources.TryGetValue(eliminated, out var killer);
            var creditedKiller = killer != null && killer != eliminated
                ? FindParticipant(killer)
                : null;
            var killTargetReached = false;
            if (creditedKiller != null && creditedKiller != participant)
            {
                creditedKiller.Kills++;
                killTargetReached = IsKillTargetMode() &&
                                    creditedKiller.Kills >= settings.killTarget;
            }
            else
                creditedKiller = null;

            participant.IsEliminated = !participant.Controller.IsRespawning;
            ParticipantEliminated?.Invoke(eliminated, creditedKiller != null ? creditedKiller.Damageable : null);
            if (participant.IsPlayer && !isMultiplayer)
            {
                statistics.RecordPlayerDeath();
                if (settings.gameMode == RoboClashGameMode.FreeForAll)
                {
                    var survivingCombatants = CountAliveCombatants();
                    if (survivingCombatants > 0)
                        statistics.SetPlayerPlacement(survivingCombatants + 1);
                }
            }
            if (creditedKiller != null && creditedKiller.IsPlayer && !participant.IsPlayer &&
                !isMultiplayer)
                statistics.RecordElimination();

            lastDamageSources.Remove(eliminated);
            StatisticsChanged?.Invoke(statistics);
            ModeStatusChanged?.Invoke();
            Debug.Log($"Participant '{eliminated.name}' was eliminated" +
                      (creditedKiller != null ? $" by '{creditedKiller.Damageable.name}'." : " without kill credit."),
                eliminated);
            if (killTargetReached)
            {
                if (isMultiplayer)
                {
                    WinnerPlayerIndex = creditedKiller.PlayerIndex;
                    EndMatch(RoboClashMatchState.Victory);
                    return;
                }
                EndMatch(creditedKiller.IsPlayer
                    ? RoboClashMatchState.Victory
                    : RoboClashMatchState.Defeat);
                return;
            }
            if (!participant.Controller.IsRespawning || IsKillTargetMode())
                ScheduleEliminationEvaluation();
        }

        private void OnParticipantRespawned(RobotArenaController controller)
        {
            var participant = controller != null ? FindParticipant(controller) : null;
            if (participant == null || State != RoboClashMatchState.Playing)
                return;

            participant.DeathProcessed = false;
            participant.IsEliminated = false;
            lastDamageSources.Remove(participant.Damageable);
            ModeStatusChanged?.Invoke();
        }

        private void ScheduleEliminationEvaluation()
        {
            if (eliminationEvaluationPending || State != RoboClashMatchState.Playing)
                return;

            eliminationEvaluationPending = true;
            eliminationRoutine = StartCoroutine(EvaluateEliminationsAtFrameEnd());
        }

        private IEnumerator EvaluateEliminationsAtFrameEnd()
        {
            yield return new WaitForEndOfFrame();
            eliminationRoutine = null;
            eliminationEvaluationPending = false;
            EvaluateEliminationResult();
        }

        private void EvaluateEliminationResult()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            if (settings.gameMode == RoboClashGameMode.Multiplayer)
                return;

            if (IsKillTargetMode())
            {
                ResolveKillTarget();
                if (State == RoboClashMatchState.Playing && settings.gameMode == RoboClashGameMode.Duel &&
                    !settings.duelRespawnsEnabled)
                {
                    if (CountAliveCombatants() == 0)
                        EndMatch(RoboClashMatchState.Draw);
                    else if (CountAliveCombatants() == 1)
                        EndMatch(IsPlayerAlive() ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
                }
                return;
            }

            if (settings.gameMode == RoboClashGameMode.Survival)
            {
                if (!IsPlayerAlive())
                    EndMatch(RoboClashMatchState.Defeat);
                else if (CountLivingWaveEnemies() == 0 && !waveBreakActive)
                    StartWaveBreak();
                return;
            }

            if (settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                (settings.gameMode == RoboClashGameMode.KingOfTheArena && settings.respawnEnabled))
                return;

            if (settings.gameMode == RoboClashGameMode.Knockout)
            {
                var remaining = 0;
                var playerSurvives = false;
                foreach (var participant in participants)
                {
                    if (participant.IsEliminated)
                        continue;
                    remaining++;
                    playerSurvives = participant.IsPlayer;
                }

                if (remaining == 0)
                    EndMatch(RoboClashMatchState.Draw);
                else if (remaining == 1)
                    EndMatch(playerSurvives ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
                return;
            }

            if (!EndsOnLastStanding())
                return;

            var playerAlive = IsPlayerAlive();
            var aliveOpponents = CountAliveOpponents();
            var aliveCombatants = CountAliveCombatants();
            if (settings.gameMode == RoboClashGameMode.FreeForAll && aliveCombatants > 1)
                return;

            if (aliveCombatants == 0)
                EndMatch(RoboClashMatchState.Draw);
            else if (playerAlive)
                EndMatch(RoboClashMatchState.Victory);
            else if (aliveOpponents == 0)
                EndMatch(RoboClashMatchState.Draw);
            else
                EndMatch(RoboClashMatchState.Defeat);
        }

        private void ResolveKillTarget()
        {
            Participant winner = null;
            foreach (var participant in participants)
            {
                if (participant.Kills < settings.killTarget)
                    continue;

                if (winner == null || participant.Kills > winner.Kills ||
                    (participant.Kills == winner.Kills &&
                     string.CompareOrdinal(participant.Id, winner.Id) < 0))
                    winner = participant;
            }

            if (winner != null)
                EndMatch(winner.IsPlayer ? RoboClashMatchState.Victory : RoboClashMatchState.Defeat);
        }

        private void StartWaveBreak()
        {
            if (settings.gameMode != RoboClashGameMode.Survival || waveBreakActive ||
                waveRoutine != null || State != RoboClashMatchState.Playing)
                return;

            waveBreakActive = true;
            waveBreakEndsAt = Time.time + settings.waveBreakSeconds;
            statistics.RecordWaveCompleted(currentWave);
            StatisticsChanged?.Invoke(statistics);
            RemoveCompletedWaveParticipants();
            ModeStatusChanged?.Invoke();
            waveRoutine = StartCoroutine(SpawnNextSurvivalWave());
        }

        private void RemoveCompletedWaveParticipants()
        {
            foreach (var enemy in currentWaveEnemies)
            {
                var participant = FindParticipant(enemy);
                if (participant == null || IsAlive(participant))
                    continue;
                var definition = participant.Controller != null
                    ? participant.Controller.GetComponent<RoboClashRobotDefinition>()
                    : null;
                var name = definition != null ? definition.robotType.ToString().ToUpperInvariant() : participant.Id;
                archivedScoreRows.Add($"{name}  {participant.Kills}K / {participant.Deaths}D");
                if (participant.Damageable != null)
                {
                    participant.Damageable.DamageApplied -= OnParticipantDamageApplied;
                    participant.Damageable.Eliminated -= OnParticipantEliminated;
                }
                if (participant.Controller != null)
                    participant.Controller.Respawned -= OnParticipantRespawned;
                participants.Remove(participant);
                if (participant.Controller != null)
                    Destroy(participant.Controller.gameObject);
            }

            currentWaveEnemies.Clear();
            lastDamageSources.Clear();
        }

        private IEnumerator SpawnNextSurvivalWave()
        {
            if (settings.waveBreakSeconds > 0f)
                yield return new WaitForSeconds(settings.waveBreakSeconds);
            else
                yield return null;

            waveRoutine = null;
            if (State != RoboClashMatchState.Playing || !IsPlayerAlive())
            {
                waveBreakActive = false;
                ModeStatusChanged?.Invoke();
                yield break;
            }

            currentWave++;
            var requestedCount = (long)settings.startingSurvivalEnemies +
                                 (long)(currentWave - 1) * settings.additionalEnemiesPerWave;
            var enemyCount = (int)Math.Min(settings.maximumSurvivalEnemiesPerWave, requestedCount);
            var enemyTypes = GetOpponentRobotTypes(enemyCount);
            var zones = FindSpawnZones();
            if (!TryGetSurvivalSpawnPositions(enemyTypes, out var positions))
            {
                waveBreakActive = false;
                Debug.LogError(
                    $"Survival wave {currentWave} could not find {enemyCount} safe, separated spawn positions. " +
                    "Reduce the wave size or add safe arena spawn areas.",
                    this);
                ModeStatusChanged?.Invoke();
                EndMatch(RoboClashMatchState.Defeat);
                yield break;
            }

            currentWaveEnemies.Clear();
            for (var i = 0; i < enemyCount; i++)
            {
                if (State != RoboClashMatchState.Playing || !IsPlayerAlive())
                    break;

                var zone = FindNearestSpawnZone(zones, positions[i]);
                var spawnedEnemy = SpawnParticipant(enemyTypes[i], positions[i], zone, false, true);
                if (spawnedEnemy != null)
                    currentWaveEnemies.Add(spawnedEnemy);
                if (i + 1 < enemyCount)
                    yield return new WaitForSeconds(0.15f);
            }

            if (currentWaveEnemies.Count != enemyCount)
            {
                Debug.LogError($"Survival wave {currentWave} spawned {currentWaveEnemies.Count} of " +
                               $"{enemyCount} requested enemies.", this);
                waveBreakActive = false;
                ModeStatusChanged?.Invoke();
                EndMatch(RoboClashMatchState.Defeat);
                yield break;
            }

            SetParticipantInputLock(false);
            waveBreakActive = false;
            UpdateActiveAIDifficulty();
            statistics.RecordWave(currentWave);
            StatisticsChanged?.Invoke(statistics);
            ModeStatusChanged?.Invoke();
            Debug.Log($"Survival wave {currentWave} started with {enemyCount} AI opponent(s).", this);
        }

        private void StopWaveRoutine()
        {
            if (waveRoutine == null)
                return;

            StopCoroutine(waveRoutine);
            waveRoutine = null;
            waveBreakActive = false;
            waveBreakEndsAt = 0f;
        }

        private int CountLivingWaveEnemies()
        {
            var count = 0;
            foreach (var enemy in currentWaveEnemies)
            {
                var participant = FindParticipant(enemy);
                if (participant != null && IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int GetInitialOpponentCount()
        {
            switch (settings.gameMode)
            {
                case RoboClashGameMode.Multiplayer:
                    return 0;
                case RoboClashGameMode.Survival:
                    return Mathf.Clamp(settings.startingSurvivalEnemies, 1,
                        settings.maximumSurvivalEnemiesPerWave);
                default:
                    return Mathf.Clamp(settings.aiOpponents, 1, 7);
            }
        }

        private RoboClashAIDifficulty GetAIDifficultyForCurrentWave()
        {
            if (settings.gameMode != RoboClashGameMode.Survival)
                return settings.aiDifficulty;

            if (settings.aiDifficulty == RoboClashAIDifficulty.Easy)
                return currentWave >= 10 ? RoboClashAIDifficulty.Hard :
                    currentWave >= 5 ? RoboClashAIDifficulty.Normal : RoboClashAIDifficulty.Easy;
            if (settings.aiDifficulty == RoboClashAIDifficulty.Normal && currentWave >= 5)
                return RoboClashAIDifficulty.Hard;
            return settings.aiDifficulty;
        }

        private void UpdateActiveAIDifficulty()
        {
            var difficulty = GetAIDifficultyForCurrentWave();
            var profile = GetAIDifficultyProfile(difficulty);
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsAlive(participant))
                    continue;
                var ai = participant.Controller.GetComponent<RoboClashRobotAI>();
                if (ai != null && ai.enabled)
                    ai.Configure(this, difficulty, profile);
            }
        }

        private bool TryGetSurvivalSpawnPositions(RoboClashRobotType[] enemyTypes,
            out Vector3[] positions)
        {
            positions = null;
            if (player == null || enemyTypes == null || enemyTypes.Length == 0)
                return false;

            var minimumPlayerDistance = 18f;
            var candidates = GetSurvivalSpawnCandidates(minimumPlayerDistance);
            if (candidates.Count < enemyTypes.Length)
            {
                minimumPlayerDistance = 10f;
                candidates = GetSurvivalSpawnCandidates(minimumPlayerDistance);
            }

            if (candidates.Count == 0)
                return false;

            var picked = RoboClashSpawnPlanner.PickSpread(candidates, enemyTypes.Length, 12f);
            positions = picked.ToArray();
            for (var i = 0; i < positions.Length; i++)
            {
                if (!IsSafeSpawn(positions[i], enemyTypes[i]) ||
                    IsPositionWithinDistance(positions[i], player.transform.position,
                        minimumPlayerDistance) ||
                    IsNearReservedPosition(positions[i], enemyTypes[i], positions, i, enemyTypes))
                {
                    positions = null;
                    return false;
                }
            }

            return true;
        }

        private List<Vector3> GetSurvivalSpawnCandidates(float minimumPlayerDistance)
        {
            var result = new List<Vector3>(spawnCandidates.Count);
            if (player == null)
                return result;

            foreach (var candidate in spawnCandidates)
            {
                if (IsPositionWithinDistance(candidate, player.transform.position, minimumPlayerDistance) ||
                    !IsSafeSpawn(candidate, RoboClashRobotType.Titan))
                    continue;
                result.Add(candidate);
            }

            return result;
        }

        private static bool IsPositionWithinDistance(Vector3 first, Vector3 second, float distance)
        {
            first.y = 0f;
            second.y = 0f;
            return (first - second).sqrMagnitude < distance * distance;
        }

        private static bool IsNearReservedPosition(Vector3 position, RoboClashRobotType type,
            Vector3[] reserved, int reservedCount, RoboClashRobotType[] types)
        {
            var radius = RoboClashRobotDefinition.GetTargetDimensions(type).x * 0.5f;
            for (var i = 0; i < reservedCount; i++)
            {
                var otherRadius = RoboClashRobotDefinition.GetTargetDimensions(types[i]).x * 0.5f;
                var minimumDistance = radius + otherRadius + 1.5f;
                if ((reserved[i] - position).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private void ResolveTimeout()
        {
            if (State != RoboClashMatchState.Playing)
                return;

            remainingTime = 0f;
            MatchTimerChanged?.Invoke(remainingTime);
            if (settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                (settings.gameMode == RoboClashGameMode.Duel && !settings.duelUsesKillTarget))
            {
                ResolveTimedScore(false);
                return;
            }
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
            {
                ResolveTimedScore(true);
                return;
            }
            if (settings.gameMode == RoboClashGameMode.Knockout)
            {
                ResolveKnockoutTimeout();
                return;
            }

            var playerAlive = player != null && !player.IsDead;
            var livingOpponents = CountLivingOpponents();
            if (!playerAlive && livingOpponents == 0)
            {
                EndMatch(RoboClashMatchState.Draw);
                return;
            }
            if (!playerAlive)
            {
                EndMatch(RoboClashMatchState.Defeat);
                return;
            }
            if (livingOpponents == 0)
            {
                EndMatch(RoboClashMatchState.Victory);
                return;
            }

            var playerVitals = player.Health + player.Shield;
            var strongestOpponentVitals = 0f;
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsLiving(participant))
                    continue;
                strongestOpponentVitals = Mathf.Max(strongestOpponentVitals,
                    participant.Damageable.Health + participant.Damageable.Shield);
            }

            if (playerVitals > strongestOpponentVitals)
                EndMatch(RoboClashMatchState.Victory);
            else if (Mathf.Approximately(playerVitals, strongestOpponentVitals))
                EndMatch(RoboClashMatchState.Draw);
            else
                EndMatch(RoboClashMatchState.Defeat);
        }

        private bool EndsOnLastStanding()
        {
            switch (settings.gameMode)
            {
                case RoboClashGameMode.Knockout: return true;
                case RoboClashGameMode.Duel: return !settings.duelRespawnsEnabled;
                case RoboClashGameMode.KingOfTheArena: return !settings.respawnEnabled;
                case RoboClashGameMode.FreeForAll: return true;
                default: return false;
            }
        }

        private void ResolveKnockoutTimeout()
        {
            var highestLives = int.MinValue;
            var leaders = new List<Participant>();
            foreach (var participant in participants)
            {
                if (participant == null || participant.IsEliminated)
                    continue;

                if (participant.LivesRemaining > highestLives)
                {
                    highestLives = participant.LivesRemaining;
                    leaders.Clear();
                    leaders.Add(participant);
                }
                else if (participant.LivesRemaining == highestLives)
                    leaders.Add(participant);
            }

            var highestVitals = float.NegativeInfinity;
            var vitalityLeaders = new List<Participant>();
            foreach (var participant in leaders)
            {
                var vitals = IsLiving(participant)
                    ? participant.Damageable.Health + participant.Damageable.Shield
                    : 0f;
                if (vitals > highestVitals + 0.001f)
                {
                    highestVitals = vitals;
                    vitalityLeaders.Clear();
                    vitalityLeaders.Add(participant);
                }
                else if (Mathf.Abs(vitals - highestVitals) <= 0.001f)
                    vitalityLeaders.Add(participant);
            }

            if (vitalityLeaders.Count == 0)
                EndMatch(RoboClashMatchState.Draw);
            else if (vitalityLeaders.Count > 1)
                EndMatch(vitalityLeaders.Exists(candidate => candidate.IsPlayer)
                    ? RoboClashMatchState.Draw
                    : RoboClashMatchState.Defeat);
            else
                EndMatch(vitalityLeaders[0].IsPlayer
                    ? RoboClashMatchState.Victory
                    : RoboClashMatchState.Defeat);
        }

        private void ResolveTimedScore(bool useKingScore)
        {
            var highest = float.NegativeInfinity;
            var leaders = new List<Participant>();
            foreach (var participant in participants)
            {
                var score = useKingScore ? participant.KingScore : participant.Kills;
                if (score > highest + 0.001f)
                {
                    highest = score;
                    leaders.Clear();
                    leaders.Add(participant);
                }
                else if (Mathf.Abs(score - highest) <= 0.001f)
                    leaders.Add(participant);
            }

            if (leaders.Count == 0)
            {
                EndMatch(RoboClashMatchState.Draw);
                return;
            }

            if (leaders.Count > 1)
                EndMatch(leaders.Exists(candidate => candidate.IsPlayer)
                    ? RoboClashMatchState.Draw
                    : RoboClashMatchState.Defeat);
            else
                EndMatch(leaders[0].IsPlayer
                    ? RoboClashMatchState.Victory
                    : RoboClashMatchState.Defeat);
        }

        private void EndMatch(RoboClashMatchState result)
        {
            if (State != RoboClashMatchState.Playing ||
                (result != RoboClashMatchState.Victory &&
                 result != RoboClashMatchState.Defeat &&
                 result != RoboClashMatchState.Draw))
                return;

            SetParticipantInputLock(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CancelParticipantProjectiles();
            foreach (var participant in participants)
            {
                participant.Controller.LockForMatchEnd();
                participant.Controller.GetComponent<RoboClashRobotAI>().enabled = false;
                participant.Controller.GetComponent<RoboClashRobotAbilities>().enabled = false;
                participant.Controller.GetComponent<RoboClashRobotCombat>().enabled = false;
            }

            var matchDuration = settings.gameMode == RoboClashGameMode.Survival ||
                                !IsTimedMatch()
                ? elapsedMatchSeconds
                : settings.matchDurationSeconds - remainingTime;
            statistics.Complete(matchDuration, result);
            statistics.SetPlayerPlacement(GetPlayerPlacement(result));
            if (settings.gameMode != RoboClashGameMode.Multiplayer)
                RoboClashSaveService.RecordMatch(statistics, settings.playerRobotType, progressionRewards);
            SetState(result);
            StopWaveRoutine();
            StatisticsChanged?.Invoke(statistics);
            Debug.Log($"RoboClash match ended: {result}.", this);
            MatchEnded?.Invoke(statistics);
        }

        private int GetPlayerPlacement(RoboClashMatchState result)
        {
            if (settings.gameMode == RoboClashGameMode.Multiplayer)
                return 0;
            if (result == RoboClashMatchState.Draw)
                return 0;
            if (result == RoboClashMatchState.Victory)
                return 1;
            if (settings.gameMode == RoboClashGameMode.FreeForAll)
                return GetPlayerPlacementByVitals();

            var playerParticipant = FindParticipant(player);
            if (playerParticipant == null)
                return 0;
            if (settings.gameMode == RoboClashGameMode.KingOfTheArena ||
                settings.gameMode == RoboClashGameMode.TimedDeathmatch ||
                IsKillTargetMode())
            {
                var playerScore = settings.gameMode == RoboClashGameMode.KingOfTheArena
                    ? playerParticipant.KingScore
                    : playerParticipant.Kills;
                var placement = 1;
                foreach (var participant in participants)
                {
                    if (participant == playerParticipant)
                        continue;
                    var opponentScore = settings.gameMode == RoboClashGameMode.KingOfTheArena
                        ? participant.KingScore
                        : participant.Kills;
                    if (opponentScore > playerScore)
                        placement++;
                }
                return placement;
            }

            return 2;
        }

        private int GetPlayerPlacementByVitals()
        {
            if (player == null)
                return 0;

            var playerVitals = player.Health + player.Shield;
            var placement = 1;
            foreach (var participant in participants)
            {
                if (participant.IsPlayer || !IsLiving(participant))
                    continue;
                var opponentVitals = participant.Damageable.Health + participant.Damageable.Shield;
                if (opponentVitals > playerVitals)
                    placement++;
            }

            return placement;
        }

        private void SetParticipantInputLock(bool locked)
        {
            foreach (var participant in participants)
            {
                var movement = participant.Controller.GetComponent<RoboClashRobotMovement>();
                if (movement != null)
                    movement.SetMatchInputLocked(locked);
            }
        }

        private void SetParticipantSimulationPaused(bool paused)
        {
            foreach (var participant in participants)
            {
                var ai = participant.Controller.GetComponent<RoboClashRobotAI>();
                var abilities = participant.Controller.GetComponent<RoboClashRobotAbilities>();
                var combat = participant.Controller.GetComponent<RoboClashRobotCombat>();
                if (paused)
                {
                    ai.enabled = false;
                    abilities.enabled = false;
                    combat.enabled = false;
                }
                else
                {
                    ai.enabled = participant.AIEnabled;
                    abilities.enabled = participant.AbilitiesEnabled;
                    combat.enabled = participant.CombatEnabled;
                }
            }
        }

        private void SetState(RoboClashMatchState nextState)
        {
            if (State == nextState)
                return;

            if (!IsTransitionAllowed(State, nextState))
            {
                Debug.LogError($"Invalid RoboClash match state transition: {State} -> {nextState}.", this);
                return;
            }

            State = nextState;
            MatchStateChanged?.Invoke(State);
        }

        private static bool IsTransitionAllowed(RoboClashMatchState current, RoboClashMatchState next)
        {
            if (next == RoboClashMatchState.Initializing)
                return current != RoboClashMatchState.Initializing;

            switch (current)
            {
                case RoboClashMatchState.Initializing:
                    return next == RoboClashMatchState.Ready;
                case RoboClashMatchState.Ready:
                    return next == RoboClashMatchState.Countdown;
                case RoboClashMatchState.Countdown:
                    return next == RoboClashMatchState.Playing;
                case RoboClashMatchState.Playing:
                    return next == RoboClashMatchState.Paused ||
                           next == RoboClashMatchState.Victory ||
                           next == RoboClashMatchState.Defeat ||
                           next == RoboClashMatchState.Draw;
                case RoboClashMatchState.Paused:
                    return next == RoboClashMatchState.Playing;
                default:
                    return false;
            }
        }

        private int CountAliveOpponents()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if ((!participant.IsPlayer ||
                     (settings.gameMode == RoboClashGameMode.Multiplayer && participant.PlayerIndex > 0)) &&
                    IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int CountAliveCombatants()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if (IsAlive(participant))
                    count++;
            }

            return count;
        }

        private int CountLivingOpponents()
        {
            var count = 0;
            foreach (var participant in participants)
            {
                if ((!participant.IsPlayer ||
                     (settings.gameMode == RoboClashGameMode.Multiplayer && participant.PlayerIndex > 0)) &&
                    IsLiving(participant))
                    count++;
            }

            return count;
        }

        private bool IsPlayerAlive()
        {
            var participant = FindParticipant(player);
            return participant != null && IsAlive(participant);
        }

        private static bool IsAlive(Participant participant)
        {
            return participant != null && participant.Damageable != null &&
                   (!participant.Damageable.IsDead || participant.Controller.IsRespawning);
        }

        private static bool IsLiving(Participant participant)
        {
            return participant != null && participant.Damageable != null && !participant.Damageable.IsDead;
        }

        private bool IsParticipant(ArenaDamageable damageable)
        {
            return FindParticipant(damageable) != null;
        }

        private Participant FindParticipant(ArenaDamageable damageable)
        {
            foreach (var participant in participants)
            {
                if (participant.Damageable == damageable)
                    return participant;
            }

            return null;
        }

        private Participant FindParticipant(RobotArenaController controller)
        {
            foreach (var participant in participants)
            {
                if (participant.Controller == controller)
                    return participant;
            }

            return null;
        }

        private Participant GetParticipant(ArenaDamageable damageable)
        {
            return damageable != null ? FindParticipant(damageable) : null;
        }

        private string GetKingControllerName()
        {
            if (kingController == null)
                return "NONE";
            var definition = kingController.GetComponent<RoboClashRobotDefinition>();
            return definition != null ? definition.robotType.ToString().ToUpperInvariant() : "UNKNOWN";
        }

        private RoboClashRobotType[] GetOpponentRobotTypes(int count)
        {
            var types = new List<RoboClashRobotType>();
            foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
            {
                if (settings.gameMode == RoboClashGameMode.Survival || type != settings.playerRobotType)
                    types.Add(type);
            }

            var random = settings.useRandomSeed
                ? new System.Random(settings.randomSeed)
                : new System.Random();
            for (var i = types.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (types[i], types[swapIndex]) = (types[swapIndex], types[i]);
            }

            var result = new RoboClashRobotType[count];
            for (var i = 0; i < count; i++)
                result[i] = types[i % types.Count];
            return result;
        }

        private RoboClashRobotType[] GetMultiplayerRobotTypes()
        {
            var count = Mathf.Clamp(settings.localPlayerCount, 2, 4);
            var types = new RoboClashRobotType[count];
            for (var i = 0; i < count; i++)
                types[i] = settings.multiplayerRobots[i];
            return types;
        }

        private Camera FindMainCamera()
        {
            Camera found = null;
            var count = 0;
            foreach (var candidate in FindObjectsByType<Camera>())
            {
                if (candidate.gameObject.scene != gameObject.scene ||
                    !candidate.isActiveAndEnabled ||
                    !candidate.CompareTag("MainCamera"))
                    continue;

                found = candidate;
                count++;
            }

            return count == 1 ? found : null;
        }

        private void DetachCameraFromRobot()
        {
            var cameraDamageable = playerCamera.GetComponentInParent<ArenaDamageable>();
            if (cameraDamageable != null && !cameraWasDetached)
            {
                originalCameraParent = playerCamera.transform.parent;
                originalCameraLocalPosition = playerCamera.transform.localPosition;
                originalCameraLocalRotation = playerCamera.transform.localRotation;
                originalCameraLocalScale = playerCamera.transform.localScale;
                cameraWasDetached = true;
                playerCamera.transform.SetParent(null, true);
            }
        }

        private void RestoreCameraParent()
        {
            if (!cameraWasDetached || playerCamera == null || originalCameraParent == null)
                return;

            playerCamera.transform.SetParent(originalCameraParent, false);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
            playerCamera.transform.localScale = originalCameraLocalScale;
        }

        private void RestoreCursorState()
        {
            Cursor.lockState = cursorLockBeforePause;
            Cursor.visible = cursorVisibleBeforePause;
        }

        private ArenaSpawnZone[] FindSpawnZones()
        {
            var found = new List<ArenaSpawnZone>();
            foreach (var zone in FindObjectsByType<ArenaSpawnZone>())
            {
                if (zone.gameObject.scene == gameObject.scene)
                    found.Add(zone);
            }
            found.Sort((left, right) => left.SpawnIndex.CompareTo(right.SpawnIndex));
            return found.ToArray();
        }

        private bool HasPlayableSurface(ArenaSpawnZone[] zones)
        {
            if (zones.Length > 0)
                return true;

            foreach (var floor in FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene == gameObject.scene && floor.GetComponent<Collider>() != null)
                    return true;
            }

            return false;
        }

        private void BuildSpawnCandidates(ArenaSpawnZone[] zones)
        {
            spawnCandidates.Clear();
            if (zones != null)
            {
                foreach (var zone in zones)
                {
                    if (zone == null)
                        continue;

                    var zoneCollider = zone.GetComponent<Collider>();
                    if (zoneCollider == null ||
                        zoneCollider.bounds.size.x < 1f || zoneCollider.bounds.size.z < 1f)
                    {
                        TryAddGroundedSpawnCandidate(zone.transform.position, zoneCollider);
                        AddRadialSpawnCandidates(zone.transform.position, zoneCollider);
                        continue;
                    }

                    var bounds = zoneCollider.bounds;
                    for (var row = 0; row < 3; row++)
                    {
                        for (var column = 0; column < 3; column++)
                        {
                            var x = Mathf.Lerp(bounds.min.x, bounds.max.x, (column + 0.5f) / 3f);
                            var z = Mathf.Lerp(bounds.min.z, bounds.max.z, (row + 0.5f) / 3f);
                            TryAddGroundedSpawnCandidate(new Vector3(x, bounds.max.y, z), zoneCollider);
                        }
                    }
                }
            }

            if (zones == null || zones.Length == 0)
            {
                foreach (var fallback in FallbackSpawns)
                    TryAddGroundedSpawnCandidate(fallback, null);
            }

            foreach (var floor in FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene != gameObject.scene)
                    continue;
                var floorCollider = floor.GetComponent<Collider>();
                if (floorCollider == null)
                    continue;

                var bounds = floorCollider.bounds;
                for (var row = 0; row < 5; row++)
                {
                    for (var column = 0; column < 5; column++)
                    {
                        var x = Mathf.Lerp(bounds.min.x, bounds.max.x, (column + 0.5f) / 5f);
                        var z = Mathf.Lerp(bounds.min.z, bounds.max.z, (row + 0.5f) / 5f);
                        TryAddGroundedSpawnCandidate(new Vector3(x, bounds.max.y, z), null);
                    }
                }
            }

            if (spawnCandidates.Count == 0)
                Debug.LogError("RoboClash could not find any safe grounded spawn candidates.", this);
        }

        private void AddRadialSpawnCandidates(Vector3 center, Collider sourceCollider)
        {
            var radii = new[] { 10f, 18f };
            foreach (var radius in radii)
            {
                for (var point = 0; point < 8; point++)
                {
                    var angle = point * 45f * Mathf.Deg2Rad;
                    var sample = center + new Vector3(Mathf.Cos(angle) * radius, 0f,
                        Mathf.Sin(angle) * radius);
                    TryAddGroundedSpawnCandidate(sample, sourceCollider);
                }
            }
        }

        private void TryAddGroundedSpawnCandidate(Vector3 sample, Collider sourceCollider)
        {
            var origin = sample + Vector3.up * 20f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 60f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            var groundPoint = Vector3.zero;
            var foundGround = false;
            foreach (var hit in hits)
            {
                if ((hit.collider == sourceCollider &&
                     hit.collider.GetComponentInParent<ArenaFloor>() == null) ||
                    hit.normal.y < 0.5f ||
                    hit.collider.gameObject.scene != gameObject.scene ||
                    hit.collider.GetComponentInParent<ArenaHazard>() != null ||
                    hit.collider.GetComponentInParent<ArenaDamageable>() != null)
                    continue;

                if (hit.distance >= nearestDistance)
                    continue;
                nearestDistance = hit.distance;
                groundPoint = hit.point;
                foundGround = true;
            }

            if (!foundGround)
                return;

            var candidate = groundPoint + Vector3.up * 0.35f;
            var overlaps = Physics.OverlapSphere(candidate + Vector3.up,
                1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var overlap in overlaps)
            {
                if (overlap == sourceCollider || overlap.GetComponentInParent<ArenaFloor>() != null ||
                    overlap.GetComponentInParent<ArenaDamageable>() != null)
                    continue;
                return;
            }

            foreach (var existing in spawnCandidates)
            {
                var horizontalDifference = existing - candidate;
                horizontalDifference.y = 0f;
                if (horizontalDifference.sqrMagnitude < 0.25f)
                    return;
            }

            spawnCandidates.Add(candidate);
        }

        private void ExcludeKingZoneSpawnCandidates()
        {
            if (kingZone == null)
                return;

            var zoneCollider = kingZone.GetComponent<Collider>();
            if (zoneCollider == null)
                return;

            var zoneBounds = zoneCollider.bounds;
            spawnCandidates.RemoveAll(candidate => zoneBounds.Contains(candidate));
        }

        private Vector3[] GetSpawnPositions(RoboClashRobotType[] opponentTypes)
        {
            var isMultiplayer = settings.gameMode == RoboClashGameMode.Multiplayer;
            var count = isMultiplayer ? settings.localPlayerCount : opponentTypes.Length + 1;
            var positions = new Vector3[count];
            var types = isMultiplayer
                ? new List<RoboClashRobotType>(opponentTypes)
                : new List<RoboClashRobotType> { settings.playerRobotType };
            if (!isMultiplayer)
                types.AddRange(opponentTypes);
            var safeCandidates = new List<Vector3>(spawnCandidates.Count);
            foreach (var candidate in spawnCandidates)
            {
                if (IsSafeSpawn(candidate, RoboClashRobotType.Titan))
                    safeCandidates.Add(candidate);
            }

            var selected = RoboClashSpawnPlanner.PickSpread(safeCandidates, count, 12f);
            if (selected.Count != count)
                return null;

            for (var i = 0; i < count; i++)
            {
                positions[i] = selected[i];
                if (!IsSafeSpawn(positions[i], types[i]) ||
                    OverlapsReservedSpawn(positions, i, positions[i], types[i], types))
                    return null;
            }

            return positions;
        }

        private bool IsSafeSpawn(Vector3 position, RoboClashRobotType type)
        {
            var dimensions = RoboClashRobotDefinition.GetTargetDimensions(type);
            var radius = Mathf.Max(0.25f, dimensions.x * 0.38f);
            var height = Mathf.Max(dimensions.y, radius * 2f);

            var foundFloor = false;
            var hasArenaFloor = false;
            foreach (var floor in FindObjectsByType<ArenaFloor>())
            {
                if (floor.gameObject.scene != gameObject.scene)
                    continue;
                var collider = floor.GetComponent<Collider>();
                if (collider == null)
                    continue;

                hasArenaFloor = true;
                var bounds = collider.bounds;
                if (position.x >= bounds.min.x + radius && position.x <= bounds.max.x - radius &&
                    position.z >= bounds.min.z + radius && position.z <= bounds.max.z - radius)
                {
                    foundFloor = true;
                    break;
                }
            }

            if (hasArenaFloor && !foundFloor)
                return false;

            var hits = Physics.OverlapCapsule(position + Vector3.up * radius,
                position + Vector3.up * (height - radius), radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                var hazard = hit.GetComponentInParent<ArenaHazard>();
                if (hazard != null)
                    return false;
                if (hit.isTrigger || hit.GetComponentInParent<ArenaFloor>() != null ||
                    hit.GetComponentInParent<ArenaDamageable>() != null)
                    continue;

                return false;
            }

            if (!hasArenaFloor && FindSpawnZones().Length == 0)
                return false;

            foreach (var groundHit in Physics.RaycastAll(position + Vector3.up * 2f,
                         Vector3.down, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y < 0.5f ||
                    groundHit.collider.GetComponentInParent<ArenaHazard>() != null ||
                    groundHit.collider.GetComponentInParent<ArenaDamageable>() != null ||
                    groundHit.collider.gameObject.scene != gameObject.scene ||
                    position.y - groundHit.point.y > 1.5f)
                    continue;

                return true;
            }

            return false;
        }

        private static bool OverlapsReservedSpawn(Vector3[] reserved, int reservedCount, Vector3 candidate,
            RoboClashRobotType candidateType, List<RoboClashRobotType> types)
        {
            for (var i = 0; i < reservedCount; i++)
            {
                var candidateRadius = RoboClashRobotDefinition.GetTargetDimensions(candidateType).x * 0.5f;
                var otherRadius = RoboClashRobotDefinition.GetTargetDimensions(types[i]).x * 0.5f;
                var minimumDistance = candidateRadius + otherRadius + 1f;
                if ((reserved[i] - candidate).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }

            return false;
        }

        private Vector3 GetArenaCenter()
        {
            var floors = FindObjectsByType<ArenaFloor>();
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var floor in floors)
            {
                if (floor.gameObject.scene != gameObject.scene)
                    continue;
                var collider = floor.GetComponent<Collider>();
                if (collider == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(collider.bounds);
            }

            if (hasBounds)
                return bounds.center;

            if (spawnCandidates.Count == 0)
                return Vector3.zero;
            var center = Vector3.zero;
            foreach (var candidate in spawnCandidates)
                center += candidate;
            return center / spawnCandidates.Count;
        }

        private void DeactivateExistingRobotInstances()
        {
            foreach (var definition in FindObjectsByType<RoboClashRobotDefinition>())
            {
                if (definition.gameObject.scene == gameObject.scene &&
                    !definition.transform.IsChildOf(participantRoot))
                    DeactivateSceneObject(definition.gameObject);
            }

            foreach (var damageable in FindObjectsByType<ArenaDamageable>())
            {
                if (damageable.gameObject.scene != gameObject.scene ||
                    damageable.transform.IsChildOf(participantRoot))
                    continue;

                var isGeneratedRobot = damageable.GetComponent<RoboClashRobotDefinition>() != null;
                var isTrainingBot = damageable.GetComponent<ArenaBotController>() != null;
                var isLegacyPlayer = damageable.GetComponent<RobotArenaController>() != null &&
                                    damageable.GetComponent<RoboClashRobotMovement>() == null;
                if (!isGeneratedRobot && !isTrainingBot && !isLegacyPlayer)
                    continue;

                DeactivateSceneObject(damageable.gameObject);
            }
        }

        private void DeactivateSceneObject(GameObject target)
        {
            if (target == null)
                return;

            if (!priorObjectStates.ContainsKey(target))
                priorObjectStates.Add(target, target.activeSelf);
            target.SetActive(false);
        }

        private void UnsubscribeParticipants()
        {
            foreach (var participant in participants)
            {
                if (participant.Damageable != null)
                {
                    participant.Damageable.DamageApplied -= OnParticipantDamageApplied;
                    participant.Damageable.Eliminated -= OnParticipantEliminated;
                }
                if (participant.Controller != null)
                    participant.Controller.Respawned -= OnParticipantRespawned;
            }
        }

        private void RestorePreviousSceneObjects()
        {
            foreach (var item in priorObjectStates)
            {
                if (item.Key != null)
                    item.Key.SetActive(item.Value);
            }
            priorObjectStates.Clear();
        }

        private void CancelParticipantProjectiles()
        {
            foreach (var projectile in FindObjectsByType<ArenaProjectile>())
            {
                if (projectile == null || projectile.gameObject.scene != gameObject.scene)
                    continue;

                foreach (var participant in participants)
                {
                    if (projectile.IsOwnedBy(participant.Controller.gameObject))
                    {
                        projectile.Cancel();
                        break;
                    }
                }
            }
        }

        private void CancelProjectilesOwnedBy(GameObject owner)
        {
            foreach (var projectile in FindObjectsByType<ArenaProjectile>())
            {
                if (projectile != null && projectile.gameObject.scene == gameObject.scene &&
                    projectile.IsOwnedBy(owner))
                    projectile.Cancel();
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
