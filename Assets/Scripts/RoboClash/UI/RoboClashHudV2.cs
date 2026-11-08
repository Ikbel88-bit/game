using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashHudV2 : MonoBehaviour
    {
        [Header("Minimap")]
        [Min(1f)] public float minimapViewRadius = 30f;
        public bool rotateMinimapWithPlayer = true;
        public bool requireRobotComponent = true;

        private RectTransform canvasRoot;
        private Canvas canvas;
        private GameObject minimapRoot;
        private GameObject objectivePanelRoot;
        private HudBar healthBar;
        private HudBar shieldBar;
        private HudSlot abilitySlot;
        private HudSlot ultimateSlot;
        private Image vignette;
        private TextMeshProUGUI robotNameLabel;
        private TextMeshProUGUI guardLabel;
        private TextMeshProUGUI northLabel;
        private TextMeshProUGUI mapStatusLabel;
        private TextMeshProUGUI objectiveLabel;
        private TextMeshProUGUI progressLabel;
        private RoboClashMinimapV2 minimap;
        private RoboClashMatchManager matchManager;
        private RobotArenaController playerController;
        private ArenaDamageable damageable;
        private RoboClashRobotDefinition definition;
        private RoboClashRobotAbilities abilities;
        private RoboClashGuardMode guardMode;
        private float retryBindingTimer;
        private float mapRebuildTimer;
        private float displayUpdateTimer;
        private float objectiveUpdateTimer;
        private float hitFx;
        private float lastTotalVitals = -1f;
        private float abilityCooldownTotal;
        private float abilityActiveTotal;
        private float ultimateCooldownTotal;
        private float ultimateActiveTotal;
        private bool settingsVisible;
        private bool multiplayerViewport;

        private void Awake()
        {
            matchManager = GetComponentInParent<RoboClashMatchManager>();
            BuildInterface();
        }

        public void ConfigureMultiplayerViewport(RobotArenaController controller,
            Camera playerCamera, Transform arenaRoot)
        {
            multiplayerViewport = true;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = playerCamera;
                canvas.planeDistance = 1f;
            }

            if (minimapRoot != null)
                minimapRoot.SetActive(false);
            if (objectivePanelRoot != null)
                objectivePanelRoot.SetActive(false);
            if (vignette != null)
                vignette.gameObject.SetActive(false);
            if (robotNameLabel != null)
                robotNameLabel.gameObject.SetActive(false);
            if (guardLabel != null)
                guardLabel.gameObject.SetActive(false);
            BindPlayer(controller, arenaRoot);
        }

        public void SetMultiplayerOverlayOnly()
        {
            multiplayerViewport = true;
            if (canvasRoot != null)
                canvasRoot.gameObject.SetActive(false);
            if (minimapRoot != null)
            {
                Destroy(minimapRoot);
                minimapRoot = null;
                minimap = null;
            }
            if (objectivePanelRoot != null)
            {
                Destroy(objectivePanelRoot);
                objectivePanelRoot = null;
            }
            enabled = false;
        }

        private void OnDisable()
        {
            RoboClashGuardEvents.Changed -= OnGuardChanged;
            UnbindPlayer();
        }

        private void OnEnable()
        {
            RoboClashGuardEvents.Changed -= OnGuardChanged;
            RoboClashGuardEvents.Changed += OnGuardChanged;
        }

        private void OnDestroy()
        {
            RoboClashGuardEvents.Changed -= OnGuardChanged;
            UnbindPlayer();
        }

        public void SetMatchState(RoboClashMatchState state)
        {
            if (canvasRoot == null)
                return;

            var isFinished = state == RoboClashMatchState.Victory ||
                            state == RoboClashMatchState.Defeat ||
                            state == RoboClashMatchState.Draw;
            canvasRoot.gameObject.SetActive(!settingsVisible &&
                state != RoboClashMatchState.Initializing &&
                state != RoboClashMatchState.Paused && !isFinished);
        }

        public void SetSettingsVisible(bool visible)
        {
            settingsVisible = visible;
            if (matchManager != null)
                SetMatchState(matchManager.State);
        }

        public void BindPlayer(RobotArenaController controller, Transform arenaRoot)
        {
            UnbindPlayer();
            playerController = controller;
            damageable = controller;
            definition = controller != null
                ? controller.GetComponent<RoboClashRobotDefinition>()
                : null;
            abilities = controller != null
                ? controller.GetComponent<RoboClashRobotAbilities>()
                : null;
            guardMode = controller != null
                ? controller.GetComponent<RoboClashGuardMode>()
                : null;
            lastTotalVitals = -1f;
            hitFx = 0f;
            abilityCooldownTotal = 0f;
            abilityActiveTotal = 0f;
            ultimateCooldownTotal = 0f;
            ultimateActiveTotal = 0f;

            if (damageable != null)
            {
                damageable.VitalsChanged += OnVitalsChanged;
                damageable.DamageApplied += OnDamageApplied;
            }
            if (abilities != null)
                abilities.StateChanged += OnAbilityStateChanged;

            if (robotNameLabel != null)
            {
                var name = definition != null
                    ? definition.robotType.ToString().ToUpperInvariant()
                    : "PLAYER";
                var router = controller != null
                    ? controller.GetComponent<RoboClashPlayerInputRouter>()
                    : null;
                robotNameLabel.text = matchManager != null &&
                                      matchManager.GameMode == RoboClashGameMode.Multiplayer
                    ? $"PLAYER {(router != null ? router.PlayerIndex + 1 : 1)}  //  {name}"
                    : name;
                robotNameLabel.color = definition != null
                    ? definition.primaryColor
                    : Color.white;
            }
            if (guardLabel != null)
                guardLabel.gameObject.SetActive(guardMode != null && guardMode.IsGuarding);

            if (minimap != null && !multiplayerViewport)
            {
                minimap.viewRadius = Mathf.Max(1f, minimapViewRadius);
                minimap.rotateWithPlayer = rotateMinimapWithPlayer;
                minimap.requireRobotComponent = requireRobotComponent;
                minimap.Configure(controller != null ? controller.transform : null, arenaRoot,
                    controller != null ? controller.PlayerCamera : null);
                RefreshMapStatus();
                mapRebuildTimer = 1.5f;
            }
        }

        private void UnbindPlayer()
        {
            if (damageable != null)
            {
                damageable.VitalsChanged -= OnVitalsChanged;
                damageable.DamageApplied -= OnDamageApplied;
            }
            if (abilities != null)
                abilities.StateChanged -= OnAbilityStateChanged;

            playerController = null;
            damageable = null;
            definition = null;
            abilities = null;
            guardMode = null;
            if (guardLabel != null)
                guardLabel.gameObject.SetActive(false);
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("RoboClash HUD V2 Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot = (RectTransform)canvasObject.transform;

            vignette = RoboClashUiKit.Img("Damage Vignette", canvasRoot,
                new Color(0.9f, 0.05f, 0.05f, 0f), RoboClashUiKit.Vignette);
            RoboClashUiKit.Stretch(vignette.rectTransform);

            BuildVitals();
            BuildAbilities();
            if (matchManager == null || matchManager.GameMode != RoboClashGameMode.Multiplayer)
            {
                BuildMinimap();
                BuildObjectivePanel();
            }
        }

        private void BuildObjectivePanel()
        {
            var panel = RoboClashUiKit.Rect("Match Objective Panel", canvasRoot);
            objectivePanelRoot = panel.gameObject;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -118f);
            panel.sizeDelta = new Vector2(900f, 64f);

            var background = RoboClashUiKit.Img("Objective Background", panel,
                new Color(0f, 0f, 0f, 0.42f));
            RoboClashUiKit.StyleRoundedPanel(background);
            RoboClashUiKit.Stretch(background.rectTransform);
            objectiveLabel = RoboClashUiKit.Label("Objective Description", panel,
                string.Empty, 12, Color.white, TextAnchor.MiddleCenter);
            objectiveLabel.rectTransform.anchorMin = new Vector2(0.03f, 0.49f);
            objectiveLabel.rectTransform.anchorMax = new Vector2(0.97f, 0.97f);
            objectiveLabel.rectTransform.offsetMin = objectiveLabel.rectTransform.offsetMax = Vector2.zero;
            objectiveLabel.textWrappingMode = TextWrappingModes.NoWrap;
            progressLabel = RoboClashUiKit.Label("Objective Progress", panel,
                string.Empty, 13, RoboClashUiKit.Cyan, TextAnchor.MiddleCenter, FontStyles.Bold);
            progressLabel.rectTransform.anchorMin = new Vector2(0.03f, 0.04f);
            progressLabel.rectTransform.anchorMax = new Vector2(0.97f, 0.51f);
            progressLabel.rectTransform.offsetMin = progressLabel.rectTransform.offsetMax = Vector2.zero;
            progressLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void BuildVitals()
        {
            var panel = RoboClashUiKit.Rect("Vitals Panel", canvasRoot);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(36f, 36f);
            panel.sizeDelta = new Vector2(480f, 140f);

            var background = RoboClashUiKit.Img("Vitals Background", panel,
                new Color(0f, 0f, 0f, 0.45f));
            RoboClashUiKit.StyleRoundedPanel(background);
            RoboClashUiKit.Stretch(background.rectTransform);
            var accent = RoboClashUiKit.Img("Vitals Accent", panel, RoboClashUiKit.Cyan);
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.sizeDelta = new Vector2(4f, 0f);

            robotNameLabel = RoboClashUiKit.Label("Robot Name", panel, "PLAYER", 17,
                Color.white, TextAnchor.MiddleLeft);
            robotNameLabel.rectTransform.anchorMin = robotNameLabel.rectTransform.anchorMax =
                Vector2.zero;
            robotNameLabel.rectTransform.pivot = Vector2.zero;
            robotNameLabel.rectTransform.anchoredPosition = new Vector2(24f, 114f);
            robotNameLabel.rectTransform.sizeDelta = new Vector2(245f, 22f);
            guardLabel = RoboClashUiKit.Label("Guard Status", panel,
                "GUARD  -50% DAMAGE", 13, RoboClashUiKit.Cyan,
                TextAnchor.MiddleRight, FontStyles.Bold, true);
            guardLabel.rectTransform.anchorMin = guardLabel.rectTransform.anchorMax =
                guardLabel.rectTransform.pivot = Vector2.one;
            guardLabel.rectTransform.anchoredPosition = new Vector2(-20f, -12f);
            guardLabel.rectTransform.sizeDelta = new Vector2(190f, 24f);
            guardLabel.gameObject.SetActive(false);

            healthBar = new HudBar(panel, "HEALTH", new Vector2(442f, 42f),
                new Vector2(24f, 20f), new Color(0.2f, 1f, 0.45f), true, 18);
            shieldBar = new HudBar(panel, "SHIELD", new Vector2(442f, 30f),
                new Vector2(24f, 72f), RoboClashUiKit.Cyan, false, 15);
        }

        private void BuildAbilities()
        {
            abilitySlot = new HudSlot(canvasRoot, "Q", 88f, new Vector2(-88f, 88f),
                RoboClashUiKit.Cyan);
            ultimateSlot = new HudSlot(canvasRoot, "E", 106f, new Vector2(88f, 94f),
                RoboClashUiKit.Gold);
        }

        private void BuildMinimap()
        {
            var mapRoot = RoboClashUiKit.Rect("Circular Tactical Map", canvasRoot);
            minimapRoot = mapRoot.gameObject;
            mapRoot.anchorMin = mapRoot.anchorMax = mapRoot.pivot = new Vector2(1f, 1f);
            mapRoot.anchoredPosition = new Vector2(-38f, -38f);
            mapRoot.sizeDelta = new Vector2(252f, 252f);

            var shadow = RoboClashUiKit.Img("Map Shadow", mapRoot,
                new Color(0f, 0f, 0f, 0.5f), RoboClashUiKit.MinimapCircle);
            RoboClashUiKit.Stretch(shadow.rectTransform, -8f);

            var maskImage = RoboClashUiKit.Img("Circular Map Mask", mapRoot, Color.white,
                RoboClashUiKit.MinimapCircle);
            RoboClashUiKit.Stretch(maskImage.rectTransform);
            var mask = maskImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var background = RoboClashUiKit.Img("Map Background", maskImage.transform,
                new Color(0.01f, 0.03f, 0.05f, 0.98f));
            RoboClashUiKit.Stretch(background.rectTransform);

            var mapRect = RoboClashUiKit.Rect("Map Geometry", maskImage.transform);
            RoboClashUiKit.Stretch(mapRect);
            minimap = mapRect.gameObject.AddComponent<RoboClashMinimapV2>();
            minimap.raycastTarget = false;
            minimap.viewRadius = minimapViewRadius;
            minimap.rotateWithPlayer = rotateMinimapWithPlayer;
            minimap.requireRobotComponent = requireRobotComponent;

            var border = RoboClashUiKit.Img("Map Border", mapRoot,
                new Color(0.2f, 0.85f, 1f, 0.95f), RoboClashUiKit.MinimapRing);
            RoboClashUiKit.Stretch(border.rectTransform, -1f);

            northLabel = RoboClashUiKit.Label("North Indicator", mapRoot, "N", 17,
                RoboClashUiKit.Gold, TextAnchor.MiddleCenter);
            northLabel.rectTransform.anchorMin = northLabel.rectTransform.anchorMax =
                northLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            northLabel.rectTransform.sizeDelta = new Vector2(26f, 26f);
            mapStatusLabel = RoboClashUiKit.Label("Map Availability", mapRoot,
                "MAP UNAVAILABLE", 12, RoboClashUiKit.Gold, TextAnchor.MiddleCenter);
            mapStatusLabel.rectTransform.anchorMin = mapStatusLabel.rectTransform.anchorMax =
                mapStatusLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mapStatusLabel.rectTransform.sizeDelta = new Vector2(200f, 28f);
            mapStatusLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var time = Time.unscaledTime;

            if (playerController == null)
            {
                retryBindingTimer -= deltaTime;
                if (retryBindingTimer <= 0f)
                {
                    retryBindingTimer = 0.5f;
                    TryBindMatchPlayer();
                }
            }

            if (!multiplayerViewport && mapRebuildTimer > 0f)
            {
                mapRebuildTimer -= deltaTime;
                if (mapRebuildTimer <= 0f && minimap != null)
                {
                    minimap.Rebuild();
                    RefreshMapStatus();
                }
            }

            if (damageable != null)
                UpdateVitals(deltaTime, time);

            displayUpdateTimer -= deltaTime;
            if (displayUpdateTimer <= 0f)
            {
                displayUpdateTimer = 0.1f;
                if (abilities != null)
                    UpdateAbilitySlots(time);
                UpdateNorth();
            }

            if (!multiplayerViewport)
            {
                objectiveUpdateTimer -= deltaTime;
                if (objectiveUpdateTimer <= 0f)
                {
                    objectiveUpdateTimer = 0.2f;
                    RefreshObjectiveLines();
                }
            }
        }

        private void RefreshObjectiveLines()
        {
            if (matchManager == null)
                return;

            if (objectiveLabel != null)
            {
                var mode = matchManager.GameMode;
                objectiveLabel.text = RoboClashObjectives.Title(mode) + "  //  " +
                                      matchManager.GetObjectiveDescription();
            }
            if (progressLabel != null)
                progressLabel.text = matchManager.GetPlayerObjectiveProgress();
        }

        private void TryBindMatchPlayer()
        {
            if (matchManager == null)
                matchManager = GetComponentInParent<RoboClashMatchManager>();
            if (matchManager != null && matchManager.PlayerController != null)
                BindPlayer(matchManager.PlayerController, matchManager.transform);
        }

        private void UpdateVitals(float deltaTime, float time)
        {
            var health = damageable.Health;
            var maximumHealth = damageable.MaximumHealth;
            var shield = damageable.Shield;
            var maximumShield = damageable.MaximumShield;
            healthBar.Tick(health, maximumHealth, deltaTime, time);
            shieldBar.Tick(shield, maximumShield, deltaTime, time);

            var total = health + shield;
            if (lastTotalVitals >= 0f && total < lastTotalVitals - 0.01f)
                hitFx = Mathf.Min(1f, hitFx + (lastTotalVitals - total) /
                    Mathf.Max(1f, maximumHealth) * 3f + 0.35f);
            lastTotalVitals = total;
            hitFx = Mathf.MoveTowards(hitFx, 0f, deltaTime * 2.2f);

            var ratio = maximumHealth > 0f ? Mathf.Clamp01(health / maximumHealth) : 0f;
            var lowHealthPulse = !damageable.IsDead && ratio <= 0.25f
                ? 0.18f + 0.12f * Mathf.Sin(time * 6f)
                : 0f;
            vignette.color = new Color(0.9f, 0.05f, 0.05f,
                Mathf.Max(hitFx * 0.55f, lowHealthPulse));
        }

        private void UpdateAbilitySlots(float time)
        {
            var dead = damageable != null && damageable.IsDead;
            var abilityName = definition != null
                ? definition.abilityName.ToUpperInvariant()
                : "ABILITY";
            var ultimateName = definition != null
                ? definition.ultimateName.ToUpperInvariant()
                : "ULTIMATE";

            if (dead)
            {
                abilitySlot.Set(abilityName, 0f, 1f, "--", "OFFLINE",
                    RoboClashUiKit.Dim, false, time);
                ultimateSlot.Set(ultimateName, 0f, 1f, "--", "OFFLINE",
                    RoboClashUiKit.Dim, false, time);
                return;
            }

            if (abilities.IsAbilityLocked)
            {
                abilitySlot.Set(abilityName, 0f, 1f, "LOCK", "LOCKED",
                    RoboClashUiKit.Dim, false, time);
            }
            else if (abilities.IsAbilityActive)
            {
                var remaining = abilities.AbilityActiveRemaining;
                if (remaining > abilityActiveTotal)
                    abilityActiveTotal = remaining;
                var duration = definition != null
                    ? definition.abilityDuration
                    : abilityActiveTotal;
                abilitySlot.Set(abilityName,
                    duration > 0f ? remaining / duration : 0f, 0f,
                    remaining.ToString("0.0"), "ACTIVE", RoboClashUiKit.Orange, false, time);
            }
            else if (!abilities.IsAbilityReady)
            {
                var remaining = abilities.AbilityCooldownRemaining;
                if (remaining > abilityCooldownTotal)
                    abilityCooldownTotal = remaining;
                var fraction = abilityCooldownTotal > 0f
                    ? Mathf.Clamp01(remaining / abilityCooldownTotal)
                    : 0f;
                abilitySlot.Set(abilityName, 1f - fraction, fraction,
                    remaining.ToString(remaining >= 10f ? "0" : "0.0"),
                    "COOLDOWN", RoboClashUiKit.Cyan, false, time);
            }
            else
            {
                abilityCooldownTotal = 0f;
                abilityActiveTotal = 0f;
                abilitySlot.Set(abilityName, 1f, 0f, "Q", "READY",
                    RoboClashUiKit.Cyan, true, time);
            }

            if (abilities.IsAbilityLocked)
            {
                ultimateSlot.Set(ultimateName, 0f, 1f, "LOCK", "LOCKED",
                    RoboClashUiKit.Dim, false, time);
            }
            else if (abilities.IsUltimateActive)
            {
                var remaining = abilities.UltimateActiveRemaining;
                if (remaining > ultimateActiveTotal)
                    ultimateActiveTotal = remaining;
                var duration = definition != null
                    ? definition.ultimateDuration
                    : ultimateActiveTotal;
                ultimateSlot.Set(ultimateName,
                    duration > 0f ? remaining / duration : 0f, 0f,
                    remaining.ToString("0.0"), "ACTIVE", RoboClashUiKit.Orange, false, time);
            }
            else if (abilities.UltimateCooldownRemaining > 0.05f)
            {
                var remaining = abilities.UltimateCooldownRemaining;
                if (remaining > ultimateCooldownTotal)
                    ultimateCooldownTotal = remaining;
                var fraction = ultimateCooldownTotal > 0f
                    ? Mathf.Clamp01(remaining / ultimateCooldownTotal)
                    : 0f;
                ultimateSlot.Set(ultimateName, 1f - fraction, fraction,
                    remaining.ToString(remaining >= 10f ? "0" : "0.0"),
                    "COOLDOWN", RoboClashUiKit.Cyan, false, time);
            }
            else if (abilities.IsUltimateReady)
            {
                ultimateCooldownTotal = 0f;
                ultimateActiveTotal = 0f;
                ultimateSlot.Set(ultimateName, 1f, 0f, "E", "READY",
                    RoboClashUiKit.Gold, true, time);
            }
            else if (abilities.UltimateChargeRequired > 0f)
            {
                var charge = Mathf.Clamp01(abilities.UltimateCharge /
                                           abilities.UltimateChargeRequired);
                ultimateSlot.Set(ultimateName, charge, 0f,
                    Mathf.FloorToInt(charge * 100f) + "%", "CHARGING",
                    RoboClashUiKit.Gold, false, time);
            }
            else
            {
                ultimateSlot.Set(ultimateName, 0f, 0f, "--", "STANDBY",
                    RoboClashUiKit.Dim, false, time);
            }
        }

        private void UpdateNorth()
        {
            if (northLabel == null || playerController == null)
                return;

            var offset = Vector2.up;
            if (rotateMinimapWithPlayer)
            {
                var yaw = playerController.transform.eulerAngles.y * Mathf.Deg2Rad;
                offset = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
            }
            northLabel.rectTransform.anchoredPosition = offset * 106f;
        }

        private void RefreshMapStatus()
        {
            if (mapStatusLabel != null && minimap != null)
                mapStatusLabel.gameObject.SetActive(!minimap.HasMapData);
        }

        private void OnVitalsChanged(ArenaDamageable changed)
        {
            if (changed == damageable)
                lastTotalVitals = -1f;
        }

        private void OnDamageApplied(ArenaDamageable damaged, float amount,
            ArenaDamageable instigator)
        {
            if (damaged == damageable && amount > 0f)
                hitFx = Mathf.Clamp01(hitFx + amount /
                    Mathf.Max(1f, damageable.MaximumHealth) * 3f + 0.35f);
        }

        private void OnAbilityStateChanged(RoboClashRobotAbilities changed)
        {
            if (changed == abilities)
                displayUpdateTimer = 0f;
        }

        private void OnGuardChanged(RoboClashGuardMode changed, bool guarding)
        {
            if (changed == guardMode && guardLabel != null)
                guardLabel.gameObject.SetActive(guarding);
        }

        private sealed class HudBar
        {
            private readonly Image fill;
            private readonly Image ghost;
            private readonly Image flash;
            private readonly TextMeshProUGUI value;
            private readonly bool isHealth;
            private readonly Color baseColor;
            private float shown = 1f;
            private float ghostShown = 1f;
            private float previous = 1f;
            private float hold;
            private float flashAmount;
            private int lastCurrent = -1;
            private int lastMaximum = -1;

            public HudBar(Transform parent, string title, Vector2 size, Vector2 position,
                Color color, bool health, int fontSize)
            {
                isHealth = health;
                baseColor = color;
                var root = RoboClashUiKit.Rect(title + " Bar", parent);
                root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
                root.anchoredPosition = position;
                root.sizeDelta = size;

                var border = RoboClashUiKit.Img("Border", root,
                    new Color(color.r, color.g, color.b, 0.65f));
                RoboClashUiKit.StyleRoundedPanel(border);
                RoboClashUiKit.Stretch(border.rectTransform);
                var background = RoboClashUiKit.Img("Background", root,
                    new Color(0.02f, 0.04f, 0.07f, 0.98f));
                RoboClashUiKit.StyleRoundedPanel(background);
                RoboClashUiKit.Stretch(background.rectTransform, 2f);

                ghost = RoboClashUiKit.Img("Damage Trail", background.transform,
                    new Color(1f, 0.55f, 0.2f, 0.9f));
                RoboClashUiKit.Stretch(ghost.rectTransform);
                fill = RoboClashUiKit.Img("Fill", background.transform, color);
                RoboClashUiKit.Stretch(fill.rectTransform);

                for (var i = 1; i <= 3; i++)
                {
                    var tick = RoboClashUiKit.Img("Tick " + i, background.transform,
                        new Color(0f, 0f, 0f, 0.48f));
                    tick.rectTransform.anchorMin = new Vector2(i / 4f, 0f);
                    tick.rectTransform.anchorMax = new Vector2(i / 4f, 1f);
                    tick.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    tick.rectTransform.sizeDelta = new Vector2(2f, 0f);
                }

                flash = RoboClashUiKit.Img("Hit Flash", background.transform,
                    new Color(1f, 1f, 1f, 0f));
                RoboClashUiKit.Stretch(flash.rectTransform);

                var titleText = RoboClashUiKit.Label("Title", root, title, fontSize - 4,
                    new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleLeft, FontStyles.Bold);
                titleText.rectTransform.offsetMin = new Vector2(9f, 0f);
                value = RoboClashUiKit.Label("Value", root, string.Empty, fontSize,
                    Color.white, TextAnchor.MiddleRight, important: true);
                value.rectTransform.offsetMax = new Vector2(-9f, 0f);
                SetRatio(fill.rectTransform, 1f);
                SetRatio(ghost.rectTransform, 1f);
            }

            public void Tick(float current, float maximum, float deltaTime, float time)
            {
                var target = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
                if (target < previous - 0.0005f)
                {
                    hold = 0.4f;
                    flashAmount = 1f;
                }
                previous = target;

                shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-18f * deltaTime));
                if (ghostShown < target)
                    ghostShown = target;
                else if (hold > 0f)
                    hold -= deltaTime;
                else
                    ghostShown = Mathf.MoveTowards(ghostShown, target, deltaTime * 0.5f);
                flashAmount = Mathf.MoveTowards(flashAmount, 0f, deltaTime * 3.5f);

                SetRatio(fill.rectTransform, shown);
                SetRatio(ghost.rectTransform, Mathf.Max(ghostShown, shown));
                fill.color = isHealth ? HealthColor(target, time) : baseColor;
                flash.color = new Color(1f, 1f, 1f, flashAmount * 0.4f);

                var currentValue = Mathf.CeilToInt(current);
                var maximumValue = Mathf.CeilToInt(maximum);
                if (currentValue != lastCurrent || maximumValue != lastMaximum)
                {
                    lastCurrent = currentValue;
                    lastMaximum = maximumValue;
                    value.text = currentValue + " / " + maximumValue;
                }
            }

            private static Color HealthColor(float ratio, float time)
            {
                var green = new Color(0.2f, 1f, 0.45f);
                var yellow = new Color(1f, 0.9f, 0.2f);
                var red = new Color(1f, 0.2f, 0.2f);
                var color = ratio > 0.5f
                    ? Color.Lerp(yellow, green, (ratio - 0.5f) * 2f)
                    : Color.Lerp(red, yellow, ratio * 2f);
                if (ratio <= 0.25f)
                    color = Color.Lerp(color, Color.white,
                        0.25f * (0.5f + 0.5f * Mathf.Sin(time * 8f)));
                return color;
            }

            private static void SetRatio(RectTransform rectTransform, float ratio)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }

        private sealed class HudSlot
        {
            private readonly Image glow;
            private readonly Image overlay;
            private readonly Image ringFill;
            private readonly TextMeshProUGUI center;
            private readonly TextMeshProUGUI caption;
            private string currentName;
            private string currentCenter;
            private string currentState;

            public HudSlot(Transform parent, string key, float size, Vector2 position,
                Color accent)
            {
                var root = RoboClashUiKit.Rect("Slot " + key, parent);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = position;
                root.sizeDelta = new Vector2(size, size);

                glow = RoboClashUiKit.Img("Ready Glow", root,
                    new Color(accent.r, accent.g, accent.b, 0f), RoboClashUiKit.Circle);
                RoboClashUiKit.Stretch(glow.rectTransform, -12f);
                var background = RoboClashUiKit.Img("Slot Background", root,
                    new Color(0.03f, 0.05f, 0.09f, 0.92f), RoboClashUiKit.Circle);
                RoboClashUiKit.Stretch(background.rectTransform);
                var ringBackground = RoboClashUiKit.Img("Ring Background", root,
                    new Color(1f, 1f, 1f, 0.16f), RoboClashUiKit.ThickRing);
                RoboClashUiKit.Stretch(ringBackground.rectTransform);

                ringFill = RoboClashUiKit.Img("Cooldown Sweep", root, accent,
                    RoboClashUiKit.ThickRing);
                RoboClashUiKit.Stretch(ringFill.rectTransform);
                ringFill.type = Image.Type.Filled;
                ringFill.fillMethod = Image.FillMethod.Radial360;
                ringFill.fillOrigin = (int)Image.Origin360.Top;
                ringFill.fillClockwise = true;
                ringFill.fillAmount = 1f;

                overlay = RoboClashUiKit.Img("Cooldown Overlay", root,
                    new Color(0f, 0f, 0f, 0.62f), RoboClashUiKit.Circle);
                RoboClashUiKit.Stretch(overlay.rectTransform, size * 0.1f);
                overlay.type = Image.Type.Filled;
                overlay.fillMethod = Image.FillMethod.Radial360;
                overlay.fillOrigin = (int)Image.Origin360.Top;
                overlay.fillClockwise = true;
                overlay.fillAmount = 0f;

                center = RoboClashUiKit.Label("Slot Value", root, key,
                    Mathf.RoundToInt(size * 0.3f), Color.white, TextAnchor.MiddleCenter,
                    important: true);
                var badge = RoboClashUiKit.Img("Key Badge", root, accent);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax =
                    new Vector2(1f, 1f);
                badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                badge.rectTransform.anchoredPosition = new Vector2(-7f, -7f);
                badge.rectTransform.sizeDelta = new Vector2(24f, 24f);
                var keyText = RoboClashUiKit.Label("Key", badge.transform, key, 15,
                    new Color(0.02f, 0.05f, 0.08f), TextAnchor.MiddleCenter);

                caption = RoboClashUiKit.Label("Ability Name and State", parent,
                    string.Empty, 13, new Color(1f, 1f, 1f, 0.92f),
                    TextAnchor.UpperCenter);
                caption.rectTransform.anchorMin = caption.rectTransform.anchorMax =
                    new Vector2(0.5f, 0f);
                caption.rectTransform.pivot = new Vector2(0.5f, 1f);
                caption.rectTransform.anchoredPosition = position + new Vector2(0f, -size * 0.5f - 9f);
                caption.rectTransform.sizeDelta = new Vector2(260f, 22f);
            }

            public void Set(string name, float ringRatio, float overlayRatio, string centerText,
                string state, Color ringColor, bool ready, float time)
            {
                ringFill.fillAmount = Mathf.Clamp01(ringRatio);
                ringFill.color = ringColor;
                overlay.fillAmount = Mathf.Clamp01(overlayRatio);

                if (centerText != currentCenter)
                {
                    currentCenter = centerText;
                    center.text = centerText;
                }
                if (name != currentName || state != currentState)
                {
                    currentName = name;
                    currentState = state;
                    caption.text = name + "  -  " + state;
                }

                center.color = ready ? ringColor : Color.white;
                var pulse = ready ? 0.5f + 0.5f * Mathf.Sin(time * 5f) : 0f;
                glow.color = new Color(ringColor.r, ringColor.g, ringColor.b,
                    ready ? 0.12f + 0.25f * pulse : 0f);
            }
        }
    }
}
