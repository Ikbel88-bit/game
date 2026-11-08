# RoboClash Main Menu and Pre-Game Interfaces Report

**Project:** RoboClash, Unity 6.6 / URP  
**Generated from source:** 2026-10-25  
**Scope:** Main menu, robot garage, match setup/summary, settings, career screen, and the handoff into gameplay. This report embeds complete copies of each listed menu/pre-game source file as it existed when generated. The match manager is a large gameplay class, so its exact pre-game selection-consumption method is included separately rather than duplicating the entire class.

## Navigation and handoff overview

- `RoboClashMenuController` creates the overlay Canvas and screen roots at runtime. Its main menu routes to Match Setup, Robot Garage, Settings, or Career/Statistics. Robot Garage selection returns to the correct prior screen; match setup advances to a summary screen before launch.
- `RoboClashSettingsPanel` builds the shared settings overlay used by the menu, including display/audio/quality controls and confirmation flows.
- `RoboClashSession` keeps selected match options and persists them through `PlayerPrefs`. `BeginMatch` marks the selected values as pending for the next gameplay scene.
- `RoboClashMenuController.StartSelectedMatch` validates the selection, records it in `RoboClashSession`, hides the menu Canvas, and loads `RoboClashGameplay`.
- `RoboClashMatchManager.Start` consumes the pending match selection once and applies the chosen robot/mode/rules before it initializes participants.
- `RoboClashMenuSceneGenerator` creates a new menu scene only if one does not already exist, validates the gameplay scene and robot prefabs, and registers menu then gameplay in Build Settings. It intentionally does not overwrite the existing `RoboClashMainMenu.unity`.
- There is no separate `RoboClashUIStateController` class in the current implementation; navigation is managed by activating one screen root at a time in the menu controller. The shared `RoboClashUIEventSystem` helper in that file reuses an appropriate EventSystem and ensures an active Input System UI module.

## Runtime transition: match selection consumption

The following is the complete current `Start` method from `Assets/Scripts/RoboClash/RoboClashMatchManager.cs`; it shows how the gameplay scene receives the pre-game selection:

```csharp
        private void Start()
        {
            if (RoboClashSession.ConsumeMatchSelection(out var selectedRobot, out var opponents,
                    out var difficulty, out var gameMode, out var survivalGrowth,
                    out var selectedWaveBreakSeconds, out var selectedTimerOption,
                    out var selectedKillTargetOption, out var selectedKnockoutLives,
                    out var allowRespawns, out var duelKillTarget))
            {
                settings.playerRobotType = selectedRobot;
                settings.aiOpponents = Mathf.Clamp(opponents, 1, 3);
                settings.startingSurvivalEnemies = Mathf.Clamp(opponents, 1,
                    settings.maximumSurvivalEnemiesPerWave);
                settings.matchDurationSeconds =
                    new[] { 180f, 300f, 420f }[Mathf.Clamp(selectedTimerOption, 0, 2)];
                settings.enableTimeLimit = true;
                settings.aiDifficulty = difficulty;
                settings.gameMode = gameMode;
                settings.additionalEnemiesPerWave = survivalGrowth;
                settings.waveBreakSeconds = selectedWaveBreakSeconds;
                settings.killTarget = new[] { 5, 10, 15, 20 }[Mathf.Clamp(selectedKillTargetOption, 0, 3)];
                settings.knockoutLives = selectedKnockoutLives;
                settings.respawnEnabled = allowRespawns;
                settings.duelUsesKillTarget = duelKillTarget;
                settings.duelRespawnsEnabled = allowRespawns;
            }

            settings.enableTimeLimit = IsTimedMatch();
            InitializeMatch();
        }

```

## Complete source files

### Runtime menu controller and pre-game screen navigation

**Source:** `Assets\Scripts\RoboClash\UI\RoboClashMenuController.cs`  
**Lines:** 1511

