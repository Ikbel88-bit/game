using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
            var isMenuOwner = owner.GetComponent<RoboClashMenuController>() != null;
            EventSystem selected = null;
            InputSystemUIInputModule selectedInputModule = null;

            foreach (var candidate in eventSystems)
            {
                if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled)
                    continue;

                if (candidate.gameObject.name == eventSystemName)
                {
                    selected = candidate;
                    selectedInputModule = candidate.GetComponent<InputSystemUIInputModule>();
                    break;
                }
            }

            if (selected == null && !isMenuOwner)
            {
                foreach (var candidate in eventSystems)
                {
                    if (candidate.gameObject.scene != scene || !candidate.isActiveAndEnabled)
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

            if (!HasEnabledPointerActions(selectedInputModule))
            {
                Debug.LogWarning(
                    "[RoboClash UI] The active InputSystemUIInputModule did not have enabled Point and Left Click actions. " +
                    "Reassigning the Input System UI default actions.",
                    selectedInputModule);
                selectedInputModule.enabled = false;
                selectedInputModule.UnassignActions();
                selectedInputModule.AssignDefaultActions();
                selectedInputModule.enabled = true;
            }

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

            EventSystem.current = selected;

            if (!HasEnabledPointerActions(selectedInputModule))
            {
                Debug.LogError(
                    "[RoboClash UI] InputSystemUIInputModule still has no enabled Point and Left Click actions after repair.",
                    selectedInputModule);
            }
        }

        private static bool HasEnabledPointerActions(InputSystemUIInputModule module)
        {
            if (module == null ||
                module.point == null || module.point.action == null || !module.point.action.enabled ||
                module.leftClick == null || module.leftClick.action == null ||
                !module.leftClick.action.enabled)
                return false;

            return Mouse.current == null ||
                   (module.point.action.controls.Count > 0 &&
                    module.leftClick.action.controls.Count > 0);
        }
    }

    [DefaultExecutionOrder(-32000)]
    public sealed class RoboClashMenuController : MonoBehaviour
    {
        private const string ArenaSceneName = RoboClashSceneUtility.ActiveGameplaySceneName;
        private const string MenuScenePath = "Assets/Scenes/RoboClashMainMenu.unity";
        [SerializeField] private GameObject[] robotPrefabs = new GameObject[4];

        private static readonly Color PanelColor = new Color(0.03f, 0.06f, 0.11f, 0.72f);
        private static readonly Color BlueColor = new Color(0.30f, 0.70f, 1f);
        private static readonly Color OrangeColor = new Color(1f, 0.58f, 0.16f);
        private static readonly Color GlassHotColor = new Color(0.08f, 0.16f, 0.26f, 0.88f);
        private static readonly Color OrangeHotColor = new Color(1f, 0.72f, 0.35f);
        private static readonly Color MutedColor = new Color(0.62f, 0.72f, 0.82f);
        private static Sprite hazardStripeSprite;
        private static readonly string[] GameModeKeys = { "KR", "TD", "VS", "SW", "KA", "KO", "MP" };
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
        private TMP_FontAsset font;
        private GameObject mainScreen;
        private GameObject selectionScreen;
        private GameObject settingsScreen;
        private GameObject matchSetupScreen;
        private GameObject duelLobbyScreen;
        private GameObject matchRulesPanel;
        private GameObject modeObjectivePanel;
        private GameObject multiplayerSetupPanel;
        private GameObject matchSummaryScreen;
        private GameObject careerScreen;
        private TextMeshProUGUI selectionName;
        private TextMeshProUGUI selectionRole;
        private TextMeshProUGUI selectionDescription;
        private TextMeshProUGUI selectionStats;
        private TextMeshProUGUI selectionWeapon;
        private TextMeshProUGUI selectionAbility;
        private TextMeshProUGUI selectionUltimate;
        private TextMeshProUGUI matchSummaryText;
        private TextMeshProUGUI careerDetailsText;
        private TextMeshProUGUI matchStartErrorText;
        private TextMeshProUGUI opponentCountText;
        private TextMeshProUGUI opponentCountLabel;
        private TextMeshProUGUI modeSettingsHint;
        private TextMeshProUGUI duelPlayerCountLabel;
        private TextMeshProUGUI modeRuleLabel;
        private TextMeshProUGUI modeObjectiveText;
        private TextMeshProUGUI multiplayerObjectiveText;
        private TextMeshProUGUI multiplayerControllerText;
        private TextMeshProUGUI multiplayerErrorText;
        private TextMeshProUGUI selectedRobotText;
        private TextMeshProUGUI careerSummaryText;
        private TextMeshProUGUI featuredRobotName;
        private TextMeshProUGUI phoneStatusText;
        private float nextPhoneStatusRefresh;
        private Button confirmButton;
        private Button[] robotCardButtons;
        private Image[] robotCardBackgrounds;
        private Button[] gameModeCardButtons;
        private Image[] gameModeCardBackgrounds;
        private TextMeshProUGUI[] gameModeCardLabels;
        private Button[] multiplayerPlayerCountButtons;
        private Image[] multiplayerPlayerCountBackgrounds;
        private Button[] duelPlayerCountButtons;
        private Image[] duelPlayerCountBackgrounds;
        private Button[] multiplayerKillTargetButtons;
        private Image[] multiplayerKillTargetBackgrounds;
        private TextMeshProUGUI[] multiplayerRobotLabels;
        private TextMeshProUGUI[] multiplayerPlayerLabels;
        private TextMeshProUGUI[] duelLobbyPlayerLabels;
        private TextMeshProUGUI[] duelLobbyRobotLabels;
        private TextMeshProUGUI[] duelLobbyRoleLabels;
        private Button[] duelLobbyPreviousButtons;
        private Button[] duelLobbyNextButtons;
        private Button[] duelLobbyReadyButtons;
        private Image[] duelLobbyPanelBackgrounds;
        private Button[] multiplayerRobotPreviousButtons;
        private Button[] multiplayerRobotNextButtons;
        private RoboClashSettingsPanel settingsPanel;
        private TMP_Dropdown aiDifficultyDropdown;
        private GameObject aiDifficultySelectionScreen;
        private TextMeshProUGUI aiDifficultySelectionTitle;
        private TextMeshProUGUI aiDifficultySelectionDescription;
        private Button[] aiDifficultyButtons;
        private TextMeshProUGUI[] aiDifficultyLabels;
        private TMP_Dropdown survivalGrowthDropdown;
        private TMP_Dropdown waveBreakDropdown;
        private TMP_Dropdown modeRuleDropdown;
        private TMP_Dropdown respawnDropdown;
        private Button decreaseOpponentButton;
        private Button increaseOpponentButton;
        private Button playButton;
        private Button startMatchButton;
        private Button normalSetupBackButton;
        private Button chooseRobotButton;
        private Button multiplayerBackButton;
        private Button multiplayerStartButton;
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
        private int multiplayerPlayerCount;
        private int duelPhonePlayerCount;
        private RoboClashRobotType[] multiplayerRobots;
        private RoboClashRobotType[] duelRobots;
        private readonly bool[] duelLobbyReady = new bool[4];

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
            multiplayerPlayerCount = RoboClashSession.LocalPlayerCount;
            duelPhonePlayerCount = RoboClashSession.DuelPhonePlayerCount;
            multiplayerRobots = RoboClashSession.MultiplayerRobots;
            duelRobots = RoboClashSession.DuelRobots;
            EnsureEventSystem();
            RoboClashSaveService.ApplySettings();
            BuildInterface();
            ShowMainMenu();
        }

        private void OnEnable()
        {
            EnsureMenuCursor();
        }

        private void LateUpdate()
        {
            EnsureMenuCursor();
        }

        private void EnsureMenuCursor()
        {
            if (!string.Equals(
                    gameObject.scene.path,
                    MenuScenePath,
                    StringComparison.OrdinalIgnoreCase))
                return;

            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;
        }

        private void DisableGameplaySystemsInMenuScene()
        {
            foreach (var manager in FindObjectsByType<RoboClashMatchManager>(FindObjectsInactive.Include))
            {
                if (manager.gameObject.scene != gameObject.scene)
                    continue;

                var hud = manager.GetComponent<RoboClashHUD>();
                if (hud != null)
                    RemoveGameplayHud(hud);

                foreach (var eventSystem in manager.GetComponentsInChildren<EventSystem>(true))
                {
                    if (eventSystem.gameObject.name != "RoboClash Gameplay Event System")
                        continue;

                    foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                        module.enabled = false;
                    eventSystem.enabled = false;
                    Destroy(eventSystem.gameObject);
                }

                manager.enabled = false;
                Destroy(manager);
                Debug.LogWarning(
                    "Removed a misplaced RoboClashMatchManager and its runtime HUD from the main-menu scene. " +
                    "Keep gameplay match systems in a gameplay scene.",
                    manager);
            }

            foreach (var hud in FindObjectsByType<RoboClashHUD>(FindObjectsInactive.Include))
            {
                if (hud.gameObject.scene == gameObject.scene && hud.enabled)
                    RemoveGameplayHud(hud);
            }
        }

        private void RemoveGameplayHud(RoboClashHUD hud)
        {
            hud.enabled = false;
            foreach (var existingCanvas in hud.GetComponentsInChildren<Canvas>(true))
            {
                if (existingCanvas.gameObject.name == "RoboClash Gameplay Canvas")
                    Destroy(existingCanvas.gameObject);
            }

            Destroy(hud);
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

            if (duelLobbyScreen != null && duelLobbyScreen.activeSelf)
            {
                ProcessDuelLobbyPhoneInput();
                UpdateDuelLobbyVisuals();
            }

            if (Time.unscaledTime >= nextPhoneStatusRefresh)
            {
                nextPhoneStatusRefresh = Time.unscaledTime + 0.5f;
                if (phoneStatusText != null)
                {
                    phoneStatusText.text = RoboClashPhoneInputServer.StatusText;
                    phoneStatusText.color = RoboClashPhoneInputServer.IsPhoneActive
                        ? BlueColor
                        : MutedColor;
                }
                if (multiplayerControllerText != null)
                    multiplayerControllerText.text =
                        $"CONTROLLERS DETECTED: {UnityEngine.InputSystem.Gamepad.all.Count}";
            }

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
            else if (duelLobbyScreen != null && duelLobbyScreen.activeSelf)
                BackFromDuelLobby();
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
            font = TMP_Settings.defaultFontAsset;
            var canvasObject = new GameObject("RoboClash Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RoboClashMenuBackdrop.Create(canvas.transform);
            BuildRobotPreviewCamera();
            mainScreen = CreateScreen("Main Menu");
            selectionScreen = CreateScreen("Robot Selection");
            matchSetupScreen = CreateScreen("Match Setup");
            duelLobbyScreen = CreateScreen("Duel Robot Selection");
            aiDifficultySelectionScreen = CreateScreen("AI Difficulty");
            matchSummaryScreen = CreateScreen("Match Summary");
            careerScreen = CreateScreen("Career Statistics");
            settingsScreen = CreateScreen("Settings");
            BuildMainScreen();
            BuildSelectionScreen();
            BuildMatchSetupScreen();
            BuildDuelLobbyScreen();
            BuildDifficultySelectionScreen();
            BuildMatchSummaryScreen();
            BuildCareerScreen();
            BuildSettingsScreen();
            phoneStatusText = AddText(canvas.transform, RoboClashPhoneInputServer.StatusText,
                11, MutedColor, TextAnchor.LowerRight,
                new Vector2(0.66f, 0.012f), new Vector2(0.99f, 0.05f),
                new Vector2(0f, 0f), new Vector2(-12f, 0f));
            RoboClashUiKit.StyleText(phoneStatusText, Mathf.RoundToInt(phoneStatusText.fontSize), true);
        }

        private void BuildRobotPreviewCamera()
        {
            previewTexture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)
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
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
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
            CreatePreviewLight("Menu Preview Warm Rim Light", new Vector3(12f, -145f, 0f),
                new Color(1f, 0.48f, 0.2f), 0.55f);
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
            var screen = CreatePanel(screenName, canvas.transform, Color.clear);
            Stretch(screen.GetComponent<RectTransform>());
            screen.GetComponent<Image>().raycastTarget = false;
            screen.AddComponent<CanvasGroup>();
            screen.AddComponent<RoboClashMenuScreenFade>();
            if (screenName != "Main Menu")
            {
                var dim = CreatePanel("Readability Dim", screen.transform,
                    new Color(0f, 0.01f, 0.03f, 0.55f));
                Stretch(dim.GetComponent<RectTransform>());
            }
            AddFullScreenAccent(screen.transform);
            return screen;
        }

        private static Sprite HazardStripeSprite()
        {
            if (hazardStripeSprite != null)
                return hazardStripeSprite;

            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    texture.SetPixel(x, y, (x + y) % 16 < 8
                        ? new Color(1f, 0.78f, 0.1f)
                        : new Color(0.04f, 0.04f, 0.05f));
                }
            }
            texture.Apply();
            hazardStripeSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return hazardStripeSprite;
        }

        private void AddFullScreenAccent(Transform parent)
        {
            var topRule = CreatePanel("Blue Accent", parent,
                new Color(BlueColor.r, BlueColor.g, BlueColor.b, 0.4f));
            Place(topRule.GetComponent<RectTransform>(), new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(0f, -2f), Vector2.zero);

            var stripe = CreatePanel("Hazard Strip", parent, new Color(1f, 1f, 1f, 0.85f));
            var image = stripe.GetComponent<Image>();
            image.sprite = HazardStripeSprite();
            image.type = Image.Type.Tiled;
            Place(stripe.GetComponent<RectTransform>(), Vector2.zero,
                new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 10f));
        }

        private void BuildMainScreen()
        {
            var title = AddText(mainScreen.transform, "ROBOCLASH", 76, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.075f, 0.77f), new Vector2(0.53f, 0.94f), Vector2.zero, Vector2.zero, true);
            title.fontStyle = FontStyles.Bold;
            var titleRule = CreatePanel("Title Orange Rule", mainScreen.transform, OrangeColor);
            Place(titleRule.GetComponent<RectTransform>(), new Vector2(0.08f, 0.745f),
                new Vector2(0.155f, 0.745f), Vector2.zero, new Vector2(0f, 3f));
            var subtitle = AddText(mainScreen.transform, "MECH ARENA  //  SINGLE PLAYER",
                16, BlueColor, TextAnchor.MiddleLeft,
                new Vector2(0.08f, 0.72f), new Vector2(0.51f, 0.78f), Vector2.zero, Vector2.zero);
            var tagline = AddText(mainScreen.transform, "ENTER THE ARENA. ADAPT. ELIMINATE.", 17, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.08f, 0.65f), new Vector2(0.51f, 0.71f),
                Vector2.zero, Vector2.zero);
            CreateRobotPreview(mainScreen.transform,
                new Vector2(0.50f, 0.08f), new Vector2(0.98f, 0.90f));

            playButton = CreateButton(mainScreen.transform, "PLAY", new Vector2(0.08f, 0.51f),
                new Vector2(0.36f, 0.60f), OrangeColor, OpenMatchSetup, true);
            CreateButton(mainScreen.transform, "ROBOT GARAGE", new Vector2(0.08f, 0.40f),
                new Vector2(0.36f, 0.49f), PanelColor, OpenRobotGarage);
            CreateButton(mainScreen.transform, "SETTINGS", new Vector2(0.08f, 0.29f),
                new Vector2(0.36f, 0.38f), PanelColor, OpenSettings);
            CreateButton(mainScreen.transform, "CAREER / STATISTICS", new Vector2(0.08f, 0.18f),
                new Vector2(0.36f, 0.27f), PanelColor, OpenCareer);
            CreateButton(mainScreen.transform, "QUIT", new Vector2(0.08f, 0.07f),
                new Vector2(0.36f, 0.16f), PanelColor, QuitGame);

            var info = CreatePanel("Featured Robot", mainScreen.transform, PanelColor);
            Place(info.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.92f, 0.30f),
                Vector2.zero, Vector2.zero);
            featuredRobotName = AddText(info.transform, string.Empty, 27, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);
            selectedRobotText = AddText(info.transform, string.Empty, 13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.48f), Vector2.zero, Vector2.zero);
            careerSummaryText = AddText(mainScreen.transform, string.Empty, 12, MutedColor,
                TextAnchor.LowerLeft, new Vector2(0.08f, 0.01f), new Vector2(0.48f, 0.065f),
                Vector2.zero, Vector2.zero);
            RoboClashUiKit.StyleText(subtitle, Mathf.RoundToInt(subtitle.fontSize), true);
            RoboClashUiKit.StyleText(tagline, Mathf.RoundToInt(tagline.fontSize), true);
            RoboClashUiKit.StyleText(featuredRobotName, Mathf.RoundToInt(featuredRobotName.fontSize), true);
            RoboClashUiKit.StyleText(selectedRobotText, Mathf.RoundToInt(selectedRobotText.fontSize), true);
            RoboClashUiKit.StyleText(careerSummaryText, Mathf.RoundToInt(careerSummaryText.fontSize), true);
        }

        private void BuildSelectionScreen()
        {
            AddText(selectionScreen.transform, "ROBOT GARAGE", 38, BlueColor, TextAnchor.MiddleLeft,
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
            selectionRole = AddText(details.transform, string.Empty, 15, BlueColor, TextAnchor.MiddleLeft,
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
            selectionStats.fontSize = 16;
            selectionWeapon.fontSize = 16;
            selectionAbility.fontSize = 16;
            selectionUltimate.fontSize = 16;
            CreateRobotPreview(details.transform,
                new Vector2(0.72f, 0.08f), new Vector2(0.98f, 0.94f));

            CreateButton(selectionScreen.transform, "BACK",
                new Vector2(0.06f, 0.08f), new Vector2(0.23f, 0.17f),
                new Color(0.06f, 0.15f, 0.23f), BackFromRobotGarage);
            confirmButton = CreateButton(selectionScreen.transform, "SELECT ROBOT",
                new Vector2(0.73f, 0.08f), new Vector2(0.94f, 0.17f), OrangeColor,
                ConfirmRobotSelection, true);
            UpdateRobotDetails();
        }

        private void BuildMatchSetupScreen()
        {
            AddText(matchSetupScreen.transform, "CHOOSE YOUR GAME MODE", 34, BlueColor,
                TextAnchor.MiddleLeft, new Vector2(0.055f, 0.89f), new Vector2(0.68f, 0.97f),
                Vector2.zero, Vector2.zero);
            AddText(matchSetupScreen.transform, "SELECT A MODE, THEN TUNE ONLY THE RULES THAT APPLY",
                13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.84f), new Vector2(0.68f, 0.90f), Vector2.zero, Vector2.zero);

            gameModeCardButtons = new Button[7];
            gameModeCardBackgrounds = new Image[7];
            gameModeCardLabels = new TextMeshProUGUI[7];
            for (var i = 0; i < gameModeCardButtons.Length; i++)
            {
                var index = i;
                var column = i % 4;
                var row = i / 4;
                var left = 0.06f + column * 0.155f;
                var bottom = row == 0 ? 0.49f : 0.17f;
                var card = CreateButton(matchSetupScreen.transform,
                    BuildModeCardText(i, GameModeKeys[i]),
                    new Vector2(left, bottom), new Vector2(left + 0.145f, bottom + 0.29f),
                    PanelColor, () => SelectGameModeCard(index));
                gameModeCardButtons[i] = card;
                gameModeCardBackgrounds[i] = card.GetComponent<Image>();
                var label = card.GetComponentInChildren<TextMeshProUGUI>();
                gameModeCardLabels[i] = label;
                label.fontSize = 11;
                label.richText = false;
                label.lineSpacing = 1.15f;
            }

            matchRulesPanel = CreatePanel("Match Rules", matchSetupScreen.transform, PanelColor);
            var optionsPanel = matchRulesPanel;
            Place(optionsPanel.GetComponent<RectTransform>(), new Vector2(0.70f, 0.15f),
                new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "MATCH RULES", 18, BlueColor, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.99f), Vector2.zero, Vector2.zero);
            AddText(optionsPanel.transform, "AI DIFFICULTY", 12, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.87f), Vector2.zero, Vector2.zero);
            aiDifficultyDropdown = CreateDropdown(optionsPanel.transform,
                new Vector2(0.04f, 0.71f), new Vector2(0.96f, 0.79f));
            aiDifficultyDropdown.ClearOptions();
            aiDifficultyDropdown.AddOptions(new List<string>
            {
                GetAIDifficultyLabel(RoboClashAIDifficulty.Easy),
                GetAIDifficultyLabel(RoboClashAIDifficulty.Normal),
                GetAIDifficultyLabel(RoboClashAIDifficulty.Hard)
            });
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
            modeSettingsHint = AddText(optionsPanel.transform, string.Empty, 11, BlueColor,
                TextAnchor.MiddleCenter, new Vector2(0.02f, 0.41f), new Vector2(0.98f, 0.47f),
                Vector2.zero, Vector2.zero);
            duelPlayerCountLabel = AddText(optionsPanel.transform, "PHONE PLAYERS",
                10, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.41f), new Vector2(0.34f, 0.47f),
                Vector2.zero, Vector2.zero);
            duelPlayerCountButtons = new Button[2];
            duelPlayerCountBackgrounds = new Image[2];
            for (var i = 0; i < duelPlayerCountButtons.Length; i++)
            {
                var count = i == 0 ? 2 : 4;
                var left = i == 0 ? 0.38f : 0.68f;
                var button = CreateButton(optionsPanel.transform, count.ToString(),
                    new Vector2(left, 0.405f), new Vector2(left + 0.24f, 0.475f),
                    PanelColor, () => SetDuelPlayerCount(count));
                duelPlayerCountButtons[i] = button;
                duelPlayerCountBackgrounds[i] = button.GetComponent<Image>();
            }
            modeRuleLabel = AddText(optionsPanel.transform, "MODE RULES", 12, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.40f),
                Vector2.zero, Vector2.zero);
            modeRuleDropdown = CreateModeRuleDropdown(optionsPanel.transform);
            respawnDropdown = CreateRespawnDropdown(optionsPanel.transform);
            survivalGrowthDropdown = CreateSurvivalGrowthDropdown(optionsPanel.transform);
            waveBreakDropdown = CreateWaveBreakDropdown(optionsPanel.transform);

            modeObjectivePanel = CreatePanel("Mode Objective", matchSetupScreen.transform, PanelColor);
            var objectivePanel = modeObjectivePanel;
            Place(objectivePanel.GetComponent<RectTransform>(), new Vector2(0.255f, 0.025f),
                new Vector2(0.72f, 0.145f), Vector2.zero, Vector2.zero);
            modeObjectiveText = AddText(objectivePanel.transform, string.Empty, 11, Color.white,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero);
            modeObjectiveText.textWrappingMode = TextWrappingModes.Normal;

            normalSetupBackButton = CreateButton(matchSetupScreen.transform, "BACK", new Vector2(0.06f, 0.06f),
                new Vector2(0.23f, 0.14f), new Color(0.06f, 0.15f, 0.23f), ShowMainMenu);
            chooseRobotButton = CreateButton(matchSetupScreen.transform, "CHOOSE ROBOT",
                new Vector2(0.77f, 0.06f), new Vector2(0.95f, 0.14f), OrangeColor,
                () => OpenRobotGarage(true), true);
            BuildMultiplayerSetupPanel();
            UpdateGameModeCards();
        }

        private void BuildMultiplayerSetupPanel()
        {
            multiplayerSetupPanel = CreatePanel("Local Multiplayer Setup",
                matchSetupScreen.transform, PanelColor);
            Place(multiplayerSetupPanel.GetComponent<RectTransform>(),
                new Vector2(0.055f, 0.15f), new Vector2(0.68f, 0.82f),
                Vector2.zero, Vector2.zero);
            AddText(multiplayerSetupPanel.transform, "LOCAL MULTIPLAYER", 21, BlueColor,
                TextAnchor.MiddleCenter, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.99f),
                Vector2.zero, Vector2.zero);

            AddText(multiplayerSetupPanel.transform, "PLAYERS", 12, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.80f), new Vector2(0.28f, 0.88f),
                Vector2.zero, Vector2.zero);
            multiplayerPlayerCountButtons = new Button[3];
            multiplayerPlayerCountBackgrounds = new Image[3];
            for (var i = 0; i < multiplayerPlayerCountButtons.Length; i++)
            {
                var count = i + 2;
                var x = 0.28f + i * 0.14f;
                var button = CreateButton(multiplayerSetupPanel.transform, count.ToString(),
                    new Vector2(x, 0.79f), new Vector2(x + 0.12f, 0.88f), PanelColor,
                    () => SetMultiplayerPlayerCount(count));
                multiplayerPlayerCountButtons[i] = button;
                multiplayerPlayerCountBackgrounds[i] = button.GetComponent<Image>();
            }

            AddText(multiplayerSetupPanel.transform, "KILL TARGET", 12, MutedColor,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.68f), new Vector2(0.24f, 0.76f),
                Vector2.zero, Vector2.zero);
            multiplayerKillTargetButtons = new Button[4];
            multiplayerKillTargetBackgrounds = new Image[4];
            for (var i = 0; i < multiplayerKillTargetButtons.Length; i++)
            {
                var option = i;
                var x = 0.27f + i * 0.14f;
                var button = CreateButton(multiplayerSetupPanel.transform,
                    new[] { "5", "10", "15", "20" }[i],
                    new Vector2(x, 0.67f), new Vector2(x + 0.12f, 0.76f), PanelColor,
                    () => SetMultiplayerKillTarget(option));
                multiplayerKillTargetButtons[i] = button;
                multiplayerKillTargetBackgrounds[i] = button.GetComponent<Image>();
            }

            multiplayerRobotLabels = new TextMeshProUGUI[4];
            multiplayerPlayerLabels = new TextMeshProUGUI[4];
            multiplayerRobotPreviousButtons = new Button[4];
            multiplayerRobotNextButtons = new Button[4];
            for (var i = 0; i < 4; i++)
            {
                var playerIndex = i;
                var bottom = 0.56f - i * 0.115f;
                multiplayerPlayerLabels[i] = AddText(multiplayerSetupPanel.transform, $"P{i + 1}",
                    14, Color.white, TextAnchor.MiddleLeft,
                    new Vector2(0.06f, bottom), new Vector2(0.20f, bottom + 0.09f),
                    Vector2.zero, Vector2.zero);
                multiplayerRobotPreviousButtons[i] = CreateButton(multiplayerSetupPanel.transform, "<",
                    new Vector2(0.25f, bottom), new Vector2(0.34f, bottom + 0.09f),
                    new Color(0.06f, 0.15f, 0.23f), () => CycleMultiplayerRobot(playerIndex, -1));
                multiplayerRobotLabels[i] = AddText(multiplayerSetupPanel.transform,
                    string.Empty, 14, BlueColor, TextAnchor.MiddleCenter,
                    new Vector2(0.35f, bottom), new Vector2(0.58f, bottom + 0.09f),
                    Vector2.zero, Vector2.zero);
                multiplayerRobotNextButtons[i] = CreateButton(multiplayerSetupPanel.transform, ">",
                    new Vector2(0.59f, bottom), new Vector2(0.68f, bottom + 0.09f),
                    new Color(0.06f, 0.15f, 0.23f), () => CycleMultiplayerRobot(playerIndex, 1));
            }

            multiplayerControllerText = AddText(multiplayerSetupPanel.transform,
                "CONTROLLERS DETECTED: 0", 12, MutedColor, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.075f), new Vector2(0.95f, 0.12f),
                Vector2.zero, Vector2.zero);
            multiplayerObjectiveText = AddText(multiplayerSetupPanel.transform,
                string.Empty, 13, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.005f), new Vector2(0.96f, 0.075f),
                Vector2.zero, Vector2.zero);
            multiplayerErrorText = AddText(multiplayerSetupPanel.transform,
                string.Empty, 11, new Color(1f, 0.34f, 0.3f), TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.20f),
                Vector2.zero, Vector2.zero);

            multiplayerBackButton = CreateButton(matchSetupScreen.transform, "BACK TO MODES",
                new Vector2(0.06f, 0.06f), new Vector2(0.23f, 0.14f),
                new Color(0.06f, 0.15f, 0.23f), ExitMultiplayerSetup);
            multiplayerStartButton = CreateButton(matchSetupScreen.transform, "START MULTIPLAYER",
                new Vector2(0.43f, 0.06f), new Vector2(0.68f, 0.14f), OrangeColor,
                StartMultiplayerMatch, true);
            multiplayerBackButton.gameObject.SetActive(false);
            multiplayerStartButton.gameObject.SetActive(false);
            multiplayerSetupPanel.SetActive(false);
            UpdateMultiplayerSetup();
        }

        private void BuildDuelLobbyScreen()
        {
            AddText(duelLobbyScreen.transform, "DUEL ROBOT SELECT", 34, BlueColor,
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.91f), new Vector2(0.94f, 0.98f),
                Vector2.zero, Vector2.zero);
            AddText(duelLobbyScreen.transform,
                "EACH PLAYER PICKS IN THEIR PANEL. ALL PLAYERS MUST PRESS START TO LAUNCH.",
                14, MutedColor, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.91f),
                Vector2.zero, Vector2.zero);

            duelLobbyPlayerLabels = new TextMeshProUGUI[4];
            duelLobbyRobotLabels = new TextMeshProUGUI[4];
            duelLobbyRoleLabels = new TextMeshProUGUI[4];
            duelLobbyPreviousButtons = new Button[4];
            duelLobbyNextButtons = new Button[4];
            duelLobbyReadyButtons = new Button[4];
            duelLobbyPanelBackgrounds = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var playerIndex = i;
                var panel = CreatePanel($"Duel Player {i + 1} Selection", duelLobbyScreen.transform, PanelColor);
                duelLobbyPanelBackgrounds[i] = panel.GetComponent<Image>();
                duelLobbyPlayerLabels[i] = AddText(panel.transform, string.Empty, 17, BlueColor,
                    TextAnchor.MiddleCenter, new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.96f),
                    Vector2.zero, Vector2.zero);
                duelLobbyRobotLabels[i] = AddText(panel.transform, string.Empty, 28, Color.white,
                    TextAnchor.MiddleCenter, new Vector2(0.04f, 0.53f), new Vector2(0.96f, 0.77f),
                    Vector2.zero, Vector2.zero);
                duelLobbyRoleLabels[i] = AddText(panel.transform, string.Empty, 13, MutedColor,
                    TextAnchor.MiddleCenter, new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.53f),
                    Vector2.zero, Vector2.zero);
                duelLobbyPreviousButtons[i] = CreateButton(panel.transform, "< PREVIOUS",
                    new Vector2(0.06f, 0.22f), new Vector2(0.44f, 0.36f),
                    new Color(0.06f, 0.15f, 0.23f), () => CycleDuelRobot(playerIndex, -1));
                duelLobbyNextButtons[i] = CreateButton(panel.transform, "NEXT >",
                    new Vector2(0.56f, 0.22f), new Vector2(0.94f, 0.36f),
                    new Color(0.06f, 0.15f, 0.23f), () => CycleDuelRobot(playerIndex, 1));
                duelLobbyReadyButtons[i] = CreateButton(panel.transform, "START",
                    new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.18f),
                    OrangeColor, () => ToggleDuelPlayerReady(playerIndex), true);

                var column = i % 2;
                var row = i / 2;
                var xMin = column == 0 ? 0.04f : 0.52f;
                var xMax = column == 0 ? 0.48f : 0.96f;
                var yMin = row == 0 ? 0.19f : 0.51f;
                var yMax = row == 0 ? 0.49f : 0.81f;
                Place(panel.GetComponent<RectTransform>(), new Vector2(xMin, yMin),
                    new Vector2(xMax, yMax), Vector2.zero, Vector2.zero);
            }

            CreateButton(duelLobbyScreen.transform, "BACK",
                new Vector2(0.04f, 0.06f), new Vector2(0.20f, 0.14f),
                new Color(0.06f, 0.15f, 0.23f), BackFromDuelLobby);
            AddText(duelLobbyScreen.transform,
                "PHONE: JUMP = NEXT  |  RELOAD = PREVIOUS  |  ULTIMATE = START",
                12, MutedColor, TextAnchor.MiddleCenter,
                new Vector2(0.22f, 0.06f), new Vector2(0.96f, 0.14f),
                Vector2.zero, Vector2.zero);
            UpdateDuelLobbyVisuals();
        }

        private void BuildDifficultySelectionScreen()
        {
            if (aiDifficultySelectionScreen == null)
                return;

            AddText(aiDifficultySelectionScreen.transform, "SELECT DIFFICULTY", 34, BlueColor,
                TextAnchor.MiddleLeft,
                new Vector2(0.055f, 0.89f), new Vector2(0.68f, 0.97f),
                Vector2.zero, Vector2.zero);
            AddText(aiDifficultySelectionScreen.transform,
                "PICK THE AI TIER FOR THIS MATCH BEFORE YOU LOCK IN THE RULES.",
                13, MutedColor, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.84f), new Vector2(0.68f, 0.90f),
                Vector2.zero, Vector2.zero);

            var panel = CreatePanel("AI Difficulty Panel", aiDifficultySelectionScreen.transform, PanelColor);
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.18f, 0.17f),
                new Vector2(0.82f, 0.80f), Vector2.zero, Vector2.zero);
            aiDifficultySelectionTitle = AddText(panel.transform, string.Empty, 26, Color.white,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.92f),
                Vector2.zero, Vector2.zero);
            aiDifficultySelectionDescription = AddText(panel.transform, string.Empty, 15, MutedColor,
                TextAnchor.UpperLeft,
                new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.76f),
                Vector2.zero, Vector2.zero);

            aiDifficultyButtons = new Button[3];
            aiDifficultyLabels = new TextMeshProUGUI[3];
            var buttonPositions = new[]
            {
                new Vector2(0.10f, 0.12f), new Vector2(0.36f, 0.12f), new Vector2(0.62f, 0.12f)
            };
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var difficulty = (RoboClashAIDifficulty)i;
                var profile = RoboClashAIProfiles.Get(difficulty);
                var button = CreateButton(panel.transform,
                    profile.label + "\n" + profile.description,
                    buttonPositions[i],
                    new Vector2(buttonPositions[i].x + 0.22f, buttonPositions[i].y + 0.20f),
                    PanelColor, () => SelectAIDifficulty(difficulty));
                aiDifficultyButtons[i] = button;
                aiDifficultyLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                if (aiDifficultyLabels[i] != null)
                {
                    aiDifficultyLabels[i].fontSize = 18;
                    aiDifficultyLabels[i].fontStyle = FontStyles.Bold;
                    aiDifficultyLabels[i].alignment = TextAlignmentOptions.Center;
                    aiDifficultyLabels[i].richText = false;
                }
            }

            CreateButton(aiDifficultySelectionScreen.transform, "BACK",
                new Vector2(0.06f, 0.06f), new Vector2(0.23f, 0.14f),
                new Color(0.06f, 0.15f, 0.23f), BackFromDifficultySelection);
            UpdateDifficultySelectionVisuals();
        }

        private void BuildMatchSummaryScreen()
        {
            AddText(matchSummaryScreen.transform, "MATCH SUMMARY", 38, BlueColor, TextAnchor.MiddleCenter,
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
                new Vector2(0.66f, 0.10f), new Vector2(0.88f, 0.19f), OrangeColor,
                StartSelectedMatch, true);
        }

        private void BuildCareerScreen()
        {
            AddText(careerScreen.transform, "CAREER // STATISTICS", 38, BlueColor, TextAnchor.MiddleCenter,
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
            settingsPanel.Root.AddComponent<CanvasGroup>();
            settingsPanel.Root.AddComponent<RoboClashMenuScreenFade>();
        }

        private void ShowMainMenu()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(true);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
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
            if (multiplayerSetupPanel != null)
                SetMultiplayerSetupVisible(false);
            SelectFirstSelectable(mainScreen);
        }

        private void OpenMatchSetup()
        {
            settingsPanel?.Hide();
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(true);
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            SetMultiplayerSetupVisible(gameMode == RoboClashGameMode.Multiplayer);
            UpdateGameModeCards();
            aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
            aiDifficultyDropdown.RefreshShownValue();
            UpdateDifficultySelectionVisuals();
            survivalGrowthDropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            waveBreakDropdown.SetValueWithoutNotify(GetWaveBreakOption(waveBreakSeconds));
            survivalGrowthDropdown.RefreshShownValue();
            waveBreakDropdown.RefreshShownValue();
            UpdateModeOptions();
            UpdateOpponentCountLabel();
            SelectFirstSelectable(matchSetupScreen);
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
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(true);
            SelectRobot(selectedRobotIndex);
            SelectFirstSelectable(selectionScreen);
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
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(true);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            garageReturnsToSummary = false;
            RefreshMatchSummary();
            SelectFirstSelectable(matchSummaryScreen);
        }

        private void OpenCareer()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(true);
            settingsScreen.SetActive(false);
            SetRobotPreviewVisible(false);
            RefreshCareerDetails();
            SelectFirstSelectable(careerScreen);
        }

        private void OpenSettings()
        {
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            duelLobbyScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(true);
            SetRobotPreviewVisible(false);
            settingsPanel.Show();
            SelectFirstSelectable(settingsPanel.Root);
        }

        private static void SelectFirstSelectable(GameObject root)
        {
            var eventSystem = EventSystem.current;
            if (root == null || eventSystem == null)
                return;

            foreach (var selectable in root.GetComponentsInChildren<Selectable>())
            {
                if (!selectable.isActiveAndEnabled || !selectable.interactable)
                    continue;

                eventSystem.SetSelectedGameObject(selectable.gameObject);
                return;
            }
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
                    ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f)
                    : PanelColor;
                var label = robotCardButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = selected ? OrangeColor : Color.white;
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
            SetMultiplayerSetupVisible(gameMode == RoboClashGameMode.Multiplayer);
            opponentCount = Mathf.Clamp(opponentCount,
                gameMode == RoboClashGameMode.Duel ? 0 : 1,
                gameMode == RoboClashGameMode.Survival ? 24 : 7);
            UpdateModeOptions();
            UpdateOpponentCountLabel();
            UpdateGameModeCards();
            if (gameMode == RoboClashGameMode.Multiplayer)
            {
                UpdateMultiplayerSetup();
                SelectFirstSelectable(multiplayerSetupPanel);
                return;
            }
            if (RequiresAIDifficultySelection(gameMode))
            {
                OpenDifficultySelection();
                return;
            }
        }

        private static bool RequiresAIDifficultySelection(RoboClashGameMode mode)
        {
            return mode != RoboClashGameMode.FreeForAll &&
                   mode != RoboClashGameMode.Multiplayer;
        }

        private void SetMultiplayerSetupVisible(bool visible)
        {
            if (multiplayerSetupPanel == null)
                return;
            multiplayerSetupPanel.SetActive(visible);
            if (matchRulesPanel != null)
                matchRulesPanel.SetActive(!visible);
            if (modeObjectivePanel != null)
                modeObjectivePanel.SetActive(!visible);
            if (gameModeCardButtons != null)
            {
                foreach (var button in gameModeCardButtons)
                {
                    if (button != null)
                        button.gameObject.SetActive(!visible);
                }
            }
            if (normalSetupBackButton != null)
                normalSetupBackButton.gameObject.SetActive(!visible);
            if (chooseRobotButton != null)
                chooseRobotButton.gameObject.SetActive(!visible);
            if (multiplayerBackButton != null)
                multiplayerBackButton.gameObject.SetActive(visible);
            if (multiplayerStartButton != null)
                multiplayerStartButton.gameObject.SetActive(visible);
        }

        private void ExitMultiplayerSetup()
        {
            gameMode = RoboClashGameMode.TimedDeathmatch;
            SetMultiplayerSetupVisible(false);
            UpdateModeOptions();
            UpdateGameModeCards();
        }

        private void OpenDifficultySelection()
        {
            if (matchSetupScreen != null)
                matchSetupScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
            {
                aiDifficultySelectionScreen.SetActive(true);
                UpdateDifficultySelectionVisuals();
                SelectFirstSelectable(aiDifficultySelectionScreen);
            }
        }

        private void BackFromDifficultySelection()
        {
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            if (matchSetupScreen != null)
            {
                matchSetupScreen.SetActive(true);
                SelectFirstSelectable(matchSetupScreen);
            }
        }

        private void SelectAIDifficulty(RoboClashAIDifficulty difficulty)
        {
            aiDifficulty = difficulty;
            RoboClashSession.SaveAIDifficulty(aiDifficulty);
            if (aiDifficultyDropdown != null)
            {
                aiDifficultyDropdown.SetValueWithoutNotify((int)aiDifficulty);
                aiDifficultyDropdown.RefreshShownValue();
            }
            UpdateDifficultySelectionVisuals();
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            if (matchSetupScreen != null)
            {
                matchSetupScreen.SetActive(true);
                UpdateModeOptions();
                SelectFirstSelectable(matchSetupScreen);
            }
        }

        private void UpdateDifficultySelectionVisuals()
        {
            if (aiDifficultyButtons == null || aiDifficultyLabels == null)
                return;

            for (var i = 0; i < aiDifficultyButtons.Length; i++)
            {
                var button = aiDifficultyButtons[i];
                var label = aiDifficultyLabels[i];
                if (button == null)
                    continue;

                var image = button.GetComponent<Image>();
                var difficulty = (RoboClashAIDifficulty)i;
                var selected = difficulty == aiDifficulty;
                if (image != null)
                    image.color = selected ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f) : PanelColor;
                if (label != null)
                    label.color = selected ? OrangeColor : Color.white;
            }

            if (aiDifficultySelectionTitle != null)
                aiDifficultySelectionTitle.text = GetAIDifficultyLabel(aiDifficulty);
            if (aiDifficultySelectionDescription != null)
                aiDifficultySelectionDescription.text = RoboClashAIProfiles.Get(aiDifficulty).description;
        }

        private void UpdateGameModeCards()
        {
            if (gameModeCardBackgrounds == null)
                return;

            var selectedIndex = GetGameModeOption(gameMode);
            for (var i = 0; i < gameModeCardBackgrounds.Length; i++)
            {
                var cardLabel = gameModeCardLabels[i];
                if (cardLabel != null)
                    cardLabel.text = BuildModeCardText(i, GameModeKeys[i]);
                var isSelected = i == selectedIndex;
                gameModeCardBackgrounds[i].color = isSelected
                    ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f)
                    : PanelColor;
                var label = gameModeCardLabels[i];
                if (label != null)
                    label.color = isSelected ? OrangeColor : Color.white;
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
                $"AI DIFFICULTY  //  {GetAIDifficultyLabel(aiDifficulty)}\n\n" +
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
                opponentCountText.text = gameMode == RoboClashGameMode.Survival ||
                                         gameMode == RoboClashGameMode.Duel
                    ? opponentCount.ToString()
                    : (opponentCount + 1).ToString();
            if (opponentCountLabel != null)
                opponentCountLabel.text = gameMode == RoboClashGameMode.Survival
                    ? "STARTING ENEMIES"
                    : gameMode == RoboClashGameMode.Duel
                        ? "AI OPPONENTS (0-7)"
                        : "COMBATANTS (2-8)";
            if (gameMode != RoboClashGameMode.Multiplayer)
                RoboClashSession.SaveSelection(RobotTypes[selectedRobotIndex], opponentCount);
            UpdateCurrentSelectionLabel();
            RefreshObjectiveDisplays();
        }

        private void IncreaseOpponents()
        {
            var maximum = gameMode == RoboClashGameMode.Survival ? 24 : 7;
            opponentCount = Mathf.Min(maximum, opponentCount + 1);
            UpdateOpponentCountLabel();
        }

        private void DecreaseOpponents()
        {
            opponentCount = Mathf.Max(gameMode == RoboClashGameMode.Duel ? 0 : 1,
                opponentCount - 1);
            UpdateOpponentCountLabel();
        }

        private TMP_Dropdown CreateModeRuleDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.32f));
            dropdown.onValueChanged.AddListener(SetModeRule);
            return dropdown;
        }

        private TMP_Dropdown CreateRespawnDropdown(Transform parent)
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
            var countLimit = survival ? 24 : 7;
            opponentCount = Mathf.Clamp(opponentCount, duel ? 0 : 1, countLimit);
            decreaseOpponentButton.gameObject.SetActive(gameMode != RoboClashGameMode.Multiplayer);
            increaseOpponentButton.gameObject.SetActive(gameMode != RoboClashGameMode.Multiplayer);
            opponentCountLabel.gameObject.SetActive(true);
            opponentCountText.gameObject.SetActive(true);
            survivalGrowthDropdown.gameObject.SetActive(survival);
            waveBreakDropdown.gameObject.SetActive(survival);
            modeSettingsHint.gameObject.SetActive(true);
            modeSettingsHint.gameObject.SetActive(!duel);
            modeSettingsHint.text = survival
                ? "WAVES GROW BY +" + survivalGrowth + " ENEMIES"
                : "COMBATANTS: " + (opponentCount + 1);
            duelPlayerCountLabel.gameObject.SetActive(duel);
            foreach (var button in duelPlayerCountButtons)
                button.gameObject.SetActive(duel);
            if (duel)
                UpdateDuelPlayerCountSelection();
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
            RefreshObjectiveDisplays();
        }

        private void SetMultiplayerPlayerCount(int count)
        {
            multiplayerPlayerCount = Mathf.Clamp(count, 2, 4);
            PersistMultiplayerSetup();
            UpdateMultiplayerSetup();
        }

        private void SetDuelPlayerCount(int count)
        {
            duelPhonePlayerCount = count == 4 ? 4 : 2;
            RoboClashSession.SaveDuelPhonePlayerCount(duelPhonePlayerCount);
            UpdateDuelPlayerCountSelection();
            RefreshObjectiveDisplays();
        }

        private void OpenDuelLobby()
        {
            Array.Clear(duelLobbyReady, 0, duelLobbyReady.Length);
            mainScreen.SetActive(false);
            selectionScreen.SetActive(false);
            matchSetupScreen.SetActive(false);
            if (aiDifficultySelectionScreen != null)
                aiDifficultySelectionScreen.SetActive(false);
            matchSummaryScreen.SetActive(false);
            careerScreen.SetActive(false);
            settingsScreen.SetActive(false);
            duelLobbyScreen.SetActive(true);
            SetRobotPreviewVisible(false);
            UpdateDuelLobbyVisuals();
            SelectFirstSelectable(duelLobbyScreen);
        }

        private void BackFromDuelLobby()
        {
            Array.Clear(duelLobbyReady, 0, duelLobbyReady.Length);
            OpenMatchSummary();
        }

        private void CycleDuelRobot(int playerIndex, int direction)
        {
            if (playerIndex < 0 || playerIndex >= duelPhonePlayerCount ||
                duelRobots == null || playerIndex >= duelRobots.Length)
                return;

            var server = RoboClashPhoneInputServer.Active;
            if (server == null || !server.IsPhoneActiveForPlayer(playerIndex))
                return;

            var current = Array.IndexOf(RobotTypes, duelRobots[playerIndex]);
            if (current < 0)
                current = 0;
            duelRobots[playerIndex] = RobotTypes[(current + direction + RobotTypes.Length) % RobotTypes.Length];
            duelLobbyReady[playerIndex] = false;
            RoboClashSession.SaveDuelRobotSelection(duelRobots);
            UpdateDuelLobbyVisuals();
        }

        private void ToggleDuelPlayerReady(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= duelPhonePlayerCount)
                return;

            var server = RoboClashPhoneInputServer.Active;
            if (server == null || !server.IsPhoneActiveForPlayer(playerIndex))
            {
                ShowMatchStartError($"Player {playerIndex + 1}'s phone is not connected.");
                return;
            }

            duelLobbyReady[playerIndex] = !duelLobbyReady[playerIndex];
            UpdateDuelLobbyVisuals();
            if (AreAllDuelPlayersReady())
                StartSelectedMatch();
        }

        private void ProcessDuelLobbyPhoneInput()
        {
            var server = RoboClashPhoneInputServer.Active;
            if (server == null)
                return;

            for (var i = 0; i < duelPhonePlayerCount; i++)
            {
                if (!server.IsPhoneActiveForPlayer(i))
                    continue;

                if (server.ConsumePressed(i, RoboClashPhoneAction.Jump))
                    CycleDuelRobot(i, 1);
                else if (server.ConsumePressed(i, RoboClashPhoneAction.Reload))
                    CycleDuelRobot(i, -1);

                if (server.ConsumePressed(i, RoboClashPhoneAction.Ultimate))
                    ToggleDuelPlayerReady(i);
            }
        }

        private bool AreAllDuelPlayersReady()
        {
            var server = RoboClashPhoneInputServer.Active;
            if (server == null || !server.ArePlayersConnected(duelPhonePlayerCount))
                return false;
            for (var i = 0; i < duelPhonePlayerCount; i++)
            {
                if (!duelLobbyReady[i])
                    return false;
            }
            return true;
        }

        private void UpdateDuelLobbyVisuals()
        {
            if (duelLobbyScreen == null || duelLobbyPlayerLabels == null)
                return;

            var server = RoboClashPhoneInputServer.Active;
            for (var i = 0; i < duelLobbyPlayerLabels.Length; i++)
            {
                var active = i < duelPhonePlayerCount;
                var playerIndex = i;
                var rect = duelLobbyPanelBackgrounds[i].rectTransform;
                if (duelPhonePlayerCount == 2)
                {
                    var left = i == 0 ? 0.04f : 0.52f;
                    var right = i == 0 ? 0.48f : 0.96f;
                    Place(rect, new Vector2(left, 0.18f), new Vector2(right, 0.82f),
                        Vector2.zero, Vector2.zero);
                }
                else
                {
                    var column = i % 2;
                    var row = i / 2;
                    var left = column == 0 ? 0.04f : 0.52f;
                    var right = column == 0 ? 0.48f : 0.96f;
                    var bottom = row == 0 ? 0.19f : 0.51f;
                    var top = row == 0 ? 0.49f : 0.81f;
                    Place(rect, new Vector2(left, bottom), new Vector2(right, top),
                        Vector2.zero, Vector2.zero);
                }

                duelLobbyPanelBackgrounds[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                var connected = server != null && server.IsPhoneActiveForPlayer(i);
                if (!connected)
                    duelLobbyReady[i] = false;
                var type = duelRobots[i];
                var typeIndex = Array.IndexOf(RobotTypes, type);
                typeIndex = Mathf.Clamp(typeIndex, 0, roles.Length - 1);
                duelLobbyPlayerLabels[i].text =
                    $"PLAYER {i + 1}  //  {(connected ? "CONNECTED" : "WAITING FOR PHONE")}";
                duelLobbyPlayerLabels[i].color = connected ? BlueColor : MutedColor;
                duelLobbyRobotLabels[i].text = type.ToString().ToUpperInvariant();
                duelLobbyRobotLabels[i].color = GetAccent(type, GetDefinition(type));
                duelLobbyRoleLabels[i].text = roles[typeIndex];
                duelLobbyPreviousButtons[i].interactable = connected && !duelLobbyReady[i];
                duelLobbyNextButtons[i].interactable = connected && !duelLobbyReady[i];
                duelLobbyReadyButtons[i].interactable = connected;
                duelLobbyReadyButtons[i].GetComponentInChildren<TextMeshProUGUI>().text =
                    duelLobbyReady[i] ? "READY - WAITING" : "START";
                duelLobbyReadyButtons[i].GetComponent<Image>().color = duelLobbyReady[i]
                    ? new Color(0.12f, 0.42f, 0.24f, 0.96f)
                    : OrangeColor;
                duelLobbyPanelBackgrounds[i].color = duelLobbyReady[i]
                    ? new Color(0.08f, 0.21f, 0.19f, 0.96f)
                    : PanelColor;
            }
        }

        private void UpdateDuelPlayerCountSelection()
        {
            if (duelPlayerCountButtons == null)
                return;
            for (var i = 0; i < duelPlayerCountButtons.Length; i++)
            {
                var selected = (i == 0 ? 2 : 4) == duelPhonePlayerCount;
                duelPlayerCountBackgrounds[i].color =
                    selected ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f) : PanelColor;
            }
        }

        private void SetMultiplayerKillTarget(int option)
        {
            killTargetOption = Mathf.Clamp(option, 0, 3);
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
            UpdateMultiplayerSetup();
            UpdateGameModeCards();
        }

        private void CycleMultiplayerRobot(int playerIndex, int direction)
        {
            if (multiplayerRobots == null || playerIndex < 0 ||
                playerIndex >= multiplayerPlayerCount || playerIndex >= multiplayerRobots.Length)
                return;

            var current = Array.IndexOf(RobotTypes, multiplayerRobots[playerIndex]);
            if (current < 0)
                current = 0;
            current = (current + direction + RobotTypes.Length) % RobotTypes.Length;
            multiplayerRobots[playerIndex] = RobotTypes[current];
            PersistMultiplayerSetup();
            UpdateMultiplayerSetup();
        }

        private void PersistMultiplayerSetup()
        {
            RoboClashSession.SaveMultiplayerSetup(multiplayerPlayerCount, multiplayerRobots);
        }

        private void UpdateMultiplayerSetup()
        {
            if (multiplayerPlayerCountButtons != null)
            {
                for (var i = 0; i < multiplayerPlayerCountButtons.Length; i++)
                {
                    var selected = i + 2 == multiplayerPlayerCount;
                    multiplayerPlayerCountBackgrounds[i].color =
                        selected ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f) : PanelColor;
                }
            }
            if (multiplayerKillTargetButtons != null)
            {
                for (var i = 0; i < multiplayerKillTargetButtons.Length; i++)
                {
                    var selected = i == killTargetOption;
                    multiplayerKillTargetBackgrounds[i].color =
                        selected ? new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.35f) : PanelColor;
                }
            }
            if (multiplayerRobotLabels != null)
            {
                for (var i = 0; i < multiplayerRobotLabels.Length; i++)
                {
                    var active = i < multiplayerPlayerCount;
                    multiplayerPlayerLabels[i].gameObject.SetActive(active);
                    multiplayerRobotLabels[i].gameObject.SetActive(active);
                    multiplayerRobotPreviousButtons[i].gameObject.SetActive(active);
                    multiplayerRobotNextButtons[i].gameObject.SetActive(active);
                    if (active)
                        multiplayerRobotLabels[i].text = multiplayerRobots[i].ToString().ToUpperInvariant();
                }
            }

            var target = new[] { 5, 10, 15, 20 }[Mathf.Clamp(killTargetOption, 0, 3)];
            if (multiplayerObjectiveText != null)
                multiplayerObjectiveText.text =
                    $"Free-for-all on one screen. First player to {target} eliminations wins. Respawns are unlimited.";
            if (multiplayerControllerText != null)
                multiplayerControllerText.text =
                    $"CONTROLLERS DETECTED: {UnityEngine.InputSystem.Gamepad.all.Count}";
            if (multiplayerErrorText != null)
                multiplayerErrorText.text = string.Empty;
        }

        private void StartMultiplayerMatch()
        {
            if (startingMatch)
                return;

            if (!RoboClashInputAssignment.CanAssign(multiplayerPlayerCount, out var assignmentMessage))
            {
                ShowMatchStartError(string.IsNullOrEmpty(assignmentMessage)
                    ? "The selected player count cannot be assigned to available controllers."
                    : assignmentMessage);
                return;
            }

            for (var i = 0; i < multiplayerPlayerCount; i++)
            {
                var definition = GetDefinition(multiplayerRobots[i]);
                if (definition == null || definition.stats == null)
                {
                    ShowMatchStartError($"Player {i + 1}'s selected robot prefab is unavailable.");
                    return;
                }
            }

            if (!Application.CanStreamedLevelBeLoaded(ArenaSceneName))
            {
                ShowMatchStartError(
                    $"Gameplay scene '{ArenaSceneName}' is not available in Build Settings. " +
                    $"Add '{RoboClashSceneUtility.XLGameplayScenePath}' to Build Settings and try again.");
                return;
            }

            startingMatch = true;
            if (multiplayerStartButton != null)
                multiplayerStartButton.interactable = false;
            try
            {
                RoboClashSession.BeginMultiplayerMatch(multiplayerPlayerCount, multiplayerRobots,
                    killTargetOption);
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

        private string BuildModeCardText(int option, string key)
        {
            var mode = GetGameModeFromOption(option);
            var settingsForMode = GetObjectiveSettings(mode);
            return key + "\n" + RoboClashObjectives.Title(mode) + "\n" +
                   RoboClashObjectives.Describe(settingsForMode, GetObjectiveTimeLimit(mode));
        }

        private RoboClashMatchSettings GetObjectiveSettings(RoboClashGameMode mode)
        {
            return new RoboClashMatchSettings
            {
                gameMode = mode,
                aiOpponents = opponentCount,
                localPlayerCount = mode == RoboClashGameMode.Duel
                    ? duelPhonePlayerCount
                    : multiplayerPlayerCount,
                killTarget = GetKillTarget(),
                knockoutLives = knockoutLives,
                duelUsesKillTarget = duelUsesKillTarget,
                duelRespawnsEnabled = respawnsEnabled,
                respawnEnabled = respawnsEnabled
            };
        }

        private float GetObjectiveTimeLimit(RoboClashGameMode mode)
        {
            return mode == RoboClashGameMode.Knockout
                ? RoboClashMatchManager.KnockoutTimeLimitSeconds
                : GetMatchDuration() * 60f;
        }

        private void UpdateObjectiveText()
        {
            if (modeObjectiveText == null)
                return;

            var objectiveSettings = GetObjectiveSettings(gameMode);
            objectiveSettings.killTarget = GetKillTarget();
            objectiveSettings.knockoutLives = knockoutLives;
            objectiveSettings.startingSurvivalEnemies = opponentCount;
            objectiveSettings.additionalEnemiesPerWave = survivalGrowth;
            objectiveSettings.waveBreakSeconds = waveBreakSeconds;
            modeObjectiveText.text = "<b>OBJECTIVE:</b> " +
                                     RoboClashObjectives.Describe(objectiveSettings,
                                         GetObjectiveTimeLimit(gameMode)) +
                                     (gameMode == RoboClashGameMode.Duel
                                         ? " Connect phones in order; first phone is Player 1."
                                         : string.Empty);
        }

        private void RefreshObjectiveDisplays()
        {
            UpdateObjectiveText();
            UpdateGameModeCards();
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
                case RoboClashGameMode.Multiplayer: return "LOCAL MULTIPLAYER";
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
                case 6: return RoboClashGameMode.Multiplayer;
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
                case RoboClashGameMode.Multiplayer: return 6;
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
            RefreshObjectiveDisplays();
        }

        private void SetRespawns(int option)
        {
            respawnsEnabled = option == 0;
            RoboClashSession.SaveModeRules(killTargetOption, knockoutLives, respawnsEnabled,
                duelUsesKillTarget);
            RefreshObjectiveDisplays();
        }

        private void StartSelectedMatch()
        {
            if (startingMatch)
                return;

            if (!Enum.IsDefined(typeof(RoboClashGameMode), gameMode) ||
                gameMode == RoboClashGameMode.FreeForAll)
            {
                ShowMatchStartError("Select one of the supported match modes before starting.");
                return;
            }

            if (gameMode == RoboClashGameMode.Duel &&
                (RoboClashPhoneInputServer.Active == null ||
                 !RoboClashPhoneInputServer.Active.ArePlayersConnected(duelPhonePlayerCount)))
            {
                var connected = RoboClashPhoneInputServer.Active != null
                    ? RoboClashPhoneInputServer.Active.ConnectedPhoneCount
                    : 0;
                ShowMatchStartError(
                    $"Connect all {duelPhonePlayerCount} phones first ({connected} currently connected).");
                return;
            }

            if (gameMode == RoboClashGameMode.Duel)
            {
                if (!duelLobbyScreen.activeSelf)
                {
                    OpenDuelLobby();
                    return;
                }

                if (!AreAllDuelPlayersReady())
                    return;

                for (var i = 0; i < duelPhonePlayerCount; i++)
                {
                    if (GetDefinition(duelRobots[i])?.stats == null)
                    {
                        ShowMatchStartError(
                            $"Player {i + 1}'s selected robot prefab is missing its robot definition.");
                        return;
                    }
                }
            }
            else if (selectedRobotIndex < 0 || selectedRobotIndex >= RobotTypes.Length ||
                     GetDefinition(RobotTypes[selectedRobotIndex])?.stats == null)
            {
                ShowMatchStartError("The selected robot prefab is missing its robot definition.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(ArenaSceneName))
            {
                ShowMatchStartError(
                    $"Gameplay scene '{ArenaSceneName}' is not available in Build Settings. " +
                    $"Add '{RoboClashSceneUtility.XLGameplayScenePath}' to Build Settings and try again.");
                return;
            }

            if (matchStartErrorText != null)
                matchStartErrorText.text = string.Empty;
            startingMatch = true;
            if (startMatchButton != null)
                startMatchButton.interactable = false;

            try
            {
                var matchRobot = gameMode == RoboClashGameMode.Duel
                    ? duelRobots[0]
                    : RobotTypes[selectedRobotIndex];
                if (gameMode == RoboClashGameMode.Duel)
                {
                    RoboClashSession.SaveDuelRobotSelection(duelRobots);
                }
                RoboClashSession.BeginMatch(matchRobot, opponentCount, aiDifficulty,
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
            if (multiplayerStartButton != null)
                multiplayerStartButton.interactable = true;
            ShowMatchStartError("Could not load the match: " + exception.Message);
            Debug.LogException(exception, this);
        }

        private void ShowMatchStartError(string message)
        {
            if (matchStartErrorText != null)
                matchStartErrorText.text = message;
            if (multiplayerErrorText != null && multiplayerSetupPanel != null &&
                multiplayerSetupPanel.activeSelf)
                multiplayerErrorText.text = message;
            Debug.LogError(message, this);
        }

        private TMP_Dropdown CreateSurvivalGrowthDropdown(Transform parent)
        {
            var dropdown = CreateDropdown(parent, new Vector2(0.02f, 0.03f), new Vector2(0.48f, 0.18f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "GROWTH +1", "GROWTH +2", "GROWTH +3" });
            dropdown.SetValueWithoutNotify(Mathf.Clamp(survivalGrowth - 1, 0, 2));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(SetSurvivalGrowth);
            return dropdown;
        }

        private TMP_Dropdown CreateWaveBreakDropdown(Transform parent)
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
            RefreshObjectiveDisplays();
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
            RefreshObjectiveDisplays();
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
            if (aiDifficultySelectionScreen != null && aiDifficultySelectionScreen.activeSelf)
                UpdateDifficultySelectionVisuals();
            if (modeSettingsHint != null)
                UpdateModeOptions();
        }

        private static string GetAIDifficultyLabel(RoboClashAIDifficulty difficulty)
        {
            return RoboClashAIProfiles.Get(difficulty).label;
        }

        private static string GetAIDifficultyDescription(RoboClashAIDifficulty difficulty)
        {
            return RoboClashAIProfiles.Get(difficulty).description;
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
                return BlueColor;
            return definition.primaryColor;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Color color, Action action, bool primary = false)
        {
            var idleColor = primary ? OrangeColor : PanelColor;
            var hotColor = primary ? OrangeHotColor : GlassHotColor;
            var buttonObject = CreatePanel(label + " Button", parent, idleColor);
            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;
            RoboClashUiKit.StyleRoundedPanel(image);
            var button = buttonObject.AddComponent<Button>();
            RoboClashRobotAudio.RoboClashAudioManager.AddButtonAudio(button);
            button.targetGraphic = image;
            button.navigation = Navigation.defaultNavigation;
            button.transition = Selectable.Transition.None;
            Place(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var bar = CreatePanel("Accent Bar", buttonObject.transform, primary ? Color.white : BlueColor);
            Place(bar.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0f, 1f),
                new Vector2(2f, 0f), new Vector2(6f, 0f));
            var labelColor = primary ? new Color(0.06f, 0.04f, 0.02f) : Color.white;
            var buttonText = AddText(buttonObject.transform, label, 17, labelColor, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(28f, 0f), Vector2.zero, true);
            var chevron = AddText(buttonObject.transform, ">", 22,
                primary ? labelColor : OrangeColor, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18f, 0f));
            var fx = buttonObject.AddComponent<RoboClashMenuButtonFx>();
            fx.Initialize(image, bar.GetComponent<Image>(), buttonText.rectTransform,
                chevron, idleColor, hotColor, primary);
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private TMP_Dropdown CreateDropdown(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = CreatePanel("Resolution Dropdown", parent, GlassHotColor);
            root.GetComponent<Image>().raycastTarget = true;
            Place(root.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.transition = Selectable.Transition.None;
            var caption = AddText(root.transform, string.Empty, 15, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.05f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);
            AddText(root.transform, "v", 14, BlueColor, TextAnchor.MiddleCenter,
                new Vector2(0.85f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            var template = CreatePanel("Template", root.transform, GlassHotColor);
            template.GetComponent<Image>().raycastTarget = true;
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, -2f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.offsetMin = Vector2.zero;
            templateRect.offsetMax = Vector2.zero;
            template.AddComponent<CanvasGroup>();
            var viewport = CreatePanel("Viewport", template.transform, GlassHotColor);
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
            var item = CreatePanel("Item", content.transform, GlassHotColor);
            item.GetComponent<Image>().raycastTarget = true;
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

        private TextMeshProUGUI AddText(Transform parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool important = false)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            Place(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.color = color;
            text.alignment = RoboClashUiKit.ToTextAlignmentOptions(alignment);
            text.raycastTarget = false;
            RoboClashUiKit.StyleText(text, size, important);
            return text;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (name != "Readability Dim" && color.a > 0f)
                RoboClashUiKit.StyleRoundedPanel(image);
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
