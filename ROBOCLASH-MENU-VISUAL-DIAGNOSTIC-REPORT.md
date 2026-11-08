# RoboClash Main Menu Visual Diagnostic Report

**Scope:** static source, Unity YAML, prefab/material, and texture-import inspection, plus a compile check of the temporary F10 diagnostic component. No existing project file was edited for this report. Runtime-only observations are labeled **UNVERIFIED**; the menu was not entered into Play Mode in this pass.

**Project:** Unity 6000.6.4f1, URP, UGUI legacy `Text`, new Input System.

## 1. Canvas and scaling

### Verbatim creation block

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 386–420 (`BuildInterface()`).

```csharp
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

            RoboClashMenuBackdrop.Create(canvas.transform);
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
            phoneStatusText = AddText(canvas.transform, RoboClashPhoneInputServer.StatusText,
                11, MutedColor, TextAnchor.LowerRight,
                new Vector2(0.66f, 0.012f), new Vector2(0.99f, 0.05f),
                new Vector2(0f, 0f), new Vector2(-12f, 0f));
            AddTextShadow(phoneStatusText, new Color(0f, 0f, 0f, 0.8f),
                new Vector2(1.5f, -1.5f));
        }
```

### Serialized/code values and effective font size

- Canvas: `ScreenSpaceOverlay`, sorting order `0`. The Canvas is made at runtime; no scene-serialized Canvas controls this menu.
- CanvasScaler: `ScaleWithScreenSize`, reference resolution `(1600, 900)`, `matchWidthOrHeight = 0.5`.
- The code does **not** explicitly assign `screenMatchMode`, `referencePixelsPerUnit`, `pixelPerfect`, `additionalShaderChannels`, or `worldCamera`. For a newly added Canvas/CanvasScaler in this Unity version the usual component defaults are Match Width Or Height, 100 reference pixels per unit, false, None, and null respectively; those are **UNVERIFIED at runtime** because the temporary dump does not print all of them. `worldCamera` is not needed for `ScreenSpaceOverlay`.
- Scale formula for Match Width Or Height:

  `scale = (screenWidth / 1600)^(1 - match) × (screenHeight / 900)^match`

  With `match = 0.5`, both listed axes are 16:9, so the horizontal and vertical ratios match.

| Screen | Width ratio | Height ratio | Canvas scale | 17 | 20 | 58 | 72 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1280×720 | 0.8 | 0.8 | 0.8 | 13.6 px | 16.0 px | 46.4 px | 57.6 px |
| 1600×900 | 1.0 | 1.0 | 1.0 | 17.0 px | 20.0 px | 58.0 px | 72.0 px |
| 1920×1080 | 1.2 | 1.2 | 1.2 | 20.4 px | 24.0 px | 69.6 px | 86.4 px |
| 2560×1440 | 1.6 | 1.6 | 1.6 | 27.2 px | 32.0 px | 92.8 px | 115.2 px |

These are scaled UGUI font-size units, not exact glyph cap-heights. The title is size 76, so its 1920×1080 effective size is 91.2 units.

**Observation:** at 1280×720, the smaller 11–17 reference-size labels scale to 8.8–13.6 units. The user-reported Game view scale of 1.3× and small Free Aspect preview are external to the project files; that additional presentation resampling can magnify softness and reduce apparent UI size. The report does not independently verify that Editor setting.

## 2. Text rendering

### Verbatim creation and main-menu text blocks

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 630–675 (`BuildMainScreen()`).

```csharp
        private void BuildMainScreen()
        {
            var title = AddText(mainScreen.transform, "ROBOCLASH", 76, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0.075f, 0.77f), new Vector2(0.53f, 0.94f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            AddTextShadow(title, new Color(OrangeColor.r, OrangeColor.g, OrangeColor.b, 0.5f),
                new Vector2(0f, -4f));
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
            AddTextShadow(subtitle, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
            AddTextShadow(tagline, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
            AddTextShadow(featuredRobotName, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
            AddTextShadow(selectedRobotText, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
            AddTextShadow(careerSummaryText, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
        }
```

Source: same file, lines 1683–1737 (`AddText`, shadow, and panel/layout helpers).

```csharp
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

        private static void AddTextShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null)
                return;

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
```

`font` is loaded from Unity's built-in `LegacyRuntime.ttf` in `BuildInterface()` above. `AddText` explicitly sets only font, content, font size, color, alignment, horizontal wrap, vertical overflow, and `raycastTarget = false`. It does not set `resizeTextForBestFit`, `alignByGeometry`, `supportRichText`, `lineSpacing`, `fontStyle`, or `material`. `fontStyle` is changed to Bold for the title, button labels, and chevrons; other main-menu text uses the generated `Text` component's default Normal. No text material override is assigned. The actual omitted Text values are component defaults, not values assigned by menu code.

### Color constants

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 134–139.

```csharp
        private static readonly Color PanelColor = new Color(0.03f, 0.06f, 0.11f, 0.72f);
        private static readonly Color BlueColor = new Color(0.30f, 0.70f, 1f);
        private static readonly Color OrangeColor = new Color(1f, 0.58f, 0.16f);
        private static readonly Color GlassHotColor = new Color(0.08f, 0.16f, 0.26f, 0.88f);
        private static readonly Color OrangeHotColor = new Color(1f, 0.72f, 0.35f);
        private static readonly Color MutedColor = new Color(0.62f, 0.72f, 0.82f);
```

`Color.white` is `(1,1,1,1)`. The default selected robot name gets the robot definition's primary color, not white; Titan's is `(0.84313726, 0.09803922, 0.19607843, 1)` (`#D71932`), as verified in `Assets/RoboClash/Generated/Materials/RC_Roster_Titan_Primary.mat` and its prefab.

### Main-menu text inventory at 1920×1080

At this resolution, Canvas scale is 1.2. Coordinates below are the actual code's normalized anchors and offsets; effective size is `fontSize × 1.2`. The screen-root parent is `Main Menu`; button labels and chevrons are children of their named button; featured text is child of `Featured Robot`; phone status is a direct Canvas child.

