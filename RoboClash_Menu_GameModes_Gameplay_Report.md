# RoboClash Menu, Game Modes, and Gameplay Report

## Scope

This report covers the main menu flow, all game modes, and the core gameplay loop for RoboClash. It uses the actual Unity C# source files from the project and includes code excerpts from the original implementation.

## Source files

- [Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs](Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs)
- [Assets/Scripts/RoboClash/UI/RoboClashSession.cs](Assets/Scripts/RoboClash/UI/RoboClashSession.cs)
- [Assets/Scripts/RoboClash/RoboClashMatchManager.cs](Assets/Scripts/RoboClash/RoboClashMatchManager.cs)
- [Assets/Scripts/RoboClash/RoboClashSceneUtility.cs](Assets/Scripts/RoboClash/RoboClashSceneUtility.cs) (if present in project; otherwise the relevant scene constants are defined in the manager)

## 1. Main menu architecture

The main menu is built and managed by `RoboClashMenuController`. It creates the robot selection, match setup, summary screens, and loads the gameplay scene when the player confirms a match.

### Main menu boot flow

```csharp
[DefaultExecutionOrder(-32000)]
public sealed class RoboClashMenuController : MonoBehaviour
{
    private const string ArenaSceneName = RoboClashSceneUtility.ActiveGameplaySceneName;
    private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";

    private GameObject mainScreen;
    private GameObject selectionScreen;
    private GameObject settingsScreen;
    private GameObject matchSetupScreen;
    private GameObject matchSummaryScreen;
    private GameObject careerScreen;

    private void Awake()
    {
        DisableGameplaySystemsInMenuScene();
        EnsureMenuCursor();
        selectedRobotIndex = Mathf.Clamp((int)RoboClashSession.SelectedRobot, 0, RobotTypes.Length - 1);
        opponentCount = RoboClashSession.OpponentCount;
        aiDifficulty = RoboClashSession.AIDifficulty;
        gameMode = RoboClashSession.GameMode == RoboClashGameMode.FreeForAll
            ? RoboClashGameMode.TimedDeathmatch
            : RoboClashSession.GameMode;
        survivalGrowth = RoboClashSession.SurvivalGrowth;
        waveBreakSeconds = RoboClashSession.WaveBreakSeconds;
        matchTimerOption = RoboClashSession.MatchTimerOption;
        killTargetOption = RoboClashSession.KillTargetOption;
        knockoutLives = RoboClashSession.KnockoutLives;
        respawnsEnabled = RoboClashSession.RespawnsEnabled;
        duelUsesKillTarget = RoboClashSession.DuelUsesKillTarget;

        EnsureEventSystem();
        RoboClashSaveService.ApplySettings();
        BuildInterface();
        ShowMainMenu();
    }
}
```

The menu is responsible for:
- Robot selection
- Match setup and rule tuning
- Choosing AI difficulty
- Picking a match mode
- Saving preferences
- Starting the gameplay scene

### Persistent user settings

`RoboClashSession` stores the selected robot and match preferences in PlayerPrefs.

```csharp
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
    private const string CameraModeKey = "RoboClash.CameraMode";
    private const string FieldOfViewKey = "RoboClash.FieldOfView";
    private const string MouseSensitivityKey = "RoboClash.MouseSensitivity";
}
```

## 2. Game mode definitions

The actual game mode enum is defined in `RoboClashMatchManager`.

```csharp
public enum RoboClashGameMode
{
    FreeForAll,
    Duel,
    Survival,
    KillRace,
    TimedDeathmatch,
    KingOfTheArena,
    Knockout
}
```

The menu labels and option mapping are part of the UI layer.

```csharp
private static string GetGameModeLabel(RoboClashGameMode mode)
{
    switch (mode)
    {
        case RoboClashGameMode.KillRace: return "KILL RACE";
        case RoboClashGameMode.TimedDeathmatch:
        case RoboClashGameMode.FreeForAll: return "TIMED DEATHMATCH";
        case RoboClashGameMode.Duel: return "DUEL";
        case RoboClashGameMode.Survival: return "SURVIVAL WAVES";
        case RoboClashGameMode.KingOfTheArena: return "KING OF THE ARENA";
        case RoboClashGameMode.Knockout: return "KNOCKOUT";
        default: return "TIMED DEATHMATCH";
    }
}
```