```csharp
using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoboClash.UI
{
    internal static class RoboClashUIEventSystem
    {
        public static void EnsureForScene(GameObject owner, string eventSystemName)
        {
            if (owner == null)
            {
                Debug.LogError("RoboClash: Cannot configure UI input without an owner.");
                return;
            }

            var scene = owner.scene;
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>();
            EventSystem selected = null;
            InputSystemUIInputModule selectedInputModule = null;

            foreach (var candidate in eventSystems)
            {
                if (candidate.gameObject.scene != scene || !candidate.gameObject.activeInHierarchy)
                    continue;

                var inputModule = candidate.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null && inputModule.isActiveAndEnabled)
                {
                    selected = candidate;
                    selectedInputModule = inputModule;
                    break;
                }

                if (selected == null)
                    selected = candidate;
            }

            if (selected == null)
            {
                var eventSystemObject = new GameObject(eventSystemName);
                eventSystemObject.transform.SetParent(owner.transform, false);
                selected = eventSystemObject.AddComponent<EventSystem>();
            }

            selected.enabled = true;
            selectedInputModule = selected.GetComponent<InputSystemUIInputModule>();
            if (selectedInputModule == null)
                selectedInputModule = selected.gameObject.AddComponent<InputSystemUIInputModule>();
            selectedInputModule.enabled = true;

            foreach (var candidate in eventSystems)
            {
                if (candidate == null || candidate == selected || candidate.gameObject.scene != scene)
                    continue;

                foreach (var module in candidate.GetComponents<BaseInputModule>())
                    module.enabled = false;
                candidate.enabled = false;
            }

            foreach (var module in selected.GetComponents<BaseInputModule>())
            {
                if (module != selectedInputModule)
                    module.enabled = false;
            }
        }
    }

    public sealed class RoboClashMenuController : MonoBehaviour
    {
        private const string ArenaSceneName = "RoboClashGameplay";
        [SerializeField] private GameObject[] robotPrefabs = new GameObject[4];

        private static readonly Color BackgroundColor = new Color(0.018f, 0.035f, 0.065f);
        private static readonly Color PanelColor = new Color(0.035f, 0.075f, 0.12f, 0.96f);
        private static readonly Color CyanColor = new Color(0.1f, 0.86f, 1f);
        private static readonly Color MutedColor = new Color(0.58f, 0.69f, 0.78f);
        private static readonly RoboClashRobotType[] RobotTypes =
        {
            RoboClashRobotType.Volt,
            RoboClashRobotType.Titan,
            RoboClashRobotType.Shadow,
            RoboClashRobotType.Spark
        };

        private readonly string[] roles =
        {
            "BALANCED FIGHTER",
            "HEAVY TANK",
            "FAST STEALTH FIGHTER",
            "SUPPORT"
        };

        private readonly string[] descriptions =
        {
            "A flexible front-line fighter built to adapt to any engagement.",
            "An armored powerhouse that trades speed for durability and impact.",
            "A fast, elusive attacker that strikes from unexpected angles.",
            "A versatile support robot with explosive area pressure."
        };

        private Canvas canvas;
        private Font font;
        private GameObject mainScreen;
        private GameObject selectionScreen;
        private GameObject settingsScreen;
        private GameObject matchSetupScreen;
        private GameObject matchSummaryScreen;
        private GameObject careerScreen;
        private Text selectionName;
        private Text selectionRole;
        private Text selectionDescription;
        private Text selectionStats;
        private Text selectionWeapon;
        private Text selectionAbility;
        private Text selectionUltimate;
        private Text matchSummaryText;
        private Text careerDetailsText;
        private Text matchStartErrorText;
        private Text opponentCountText;
        private Text opponentCountLabel;
        private Text modeSettingsHint;
        private Text modeRuleLabel;
        private Text selectedRobotText;
        private Text careerSummaryText;
        private Text featuredRobotName;
        private Button confirmButton;
        private Button[] robotCardButtons;
        private Image[] robotCardBackgrounds;
        private Button[] gameModeCardButtons;
        private Image[] gameModeCardBackgrounds;
        private RoboClashSettingsPanel settingsPanel;
        private Dropdown aiDifficultyDropdown;
        private Dropdown survivalGrowthDropdown;
        private Dropdown waveBreakDropdown;
        private Dropdown modeRuleDropdown;
        private Dropdown respawnDropdown;
        private Button decreaseOpponentButton;
        private Button increaseOpponentButton;
        private Button startMatchButton;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private Transform previewRoot;
        private GameObject previewRobot;
        private GameObject previewPrefab;
        private readonly Dictionary<Camera, int> cameraCullingMasks = new Dictionary<Camera, int>();
        private bool garageFromMatchSetup;
        private bool garageReturnsToSummary;
        private bool startingMatch;
        private int previewLayer = 31;
        private int selectedRobotIndex;
        private int opponentCount;
        private int survivalGrowth;
        private float waveBreakSeconds;
        private int matchTimerOption;
        private int killTargetOption;
        private int knockoutLives;
        private bool respawnsEnabled;
        private bool duelUsesKillTarget;
        private RoboClashAIDifficulty aiDifficulty;
        private RoboClashGameMode gameMode;

        private void Awake()
        {
            DisableGameplaySystemsInMenuScene();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

        private void DisableGameplaySystemsInMenuScene()
        {
            foreach (var manager in FindObjectsByType<RoboClashMatchManager>())
            {
                if (manager.gameObject.scene != gameObject.scene)
                    continue;

                var hud = manager.GetComponent<RoboClashHUD>();
                if (hud != null)
                {
                    foreach (var existingCanvas in hud.GetComponentsInChildren<Canvas>(true))
                    {
                        if (existingCanvas.gameObject.name == "RoboClash Gameplay Canvas")
                            existingCanvas.gameObject.SetActive(false);
                    }

                    hud.enabled = false;
                }

                manager.enabled = false;
                Debug.LogWarning(
                    "A RoboClashMatchManager was found in the main-menu scene and has been disabled. " +
                    "Keep gameplay match systems in RoboClashGameplay.unity.",
                    manager);
            }
        }

        public void ConfigureRobotPrefabs(GameObject[] prefabs)
        {
            robotPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : Array.Empty<GameObject>();
            if (selectionName != null)
                UpdateRobotDetails();
        }

        private void Update()
        {
            if (previewRobot != null && (mainScreen.activeSelf || selectionScreen.activeSelf))
                previewRobot.transform.Rotate(0f, 18f * Time.unscaledDeltaTime, 0f, Space.World);

            if (UnityEngine.InputSystem.Keyboard.current == null ||
                !UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (settingsScreen.activeSelf)
            {
                settingsPanel.Hide();
                ShowMainMenu();
            }
            else if (selectionScreen.activeSelf)
                BackFromRobotGarage();
            else if (matchSummaryScreen.activeSelf)
                OpenRobotGarageFromSummary();
            else if (matchSetupScreen.activeSelf || careerScreen.activeSelf)
                ShowMainMenu();
        }

        private void OnDestroy()
        {
            foreach (var cameraEntry in cameraCullingMasks)
            {
                if (cameraEntry.Key != null)
                    cameraEntry.Key.cullingMask = cameraEntry.Value;
            }

            if (previewTexture != null)
            {
                previewTexture.Release();
                Destroy(previewTexture);
            }
        }

        private void EnsureEventSystem()
        {
            RoboClashUIEventSystem.EnsureForScene(gameObject, "RoboClash Menu Event System");
        }

        private void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("RoboClash Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildRobotPreviewCamera();
            mainScreen = CreateScreen("Main Menu");
            selectionScreen = CreateScreen("Robot Selection");
            matchSetupScreen = CreateScreen("Match Setup");
            matchSummaryScreen = CreateScreen("Match Summary");
            careerScreen = CreateScreen("Career Statistics");
            settingsScreen = CreateScreen("Settings");
            BuildMainScreen();
            BuildSelectionScreen();
            BuildMatchSetupScreen();
            BuildMatchSummaryScreen();
            BuildCareerScreen();
            BuildSettingsScreen();
        }

        private void BuildRobotPreviewCamera()
        {
            previewTexture = new RenderTexture(768, 768, 16, RenderTextureFormat.Default)
            {
                name = "RoboClash Menu Robot Preview",
                antiAliasing = 2
            };
            previewTexture.Create();
            previewRoot = new GameObject("Menu Robot Preview Root").transform;
            previewRoot.SetParent(transform, false);
            previewRoot.gameObject.SetActive(false);

            var cameraObject = new GameObject("Menu Robot Preview Camera");
            cameraObject.transform.SetParent(transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.targetTexture = previewTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.018f, 0.035f, 0.065f);
            previewCamera.cullingMask = 1 << previewLayer;
            previewCamera.fieldOfView = 30f;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = true;
            foreach (var otherCamera in FindObjectsByType<Camera>())
            {
                if (otherCamera == previewCamera)
                    continue;
                cameraCullingMasks[otherCamera] = otherCamera.cullingMask;
                otherCamera.cullingMask &= ~(1 << previewLayer);
            }

            CreatePreviewLight("Menu Preview Key Light", new Vector3(35f, -32f, 0f),
                new Color(0.55f, 0.8f, 1f), 1.2f);
            CreatePreviewLight("Menu Preview Rim Light", new Vector3(18f, 145f, 0f),
                new Color(0.15f, 0.55f, 1f), 0.8f);
        }

        private void CreatePreviewLight(string lightName, Vector3 euler, Color color, float intensity)
        {
            var lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(euler);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << previewLayer;
            light.shadows = LightShadows.None;
        }

        private RawImage CreateRobotPreview(Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var previewObject = new GameObject("Robot Render Preview",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            previewObject.transform.SetParent(parent, false);
            Place(previewObject.GetComponent<RectTransform>(), minimum, maximum, Vector2.zero, Vector2.zero);
            var image = previewObject.GetComponent<RawImage>();
            image.texture = previewTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private void UpdatePreviewRobot()
        {
            if (previewRoot == null)
                return;

            var prefab = robotPrefabs != null && selectedRobotIndex >= 0 &&
                         selectedRobotIndex < robotPrefabs.Length
                ? robotPrefabs[selectedRobotIndex]
                : null;
            if (previewRobot != null && previewPrefab == prefab)
                return;

            if (previewRobot != null)
            {
                previewRobot.SetActive(false);
                Destroy(previewRobot);
                previewRobot = null;
            }

            previewRoot.gameObject.SetActive(false);
            if (prefab == null)
            {
                previewPrefab = null;
                previewRoot.gameObject.SetActive(true);
                return;
            }

            previewRobot = Instantiate(prefab, previewRoot);
            previewPrefab = prefab;
            previewRobot.name = prefab.name + " Menu Preview";
            previewRobot.transform.localPosition = Vector3.zero;
            previewRobot.transform.localRotation = Quaternion.identity;
            foreach (var component in previewRobot.GetComponentsInChildren<Behaviour>(true))
                component.enabled = false;
            foreach (var collider in previewRobot.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var rigidbody in previewRobot.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }
            foreach (var audioSource in previewRobot.GetComponentsInChildren<AudioSource>(true))
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
            foreach (var animator in previewRobot.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            foreach (var particleSystem in previewRobot.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetLayerRecursively(previewRobot.transform, previewLayer);
            previewRoot.gameObject.SetActive(true);

            var renderers = previewRobot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                previewCamera.transform.position = new Vector3(0f, 1.5f, -6f);
                previewCamera.transform.LookAt(Vector3.up);
                Debug.LogWarning($"Robot preview for '{prefab.name}' has no renderers.", this);
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var target = bounds.center;
            var distance = Mathf.Max(4f, bounds.extents.magnitude * 2.8f);
            previewCamera.transform.position = target + new Vector3(distance * 0.55f,
                bounds.extents.y * 0.25f, -distance);
            previewCamera.transform.LookAt(target);
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = distance * 4f;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private void SetRobotPreviewVisible(bool visible)
        {
            if (previewRoot != null)
                previewRoot.gameObject.SetActive(visible);
        }

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

            var rightBar = CreatePanel("Side Accent", parent, new Color(0.05f, 0.48f, 0.77f, 0.3f));
            Place(rightBar.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(-42f, 0f), new Vector2(2f, 0f));
        }

        private void BuildMainScreen()
        {
            AddText(mainScreen.transform, "ROBOCLASH", 58, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.075f, 0.77f), new Vector2(0.53f, 0.93f), Vector2.zero, Vector2.zero);
            AddText(mainScreen.transform, "MECH ARENA  //  SINGLE PLAYER", 16, CyanColor, TextAnchor.MiddleLeft,
                new Vector2(0.08f, 0.72f), new Vector2(0.51f, 0.78f), Vector2.zero, Vector2.zero);
            AddText(mainScreen.transform, "ENTER THE ARENA. ADAPT. ELIMINATE.", 17, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.08f, 0.65f), new Vector2(0.51f, 0.71f),
                Vector2.zero, Vector2.zero);
            CreateRobotPreview(mainScreen.transform,
                new Vector2(0.49f, 0.12f), new Vector2(0.97f, 0.87f));

            CreateButton(mainScreen.transform, "PLAY", new Vector2(0.08f, 0.51f), new Vector2(0.36f, 0.60f),
                CyanColor, OpenMatchSetup);
            CreateButton(mainScreen.transform, "ROBOT GARAGE", new Vector2(0.08f, 0.40f),
                new Vector2(0.36f, 0.49f), new Color(0.06f, 0.15f, 0.23f), OpenRobotGarage);
            CreateButton(mainScreen.transform, "SETTINGS", new Vector2(0.08f, 0.29f),
                new Vector2(0.36f, 0.38f), new Color(0.06f, 0.15f, 0.23f), OpenSettings);
            CreateButton(mainScreen.transform, "CAREER / STATISTICS", new Vector2(0.08f, 0.18f),
                new Vector2(0.36f, 0.27f), new Color(0.06f, 0.15f, 0.23f), OpenCareer);
            CreateButton(mainScreen.transform, "QUIT", new Vector2(0.08f, 0.07f),
                new Vector2(0.36f, 0.16f), new Color(0.12f, 0.11f, 0.16f), QuitGame);

            var info = CreatePanel("Featured Robot", mainScreen.transform, new Color(0.025f, 0.055f, 0.09f, 0.88f));
            Place(info.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.92f, 0.30f),
                Vector2.zero, Vector2.zero);
            featuredRobotName = AddText(info.transform, string.Empty, 27, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);
            selectedRobotText = AddText(info.transform, string.Empty, 13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            careerSummaryText = AddText(mainScreen.transform, string.Empty, 12, MutedColor,
                TextAnchor.LowerLeft, new Vector2(0.08f, 0.01f), new Vector2(0.48f, 0.065f),
                Vector2.zero, Vector2.zero);
        }

        private void BuildSelectionScreen()
        {
            AddText(selectionScreen.transform, "ROBOT GARAGE", 38, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.88f), new Vector2(0.75f, 0.97f), Vector2.zero, Vector2.zero);
            AddText(selectionScreen.transform, "SELECT A FRAME TO REVIEW ITS LIVE COMBAT PROFILE", 15, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.065f, 0.82f), new Vector2(0.8f, 0.89f),
                Vector2.zero, Vector2.zero);

            robotCardButtons = new Button[RobotTypes.Length];
            robotCardBackgrounds = new Image[RobotTypes.Length];
            for (var i = 0; i < RobotTypes.Length; i++)
            {
                var index = i;
                var card = CreateButton(selectionScreen.transform, RobotTypes[i].ToString().ToUpperInvariant(),
                    new Vector2(0.06f + i * 0.19f, 0.69f), new Vector2(0.235f + i * 0.19f, 0.80f),
                    PanelColor, () => SelectRobot(index));
                robotCardButtons[i] = card;
                robotCardBackgrounds[i] = card.GetComponent<Image>();
            }

            var details = CreatePanel("Robot Details", selectionScreen.transform, PanelColor);
            Place(details.GetComponent<RectTransform>(), new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.65f),
                Vector2.zero, Vector2.zero);
            selectionName = AddText(details.transform, string.Empty, 32, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.78f), new Vector2(0.66f, 0.96f), Vector2.zero, Vector2.zero);
            selectionRole = AddText(details.transform, string.Empty, 15, CyanColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.69f), new Vector2(0.66f, 0.81f), Vector2.zero, Vector2.zero);
            selectionDescription = AddText(details.transform, string.Empty, 15, MutedColor, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.55f), new Vector2(0.70f, 0.69f), Vector2.zero, Vector2.zero);
            selectionStats = AddText(details.transform, string.Empty, 17, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.04f, 0.29f), new Vector2(0.40f, 0.54f), Vector2.zero, Vector2.zero);
            selectionWeapon = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.47f), new Vector2(0.70f, 0.60f), Vector2.zero, Vector2.zero);
            selectionAbility = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.33f), new Vector2(0.70f, 0.47f), Vector2.zero, Vector2.zero);
            selectionUltimate = AddText(details.transform, string.Empty, 16, Color.white, TextAnchor.UpperLeft,
                new Vector2(0.43f, 0.18f), new Vector2(0.70f, 0.33f), Vector2.zero, Vector2.zero);
            selectionStats.fontSize = 14;
            selectionWeapon.fontSize = 14;
            selectionAbility.fontSize = 12;
            selectionUltimate.fontSize = 12;
            CreateRobotPreview(details.transform,
                new Vector2(0.72f, 0.08f), new Vector2(0.98f, 0.94f));

            CreateButton(selectionScreen.transform, "BACK",
                new Vector2(0.06f, 0.08f), new Vector2(0.23f, 0.17f),
                new Color(0.06f, 0.15f, 0.23f), BackFromRobotGarage);
            confirmButton = CreateButton(selectionScreen.transform, "SELECT ROBOT",
                new Vector2(0.73f, 0.08f), new Vector2(0.94f, 0.17f), CyanColor, ConfirmRobotSelection);
            UpdateRobotDetails();
        }

        private void BuildMatchSetupScreen()
        {
            AddText(matchSetupScreen.transform, "CHOOSE YOUR GAME MODE", 34, Color.white,
                TextAnchor.MiddleLeft, new Vector2(0.055f, 0.89f), new Vector2(0.68f, 0.97f),
                Vector2.zero, Vector2.zero);
            AddText(matchSetupScreen.transform, "SELECT A MODE, THEN TUNE ONLY THE RULES THAT APPLY",
                13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.84f), new Vector2(0.68f, 0.90f), Vector2.zero, Vector2.zero);

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
            gameModeCardButtons = new Button[6];
            gameModeCardBackgrounds = new Image[6];
            for (var i = 0; i < gameModeCardButtons.Length; i++)
            {
                var index = i;
                var column = i % 3;
                var row = i / 3;
                var left = 0.06f + column * 0.205f;
                var bottom = row == 0 ? 0.49f : 0.17f;
                var card = CreateButton(matchSetupScreen.transform,
                    modeKeys[i] + "\n" + GetGameModeFromOption(i).ToString()                    .Replace("KingOfTheArena", "KING OF THE ARENA")
                        .Replace("TimedDeathmatch", "TIMED DEATHMATCH").Replace("KillRace", "KILL RACE")
                        .Replace("Survival", "SURVIVAL WAVES").Replace("Knockout", "KNOCKOUT").Replace("Duel", "DUEL")
                        .ToUpperInvariant() + "\n" + descriptionsByMode[i],
                    new Vector2(left, bottom), new Vector2(left + 0.19f, bottom + 0.29f),
                    PanelColor, () => SelectGameModeCard(index));
                gameModeCardButtons[i] = card;
                gameModeCardBackgrounds[i] = card.GetComponent<Image>();
                var label = card.GetComponentInChildren<Text>();
                label.fontSize = 12;
                label.supportRichText = false;
                label.lineSpacing = 1.15f;
            }

            var optionsPanel = CreatePanel("Match Rules", matchSetupScreen.transform, PanelColor);
            Place(optionsPanel.GetComponent<RectTransform>(), new Vector2(0.70f, 0.15f),
                new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "MATCH RULES", 18, CyanColor, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "AI DIFFICULTY", 12, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f), Vector2.zero, Vector2.zero);
            aiDifficultyDropdown = CreateDropdown(optionsPanel.transform,
                new Vector2(0.04f, 0.71f), new Vector2(0.96f, 0.79f));
            aiDifficultyDropdown.ClearOptions();
            aiDifficultyDropdown.AddOptions(new List<string> { "EASY", "NORMAL", "HARD" });
            aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
            aiDifficultyDropdown.onValueChanged.AddListener(SetAIDifficulty);
            opponentCountLabel = AddText(optionsPanel.transform, "AI OPPONENTS", 13, MutedColor,
                TextAnchor.MiddleCenter, new Vector2(0.02f, 0.61f), new Vector2(0.98f, 0.69f),
                Vector2.zero, Vector2.zero);
            decreaseOpponentButton = CreateButton(optionsPanel.transform, "-",
                new Vector2(0.1f, 0.48f), new Vector2(0.39f, 0.60f),
                new Color(0.06f, 0.15f, 0.23f), DecreaseOpponents);
            increaseOpponentButton = CreateButton(optionsPanel.transform, "+",
                new Vector2(0.61f, 0.48f), new Vector2(0.9f, 0.60f),
                new Color(0.06f, 0.15f, 0.23f), IncreaseOpponents);
            opponentCountText = AddText(optionsPanel.transform, string.Empty, 28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.38f, 0.48f), new Vector2(0.62f, 0.60f), Vector2.zero, Vector2.zero);
            modeSettingsHint = AddText(optionsPanel.transform, string.Empty, 11, CyanColor,
                TextAnchor.MiddleCenter, new Vector2(0.02f, 0.41f), new Vector2(0.98f, 0.47f),
                Vector2.zero, Vector2.zero);
            modeRuleLabel = AddText(optionsPanel.transform, "MODE RULES", 12, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.40f),
                Vector2.zero, Vector2.zero);
            modeRuleDropdown = CreateModeRuleDropdown(optionsPanel.transform);
            respawnDropdown = CreateRespawnDropdown(optionsPanel.transform);
            survivalGrowthDropdown = CreateSurvivalGrowthDropdown(optionsPanel.transform);
            waveBreakDropdown = CreateWaveBreakDropdown(optionsPanel.transform);

            CreateButton(matchSetupScreen.transform, "BACK", new Vector2(0.06f, 0.06f),
                new Vector2(0.23f, 0.14f), new Color(0.06f, 0.15f, 0.23f), ShowMainMenu);
            CreateButton(matchSetupScreen.transform, "CHOOSE ROBOT",
                new Vector2(0.73f, 0.06f), new Vector2(0.95f, 0.14f), CyanColor,
                () => OpenRobotGarage(true));
            UpdateGameModeCards();
        }

        private void BuildMatchSummaryScreen()
        {
            AddText(matchSummaryScreen.transform, "MATCH SUMMARY", 38, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.82f), new Vector2(0.88f, 0.94f), Vector2.zero, Vector2.zero);
            var summaryPanel = CreatePanel("Selected Match", matchSummaryScreen.transform, PanelColor);
            Place(summaryPanel.GetComponent<RectTransform>(), new Vector2(0.20f, 0.27f),
                new Vector2(0.80f, 0.79f), Vector2.zero, Vector2.zero);
            matchSummaryText = AddText(summaryPanel.transform, string.Empty, 22, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.90f),
                Vector2.zero, Vector2.zero);
            matchStartErrorText = AddText(matchSummaryScreen.transform, string.Empty, 12,
                new Color(1f, 0.34f, 0.3f), TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
            CreateButton(matchSummaryScreen.transform, "EDIT RULES", new Vector2(0.12f, 0.10f),
                new Vector2(0.34f, 0.19f), new Color(0.06f, 0.15f, 0.23f), OpenMatchSetup);
            CreateButton(matchSummaryScreen.transform, "CHANGE ROBOT", new Vector2(0.39f, 0.10f),
                new Vector2(0.61f, 0.19f), new Color(0.06f, 0.15f, 0.23f),
                OpenRobotGarageFromSummary);
            startMatchButton = CreateButton(matchSummaryScreen.transform, "START MATCH",
                new Vector2(0.66f, 0.10f), new Vector2(0.88f, 0.19f), CyanColor, StartSelectedMatch);
        }

        private void BuildCareerScreen()
        {
            AddText(careerScreen.transform, "CAREER // STATISTICS", 38, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.83f), new Vector2(0.90f, 0.94f), Vector2.zero, Vector2.zero);
            var panel = CreatePanel("Career Record", careerScreen.transform, PanelColor);
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.16f, 0.24f),
                new Vector2(0.84f, 0.78f), Vector2.zero, Vector2.zero);
            careerDetailsText = AddText(panel.transform, string.Empty, 21, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            CreateButton(careerScreen.transform, "RESET CAREER", new Vector2(0.18f, 0.09f),
                new Vector2(0.40f, 0.18f), new Color(0.12f, 0.11f, 0.16f), ConfirmCareerReset);
            CreateButton(careerScreen.transform, "BACK", new Vector2(0.60f, 0.09f),
                new Vector2(0.82f, 0.18f), new Color(0.06f, 0.15f, 0.23f), ShowMainMenu);
        }

        private void BuildSettingsScreen()
        {
            settingsPanel = gameObject.AddComponent<RoboClashSettingsPanel>();
            settingsPanel.Initialize(canvas.transform, font, ShowMainMenu);
        }

        private void ShowMainMenu()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(true);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(true);
            garageFromMatchSetup = false;
            garageReturnsToSummary = false;
            UpdateCurrentSelectionLabel();
            RefreshCareerSummary();
            RefreshCareerDetails();
            UpdateRobotDetails();
        }

        private void OpenMatchSetup()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(true);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            UpdateGameModeCards();
            aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
            survivalGrowthDropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            waveBreakDropdown.SetValueWithoutNotify(GetWaveBreakOption(waveBreakSeconds));
            survivalGrowthDropdown.RefreshShownValue();
            waveBreakDropdown.RefreshShownValue();
            UpdateModeOptions();
            UpdateOpponentCountLabel();
        }

        private void OpenRobotGarage()
        {
            OpenRobotGarage(false);
        }

        private void OpenRobotGarage(bool fromMatchSetup)
        {
            settingsPanel?.Hide();
            garageFromMatchSetup = fromMatchSetup;
            garageReturnsToSummary = false;
            mainScreen.SetActive(false);
            selectionScreen.SetActive(true);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(true);
            SelectRobot(selectedRobotIndex);
        }

        private void OpenRobotGarageFromSummary()
        {
            OpenRobotGarage(true);
            garageReturnsToSummary = true;
        }

        private void BackFromRobotGarage()
        {
            if (garageReturnsToSummary)
                OpenMatchSummary();
            else if (garageFromMatchSetup)
                OpenMatchSetup();
            else
                ShowMainMenu();
        }

        private void ConfirmRobotSelection()
        {
            if (GetDefinition(RobotTypes[selectedRobotIndex]) == null)
            {
                confirmButton.interactable = false;
                Debug.LogError("The selected robot prefab is missing its RoboClashRobotDefinition.", this);
                return;
            }

            if (garageFromMatchSetup)
                OpenMatchSummary();
            else
                ShowMainMenu();
        }

        private void OpenMatchSummary()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(true);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            RefreshMatchSummary();
        }

        private void OpenCareer()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(true);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            RefreshCareerDetails();
        }

        private void OpenSettings()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(true);
            SetRobotPreviewVisible(false);
            settingsPanel.Show();
        }

        private void SelectRobot(int index)
        {
            if (index < 0 || index >= RobotTypes.Length)
                return;

            selectedRobotIndex = index;
            RoboClashSession.SaveSelection(RobotTypes[index], opponentCount);
            UpdateRobotDetails();
            UpdateCurrentSelectionLabel();
        }

        private void UpdateRobotDetails()
        {
            if (selectionName == null)
                return;

            var type = RobotTypes[selectedRobotIndex];
            var definition = GetDefinition(type);
            var accent = GetAccent(type, definition);
            selectionName.text = type.ToString().ToUpperInvariant();
            selectionName.color = accent;
            if (featuredRobotName != null)
            {
                featuredRobotName.text = type.ToString().ToUpperInvariant();
                featuredRobotName.color = accent;
            }
            selectionRole.text = roles[selectedRobotIndex];
            selectionRole.color = accent;
            selectionDescription.text = definition != null
                ? descriptions[selectedRobotIndex] + "\nPASSIVE  " + definition.passiveName
                : descriptions[selectedRobotIndex];
            UpdatePreviewRobot();
            if (definition == null || definition.stats == null)
            {
                selectionStats.text = "Robot prefab unavailable.";
                selectionWeapon.text = string.Empty;
                selectionAbility.text = string.Empty;
                selectionUltimate.text = string.Empty;
                if (confirmButton != null)
                    confirmButton.interactable = false;
            }
            else
            {
                var stats = definition.stats;
                selectionStats.text =
                    $"HEALTH {stats.maxHealth:0}  //  SHIELD {stats.maxShield:0}\n" +
                    $"SPEED {stats.moveSpeed:0.0} m/s\n" +
                    $"DAMAGE {stats.damagePerShot:0.#}  //  {stats.shotsPerSecond:0.##}/s\n" +
                    $"MAG {stats.magazineSize}  //  RELOAD {stats.reloadDuration:0.##}s\n" +
                    $"RANGE {stats.weaponRange:0.#} m";
                selectionWeapon.text = $"WEAPON  {definition.weaponName}";
                selectionAbility.text =
                    $"ABILITY  {definition.abilityName}  //  {definition.abilityDuration:0.##}s, " +
                    $"{definition.abilityCooldown:0.#}s CD\n{GetAbilitySummary(type)}";
                selectionUltimate.text =
                    $"ULTIMATE  {definition.ultimateName}  //  {definition.ultimateDuration:0.##}s, " +
                    $"{definition.ultimateCooldown:0.#}s CD\n{GetUltimateSummary(type)}";
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }

            for (var i = 0; i < robotCardBackgrounds.Length; i++)
            {
                var selected = i == selectedRobotIndex;
                var cardAccent = GetAccent(RobotTypes[i], GetDefinition(RobotTypes[i]));
                robotCardBackgrounds[i].color = selected
                    ? new Color(cardAccent.r, cardAccent.g, cardAccent.b, 0.34f)
                    : PanelColor;
                var label = robotCardButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? cardAccent : Color.white;
            }
        }

        private void UpdateCurrentSelectionLabel()
        {
            if (selectedRobotText != null)
                selectedRobotText.text = "CURRENT ROBOT  //  " +
                                         RobotTypes[selectedRobotIndex].ToString().ToUpperInvariant();
        }

        private static string GetAbilitySummary(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Volt: return "Dashes 5 m forward.";
                case RoboClashRobotType.Titan: return "Reduces incoming damage by 45%.";
                case RoboClashRobotType.Shadow: return "Cloaks until fired or the effect ends.";
                case RoboClashRobotType.Spark: return "Repairs 4 HP/s and restores 5 shield/s.";
                default: return string.Empty;
            }
        }

        private static string GetUltimateSummary(RoboClashRobotType type)
        {
            switch (type)
            {
                case RoboClashRobotType.Volt: return "+30% move speed and +35% fire rate.";
                case RoboClashRobotType.Titan: return "Fires a 90-damage rail projectile.";
                case RoboClashRobotType.Shadow: return "Strikes toward a nearby rival for 70 damage.";
                case RoboClashRobotType.Spark: return "8 m EMP; slows nearby rivals for 3 s.";
                default: return string.Empty;
            }
        }

        private void SelectGameModeCard(int index)
        {
            if (index < 0 || index >= gameModeCardButtons.Length)
                return;

            gameMode = GetGameModeFromOption(index);
            if (gameMode == RoboClashGameMode.Duel)
                opponentCount = 1;
            else
                opponentCount = Mathf.Clamp(opponentCount, 1,
                    gameMode == RoboClashGameMode.Survival ? 24 : 3);
            UpdateModeOptions();
            UpdateOpponentCountLabel();
            UpdateGameModeCards();
        }

        private void UpdateGameModeCards()
        {
            if (gameModeCardBackgrounds == null)
                return;

            var selectedIndex = GetGameModeOption(gameMode);
            for (var i = 0; i < gameModeCardBackgrounds.Length; i++)
            {
                var isSelected = i == selectedIndex;
                gameModeCardBackgrounds[i].color = isSelected
                    ? new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.34f)
                    : PanelColor;
                var label = gameModeCardButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = isSelected ? CyanColor : Color.white;
            }
        }

        private void RefreshMatchSummary()
        {
            if (matchSummaryText == null)
                return;

            var modeRules = string.Empty;
            switch (gameMode)
            {
                case RoboClashGameMode.KillRace:
                    modeRules = $"FIRST TO {GetKillTarget()} ELIMINATIONS // RESPAWNS ON";
                    break;
                case RoboClashGameMode.TimedDeathmatch:
                    modeRules = $"{GetMatchDuration()} MINUTES // MOST ELIMINATIONS WINS // RESPAWNS ON";
                    break;
                case RoboClashGameMode.Duel:
                    modeRules = duelUsesKillTarget
                        ? $"FIRST TO {GetKillTarget()} ELIMINATIONS // RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}"
                        : $"{GetMatchDuration()} MINUTES // RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}";
                    break;
                case RoboClashGameMode.Survival:
                    modeRules = $"{opponentCount} STARTING ENEMIES // +{survivalGrowth} PER WAVE // " +
                                $"{waveBreakSeconds:0.#}s BREAK";
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    modeRules = $"{GetMatchDuration()} MINUTES // CONTROL THE CENTRAL ZONE // " +
                                $"RESPAWNS {(respawnsEnabled ? "ON" : "OFF")}";
                    break;
                case RoboClashGameMode.Knockout:
                    modeRules = $"{knockoutLives} LIVES PER ROBOT // LAST ROBOT STANDING WINS";
                    break;
                default:
                    Debug.LogError($"Unsupported match mode selected: {gameMode}.", this);
                    modeRules = "INVALID MODE";
                    break;
            }

            matchSummaryText.text =
                $"ROBOT  //  {RobotTypes[selectedRobotIndex].ToString().ToUpperInvariant()}\n\n" +
                $"MODE  //  {GetGameModeLabel(gameMode)}\n\n" +
                $"RULES  //  {modeRules}\n\n" +
                $"COMBATANTS  //  {(gameMode == RoboClashGameMode.Survival ? opponentCount : opponentCount + 1)}\n\n" +
                $"AI DIFFICULTY  //  {aiDifficulty.ToString().ToUpperInvariant()}\n\n" +
                "ARENA  //  ROBOCLASH ARENA (PROCEDURAL)";
        }

        private int GetKillTarget()
        {
            return new[] { 5, 10, 15, 20 }[Mathf.Clamp(killTargetOption, 0, 3)];
        }

        private int GetMatchDuration()
        {
            return new[] { 3, 5, 7 }[Mathf.Clamp(matchTimerOption, 0, 2)];
        }

        private void RefreshCareerDetails()
        {
            if (careerDetailsText == null)
                return;

            var career = RoboClashSaveService.Current.career;
            var requiredXP = RoboClashSaveService.GetXPRequiredForNextLevel(career.level);
            var playTime = TimeSpan.FromSeconds(Mathf.Max(0f, career.totalPlayTimeSeconds));
            var playTimeText = playTime.TotalHours >= 1
                ? $"{(int)playTime.TotalHours}h {playTime.Minutes}m"
                : $"{playTime.Minutes}m";
            careerDetailsText.text =
                $"LEVEL  {career.level}     XP  {career.currentXP} / {requiredXP}     LIFETIME XP  {career.lifetimeXP}\n\n" +
                $"MATCHES  {career.totalMatches}     WINS  {career.totalWins}     LOSSES  {career.totalLosses}     DRAWS  {career.totalDraws}\n\n" +
                $"ELIMINATIONS  {career.totalEliminations}     DAMAGE DEALT  {career.totalDamageDealt:0}\n\n" +
                $"BEST SURVIVAL WAVE  {career.highestSurvivalWave}     PLAY TIME  {playTimeText}\n\n" +
                $"MOST RECENT ROBOT  {career.preferredRobotId}\n\n" +
                (career.totalMatches == 0 ? "NO MATCH RESULTS RECORDED YET." : "STATISTICS FROM SAVED MATCH RESULTS.");
        }

        private void UpdateOpponentCountLabel()
        {
            if (opponentCountText != null)
                opponentCountText.text = gameMode == RoboClashGameMode.Survival
                    ? opponentCount.ToString()
                    : (opponentCount + 1).ToString();
            if (opponentCountLabel != null)
                opponentCountLabel.text = gameMode == RoboClashGameMode.Survival
                    ? "STARTING ENEMIES"
                    : "COMBATANTS (2-4)";
            RoboClashSession.SaveSelection(RobotTypes[selectedRobotIndex], opponentCount);
            UpdateCurrentSelectionLabel();
        }

        private void IncreaseOpponents()
        {
            var maximum = gameMode == RoboClashGameMode.Survival ? 24 : 3;
            opponentCount = Mathf.Min(maximum, opponentCount + 1);
            UpdateOpponentCountLabel();
        }

        private void DecreaseOpponents()
        {
            opponentCount = Mathf.Max(1, opponentCount - 1);
            UpdateOpponentCountLabel();
        }

        private Dropdown CreateModeRuleDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.32f));
            dropdown.onValueChanged.AddListener(SetModeRule);
            return dropdown;
        }

        private Dropdown CreateRespawnDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.04f, 0.09f), new Vector2(0.96f, 0.20f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "RESPAWN ON", "NO RESPAWN" });
            dropdown.SetValueWithoutNotify(respawnsEnabled ? 0 : 1);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetRespawns);
            return dropdown;
        }

        private void UpdateModeOptions()
        {
            var duel = gameMode == RoboClashGameMode.Duel;
            var survival = gameMode == RoboClashGameMode.Survival;
            var knockout = gameMode == RoboClashGameMode.Knockout;
            var king = gameMode == RoboClashGameMode.KingOfTheArena;
            var countLimit = survival ? 24 : 3;
            opponentCount = Mathf.Clamp(opponentCount, 1, countLimit);
            decreaseOpponentButton.gameObject.SetActive(!duel);
            increaseOpponentButton.gameObject.SetActive(!duel);
            opponentCountLabel.gameObject.SetActive(true);
            opponentCountText.gameObject.SetActive(true);
            survivalGrowthDropdown.gameObject.SetActive(survival);
            waveBreakDropdown.gameObject.SetActive(survival);
            modeSettingsHint.gameObject.SetActive(true);
            modeSettingsHint.text = survival
                ? "WAVES GROW BY +" + survivalGrowth + " ENEMIES"
                : duel
                    ? "2 COMBATANTS // PLAYER VS AI"
                    : "COMBATANTS: " + (opponentCount + 1);
            modeRuleDropdown.gameObject.SetActive(!survival);
            respawnDropdown.gameObject.SetActive(duel || king);
            var ruleOptions = new List<string>();
            var ruleLabel = "MODE RULES";
            var selectedRule = 0;
            if (gameMode == RoboClashGameMode.KillRace)
            {
                ruleLabel = "KILL TARGET";
                ruleOptions.AddRange(new[] { "5 KILLS", "10 KILLS", "15 KILLS", "20 KILLS" });
                selectedRule = killTargetOption;
            }
            else if (gameMode == RoboClashGameMode.TimedDeathmatch ||
                     gameMode == RoboClashGameMode.KingOfTheArena)
            {
                ruleLabel = "MATCH DURATION";
                ruleOptions.AddRange(new[] { "3 MINUTES", "5 MINUTES", "7 MINUTES" });
                selectedRule = matchTimerOption;
            }
            else if (gameMode == RoboClashGameMode.Duel)
            {
                ruleLabel = "DUEL WIN RULE";
                ruleOptions.AddRange(new[]
                {
                    "5 KILLS", "10 KILLS", "15 KILLS", "20 KILLS",
                    "3 MINUTES", "5 MINUTES", "7 MINUTES"
                });
                selectedRule = duelUsesKillTarget ? killTargetOption : 4 + matchTimerOption;
            }
            else if (knockout)
            {
                ruleLabel = "LIVES PER ROBOT";
                ruleOptions.AddRange(new[] { "1 LIFE", "3 LIVES", "5 LIVES" });
                selectedRule = knockoutLives <= 1 ? 0 : knockoutLives <= 3 ? 1 : 2;
            }

            modeRuleDropdown.ClearOptions();
            modeRuleDropdown.AddOptions(ruleOptions);
            modeRuleDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedRule, 0, Mathf.Max(0, ruleOptions.Count - 1)));
            modeRuleDropdown.RefreshShownValue();
            modeRuleLabel.text = ruleLabel;
            modeRuleLabel.gameObject.SetActive(!survival);
            respawnDropdown.SetValueWithoutNotify(respawnsEnabled ? 0 : 1);
            respawnDropdown.RefreshShownValue();
        }

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

        private static int GetGameModeOption(RoboClashGameMode mode)
        {
            switch (mode)
            {
                case RoboClashGameMode.KillRace: return 0;
                case RoboClashGameMode.Duel: return 2;
                case RoboClashGameMode.Survival: return 3;
                case RoboClashGameMode.KingOfTheArena: return 4;
                case RoboClashGameMode.Knockout: return 5;
                default: return 1;
            }
        }

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

            RoboClashSession.SaveMatchTimerOption(matchTimerOption);
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
        }

        private void SetRespawns(int option)
        {
            respawnsEnabled = option == 0;
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
        }

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
                    "Add Assets/Scenes/RoboClashGameplay.unity to Build Settings and try again.");
                return;
            }

            if (matchStartErrorText != null)
                matchStartErrorText.text = string.Empty;
            startingMatch = true;
            if (startMatchButton != null)
                startMatchButton.interactable = false;

            try
            {
                RoboClashSession.BeginMatch(RobotTypes[selectedRobotIndex], opponentCount, aiDifficulty,
                    gameMode, survivalGrowth, waveBreakSeconds, matchTimerOption,
                    killTargetOption, knockoutLives, respawnsEnabled, duelUsesKillTarget);
                canvas.gameObject.SetActive(false);
                SceneManager.LoadScene(ArenaSceneName);
            }
            catch (UnityException exception)
            {
                HandleMatchStartFailure(exception);
            }
            catch (ArgumentException exception)
            {
                HandleMatchStartFailure(exception);
            }
        }

        private void HandleMatchStartFailure(Exception exception)
        {
            startingMatch = false;
            if (canvas != null)
                canvas.gameObject.SetActive(true);
            if (startMatchButton != null)
                startMatchButton.interactable = true;
            ShowMatchStartError("Could not load the match: " + exception.Message);
            Debug.LogException(exception, this);
        }

        private void ShowMatchStartError(string message)
        {
            if (matchStartErrorText != null)
                matchStartErrorText.text = message;
            Debug.LogError(message, this);
        }

        private Dropdown CreateSurvivalGrowthDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.02f, 0.03f), new Vector2(0.48f, 0.18f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "GROWTH +1", "GROWTH +2", "GROWTH +3" });
            dropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetSurvivalGrowth);
            return dropdown;
        }

        private Dropdown CreateWaveBreakDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.52f, 0.03f), new Vector2(0.98f, 0.18f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "BREAK 3s", "BREAK 5s", "BREAK 8s" });
            dropdown.SetValueWithoutNotify(GetWaveBreakOption(waveBreakSeconds));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetWaveBreak);
            return dropdown;
        }

        private void SetSurvivalGrowth(int index)
        {
            survivalGrowth = Mathf.Clamp(index + 1, 1, 3);
            RoboClashSession.SaveSurvivalSettings(survivalGrowth, waveBreakSeconds);
        }

        private void SetWaveBreak(int index)
        {
            switch (index)
            {
                case 0: waveBreakSeconds = 3f; break;
                case 2: waveBreakSeconds = 8f; break;
                default: waveBreakSeconds = 5f; break;
            }
            RoboClashSession.SaveSurvivalSettings(survivalGrowth, waveBreakSeconds);
        }

        private static int GetWaveBreakOption(float seconds)
        {
            if (seconds < 4f)
                return 0;
            return seconds >= 6.5f ? 2 : 1;
        }

        private void SetAIDifficulty(int index)
        {
            if (!Enum.IsDefined(typeof(RoboClashAIDifficulty), index))
                return;

            aiDifficulty = (RoboClashAIDifficulty)index;
            RoboClashSession.SaveAIDifficulty(aiDifficulty);
            if (modeSettingsHint != null)
                UpdateModeOptions();
        }

        private void QuitGame()
        {
            if (Application.isEditor)
            {
                Debug.Log("RoboClash Quit was selected. Application.Quit is available in a standalone build.", this);
                return;
            }

            Application.Quit();
        }

        private void ConfirmCareerReset()
        {
            settingsPanel.ShowConfirmation("RESET ALL CAREER STATS AND XP? SETTINGS WILL BE KEPT.",
                () =>
                {
                    RoboClashSaveService.ResetCareer();
                    RefreshCareerSummary();
                    RefreshCareerDetails();
                    settingsPanel.Hide();
                }, false);
        }

        private void RefreshCareerSummary()
        {
            if (careerSummaryText == null)
                return;

            var career = RoboClashSaveService.Current.career;
            var requiredXP = RoboClashSaveService.GetXPRequiredForNextLevel(career.level);
            careerSummaryText.text =
                $"CAREER  //  LEVEL {career.level}  //  XP {career.currentXP}/{requiredXP}\n" +
                $"MATCHES {career.totalMatches}   W {career.totalWins} / L {career.totalLosses} / D {career.totalDraws}\n" +
                $"ELIMINATIONS {career.totalEliminations}   DAMAGE {career.totalDamageDealt:0}\n" +
                $"BEST WAVE {career.highestSurvivalWave}   PREFERRED {career.preferredRobotId}";
        }

        private RoboClashRobotDefinition GetDefinition(RoboClashRobotType type)
        {
            var index = (int)type;
            if (robotPrefabs == null || index < 0 || index >= robotPrefabs.Length ||
                robotPrefabs[index] == null)
                return null;

            return robotPrefabs[index].GetComponent<RoboClashRobotDefinition>();
        }

        private static Color GetAccent(RoboClashRobotType type, RoboClashRobotDefinition definition)
        {
            if (definition == null)
                return CyanColor;
            return definition.primaryColor;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Color color, Action action)
        {
            var buttonObject = CreatePanel(label + " Button", parent, color);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.78f);
            colors.pressedColor = new Color(0.72f, 0.9f, 1f, 0.8f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.22f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Place(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var buttonText = AddText(buttonObject.transform, label, 17, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            buttonText.fontStyle = FontStyle.Bold;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = CreatePanel("Resolution Dropdown", parent, new Color(0.07f, 0.14f, 0.2f));
            Place(root.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var dropdown = root.AddComponent<Dropdown>();
            var caption = AddText(root.transform, string.Empty, 15, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.05f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);
            AddText(root.transform, "v", 14, CyanColor, TextAnchor.MiddleCenter,
                new Vector2(0.85f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            var template = CreatePanel("Template", root.transform, new Color(0.04f, 0.09f, 0.14f));
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, -2f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.offsetMin = Vector2.zero;
            templateRect.offsetMax = Vector2.zero;
            template.AddComponent<CanvasGroup>();
            var viewport = CreatePanel("Viewport", template.transform, new Color(0.04f, 0.09f, 0.14f));
            Stretch(viewport.GetComponent<RectTransform>());
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            Stretch(content.GetComponent<RectTransform>());
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toggleGroup = content.AddComponent<ToggleGroup>();
            var item = CreatePanel("Item", content.transform, new Color(0.07f, 0.14f, 0.2f));
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            itemRect.anchoredPosition = Vector2.zero;
            item.AddComponent<LayoutElement>().preferredHeight = 32f;
            var itemToggle = item.AddComponent<Toggle>();
            var itemText = AddText(item.transform, "Option", 15, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.08f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            itemToggle.group = toggleGroup;

            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.captionText = caption;
            dropdown.template = templateRect;
            dropdown.itemText = itemText;
            dropdown.itemImage = item.GetComponent<Image>();
            itemToggle.targetGraphic = item.GetComponent<Image>();
            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            template.SetActive(false);
            return dropdown;
        }

        private Text AddText(Transform parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            Place(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static void Stretch(RectTransform rect, Vector2? minimum = null, Vector2? maximum = null)
        {
            rect.anchorMin = minimum ?? Vector2.zero;
            rect.anchorMax = maximum ?? Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
```

