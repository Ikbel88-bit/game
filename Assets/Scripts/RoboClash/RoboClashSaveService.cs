using System;
using System.IO;
using UnityEngine;

namespace RoboClash
{
    [Serializable]
    public sealed class RoboClashPlayerSettings
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public bool fullscreen;
        public int resolutionWidth;
        public int resolutionHeight;
        public int qualityLevel;
        public bool vSync;
        public int targetFrameRate = 60;
    }

    [Serializable]
    public sealed class RoboClashCareerData
    {
        public int totalMatches;
        public int totalWins;
        public int totalLosses;
        public int totalDraws;
        public int totalEliminations;
        public float totalDamageDealt;
        public int highestSurvivalWave;
        public float totalPlayTimeSeconds;
        public string preferredRobotId = "VOLT";
        public int level = 1;
        public int currentXP;
        public int lifetimeXP;
    }

    [Serializable]
    public sealed class RoboClashSaveData
    {
        public int saveVersion = 1;
        public RoboClashPlayerSettings settings = new RoboClashPlayerSettings();
        public RoboClashCareerData career = new RoboClashCareerData();
    }

    [Serializable]
    public sealed class RoboClashProgressionRewards
    {
        [Min(0)] public int matchCompletionXP = 100;
        [Min(0)] public int victoryBonusXP = 50;
        [Min(0)] public int eliminationXP = 20;
        [Min(0)] public int survivalWaveXP = 10;
    }

    public static class RoboClashSaveService
    {
        private const int CurrentSaveVersion = 1;
        private const int MaximumLifetimeXP = 1000000000;
        private const string MasterVolumeKey = "RoboClash.MasterVolume";
        private const string FullscreenKey = "RoboClash.Fullscreen";
        private const string ResolutionKey = "RoboClash.Resolution";
        private static RoboClashSaveData current;
        private static RoboClashPlayerSettings defaultSettings;
        private static bool isLoaded;
        private static bool preserveExistingSave;

        public static int LastMatchXP { get; private set; }
        public static event Action<float> MusicVolumeChanged;

        public static float MusicVolume
        {
            get
            {
                EnsureLoaded();
                return current.settings.musicVolume;
            }
        }

        public static RoboClashSaveData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        public static string SavePath => Path.Combine(Application.persistentDataPath, "roboclash-save.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            current = null;
            defaultSettings = null;
            isLoaded = false;
            preserveExistingSave = false;
            LastMatchXP = 0;
            MusicVolumeChanged = null;
            Application.quitting -= SaveOnQuit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            EnsureLoaded();
            Application.quitting -= SaveOnQuit;
            Application.quitting += SaveOnQuit;
        }

        public static void Load()
        {
            if (isLoaded)
                return;

            defaultSettings = CaptureDefaultSettings();
            isLoaded = true;
            preserveExistingSave = false;
            if (!File.Exists(SavePath))
            {
                current = CreateDefaultData();
                var hasLegacyResolution = MigrateLegacySettings(current.settings);
                if (!hasLegacyResolution)
                {
                    var native = GetNativeResolution();
                    current.settings.fullscreen = true;
                    current.settings.resolutionWidth = native.x;
                    current.settings.resolutionHeight = native.y;
                }
                ApplySettings();
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var loaded = JsonUtility.FromJson<RoboClashSaveData>(json);
                if (loaded == null || loaded.saveVersion != CurrentSaveVersion)
                {
                    preserveExistingSave = true;
                    Debug.LogWarning(
                        $"RoboClash save version is unsupported or invalid. Defaults are in use; the existing save was preserved. Path: {SavePath}");
                    current = CreateDefaultData();
                    ApplySettings();
                    return;
                }

                current = loaded;
                var hasSavedResolution = current.settings != null &&
                                         current.settings.resolutionWidth > 0 &&
                                         current.settings.resolutionHeight > 0;
                var invalidSavedResolution = !IsValidSavedResolution(
                    current.settings != null ? current.settings.resolutionWidth : 0,
                    current.settings != null ? current.settings.resolutionHeight : 0);
                Validate(current);
                if (!hasSavedResolution)
                    current.settings.fullscreen = true;
                ApplySettings();
                if (invalidSavedResolution)
                    Save();
            }
            catch (IOException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"Could not read the RoboClash save file at '{SavePath}': {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
            catch (UnauthorizedAccessException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"Access to the RoboClash save file was denied at '{SavePath}': {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
            catch (ArgumentException exception)
            {
                preserveExistingSave = true;
                Debug.LogError($"The RoboClash save file at '{SavePath}' is invalid: {exception.Message}");
                current = CreateDefaultData();
                ApplySettings();
            }
        }

        public static bool Save()
        {
            EnsureLoaded();
            Validate(current);
            if (preserveExistingSave)
            {
                Debug.LogWarning(
                    $"RoboClash data was not written because an existing save could not be safely loaded. Path: {SavePath}");
                return false;
            }

            var directory = Path.GetDirectoryName(SavePath);
            var temporaryPath = SavePath + ".tmp";

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(current, true));
                if (File.Exists(SavePath))
                    File.Replace(temporaryPath, SavePath, null);
                else
                    File.Move(temporaryPath, SavePath);
                return true;
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not save RoboClash data to '{SavePath}': {exception.Message}");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"Access to the RoboClash save location was denied at '{SavePath}': {exception.Message}");
                return false;
            }
            catch (PlatformNotSupportedException exception)
            {
                Debug.LogError($"Atomic save replacement is unavailable at '{SavePath}': {exception.Message}");
                return false;
            }
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            current.settings.masterVolume = ClampVolume(value, 1f);
            ApplyAudioSettings();
        }

        public static void SetMusicVolume(float value)
        {
            EnsureLoaded();
            current.settings.musicVolume = ClampVolume(value, 0.8f);
            MusicVolumeChanged?.Invoke(current.settings.musicVolume);
        }

        public static void SetSfxVolume(float value)
        {
            EnsureLoaded();
            current.settings.sfxVolume = ClampVolume(value, 1f);
            ApplyAudioSettings();
        }

        public static void SetFullscreen(bool value)
        {
            EnsureLoaded();
            current.settings.fullscreen = value;
            Screen.SetResolution(current.settings.resolutionWidth, current.settings.resolutionHeight,
                GetFullScreenMode(value));
        }

        public static void SetResolution(int width, int height)
        {
            EnsureLoaded();
            if (!IsValidSavedResolution(width, height))
            {
                Debug.LogWarning($"The requested RoboClash resolution {width} x {height} is not supported.");
                return;
            }

            current.settings.resolutionWidth = width;
            current.settings.resolutionHeight = height;
            Screen.SetResolution(width, height, GetFullScreenMode(current.settings.fullscreen));
        }

        public static Vector2Int NativeResolution => GetNativeResolution();

        public static bool IsValidSavedResolution(int width, int height)
        {
            var native = GetNativeResolution();
            if (width == native.x && height == native.y)
                return true;
            if (width < 1280 || height < 720 || !HasNativeAspectRatio(width, height, native))
                return false;

            foreach (var resolution in Screen.resolutions)
            {
                if (resolution.width == width && resolution.height == height)
                    return true;
            }

            return false;
        }

        public static void SetQualityLevel(int index)
        {
            EnsureLoaded();
            if (QualitySettings.names.Length == 0)
            {
                Debug.LogError("RoboClash cannot apply graphics quality because the project has no quality levels.");
                return;
            }

            current.settings.qualityLevel = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(current.settings.qualityLevel, true);
            Save();
        }

        public static void SetVSync(bool enabled)
        {
            EnsureLoaded();
            current.settings.vSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            ApplyFrameRate();
        }

        public static void SetTargetFrameRate(int frameRate)
        {
            EnsureLoaded();
            current.settings.targetFrameRate = IsSupportedFrameRate(frameRate) ? frameRate : 60;
            ApplyFrameRate();
        }

        public static void ApplySettings()
        {
            EnsureLoaded();
            Validate(current);
            ApplyAudioSettings();
            MusicVolumeChanged?.Invoke(current.settings.musicVolume);
            var settings = current.settings;
            Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight,
                GetFullScreenMode(settings.fullscreen));

            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            QualitySettings.vSyncCount = settings.vSync ? 1 : 0;
            ApplyFrameRate();
        }

        public static void ResetSettings()
        {
            EnsureLoaded();
            current.settings = CreateDefaultData().settings;
            ApplySettings();
            Save();
        }

        public static void ResetCareer()
        {
            EnsureLoaded();
            var preferredRobot = current.career.preferredRobotId;
            current.career = new RoboClashCareerData { preferredRobotId = preferredRobot };
            Save();
        }

        public static void SetPreferredRobot(RoboClashRobotType robot)
        {
            EnsureLoaded();
            current.career.preferredRobotId = robot.ToString().ToUpperInvariant();
        }

        public static int GetXPRequiredForNextLevel(int level)
        {
            return Mathf.Min(100 + Mathf.Max(0, level - 1) * 50, 1000000);
        }

        public static int AwardXPForMatch(
            RoboClashMatchStatistics statistics, RoboClashProgressionRewards rewards = null)
        {
            rewards = rewards ?? new RoboClashProgressionRewards();
            long award = Mathf.Max(0, rewards.matchCompletionXP) +
                         (long)Mathf.Max(0, statistics.PlayerEliminations) *
                         Mathf.Max(0, rewards.eliminationXP);
            if (statistics.FinalResult == RoboClashMatchState.Victory)
                award += Mathf.Max(0, rewards.victoryBonusXP);
            if (statistics.GameMode == RoboClashGameMode.Survival)
                award += (long)Mathf.Max(0, statistics.HighestWaveCompleted) *
                         Mathf.Max(0, rewards.survivalWaveXP);
            return (int)Math.Min(MaximumLifetimeXP, award);
        }

        public static void RecordMatch(RoboClashMatchStatistics statistics, RoboClashRobotType playerRobot,
            RoboClashProgressionRewards rewards)
        {
            EnsureLoaded();
            var career = current.career;
            career.totalMatches++;
            career.totalEliminations = (int)Math.Min(
                int.MaxValue,
                (long)career.totalEliminations + Mathf.Max(0, statistics.PlayerEliminations));
            career.totalDamageDealt += Mathf.Max(0f, statistics.PlayerDamageDealt);
            career.totalPlayTimeSeconds += Mathf.Max(0f, statistics.MatchDurationSeconds);
            if (statistics.GameMode == RoboClashGameMode.Survival)
                career.highestSurvivalWave = Mathf.Max(career.highestSurvivalWave, statistics.HighestWaveReached);

            switch (statistics.FinalResult)
            {
                case RoboClashMatchState.Victory:
                    career.totalWins++;
                    break;
                case RoboClashMatchState.Defeat:
                    career.totalLosses++;
                    break;
                case RoboClashMatchState.Draw:
                    career.totalDraws++;
                    break;
            }

            career.preferredRobotId = playerRobot.ToString().ToUpperInvariant();
            LastMatchXP = AwardXPForMatch(statistics, rewards);
            AddXP(career, LastMatchXP);
            Save();
        }

        private static void AddXP(RoboClashCareerData career, int amount)
        {
            career.lifetimeXP = Mathf.Min(MaximumLifetimeXP, career.lifetimeXP + Mathf.Max(0, amount));
            var remainingXP = career.lifetimeXP;
            var level = 1;
            while (remainingXP >= GetXPRequiredForNextLevel(level))
            {
                remainingXP -= GetXPRequiredForNextLevel(level);
                level++;
            }

            career.level = level;
            career.currentXP = remainingXP;
        }

        private static void EnsureLoaded()
        {
            if (!isLoaded)
                Load();
        }

        private static void SaveOnQuit()
        {
            Save();
        }

        private static RoboClashSaveData CreateDefaultData()
        {
            return new RoboClashSaveData
            {
                settings = CopySettings(defaultSettings),
                career = new RoboClashCareerData()
            };
        }

        private static RoboClashPlayerSettings CaptureDefaultSettings()
        {
            return new RoboClashPlayerSettings
            {
                masterVolume = 1f,
                musicVolume = 0.8f,
                sfxVolume = 1f,
                fullscreen = true,
                resolutionWidth = GetNativeResolution().x,
                resolutionHeight = GetNativeResolution().y,
                qualityLevel = QualitySettings.names.Length > 0
                    ? QualitySettings.names.Length - 1
                    : 0,
                vSync = QualitySettings.vSyncCount > 0,
                targetFrameRate = 60
            };
        }

        private static RoboClashPlayerSettings CopySettings(RoboClashPlayerSettings settings)
        {
            return new RoboClashPlayerSettings
            {
                masterVolume = settings.masterVolume,
                musicVolume = settings.musicVolume,
                sfxVolume = settings.sfxVolume,
                fullscreen = settings.fullscreen,
                resolutionWidth = settings.resolutionWidth,
                resolutionHeight = settings.resolutionHeight,
                qualityLevel = settings.qualityLevel,
                vSync = settings.vSync,
                targetFrameRate = settings.targetFrameRate
            };
        }

        private static bool MigrateLegacySettings(RoboClashPlayerSettings settings)
        {
            settings.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            settings.fullscreen = PlayerPrefs.GetInt(FullscreenKey, settings.fullscreen ? 1 : 0) != 0;
            var resolutions = Screen.resolutions;
            var savedIndex = PlayerPrefs.GetInt(ResolutionKey, -1);
            if (savedIndex < 0 || resolutions.Length == 0)
                return false;

            var seen = 0;
            var previousWidth = -1;
            var previousHeight = -1;
            foreach (var resolution in resolutions)
            {
                if (resolution.width == previousWidth && resolution.height == previousHeight)
                    continue;
                previousWidth = resolution.width;
                previousHeight = resolution.height;
                if (seen++ != savedIndex)
                    continue;

                settings.resolutionWidth = resolution.width;
                settings.resolutionHeight = resolution.height;
                return true;
            }

            return false;
        }

        private static void Validate(RoboClashSaveData data)
        {
            if (data.settings == null)
                data.settings = CreateDefaultData().settings;
            if (data.career == null)
                data.career = new RoboClashCareerData();

            var defaults = CreateDefaultData();
            var settings = data.settings;
            settings.masterVolume = ClampVolume(settings.masterVolume, defaults.settings.masterVolume);
            settings.musicVolume = ClampVolume(settings.musicVolume, defaults.settings.musicVolume);
            settings.sfxVolume = ClampVolume(settings.sfxVolume, defaults.settings.sfxVolume);
            if (!IsValidSavedResolution(settings.resolutionWidth, settings.resolutionHeight))
            {
                settings.resolutionWidth = defaults.settings.resolutionWidth;
                settings.resolutionHeight = defaults.settings.resolutionHeight;
            }

            settings.qualityLevel = QualitySettings.names.Length > 0
                ? Mathf.Clamp(settings.qualityLevel, 0, QualitySettings.names.Length - 1)
                : 0;
            if (!IsSupportedFrameRate(settings.targetFrameRate))
                settings.targetFrameRate = 60;

            var career = data.career;
            career.totalMatches = Mathf.Max(0, career.totalMatches);
            career.totalWins = Mathf.Clamp(career.totalWins, 0, career.totalMatches);
            career.totalLosses = Mathf.Clamp(career.totalLosses, 0, career.totalMatches - career.totalWins);
            career.totalDraws = Mathf.Clamp(career.totalDraws, 0,
                career.totalMatches - career.totalWins - career.totalLosses);
            career.totalEliminations = Mathf.Max(0, career.totalEliminations);
            career.totalDamageDealt = IsFinite(career.totalDamageDealt)
                ? Mathf.Max(0f, career.totalDamageDealt)
                : 0f;
            career.highestSurvivalWave = Mathf.Max(0, career.highestSurvivalWave);
            career.totalPlayTimeSeconds = IsFinite(career.totalPlayTimeSeconds)
                ? Mathf.Max(0f, career.totalPlayTimeSeconds)
                : 0f;
            if (!Enum.TryParse(career.preferredRobotId, true, out RoboClashRobotType preferredRobot) ||
                !Enum.IsDefined(typeof(RoboClashRobotType), preferredRobot))
                career.preferredRobotId = "VOLT";
            career.lifetimeXP = Mathf.Clamp(career.lifetimeXP, 0, MaximumLifetimeXP);
            AddXP(career, 0);
        }

        private static void ApplyAudioSettings()
        {
            AudioListener.volume = current.settings.masterVolume;
            RoboClashRobotAudio.ApplySfxVolume(current.settings.sfxVolume);
        }

        private static void ApplyFrameRate()
        {
            Application.targetFrameRate = current.settings.vSync
                ? -1
                : current.settings.targetFrameRate;
        }

        private static Vector2Int GetNativeResolution()
        {
            var display = Display.main;
            var width = display != null ? display.systemWidth : 0;
            var height = display != null ? display.systemHeight : 0;
            if (width <= 0 || height <= 0)
            {
                var currentResolution = Screen.currentResolution;
                width = currentResolution.width;
                height = currentResolution.height;
            }
            if (width <= 0 || height <= 0)
            {
                width = Screen.width;
                height = Screen.height;
            }

            return new Vector2Int(Mathf.Max(1, width), Mathf.Max(1, height));
        }

        private static bool HasNativeAspectRatio(int width, int height, Vector2Int native)
        {
            return width > 0 && height > 0 &&
                   (long)width * native.y == (long)height * native.x;
        }

        private static FullScreenMode GetFullScreenMode(bool fullscreen)
        {
            return fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        private static bool IsSupportedFrameRate(int frameRate)
        {
            return frameRate == -1 || frameRate == 30 || frameRate == 60 ||
                   frameRate == 120 || frameRate == 144;
        }

        private static float ClampVolume(float value, float fallback)
        {
            return IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
