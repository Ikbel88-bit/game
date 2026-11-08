using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using RoboClash.UI;

namespace RoboClash
{
    public sealed class RoboClashSplitScreenManager : MonoBehaviour
    {
        public bool twoPlayerSideBySide = true;

        private RoboClashMatchManager matchManager;
        private TextMeshProUGUI overviewScoreboard;
        private float nextScoreboardUpdate;
        private readonly List<GameObject> generatedObjects = new List<GameObject>();

        public Rect GetViewport(int index, int count)
        {
            if (count <= 1 || index < 0 || index >= count)
                return new Rect(0f, 0f, 1f, 1f);
            if (count == 2)
                return twoPlayerSideBySide
                    ? new Rect(index * 0.5f, 0f, 0.5f, 1f)
                    : new Rect(0f, index == 0 ? 0.5f : 0f, 1f, 0.5f);

            var x = (index % 2) * 0.5f;
            var y = index < 2 ? 0.5f : 0f;
            return new Rect(x, y, 0.5f, 0.5f);
        }

        public void Setup(IReadOnlyList<RoboClashRobotMovement> players,
            RoboClashMatchManager manager)
        {
            if (players == null || players.Count < 2 || manager == null)
                return;

            ClearGeneratedObjects();
            matchManager = manager;
            for (var i = 0; i < players.Count; i++)
            {
                var movement = players[i];
                var camera = movement != null ? movement.PlayerCamera : null;
                if (camera == null)
                    continue;

                camera.rect = GetViewport(i, players.Count);
                camera.depth = i;
                camera.tag = i == 0 ? "MainCamera" : "Untagged";
            }

            ConfigureAudioListeners(players[0] != null ? players[0].PlayerCamera : null);
            BuildDividers(players.Count);
            if (players.Count == 3)
                BuildOverviewQuadrant(new Rect(0.5f, 0f, 0.5f, 0.5f));
        }

        private void ClearGeneratedObjects()
        {
            foreach (var generated in generatedObjects)
            {
                if (generated == null)
                    continue;
                generated.SetActive(false);
                Destroy(generated);
            }
            generatedObjects.Clear();
            overviewScoreboard = null;
        }

        private void ConfigureAudioListeners(Camera primaryCamera)
        {
            if (primaryCamera == null)
                return;

            var primaryListener = primaryCamera.GetComponent<AudioListener>();
            if (primaryListener == null)
                primaryListener = primaryCamera.gameObject.AddComponent<AudioListener>();

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            foreach (var listener in listeners)
            {
                if (listener != null && listener.gameObject.scene == gameObject.scene)
                    listener.enabled = listener == primaryListener;
            }
        }

        private void BuildDividers(int count)
        {
            var canvasGo = new GameObject("SplitScreenDividers", typeof(RectTransform),
                typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            generatedObjects.Add(canvasGo);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            if (count == 2 && twoPlayerSideBySide)
                Line(canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(4f, 4000f));
            else if (count == 2)
                Line(canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(4000f, 4f));
            else if (count >= 3)
            {
                Line(canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(4f, 4000f));
                Line(canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(4000f, 4f));
            }
        }

        private static void Line(Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = RoboClashUiKit.RoundedRectangle;
            image.color = Color.black;
        }

        private void BuildOverviewQuadrant(Rect viewport)
        {
            if (!TryGetArenaBounds(out var bounds))
            {
                Debug.LogWarning(
                    "Multiplayer overview camera was not created because no ArenaFloor/ArenaWall renderers were found.",
                    this);
                return;
            }

            var cameraObject = new GameObject("Multiplayer Overview Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            generatedObjects.Add(cameraObject);
            cameraObject.transform.SetPositionAndRotation(
                bounds.center + Vector3.up * 80f,
                Quaternion.Euler(90f, 0f, 0f));
            var overviewCamera = cameraObject.GetComponent<Camera>();
            overviewCamera.rect = viewport;
            overviewCamera.depth = 10f;
            overviewCamera.orthographic = true;
            overviewCamera.clearFlags = CameraClearFlags.Skybox;
            overviewCamera.cullingMask = Physics.AllLayers;
            var viewportAspect = Screen.height > 0
                ? (Screen.width * viewport.width) / (Screen.height * viewport.height)
                : 1f;
            overviewCamera.orthographicSize = Mathf.Max(1f, Mathf.Max(
                bounds.extents.z,
                bounds.extents.x / Mathf.Max(0.1f, viewportAspect)) * 1.1f);
            overviewCamera.nearClipPlane = 0.1f;
            overviewCamera.farClipPlane = 200f;
            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
                cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;

            var overlayObject = new GameObject("Overview Scoreboard", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            overlayObject.transform.SetParent(transform, false);
            generatedObjects.Add(overlayObject);
            var canvas = overlayObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 510;
            var scaler = overlayObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("Scoreboard Panel", typeof(RectTransform),
                typeof(Image));
            panelObject.transform.SetParent(overlayObject.transform, false);
            var panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.52f, 0.02f);
            panel.anchorMax = new Vector2(0.98f, 0.18f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            var panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = RoboClashUiKit.RoundedRectangle;
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            var textObject = new GameObject("Scoreboard Text", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);
            overviewScoreboard = textObject.GetComponent<TextMeshProUGUI>();
            overviewScoreboard.font = TMP_Settings.defaultFontAsset;
            overviewScoreboard.alignment = TextAlignmentOptions.Center;
            overviewScoreboard.fontSize = 24f;
            overviewScoreboard.color = Color.white;
            overviewScoreboard.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private bool TryGetArenaBounds(out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != gameObject.scene ||
                    (renderer.GetComponentInParent<ArenaFloor>() == null &&
                     renderer.GetComponentInParent<ArenaWall>() == null))
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void Update()
        {
            if (overviewScoreboard == null || matchManager == null ||
                Time.unscaledTime < nextScoreboardUpdate)
                return;

            nextScoreboardUpdate = Time.unscaledTime + 0.2f;
            overviewScoreboard.text = matchManager.GetMultiplayerScoreboardText();
        }
    }
}
