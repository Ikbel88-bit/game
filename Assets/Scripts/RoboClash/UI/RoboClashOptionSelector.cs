using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace RoboClash.UI
{
    [DisallowMultipleComponent]
    public sealed class RoboClashOptionSelector : MonoBehaviour
    {
        private const float OptionHeight = 34f;
        private const float MaximumPopupHeight = 238f;
        private static readonly Color GlassColor = new Color(0.03f, 0.06f, 0.11f, 0.98f);
        private static readonly Color ControlColor = new Color(0.08f, 0.16f, 0.26f, 0.96f);
        private static readonly Color ControlHotColor = new Color(0.12f, 0.23f, 0.35f, 1f);
        private static readonly Color OrangeColor = new Color(1f, 0.58f, 0.16f, 1f);
        private static readonly Color OrangeHotColor = new Color(1f, 0.72f, 0.35f, 1f);

        [System.Serializable]
        public sealed class IntEvent : UnityEvent<int>
        {
        }

        public IntEvent onValueChanged = new IntEvent();

        private readonly List<string> options = new List<string>();
        private TextMeshProUGUI label;
        private TextMeshProUGUI arrow;
        private Button selectorButton;
        private Button[] optionButtons = new Button[0];
        private RectTransform popupRect;
        private RectTransform popupParent;
        private RectTransform hostRect;
        private ScrollRect scrollRect;
        private TMP_FontAsset font;
        private int index;
        private bool isInteractable = true;

        public int OptionCount => options.Count;

        public int value
        {
            get => index;
            set
            {
                SetValueWithoutNotify(value);
                if (isInteractable)
                    onValueChanged.Invoke(index);
            }
        }

        public bool interactable
        {
            get => isInteractable;
            set
            {
                isInteractable = value;
                if (selectorButton != null)
                    selectorButton.interactable = value;
            }
        }

        public static RoboClashOptionSelector Attach(GameObject host, TMP_FontAsset font,
            Transform popupParent)
        {
            if (host == null || popupParent == null)
            {
                Debug.LogError("Cannot attach a RoboClash option selector without its host and popup parent.");
                return null;
            }

            var selector = host.GetComponent<RoboClashOptionSelector>();
            if (selector == null)
                selector = host.AddComponent<RoboClashOptionSelector>();

            selector.hostRect = host.GetComponent<RectTransform>();
            selector.font = font;
            selector.popupParent = popupParent as RectTransform;
            if (selector.popupParent == null)
            {
                Debug.LogError("The RoboClash option selector popup parent must have a RectTransform.", host);
                return null;
            }

            var background = host.GetComponent<Image>();
            if (background == null)
                background = host.AddComponent<Image>();
            background.color = Color.white;
            background.raycastTarget = true;
            RoboClashUiKit.StyleRoundedPanel(background);

            selector.selectorButton = host.GetComponent<Button>();
            if (selector.selectorButton == null)
                selector.selectorButton = host.AddComponent<Button>();
            selector.selectorButton.targetGraphic = background;
            var selectorColors = selector.selectorButton.colors;
            selectorColors.normalColor = ControlColor;
            selectorColors.highlightedColor = ControlHotColor;
            selectorColors.pressedColor = OrangeColor;
            selectorColors.selectedColor = OrangeColor;
            selectorColors.disabledColor = new Color(0.08f, 0.12f, 0.17f, 0.65f);
            selector.selectorButton.colors = selectorColors;
            selector.selectorButton.onClick.AddListener(selector.TogglePopup);

            selector.label = MakeText(host.transform, font, 15);
            selector.label.rectTransform.anchorMin = Vector2.zero;
            selector.label.rectTransform.anchorMax = Vector2.one;
            selector.label.rectTransform.offsetMin = new Vector2(12f, 0f);
            selector.label.rectTransform.offsetMax = new Vector2(-38f, 0f);
            selector.label.alignment = TextAlignmentOptions.MidlineLeft;

            selector.arrow = MakeText(host.transform, font, 14);
            selector.arrow.text = "v";
            selector.arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
            selector.arrow.rectTransform.anchorMax = Vector2.one;
            selector.arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
            selector.arrow.rectTransform.sizeDelta = new Vector2(30f, 0f);
            selector.arrow.alignment = TextAlignmentOptions.Midline;

            selector.CreatePopup();
            selector.interactable = selector.isInteractable;
            selector.RefreshLabel();
            return selector;
        }

        public void ClearOptions()
        {
            options.Clear();
            index = 0;
            RefreshLabel();
            RebuildOptionButtons();
        }

        public void AddOptions(IEnumerable<string> newOptions)
        {
            if (newOptions == null)
            {
                Debug.LogError("Cannot add a missing option list to a RoboClash selector.", this);
                return;
            }

            options.AddRange(newOptions);
            index = options.Count == 0 ? 0 : Mathf.Clamp(index, 0, options.Count - 1);
            RefreshLabel();
            RebuildOptionButtons();
        }

        public void SetValueWithoutNotify(int newIndex)
        {
            index = options.Count == 0 ? 0 : Mathf.Clamp(newIndex, 0, options.Count - 1);
            RefreshLabel();
            UpdateOptionHighlight();
        }

        private void OnDisable()
        {
            ClosePopup();
        }

        private void CreatePopup()
        {
            var overlayObject = new GameObject("Option List Overlay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            overlayObject.transform.SetParent(popupParent, false);
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            var overlayButton = overlayObject.GetComponent<Button>();
            overlayButton.targetGraphic = overlayImage;
            overlayButton.onClick.AddListener(ClosePopup);

            var popupObject = new GameObject("Option List",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            popupObject.transform.SetParent(overlayObject.transform, false);
            popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.pivot = new Vector2(0f, 1f);
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.zero;
            popupObject.GetComponent<Image>().color = GlassColor;
            RoboClashUiKit.StyleRoundedPanel(popupObject.GetComponent<Image>());

            var viewportObject = new GameObject("Viewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(popupObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(4f, 4f);
            viewport.offsetMax = new Vector2(-4f, -4f);
            viewportObject.GetComponent<Image>().color = Color.white;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject("Content",
                typeof(RectTransform), typeof(CanvasRenderer));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            scrollRect = popupObject.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = OptionHeight;
            overlayObject.SetActive(false);
            RebuildOptionButtons();
        }

        private void RebuildOptionButtons()
        {
            if (scrollRect == null)
                return;

            var content = scrollRect.content;
            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            optionButtons = new Button[options.Count];
            content.sizeDelta = new Vector2(0f, options.Count * OptionHeight);
            for (var i = 0; i < options.Count; i++)
            {
                var row = new GameObject("Option " + i,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                row.transform.SetParent(content, false);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(0f, OptionHeight);
                rowRect.anchoredPosition = new Vector2(0f, -i * OptionHeight);

                var rowImage = row.GetComponent<Image>();
                rowImage.color = Color.white;
                RoboClashUiKit.StyleRoundedPanel(rowImage);
                var rowButton = row.GetComponent<Button>();
                rowButton.targetGraphic = rowImage;
                var rowColors = rowButton.colors;
                rowColors.normalColor = ControlColor;
                rowColors.highlightedColor = ControlHotColor;
                rowColors.pressedColor = OrangeColor;
                rowColors.selectedColor = OrangeColor;
                rowColors.disabledColor = new Color(0.08f, 0.12f, 0.17f, 0.65f);
                rowButton.colors = rowColors;
                var optionIndex = i;
                rowButton.onClick.AddListener(() => SelectOption(optionIndex));

                var optionText = MakeText(row.transform, font, 14);
                optionText.text = options[i];
                optionText.rectTransform.offsetMin = new Vector2(10f, 0f);
                optionText.rectTransform.offsetMax = new Vector2(-8f, 0f);
                optionText.alignment = TextAlignmentOptions.MidlineLeft;
                optionButtons[i] = rowButton;
            }

            UpdateOptionHighlight();
        }

        private void TogglePopup()
        {
            if (!isInteractable || popupRect == null)
                return;

            var overlay = popupRect.parent.gameObject;
            if (overlay.activeSelf)
            {
                ClosePopup();
                return;
            }

            PositionPopup();
            overlay.transform.SetAsLastSibling();
            overlay.SetActive(true);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void PositionPopup()
        {
            var corners = new Vector3[4];
            hostRect.GetWorldCorners(corners);
            var bottomLeft = popupParent.InverseTransformPoint(corners[0]);
            var topLeft = popupParent.InverseTransformPoint(corners[1]);
            var bottomY = bottomLeft.y;
            var topY = topLeft.y;
            var parentRect = popupParent.rect;
            var desiredHeight = Mathf.Min(options.Count * OptionHeight + 8f, MaximumPopupHeight);
            var spaceBelow = bottomY - parentRect.yMin;
            var spaceAbove = parentRect.yMax - topY;
            var showBelow = spaceBelow >= desiredHeight || spaceBelow >= spaceAbove;
            desiredHeight = Mathf.Min(desiredHeight, showBelow ? spaceBelow : spaceAbove);
            var popupTopY = showBelow ? bottomY : topY + desiredHeight;
            popupRect.anchoredPosition = new Vector2(
                bottomLeft.x - parentRect.xMin, popupTopY - parentRect.yMin);
            popupRect.sizeDelta = new Vector2(hostRect.rect.width, desiredHeight);
        }

        private void SelectOption(int selectedIndex)
        {
            ClosePopup();
            value = selectedIndex;
        }

        private void ClosePopup()
        {
            if (popupRect != null)
                popupRect.parent.gameObject.SetActive(false);
        }

        private void RefreshLabel()
        {
            if (label != null)
                label.text = options.Count > 0 ? options[index] : "-";
            if (selectorButton != null)
                selectorButton.interactable = isInteractable && options.Count > 0;
        }

        private void UpdateOptionHighlight()
        {
            if (optionButtons == null)
                return;

            for (var i = 0; i < optionButtons.Length; i++)
            {
                var colors = optionButtons[i].colors;
                var selected = i == index;
                colors.normalColor = selected ? OrangeColor : ControlColor;
                colors.highlightedColor = selected ? OrangeHotColor : ControlHotColor;
                colors.selectedColor = selected ? OrangeHotColor : ControlHotColor;
                optionButtons[i].colors = colors;
            }
        }

        private static TextMeshProUGUI MakeText(Transform parent, TMP_FontAsset font, int size)
        {
            var gameObject = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Midline;
            text.raycastTarget = false;
            RoboClashUiKit.StyleText(text, size);
            return text;
        }
    }
}
