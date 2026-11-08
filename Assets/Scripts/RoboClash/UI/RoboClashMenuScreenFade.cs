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
