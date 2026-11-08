using System;
using UnityEngine;

namespace RoboClash
{
    public enum RoboClashCameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    public static class RoboClashSession
    {
        private const string SelectedRobotKey = "RoboClash.SelectedRobot";
        private const string OpponentCountKey = "RoboClash.OpponentCount";
        private const string AIDifficultyKey = "RoboClash.AIDifficulty";
        private const string GameModeKey = "RoboClash.GameMode";
        private const string SurvivalGrowthKey = "RoboClash.SurvivalGrowth";
        private const string WaveBreakKey = "RoboClash.WaveBreakSeconds";
        private const string MatchTimerOptionKey = "RoboClash.MatchTimerOption";
        private const string KillTargetOptionKey = "RoboClash.KillTargetOption";
        private const string KnockoutLivesKey = "RoboClash.KnockoutLives";
        private const string RespawnsEnabledKey = "RoboClash.RespawnsEnabled";
        private const string DuelUsesKillTargetKey = "RoboClash.DuelUsesKillTarget";
        private const string LocalPlayerCountKey = "RoboClash.LocalPlayerCount";
        private const string MultiplayerRobotKeyPrefix = "RoboClash.MPRobot";
        private const string CameraModeKey = "RoboClash.CameraMode";
        private const string FieldOfViewKey = "RoboClash.FieldOfView";
        private const string MouseSensitivityKey = "RoboClash.MouseSensitivity";

        private static RoboClashRobotType selectedRobot;
        private static int opponentCount;
        private static RoboClashAIDifficulty aiDifficulty;
        private static RoboClashGameMode gameMode;
        private static int survivalGrowth;
        private static float waveBreakSeconds;
        private static int matchTimerOption;
        private static int killTargetOption;
        private static int knockoutLives;
        private static bool respawnsEnabled;
        private static bool duelUsesKillTarget;
        private static int localPlayerCount = 2;
        private static RoboClashRobotType[] multiplayerRobots =
        {
            RoboClashRobotType.Volt, RoboClashRobotType.Titan,
            RoboClashRobotType.Shadow, RoboClashRobotType.Spark
        };
        private static RoboClashCameraMode cameraMode;
        private static int fieldOfView = 80;
        private static float mouseSensitivity = 0.12f;
        private static bool preferencesLoaded;
        private static bool hasPendingMatchSelection;

        public static RoboClashRobotType SelectedRobot
        {
            get
            {
                LoadPreferences();
                return selectedRobot;
            }
        }

        public static int OpponentCount
        {
            get
            {
                LoadPreferences();
                return opponentCount;
            }
        }

        public static RoboClashAIDifficulty AIDifficulty
        {
            get
            {
                LoadPreferences();
                return aiDifficulty;
            }
        }

        public static RoboClashGameMode GameMode
        {
            get
            {
                LoadPreferences();
                gameMode = RoboClashMatchManager.SanitizeGameMode(gameMode);
                return gameMode;
            }
        }

        public static int SurvivalGrowth
        {
            get
            {
                LoadPreferences();
                return survivalGrowth;
            }
        }

        public static float WaveBreakSeconds
        {
            get
            {
                LoadPreferences();
                return waveBreakSeconds;
            }
        }

        public static int MatchTimerOption
        {
            get
            {
                LoadPreferences();
                return matchTimerOption;
            }
        }

        public static int KillTargetOption
        {
            get { LoadPreferences(); return killTargetOption; }
        }

        public static int KnockoutLives
        {
            get { LoadPreferences(); return knockoutLives; }
        }

        public static bool RespawnsEnabled
        {
            get { LoadPreferences(); return respawnsEnabled; }
        }

        public static bool DuelUsesKillTarget
        {
            get { LoadPreferences(); return duelUsesKillTarget; }
        }

        public static int LocalPlayerCount
        {
            get { LoadPreferences(); return localPlayerCount; }
        }

        public static RoboClashRobotType[] MultiplayerRobots
        {
            get
            {
                LoadPreferences();
                return (RoboClashRobotType[])multiplayerRobots.Clone();
            }
        }

        public static RoboClashCameraMode CameraMode
        {
            get { LoadPreferences(); return cameraMode; }
        }

        public static int FieldOfView
        {
            get { LoadPreferences(); return fieldOfView; }
        }

        public static float MouseSensitivity
        {
            get { LoadPreferences(); return mouseSensitivity; }
        }

        public static void SaveFieldOfView(int degrees)
        {
            LoadPreferences();
            fieldOfView = Mathf.Clamp(degrees, 60, 110);
            PlayerPrefs.SetInt(FieldOfViewKey, fieldOfView);
            PlayerPrefs.Save();
        }

        public static void SaveMouseSensitivity(float value)
        {
            LoadPreferences();
            mouseSensitivity = Mathf.Clamp(value, 0.03f, 0.4f);
            PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
            PlayerPrefs.Save();
        }