### Runtime settings interface

**Source:** `Assets\Scripts\RoboClash\UI\RoboClashSettingsPanel.cs`  
**Lines:** 566

```csharp
using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashSettingsPanel : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.018f, 0.045f, 0.075f, 0.98f);
        private static readonly Color ControlColor = new Color(0.055f, 0.14f, 0.2f, 1f);
        private static readonly Color Cyan = new Color(0.08f, 0.8f, 1f);

        private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();
        private Transform customRows;
        private Font font;
        private Action backAction;
        private Action confirmationAction;
        private GameObject contentRoot;
        private GameObject confirmationRoot;
        private GameObject root;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Toggle fullscreenToggle;
        private Toggle vSyncToggle;
        private Dropdown resolutionDropdown;
        private Dropdown qualityDropdown;
        private Dropdown frameRateDropdown;
        private int customRowIndex;
        private bool confirmationClosesPanel;
        private bool resolutionChangePending;
        private float resolutionConfirmationSeconds;
        private int previousResolutionWidth;
        private int previousResolutionHeight;
        private bool previousFullscreen;
        private Text confirmationMessage;

        public GameObject Root => root;

        private void Update()
        {
            if (!resolutionChangePending || root == null || !root.activeSelf ||
                confirmationRoot == null || !confirmationRoot.activeSelf)
                return;

            resolutionConfirmationSeconds -= Time.unscaledDeltaTime;
            if (resolutionConfirmationSeconds <= 0f)
            {
                RestorePreviousResolution();
                FinishConfirmation();
                return;
            }

            confirmationMessage.text =
                $"KEEP THIS DISPLAY MODE? REVERTING IN {Mathf.CeilToInt(resolutionConfirmationSeconds)} SECONDS.";
        }

        public void Initialize(Transform canvasRoot, Font uiFont, Action onBack)
        {
            if (root != null)
            {
                backAction = onBack;
                Refresh();
                return;
            }

            font = uiFont;
            backAction = onBack;
            root = CreateObject("RoboClash Settings", canvasRoot);
            Stretch(root.GetComponent<RectTransform>());
            var backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.005f, 0.018f, 0.035f, 0.94f);
            contentRoot = CreateObject("Settings Content", root.transform);
            Stretch(contentRoot.GetComponent<RectTransform>());
            BuildControls();
            root.SetActive(false);
        }

        public void Show()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root == null)
                return;

            if (resolutionChangePending)
                RestorePreviousResolution();
            RoboClashSaveService.Save();
            confirmationAction = null;
            confirmationRoot?.SetActive(false);
            contentRoot.SetActive(true);
            root.SetActive(false);
        }

        public void ShowConfirmation(string message, Action onConfirmed, bool closePanelWhenFinished)
        {
            confirmationAction = onConfirmed;
            confirmationClosesPanel = closePanelWhenFinished;
            root.SetActive(true);
            contentRoot.SetActive(false);
            confirmationRoot.SetActive(true);
            confirmationMessage.text = message;
        }

        public Dropdown AddCustomDropdownRow(string label, IReadOnlyList<string> options,
            int selectedIndex, Action<int> onChanged)
        {
            if (customRows == null)
                return null;

            var y = customRowIndex++ == 0 ? 0.18f : 0.10f;
            CreateText(customRows, label.ToUpperInvariant(), 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + 0.065f), TextAnchor.MiddleLeft);
            var dropdown = CreateDropdown(customRows,
                new Vector2(0.42f, y), new Vector2(0.94f, y + 0.065f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(options.Count > 0
                ? Mathf.Clamp(selectedIndex, 0, options.Count - 1)
                : 0);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(value => onChanged?.Invoke(value));
            return dropdown;
        }

        public void Refresh()
        {
            if (root == null)
                return;

            var settings = RoboClashSaveService.Current.settings;
            masterSlider.SetValueWithoutNotify(settings.masterVolume);
            musicSlider.SetValueWithoutNotify(settings.musicVolume);
            sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
            fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
            vSyncToggle.SetIsOnWithoutNotify(settings.vSync);
            PopulateResolutionOptions(settings.resolutionWidth, settings.resolutionHeight);
            PopulateQualityOptions(settings.qualityLevel);
            frameRateDropdown.SetValueWithoutNotify(GetFrameRateOption(settings.targetFrameRate));
            frameRateDropdown.RefreshShownValue();
        }

        private void BuildControls()
        {
            var panel = CreatePanel(contentRoot.transform, "Settings Panel", PanelColor,
                new Vector2(0.13f, 0.045f), new Vector2(0.87f, 0.955f));
            CreateText(panel.transform, "AUDIO & DISPLAY SETTINGS", 30, Color.white,
                new Vector2(0.06f, 0.91f), new Vector2(0.94f, 0.99f), TextAnchor.MiddleCenter);
            CreateText(panel.transform, "MUSIC OUTPUT UNAVAILABLE // NO MUSIC TRACK IS ASSIGNED.",
                11, new Color(1f, 0.72f, 0.25f),
                new Vector2(0.06f, 0.875f), new Vector2(0.94f, 0.91f), TextAnchor.MiddleCenter);

            masterSlider = AddSliderRow(panel.transform, "MASTER VOLUME", 0.82f,
                RoboClashSaveService.Current.settings.masterVolume, RoboClashSaveService.SetMasterVolume);
            musicSlider = AddSliderRow(panel.transform, "MUSIC VOLUME", 0.74f,
                RoboClashSaveService.Current.settings.musicVolume, RoboClashSaveService.SetMusicVolume);
            musicSlider.interactable = false;
            sfxSlider = AddSliderRow(panel.transform, "SFX VOLUME", 0.66f,
                RoboClashSaveService.Current.settings.sfxVolume, RoboClashSaveService.SetSfxVolume);

            CreateText(panel.transform, "FULLSCREEN", 15, Color.white,
                new Vector2(0.06f, 0.58f), new Vector2(0.39f, 0.645f), TextAnchor.MiddleLeft);
            fullscreenToggle = CreateToggle(panel.transform,
                new Vector2(0.42f, 0.58f), new Vector2(0.52f, 0.645f),
                RoboClashSaveService.Current.settings.fullscreen, RoboClashSaveService.SetFullscreen);

            CreateText(panel.transform, "RESOLUTION", 15, Color.white,
                new Vector2(0.06f, 0.50f), new Vector2(0.39f, 0.565f), TextAnchor.MiddleLeft);
            resolutionDropdown = CreateDropdown(panel.transform,
                new Vector2(0.42f, 0.50f), new Vector2(0.94f, 0.565f));
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);

            CreateText(panel.transform, "GRAPHICS QUALITY", 15, Color.white,
                new Vector2(0.06f, 0.42f), new Vector2(0.39f, 0.485f), TextAnchor.MiddleLeft);
            qualityDropdown = CreateDropdown(panel.transform,
                new Vector2(0.42f, 0.42f), new Vector2(0.94f, 0.485f));
            qualityDropdown.onValueChanged.AddListener(RoboClashSaveService.SetQualityLevel);

            CreateText(panel.transform, "V-SYNC", 15, Color.white,
                new Vector2(0.06f, 0.34f), new Vector2(0.39f, 0.405f), TextAnchor.MiddleLeft);
            vSyncToggle = CreateToggle(panel.transform,
                new Vector2(0.42f, 0.34f), new Vector2(0.52f, 0.405f),
                RoboClashSaveService.Current.settings.vSync, RoboClashSaveService.SetVSync);

            CreateText(panel.transform, "FRAME-RATE LIMIT", 15, Color.white,
                new Vector2(0.06f, 0.26f), new Vector2(0.39f, 0.325f), TextAnchor.MiddleLeft);
            frameRateDropdown = CreateDropdown(panel.transform,
                new Vector2(0.42f, 0.26f), new Vector2(0.94f, 0.325f));
            frameRateDropdown.AddOptions(new List<string>
            {
                "30 FPS", "60 FPS", "120 FPS", "144 FPS", "UNLIMITED"
            });
            frameRateDropdown.onValueChanged.AddListener(ApplyFrameRate);

            customRows = panel.transform;
            CreateText(panel.transform, "CONTROLS", 12, Cyan, new Vector2(0.06f, 0.11f),
                new Vector2(0.39f, 0.18f), TextAnchor.MiddleLeft);
            CreateText(panel.transform,
                "WASD MOVE // MOUSE AIM // LMB FIRE // R RELOAD // Q ABILITY // E ULTIMATE // SPACE JUMP // ESC MENU",
                10, Color.white, new Vector2(0.42f, 0.105f), new Vector2(0.94f, 0.18f),
                TextAnchor.MiddleLeft);
            CreateButton(panel.transform, "RESET SETTINGS", new Vector2(0.06f, 0.035f),
                new Vector2(0.31f, 0.095f), ConfirmResetSettings);
            CreateButton(panel.transform, "BACK", new Vector2(0.69f, 0.04f),
                new Vector2(0.94f, 0.12f), ReturnToCaller);
            confirmationRoot = BuildConfirmation(panel.transform);
            confirmationRoot.SetActive(false);
        }

        private GameObject BuildConfirmation(Transform parent)
        {
            var overlay = CreateObject("Confirmation", parent);
            Stretch(overlay.GetComponent<RectTransform>());
            var background = overlay.AddComponent<Image>();
            background.color = new Color(0.005f, 0.018f, 0.035f, 0.96f);
            var panel = CreatePanel(overlay.transform, "Confirmation Panel", PanelColor,
                new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.7f));
            confirmationMessage = CreateText(panel.transform, string.Empty, 19, Color.white,
                new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.88f), TextAnchor.MiddleCenter,
                "Message");
            CreateButton(panel.transform, "CONFIRM", new Vector2(0.1f, 0.12f),
                new Vector2(0.45f, 0.32f), ConfirmAction);
            CreateButton(panel.transform, "CANCEL", new Vector2(0.55f, 0.12f),
                new Vector2(0.9f, 0.32f), CancelConfirmation);
            return overlay;
        }

        private Slider AddSliderRow(Transform parent, string label, float y, float value, Action<float> changed)
        {
            CreateText(parent, label, 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + 0.065f), TextAnchor.MiddleLeft);
            var slider = CreateSlider(parent, new Vector2(0.42f, y + 0.01f),
                new Vector2(0.94f, y + 0.055f), value);
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged));
            return slider;
        }

        private Slider CreateSlider(Transform parent, Vector2 minimum, Vector2 maximum, float value)
        {
            var sliderObject = CreateObject("Volume Slider", parent, typeof(Slider));
            Place(sliderObject.GetComponent<RectTransform>(), minimum, maximum);
            var background = CreatePanel(sliderObject.transform, "Track",
                new Color(0.07f, 0.14f, 0.2f), Vector2.zero, Vector2.one);
            var fill = CreatePanel(background.transform, "Fill", Cyan,
                Vector2.zero, Vector2.one);
            var handle = CreatePanel(sliderObject.transform, "Handle", Color.white,
                Vector2.zero, Vector2.one);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 0f);
            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            return slider;
        }

        private Toggle CreateToggle(Transform parent, Vector2 minimum, Vector2 maximum,
            bool value, Action<bool> changed)
        {
            var toggleObject = CreateObject("Setting Toggle", parent, typeof(Toggle));
            Place(toggleObject.GetComponent<RectTransform>(), minimum, maximum);
            var background = CreatePanel(toggleObject.transform, "Background", ControlColor,
                Vector2.zero, Vector2.one);
            var check = CreatePanel(background.transform, "Check", Cyan,
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(changedValue => changed(changedValue));
            return toggle;
        }

        private Dropdown CreateDropdown(Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var dropdownObject = CreatePanel(parent, "Settings Dropdown", ControlColor,
                minimum, maximum);
            var dropdown = dropdownObject.AddComponent<Dropdown>();
            var caption = CreateText(dropdownObject.transform, string.Empty, 14, Color.white,
                new Vector2(0.06f, 0.05f), new Vector2(0.86f, 0.95f), TextAnchor.MiddleLeft);
            CreateText(dropdownObject.transform, "v", 14, Cyan,
                new Vector2(0.86f, 0.05f), new Vector2(0.98f, 0.95f), TextAnchor.MiddleCenter);
            var template = CreatePanel(dropdownObject.transform, "Template", ControlColor,
                new Vector2(0f, -2f), new Vector2(1f, 0f));
            var templateRect = template.GetComponent<RectTransform>();
            template.AddComponent<CanvasGroup>();
            var viewport = CreatePanel(template.transform, "Viewport", ControlColor,
                Vector2.zero, Vector2.one);
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var content = CreateObject("Content", viewport.transform);
            Stretch(content.GetComponent<RectTransform>());
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toggleGroup = content.AddComponent<ToggleGroup>();
            var item = CreatePanel(content.transform, "Item", ControlColor,
                Vector2.zero, Vector2.one);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 30f);
            item.AddComponent<LayoutElement>().preferredHeight = 30f;
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.group = toggleGroup;
            var itemText = CreateText(item.transform, "Option", 14, Color.white,
                new Vector2(0.08f, 0f), Vector2.one, TextAnchor.MiddleLeft);
            itemToggle.targetGraphic = item.GetComponent<Image>();
            dropdown.targetGraphic = dropdownObject.GetComponent<Image>();
            dropdown.captionText = caption;
            dropdown.template = templateRect;
            dropdown.itemText = itemText;
            dropdown.itemImage = item.GetComponent<Image>();
            var scroll = template.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            template.SetActive(false);
            return dropdown;
        }

        private void PopulateResolutionOptions(int selectedWidth, int selectedHeight)
        {
            availableResolutions.Clear();
            var currentIndex = 0;
            var currentResolutionIndex = 0;
            var selectedResolutionFound = false;
            var currentResolutionFound = false;
            var resolutions = Screen.resolutions;
            foreach (var resolution in resolutions)
            {
                var duplicate = false;
                for (var i = 0; i < availableResolutions.Count; i++)
                {
                    if (availableResolutions[i].x == resolution.width &&
                        availableResolutions[i].y == resolution.height)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                    continue;
                if (resolution.width == selectedWidth && resolution.height == selectedHeight)
                {
                    currentIndex = availableResolutions.Count;
                    selectedResolutionFound = true;
                }
                if (resolution.width == Screen.width && resolution.height == Screen.height)
                {
                    currentResolutionIndex = availableResolutions.Count;
                    currentResolutionFound = true;
                }
                availableResolutions.Add(new Vector2Int(resolution.width, resolution.height));
            }

            if (!currentResolutionFound)
            {
                currentResolutionIndex = availableResolutions.Count;
                availableResolutions.Add(new Vector2Int(Screen.width, Screen.height));
            }
            if (!selectedResolutionFound)
                currentIndex = currentResolutionIndex;

            var labels = new List<string>(availableResolutions.Count);
            foreach (var resolution in availableResolutions)
                labels.Add($"{resolution.x} x {resolution.y}");
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(currentIndex, 0, labels.Count - 1));
            resolutionDropdown.RefreshShownValue();
        }

        private void PopulateQualityOptions(int selectedQuality)
        {
            var names = QualitySettings.names;
            qualityDropdown.ClearOptions();
            if (names.Length == 0)
            {
                qualityDropdown.AddOptions(new List<string> { "UNAVAILABLE" });
                qualityDropdown.interactable = false;
                return;
            }

            qualityDropdown.interactable = true;
            qualityDropdown.AddOptions(new List<string>(names));
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedQuality, 0, names.Length - 1));
            qualityDropdown.RefreshShownValue();
        }

        private void ApplyResolution(int index)
        {
            if (index < 0 || index >= availableResolutions.Count)
                return;
            var resolution = availableResolutions[index];
            if (resolution.x == Screen.width && resolution.y == Screen.height)
                return;

            previousResolutionWidth = Screen.width;
            previousResolutionHeight = Screen.height;
            previousFullscreen = Screen.fullScreen;
            RoboClashSaveService.SetResolution(resolution.x, resolution.y);
            resolutionChangePending = true;
            resolutionConfirmationSeconds = 10f;
            ShowConfirmation(
                $"KEEP {resolution.x} x {resolution.y}? REVERTING IN 10 SECONDS.",
                ConfirmResolution, false);
        }

        private void ConfirmResolution()
        {
            resolutionChangePending = false;
            RoboClashSaveService.Save();
        }

        private void RestorePreviousResolution()
        {
            if (!resolutionChangePending)
                return;

            resolutionChangePending = false;
            RoboClashSaveService.SetFullscreen(previousFullscreen);
            RoboClashSaveService.SetResolution(previousResolutionWidth, previousResolutionHeight);
        }

        private static void ApplyFrameRate(int index)
        {
            var rates = new[] { 30, 60, 120, 144, -1 };
            if (index >= 0 && index < rates.Length)
                RoboClashSaveService.SetTargetFrameRate(rates[index]);
        }

        private static int GetFrameRateOption(int frameRate)
        {
            switch (frameRate)
            {
                case 30: return 0;
                case 120: return 2;
                case 144: return 3;
                case -1: return 4;
                default: return 1;
            }
        }

        private void ConfirmAction()
        {
            confirmationAction?.Invoke();
            FinishConfirmation();
        }

        private void ConfirmResetSettings()
        {
            ShowConfirmation("RESET AUDIO AND DISPLAY SETTINGS TO DEFAULTS?",
                () =>
                {
                    RoboClashSaveService.ResetSettings();
                    Refresh();
                }, false);
        }

        private void CancelConfirmation()
        {
            if (resolutionChangePending)
                RestorePreviousResolution();
            FinishConfirmation();
        }

        private void FinishConfirmation()
        {
            confirmationAction = null;
            confirmationRoot.SetActive(false);
            if (confirmationClosesPanel)
            {
                Hide();
                backAction?.Invoke();
                return;
            }

            contentRoot.SetActive(true);
            Refresh();
        }

        private void ReturnToCaller()
        {
            Hide();
            backAction?.Invoke();
        }

        private Button CreateButton(Transform parent, string label, Vector2 minimum, Vector2 maximum,
            Action action)
        {
            var buttonObject = CreatePanel(parent, label + " Button", ControlColor, minimum, maximum);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            CreateText(buttonObject.transform, label, 14, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 minimum, Vector2 maximum)
        {
            var panel = CreateObject(name, parent);
            Place(panel.GetComponent<RectTransform>(), minimum, maximum);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string value, int size, Color color,
            Vector2 minimum, Vector2 maximum, TextAnchor alignment, string objectName = "Text")
        {
            var textObject = CreateObject(objectName, parent, typeof(CanvasRenderer), typeof(Text));
            Place(textObject.GetComponent<RectTransform>(), minimum, maximum);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(string objectName, Transform parent, params Type[] components)
        {
            var gameObject = new GameObject(objectName, MergeRectTransform(components));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Type[] MergeRectTransform(Type[] components)
        {
            var result = new Type[components.Length + 1];
            result[0] = typeof(RectTransform);
            Array.Copy(components, 0, result, 1, components.Length);
            return result;
        }

        private static void Place(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            Place(rect, Vector2.zero, Vector2.one);
        }
    }
}
```