| Content | Font size / effective | RGBA / style | Anchor min → max; offsets min → max | Alignment | Parent |
|---|---:|---|---|---|---|
| `ROBOCLASH` | 76 / 91.2 px | white `(1,1,1,1)`, Bold | `(0.075,0.77)→(0.53,0.94)`; `(0,0)→(0,0)` | MiddleLeft | Main Menu |
| `MECH ARENA  //  SINGLE PLAYER` | 16 / 19.2 px | Blue `(0.30,0.70,1,1)`, Normal | `(0.08,0.72)→(0.51,0.78)`; `(0,0)→(0,0)` | MiddleLeft | Main Menu |
| `ENTER THE ARENA. ADAPT. ELIMINATE.` | 17 / 20.4 px | Muted `(0.62,0.72,0.82,1)`, Normal | `(0.08,0.65)→(0.51,0.71)`; `(0,0)→(0,0)` | MiddleLeft | Main Menu |
| `PLAY` | 17 / 20.4 px | dark `(0.06,0.04,0.02,1)`, Bold | `(0,0)→(1,1)`; `(28,0)→(0,0)` | MiddleLeft | PLAY Button |
| `ROBOT GARAGE` | 17 / 20.4 px | white `(1,1,1,1)`, Bold | `(0,0)→(1,1)`; `(28,0)→(0,0)` | MiddleLeft | ROBOT GARAGE Button |
| `SETTINGS` | 17 / 20.4 px | white `(1,1,1,1)`, Bold | `(0,0)→(1,1)`; `(28,0)→(0,0)` | MiddleLeft | SETTINGS Button |
| `CAREER / STATISTICS` | 17 / 20.4 px | white `(1,1,1,1)`, Bold | `(0,0)→(1,1)`; `(28,0)→(0,0)` | MiddleLeft | CAREER / STATISTICS Button |
| `QUIT` | 17 / 20.4 px | white `(1,1,1,1)`, Bold | `(0,0)→(1,1)`; `(28,0)→(0,0)` | MiddleLeft | QUIT Button |
| `>` (one per button) | 22 / 26.4 px | PLAY dark; other buttons Orange `(1,0.58,0.16,1)`, Bold | `(0,0)→(1,1)`; `(0,0)→(-18,0)` | MiddleRight | Each button |
| `TITAN` (if Titan is selected) | 27 / 32.4 px | Titan primary `(0.84313726,0.09803922,0.19607843,1)`, Normal | `(0.06,0.48)→(0.94,0.90)`; `(0,0)→(0,0)` | MiddleLeft | Featured Robot |
| `CURRENT ROBOT  //  TITAN` (if Titan is selected) | 13 / 15.6 px | Muted `(0.62,0.72,0.82,1)`, Normal | `(0.06,0.08)→(0.94,0.48)`; `(0,0)→(0,0)` | MiddleLeft | Featured Robot |
| Four dynamic career summary lines | 12 / 14.4 px | Muted `(0.62,0.72,0.82,1)`, Normal | `(0.08,0.01)→(0.48,0.065)`; `(0,0)→(0,0)` | LowerLeft | Main Menu |
| `PHONE: CONNECTED` or `PHONE: WAITING  USB ADB REVERSE REQUIRED` | 11 / 13.2 px | Blue when active; Muted otherwise | `(0.66,0.012)→(0.99,0.05)`; `(0,0)→(-12,0)` | LowerRight | RoboClash Menu Canvas |

The selected-robot content is dynamic (`RobotTypes[selectedRobotIndex]`), so Titan is not guaranteed by static code for every saved run. `RefreshCareerSummary()` produces:

```csharp
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
```

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 1558–1573 (`RefreshCareerSummary()`).

The phone label reads `RoboClashPhoneInputServer.StatusText` (`Assets/Scripts/RoboClash/RoboClashPhoneInputServer.cs:97–99`); its live string is one of the two listed values, and its live color depends on phone-server state. Which value is active at runtime is **UNVERIFIED** without Play Mode.

### Shadows and outlines

The menu code adds `Shadow` components, no `Outline` components. Targets, effect color, distance:

| Target | Effect color | Effect distance | `useGraphicAlpha` |
|---|---|---|---|
| ROBOCLASH title | Orange RGB, alpha 0.5 | `(0,-4)` | `true` (UGUI `Shadow` serialized default; never changed by menu code) |
| Subtitle | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Tagline | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Featured robot name | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Current robot text | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Career summary | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Every button label | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |
| Phone status | black `(0,0,0,0.8)` | `(1.5,-1.5)` | `true` (UGUI default) |

`useGraphicAlpha = true` is the default initializer in Unity 6.6's built-in `com.unity.ugui/Runtime/UGUI/UI/Core/VertexModifiers/Shadow.cs`, line 21; the menu code does not override it. No `Shadow` or `Outline` is attached to a menu `Image`. `Outline`: **NOT FOUND** in the inspected menu source. In particular, the subtitle is created once; a duplicate subtitle: **NOT FOUND** in `BuildMainScreen()`. The reported blue double-underline artifact is **NOT EXPLAINED by the inspected menu code**. The one shadow is not blue, and the orange title rule is a separate short line above the subtitle.

### WCAG contrast estimates

Ratios use WCAG 2.x sRGB relative luminance, the literal source colors, and representative sampled raw artwork pixels. Samples from the 1672×941 image were: sky orange `#C8654F` at `(1280,260)`, dark foreground `#0A0912` at `(1600,650)`, blue light `#2359CA` at `(976,180)`, deep sky `#011430` at `(100,100)`, and sunlight `#FCF6EC` at `(1285,420)`. For the glass-panel comparison, `PanelColor` (72% alpha) was composited over assumed mid-dark `#202634`, yielding approximately `#0E1623`. Raw-art samples are not composited with menu shades, so these are diagnostic samples, not exact per-glyph on-screen ratios.

| Text color | On glass over `#202634` | Sky orange `#C8654F` | Dark foreground `#0A0912` | Blue light `#2359CA` | Deep sky `#011430` | Sunlight `#FCF6EC` | Below 4.5:1 samples |
|---|---:|---:|---:|---:|---:|---:|---|
| White `(1,1,1)` | 18.17 | 3.87 | 19.80 | 6.26 | 18.36 | 1.08 | Sky orange, sunlight |
| Muted `(0.62,0.72,0.82)` | 8.83 | 1.88 | 9.62 | 3.04 | 8.92 | 1.91 | Sky orange, blue light, sunlight |
| Blue `(0.30,0.70,1)` | 7.93 | 1.69 | 8.64 | 2.73 | 8.01 | 2.13 | Sky orange, blue light, sunlight |
| Orange `(1,0.58,0.16)` | 8.23 | 1.75 | 8.97 | 2.84 | 8.32 | 2.05 | Sky orange, blue light, sunlight |
| PLAY dark `(0.06,0.04,0.02)` | Not applicable (PLAY is orange) | 5.09 | 1.01 | 3.14 | 1.07 | 18.30 | Dark/blue/deep samples are not PLAY's background |
| Titan red `(0.843,0.098,0.196)` | 3.53 | 1.33 | 3.84 | 1.22 | 3.56 | 4.79 | Glass composite, sky orange, dark, blue, deep sky |