        public static void SaveCameraMode(RoboClashCameraMode mode)
        {
            LoadPreferences();
            cameraMode = Enum.IsDefined(typeof(RoboClashCameraMode), mode)
                ? mode
                : RoboClashCameraMode.FirstPerson;
            PlayerPrefs.SetInt(CameraModeKey, (int)cameraMode);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransientState()
        {
            hasPendingMatchSelection = false;
        }

        public static void SaveSelection(RoboClashRobotType robot, int opponents)
        {
            LoadPreferences();
            selectedRobot = robot;
            opponentCount = Mathf.Clamp(opponents, 1, 24);
            PlayerPrefs.SetInt(SelectedRobotKey, (int)selectedRobot);
            PlayerPrefs.SetInt(OpponentCountKey, opponentCount);
            PlayerPrefs.Save();
            RoboClashSaveService.SetPreferredRobot(selectedRobot);
            RoboClashSaveService.Save();
        }

        public static void SaveAIDifficulty(RoboClashAIDifficulty difficulty)
        {
            LoadPreferences();
            aiDifficulty = Enum.IsDefined(typeof(RoboClashAIDifficulty), difficulty)
                ? difficulty
                : RoboClashAIDifficulty.Normal;
            PlayerPrefs.SetInt(AIDifficultyKey, (int)aiDifficulty);
            PlayerPrefs.Save();
        }

        public static void SaveSurvivalSettings(int enemiesAddedPerWave, float breakSeconds)
        {
            LoadPreferences();
            survivalGrowth = Mathf.Clamp(enemiesAddedPerWave, 1, 8);
            waveBreakSeconds = Mathf.Clamp(breakSeconds, 0f, 20f);
            PlayerPrefs.SetInt(SurvivalGrowthKey, survivalGrowth);
            PlayerPrefs.SetFloat(WaveBreakKey, waveBreakSeconds);
            PlayerPrefs.Save();
        }

        public static void SaveMatchTimerOption(int option)
        {
            LoadPreferences();
            matchTimerOption = Mathf.Clamp(option, 0, 2);
            PlayerPrefs.SetInt(MatchTimerOptionKey, matchTimerOption);
            PlayerPrefs.Save();
        }

        public static void SaveModeRules(int targetOption, int lives, bool allowRespawns, bool duelKillTarget)
        {
            LoadPreferences();
            killTargetOption = Mathf.Clamp(targetOption, 0, 3);
            knockoutLives = Mathf.Clamp(lives, 1, 10);
            respawnsEnabled = allowRespawns;
            duelUsesKillTarget = duelKillTarget;
            PlayerPrefs.SetInt(KillTargetOptionKey, killTargetOption);
            PlayerPrefs.SetInt(KnockoutLivesKey, knockoutLives);
            PlayerPrefs.SetInt(RespawnsEnabledKey, respawnsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(DuelUsesKillTargetKey, duelUsesKillTarget ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void BeginMatch(RoboClashRobotType robot, int opponents,
            RoboClashAIDifficulty difficulty, RoboClashGameMode selectedGameMode,
            int selectedSurvivalGrowth, float selectedWaveBreakSeconds, int selectedTimerOption,
            int selectedKillTargetOption, int selectedKnockoutLives, bool allowRespawns,
            bool duelKillTarget)
        {
            SaveSelection(robot, opponents);
            SaveAIDifficulty(difficulty);
            gameMode = RoboClashMatchManager.SanitizeGameMode(selectedGameMode);
            PlayerPrefs.SetInt(GameModeKey, (int)gameMode);
            SaveSurvivalSettings(selectedSurvivalGrowth, selectedWaveBreakSeconds);
            SaveMatchTimerOption(selectedTimerOption);
            SaveModeRules(selectedKillTargetOption, selectedKnockoutLives, allowRespawns, duelKillTarget);
            hasPendingMatchSelection = true;
        }

        public static void BeginMultiplayerMatch(int playerCount, RoboClashRobotType[] robots,
            int selectedKillTargetOption)
        {
            LoadPreferences();
            SaveMultiplayerSetup(playerCount, robots);
            killTargetOption = Mathf.Clamp(selectedKillTargetOption, 0, 3);
            gameMode = RoboClashGameMode.Multiplayer;
            PlayerPrefs.SetInt(GameModeKey, (int)gameMode);
            PlayerPrefs.SetInt(KillTargetOptionKey, killTargetOption);
            PlayerPrefs.Save();
            hasPendingMatchSelection = true;
        }

        public static void SaveMultiplayerSetup(int playerCount, RoboClashRobotType[] robots)
        {
            LoadPreferences();
            if (robots == null || robots.Length < Mathf.Clamp(playerCount, 2, 4))
                throw new ArgumentException("A robot selection is required for every local player.", nameof(robots));

            localPlayerCount = Mathf.Clamp(playerCount, 2, 4);
            if (multiplayerRobots == null || multiplayerRobots.Length != 4)
                multiplayerRobots = new[]
                {
                    RoboClashRobotType.Volt, RoboClashRobotType.Titan,
                    RoboClashRobotType.Shadow, RoboClashRobotType.Spark
                };
            for (var i = 0; i < multiplayerRobots.Length; i++)
            {
                var robot = i < robots.Length && Enum.IsDefined(typeof(RoboClashRobotType), robots[i])
                    ? robots[i]
                    : multiplayerRobots[i];
                multiplayerRobots[i] = robot;
                PlayerPrefs.SetInt(MultiplayerRobotKeyPrefix + i, (int)robot);
            }
            PlayerPrefs.SetInt(LocalPlayerCountKey, localPlayerCount);
            PlayerPrefs.Save();
        }

        public static bool ConsumeMatchSelection(out RoboClashRobotType robot, out int opponents,
            out RoboClashAIDifficulty difficulty, out RoboClashGameMode selectedGameMode,
            out int selectedSurvivalGrowth, out float selectedWaveBreakSeconds,
            out int selectedTimerOption, out int selectedKillTargetOption,
            out int selectedKnockoutLives, out bool allowRespawns, out bool duelKillTarget)
        {
            LoadPreferences();
            robot = selectedRobot;
            opponents = opponentCount;
            difficulty = aiDifficulty;
            selectedGameMode = gameMode;
            selectedSurvivalGrowth = survivalGrowth;
            selectedWaveBreakSeconds = waveBreakSeconds;
            selectedTimerOption = matchTimerOption;
            selectedKillTargetOption = killTargetOption;
            selectedKnockoutLives = knockoutLives;
            allowRespawns = respawnsEnabled;
            duelKillTarget = duelUsesKillTarget;
            if (!hasPendingMatchSelection)
                return false;

            hasPendingMatchSelection = false;
            return true;
        }

        private static void LoadPreferences()
        {
            if (preferencesLoaded)
                return;

            var savedRobot = PlayerPrefs.GetInt(SelectedRobotKey, (int)RoboClashRobotType.Volt);
            selectedRobot = savedRobot >= (int)RoboClashRobotType.Volt &&
                            savedRobot <= (int)RoboClashRobotType.Spark
                ? (RoboClashRobotType)savedRobot
                : RoboClashRobotType.Volt;
            opponentCount = Mathf.Clamp(PlayerPrefs.GetInt(OpponentCountKey, 3), 1, 24);
            localPlayerCount = Mathf.Clamp(PlayerPrefs.GetInt(LocalPlayerCountKey, 2), 2, 4);
            if (multiplayerRobots == null || multiplayerRobots.Length != 4)
                multiplayerRobots = new[]
                {
                    RoboClashRobotType.Volt, RoboClashRobotType.Titan,
                    RoboClashRobotType.Shadow, RoboClashRobotType.Spark
                };
            for (var i = 0; i < multiplayerRobots.Length; i++)
            {
                var savedType = PlayerPrefs.GetInt(MultiplayerRobotKeyPrefix + i, i);
                multiplayerRobots[i] = Enum.IsDefined(typeof(RoboClashRobotType), savedType)
                    ? (RoboClashRobotType)savedType
                    : (RoboClashRobotType)i;
            }
            var savedDifficulty = PlayerPrefs.GetInt(AIDifficultyKey, (int)RoboClashAIDifficulty.Normal);
            aiDifficulty = Enum.IsDefined(typeof(RoboClashAIDifficulty), savedDifficulty)
                ? (RoboClashAIDifficulty)savedDifficulty
                : RoboClashAIDifficulty.Normal;
            var savedGameMode = PlayerPrefs.GetInt(GameModeKey, (int)RoboClashGameMode.TimedDeathmatch);
            gameMode = RoboClashMatchManager.SanitizeGameMode((RoboClashGameMode)savedGameMode);
            survivalGrowth = Mathf.Clamp(PlayerPrefs.GetInt(SurvivalGrowthKey, 1), 1, 8);
            waveBreakSeconds = Mathf.Clamp(PlayerPrefs.GetFloat(WaveBreakKey, 5f), 0f, 20f);
            matchTimerOption = Mathf.Clamp(PlayerPrefs.GetInt(MatchTimerOptionKey, 1), 0, 2);
            killTargetOption = Mathf.Clamp(PlayerPrefs.GetInt(KillTargetOptionKey, 1), 0, 3);
            knockoutLives = Mathf.Clamp(PlayerPrefs.GetInt(KnockoutLivesKey, 3), 1, 10);
            respawnsEnabled = PlayerPrefs.GetInt(RespawnsEnabledKey, 1) != 0;
            duelUsesKillTarget = PlayerPrefs.GetInt(DuelUsesKillTargetKey, 1) != 0;
            var savedCameraMode = PlayerPrefs.GetInt(CameraModeKey, (int)RoboClashCameraMode.FirstPerson);
            cameraMode = Enum.IsDefined(typeof(RoboClashCameraMode), savedCameraMode)
                ? (RoboClashCameraMode)savedCameraMode
                : RoboClashCameraMode.FirstPerson;
            fieldOfView = Mathf.Clamp(PlayerPrefs.GetInt(FieldOfViewKey, 80), 60, 110);
            mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 0.12f), 0.03f, 0.4f);
            preferencesLoaded = true;
        }
    }
}
