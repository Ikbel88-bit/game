# RoboClash UI and C# Source Report

## Scope and implementation overview

This report documents the player-facing menus, settings, gameplay HUD, pause screen, health/shield displays, match results, minimap, and UI support code in the RoboClash Unity project. The excerpts are taken from the project’s C# sources; the linked files contain the full implementations.

Most of the interface is **constructed at runtime** with Unity’s built-in `UnityEngine.UI` (`Text`, `Image`, `Button`, `Slider`, `Toggle`, and `Dropdown`) rather than stored as a hand-authored Canvas hierarchy. The main menu and gameplay interface each create a Screen Space Overlay Canvas, scale it to a reference resolution, and build panels, labels, and controls from code.

The UI-related sources are under [`Assets/Scripts/RoboClash/UI`](./Assets/Scripts/RoboClash/UI). Gameplay uses `RoboClashHUD`, which also creates/attaches `RoboClashHudV2`. The menu scene uses `RoboClashMenuController`. Both gameplay scenes contain `RoboClashHUD`; the XL gameplay UI can also be added with the Editor command in [`RoboClashGameplayUIGenerator.cs`](./Assets/Editor/RoboClashGameplayUIGenerator.cs).

## Main-menu interface

### Canvas, navigation, and screen flow

[`RoboClashMenuController.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) owns the menu canvas and its pages. Its `Awake` loads saved menu choices/settings, establishes the Input System event handling, creates the interface, and shows the main screen. The canvas is a scaled overlay:

```csharp
var canvasObject = new GameObject("RoboClash Menu Canvas",
    typeof(RectTransform), typeof(Canvas),
    typeof(CanvasScaler), typeof(GraphicRaycaster));
canvasObject.transform.SetParent(transform, false);
canvas = canvasObject.GetComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 0;
var scaler = canvasObject.GetComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1600f, 900f);
scaler.matchWidthOrHeight = 0.5f;
```

The menu creates these pages:

| Page | Visible content and actions |
|---|---|
| Main menu | `ROBOCLASH` title, single-player tagline, animated 3D robot preview, featured robot/career summary, and **PLAY**, **ROBOT GARAGE**, **SETTINGS**, **CAREER / STATISTICS**, and **QUIT** buttons. |
| Robot Garage | Cards for Volt, Titan, Shadow, and Spark; a detailed combat profile (role, description, stats, weapon, ability, ultimate); live robot preview; **BACK** and **SELECT ROBOT**. |
| Match Setup | Six mode cards—Kill Race, Timed Deathmatch, Duel, Survival Waves, King of the Arena, and Knockout—plus AI difficulty, combatant/enemy count, and mode-specific rules. |
| Match Summary | Selected robot, mode/rules, combatants, AI difficulty, arena name; **EDIT RULES**, **CHANGE ROBOT**, and **START MATCH**. |
| Career / Statistics | Level, XP, lifetime XP, matches, wins/losses/draws, eliminations, damage, best Survival wave, play time, preferred robot; **RESET CAREER** and **BACK**. |
| Settings | Shared settings panel described below. |

Important fixed menu copy includes `MECH ARENA // SINGLE PLAYER`, `ENTER THE ARENA. ADAPT. ELIMINATE.`, `SELECT A FRAME TO REVIEW ITS LIVE COMBAT PROFILE`, `CHOOSE YOUR GAME MODE`, and `SELECT A MODE, THEN TUNE ONLY THE RULES THAT APPLY`. The main menu also shows the current career summary and the PC's phone-controller Wi-Fi status. Robot Garage shows the selected robot's name, role, description, combat stats, weapon, ability, and ultimate.

The main-menu page’s labels/actions are built directly in C#:

```csharp
var title = AddText(mainScreen.transform, "ROBOCLASH", 76, Color.white, ...);
CreateRobotPreview(mainScreen.transform, ...);
playButton = CreateButton(mainScreen.transform, "PLAY", ..., OpenMatchSetup, true);
CreateButton(mainScreen.transform, "ROBOT GARAGE", ..., OpenRobotGarage);
CreateButton(mainScreen.transform, "SETTINGS", ..., OpenSettings);
CreateButton(mainScreen.transform, "CAREER / STATISTICS", ..., OpenCareer);
CreateButton(mainScreen.transform, "QUIT", ..., QuitGame);
```

