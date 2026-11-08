using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RoboClash;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class RoboClashMenuDiagnostics : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Debug.isDebugBuild) return;
        if (FindAnyObjectByType<RoboClashMenuDiagnostics>() != null) return;
        var go = new GameObject("RoboClash Menu Diagnostics");
        DontDestroyOnLoad(go);
        go.AddComponent<RoboClashMenuDiagnostics>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) Dump();
    }

    static void Dump()
    {
        var flaggedItems = new List<string>();
        var canvasLines = new StringBuilder();
        var textLines = new StringBuilder();
        var imageLines = new StringBuilder();
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        var images = FindObjectsByType<Image>(FindObjectsInactive.Include);

        foreach (var canvas in canvases)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            canvasLines.AppendLine(
                $"{GetGameObjectPath(canvas.gameObject)}: renderMode={canvas.renderMode} " +
                $"sortingOrder={canvas.sortingOrder} pixelPerfect={canvas.pixelPerfect} " +
                $"scalerMode={(scaler != null ? scaler.uiScaleMode.ToString() : "none")} " +
                $"referenceResolution={(scaler != null ? scaler.referenceResolution.ToString() : "n/a")} " +
                $"matchWidthOrHeight={(scaler != null ? scaler.matchWidthOrHeight.ToString("0.###") : "n/a")} " +
                $"computedScaleFactor={canvas.scaleFactor:0.###}");
        }

        foreach (var text in texts)
        {
            var canvas = text.canvas;
            var scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            var effectivePixelSize = text.fontSize * scaleFactor;
            var fractionalOffset = GetFractionalPixelOffset(text.rectTransform, canvas,
                out var screenPosition);
            var objectPath = GetGameObjectPath(text.gameObject);
            var flags = new List<string>();
            if (effectivePixelSize < 14f)
                flags.Add($"effective size {effectivePixelSize:0.0}px < 14px");
            if (text.enableAutoSizing)
                flags.Add("auto-sizing enabled");
            if (fractionalOffset)
                flags.Add($"fractional screen position ({screenPosition.x:0.###}, {screenPosition.y:0.###})");

            if (flags.Count > 0)
                flaggedItems.Add($"[TEXT] {objectPath}: {string.Join(", ", flags)}");

            textLines.AppendLine(
                $"{objectPath}: font={(text.font != null ? text.font.name : "null")} " +
                $"fontSize={text.fontSize} autoSizing={text.enableAutoSizing} " +
                $"effectivePixelSize={effectivePixelSize:0.0}px " +
                $"screenPosition={(fractionalOffset || canvas != null ? screenPosition.ToString("0.###") : "n/a")} " +
                $"fractionalPixelOffset={fractionalOffset}");
        }

        foreach (var image in images)
        {
            var sprite = image.sprite;
            var texture = sprite != null ? sprite.texture : null;
            var displayedSize = GetDisplayedImageSize(image, out var hasDisplayedSize);
            var isSmallTextureUpscaled = texture != null &&
                                         (texture.width < 128 || texture.height < 128) &&
                                         hasDisplayedSize &&
                                         (displayedSize.x > texture.width + 1f ||
                                          displayedSize.y > texture.height + 1f);
            var objectPath = GetGameObjectPath(image.gameObject);
            if (isSmallTextureUpscaled)
            {
                flaggedItems.Add(
                    $"[IMAGE] {objectPath}: small texture {texture.width}x{texture.height}px " +
                    $"displayed at {displayedSize.x:0}x{displayedSize.y:0}px");
            }

            imageLines.AppendLine(
                $"{objectPath}: sprite={(sprite != null ? sprite.name : "none")} " +
                $"texture={(texture != null ? texture.name : "none")} " +
                $"textureSize={(texture != null ? $"{texture.width}x{texture.height}" : "n/a")} " +
                $"filterMode={(texture != null ? texture.filterMode.ToString() : "n/a")} " +
                $"mipmaps={(texture != null ? texture.mipmapCount.ToString() : "n/a")} " +
                $"displayedScreenSize={(hasDisplayedSize ? $"{displayedSize.x:0.##}x{displayedSize.y:0.##}" : "n/a")} " +
                $"smallTextureUpscaled={isSmallTextureUpscaled}");
        }

        flaggedItems.Sort(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.AppendLine("=== RoboClash menu diagnostic ===");
        var currentResolution = Screen.currentResolution;
        var mainDisplay = Display.main;
        var savedSettings = RoboClashSaveService.Current.settings;
        sb.AppendLine("\n--- Flagged UI items (sorted) ---");
        if (flaggedItems.Count == 0)
            sb.AppendLine("None.");
        else
            foreach (var item in flaggedItems)
                sb.AppendLine(item);

        sb.AppendLine("\n--- Display and saved settings ---");
        sb.AppendLine(
            $"Screen: {Screen.width}x{Screen.height} fullScreen={Screen.fullScreen} " +
            $"fullScreenMode={Screen.fullScreenMode} currentResolution={currentResolution} " +
            $"dpi={Screen.dpi:0.##} nativeResolution=" +
            $"{(mainDisplay != null ? $"{mainDisplay.systemWidth}x{mainDisplay.systemHeight}" : "unavailable")} " +
            $"savedResolution={savedSettings.resolutionWidth}x{savedSettings.resolutionHeight} " +
            $"savedFullscreen={savedSettings.fullscreen}");

        var q = QualitySettings.GetQualityLevel();
        var qualityNames = QualitySettings.names;
        var qualityName = q >= 0 && q < qualityNames.Length ? qualityNames[q] : "unavailable";
        sb.AppendLine($"Quality: {qualityName} (index {q}) antiAliasing={QualitySettings.antiAliasing} " +
                      $"aniso={QualitySettings.anisotropicFiltering} vsync={QualitySettings.vSyncCount}");
        var rp = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine($"Active render pipeline: {(rp != null ? rp.name : "Built-in")}");
        if (rp is UniversalRenderPipelineAsset urp)
            sb.AppendLine($"URP: msaa={urp.msaaSampleCount} renderScale={urp.renderScale} hdr={urp.supportsHDR} " +
                          $"upscaling={urp.upscalingFilter}");

        sb.AppendLine("\n--- Sprites in Resources/Menu ---");
        foreach (var s in Resources.LoadAll<Sprite>("Menu"))
            sb.AppendLine($"{s.name}: rect={s.rect.width}x{s.rect.height} texture={s.texture.width}x{s.texture.height} " +
                          $"format={s.texture.format} mips={s.texture.mipmapCount} filter={s.texture.filterMode}");

        sb.AppendLine("\n--- Volumes ---");
        foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include))
        {
            sb.AppendLine($"{v.name}: global={v.isGlobal} weight={v.weight} priority={v.priority} " +
                          $"profile={(v.sharedProfile != null ? v.sharedProfile.name : "none")}");
            if (v.sharedProfile != null)
                foreach (var c in v.sharedProfile.components)
                    sb.AppendLine($"    {c.GetType().Name} active={c.active}");
        }

        sb.AppendLine("\n--- Cameras ---");
        foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            sb.AppendLine($"{c.name}: enabled={c.enabled} active={c.gameObject.activeInHierarchy} clear={c.clearFlags} " +
                          $"bg={c.backgroundColor} mask={c.cullingMask} fov={c.fieldOfView} ortho={c.orthographic} " +
                          $"hdr={c.allowHDR} msaa={c.allowMSAA} pos={c.transform.position}");
            var data = c.GetUniversalAdditionalCameraData();
            if (data != null)
                sb.AppendLine($"    URP: postProcessing={data.renderPostProcessing} aa={data.antialiasing} type={data.renderType}");
            if (c.targetTexture != null)
            {
                var t = c.targetTexture;
                sb.AppendLine($"    RT: {t.width}x{t.height} format={t.format} depth={t.depth} aa={t.antiAliasing} " +
                              $"filter={t.filterMode} aniso={t.anisoLevel} mips={t.useMipMap}");
            }
        }

        sb.AppendLine("\n--- Lights ---");
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include))
            sb.AppendLine($"{l.name}: type={l.type} color={l.color} intensity={l.intensity} range={l.range} " +
                          $"shadows={l.shadows} mask={l.cullingMask} layer={l.gameObject.layer} active={l.gameObject.activeInHierarchy}");

        sb.AppendLine("\n--- Canvases ---");
        sb.Append(canvasLines);
        foreach (var canvas in canvases)
        {
            if (canvas.isRootCanvas)
                Walk((RectTransform)canvas.transform, 0, sb);
        }

        sb.AppendLine("\n--- Unity UI Text components (including inactive) ---");
        sb.Append(textLines);
        sb.AppendLine("\n--- Unity UI Image components (including inactive) ---");
        sb.Append(imageLines);

        var report = sb.ToString();
        GUIUtility.systemCopyBuffer = report;
        var filePath = Path.Combine(Application.persistentDataPath, "menu-diagnostic.txt");
        File.WriteAllText(filePath, report);
        Debug.Log($"[Diag] Dump copied to clipboard and written to {filePath}");
    }

    static string GetGameObjectPath(GameObject gameObject)
    {
        var path = gameObject.name;
        var parent = gameObject.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    static bool GetFractionalPixelOffset(RectTransform rectTransform, Canvas canvas,
        out Vector2 screenPosition)
    {
        if (rectTransform == null || canvas == null)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        screenPosition = RectTransformUtility.WorldToScreenPoint(
            canvas.worldCamera, rectTransform.position);
        return HasFractionalPixel(screenPosition.x) || HasFractionalPixel(screenPosition.y);
    }

    static bool HasFractionalPixel(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) > 0.01f;
    }

    static Vector2 GetDisplayedImageSize(Image image, out bool hasDisplayedSize)
    {
        var rectTransform = image.rectTransform;
        var canvas = image.canvas;
        if (rectTransform == null)
        {
            hasDisplayedSize = false;
            return Vector2.zero;
        }

        if (canvas == null)
        {
            hasDisplayedSize = false;
            return Vector2.zero;
        }

        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (var i = 0; i < corners.Length; i++)
        {
            var point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[i]);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }

        hasDisplayedSize = IsFinite(maximum.x - minimum.x) && IsFinite(maximum.y - minimum.y);
        return hasDisplayedSize ? maximum - minimum : Vector2.zero;
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    static void Walk(RectTransform rt, int depth, StringBuilder sb)
    {
        if (!rt.gameObject.activeInHierarchy) return;
        var indent = new string(' ', depth * 2);
        var sizePx = new Vector2(rt.rect.width * rt.lossyScale.x, rt.rect.height * rt.lossyScale.y);
        var line = new StringBuilder($"{indent}{rt.name} [{sizePx.x:0}x{sizePx.y:0}px]");

        var img = rt.GetComponent<Image>();
        if (img != null)
            line.Append($" Image(color={img.color} sprite={(img.sprite != null ? img.sprite.name : "none")} " +
                        $"type={img.type} raycast={img.raycastTarget})");
        var raw = rt.GetComponent<RawImage>();
        if (raw != null)
            line.Append($" RawImage(tex={(raw.texture != null ? raw.texture.width + "x" + raw.texture.height : "none")} color={raw.color})");
        var txt = rt.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            var scale = txt.canvas != null ? txt.canvas.scaleFactor : 1f;
            line.Append($" TMP('{txt.text}' font={(txt.font != null ? txt.font.name : "null")} size={txt.fontSize} " +
                        $"effectivePx={txt.fontSize * scale:0.0} style={txt.fontStyle} color={txt.color} " +
                        $"autoSizing={txt.enableAutoSizing} rich={txt.richText})");
        }
        foreach (var sh in rt.GetComponents<Shadow>())
            line.Append($" {sh.GetType().Name}(color={sh.effectColor} dist={sh.effectDistance})");
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg != null) line.Append($" CanvasGroup(alpha={cg.alpha})");
        if (rt.GetComponent<Mask>() != null) line.Append(" Mask");
        if (rt.GetComponent<RectMask2D>() != null) line.Append(" RectMask2D");
        sb.AppendLine(line.ToString());

        for (int i = 0; i < rt.childCount; i++)
            if (rt.GetChild(i) is RectTransform child) Walk(child, depth + 1, sb);
    }
}
