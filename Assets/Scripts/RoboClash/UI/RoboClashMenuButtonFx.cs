using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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
        private TextMeshProUGUI chevron;
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
            TextMeshProUGUI chevronText, Color idle, Color hot, bool isPrimary)
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
