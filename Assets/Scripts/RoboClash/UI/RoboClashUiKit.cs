using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoboClash.UI
{
    public static class RoboClashUiKit
    {
        public static readonly Color Cyan = new Color(0.2f, 0.85f, 1f, 1f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.2f, 1f);
        public static readonly Color Orange = new Color(1f, 0.5f, 0.15f, 1f);
        public static readonly Color Dim = new Color(0.45f, 0.5f, 0.58f, 1f);

        private static Sprite circle;
        private static Sprite ring;
        private static Sprite thickRing;
        private static Sprite vignette;
        private static Sprite minimapCircle;
        private static Sprite minimapRing;
        private static Sprite roundedRectangle;

        public static Sprite Circle => circle != null ? circle : circle = MakeRadial(256, 0f);
        public static Sprite Ring => ring != null ? ring : ring = MakeRadial(256, 0.96f);
        public static Sprite ThickRing =>
            thickRing != null ? thickRing : thickRing = MakeRadial(256, 0.84f);
        public static Sprite Vignette =>
            vignette != null ? vignette : vignette = MakeVignette(256);
        public static Sprite MinimapCircle =>
            minimapCircle != null ? minimapCircle : minimapCircle = MakeRadial(512, 0f);
        public static Sprite MinimapRing =>
            minimapRing != null ? minimapRing : minimapRing = MakeRadial(512, 0.96f);
        public static Sprite RoundedRectangle =>
            roundedRectangle != null ? roundedRectangle : roundedRectangle = MakeRoundedRectangle(64);

        private static Sprite MakeRadial(int size, float innerRatio)
        {
            var texture = CreateProceduralTexture(size);
            var pixels = new Color32[size * size];
            var radius = size * 0.5f;
            var innerRadius = radius * innerRatio;
            const float edgeBand = 1.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x + 0.5f - radius;
                    var dy = y + 0.5f - radius;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = 1f - Mathf.SmoothStep(
                        radius - edgeBand * 0.5f, radius + edgeBand * 0.5f, distance);
                    if (innerRatio > 0f)
                        alpha *= Mathf.SmoothStep(
                            innerRadius - edgeBand * 0.5f, innerRadius + edgeBand * 0.5f, distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeVignette(int size)
        {
            var texture = CreateProceduralTexture(size);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x / (size - 1f) - 0.5f) * 2f;
                    var ny = (y / (size - 1f) - 0.5f) * 2f;
                    var distance = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
                    var alpha = Mathf.SmoothStep(0.45f, 1f, distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRoundedRectangle(int size)
        {
            var texture = CreateProceduralTexture(size);
            var pixels = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var halfExtents = new Vector2(size * 0.5f, size * 0.5f);
            const float cornerRadius = 12f;
            const float edgeBand = 1.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var q = new Vector2(
                        Mathf.Abs(point.x - center.x) - (halfExtents.x - cornerRadius),
                        Mathf.Abs(point.y - center.y) - (halfExtents.y - cornerRadius));
                    var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                    var signedDistance = outside +
                                         Mathf.Min(Mathf.Max(q.x, q.y), 0f) - cornerRadius;
                    var alpha = 1f - Mathf.SmoothStep(
                        -edgeBand * 0.5f, edgeBand * 0.5f, signedDistance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(16f, 16f, 16f, 16f));
        }

        private static Texture2D CreateProceduralTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        public static void Stretch(RectTransform rectTransform, float inset = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Img(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void StyleRoundedPanel(Image image)
        {
            if (image == null)
                return;
            image.sprite = RoundedRectangle;
            image.type = Image.Type.Sliced;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, int size,
            Color color, TextAnchor anchor, FontStyles style = FontStyles.Normal,
            bool important = false)
        {
            var rectTransform = Rect(name, parent);
            Stretch(rectTransform);
            var label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = color;
            label.alignment = ToTextAlignmentOptions(anchor);
            label.raycastTarget = false;
            StyleText(label, size, important);
            label.fontStyle = style;
            return label;
        }

        public static void StyleText(TextMeshProUGUI text, int baseSize, bool important = false)
        {
            if (text == null)
                return;

            if (text.font == null)
                text.font = TMP_Settings.defaultFontAsset;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.fontSize = baseSize;
            text.enableAutoSizing = false;
            text.richText = false;
            text.outlineWidth = 0.15f;
            text.outlineColor = new Color32(0, 0, 0, 217);
            text.overflowMode = TextOverflowModes.Overflow;
            if (important)
            {
                var material = text.fontMaterial;
                material.EnableKeyword("UNDERLAY_ON");
                material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
                material.SetFloat("_UnderlayOffsetX", 0.1f);
                material.SetFloat("_UnderlayOffsetY", -0.1f);
                material.SetFloat("_UnderlayDilate", 0.1f);
                material.SetFloat("_UnderlaySoftness", 0f);
                text.fontMaterial = material;
            }

            var rectTransform = text.rectTransform;
            rectTransform.anchoredPosition = Round(rectTransform.anchoredPosition);
            rectTransform.sizeDelta = Round(rectTransform.sizeDelta);
        }

        public static TextAlignmentOptions ToTextAlignmentOptions(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static Vector2 Round(Vector2 value)
        {
            return new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
        }
    }
}
