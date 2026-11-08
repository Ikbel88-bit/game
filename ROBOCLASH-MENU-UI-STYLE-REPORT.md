# RoboClash Main Menu — UI and Style Report

## Purpose

This report describes the existing RoboClash main menu and its supporting screens for a future UI redesign. It is intended as implementation context for Claude or another coding assistant. It documents the actual source structure, styles, navigation, and constraints; it does not prescribe a replacement design.

The primary implementation is [`Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs). Menu UI is constructed at runtime using Unity UGUI. The main menu scene contains a `RoboClashMenuController` component with four robot prefab references; the screen hierarchy, Canvas, buttons, text, EventSystem, and robot preview are built by code at runtime.

## Existing visual direction

The current menu uses a sci-fi / industrial HUD visual language:

- Deep navy full-screen background.
- Cyan accent color for primary actions, active selections, borders, and headings.
- Dark translucent blue panels with bright white text.
- Muted blue-grey supporting copy.
- Uppercase labels, short technical phrases, and `//` separators.
- Thin cyan top rule and vertical right-side accent.
- A live 3D render of the selected robot in the main-menu screen and robot garage.
- Mostly flat color-filled buttons with a subtle cyan outline and Unity's standard dselectable feedback.

The built-in legacy Unity font (`LegacyRuntime.ttf`) and UGUI `Text` components are used. There is no TextMeshPro in this menu implementation.

### Theme colors

```csharp
private static readonly Color BackgroundColor =
    new Color(0.018f, 0.035f, 0.065f);
private static readonly Color PanelColor =
    new Color(0.035f, 0.075f, 0.12f, 0.96f);
private static readonly Color CyanColor =
    new Color(0.1f, 0.86f, 1f);
private static readonly Color MutedColor =
    new Color(0.58f, 0.69f, 0.78f);
```

Main menu text and button labels use uppercase wording. Examples include `ROBOCLASH`, `MECH ARENA // SINGLE PLAYER`, `ENTER THE ARENA. ADAPT. ELIMINATE.`, and `CAREER // LEVEL ...`.

## Runtime UI setup

`BuildInterface()` creates a screen-space-overlay Canvas and a `CanvasScaler` using a 1600 x 900 reference resolution. It then creates the separate menu screens and populates each one:

```csharp
font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
var canvasObject = new GameObject(
    "RoboClash Menu Canvas",
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

mainScreen = CreateScreen("Main Menu");
selectionScreen = CreateScreen("Robot Selection");
matchSetupScreen = CreateScreen("Match Setup");
matchSummaryScreen = CreateScreen("Match Summary");
careerScreen = CreateScreen("Career Statistics");
settingsScreen = CreateScreen("Settings");
```

The screens are siblings under one Canvas and are shown/hidden with `SetActive`. The settings screen is delegated to `RoboClashSettingsPanel`.

Each menu screen uses the same full-screen background and accent treatment:

```csharp
private GameObject CreateScreen(string screenName)
{
    var screen = CreatePanel(screenName, canvas.transform, BackgroundColor);
    Stretch(screen.GetComponent<RectTransform>());
    AddFullScreenAccent(screen.transform);
    return screen;
}

private void AddFullScreenAccent(Transform parent)
{
    var topRule = CreatePanel("Cyan Accent", parent, CyanColor);
    Place(topRule.GetComponent<RectTransform>(), new Vector2(0f, 1f),
        new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 6f));

    var rightBar = CreatePanel("Side Accent", parent,
        new Color(0.05f, 0.48f, 0.77f, 0.3f));
    Place(rightBar.GetComponent<RectTransform>(), new Vector2(1f, 0f),
        new Vector2(1f, 1f), new Vector2(-42f, 0f), new Vector2(2f, 0f));
}
```

## Main menu screen

The main screen is arranged as a left-side brand and navigation column, with a large robot preview occupying the right side. A featured-robot panel and a compact career summary sit toward the lower portion of the screen.

The title/subtitle/tagline and menu buttons are placed using normalized `RectTransform` anchors:

```csharp
AddText(mainScreen.transform, "ROBOCLASH", 58, Color.white,
    TextAnchor.MiddleLeft, new Vector2(0.075f, 0.77f),
    new Vector2(0.53f, 0.93f), Vector2.zero, Vector2.zero);
AddText(mainScreen.transform, "MECH ARENA  //  SINGLE PLAYER",
    16, CyanColor, TextAnchor.MiddleLeft,
    new Vector2(0.08f, 0.72f), new Vector2(0.51f, 0.78f),
    Vector2.zero, Vector2.zero);
AddText(mainScreen.transform, "ENTER THE ARENA. ADAPT. ELIMINATE.",
    17, MutedColor, TextAnchor.MiddleLeft,
    new Vector2(0.08f, 0.65f), new Vector2(0.51f, 0.71f),
    Vector2.zero, Vector2.zero);

CreateRobotPreview(mainScreen.transform,
    new Vector2(0.49f, 0.12f), new Vector2(0.97f, 0.87f));

playButton = CreateButton(mainScreen.transform, "PLAY",
    new Vector2(0.08f, 0.51f), new Vector2(0.36f, 0.60f),
    CyanColor, OpenMatchSetup);
CreateButton(mainScreen.transform, "ROBOT GARAGE",
    new Vector2(0.08f, 0.40f), new Vector2(0.36f, 0.49f),
    new Color(0.06f, 0.15f, 0.23f), OpenRobotGarage);
CreateButton(mainScreen.transform, "SETTINGS",
    new Vector2(0.08f, 0.29f), new Vector2(0.36f, 0.38f),
    new Color(0.06f, 0.15f, 0.23f), OpenSettings);
CreateButton(mainScreen.transform, "CAREER / STATISTICS",
    new Vector2(0.08f, 0.18f), new Vector2(0.36f, 0.27f),
    new Color(0.06f, 0.15f, 0.23f), OpenCareer);
CreateButton(mainScreen.transform, "QUIT",
    new Vector2(0.08f, 0.07f), new Vector2(0.36f, 0.16f),
    new Color(0.12f, 0.11f, 0.16f), QuitGame);
```

The featured panel displays the currently selected robot name and role. The career summary displays level/XP, match record, eliminations/damage, highest survival wave, and preferred robot.

### 3D robot preview

The menu camera renders the selected prefab into a 768 x 768 `RenderTexture`. The preview uses a dedicated layer and camera culling mask to isolate it from the game scene. Preview copies have their gameplay behaviors and colliders disabled; the camera and preview lights are also created by the menu controller.

This preview is part of the UI composition and should be preserved or deliberately replaced by an equivalent presentation in a redesign. Do not accidentally allow preview robot cameras/lights or layers to interfere with gameplay cameras.

## Robot Garage

The Robot Garage screen provides:

- Four robot selection cards: Volt, Titan, Shadow, Spark.
- A details panel containing the robot name, role, description/passive, combat stats, weapon, ability, and ultimate.
- A live 3D preview.
- `BACK` and `SELECT ROBOT` actions.

The selected robot card is tinted with that robot's primary color. Other cards use the dark panel color. The selection name and role use the robot-specific accent color. Selection is saved through `RoboClashSession.SaveSelection`.

Role labels currently include `BALANCED FIGHTER`, `HEAVY TANK`, `FAST STEALTH FIGHTER`, and `SUPPORT`. Text is information-dense and technical; a redesign may improve hierarchy and legibility while preserving the underlying robot data and selection callbacks.

## Match setup and match summary

The Play action opens `CHOOSE YOUR GAME MODE`. Six clickable mode cards are displayed in a 3-column by 2-row grid:

1. Kill Race
2. Timed Deathmatch
3. Duel
4. Survival Waves
5. King of the Arena
6. Knockout

Each card contains a short mode key, mode name, and rules hint. The active card is tinted cyan and its label changes to cyan. A separate `MATCH RULES` panel holds difficulty, opponent count, and the controls relevant to the selected mode. The bottom actions are `BACK` and `CHOOSE ROBOT`.

After setup, the match summary shows the selected robot, mode, rules, combatant count, AI difficulty, and the arena label. Actions include `EDIT RULES`, `CHANGE ROBOT`, and `START MATCH`.

The current match summary label is:

```csharp
"ARENA  //  ROBOCLASH ARENA (PROCEDURAL)"
```

The menu starts a match through `RoboClashSession.BeginMatch(...)`, then loads the gameplay scene named `RoboClashGameplay`. A visual redesign must retain this transition and the selected mode/rule values.

## Career, settings, and quit

- **Career / Statistics:** shows career level/XP, match record, eliminations, damage, best survival wave, and preferred robot. Includes `RESET CAREER` and `BACK`. Reset is confirmed through the settings panel's confirmation overlay and keeps settings intact.
- **Settings:** calls `RoboClashSettingsPanel.Show()`; settings are implemented separately. See [`ROBOCLASH-SETTINGS-SCREEN-REPORT.md`](./ROBOCLASH-SETTINGS-SCREEN-REPORT.md).
- **Quit:** calls `Application.Quit()` in a standalone build. In the Editor, it logs that Quit is only available in a standalone build.
- **Phone status:** a small status label at the bottom right reports the phone controller state using `RoboClashPhoneInputServer.StatusText`.

## Button construction and interaction styling

Buttons are created as UGUI `Image` + `Button` + `Outline` objects with a child legacy `Text`. They use built-in selectable color transitions, a thin cyan outline, audio feedback, and an `Action` callback:

```csharp
private Button CreateButton(Transform parent, string label,
    Vector2 anchorMin, Vector2 anchorMax, Color color, Action action)
{
    var buttonObject = CreatePanel(label + " Button", parent, color);
    var button = buttonObject.AddComponent<Button>();
    RoboClashRobotAudio.RoboClashAudioManager.AddButtonAudio(button);
    button.targetGraphic = buttonObject.GetComponent<Image>();
    button.navigation = Navigation.defaultNavigation;

    var colors = button.colors;
    colors.normalColor = Color.white;
    colors.highlightedColor = new Color(1f, 1f, 1f, 0.78f);
    colors.pressedColor = new Color(0.72f, 0.9f, 1f, 0.8f);
    colors.selectedColor = Color.white;
    button.colors = colors;

    var outline = buttonObject.AddComponent<Outline>();
    outline.effectColor = new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.22f);
    outline.effectDistance = new Vector2(1.5f, -1.5f);

    Place(buttonObject.GetComponent<RectTransform>(),
        anchorMin, anchorMax, Vector2.zero, Vector2.zero);
    var buttonText = AddText(buttonObject.transform, label, 17, Color.white,
        TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
        Vector2.zero, Vector2.zero);
    buttonText.fontStyle = FontStyle.Bold;
    button.onClick.AddListener(() => action?.Invoke());
    return button;
}
```

The default `Selectable` navigation is enabled, and `SelectFirstSelectable()` selects a control when a screen opens. Keep keyboard/controller navigation usable if replacing the visual button style. UI input is configured with an `InputSystemUIInputModule` by `RoboClashUIEventSystem.EnsureForScene`.

## Navigation flow

```text
Main Menu
├── PLAY
│   └── Match Setup
│       ├── BACK -> Main Menu
│       └── CHOOSE ROBOT -> Robot Garage -> SELECT ROBOT -> Match Summary
│           ├── EDIT RULES -> Match Setup
│           ├── CHANGE ROBOT -> Robot Garage -> Match Summary
│           └── START MATCH -> RoboClashGameplay scene
├── ROBOT GARAGE
│   ├── BACK -> Main Menu
│   └── SELECT ROBOT -> Main Menu
├── SETTINGS -> Settings panel -> Main Menu
├── CAREER / STATISTICS -> Career -> Main Menu
└── QUIT
```

The controller handles Escape according to the active menu page: settings and setup return to the main menu; garage returns to its caller; career returns to the main menu. The selected robot/mode context affects whether the garage returns to main menu, setup, or summary.

## Scene and architectural constraints for redesign

- Main menu scene: [`Assets/Scenes/RoboClashMainMenu.unity`](./Assets/Scenes/RoboClashMainMenu.unity).
- Gameplay scene: [`Assets/Scenes/RoboClashGameplay.unity`](./Assets/Scenes/RoboClashGameplay.unity).
- Menu UI source: [`Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`](./Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs).
- The menu is runtime-generated from a `RoboClashMenuController` component. Avoid adding duplicate serialized Canvas/screens unless changing that architecture intentionally.
- Preserve robot prefab references, selection persistence, settings entry, career reset confirmation, match configuration, input-system UI navigation, phone status text, and scene loading.
- The menu scene deliberately removes misplaced match-manager/HUD components and should stay free of gameplay systems.
- Keep legacy `Text`/UGUI; do not introduce TextMeshPro unless explicitly approved.
- Do not modify combat, movement, phone input/networking, match rules, or arena generation as part of a visual-only pass.
- Menu option names and actions must remain connected to their existing handlers; visual changes should not make a button merely decorative.

## Suggested redesign handoff for Claude

Use this report as the factual baseline. Before editing, inspect the current source and menu scene. Propose a refreshed sci-fi arena presentation with a clear visual hierarchy, readable typography, consistent spacing, responsive layout, strong focus/hover/pressed/selected states, and safe-area support. Then implement it in the existing runtime UI architecture with minimal changes to gameplay logic. Preserve all existing screen actions and navigation behavior. Do not alter the large gameplay arena or the phone controller. Validate with the Unity C# build and a Play Mode check at common aspect ratios.