```csharp
private static RoboClashGameMode GetGameModeFromOption(int option)
{
    switch (option)
    {
        case 0: return RoboClashGameMode.KillRace;
        case 1: return RoboClashGameMode.TimedDeathmatch;
        case 2: return RoboClashGameMode.Duel;
        case 3: return RoboClashGameMode.Survival;
        case 4: return RoboClashGameMode.KingOfTheArena;
        case 5: return RoboClashGameMode.Knockout;
        default: return RoboClashGameMode.TimedDeathmatch;
    }
}
```

## 3. Match setup screen and settings rules

The match setup screen creates cards for each mode and lets the player tune options for the selected mode.

```csharp
var descriptionsByMode = new[]
{
    "TARGET: 5 / 10 / 15 / 20",
    "TIME: 3 / 5 / 7 MINUTES",
    "2 COMBATANTS // KILLS OR TIME",
    "PROGRESSIVE WAVES",
    "CONTROL THE CENTRAL ZONE",
    "LIVES: 1 / 3 / 5"
};

var modeKeys = new[] { "KR", "TD", "VS", "SW", "KA", "KO" };
```

Mode rules are set based on the current selected game mode:

```csharp
private void SetModeRule(int option)
{
    if (gameMode == RoboClashGameMode.KillRace)
        killTargetOption = Mathf.Clamp(option, 0, 3);
    else if (gameMode == RoboClashGameMode.TimedDeathmatch ||
             gameMode == RoboClashGameMode.KingOfTheArena)
        matchTimerOption = Mathf.Clamp(option, 0, 2);
    else if (gameMode == RoboClashGameMode.Duel)
    {
        duelUsesKillTarget = option < 4;
        if (duelUsesKillTarget)
            killTargetOption = Mathf.Clamp(option, 0, 3);
        else
            matchTimerOption = Mathf.Clamp(option - 4, 0, 2);
    }
    else if (gameMode == RoboClashGameMode.Knockout)
        knockoutLives = option == 0 ? 1 : option == 1 ? 3 : 5;

    RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
        duelUsesKillTarget);
}
```

Respawn and match configuration is also saved per mode:

```csharp
private void SetRespawns(int option)
{
    respawnsEnabled = option == 0;
    RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
        duelUsesKillTarget);
}
```

## 4. Starting a match

When the player clicks start, the menu validates the selected robot and game mode, then loads the arena scene.

```csharp
private void StartSelectedMatch()
{
    if (startingMatch)
        return;

    if (selectedRobotIndex < 0 || selectedRobotIndex >= RobotTypes.Length ||
        GetDefinition(RobotTypes[selectedRobotIndex])?.stats == null)
    {
        ShowMatchStartError("The selected robot prefab is missing its robot definition.");
        return;
    }

    if (!Enum.IsDefined(typeof(RoboClashGameMode), gameMode) ||
        gameMode == RoboClashGameMode.FreeForAll)
    {
        ShowMatchStartError("Select one of the supported match modes before starting.");
        return;
    }

    if (!Application.CanStreamedLevelBeLoaded(ArenaSceneName))
    {
        ShowMatchStartError(
            $"Gameplay scene '{ArenaSceneName}' is not available in Build Settings. " +
            $"Add '{RoboClashSceneUtility.XLGameplayScenePath}' to Build Settings and try again.");
        return;
    }

    RoboClashSession.BeginMatch(RobotTypes[selectedRobotIndex], opponentCount, aiDifficulty,
        gameMode, survivalGrowth, waveBreakSeconds, matchTimerOption,
        killTargetOption, knockoutLives, respawnsEnabled, duelUsesKillTarget);

    canvas.gameObject.SetActive(false);
    SceneManager.LoadScene(ArenaSceneName);
}
```

`RoboClashSession.BeginMatch` persists and sets the selected match state before the arena loads.