The `...` tokens in short excerpts denote omitted original layout arguments; the linked C# file has the complete calls.

Pages are mutually switched by the controller. For example, opening match setup hides the other page roots and refreshes the current options:

```csharp
mainScreen.SetActive(false);
selectionScreen.SetActive(false);
matchSetupScreen.SetActive(true);
matchSummaryScreen.SetActive(false);
careerScreen.SetActive(false);
settingsScreen.SetActive(false);
UpdateGameModeCards();
UpdateModeOptions();
UpdateOpponentCountLabel();
```

On Escape, the menu backs out of Settings, Robot Garage, Match Summary, Match Setup, or Career according to the currently visible page. The menu also keeps the cursor visible and unlocked and periodically refreshes the phone/Wi-Fi status label.

Starting a match validates the selected robot, game mode, and build-scene availability, saves the selected match configuration through `RoboClashSession`, then loads the active gameplay scene. Errors are shown on the summary page and logged rather than silently starting an invalid match.

### Robot preview, visuals, and controls

The menu creates a dedicated preview camera and render texture for the robot model, temporarily restricts camera culling masks to the preview layer, and rotates the preview robot using unscaled time while Main Menu or Robot Garage is visible. The menu backdrop uses a navy fallback, optional sprite artwork from `Resources/Menu`, readability gradients, and subtle unscaled artwork drift.

Buttons are built with a colored background, accent bar, label, and `RoboClashMenuButtonFx`. Hover, keyboard/gamepad selection, and press animate the bar, label position, color, chevron, and scale. The project’s [`RoboClashUIEventSystem`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) ensures an active `InputSystemUIInputModule` has point/click actions and disables duplicate event systems in the same scene.

### Audio/display/settings panel

[`RoboClashSettingsPanel.cs`](./Assets/Scripts/RoboClash/UI/RoboClashSettingsPanel.cs) is shared by the Main Menu and gameplay pause screen. It creates:

- Master, music, and sound-effects volume sliders.
- Fullscreen toggle, resolution selector, graphics-quality selector, V-Sync toggle, and frame-rate limit.
- First-/third-person camera mode, field of view, and mouse sensitivity.
- A keyboard-controls help text and **RESET SETTINGS** / **BACK** controls.
- Confirmation dialogs for reset actions and a 10-second display-mode confirmation with automatic rollback if not accepted.

The panel heading is `AUDIO & DISPLAY SETTINGS`; it also displays `ARENA AMBIENCE ENABLED // MUSIC TRACKS NOT ASSIGNED.` and a controls reminder for WASD, mouse aim, fire, reload, ability, ultimate, jump, and Escape.

The panel reads persisted values without firing callbacks, then applies changed values through `RoboClashSaveService` and `RoboClashSession`. Resolution changes are explicitly confirmed or restored:

```csharp
resolutionChangePending = true;
resolutionConfirmationSeconds = 10f;
ShowConfirmation(
    $"KEEP {resolution.x} x {resolution.y}? REVERTING IN 10 SECONDS.",
    ConfirmResolution, false);
```

The settings panel uses unscaled time for its confirmation countdown, so it remains usable while a match is paused. `RoboClashSession` stores session/menu choices such as robot, match rules, camera mode, FOV, and mouse sensitivity using PlayerPrefs; `RoboClashSaveService` persists broader career and audio/display settings.

## Gameplay HUD: match, combat, and feedback