Text color contrast varies across the artwork. The glass panel improves most light text, but Titan red on the glass estimate is below 4.5:1. The PLAY label is high contrast against its own orange fill: approximately 8.91:1 on idle Orange and 11.45:1 on OrangeHot.

## 3. Backdrop image

### Verbatim `RoboClashMenuBackdrop.cs`

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuBackdrop.cs`, lines 1–151 (entire file).

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashMenuBackdrop : MonoBehaviour
    {
        public const string ResourceFolder = "Menu";

        private static Sprite horizontalGradient;
        private static Sprite verticalGradient;
        private static bool missingArtworkWarningLogged;
        private RectTransform artworkDrift;

        public static RoboClashMenuBackdrop Create(Transform canvas)
        {
            if (canvas == null)
            {
                Debug.LogError("Cannot create the RoboClash menu backdrop without a Canvas.");
                return null;
            }

            var gameObject = new GameObject("Menu Backdrop",
                typeof(RectTransform), typeof(RectMask2D));
            gameObject.transform.SetParent(canvas, false);
            gameObject.transform.SetAsFirstSibling();
            Stretch((RectTransform)gameObject.transform);

            var backdrop = gameObject.AddComponent<RoboClashMenuBackdrop>();
            backdrop.Build();
            return backdrop;
        }

        private void Build()
        {
            var baseImage = CreateImage("Base", transform,
                new Color(0.018f, 0.035f, 0.065f), null);
            Stretch(baseImage.rectTransform);

            var sprites = Resources.LoadAll<Sprite>(ResourceFolder);
            var sprite = sprites.Length > 0 ? sprites[0] : null;
            if (sprite != null)
            {
                var driftObject = new GameObject("Art Drift", typeof(RectTransform));
                driftObject.transform.SetParent(transform, false);
                artworkDrift = (RectTransform)driftObject.transform;
                Stretch(artworkDrift);

                var artwork = CreateImage("Citadel Artwork", artworkDrift, Color.white, sprite);
                var fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
            else if (!missingArtworkWarningLogged)
            {
                missingArtworkWarningLogged = true;
                Debug.LogWarning(
                    "RoboClash menu artwork was not found. Add a Sprite to Assets/Resources/Menu; using the navy fallback.");
            }

            var leftShade = CreateImage("Left Readability Shade", transform,
                new Color(0.01f, 0.02f, 0.05f, 0.92f), GetHorizontalGradient());
            SetRect(leftShade.rectTransform, Vector2.zero, new Vector2(0.62f, 1f));

            var bottomShade = CreateImage("Bottom Readability Shade", transform,
                new Color(0.01f, 0.02f, 0.05f, 0.85f), GetVerticalGradient());
            SetRect(bottomShade.rectTransform, Vector2.zero, new Vector2(1f, 0.35f));
        }

        private void Update()
        {
            if (artworkDrift == null)
                return;

            var time = Time.unscaledTime;
            var scale = 1.06f + 0.015f * Mathf.Sin(time * 0.15f);
            artworkDrift.localScale = new Vector3(scale, scale, 1f);
            artworkDrift.anchoredPosition = new Vector2(
                Mathf.Sin(time * 0.11f) * 14f,
                Mathf.Cos(time * 0.09f) * 8f);
        }

        private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var gameObject = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite GetHorizontalGradient()
        {
            if (horizontalGradient != null)
                return horizontalGradient;

            var texture = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (var x = 0; x < 256; x++)
            {
                var alpha = Mathf.Pow(1f - x / 255f, 1.5f);
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            horizontalGradient = Sprite.Create(texture, new Rect(0f, 0f, 256f, 1f),
                new Vector2(0.5f, 0.5f), 100f);
            return horizontalGradient;
        }

        private static Sprite GetVerticalGradient()
        {
            if (verticalGradient != null)
                return verticalGradient;

            var texture = new Texture2D(1, 256, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < 256; y++)
            {
                var alpha = Mathf.Pow(1f - y / 255f, 1.5f);
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            verticalGradient = Sprite.Create(texture, new Rect(0f, 0f, 1f, 256f),
                new Vector2(0.5f, 0.5f), 100f);
            return verticalGradient;
        }
    }
}
```

### Loaded files, dimensions, import settings

Files under `Assets/Resources/Menu/`:

- `Mech Overlooking the Sunset Citadel.png`
- `Mech Overlooking the Sunset Citadel.png.meta`

Files under `Assets/Image/`:

- `Mech Overlooking the Sunset Citadel.png`
- `Mech Overlooking the Sunset Citadel.png.meta`

The Resources folder currently contains exactly one PNG/Sprite, named `Mech Overlooking the Sunset Citadel_0` after import. Therefore `Resources.LoadAll<Sprite>("Menu")[0]` resolves to the intended artwork in the current project; the first-element ambiguity would matter only if more sprites were later added. The imported sprite/texture dimensions are 1672×941 px. The source in `Assets/Image/` has separate importer settings and is not the runtime Sprite asset.

Runtime copy metadata: `Assets/Resources/Menu/Mech Overlooking the Sunset Citadel.png.meta`, lines 10–62 and 76–98.

Verbatim importer settings from that `.meta` file, lines 10–62 and 76–98:

```yaml
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 50
  spriteMode: 1
  alphaIsTransparency: 1
  textureType: 8
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 2
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
```

| Setting | Runtime resource value |
|---|---|
| `textureType` | `8` = Sprite |
| `spriteMode` | `1` = Single |
| `maxTextureSize` | 2048 |
| `textureCompression` | Default platform value `2` (CompressedHQ); Standalone entry value `1` but `overridden: 0`, so not an active explicit override |
| `crunchedCompression` | 0 |
| `mipmapEnabled` | 0 |
| `filterMode` | 1 = Bilinear |
| `sRGBTexture` | 1 |
| `alphaIsTransparency` | 1 |
| `nPOTScale` | 0 = None |
| Standalone | max 2048, texture format `-1` (platform default), compression setting 1, crunched 0, override 0 |

The image is smaller than both requested screen sizes. The Sprite's 1672×941 aspect is 1.7779; `EnvelopeParent` fits a 16:9 (1.7778) canvas to approximately 1920×1080, subject to fractional rounding/crop. Base width/height upscale at 1920×1080 is approximately `1920/1672 = 1.148×` and `1080/941 = 1.148×`; at 3840×2160 it is approximately 2.297×. `artworkDrift` multiplies this by a time-varying 1.06–1.075:

| Screen | Base upscale | With drift zoom |
|---|---:|---:|
| 1920×1080 | 1.148× | 1.218×–1.234× |
| 3840×2160 | 2.297× | 2.434×–2.469× |