### Match selection session and persistence handoff

**Source:** `Assets\Scripts\RoboClash\UI\RoboClashSession.cs`  
**Lines:** 249

```csharp
using System;
using UnityEngine;

namespace RoboClash
{
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
            gameMode = Enum.IsDefined(typeof(RoboClashGameMode), selectedGameMode)
                ? selectedGameMode
                : RoboClashGameMode.FreeForAll;
            PlayerPrefs.SetInt(GameModeKey, (int)gameMode);
            SaveSurvivalSettings(selectedSurvivalGrowth, selectedWaveBreakSeconds);
            SaveMatchTimerOption(selectedTimerOption);
            SaveModeRules(selectedKillTargetOption, selectedKnockoutLives, allowRespawns, duelKillTarget);
            hasPendingMatchSelection = true;
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
            var savedDifficulty = PlayerPrefs.GetInt(AIDifficultyKey, (int)RoboClashAIDifficulty.Normal);
            aiDifficulty = Enum.IsDefined(typeof(RoboClashAIDifficulty), savedDifficulty)
                ? (RoboClashAIDifficulty)savedDifficulty
                : RoboClashAIDifficulty.Normal;
            var savedGameMode = PlayerPrefs.GetInt(GameModeKey, (int)RoboClashGameMode.TimedDeathmatch);
            gameMode = Enum.IsDefined(typeof(RoboClashGameMode), savedGameMode)
                ? (RoboClashGameMode)savedGameMode
                : RoboClashGameMode.FreeForAll;
            survivalGrowth = Mathf.Clamp(PlayerPrefs.GetInt(SurvivalGrowthKey, 1), 1, 8);
            waveBreakSeconds = Mathf.Clamp(PlayerPrefs.GetFloat(WaveBreakKey, 5f), 0f, 20f);
            matchTimerOption = Mathf.Clamp(PlayerPrefs.GetInt(MatchTimerOptionKey, 1), 0, 2);
            killTargetOption = Mathf.Clamp(PlayerPrefs.GetInt(KillTargetOptionKey, 1), 0, 3);
            knockoutLives = Mathf.Clamp(PlayerPrefs.GetInt(KnockoutLivesKey, 3), 1, 10);
            respawnsEnabled = PlayerPrefs.GetInt(RespawnsEnabledKey, 1) != 0;
            duelUsesKillTarget = PlayerPrefs.GetInt(DuelUsesKillTargetKey, 1) != 0;
            preferencesLoaded = true;
        }
    }
}
```