[`RoboClashHUD.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) creates the first gameplay Canvas at sorting order 20. It contains:

- Match timer, match state, mode-specific objective/combatant status, scoreboard, and countdown.
- Phone-controller connection status.
- Four-entry kill feed, short-lived notifications, and the centered crosshair.
- Weapon name, ammunition count, reload countdown, and reload progress bar.
- Low-health banner, damage flash, elimination/respawn message.
- Pause menu, shared Settings panel, and end-of-match results panel.

The runtime interface is assembled in one place:

```csharp
BuildPlayerFeedback();
BuildWeaponPanel();
BuildMatchPanel();
BuildKillFeed();
BuildCrosshair();
BuildNotifications();
BuildPausePanel();
BuildPauseSettings();
BuildResultsPanel();
redesignedHud = GetComponent<RoboClashHudV2>();
if (redesignedHud == null)
    redesignedHud = gameObject.AddComponent<RoboClashHudV2>();
```

The match manager publishes state/timer/countdown/statistics/player/elimination events; the HUD subscribes and updates text or panel visibility from those events. Mode status varies by game mode. Examples include `KILL RACE // kills/target`, active combatant counts, King-of-the-Arena score, Knockout lives, and Survival wave/enemy/kill counts. Countdown text displays `3`, `2`, `1`, then `FIGHT`; the state panel reflects values such as `INITIALIZING`, `COUNTDOWN`, `PLAYING`, and `PAUSED`.

The crosshair is shown only while the player is alive, the match is Playing, and a gameplay camera mode is active. It briefly scales/highlights when firing or confirming a hit. Notifications such as `FIGHT!`, `LOW HEALTH`, `LOW AMMO`, `RELOADING`, `ABILITY READY`, and wave transitions are timed using unscaled time.

### Pause, settings, and match results

Escape toggles pause/resume while the match is Playing or Paused. The pause overlay has **RESUME**, **RESTART MATCH**, **SETTINGS**, and **MAIN MENU**. The Settings button hides the redesigned HUD behind the settings panel. Restart and return-to-menu hide pause/results/settings UI before asking the match manager to transition.

```csharp
private void BuildPausePanel()
{
    pauseRoot = CreateOverlay("Pause Menu");
    CreateText(pauseRoot.transform, "PAUSED", ...);
    CreateMenuButton(pauseRoot.transform, "RESUME", ..., ResumeMatch);
    CreateMenuButton(pauseRoot.transform, "RESTART MATCH", ..., RestartMatch);
    CreateMenuButton(pauseRoot.transform, "SETTINGS", ..., OpenPauseSettings);
    CreateMenuButton(pauseRoot.transform, "MAIN MENU", ..., ReturnToMainMenu);
    pauseRoot.SetActive(false);
}
```

Pausing preserves and restores the current time scale and cursor state; the manager also locks participant input/simulation. The PC keyboard/mouse control path is unchanged by the overlay:

```csharp
timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
cursorLockBeforePause = Cursor.lockState;
cursorVisibleBeforePause = Cursor.visible;
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;
SetParticipantInputLock(true);
SetParticipantSimulationPaused(true);
Time.timeScale = 0f;
SetState(RoboClashMatchState.Paused);
```

At Victory, Defeat, or Draw, the results panel displays the final result, robot and mode, mode-specific placement/waves, eliminations/deaths, match/survival duration, scoreboard, and career XP/level. It offers **PLAY AGAIN** and **MAIN MENU**.

## Redesigned HUD: health, shield, abilities, minimap

[`RoboClashHudV2.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHudV2.cs) creates a second Screen Space Overlay Canvas at sorting order 40. This is the currently attached redesigned HUD layer; it is built by `RoboClashHUD` at runtime when not already present. It contains:

- Bottom-left robot name and animated **HEALTH** and **SHIELD** bars.
- Bottom-center circular ability and ultimate status slots.
- Upper-right circular tactical minimap with north indicator and a `MAP UNAVAILABLE` fallback.
- Full-screen damage/low-health vignette.

The redesigned canvas is hidden during initialization, pause, finished matches, and while the shared Settings panel is visible; the pause/results overlays are owned by `RoboClashHUD` on its separate canvas.

```csharp
var isFinished = state == RoboClashMatchState.Victory ||
                 state == RoboClashMatchState.Defeat ||
                 state == RoboClashMatchState.Draw;
canvasRoot.gameObject.SetActive(!settingsVisible &&
    state != RoboClashMatchState.Initializing &&
    state != RoboClashMatchState.Paused && !isFinished);
```

The vitals widgets read `ArenaDamageable.Health`, `MaximumHealth`, `Shield`, and `MaximumShield`. `VitalsChanged` and `DamageApplied` events are subscribed when the HUD binds to the player. A damage event adds hit feedback; the health bar interpolates toward the new value and retains a trailing damage bar briefly. Low health changes/pulses the health color and adds a red screen vignette.

```csharp
var target = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-18f * deltaTime));
if (ghostShown < target)
    ghostShown = target;