The whole backdrop has a `RectMask2D`; the artwork is scaled and offset within it and is clipped at the screen boundary. The left and bottom gradients are sibling Images outside the drifted artwork and are not clipped by the artwork's own bounds.

The menu background import rule is in `Assets/Editor/RoboClashMenuBackgroundImporter.cs`, lines 1–20. It sets Sprite/Single, mipmaps off, max size 2048, CompressedHQ, Bilinear. The PNG source file itself is 1672×941. CompressedHQ is not a lossless setting, but no crunch is enabled.

### Generated gradient sprites

Both gradients are created at runtime in `RoboClashMenuBackdrop.cs`:

| Sprite | Texture | Bit depth/format | Filter/wrap | RGB and alpha curve | Image tint |
|---|---|---|---|---|---|
| Horizontal | 256×1 | RGBA32 (8 bits/channel) | Bilinear / Clamp | white RGB; `alpha = pow(1 - x/255, 1.5)` | `(0.01,0.02,0.05,0.92)` |
| Vertical | 1×256 | RGBA32 (8 bits/channel) | Bilinear / Clamp | white RGB; `alpha = pow(1 - y/255, 1.5)` | `(0.01,0.02,0.05,0.85)` |

No separate PNG or alpha-curve asset exists for these sprites. Their one-dimensional 8-bit alpha is bilinearly stretched across large areas (the horizontal ramp spans 62% of screen width; the vertical ramp spans 35% of screen height). At 1920×1080, that is roughly 4.65 screen pixels per horizontal alpha sample and 1.48 px per vertical sample. Quantization plus long, smooth dark gradients is a plausible banding source; actual visible banding was not reproduced in Play Mode.

## 4. Layer stack and panel transparency

`RoboClashMenuBackdrop.Create()` calls `SetAsFirstSibling()`. `BuildInterface()` then creates screen roots in order, followed by `phoneStatusText` directly on the Canvas. The `Main Menu` root is the first screen root and is activated by the controller; other screens are inactive. Later siblings draw above earlier siblings.

Main-menu draw order, back-to-front (the reverse list is top-to-bottom/front-to-back):

1. **Backdrop Base** — solid `(0.018,0.035,0.065,1)`.
2. **Citadel Artwork** — White tint `(1,1,1,1)`; `AspectRatioFitter.EnvelopeParent`; drift scale 1.06–1.075.
3. **Left Readability Shade** — RGB `(0.01,0.02,0.05)`, max alpha 0.92 multiplied by horizontal 8-bit gradient.
4. **Bottom Readability Shade** — RGB `(0.01,0.02,0.05)`, max alpha 0.85 multiplied by vertical 8-bit gradient.
5. **Main Menu root Image** — `Color.clear`; root CanvasGroup fades from alpha 0 to 1 over 0.3 seconds. The main menu gets no `Readability Dim` child; `CreateScreen` creates that dim only when `screenName != "Main Menu"`.
6. **Blue Accent** — Blue `(0.30,0.70,1)` at alpha 0.4, a 2-reference-unit top rule.
7. **Hazard Strip** — white tint at alpha 0.85 over a generated 16×16 yellow/near-black texture, Image type `Tiled`, height 10 reference units at the bottom.
8. **ROBOCLASH/title rule/subtitle/tagline**, then **preview RawImage**, then the five buttons, then the **Featured Robot** panel and its text, then the career summary. These are created in that order in `BuildMainScreen()`. The featured panel is later than and overlaps the preview's lower-right rectangle, so it draws over that part of the preview.
9. **Phone status Text** — direct Canvas child created after the screens; it draws above them.

Accordingly, the explicit top-to-bottom/front-to-back order is: phone status; career summary; Featured Robot panel/text; buttons; preview RawImage; tagline; subtitle; title/title rule; hazard strip and blue accent; Main Menu root; bottom and left shade Images; drifted artwork; opaque Base.

Text effects do not add independent full-panel images: each `Shadow` is a text mesh effect. The title rule is a solid orange Image; button accent bars are white on PLAY and Blue on the others.

At 1920×1080, a representative horizontal shade alpha at button-center x=0.22 is about 0.48: `0.92 × (1 - 0.22/0.62)^1.5`. The bottom-shade alpha at the vertical centers of PLAY/ROBOT GARAGE/SETTINGS is approximately zero; CAREER is approximately 0.18 and QUIT approximately 0.47. Combined shade opacity is `1 - (1-leftAlpha)(1-bottomAlpha)`, approximately 0.48 for the upper three, 0.57 for CAREER, and 0.72 for QUIT. The panel itself is then composited in front at 0.72 alpha (selected/hovered glass is 0.88). This approximates the alpha stack; the artwork colors and final display transfer are spatially variable.

| Button center | Left shade | Bottom shade | Combined shade behind panel | Panel alpha |
|---|---:|---:|---:|---:|
| PLAY | 0.48 | ~0.00 | 0.48 | 0.72 idle orange; hot orange is opaque |
| ROBOT GARAGE | 0.48 | ~0.00 | 0.48 | 0.72 |
| SETTINGS | 0.48 | ~0.00 | 0.48 | 0.72 |
| CAREER / STATISTICS | 0.48 | 0.18 | 0.57 | 0.72 |
| QUIT | 0.48 | 0.47 | 0.72 | 0.72 |

The main-screen banding candidates evidenced in source are the two large RGBA32 gradient Images and their alpha compositing under semi-transparent panel Images. The hazard texture is a separate, explicit tiled diagonal stripe pattern at the bottom; it is not positioned behind most panel surfaces but touches the lowest career-text area. `RectMask2D` is present on the backdrop root. `Mask`: **NOT FOUND** in main-menu construction. Dropdown Masks/scroll view masks belong to other screens, which are inactive on the main menu. The static source identifies plausible stripe/banding contributors, but the screenshot symptom itself remains **UNVERIFIED**.

Verbatim relevant layer and stripe code: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 573–628.

```csharp
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
```