### Player settings and career save service

**Source:** `Assets\Scripts\RoboClash\RoboClashSaveService.cs`  
**Lines:** 560

```csharp
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
                MigrateLegacySettings(current.settings);
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
                Validate(current);
                ApplySettings();
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
            Screen.fullScreen = value;
        }

        public static void SetResolution(int width, int height)
        {
            EnsureLoaded();
            if (width <= 0 || height <= 0 || !IsSupportedResolution(width, height))
            {
                Debug.LogWarning($"The requested RoboClash resolution {width} x {height} is not supported.");
                return;
            }

            current.settings.resolutionWidth = width;
            current.settings.resolutionHeight = height;
            Screen.SetResolution(width, height, current.settings.fullscreen);
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
            if (settings.resolutionWidth > 0 && settings.resolutionHeight > 0 &&
                IsSupportedResolution(settings.resolutionWidth, settings.resolutionHeight))
            {
                Screen.SetResolution(
                    settings.resolutionWidth, settings.resolutionHeight, settings.fullscreen);
            }
            else
            {
                Screen.fullScreen = settings.fullscreen;
            }

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
                fullscreen = Screen.fullScreen,
                resolutionWidth = Screen.width,
                resolutionHeight = Screen.height,
                qualityLevel = QualitySettings.names.Length > 0
                    ? Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)
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

        private static void MigrateLegacySettings(RoboClashPlayerSettings settings)
        {
            settings.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            settings.fullscreen = PlayerPrefs.GetInt(FullscreenKey, settings.fullscreen ? 1 : 0) != 0;
            var resolutions = Screen.resolutions;
            var savedIndex = PlayerPrefs.GetInt(ResolutionKey, -1);
            if (savedIndex < 0 || resolutions.Length == 0)
                return;

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
                return;
            }
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
            if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0 ||
                !IsSupportedResolution(settings.resolutionWidth, settings.resolutionHeight))
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

        private static bool IsSupportedResolution(int width, int height)
        {
            foreach (var resolution in Screen.resolutions)
            {
                if (resolution.width == width && resolution.height == height)
                    return true;
            }

            return width == Screen.width && height == Screen.height;
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
```

