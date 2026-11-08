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