The screen fade component is a separate runtime layer effect. Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuScreenFade.cs`, lines 1–35 (entire file).

```csharp
using UnityEngine;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class RoboClashMenuScreenFade : MonoBehaviour
    {
        private const float FadeDuration = 0.3f;
        private CanvasGroup canvasGroup;
        private float progress = 1f;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            progress = 0f;
            canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (progress >= 1f)
                return;

            progress = Mathf.Min(1f, progress + Time.unscaledDeltaTime / FadeDuration);
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
        }
    }
}
```

## 5. Buttons

### Verbatim `CreateButton()` and main-screen call sites

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 649–658.

```csharp
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
```

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 1589–1620 (`CreateButton()`).

```csharp
        private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Color color, Action action, bool primary = false)
        {
            var idleColor = primary ? OrangeColor : PanelColor;
            var hotColor = primary ? OrangeHotColor : GlassHotColor;
            var buttonObject = CreatePanel(label + " Button", parent, idleColor);
            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;
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
                Vector2.zero, Vector2.one, new Vector2(28f, 0f), Vector2.zero);
            buttonText.fontStyle = FontStyle.Bold;
            AddTextShadow(buttonText, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
            var chevron = AddText(buttonObject.transform, ">", 22,
                primary ? labelColor : OrangeColor, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18f, 0f));
            chevron.fontStyle = FontStyle.Bold;
            var fx = buttonObject.AddComponent<RoboClashMenuButtonFx>();
            fx.Initialize(image, bar.GetComponent<Image>(), buttonText.rectTransform,
                chevron, idleColor, hotColor, primary);
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }
```

Button rects are 28% of screen width × 9% of screen height: 537.6×97.2 px each at 1920×1080. Their anchor ranges are listed above. The `color` parameter is not read in the shown method; actual idle/hot colors are selected from `primary`.

### Verbatim `RoboClashMenuButtonFx.cs`

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuButtonFx.cs`, lines 1–112 (entire file).

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashMenuButtonFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Image background;
        private Image accentBar;
        private RectTransform label;
        private RectTransform buttonRect;
        private Text chevron;
        private Color idleColor;
        private Color hotColor;
        private Color chevronColor;
        private bool primary;
        private bool hovering;
        private bool selected;
        private bool pressed;
        private bool lastPressed;
        private float transition;
        private float labelBaseX;
        private float labelBaseY;

        public void Initialize(Image backgroundImage, Image barImage, RectTransform labelRect,
            Text chevronText, Color idle, Color hot, bool isPrimary)
        {
            background = backgroundImage;
            accentBar = barImage;
            label = labelRect;
            chevron = chevronText;
            idleColor = idle;
            hotColor = hot;
            primary = isPrimary;
            buttonRect = (RectTransform)transform;
            if (label != null)
            {
                labelBaseX = label.anchoredPosition.x;
                labelBaseY = label.anchoredPosition.y;
            }
            chevronColor = chevron != null ? chevron.color : Color.white;
            ApplyVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData) => hovering = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            pressed = false;
        }

        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        private void OnDisable()
        {
            hovering = false;
            selected = false;
            pressed = false;
            transition = 0f;
            lastPressed = false;
            ApplyVisuals();
        }

        private void Update()
        {
            var target = hovering || selected ? 1f : 0f;
            if (Mathf.Approximately(transition, target) && lastPressed == pressed)
                return;

            transition = Mathf.MoveTowards(transition, target, Time.unscaledDeltaTime * 9f);
            lastPressed = pressed;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (background == null)
                return;

            background.color = Color.Lerp(idleColor, hotColor, transition);
            if (accentBar != null)
            {
                var barRect = accentBar.rectTransform;
                var size = barRect.sizeDelta;
                size.x = Mathf.Lerp(4f, primary ? 10f : 8f, transition);
                barRect.sizeDelta = size;
            }

            if (label != null)
                label.anchoredPosition = new Vector2(
                    labelBaseX + 10f * transition, labelBaseY);

            if (chevron != null)
                chevron.color = new Color(chevronColor.r, chevronColor.g,
                    chevronColor.b, chevronColor.a * transition);

            if (buttonRect != null)
            {
                var scale = pressed ? 0.98f : 1f + 0.015f * transition;
                buttonRect.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
```

`ShowMainMenu()` calls `SelectFirstSelectable(mainScreen)` after activating the main screen, and that method selects the first active, interactable `Selectable` found under the root. In the creation order, PLAY is the first Button. Therefore PLAY begins in the selected/hot state when an EventSystem is present; without an EventSystem, that specific selection behavior is not established.

| PLAY state | Background RGB | Label RGB | Approx. contrast |
|---|---|---|---:|
| Idle | Orange `(1,0.58,0.16)` | dark `(0.06,0.04,0.02)` | 8.91:1 |
| Hover or selected | OrangeHot `(1,0.72,0.35)` | same dark | 11.45:1 |

The initial selected fill is visibly lighter than idle because its green and blue channels increase. The label has good contrast against both fills; it is size 17 reference units (20.4 scaled units at 1920×1080), not white.

## 6. 3D robot preview

### Verbatim preview rig code

Source: `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 422–484.

```csharp
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
```

Source: same file, lines 486–558 (`UpdatePreviewRobot()`).

```csharp
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
```

### Static values and limits

- RenderTexture: 768×768, depth 24, `ARGB32`, anti-aliasing 2×. `filterMode`, `anisoLevel`, and `useMipMap` are not assigned in this method; their actual runtime values are **UNVERIFIED**. The F10 script prints them.
- Camera: SolidColor clear, transparent black background, culling mask `1 << 31` (preview layer 31), 30° perspective FOV; `allowHDR = false`, `allowMSAA = true`. `orthographic` is not assigned, so the newly added Camera's default is expected to be perspective; not separately measured in Play Mode. Position and rotation are not fixed in the creation method; `UpdatePreviewRobot()` computes position and `LookAt(target)` from renderer bounds. URP additional camera data is not added by this code.
- Three directional preview lights:

| Name | Color RGB | Intensity | Local position | Rotation | Range | Shadows | Culling mask |
|---|---|---:|---|---|---|---|---|
| Menu Preview Key Light | `(0.55,0.8,1)` | 1.2 | `(0,0,0)` | `(35,-32,0)` | Not relevant to Directional | None | layer 31 only |
| Menu Preview Rim Light | `(0.15,0.55,1)` | 0.8 | `(0,0,0)` | `(18,145,0)` | Not relevant to Directional | None | layer 31 only |
| Menu Preview Warm Rim Light | `(1,0.48,0.2)` | 0.55 | `(0,0,0)` | `(12,-145,0)` | Not relevant to Directional | None | layer 31 only |

Each light is parented to the menu controller with `SetParent(transform, false)` and receives no local-position assignment, so its local position remains the new Transform default `(0,0,0)`; its world position follows the controller root.

- Preview layer is initialized as integer 31 in the controller. The preview camera and lights use only that layer; the robot hierarchy is recursively assigned to it. Existing menu-scene lights have default all-layer culling masks in scene YAML, so their effect on the preview robot is not excluded by the code shown.
- RawImage: anchors `(0.50,0.08)` to `(0.98,0.90)`, zero offsets, white tint, default full UV rect (not set by code). At 1920×1080 it measures 921.6×885.6 px. Compared with 768×768 texture, the displayed area is 1.20× wider and 1.154× taller than the texture resolution; sampling/filtering is required.
- Robot framing: bounds are unioned across renderers; `distance = max(4, bounds.extents.magnitude × 2.8)`, camera offset is `(0.55d, 0.25 × extents.y, -d)`, then camera looks at the bounds center. Exact current prefab bounds and actual on-screen crop are **UNVERIFIED** until runtime. A 30° FOV with this distance can frame a bounding sphere tightly; actual mesh silhouette/projection cannot be derived from prefab dimensions alone.

### Titan materials and over-bright hypothesis

The Titan prefab references generated Primary, Secondary, Joints, Emission, and Shimmer materials. Their shader GUID `933532a4fcc9baf4fa0491de14d08ed7` resolves to Unity URP `Shaders/Lit.shader`. Primary Titan material: base color `#D71932`, metallic 0.62, smoothness 0.72, emission `(0,0,0,1)`. Secondary is `(0.1882353,0.20392157,0.23137255)`, Joints `(0.09411765,0.101960786,0.1254902)`, both metallic 0.62/smoothness 0.72 with no emission. Emission material has base `(1,0.23137255,0.15294118,1)`, metallic 0.62, smoothness 0.72, and HDR emission `(1.8,0.4164706,0.27529413,1.8)`. Shimmer uses the same orange-red RGB at alpha 0.16, metallic 0, smoothness 0.5, emission zero.

Verbatim selected material settings:

`Assets/RoboClash/Generated/Materials/RC_Roster_Titan_Primary.mat`, lines 104, 111, 121, 123:

```yaml
    - _Metallic: 0.62
    - _Smoothness: 0.72
    - _BaseColor: {r: 0.84313726, g: 0.09803922, b: 0.19607843, a: 1}
    - _EmissionColor: {r: 0, g: 0, b: 0, a: 1}
```

`Assets/RoboClash/Generated/Materials/RC_Roster_Titan_Emission.mat`, lines 24, 117, 124, 134, 136:

```yaml
  m_Shader: {fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}
    - _Metallic: 0.62
    - _Smoothness: 0.72
    - _BaseColor: {r: 1, g: 0.23137255, b: 0.15294118, a: 1}
    - _EmissionColor: {r: 1.8, g: 0.4164706, b: 0.27529413, a: 1.8}
```

The menu scene contains no Volume component, so its static scene does not provide a global Bloom/tonemapping profile. Its PC URP asset supports HDR, but this preview camera explicitly disables HDR. Accordingly, the gameplay scene's active Bloom settings cannot be attributed to the menu preview from scene YAML. The preview's high emission material and three directional lights are confirmed; the reported white rim exposure in the actual screenshot remains **UNVERIFIED**.

## 7. Featured-robot panel and career summary

The complete `BuildMainScreen()` excerpt in §2 shows the panel and career Text definitions. Relevant exact runtime assignments:

- Featured panel: `PanelColor = (0.03,0.06,0.11,0.72)`, anchors `(0.55,0.12)` to `(0.92,0.30)`. At 1920×1080: 710.4×194.4 px.
- Featured robot name: font 27, anchors `(0.06,0.48)` to `(0.94,0.90)` relative to panel; Titan changes its color to Titan `primaryColor` via `UpdateRobotDetails()`.
- Current-robot line: font 13, anchors `(0.06,0.08)` to `(0.94,0.48)` relative to panel; text is `CURRENT ROBOT  //  {selected type}`.
- Career block: font 12, anchors `(0.08,0.01)` to `(0.48,0.065)` relative to full screen; at 1920×1080 its rect is 768×59.4 px.
- `RefreshCareerSummary()` fills four lines with career level/XP, match record, eliminations/damage, and best wave/preferred robot. With `verticalOverflow = Overflow`, the four lines may draw beyond the 59.4 px rect depending on font metrics; exact glyph layout is **UNVERIFIED**. The career rect is near QUIT and the bottom hazard stripe: QUIT ends at anchor y=0.16, career top is y=0.065, so there is a 5.4 px normalized-screen gap between rects at 1080p; career begins at 10.8 px from bottom while the 10-reference-unit stripe is about 12 px high at scale 1.2, creating approximately 1.2 px of rect overlap with the stripe.
- The featured panel rectangle overlaps the preview RawImage rectangle (`x=.55–.92`, `y=.12–.30` is inside preview `x=.50–.98`, `y=.08–.90`) and is a later sibling, so it covers that portion.
- The Titan name's estimated 3.53:1 contrast on glass over mid-dark background is below 4.5:1. Career and current-robot text use MutedColor; their glass-composite contrast estimate is 8.83:1, but their raw-art contrast varies and the career size is only 14.4 scaled units at 1080p.

The full selection-driven color/content update is `Assets/Scripts/RoboClash/UI/RoboClashMenuController.cs`, lines 1024–1085:

```csharp
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
                var label = robotCardButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                    label.color = selected ? OrangeColor : Color.white;
            }
        }
```

The current robot content is set by this separate full method (same file, lines 1087–1092):

```csharp
        private void UpdateCurrentSelectionLabel()
        {
            if (selectedRobotText != null)
                selectedRobotText.text = "CURRENT ROBOT  //  " +
                                         RobotTypes[selectedRobotIndex].ToString().ToUpperInvariant();
        }
```

## 8. Rendering pipeline and scene

### Main Menu scene

`Assets/Scenes/RoboClashMainMenu.unity` contains one Camera (Main Camera) and six Light components. Global Volume in this scene: **NOT FOUND**. Main Camera serialized values: HDR enabled, MSAA allowed, perspective; its scene YAML has no `UniversalAdditionalCameraData` component or serialized `m_RenderPostProcessing` / `m_Antialiasing` fields. The runtime diagnostic script can report the effective URP camera data after Unity constructs the camera. Therefore the Main Camera's effective URP post-processing and antialiasing settings are **UNVERIFIED**, not inferred from missing YAML fields.

Verbatim Main Camera component from `Assets/Scenes/RoboClashMainMenu.unity`, lines 29177–29228:

```yaml
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1380248933}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 0
  orthographic size: 5
  m_Depth: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
```

Scene light values (local positions from their transforms):

| Light | Type | RGB | Intensity | Range | Shadows | Local position |
|---|---|---|---:|---:|---|---|
| Power Cyan Flood | Spot | `(0.03,0.65,1)` | 1400 | 28 | None | `(-42,4.5,0)` |
| Armory Blue Flood | Spot | `(0.12,0.35,1)` | 1400 | 28 | None | `(0,4.5,42)` |
| Hazard Amber Flood | Spot | `(1,0.3,0.05)` | 1400 | 28 | None | `(42,4.5,0)` |
| Large Arena Key Light | Directional | `(0.67,0.82,1)` | 1.15 | 10 serialized | Hard shadows | `(0,0,0)` |
| Large Hub Reactor Glow | Point | `(0.03,0.55,1)` | 850 | 14 | None | `(0,2.5,0)` |
| Workshop Red Flood | Spot | `(1,0.08,0.06)` | 1400 | 28 | None | `(0,4.5,-42)` |

All six scene lights have serialized culling mask bits `4294967295` (all layers). The directional light's serialized shadow type is 2; other listed lights use 0 (no shadows). The menu artwork itself is a full-screen UI image, so these 3D lights do not directly brighten the bitmap background.

### Gameplay comparison

`Assets/Scenes/RoboClashGameplay.unity` has a Global Volume and its Main Camera includes `UniversalAdditionalCameraData` with `m_RenderPostProcessing: 1`, antialiasing mode 2, quality 2. The attached profile `Global Volume (1) Profile` has active Color Adjustments (contrast 10 and saturation 10 overrides), Tonemapping (mode 2 override), and active Bloom (threshold 0.9 and intensity 0.4 overrides). This setup is not present in the Main Menu scene.

### Quality settings

`ProjectSettings/QualitySettings.asset`: levels are PC index 0 and Mobile index 1; serialized current quality is 0. The PC level has 4× `antiAliasing`, PC URP asset `Assets/Settings/PC_RPAsset.asset` has HDR on, MSAA 4, render scale 1, upscaling filter 0. The Mobile URP asset has HDR on, MSAA 1, render scale 0.8, upscaling filter 0; Mobile excludes Standalone target.

Verbatim quality settings excerpts: `ProjectSettings/QualitySettings.asset`, lines 7–28 and 65–80:

```yaml
  m_CurrentQuality: 0
  m_QualitySettings:
  - serializedVersion: 5
    name: PC
    pixelLightCount: 2
    shadows: 2
    shadowResolution: 1
    shadowProjection: 1
    shadowCascades: 2
    shadowDistance: 40
    shadowNearPlaneOffset: 3
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {x: 0.06666667, y: 0.2, z: 0.46666667}
    shadowmaskMode: 1
    skinWeights: 4
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 2
    antiAliasing: 4
    softParticles: 0
    softVegetation: 1
    realtimeReflectionProbes: 0
```

The Mobile level is a second quality level named `Mobile` at index 1; its asset reference and `excludedTargetPlatforms: - Standalone` are in the same file. The explicit PC URP settings are `Assets/Settings/PC_RPAsset.asset`, lines 23–30:

```yaml
  m_SupportsHDR: 1
  m_HDRColorBufferPrecision: 0
  m_MSAA: 4
  m_RenderScale: 1
  m_UpscalingFilter: 0
  m_FsrOverrideSharpness: 0
  m_FsrSharpness: 0.92
  m_EnableLODCrossFade: 1
```

The Mobile asset's equivalent values are `Assets/Settings/Mobile_RPAsset.asset`, lines 23–30: `m_SupportsHDR: 1`, `m_MSAA: 1`, `m_RenderScale: 0.8`, `m_UpscalingFilter: 0`.

`RoboClashMenuController.Awake()` calls `RoboClashSaveService.ApplySettings()` before `BuildInterface()`. That service applies the saved `qualityLevel` with `QualitySettings.SetQualityLevel(...)`; default settings choose the final entry in `QualitySettings.names`. Thus project/editor current quality is PC index 0, but an existing saved preference can override it on menu load. The user's persisted settings and actual active run quality are not available statically; the runtime dump will report the effective level.

Verbatim override method: `Assets/Scripts/RoboClash/RoboClashSaveService.cs`, lines 280–301.

```csharp
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
```

### Player Settings

`ProjectSettings/ProjectSettings.asset` lists default resolution 1024×768, fullscreen mode value 1, and active color space value 1 (Linear). The Windows graphics API list serializes API IDs `2` and `18`; an explicit setting called `Use DX11.1+` is **NOT FOUND**. That exact field name is not present in the inspected Player Settings.

Verbatim `ProjectSettings/ProjectSettings.asset` excerpts:

```yaml
  defaultScreenWidth: 1024
  defaultScreenHeight: 768
  defaultScreenWidthWeb: 960
  defaultScreenHeightWeb: 600
  m_StereoRenderingPath: 0
  m_ActiveColorSpace: 1
```

Source lines 116:

```yaml
  fullscreenMode: 1
```

Source lines 475–485:

```yaml
  m_BuildTargetGraphicsAPIs:
  - m_BuildTarget: WindowsStandaloneSupport
    m_APIs: 0200000012000000i
    m_Automatic: 0
```

The reported Unity Game view scale of 1.3× and Free Aspect/small-window state is user-provided context, not serialized project evidence. It can add display resampling and makes UI appear physically smaller; the requested re-test at 1× Full HD would remove that Editor presentation factor.

## 9. Hypotheses

| ID | Status | Evidence / limits |
|---|---|---|
| H1 Text is too small for its canvas scale and looks blurry at the current window size | **CONFIRMED** | Canvas scale is 0.8 at 1280×720; 11/12/13/16/17 reference sizes become 8.8/9.6/10.4/12.8/13.6 units there. At 1920×1080 they become 13.2/14.4/15.6/19.2/20.4. Actual screenshot glyph sharpness remains **UNVERIFIED**. |
| H2 Text has no dark plate or strong outline over a busy, bright background | **REFUTED** | The blanket claim is false: buttons and featured robot have semi-transparent dark plates; title, subtitle, and tagline do not have individual plates. The subtitle/tagline have a single black Shadow, not an Outline. Bright raw-art samples can still fall below 4.5:1, though left/bottom shades alter the on-screen background. |
| H3 Subtitle artifact is caused by a Shadow/Outline pair or duplicate Text | **REFUTED** | One subtitle `Text` is created and receives one black Shadow at `(1.5,-1.5)`; no Outline or duplicate subtitle exists in `BuildMainScreen()`. The reported blue artifact itself is not reproduced and remains unexplained. |
| H4 Backdrop is lower resolution, further upscaled by drift; compression/filter or wrong sprite contributes | **CONFIRMED** | 1672×941 source is upscaled 1.218–1.234× at 1080p including drift, with Bilinear filtering and CompressedHQ. Only one sprite exists in Resources/Menu and it is the expected artwork. A wrong resource sprite was NOT FOUND. Mipmaps/crunch are off. |
| H5 Horizontal panel banding comes from translucent stack and/or 8-bit gradients | **UNVERIFIED** | Large RGBA32 256×1/1×256 alpha gradients are bilinearly stretched under panels; glass alpha is 0.72 and selected glass 0.88. Quantization and compositing are evidenced as plausible sources, but the actual visible banding and causal relationship were not runtime-tested. |
| H6 Preview texture is small, has no MSAA, is stretched; lighting/material/bloom overexpose it; robot framing is too large and competes with artwork | **REFUTED** | RT is 768×768 and RawImage is 921.6×885.6 px at 1080p; there is 2× MSAA, so “no MSAA” is false. Its rectangle covers 48% of screen width and 82% of height over the backdrop, so visual competition is plausible but subjective. Titan has HDR emission `(1.8,0.416,0.275,1.8)` and three preview lights; preview camera HDR is false and menu scene has no Volume, so gameplay Bloom is not evidenced as a cause. Camera bounds framing is tight by formula; exact visible crop/overexposure remains **UNVERIFIED**. |
| H7 Featured robot and career texts have insufficient contrast and size | **CONFIRMED** | Titan red on estimated glass composite is 3.53:1. Career text is size 12 (14.4 scaled units at 1080p) in a 59.4 px-high rect; cramped placement is evidenced, but actual wrap/overlap and subjective legibility are **UNVERIFIED**. |
| H8 PLAY selected/hot is washed out and dark label too small | **CONFIRMED** | PLAY starts selected if EventSystem is present and transitions from Orange `(1,.58,.16)` to lighter OrangeHot `(1,.72,.35)`. Label is dark, size 17 (20.4 scaled units at 1080p), with estimated contrast 11.45:1 hot. “Too small” is subjective and not established by source alone. |
| H9 Menu lacks gameplay post-processing/quality or runs at lower quality | **CONFIRMED** | Main Menu has no Volume and no serialized URP camera-data component; gameplay has Global Volume, Bloom, Tonemapping, post-processing enabled, and AA mode 2. Project quality current is PC index 0, but whether saved settings lower the runtime quality is **UNVERIFIED**. |
| H10 Other sharpness/legibility/style factors | **CONFIRMED** | Legacy `Text` uses `LegacyRuntime.ttf`, not an SDF text asset; the canvas scales down at 1280×720; small status/career sizes, 8-bit gradients, artwork resampling, and the selected pale-orange state are independently evidenced. The 0.3 s screen fade can also affect captures taken during transition. Exact perceived effect remains runtime-dependent. |

## 10. Summary table

| Problem | Evidence (file:line or setting) | Severity | Likely fix area |
|---|---|---|---|
| Small supporting text at lower resolutions | `RoboClashMenuController.cs:414-418, 667-674`; CanvasScaler reference 1600×900, match 0.5 | High | Text sizing and canvas scale |
| Variable text contrast on bright artwork | `RoboClashMenuController.cs:640-674`; raw-art contrast samples in §2 | High | Text/backdrop treatment |
| Titan featured label red on translucent dark glass | `RoboClashMenuController.cs:660-665, 1024-1034`; Titan Primary `.mat:121`; 3.53:1 estimate | High | Featured-panel text color/contrast |
| Career block cramped against lower edge and hazard strip | `RoboClashMenuController.cs:667-674, 1558-1572`; `AddFullScreenAccent():615-628` | Med | Career block layout/size |
| Backdrop upscaling and drift zoom | `RoboClashMenuBackdrop.cs:35-55, 68-77`; image 1672×941 | Med | Backdrop source/import/display scale |
| Gradient banding risk beneath semi-transparent panels | `RoboClashMenuBackdrop.cs:117-150`; `RoboClashMenuController.cs:134, 1592-1595` | Med | Gradient/alpha composition |
| Selected PLAY fill is substantially lighter than idle orange | `RoboClashMenuButtonFx.cs:73-81`; OrangeHot `(1,.72,.35)` | Low | Button selected-state color |
| Preview is slightly enlarged beyond RT pixel dimensions | `RoboClashMenuController.cs:422-430, 473-484`; 1.20× horizontal / 1.154× vertical | Med | Preview render resolution/display rectangle |
| Preview emission and additive lights may exaggerate warm/red regions | Titan Emission `.mat:117,124,134,136`; `RoboClashMenuController.cs:452-470` | Med | Preview lighting/material emission |
| Main menu lacks gameplay scene volume post-processing | Main Menu scene: one Camera, zero Volumes; Gameplay camera/profile contain PP/Bloom | Med | Scene rendering setup |

## Part B. Temporary runtime dump

Added only `Assets/Scripts/RoboClash/UI/RoboClashMenuDiagnostics.cs`. It initializes in a debug build after scene load, listens for F10 using the new Input System, writes the hierarchy and render-state dump to `Application.persistentDataPath/menu-diagnostic.txt`, copies it to `GUIUtility.systemCopyBuffer`, and logs the output path. It reports active quality/URP, loaded sprites, volumes, cameras, lights, Canvas scale and active hierarchy, Images, Text values, Shadow components, CanvasGroup, Mask, and RectMask2D.

The code is compile-checked against the project's `Assembly-CSharp.csproj` with the temporary source included: **Build succeeded, 0 warnings, 0 errors**. The requested exact command `dotnet build Assembly-CSharp.csproj --no-restore` also completed: **Build succeeded, 0 warnings, 0 errors**.

The dump is not a full replacement for the static evidence in this report: it does not include camera rotation, material properties, all CanvasScaler fields, Text anchors/alignment/overflow/material, or `Shadow.useGraphicAlpha`. It also has not been executed in Play Mode here. Those run-only details and the screenshot's actual appearance remain **UNVERIFIED**.

### Sections not fully verified

- Play Mode visual result, actual currently selected robot, live phone-status string, runtime saved quality level, actual component defaults, preview renderer bounds/framing, and the reported artifacts were not runtime-observed.
- Actual physical-panel banding and screenshot-specific contrast cannot be concluded from static YAML/source alone; contrast table entries are explicitly representative estimates.
- `Use DX11.1+` is **NOT FOUND** as a Player Settings field; Windows graphics API IDs were present but are not translated here to that label.
- `Shadow.useGraphicAlpha`, RenderTexture filter/aniso/mipmap, preview camera rotation, and full runtime Canvas defaults are not printed by the temporary F10 script.

## Files touched in this diagnostic task

1. Added `Assets/Scripts/RoboClash/UI/RoboClashMenuDiagnostics.cs`.
2. Added `ROBOCLASH-MENU-VISUAL-DIAGNOSTIC-REPORT.md`.

No pre-existing file was modified during this diagnostic task.
