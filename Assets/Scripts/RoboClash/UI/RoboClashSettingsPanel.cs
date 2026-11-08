using System;
using System.Collections.Generic;
using RoboClash;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashSettingsPanel : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.03f, 0.06f, 0.11f, 0.82f);
        private static readonly Color ControlColor = new Color(0.08f, 0.16f, 0.26f, 0.92f);
        private static readonly Color Blue = new Color(0.30f, 0.70f, 1f);
        private static readonly Color Orange = new Color(1f, 0.58f, 0.16f);
        private static readonly int[] FovValues = { 70, 80, 90, 100, 110 };
        private static readonly float[] SensitivityValues = { 0.06f, 0.09f, 0.12f, 0.16f, 0.22f };
        private static readonly string[] SensitivityLabels =
            { "VERY LOW", "LOW", "MEDIUM", "HIGH", "VERY HIGH" };
        private const float RowHeight = 0.05f;

        private readonly List<Vector2Int> availableResolutions = new List<Vector2Int>();
        private Transform customRows;
        private TMP_FontAsset font;
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
        private RoboClashOptionSelector resolutionDropdown;
        private RoboClashOptionSelector qualityDropdown;
        private RoboClashOptionSelector frameRateDropdown;
        private RoboClashOptionSelector cameraModeDropdown;
        private RoboClashOptionSelector fovDropdown;
        private RoboClashOptionSelector sensitivityDropdown;
        private int customRowIndex;
        private bool refreshing;
        private bool confirmationClosesPanel;
        private bool resolutionChangePending;
        private float resolutionConfirmationSeconds;
        private int lastConfirmationDisplayedSecond = -1;
        private int previousResolutionWidth;
        private int previousResolutionHeight;
        private bool previousFullscreen;
        private TextMeshProUGUI confirmationMessage;

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

            var secondsRemaining = Mathf.CeilToInt(resolutionConfirmationSeconds);
            if (secondsRemaining != lastConfirmationDisplayedSecond)
            {
                lastConfirmationDisplayedSecond = secondsRemaining;
                confirmationMessage.text =
                    $"KEEP THIS DISPLAY MODE? REVERTING IN {secondsRemaining} SECONDS.";
            }
        }

        public void Initialize(Transform canvasRoot, TMP_FontAsset uiFont, Action onBack)
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
            backdrop.color = new Color(0f, 0.01f, 0.03f, 0.78f);
            contentRoot = CreateObject("Settings Content", root.transform);
            Stretch(contentRoot.GetComponent<RectTransform>());
            BuildControls();
            root.SetActive(false);
        }

        public void Show()
        {
            RoboClashUIEventSystem.EnsureForScene(gameObject,
                gameObject.GetComponent<RoboClashMenuController>() != null
                    ? "RoboClash Menu Event System"
                    : "RoboClash Gameplay Event System");
            Refresh();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
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

        private void SetCameraMode(int index)
        {
            if (refreshing)
                return;

            RoboClashSession.SaveCameraMode(index == (int)RoboClashCameraMode.ThirdPerson
                ? RoboClashCameraMode.ThirdPerson
                : RoboClashCameraMode.FirstPerson);
        }

        public void ShowConfirmation(string message, Action onConfirmed, bool closePanelWhenFinished)
        {
            confirmationAction = onConfirmed;
            confirmationClosesPanel = closePanelWhenFinished;
            root.SetActive(true);
            contentRoot.SetActive(false);
            confirmationRoot.SetActive(true);
            confirmationRoot.transform.SetAsLastSibling();
            confirmationMessage.text = message;
        }

        public RoboClashOptionSelector AddCustomDropdownRow(string label, IReadOnlyList<string> options,
            int selectedIndex, Action<int> onChanged)
        {
            if (customRows == null)
                return null;

            var y = customRowIndex++ == 0 ? 0.18f : 0.10f;
            CreateText(customRows, label.ToUpperInvariant(), 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + 0.065f), TextAnchor.MiddleLeft);
            var dropdown = CreateSettingDropdown(customRows,
                new Vector2(0.42f, y), new Vector2(0.94f, y + 0.065f));
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(options.Count > 0
                ? Mathf.Clamp(selectedIndex, 0, options.Count - 1)
                : 0);
            dropdown.onValueChanged.AddListener(value =>
            {
                if (!refreshing)
                    onChanged?.Invoke(value);
            });
            return dropdown;
        }

        public void Refresh()
        {
            if (root == null)
                return;

            refreshing = true;
            try
            {
                var settings = RoboClashSaveService.Current.settings;
                masterSlider.SetValueWithoutNotify(settings.masterVolume);
                musicSlider.SetValueWithoutNotify(settings.musicVolume);
                sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
                fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
                vSyncToggle.SetIsOnWithoutNotify(settings.vSync);
                cameraModeDropdown.SetValueWithoutNotify((int)RoboClashSession.CameraMode);
                fovDropdown.SetValueWithoutNotify(
                    NearestIndex(FovValues, RoboClashSession.FieldOfView));
                sensitivityDropdown.SetValueWithoutNotify(
                    NearestIndex(SensitivityValues, RoboClashSession.MouseSensitivity));
                PopulateResolutionOptions(settings.resolutionWidth, settings.resolutionHeight);
                PopulateQualityOptions(settings.qualityLevel);
                frameRateDropdown.SetValueWithoutNotify(
                    GetFrameRateOption(settings.targetFrameRate));
            }
            finally
            {
                refreshing = false;
            }
        }

        private void BuildControls()
        {
            var panel = CreatePanel(contentRoot.transform, "Settings Panel", PanelColor,
                new Vector2(0.13f, 0.045f), new Vector2(0.87f, 0.955f));
            CreateText(panel.transform, "AUDIO & DISPLAY SETTINGS", 30, Blue,
                new Vector2(0.06f, 0.92f), new Vector2(0.94f, 0.995f),
                TextAnchor.MiddleCenter).fontStyle = FontStyles.Bold;
            CreateText(panel.transform, "ARENA AMBIENCE ENABLED // MUSIC TRACKS NOT ASSIGNED.",
                11, new Color(1f, 0.72f, 0.25f),
                new Vector2(0.06f, 0.89f), new Vector2(0.94f, 0.92f), TextAnchor.MiddleCenter);

            masterSlider = AddSliderRow(panel.transform, "MASTER VOLUME", RowY(0),
                RoboClashSaveService.Current.settings.masterVolume, RoboClashSaveService.SetMasterVolume);
            musicSlider = AddSliderRow(panel.transform, "MUSIC VOLUME", RowY(1),
                RoboClashSaveService.Current.settings.musicVolume, RoboClashSaveService.SetMusicVolume);
            musicSlider.interactable = true;
            sfxSlider = AddSliderRow(panel.transform, "SFX VOLUME", RowY(2),
                RoboClashSaveService.Current.settings.sfxVolume, RoboClashSaveService.SetSfxVolume);

            CreateSettingLabel(panel.transform, "FULLSCREEN", 3);
            fullscreenToggle = CreateToggle(panel.transform, SettingMinimum(3),
                SettingMaximum(3, 0.52f),
                RoboClashSaveService.Current.settings.fullscreen, RoboClashSaveService.SetFullscreen);

            CreateSettingLabel(panel.transform, "RESOLUTION", 4);
            resolutionDropdown = CreateSettingDropdown(panel.transform, 4);
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);

            CreateSettingLabel(panel.transform, "GRAPHICS QUALITY", 5);
            qualityDropdown = CreateSettingDropdown(panel.transform, 5);
            qualityDropdown.onValueChanged.AddListener(ApplyQualityLevel);

            CreateSettingLabel(panel.transform, "V-SYNC", 6);
            vSyncToggle = CreateToggle(panel.transform, SettingMinimum(6),
                SettingMaximum(6, 0.52f),
                RoboClashSaveService.Current.settings.vSync, RoboClashSaveService.SetVSync);

            CreateSettingLabel(panel.transform, "FRAME-RATE LIMIT", 7);
            frameRateDropdown = CreateSettingDropdown(panel.transform, 7);
            frameRateDropdown.AddOptions(new List<string>
            {
                "30 FPS", "60 FPS", "120 FPS", "144 FPS", "UNLIMITED"
            });
            frameRateDropdown.onValueChanged.AddListener(ApplyFrameRate);

            CreateSettingLabel(panel.transform, "CAMERA MODE", 8);
            cameraModeDropdown = CreateSettingDropdown(panel.transform, 8);
            cameraModeDropdown.AddOptions(new List<string> { "FIRST PERSON", "THIRD PERSON" });
            cameraModeDropdown.onValueChanged.AddListener(SetCameraMode);

            CreateSettingLabel(panel.transform, "FIELD OF VIEW", 9);
            fovDropdown = CreateSettingDropdown(panel.transform, 9);
            fovDropdown.AddOptions(new List<string> { "70", "80", "90", "100", "110" });
            fovDropdown.onValueChanged.AddListener(index =>
            {
                if (!refreshing)
                    RoboClashSession.SaveFieldOfView(
                        FovValues[Mathf.Clamp(index, 0, FovValues.Length - 1)]);
            });

            CreateSettingLabel(panel.transform, "MOUSE SENSITIVITY", 10);
            sensitivityDropdown = CreateSettingDropdown(panel.transform, 10);
            sensitivityDropdown.AddOptions(new List<string>(SensitivityLabels));
            sensitivityDropdown.onValueChanged.AddListener(index =>
            {
                if (!refreshing)
                    RoboClashSession.SaveMouseSensitivity(
                        SensitivityValues[Mathf.Clamp(index, 0, SensitivityValues.Length - 1)]);
            });

            customRows = panel.transform;
            CreateText(panel.transform, "CONTROLS", 12, Blue, new Vector2(0.06f, 0.12f),
                new Vector2(0.39f, 0.18f), TextAnchor.MiddleLeft);
            CreateText(panel.transform,
                "WASD MOVE // MOUSE AIM // LMB FIRE // R RELOAD // Q ABILITY // E ULTIMATE // SPACE JUMP // ESC MENU",
                10, Color.white, new Vector2(0.42f, 0.12f), new Vector2(0.94f, 0.20f),
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
            background.color = new Color(0f, 0.01f, 0.03f, 0.88f);
            var panel = CreatePanel(overlay.transform, "Confirmation Panel", PanelColor,
                new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.7f));
            confirmationMessage = CreateText(panel.transform, string.Empty, 19, Color.white,
                new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.88f), TextAnchor.MiddleCenter,
                "Message", true);
            CreateButton(panel.transform, "CONFIRM", new Vector2(0.1f, 0.12f),
                new Vector2(0.45f, 0.32f), ConfirmAction);
            CreateButton(panel.transform, "CANCEL", new Vector2(0.55f, 0.12f),
                new Vector2(0.9f, 0.32f), CancelConfirmation);
            return overlay;
        }

        private Slider AddSliderRow(Transform parent, string label, float y, float value, Action<float> changed)
        {
            CreateText(parent, label, 15, Color.white,
                new Vector2(0.06f, y), new Vector2(0.39f, y + RowHeight), TextAnchor.MiddleLeft);
            var slider = CreateSlider(parent, new Vector2(0.42f, y + 0.005f),
                new Vector2(0.94f, y + RowHeight - 0.005f), value);
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged));
            return slider;
        }

        private static float RowY(int row)
        {
            return 0.835f - row * 0.062f;
        }

        private static Vector2 SettingMinimum(int row, float x = 0.42f)
        {
            return new Vector2(x, RowY(row));
        }

        private static Vector2 SettingMaximum(int row, float x = 0.94f)
        {
            return new Vector2(x, RowY(row) + RowHeight);
        }

        private void CreateSettingLabel(Transform parent, string label, int row)
        {
            CreateText(parent, label, 15, Color.white,
                new Vector2(0.06f, RowY(row)), new Vector2(0.39f, RowY(row) + RowHeight),
                TextAnchor.MiddleLeft);
        }

        private RoboClashOptionSelector CreateSettingDropdown(Transform parent, int row)
        {
            return CreateSettingDropdown(parent, SettingMinimum(row), SettingMaximum(row));
        }

        private RoboClashOptionSelector CreateSettingDropdown(Transform parent,
            Vector2 minimum, Vector2 maximum)
        {
            var selectorObject = CreateObject("Settings Option Selector", parent);
            Place(selectorObject.GetComponent<RectTransform>(), minimum, maximum);
            return RoboClashOptionSelector.Attach(selectorObject, font, root.transform);
        }

        private static int NearestIndex(int[] values, int target)
        {
            var best = 0;
            for (var i = 1; i < values.Length; i++)
            {
                if (Mathf.Abs(values[i] - target) < Mathf.Abs(values[best] - target))
                    best = i;
            }
            return best;
        }

        private static int NearestIndex(float[] values, float target)
        {
            var best = 0;
            for (var i = 1; i < values.Length; i++)
            {
                if (Mathf.Abs(values[i] - target) < Mathf.Abs(values[best] - target))
                    best = i;
            }
            return best;
        }

        private Slider CreateSlider(Transform parent, Vector2 minimum, Vector2 maximum, float value)
        {
            var sliderObject = CreateObject("Volume Slider", parent, typeof(Slider));
            Place(sliderObject.GetComponent<RectTransform>(), minimum, maximum);
            var background = CreatePanel(sliderObject.transform, "Track",
                new Color(0.07f, 0.14f, 0.2f), Vector2.zero, Vector2.one);
            var fill = CreatePanel(background.transform, "Fill", Blue,
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
            var check = CreatePanel(background.transform, "Check", Orange,
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(changedValue => changed(changedValue));
            return toggle;
        }

        private void PopulateResolutionOptions(int selectedWidth, int selectedHeight)
        {
            availableResolutions.Clear();
            var native = RoboClashSaveService.NativeResolution;
            availableResolutions.Add(native);

            var resolutions = new List<Vector2Int>();
            foreach (var resolution in Screen.resolutions)
            {
                var candidate = new Vector2Int(resolution.width, resolution.height);
                if (!RoboClashSaveService.IsValidSavedResolution(candidate.x, candidate.y) ||
                    candidate == native || resolutions.Contains(candidate))
                    continue;

                resolutions.Add(candidate);
            }

            resolutions.Sort((left, right) =>
            {
                var areaComparison = ((long)right.x * right.y).CompareTo((long)left.x * left.y);
                if (areaComparison != 0)
                    return areaComparison;
                var widthComparison = right.x.CompareTo(left.x);
                return widthComparison != 0 ? widthComparison : right.y.CompareTo(left.y);
            });
            availableResolutions.AddRange(resolutions);

            var labels = new List<string>(availableResolutions.Count);
            for (var i = 0; i < availableResolutions.Count; i++)
            {
                var resolution = availableResolutions[i];
                labels.Add(i == 0
                    ? $"NATIVE ({resolution.x} x {resolution.y})"
                    : $"{resolution.x} x {resolution.y}");
            }

            var selectedIndex = availableResolutions.IndexOf(
                new Vector2Int(selectedWidth, selectedHeight));
            if (selectedIndex < 0)
                selectedIndex = 0;
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(selectedIndex);
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
            var activeQuality = QualitySettings.GetQualityLevel();
            var selected = selectedQuality >= 0 && selectedQuality < names.Length
                ? selectedQuality
                : activeQuality;
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, names.Length - 1));
        }

        private void ApplyResolution(int index)
        {
            if (refreshing || index < 0 || index >= availableResolutions.Count)
                return;
            var resolution = availableResolutions[index];
            if (resolution.x == Screen.width && resolution.y == Screen.height)
                return;

#if UNITY_EDITOR
            Debug.LogWarning("Resolution changes only take effect in a standalone build.");
#endif
            previousResolutionWidth = Screen.width;
            previousResolutionHeight = Screen.height;
            previousFullscreen = Screen.fullScreen;
            RoboClashSaveService.SetResolution(resolution.x, resolution.y);
            resolutionChangePending = true;
            resolutionConfirmationSeconds = 10f;
            lastConfirmationDisplayedSecond = 10;
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

        private void ApplyQualityLevel(int index)
        {
            if (refreshing)
                return;
            RoboClashSaveService.SetQualityLevel(index);
        }

        private void ApplyFrameRate(int index)
        {
            if (refreshing)
                return;

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
                    RoboClashSession.SaveCameraMode(RoboClashCameraMode.FirstPerson);
                    RoboClashSession.SaveFieldOfView(80);
                    RoboClashSession.SaveMouseSensitivity(0.12f);
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
            RoboClashRobotAudio.RoboClashAudioManager.AddButtonAudio(button);
            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = Color.white;
            button.targetGraphic = buttonImage;
            var colors = button.colors;
            colors.normalColor = ControlColor;
            colors.highlightedColor = new Color(0.12f, 0.23f, 0.35f, 1f);
            colors.pressedColor = Orange;
            colors.selectedColor = Orange;
            colors.disabledColor = new Color(0.08f, 0.12f, 0.17f, 0.65f);
            button.colors = colors;
            CreateText(buttonObject.transform, label, 14, Color.white,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, important: true);
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 minimum, Vector2 maximum)
        {
            var panel = CreateObject(name, parent);
            Place(panel.GetComponent<RectTransform>(), minimum, maximum);
            var image = panel.AddComponent<Image>();
            image.color = color;
            RoboClashUiKit.StyleRoundedPanel(image);
            return panel;
        }

        private TextMeshProUGUI CreateText(Transform parent, string value, int size, Color color,
            Vector2 minimum, Vector2 maximum, TextAnchor alignment, string objectName = "Text",
            bool important = false)
        {
            var textObject = CreateObject(objectName, parent, typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Place(textObject.GetComponent<RectTransform>(), minimum, maximum);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.color = color;
            text.alignment = RoboClashUiKit.ToTextAlignmentOptions(alignment);
            text.raycastTarget = false;
            RoboClashUiKit.StyleText(text, size, important);
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