```csharp
public static void BeginMatch(
    RoboClashRobotType selectedRobot, int opponentCount,
    RoboClashAIDifficulty difficulty, RoboClashGameMode selectedGameMode,
    int survivalGrowth, float waveBreakSeconds, int matchTimerOption,
    int killTargetOption, int knockoutLives, bool respawnsEnabled,
    bool duelUsesKillTarget)
{
    gameMode = Enum.IsDefined(typeof(RoboClashGameMode), selectedGameMode)
        ? selectedGameMode
        : RoboClashGameMode.FreeForAll;

    PlayerPrefs.SetInt(GameModeKey, (int)gameMode);
    ...
}
```

## 5. Actual gameplay loop and match manager

The match manager runs the gameplay state machine and controls all active participants.

### Match states

```csharp
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
```

### State transitions

```csharp
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
```

### Gameplay scene constants

```csharp
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
```

## 6. Real game modes in practice

### 6.1 Kill Race

This mode uses a kill target threshold. The menu rule option is set here:

```csharp
if (gameMode == RoboClashGameMode.KillRace)
    killTargetOption = Mathf.Clamp(option, 0, 3);
```

Match resolution is driven by kills and kill target thresholds.

### 6.2 Timed Deathmatch

This mode is time-limited and resolves final results by score at the timeout:

```csharp
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
```

### 6.3 Duel

Duel is a two-player mode that can be either time-based or kill-based depending on menu settings.

```csharp
else if (gameMode == RoboClashGameMode.Duel)
{
    duelUsesKillTarget = option < 4;
    if (duelUsesKillTarget)
        killTargetOption = Mathf.Clamp(option, 0, 3);
    else
        matchTimerOption = Mathf.Clamp(option - 4, 0, 2);
}
```

### 6.4 Survival Waves

This is the horde/wave survival mode. It calculates enemy counts and scales difficulty over time.

```csharp
private int GetInitialOpponentCount()
{
    switch (settings.gameMode)
    {
        case RoboClashGameMode.Duel:
            return 1;
        case RoboClashGameMode.Survival:
            return Mathf.Clamp(settings.startingSurvivalEnemies, 1,
                settings.maximumSurvivalEnemiesPerWave);
        default:
            return Mathf.Clamp(settings.aiOpponents, 1, 3);
    }
}
```

```csharp
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
```

This mode creates increasingly difficult enemy waves and tracks survival progression.

### 6.5 King of the Arena

This is a control/zone objective mode. The timeout flow explicitly resolves it as a score-based mode.

```csharp
if (settings.gameMode == RoboClashGameMode.KingOfTheArena)
{
    ResolveTimedScore(true);
    return;
}
```

### 6.6 Knockout

The mode uses lives for the player and/or opponents. It configures respawns based on the selected life count.

```csharp
case RoboClashGameMode.Knockout:
    controller.ConfigureMatchRespawns(settings.knockoutLives - 1);
    break;
```

The menu option mapping is also explicit:

```csharp
else if (gameMode == RoboClashGameMode.Knockout)
    knockoutLives = option == 0 ? 1 : option == 1 ? 3 : 5;
```

### 6.7 Free For All

The enum includes `FreeForAll`, but the menu normalizes it to timed deathmatch to avoid unsupported selection states.

```csharp
gameMode = RoboClashSession.GameMode == RoboClashGameMode.FreeForAll
    ? RoboClashGameMode.TimedDeathmatch
    : RoboClashSession.GameMode;
```

## 7. Gameplay match resolution and end conditions

When a match ends, the manager locks inputs, stops projectiles, disables AI and abilities, records statistics, and sets final state.

```csharp
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

    statistics.Complete(matchDuration, result);
    statistics.SetPlayerPlacement(GetPlayerPlacement(result));
    SetState(result);
    StopWaveRoutine();
    StatisticsChanged?.Invoke(statistics);
}
```

## 8. Conclusion

The project’s main menu and gameplay systems are implemented in a clear, mode-driven architecture:
- `RoboClashMenuController` builds the menu and match setup flow.
- `RoboClashSession` persists the selected robot and rules.
- `RoboClashMatchManager` owns the gameplay state and mode resolution.
- The game supports six main visible modes: Kill Race, Timed Deathmatch, Duel, Survival Waves, King of the Arena, and Knockout.

This is the real design of the project as implemented in the source code.