else if (hold > 0f)
    hold -= deltaTime;
else
    ghostShown = Mathf.MoveTowards(ghostShown, target, deltaTime * 0.5f);
SetRatio(fill.rectTransform, shown);
SetRatio(ghost.rectTransform, Mathf.Max(ghostShown, shown));
```

Health color transitions from green through yellow to red; at or below 25%, it pulses. Shield uses cyan. Both bars show current/max numbers and flash on damage.

Ability slots are driven by `RoboClashRobotAbilities` and the selected robot definition. They present `READY`, `ACTIVE`, `COOLDOWN`, `LOCKED`, `OFFLINE`, `CHARGING`, or `STANDBY`, with remaining seconds or charge percent, radial fill/overlay, and ready glow. The ability key badge is `Q`, and the ultimate badge is `E`.

The minimap is [`RoboClashMinimapV2.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMinimapV2.cs), a `MaskableGraphic` that builds its own mesh from arena floor, wall, obstacle, hazard, enemy, and player data. Enemy positions refresh every 0.25 seconds; geometry is redrawn from world positions relative to the player and optionally rotates with player yaw. The older [`RoboClashMinimap.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMinimap.cs) remains in the source tree, but current runtime code attaches `RoboClashMinimapV2`.

## UI code and data flow

### Health/shield source

[`ArenaDamageable.cs`](./Assets/Scripts/RoboClash/ArenaDamageable.cs) owns the live values and raises events for the HUD:

```csharp
public float Health { get; private set; }
public float Shield { get; private set; }
public float MaximumHealth => maximumHealth;
public float MaximumShield => maximumShield;

public event Action<ArenaDamageable, float, ArenaDamageable> DamageApplied;
public event Action<ArenaDamageable> VitalsChanged;
```

Damage is absorbed by shield before health, then the HUD is notified:

```csharp
var absorbed = Mathf.Min(Shield, amount);
Shield -= absorbed;
Health = Mathf.Max(0f, Health - (amount - absorbed));
VitalsChanged?.Invoke(this);
if (effectiveDamage > 0f)
    DamageApplied?.Invoke(this, effectiveDamage, instigator);
```

### Mode-specific HUD text

`RoboClashHUD.RefreshModeStatus` creates the active objective text and scoreboard from `RoboClashMatchManager`:

```csharp
case RoboClashGameMode.KillRace:
    combatantsText.text =
        $"KILL RACE  //  {matchManager.PlayerKills}/{matchManager.KillTarget}";
    break;
case RoboClashGameMode.Knockout:
    combatantsText.text =
        $"KNOCKOUT  //  {matchManager.PlayerLivesRemaining} LIVES LEFT";
    break;
default:
    combatantsText.text =
        $"COMBATANTS  {matchManager.AliveCombatantCount}";
    break;
```

### Results text

The final result text combines match statistics and saved career progression:

```csharp
resultDetailsText.text =
    $"{robotName}  //  {statistics.GameMode.ToString().ToUpperInvariant()}\n" +
    $"{modeDetails}" +
    $"ELIMINATIONS  {statistics.PlayerEliminations}      DEATHS  {statistics.PlayerDeaths}\n" +
    $"{durationLabel}  {duration / 60:00}:{duration % 60:00}\n" +
    $"{matchManager.GetScoreboardText()}\n" +
    $"LEVEL {career.level}  //  XP +{RoboClashSaveService.LastMatchXP}";
```

### Shared UI helpers and styling

[`RoboClashUiKit.cs`](./Assets/Scripts/RoboClash/UI/RoboClashUiKit.cs) supplies reusable `RectTransform`, `Image`, and `Text` builders, plus procedural circle/ring/vignette sprites. Menu and settings controls use local helpers in their owning scripts. `RoboClashOptionSelector.cs` provides a custom scrollable popup selector used by the shared settings panel. `RoboClashMenuBackdrop.cs` creates the menu art/shading, `RoboClashMenuButtonFx.cs` animates menu button states, and `RoboClashMenuScreenFade.cs` fades the menu settings panel using unscaled time.

[`RoboClashMenuDiagnostics.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuDiagnostics.cs) is debug-build-only: F10 captures current screen/rendering/camera/canvas/UI-tree diagnostics, copies the text to the clipboard, writes `menu-diagnostic.txt` under `Application.persistentDataPath`, and logs its path. It is not a player-facing menu.

