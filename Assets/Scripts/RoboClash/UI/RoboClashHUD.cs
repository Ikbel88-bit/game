using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(32000)]
    public sealed class RoboClashHUD : MonoBehaviour
    {
        private const int KillFeedSize = 4;

        private static readonly Color Cyan = new Color(0.08f, 0.8f, 1f);
        private static readonly Color Green = new Color(0.18f, 0.95f, 0.58f);
        private static readonly Color Red = new Color(1f, 0.18f, 0.16f);
        private static readonly Color Amber = new Color(1f, 0.66f, 0.12f);
        private static readonly Color MutedColor = new Color(0.58f, 0.69f, 0.78f);
        private static readonly Color PanelColor = new Color(0.015f, 0.045f, 0.075f, 0.93f);

        private RoboClashMatchManager matchManager;
        private RobotArenaController playerController;
        private RoboClashRobotDefinition playerDefinition;
        private ArenaDamageable playerDamageable;
        private RoboClashRobotCombat playerCombat;
        private RoboClashRobotAbilities playerAbilities;

        private Canvas canvas;
        private TMP_FontAsset font;
        private GameObject gameplayRoot;
        private GameObject matchPanelRoot;
        private GameObject killFeedRoot;
        private GameObject pauseRoot;
        private GameObject resultsRoot;
        private GameObject crosshairRoot;
        private TextMeshProUGUI weaponNameText;
        private TextMeshProUGUI ammoText;
        private TextMeshProUGUI reloadText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI stateText;
        private TextMeshProUGUI combatantsText;
        private TextMeshProUGUI phoneInputStatusText;
        private TextMeshProUGUI scoreboardText;
        private TextMeshProUGUI countdownText;
        private TextMeshProUGUI countdownTitleText;
        private TextMeshProUGUI countdownObjectiveText;
        private CanvasGroup countdownBannerGroup;
        private TextMeshProUGUI notificationText;
        private TextMeshProUGUI eliminatedText;
        private TextMeshProUGUI crosshairText;
        private RectTransform crosshairRect;
        private TextMeshProUGUI resultTitleText;
        private TextMeshProUGUI resultDetailsText;
        private TextMeshProUGUI playerKillsText;
        private TextMeshProUGUI[] killFeedTexts;
        private Image reloadFill;
        private Image lowHealthTint;
        private Image damageFlash;
        private float damageFlashAlpha;
        private RoboClashHudV2 redesignedHud;
        private RoboClashSettingsPanel settingsPanel;
        private string[] killFeedEntries;
        private Color[] killFeedColors;
        private float[] killFeedExpiry;
        private float notificationExpiry;
        private float nextStatusRefresh;
        private float nextPhoneStatusRefresh;
        private float nextCombatantRefresh;
        private bool fadingCountdownBanner;
        private int lastDisplayedSecond = -1;
        private int lastDisplayedWave;
        private bool lastDisplayedWaveBreak;
        private int displayedMatchState = -1;
        private bool lowAmmo;
        private bool lowHealth;
        private bool wasReloading;
        private bool wasOutOfAmmo;
        private bool wasAbilityReady;
        private bool wasUltimateReady;
        private bool resultsShown;
        private bool viewportOnly;
        private bool globalOverlayOnly;
        private bool explicitPlayerBinding;
        private bool buildingMultiplayerViewport;
        private float nextPlayerKillsRefresh;

        private void Awake()
        {
            if (!enabled)
                return;

            if (!RoboClashSceneUtility.IsGameplayScene(gameObject.scene))
            {
                Debug.LogError(
                    "RoboClashHUD must only initialize in a supported gameplay scene, but was found in " +
                    $"scene '{gameObject.scene.name}' at '{gameObject.scene.path}'. " +
                    "Its gameplay Canvas and EventSystem were not created.",
                    this);
                enabled = false;
                return;
            }

            matchManager = GetComponentInParent<RoboClashMatchManager>();
            buildingMultiplayerViewport = matchManager != null &&
                                          (matchManager.GameMode == RoboClashGameMode.Multiplayer ||
                                           matchManager.GameMode == RoboClashGameMode.Duel);
            BuildInterface();
        }

        private void RefreshCrosshairVisibility()
        {
            if (crosshairRoot == null)
                return;
            if (globalOverlayOnly)
            {
                crosshairRoot.SetActive(false);
                return;
            }

            var isPlaying = matchManager != null &&
                            matchManager.State == RoboClashMatchState.Playing;
            var isAlive = playerController != null && !playerController.IsDead;
            var movement = playerController != null
                ? playerController.GetComponent<RoboClashRobotMovement>()
                : null;
            if (crosshairRect != null)
            {
                var aimPoint = movement != null
                    ? movement.AimViewportPoint
                    : new Vector2(0.5f, 0.5f);
                crosshairRect.anchorMin = aimPoint;
                crosshairRect.anchorMax = aimPoint;
                crosshairRect.anchoredPosition = Vector2.zero;
            }
            var hasGameplayCamera = movement == null
                ? playerController != null && playerController.PlayerCamera != null
                : movement.CameraState == RoboClashCameraState.FirstPerson ||
                  movement.CameraState == RoboClashCameraState.ThirdPerson;
            crosshairRoot.SetActive(isPlaying && isAlive && hasGameplayCamera);

            if (crosshairRect != null)
            {
                var firingScale = playerCombat != null && playerCombat.IsFiring ? 1.2f : 1f;
                crosshairRect.localScale = Vector3.one * firingScale;
            }
        }

        private void RefreshPhoneInputStatus()
        {
            if (phoneInputStatusText == null)
                return;

            var phoneActive = RoboClashPhoneInputServer.IsPhoneActive;
            phoneInputStatusText.text = RoboClashPhoneInputServer.StatusText;
            if (phoneActive && RoboClashPhoneInputServer.Active != null)
            {
                var input = RoboClashPhoneInputServer.Active.CurrentInput;
                phoneInputStatusText.text += input.CompassValid
                    ? $"\nPhone compass: {input.CompassHeading:0}° (valid)"
                    : "\nPhone compass: n/a";
            }
            phoneInputStatusText.color = phoneActive
                ? Green
                : MutedColor;
        }

        private void OnEnable()
        {
            SubscribeToMatch();
            BindPlayer(matchManager != null ? matchManager.PlayerController : null);
        }

        private void Start()
        {
            if (!explicitPlayerBinding)
                BindPlayer(matchManager != null ? matchManager.PlayerController : null);
            if (matchManager != null)
            {
                RefreshTimer(matchManager.RemainingTime);
                RefreshMatchState(matchManager.State);
                RefreshCombatantCount();
                RefreshModeStatus();
                if (matchManager.State == RoboClashMatchState.Victory ||
                    matchManager.State == RoboClashMatchState.Defeat ||
                    matchManager.State == RoboClashMatchState.Draw)
                    ShowResults(matchManager.Statistics);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromMatch();
            BindPlayer(null);
        }

        private void Update()
        {
            UpdateCountdownBanner(Time.unscaledDeltaTime);

            if (matchManager == null)
                return;

            if (viewportOnly && playerKillsText != null &&
                Time.unscaledTime >= nextPlayerKillsRefresh)
            {
                nextPlayerKillsRefresh = Time.unscaledTime + 0.2f;
                var inputRouter = playerController != null
                    ? playerController.GetComponent<RoboClashPlayerInputRouter>()
                    : null;
                var playerIndex = inputRouter != null ? inputRouter.PlayerIndex : -1;
                var kills = matchManager.GetPlayerKills(playerIndex);
                playerKillsText.text = matchManager.GameMode == RoboClashGameMode.Duel &&
                                       !matchManager.DuelUsesKillTarget
                    ? $"YOU {kills} KILLS"
                    : $"YOU {kills} / {matchManager.KillTarget}";
            }

            if (matchManager.State == RoboClashMatchState.Playing && playerAbilities != null)
            {
                var movement = playerAbilities.GetComponent<RoboClashRobotMovement>();
                var keyboardInput = movement != null
                    ? movement.InputSource as KeyboardMouseInputSource
                    : null;
                if (keyboardInput != null)
                {
                    if (keyboardInput.AbilityPressedThisFrame && !playerAbilities.IsAbilityReady)
                        ShowNotification("ABILITY UNAVAILABLE");
                    if (keyboardInput.UltimatePressedThisFrame && !playerAbilities.IsUltimateReady)
                        ShowNotification("ULTIMATE UNAVAILABLE");
                }
            }

            var now = Time.unscaledTime;
            if (now >= nextStatusRefresh)
            {
                nextStatusRefresh = now + 0.12f;
                RefreshAbilityStatus();
                RefreshAmmo();
                RefreshCrosshairVisibility();
                if (notificationText != null)
                    notificationText.gameObject.SetActive(now < notificationExpiry);
                if (killFeedTexts != null)
                {
                    for (var i = 0; i < killFeedTexts.Length; i++)
                        killFeedTexts[i].gameObject.SetActive(now < killFeedExpiry[i]);
                }
            }

            if (now >= nextPhoneStatusRefresh)
            {
                nextPhoneStatusRefresh = now + 0.5f;
                RefreshPhoneInputStatus();
            }

            UpdateDamageFlash(Time.unscaledDeltaTime);

            if (now >= nextCombatantRefresh)
            {
                nextCombatantRefresh = now + 0.25f;
                RefreshCombatantCount();
                RefreshModeStatus();
            }
        }

        private void BuildInterface()
        {
            font = TMP_Settings.defaultFontAsset;
            var canvasObject = new GameObject("RoboClash Gameplay Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (!buildingMultiplayerViewport)
                EnsureEventSystem();

            gameplayRoot = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayRoot.transform.SetParent(canvas.transform, false);
            Stretch(gameplayRoot.GetComponent<RectTransform>());
            BuildPlayerFeedback(buildingMultiplayerViewport);
            BuildWeaponPanel(buildingMultiplayerViewport);
            if (!buildingMultiplayerViewport)
            {
                BuildMatchPanel();
                BuildKillFeed();
            }
            BuildCrosshair();
            if (!buildingMultiplayerViewport)
            {
                BuildNotifications();
                BuildPausePanel();
                BuildPauseSettings();
                BuildResultsPanel();
            }
            redesignedHud = GetComponent<RoboClashHudV2>();
            if (redesignedHud == null)
                redesignedHud = gameObject.AddComponent<RoboClashHudV2>();
            if (buildingMultiplayerViewport)
                gameplayRoot.SetActive(false);
            else
                ShowMatchState(RoboClashMatchState.Initializing);
        }

        private void BuildPlayerFeedback(bool compact)
        {
            if (compact)
            {
                eliminatedText = CreateText(gameplayRoot.transform, string.Empty, 27, Red,
                    new Vector2(0.25f, 0.42f), new Vector2(0.75f, 0.52f),
                    TextAnchor.MiddleCenter, true);
                eliminatedText.gameObject.SetActive(false);
                return;
            }

            lowHealthTint = CreatePanel(gameplayRoot.transform, "Low Health Warning",
                new Vector2(0.36f, 0.78f), new Vector2(0.64f, 0.84f),
                new Color(0.42f, 0.015f, 0.025f, 0.86f)).GetComponent<Image>();
            lowHealthTint.raycastTarget = false;
            CreateText(lowHealthTint.transform, "LOW HEALTH", 18, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            lowHealthTint.gameObject.SetActive(false);
            damageFlash = CreatePanel(gameplayRoot.transform, "Player Damage Flash",
                Vector2.zero, Vector2.one, new Color(1f, 0.035f, 0.02f, 0f)).GetComponent<Image>();
            damageFlash.raycastTarget = false;
            eliminatedText = CreateText(gameplayRoot.transform, string.Empty, 27, Red,
                new Vector2(0.32f, 0.42f), new Vector2(0.68f, 0.52f),
                TextAnchor.MiddleCenter, true);
            eliminatedText.gameObject.SetActive(false);
        }

        private void BuildWeaponPanel(bool compact)
        {
            var panel = CreatePanel(gameplayRoot.transform, "Weapon Status",
                new Vector2(0.755f, 0.035f), new Vector2(0.975f, 0.235f),
                new Color(0f, 0f, 0f, 0.45f));
            RoboClashUiKit.StyleRoundedPanel(panel.GetComponent<Image>());
            var top = compact ? 0.9f : 0.78f;
            if (!compact)
                weaponNameText = CreateText(panel.transform, string.Empty, 13, Cyan,
                    new Vector2(0.08f, top), new Vector2(0.92f, 0.96f),
                    TextAnchor.MiddleLeft);
            ammoText = CreateText(panel.transform, "-- / --", 26, Color.white,
                new Vector2(0.08f, compact ? 0.5f : 0.43f),
                new Vector2(0.92f, top), TextAnchor.MiddleLeft, true);
            reloadText = CreateText(panel.transform, string.Empty, 14, Amber,
                new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.45f),
                TextAnchor.MiddleLeft);
            reloadFill = CreateBar(panel.transform, "Reload Progress",
                new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.17f), Amber);
        }

        private void BuildMatchPanel()
        {
            var panel = CreatePanel(gameplayRoot.transform, "Match Status",
                new Vector2(0.37f, 0.91f), new Vector2(0.63f, 0.985f),
                new Color(0f, 0f, 0f, 0.45f));
            matchPanelRoot = panel;
            RoboClashUiKit.StyleRoundedPanel(panel.GetComponent<Image>());
            timerText = CreateText(panel.transform, "00:00", 25, Color.white,
                new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.98f),
                TextAnchor.MiddleCenter, true);
            stateText = CreateText(panel.transform, "INITIALIZING", 13, Cyan,
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.45f), TextAnchor.MiddleCenter);
            combatantsText = CreateText(gameplayRoot.transform, "COMBATANTS: --",
                13, Color.white, new Vector2(0.37f, 0.875f), new Vector2(0.63f, 0.91f),
                TextAnchor.MiddleCenter);
            phoneInputStatusText = CreateText(gameplayRoot.transform, "PC CONTROLS",
                11, MutedColor, new Vector2(0.02f, 0.91f), new Vector2(0.4f, 0.985f),
                TextAnchor.MiddleLeft);
            scoreboardText = CreateText(gameplayRoot.transform, string.Empty, 12, Color.white,
                new Vector2(0.74f, 0.53f), new Vector2(0.975f, 0.69f), TextAnchor.UpperRight);
            var banner = CreatePanel(gameplayRoot.transform, "Countdown Objective Banner",
                new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.70f),
                new Color(0.005f, 0.025f, 0.045f, 0.94f));
            RoboClashUiKit.StyleRoundedPanel(banner.GetComponent<Image>());
            countdownBannerGroup = banner.AddComponent<CanvasGroup>();
            countdownTitleText = CreateText(banner.transform, string.Empty, 31, Cyan,
                new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.96f),
                TextAnchor.MiddleCenter, true);
            countdownObjectiveText = CreateText(banner.transform, string.Empty, 17, Color.white,
                new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.72f),
                TextAnchor.MiddleCenter);
            countdownText = CreateText(banner.transform, string.Empty, 52, Color.white,
                new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.38f),
                TextAnchor.MiddleCenter, true);
            banner.SetActive(false);
        }

        private void BuildKillFeed()
        {
            var background = CreatePanel(gameplayRoot.transform, "Kill Feed Background",
                new Vector2(0.015f, 0.60f), new Vector2(0.315f, 0.875f),
                new Color(0f, 0f, 0f, 0.45f));
            killFeedRoot = background;
            RoboClashUiKit.StyleRoundedPanel(background.GetComponent<Image>());
            killFeedTexts = new TextMeshProUGUI[KillFeedSize];
            killFeedEntries = new string[KillFeedSize];
            killFeedColors = new Color[KillFeedSize];
            killFeedExpiry = new float[KillFeedSize];
            for (var i = 0; i < killFeedTexts.Length; i++)
            {
                var top = 0.87f - i * 0.045f;
                killFeedTexts[i] = CreateText(gameplayRoot.transform, string.Empty, 13, Color.white,
                    new Vector2(0.025f, top - 0.04f), new Vector2(0.30f, top),
                    TextAnchor.MiddleLeft);
                killFeedTexts[i].gameObject.SetActive(false);
            }
        }

        private void BuildCrosshair()
        {
            crosshairRoot = new GameObject("Crosshair", typeof(RectTransform));
            crosshairRoot.transform.SetParent(gameplayRoot.transform, false);
            var rect = crosshairRoot.GetComponent<RectTransform>();
            crosshairRect = rect;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 40f);
            rect.anchoredPosition = Vector2.zero;
            crosshairText = CreateText(crosshairRoot.transform, "+", 30, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true);
        }

        private void BuildNotifications()
        {
            notificationText = CreateText(gameplayRoot.transform, string.Empty, 18, Amber,
                new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.88f), TextAnchor.MiddleCenter);
            notificationText.gameObject.SetActive(false);
        }

        private void BuildPausePanel()
        {
            pauseRoot = CreateOverlay("Pause Menu");
            CreateText(pauseRoot.transform, "PAUSED", 34, Color.white,
                new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.91f),
                TextAnchor.MiddleCenter).fontStyle = FontStyles.Bold;
            CreateMenuButton(pauseRoot.transform, "RESUME", new Vector2(0.32f, 0.63f),
                new Vector2(0.68f, 0.72f), ResumeMatch);
            CreateMenuButton(pauseRoot.transform, "RESTART MATCH", new Vector2(0.32f, 0.51f),
                new Vector2(0.68f, 0.60f), RestartMatch);
            CreateMenuButton(pauseRoot.transform, "SETTINGS", new Vector2(0.32f, 0.39f),
                new Vector2(0.68f, 0.48f), OpenPauseSettings);
            CreateMenuButton(pauseRoot.transform, "MAIN MENU", new Vector2(0.32f, 0.27f),
                new Vector2(0.68f, 0.36f), ReturnToMainMenu);
            pauseRoot.SetActive(false);
        }

        private void BuildPauseSettings()
        {
            settingsPanel = gameObject.AddComponent<RoboClashSettingsPanel>();
            settingsPanel.Initialize(canvas.transform, font, ClosePauseSettings);
            settingsPanel.Root.SetActive(false);
        }

        private void BuildResultsPanel()
        {
            resultsRoot = CreateOverlay("Match Results");
            resultTitleText = CreateText(resultsRoot.transform, "MATCH COMPLETE", 40, Color.white,
                new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.9f),
                TextAnchor.MiddleCenter, true);
            resultTitleText.fontStyle = FontStyles.Bold;
            resultDetailsText = CreateText(resultsRoot.transform, string.Empty, 16, Color.white,
                new Vector2(0.16f, 0.41f), new Vector2(0.84f, 0.73f), TextAnchor.MiddleCenter);
            CreateMenuButton(resultsRoot.transform, "PLAY AGAIN", new Vector2(0.26f, 0.32f),
                new Vector2(0.48f, 0.42f), RestartMatch);
            CreateMenuButton(resultsRoot.transform, "MAIN MENU", new Vector2(0.52f, 0.32f),
                new Vector2(0.74f, 0.42f), ReturnToMainMenu);
            resultsRoot.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            RoboClashUIEventSystem.EnsureForScene(gameObject, "RoboClash Gameplay Event System");
        }

        private void SubscribeToMatch()
        {
            if (matchManager == null)
                return;

            matchManager.PlayerRegistered += OnPlayerRegistered;
            matchManager.MatchStateChanged += RefreshMatchState;
            matchManager.MatchTimerChanged += RefreshTimer;
            matchManager.CountdownChanged += RefreshCountdown;
            matchManager.StatisticsChanged += RefreshStatistics;
            matchManager.MatchEnded += ShowResults;
            matchManager.ParticipantEliminated += OnParticipantEliminated;
            matchManager.ModeStatusChanged += RefreshModeStatus;
        }

        private void UnsubscribeFromMatch()
        {
            if (matchManager == null)
                return;

            matchManager.PlayerRegistered -= OnPlayerRegistered;
            matchManager.MatchStateChanged -= RefreshMatchState;
            matchManager.MatchTimerChanged -= RefreshTimer;
            matchManager.CountdownChanged -= RefreshCountdown;
            matchManager.StatisticsChanged -= RefreshStatistics;
            matchManager.MatchEnded -= ShowResults;
            matchManager.ParticipantEliminated -= OnParticipantEliminated;
            matchManager.ModeStatusChanged -= RefreshModeStatus;
        }

        private void OnPlayerRegistered(RobotArenaController controller)
        {
            if (!explicitPlayerBinding)
                BindPlayer(controller);
        }

        private void BindPlayer(RobotArenaController controller)
        {
            UnbindPlayer();
            lowHealth = false;
            lowAmmo = false;
            wasReloading = false;
            wasOutOfAmmo = false;
            wasAbilityReady = false;
            wasUltimateReady = false;
            damageFlashAlpha = 0f;
            if (damageFlash != null)
                damageFlash.color = new Color(1f, 0.035f, 0.02f, 0f);
            if (eliminatedText != null)
                eliminatedText.gameObject.SetActive(false);
            playerController = controller;
            if (!globalOverlayOnly && redesignedHud != null)
                redesignedHud.BindPlayer(controller, matchManager != null ? matchManager.transform : null);
            if (playerController == null)
            {
                RefreshCrosshairVisibility();
                HideObject(lowHealthTint != null ? lowHealthTint.gameObject : null);
                if (weaponNameText != null)
                    weaponNameText.text = string.Empty;
                ammoText.text = "-- / --";
                ammoText.color = Color.white;
                reloadText.text = string.Empty;
                reloadText.gameObject.SetActive(false);
                reloadFill.fillAmount = 0f;
                reloadFill.gameObject.SetActive(false);
                return;
            }

            playerDefinition = controller.GetComponent<RoboClashRobotDefinition>();
            playerDamageable = controller;
            playerCombat = controller.GetComponent<RoboClashRobotCombat>();
            playerAbilities = controller.GetComponent<RoboClashRobotAbilities>();

            if (playerDamageable != null)
            {
                playerDamageable.VitalsChanged += OnVitalsChanged;
                playerDamageable.DamageApplied += OnPlayerDamageApplied;
                playerDamageable.Eliminated += OnPlayerEliminated;
            }
            if (playerCombat != null)
                playerCombat.StateChanged += OnCombatStateChanged;
            if (playerAbilities != null)
            {
                playerAbilities.StateChanged += OnAbilityStateChanged;
                playerAbilities.AbilityActivated += OnAbilityActivated;
                playerAbilities.UltimateActivated += OnUltimateActivated;
                wasAbilityReady = playerAbilities.IsAbilityReady;
                wasUltimateReady = playerAbilities.IsUltimateReady;
            }
            playerController.HitConfirmed += OnHitConfirmed;

            if (weaponNameText != null)
            {
                weaponNameText.text = playerDefinition != null
                    ? playerDefinition.weaponName.ToUpperInvariant()
                    : "WEAPON";
                weaponNameText.color = playerDefinition != null ? playerDefinition.emissionColor : Cyan;
            }
            RefreshVitals();
            RefreshAmmo();
            RefreshAbilityStatus();
            RefreshCrosshairVisibility();
        }

        private void UnbindPlayer()
        {
            if (playerDamageable != null)
            {
                playerDamageable.VitalsChanged -= OnVitalsChanged;
                playerDamageable.DamageApplied -= OnPlayerDamageApplied;
                playerDamageable.Eliminated -= OnPlayerEliminated;
            }
            if (playerCombat != null)
                playerCombat.StateChanged -= OnCombatStateChanged;
            if (playerAbilities != null)
            {
                playerAbilities.StateChanged -= OnAbilityStateChanged;
                playerAbilities.AbilityActivated -= OnAbilityActivated;
                playerAbilities.UltimateActivated -= OnUltimateActivated;
            }
            if (playerController != null)
                playerController.HitConfirmed -= OnHitConfirmed;

            playerController = null;
            playerDefinition = null;
            playerDamageable = null;
            playerCombat = null;
            playerAbilities = null;
        }

        private void OnVitalsChanged(ArenaDamageable damageable)
        {
            if (damageable == playerDamageable)
                RefreshVitals();
        }

        private void OnPlayerDamageApplied(
            ArenaDamageable damageable, float amount, ArenaDamageable instigator)
        {
            if (damageable != playerDamageable || amount <= 0f || damageFlash == null)
                return;

            damageFlashAlpha = Mathf.Clamp01(damageFlashAlpha + 0.42f);
            damageFlash.color = new Color(1f, 0.035f, 0.02f, damageFlashAlpha);
        }

        private void OnPlayerEliminated(ArenaDamageable damageable)
        {
            if (!globalOverlayOnly && damageable == playerDamageable && eliminatedText != null)
            {
                eliminatedText.text = playerController != null && playerController.IsRespawning
                    ? "REBOOTING..."
                    : "ELIMINATED";
                eliminatedText.gameObject.SetActive(true);
            }
        }

        private void OnCombatStateChanged(RoboClashRobotCombat combat)
        {
            if (combat == playerCombat)
                RefreshAmmo();
        }

        private void OnAbilityStateChanged(RoboClashRobotAbilities abilities)
        {
            if (abilities == playerAbilities)
                RefreshAbilityStatus();
        }

        private void OnAbilityActivated()
        {
            ShowNotification("ABILITY ACTIVATED");
        }

        private void OnUltimateActivated()
        {
            ShowNotification("ULTIMATE ACTIVATED");
        }

        private void OnHitConfirmed()
        {
            if (crosshairText == null)
                return;
            crosshairText.color = Green;
            Invoke(nameof(ResetCrosshair), 0.16f);
        }

        private void ResetCrosshair()
        {
            if (crosshairText != null)
                crosshairText.color = Color.white;
        }

        private void RefreshVitals()
        {
            if (playerDamageable == null)
                return;

            var healthRatio = playerDamageable.MaximumHealth > 0f
                ? Mathf.Clamp01(playerDamageable.Health / playerDamageable.MaximumHealth)
                : 0f;
            var isLow = healthRatio > 0f && healthRatio <= 0.25f;
            if (!viewportOnly && !globalOverlayOnly && lowHealthTint != null)
                lowHealthTint.gameObject.SetActive(isLow);
            else
                HideObject(lowHealthTint != null ? lowHealthTint.gameObject : null);
            if (!viewportOnly && !globalOverlayOnly && isLow && !lowHealth)
                ShowNotification("LOW HEALTH");
            lowHealth = isLow;
            if (!playerDamageable.IsDead && eliminatedText.gameObject.activeSelf)
                eliminatedText.gameObject.SetActive(false);
        }

        private void UpdateDamageFlash(float deltaTime)
        {
            if (damageFlash == null || damageFlashAlpha <= 0f)
                return;

            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, deltaTime * 1.8f);
            damageFlash.color = new Color(1f, 0.035f, 0.02f, damageFlashAlpha);
        }

        private void RefreshAmmo()
        {
            if (playerCombat == null)
            {
                if (weaponNameText != null)
                    weaponNameText.text = "WEAPON UNAVAILABLE";
                ammoText.text = "-- / --";
                reloadText.text = string.Empty;
                reloadFill.fillAmount = 0f;
                return;
            }

            var capacity = playerCombat.MagazineCapacity;
            ammoText.text = $"{playerCombat.Ammunition:00} / {capacity:00}";
            var isLow = capacity > 0 && playerCombat.Ammunition <= Mathf.Max(1, capacity / 4);
            ammoText.color = isLow ? Amber : Color.white;
            if (isLow && !lowAmmo)
                ShowNotification("LOW AMMO");
            lowAmmo = isLow;

            if (playerCombat.IsReloading)
            {
                if (!wasReloading)
                    ShowNotification("RELOADING");
                reloadText.text = $"RELOADING  {playerCombat.ReloadRemaining:0.0}s";
                reloadFill.fillAmount = playerCombat.ReloadProgress;
                reloadText.gameObject.SetActive(true);
                reloadFill.gameObject.SetActive(true);
            }
            else
            {
                if (wasReloading && playerCombat.Ammunition >= capacity)
                    ShowNotification("RELOAD COMPLETE");
                else if (playerCombat.Ammunition <= 0 && !wasReloading && !wasOutOfAmmo)
                    ShowNotification("MAGAZINE EMPTY");
                reloadText.text = playerCombat.Ammunition <= 0 ? "EMPTY" : string.Empty;
                reloadFill.fillAmount = 0f;
                reloadText.gameObject.SetActive(playerCombat.Ammunition <= 0);
                reloadFill.gameObject.SetActive(false);
            }
            wasReloading = playerCombat.IsReloading;
            wasOutOfAmmo = playerCombat.Ammunition <= 0;
        }

        private void RefreshAbilityStatus()
        {
            if (playerAbilities == null)
                return;

            var abilityReady = playerAbilities.IsAbilityReady;
            var ultimateReady = playerAbilities.IsUltimateReady;

            if (wasAbilityReady == false && abilityReady)
                ShowNotification("ABILITY READY");
            if (wasUltimateReady == false && ultimateReady)
                ShowNotification("ULTIMATE READY");
            wasAbilityReady = abilityReady;
            wasUltimateReady = ultimateReady;
        }

        private void RefreshTimer(float remainingSeconds)
        {
            if (timerText == null)
                return;
            if (matchManager != null && matchManager.GameMode != RoboClashGameMode.Survival &&
                !matchManager.HasMatchTimer)
            {
                timerText.text = string.Empty;
                lastDisplayedSecond = -1;
                return;
            }

            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            if (totalSeconds == lastDisplayedSecond)
                return;
            lastDisplayedSecond = totalSeconds;
            timerText.text = matchManager != null && matchManager.GameMode == RoboClashGameMode.Survival
                ? $"TIME {totalSeconds / 60:00}:{totalSeconds % 60:00}"
                : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void RefreshMatchState(RoboClashMatchState state)
        {
            if (stateText != null)
            {
                stateText.text = state.ToString().ToUpperInvariant();
                stateText.color = state == RoboClashMatchState.Playing ? Green : Cyan;
            }
            ShowMatchState(state);
            if (state == RoboClashMatchState.Initializing && resultsRoot != null)
            {
                resultsShown = false;
                resultsRoot.SetActive(false);
            }
        }

        private void ShowMatchState(RoboClashMatchState state)
        {
            if (gameplayRoot == null)
                return;
            if (viewportOnly)
            {
                var finished = state == RoboClashMatchState.Victory ||
                               state == RoboClashMatchState.Defeat ||
                               state == RoboClashMatchState.Draw;
                gameplayRoot.SetActive(!finished &&
                                       state != RoboClashMatchState.Initializing);
                redesignedHud?.SetMatchState(state);
                if (!finished && state != RoboClashMatchState.Initializing)
                {
                    crosshairRoot.SetActive(state == RoboClashMatchState.Playing);
                    RefreshCrosshairVisibility();
                }
                return;
            }
            if (!globalOverlayOnly && redesignedHud != null)
                redesignedHud.SetMatchState(state);
            gameplayRoot.SetActive(state != RoboClashMatchState.Initializing);
            var isPlaying = state == RoboClashMatchState.Playing;
            if (!globalOverlayOnly)
                crosshairRoot.SetActive(isPlaying);
            RefreshCrosshairVisibility();
            if (state != RoboClashMatchState.Paused)
            {
                pauseRoot.SetActive(false);
                settingsPanel.Hide();
            }

            if (state == RoboClashMatchState.Victory ||
                state == RoboClashMatchState.Defeat ||
                state == RoboClashMatchState.Draw)
            {
                gameplayRoot.SetActive(false);
                if (stateText != null)
                    stateText.text = state.ToString().ToUpperInvariant();
            }
            else if (state == RoboClashMatchState.Paused)
                pauseRoot.SetActive(true);

            if (state == RoboClashMatchState.Countdown)
            {
                if (countdownBannerGroup != null)
                {
                    countdownBannerGroup.gameObject.SetActive(true);
                    countdownBannerGroup.alpha = 1f;
                    fadingCountdownBanner = false;
                }
                RefreshCountdownObjective();
                countdownText.text = matchManager != null ? matchManager.CountdownValue.ToString() : string.Empty;
            }
            else
            {
                if (countdownText != null)
                    countdownText.text = string.Empty;
                if (state == RoboClashMatchState.Playing && countdownBannerGroup != null &&
                    countdownBannerGroup.gameObject.activeSelf)
                    fadingCountdownBanner = true;
                else if (countdownBannerGroup != null)
                    countdownBannerGroup.gameObject.SetActive(false);
            }

            if (displayedMatchState != (int)state)
            {
                displayedMatchState = (int)state;
                if (state == RoboClashMatchState.Playing)
                    ShowNotification("FIGHT!");
            }
        }

        private void RefreshCountdown(int value)
        {
            if (countdownText != null && matchManager != null &&
                matchManager.State == RoboClashMatchState.Countdown)
            {
                countdownText.text = value > 0 ? value.ToString() : "FIGHT";
                RefreshCountdownObjective();
            }
        }

        private void RefreshCountdownObjective()
        {
            if (matchManager == null)
                return;
            if (countdownTitleText != null)
                countdownTitleText.text = RoboClashObjectives.Title(matchManager.GameMode);
            if (countdownObjectiveText != null)
                countdownObjectiveText.text = matchManager.GetObjectiveDescription();
        }

        private void UpdateCountdownBanner(float deltaTime)
        {
            if (!fadingCountdownBanner || countdownBannerGroup == null)
                return;

            countdownBannerGroup.alpha = Mathf.MoveTowards(countdownBannerGroup.alpha,
                0f, deltaTime * 2.5f);
            if (countdownBannerGroup.alpha <= 0f)
            {
                countdownBannerGroup.gameObject.SetActive(false);
                fadingCountdownBanner = false;
            }
        }

        private void RefreshCombatantCount()
        {
            RefreshModeStatus();
        }

        private void RefreshModeStatus()
        {
            if (matchManager == null || combatantsText == null)
                return;

            if (matchManager.GameMode == RoboClashGameMode.Survival)
            {
                var wave = matchManager.CurrentWave;
                var breakActive = matchManager.IsWaveBreakActive;
                if (wave < lastDisplayedWave)
                {
                    lastDisplayedWave = 0;
                    lastDisplayedWaveBreak = false;
                }
                if (wave > lastDisplayedWave && lastDisplayedWave > 0)
                    ShowNotification($"WAVE {wave} INCOMING");
                else if (breakActive && !lastDisplayedWaveBreak)
                    ShowNotification($"WAVE {wave} CLEARED");

                lastDisplayedWave = wave;
                lastDisplayedWaveBreak = breakActive;
                RefreshTimer(matchManager.ElapsedMatchSeconds);
                combatantsText.text = breakActive
                    ? $"WAVE {wave} CLEAR  //  NEXT {Mathf.CeilToInt(matchManager.NextWaveCountdown)}s"
                    : $"WAVE {wave}  //  LEFT {matchManager.EnemiesRemaining}  //  KILLS {matchManager.Statistics.PlayerEliminations}";
                scoreboardText.text = string.Empty;
                return;
            }

            switch (matchManager.GameMode)
            {
                case RoboClashGameMode.KillRace:
                    combatantsText.text =
                        $"KILL RACE  //  {matchManager.PlayerKills}/{matchManager.KillTarget}";
                    break;
                case RoboClashGameMode.TimedDeathmatch:
                    combatantsText.text = $"DEATHMATCH  //  {matchManager.AliveCombatantCount} ACTIVE";
                    break;
                case RoboClashGameMode.FreeForAll:
                    combatantsText.text = $"FREE-FOR-ALL  //  {matchManager.AliveCombatantCount} ACTIVE";
                    break;
                case RoboClashGameMode.Duel:
                    var inputRouter = playerController != null
                        ? playerController.GetComponent<RoboClashPlayerInputRouter>()
                        : null;
                    var playerIndex = inputRouter != null ? inputRouter.PlayerIndex : 0;
                    var kills = playerIndex >= 0
                        ? matchManager.GetPlayerKills(playerIndex)
                        : matchManager.PlayerKills;
                    combatantsText.text = matchManager.DuelUsesKillTarget
                        ? $"DUEL  //  {kills}/{matchManager.KillTarget} KILLS"
                        : $"DUEL  //  {kills} PLAYER KILLS";
                    break;
                case RoboClashGameMode.KingOfTheArena:
                    combatantsText.text =
                        $"KING  //  {matchManager.KingControllerName} {matchManager.KingControllerScore}s";
                    break;
                case RoboClashGameMode.Knockout:
                    combatantsText.text = $"KNOCKOUT  //  {matchManager.PlayerLivesRemaining} LIVES LEFT";
                    break;
                case RoboClashGameMode.Multiplayer:
                    combatantsText.text = matchManager.GetPlayerObjectiveProgress();
                    break;
                default:
                    combatantsText.text = $"COMBATANTS  {matchManager.AliveCombatantCount}";
                    break;
            }
            scoreboardText.text = matchManager.GetScoreboardText();
        }

        private void RefreshStatistics(RoboClashMatchStatistics statistics)
        {
            if (resultsShown)
                PopulateResultDetails(statistics);
        }

        private void ShowResults(RoboClashMatchStatistics statistics)
        {
            if (viewportOnly)
                return;
            if (resultsShown)
                return;
            resultsShown = true;
            gameplayRoot.SetActive(false);
            pauseRoot.SetActive(false);
            settingsPanel.Hide();
            resultTitleText.text = statistics.GameMode == RoboClashGameMode.Multiplayer ||
                                   statistics.GameMode == RoboClashGameMode.Duel &&
                                   matchManager.WinnerPlayerIndex >= 0
                ? $"PLAYER {Mathf.Max(0, matchManager.WinnerPlayerIndex) + 1} WINS"
                : statistics.FinalResult.ToString().ToUpperInvariant();
            resultTitleText.color = statistics.FinalResult == RoboClashMatchState.Victory
                ? Green
                : statistics.FinalResult == RoboClashMatchState.Defeat ? Red : Amber;
            PopulateResultDetails(statistics);
            resultsRoot.SetActive(true);
        }

        public void ConfigureMultiplayerGlobalOverlay()
        {
            globalOverlayOnly = true;
            if (canvas != null)
                canvas.sortingOrder = 600;
            HideObject(matchPanelRoot);
            HideObject(phoneInputStatusText != null ? phoneInputStatusText.gameObject : null);
            HideObject(scoreboardText != null ? scoreboardText.gameObject : null);
            HideObject(killFeedRoot);
            HideObject(notificationText != null ? notificationText.gameObject : null);
            HideObject(crosshairRoot);
            HideObject(lowHealthTint != null ? lowHealthTint.gameObject : null);
            HideObject(damageFlash != null ? damageFlash.gameObject : null);
            HideObject(eliminatedText != null ? eliminatedText.gameObject : null);
            HideObject(weaponNameText != null && weaponNameText.transform.parent != null
                ? weaponNameText.transform.parent.gameObject
                : null);
            redesignedHud?.SetMultiplayerOverlayOnly();
        }

        public void ConfigureMultiplayerPlayer(RobotArenaController controller,
            Camera playerCamera, int playerIndex)
        {
            explicitPlayerBinding = true;
            viewportOnly = true;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = playerCamera;
                canvas.planeDistance = 1f;
                canvas.sortingOrder = 20 + playerIndex;
            }

            HideObject(matchPanelRoot);
            HideObject(weaponNameText != null ? weaponNameText.gameObject : null);
            HideObject(phoneInputStatusText != null ? phoneInputStatusText.gameObject : null);
            HideObject(scoreboardText != null ? scoreboardText.gameObject : null);
            HideObject(killFeedRoot);
            HideObject(notificationText != null ? notificationText.gameObject : null);
            HideObject(lowHealthTint != null ? lowHealthTint.gameObject : null);
            HideObject(damageFlash != null ? damageFlash.gameObject : null);
            HideObject(pauseRoot);
            HideObject(resultsRoot);
            HideObject(countdownBannerGroup != null ? countdownBannerGroup.gameObject : null);
            HideObject(settingsPanel != null && settingsPanel.Root != null
                ? settingsPanel.Root.gameObject
                : null);

            if (playerKillsText == null)
            {
                playerKillsText = CreateText(gameplayRoot.transform, string.Empty, 18, Color.white,
                    new Vector2(0.35f, 0.9f), new Vector2(0.65f, 0.96f),
                    TextAnchor.MiddleCenter, true);
            }
            playerKillsText.gameObject.SetActive(true);

            if (redesignedHud == null)
                redesignedHud = GetComponent<RoboClashHudV2>();
            redesignedHud?.ConfigureMultiplayerViewport(controller, playerCamera,
                matchManager != null ? matchManager.transform : null);
            BindPlayer(controller);
            ShowMatchState(matchManager != null ? matchManager.State : RoboClashMatchState.Ready);
        }

        private static void HideObject(GameObject target)
        {
            if (target != null)
                target.SetActive(false);
        }

        private void PopulateResultDetails(RoboClashMatchStatistics statistics)
        {
            var robotName = playerDefinition != null
                ? playerDefinition.robotType.ToString().ToUpperInvariant()
                : "PLAYER";
            var duration = Mathf.Max(0, Mathf.FloorToInt(statistics.MatchDurationSeconds));
            var modeDetails = statistics.GameMode == RoboClashGameMode.Survival
                ? $"WAVE REACHED  {statistics.HighestWaveReached}  //  CLEARED  {statistics.HighestWaveCompleted}\n"
                : statistics.GameMode == RoboClashGameMode.FreeForAll &&
                  statistics.PlayerPlacement > 0
                    ? $"PLACEMENT  {statistics.PlayerPlacement}\n"
                    : string.Empty;
            var durationLabel = statistics.GameMode == RoboClashGameMode.Survival
                ? "SURVIVAL TIME"
                : "MATCH DURATION";
            if (statistics.GameMode == RoboClashGameMode.Multiplayer)
            {
                resultDetailsText.text =
                    $"{RoboClashObjectives.Title(statistics.GameMode)}\n" +
                    $"FIRST TO {matchManager.KillTarget} ELIMINATIONS\n\n" +
                    matchManager.GetScoreboardText();
                return;
            }
            if (statistics.GameMode == RoboClashGameMode.Duel &&
                matchManager.WinnerPlayerIndex >= 0)
            {
                var rule = matchManager.DuelUsesKillTarget
                    ? $"FIRST TO {matchManager.KillTarget} ELIMINATIONS"
                    : "MOST ELIMINATIONS WHEN TIME EXPIRES";
                resultDetailsText.text =
                    $"{RoboClashObjectives.Title(statistics.GameMode)}\n" +
                    $"{rule}\n\n" +
                    matchManager.GetScoreboardText();
                return;
            }
            var career = RoboClashSaveService.Current.career;
            resultDetailsText.text =
                $"{robotName}  //  {statistics.GameMode.ToString().ToUpperInvariant()}\n" +
                $"{modeDetails}" +
                $"ELIMINATIONS  {statistics.PlayerEliminations}      DEATHS  {statistics.PlayerDeaths}\n" +
                $"{durationLabel}  {duration / 60:00}:{duration % 60:00}\n" +
                $"{matchManager.GetScoreboardText()}\n" +
                $"LEVEL {career.level}  //  XP +{RoboClashSaveService.LastMatchXP}  " +
                $"({career.currentXP}/{RoboClashSaveService.GetXPRequiredForNextLevel(career.level)} XP)";
        }

        private void OnParticipantEliminated(ArenaDamageable eliminated, ArenaDamageable killer)
        {
            if (eliminated == null || viewportOnly)
                return;

            if (eliminated == playerDamageable)
            {
                PushKillFeed("YOU WERE ELIMINATED", Red);
                return;
            }

            var victimName = GetRobotName(eliminated);
            if (killer == null)
                PushKillFeed($"{victimName} WAS ELIMINATED", Color.white);
            else
            {
                var killerName = killer == playerDamageable ? "YOU" : GetRobotName(killer);
                PushKillFeed($"{killerName} ELIMINATED {victimName}",
                    killer == playerDamageable ? Green : Color.white);
            }
        }

        private static string GetRobotName(ArenaDamageable damageable)
        {
            var definition = damageable.GetComponent<RoboClashRobotDefinition>();
            return definition != null
                ? definition.robotType.ToString().ToUpperInvariant()
                : damageable.name.ToUpperInvariant();
        }

        private void PushKillFeed(string entry, Color color)
        {
            for (var i = killFeedEntries.Length - 1; i > 0; i--)
            {
                killFeedEntries[i] = killFeedEntries[i - 1];
                killFeedColors[i] = killFeedColors[i - 1];
                killFeedExpiry[i] = killFeedExpiry[i - 1];
                killFeedTexts[i].text = killFeedEntries[i] ?? string.Empty;
                killFeedTexts[i].color = killFeedColors[i];
                killFeedTexts[i].gameObject.SetActive(!string.IsNullOrEmpty(killFeedEntries[i]));
            }

            killFeedEntries[0] = entry;
            killFeedColors[0] = color;
            killFeedExpiry[0] = Time.unscaledTime + 5f;
            killFeedTexts[0].text = entry;
            killFeedTexts[0].color = color;
            killFeedTexts[0].gameObject.SetActive(true);
        }

        private void ShowNotification(string message)
        {
            if (globalOverlayOnly || viewportOnly || notificationText == null ||
                string.IsNullOrEmpty(message))
                return;
            notificationText.text = message;
            notificationExpiry = Time.unscaledTime + 1.7f;
            notificationText.gameObject.SetActive(true);
        }

        private void PauseMatch()
        {
            matchManager?.PauseMatch();
        }

        private void ResumeMatch()
        {
            settingsPanel.Hide();
            redesignedHud?.SetSettingsVisible(false);
            matchManager?.ResumeMatch();
        }

        private void RestartMatch()
        {
            resultsRoot.SetActive(false);
            pauseRoot.SetActive(false);
            settingsPanel.Hide();
            redesignedHud?.SetSettingsVisible(false);
            matchManager?.RestartMatch();
        }

        private void ReturnToMainMenu()
        {
            matchManager?.ReturnToMainMenu();
        }

        private void OpenPauseSettings()
        {
            redesignedHud?.SetSettingsVisible(true);
            settingsPanel.Show();
        }

        private void ClosePauseSettings()
        {
            settingsPanel.Hide();
            redesignedHud?.SetSettingsVisible(false);
        }

        private GameObject CreateOverlay(string name)
        {
            var overlay = CreatePanel(canvas.transform, name, Vector2.zero, Vector2.one,
                new Color(0.005f, 0.018f, 0.035f, 0.92f));
            return overlay;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null)
                panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private TextMeshProUGUI CreateText(Transform parent, string value, int size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment, bool important = false)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.color = color;
            text.text = value;
            text.alignment = RoboClashUiKit.ToTextAlignmentOptions(alignment);
            text.raycastTarget = false;
            RoboClashUiKit.StyleText(text, size, important);
            return text;
        }

        private Image CreateBar(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Color fillColor)
        {
            var background = CreatePanel(parent, name, anchorMin, anchorMax,
                new Color(0.04f, 0.09f, 0.13f, 1f));
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(background.transform, false);
            var rect = fillObject.GetComponent<RectTransform>();
            Stretch(rect);
            var image = fillObject.GetComponent<Image>();
            image.color = fillColor;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateMenuButton(Transform parent, string label, Vector2 anchorMin,
            Vector2 anchorMax, Action action)
        {
            var buttonObject = CreatePanel(parent, label + " Button", anchorMin, anchorMax,
                new Color(0.035f, 0.16f, 0.23f, 1f));
            RoboClashUiKit.StyleRoundedPanel(buttonObject.GetComponent<Image>());
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.8f, 0.92f, 1f);
            button.colors = colors;
            var labelText = CreateText(buttonObject.transform, label, 16, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true);
            button.onClick.AddListener(() => action());
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