### Editor menu scene generator

**Source:** `Assets\Editor\RoboClashMenuSceneGenerator.cs`  
**Lines:** 214

```csharp
using System;
using System.Collections.Generic;
using RoboClash;
using RoboClash.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoboClashMenuSceneGenerator
{
    private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
    private const string ArenaScenePath = "Assets/Scenes/RoboClashGameplay.unity";
    private const string MenuRootName = "RoboClash Generated Main Menu";

    [MenuItem("Tools/RoboClash/Create Main Menu UI")]
    public static void CreateMainMenuScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
        {
            Debug.LogError(
                $"Menu scene already exists at '{MenuScenePath}'. It was not overwritten. Rename or back up that scene before generating a replacement.");
            return;
        }

        if (!ValidateArenaScene())
            return;

        if (!EditorUtility.DisplayDialog(
                "Create RoboClash Main Menu",
                "Create a new menu scene, register it before the gameplay scene in Build Settings, and make the menu the startup scene? Existing scene assets will be preserved.",
                "Create Menu",
                "Cancel"))
            return;

        var robotPrefabs = LoadRobotPrefabs();
        if (robotPrefabs == null)
            return;

        var previousActiveScene = SceneManager.GetActiveScene();
        var menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var root = new GameObject(MenuRootName);
            var controller = root.AddComponent<RoboClashMenuController>();
            controller.ConfigureRobotPrefabs(robotPrefabs);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(menuScene);

            if (!EditorSceneManager.SaveScene(menuScene, MenuScenePath))
                throw new InvalidOperationException("Unity could not save the RoboClash main menu scene.");

            ConfigureBuildSceneOrder();
            AssetDatabase.Refresh();

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            Debug.Log(
                $"Created '{MenuScenePath}' and made it the first enabled scene in Build Settings. " +
                "The menu scene builds its Canvas and EventSystem at runtime.",
                root);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not create the RoboClash menu scene: " + exception.Message);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null &&
                menuScene.IsValid() && menuScene.isLoaded)
                EditorSceneManager.CloseScene(menuScene, true);
            return;
        }

        if (menuScene.IsValid() && menuScene.isLoaded)
            EditorSceneManager.CloseScene(menuScene, true);

        var menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (menuAsset != null)
            EditorGUIUtility.PingObject(menuAsset);
    }

    private static GameObject[] LoadRobotPrefabs()
    {
        var prefabs = new GameObject[Enum.GetValues(typeof(RoboClashRobotType)).Length];
        foreach (RoboClashRobotType type in Enum.GetValues(typeof(RoboClashRobotType)))
        {
            var path = $"Assets/RoboClash/Generated/Prefabs/{type.ToString().ToUpperInvariant()}.prefab";
            prefabs[(int)type] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabs[(int)type] == null ||
                prefabs[(int)type].GetComponent<RoboClashRobotDefinition>() == null)
            {
                Debug.LogError($"Cannot create the menu: a valid generated robot prefab is missing at '{path}'.");
                return null;
            }
        }

        return prefabs;
    }

    private static bool ValidateArenaScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath) == null)
        {
            Debug.LogError(
                $"Cannot create the menu: gameplay scene '{ArenaScenePath}' does not exist. Create the arena first.");
            return false;
        }

        var previousActiveScene = SceneManager.GetActiveScene();
        Scene arenaScene = default;
        var openedHere = false;
        try
        {
            foreach (var loadedScene in GetLoadedScenes())
            {
                if (loadedScene.path != ArenaScenePath)
                    continue;
                arenaScene = loadedScene;
                break;
            }

            if (!arenaScene.IsValid())
            {
                arenaScene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            var manager = FindManagerInScene(arenaScene);
            if (manager == null)
            {
                Debug.LogError(
                    "Cannot create a connected main menu yet: Arena has no RoboClashMatchManager. " +
                    "Run Tools > RoboClash > Generate Arena, save it, then run Tools > RoboClash > Create Main Menu UI.");
                return false;
            }

            if (!manager.ValidateConfiguration(out var issue))
            {
                Debug.LogError("Cannot create a connected main menu: " + issue);
                return false;
            }

            var cameraFound = false;
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>())
            {
                if (camera.gameObject.scene == arenaScene && camera.isActiveAndEnabled &&
                    camera.CompareTag("MainCamera"))
                {
                    cameraFound = true;
                    break;
                }
            }

            if (!cameraFound)
            {
                Debug.LogError("Cannot create a connected main menu: Arena has no active MainCamera.");
                return false;
            }

            return true;
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            if (openedHere && arenaScene.IsValid() && arenaScene.isLoaded)
                EditorSceneManager.CloseScene(arenaScene, true);
        }
    }

    private static Scene[] GetLoadedScenes()
    {
        var scenes = new Scene[SceneManager.sceneCount];
        for (var i = 0; i < scenes.Length; i++)
            scenes[i] = SceneManager.GetSceneAt(i);
        return scenes;
    }

    private static RoboClashMatchManager FindManagerInScene(Scene scene)
    {
        foreach (var manager in UnityEngine.Object.FindObjectsByType<RoboClashMatchManager>())
        {
            if (manager.gameObject.scene == scene)
                return manager;
        }

        return null;
    }

    private static void ConfigureBuildSceneOrder()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ArenaScenePath, true)
        };

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path == MenuScenePath || existing.path == ArenaScenePath)
                continue;

            if (existing.path == "Assets/Scenes/Arena.unity")
            {
                scenes.Add(new EditorBuildSettingsScene(existing.path, false));
                continue;
            }

            scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
```

## Validation notes

This is a source-reference report, not a claim of Play Mode testing. It reproduces current source code and describes the menu flow from that code. Compile and interactive UI behavior should be verified in Unity after opening the existing menu scene and entering Play Mode.