## Source file index

| C# source | UI responsibility |
|---|---|
| [`RoboClashMenuController.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs) | Main menu, robot garage, robot preview, match setup/summary, career view, navigation, menu event-system setup. |
| [`RoboClashSettingsPanel.cs`](./Assets/Scripts/RoboClash/UI/RoboClashSettingsPanel.cs) | Shared audio/display/gameplay settings, confirmations, resolution rollback. |
| [`RoboClashOptionSelector.cs`](./Assets/Scripts/RoboClash/UI/RoboClashOptionSelector.cs) | Scrollable popup dropdown selector. |
| [`RoboClashMenuBackdrop.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuBackdrop.cs) | Main-menu artwork, fallback, shades, and subtle movement. |
| [`RoboClashMenuButtonFx.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuButtonFx.cs) | Hover/selection/press visuals and animation. |
| [`RoboClashMenuScreenFade.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuScreenFade.cs) | Settings-screen fade-in. |
| [`RoboClashHUD.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHUD.cs) | Match status, kill feed, weapon/ammo, crosshair, low-health/damage feedback, pause and results. |
| [`RoboClashHudV2.cs`](./Assets/Scripts/RoboClash/UI/RoboClashHudV2.cs) | Health/shield bars, ability/ultimate slots, minimap frame, and damage vignette. |
| [`RoboClashMinimapV2.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMinimapV2.cs) | Current procedural tactical map graphic. |
| [`RoboClashMinimap.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMinimap.cs) | Older minimap implementation; not attached by current runtime code. |
| [`RoboClashUiKit.cs`](./Assets/Scripts/RoboClash/UI/RoboClashUiKit.cs) | Shared HUD graphic, label, and procedural sprite helpers. |
| [`RoboClashSession.cs`](./Assets/Scripts/RoboClash/UI/RoboClashSession.cs) | PlayerPrefs-backed robot/match/camera selection and session preferences. |
| [`RoboClashMenuDiagnostics.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuDiagnostics.cs) | Debug-only F10 scene/UI diagnostics. |
| [`RoboClashMatchManager.cs`](./Assets/Scripts/RoboClash/RoboClashMatchManager.cs) | Match state/events and pause/resume/restart transitions consumed by the HUD. |
| [`ArenaDamageable.cs`](./Assets/Scripts/RoboClash/ArenaDamageable.cs) | Health/shield values and vitals/damage/elimination events consumed by HUD. |
| [`RoboClashGameplayUIGenerator.cs`](./Assets/Editor/RoboClashGameplayUIGenerator.cs) | Editor command to add a gameplay HUD component to the XL gameplay scene. |

## Notes

- The interface code uses Unity's legacy built-in UI `Text`, not TextMeshPro.
- The main-menu and gameplay interfaces are assembled at runtime; editing their labels/layout generally means changing the listed C# builder methods.
- `RoboClashHUD` rejects initialization outside supported gameplay scenes. `RoboClashMenuController` removes misplaced gameplay match/HUD components from the main menu.
- Settings/game mode/robot/career values come from separate persistence helpers; the UI presents and edits those values rather than owning combat simulation.
- This is a source/code report, not a Play Mode, resolution matrix, accessibility, or platform-build test. UI placement should still be checked in Unity at target aspect ratios and on device.
